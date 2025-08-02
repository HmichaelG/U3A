using DevExpress.Blazor.RichEdit;
using DevExpress.Drawing;
using DevExpress.XtraRichEdit.Import.Rtf;
using DevExpress.XtraRichEdit.Layout;
using Eway.Rapid.Abstractions.Response;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualBasic;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using U3A.BusinessRules;
using U3A.Database;
using U3A.Model;
using U3A.Model;

namespace U3A.Services;

public class MemberFeeCalculationService
{
    ConcurrentBag<MemberFee> MemberFees { get; set; } = new();
    public int BillingYear { get; set; }
    public Term BillingTerm { get; set; }

    List<Person> People { get; set; } = null;
    SystemSettings Settings { get; set; } = null;
    List<Fee> Fees { get; set; } = null;
    List<Receipt> Receipts { get; set; } = null;
    Term[] Terms { get; set; } = null;
    List<Enrolment> Enrolments { get; set; } = null;
    List<Class> Classes { get; set; } = null;

    DateTime localTime;
    private MemberFeeCalculationService()
    {
        MemberFees = new ConcurrentBag<MemberFee>();
    }

    public static async Task<MemberFeeCalculationService> CreateAsync(U3ADbContext dbc, Term Term, Person? Person = null)
    {
        var instance = new MemberFeeCalculationService();
        await instance.InitializeAsync(dbc, Term, Person);
        return instance;
    }
    public static async Task<MemberFeeCalculationService> CreateAsync(U3ADbContext dbc, Person? Person = null)
    {
        var instance = new MemberFeeCalculationService();
        await instance.InitializeAsync(dbc,null, Person);
        return instance;
    }

    private async Task InitializeAsync(U3ADbContext dbc, Term? Term, Person? person)
    {
        localTime = dbc.GetLocalTime(DateTime.UtcNow);
        Settings = await dbc.SystemSettings.FirstOrDefaultAsync();
        if (Term != null)
        {
            BillingTerm = Term;
            BillingYear = Term.Year;
            Terms = await dbc.Term
                        .Where(x => x.Year == BillingYear)
                        .OrderBy(x => x.TermNumber)
                        .ToArrayAsync();
        }
        else {
            Terms = await dbc.Term
                        .Where(x => x.Year == localTime.Year)
                        .OrderBy(x => x.TermNumber)
                        .ToArrayAsync();
            GetBillingTerm(); 
        }
        People = await dbc.Person.AsNoTracking()
                        .Where(x => person == null || x.ID == person.ID)
                        .ToListAsync();
        Fees = await dbc.Fee.AsNoTracking().IgnoreQueryFilters()
                            .Where(x => (person == null || x.PersonID == person.ID)
                                        && !x.IsDeleted && x.ProcessingYear >= BillingYear)
                            .ToListAsync();
        Receipts = await dbc.Receipt.AsNoTracking().IgnoreQueryFilters()
                            .Where(x => (person == null || x.PersonID == person.ID)
                                        && !x.IsDeleted && x.ProcessingYear >= BillingYear)
                            .ToListAsync();
        Enrolments = await dbc.Enrolment.AsNoTracking().IgnoreQueryFilters()
                            .Include(x => x.Term)
                            .Include(x => x.Course).ThenInclude(x => x.Classes)
                            .Include(x => x.Class)
                            .Where(x => (person == null || x.PersonID == person.ID)
                                        && !x.IsDeleted && x.Term.Year == BillingYear).ToListAsync();
        Classes = await dbc.Class.AsNoTracking()
                                .Include(x => x.Course)
                                .Where(x => x.Course.Year == BillingYear).ToListAsync();
    }



    public PersonFinancialStatus? PersonWithFinancialStatus { get; set; }

    public List<MemberFee> GetMemberFees(Guid PersonID) => MemberFees
                                                  .Where(x => x.PersonID == PersonID)
                                                  .OrderBy(x => x.Date)
                                                  .ThenBy(x => x.SortOrder)
                                                  .ToList();
    public List<MemberFee> GetAllocatedMemberFees(Person person)
    {
        return AllocateMemberPayments(MemberFees, person,
                ShowFullAllocation: true, AddUnallocatedCredit: true)
                .ToList();
    }

    public async Task<List<MemberPaymentAvailable>> GetAvailableMemberPaymentsAsync(Person person = null)
    {
        var result = new List<MemberPaymentAvailable>();
        if (person == null) { person = People.FirstOrDefault(); }
        if (Settings.AllowedMemberFeePaymentTypes == MemberFeePaymentType.PerYearAndPerSemester)
        {
            if (await IsComplimentaryMembership(person)) { return result; }
            decimal yearlyFee = GetTermFee(BillingTerm.TermNumber)
                                    + await GetTotalOtherMembershipFees(person);
            decimal semesterFee = decimal.Round(yearlyFee / 2, 2);
            var totalFee = await CalculateFeeAsync(person);
            if (totalFee < yearlyFee) { return result; }
            if (BillingTerm.TermNumber <= 2)
            {
                result.Add(new MemberPaymentAvailable()
                {
                    Amount = totalFee,
                    Description = $"{totalFee.ToString("c2")} {BillingYear} Full Year Fee",
                    TermsPaid = null
                });
                result.Add(new MemberPaymentAvailable()
                {
                    Amount = totalFee - semesterFee,
                    Description = $"{(totalFee - semesterFee).ToString("c2")} {BillingYear} Semester (Term 1 & 2) Fee",
                    TermsPaid = 2
                }); ;
            }
        }
        return result;
    }
    async Task<decimal> GetTotalOtherMembershipFees(Person person)
    {
        var result = 0m;
        result = Fees
                    .Where(x => !x.Person.IsDeleted
                                && x.PersonID == person.ID
                                && x.IsMembershipFee)
                    .Sum(x => x.Amount);
        return result;
    }

    /// <summary>
    /// Calculate fees for an individual. Used for Member Portal calculations
    /// </summary>
    /// <param name="U3Adbfactory"></param>
    /// <param name="person"></param>
    /// <returns></returns>
    public async Task<decimal> CalculateFeeAsync()
    {
        return await CalculateFeeAsync(null, null);
    }
    public async Task<decimal> CalculateFeeAsync(int CalculateForTerm)
    {
        return await CalculateFeeAsync(null, CalculateForTerm);
    }

    public async Task<decimal> CalculateFeeAsync(Person? person = null, int? CalculateForTerm = null)
    {
        var result = decimal.Zero;
        if (person == null) { person = People.FirstOrDefault(); }
        var defaultDate = new DateTime(BillingYear, 1, 1);
        MemberFees.Clear();
        PersonWithFinancialStatus = new PersonFinancialStatus()
        {
            PersonBase = person,
            FirstName = person.FirstName,
            LastName = person.LastName,
            IsLifeMember = person.IsLifeMember,
            IsCourseLeader = person.IsCourseLeader,
            FinancialTo = person.FinancialTo,
            FinancialToTerm = person.FinancialToTerm,
            FinancialToText = person.FinancialToText,
            FinancialToBriefText = person.FinancialToBriefText,
            DateJoined = person.DateJoined,
            Mobile = person.AdjustedMobile,
            HomePhone = person.AdjustedHomePhone,
            Email = person.Email,
            Enrolments = await ActiveCourseCountAsync(person),
            Waitlisted = await WaitlistedCourseCountAsync(person)
        };
        if (Settings != null)
        {
            var isComplimentary = await IsComplimentaryMembership(person);
            // membership fees
            if (isComplimentary && Settings.IncludeMembershipFeeInComplimentary)
            {
                var complimentaryCalcDate = await GetComplimentaryCalculationDate(person);
                if (complimentaryCalcDate != null)
                {
                    AddFee(person,
                        MemberFeeSortOrder.Complimentary, defaultDate, $"{complimentaryCalcDate?.ToString("dd-MMM-yyyy")} {BillingYear} complimentary membership", 0.00M);
                }
                else
                {
                    AddFee(person,
                        MemberFeeSortOrder.Complimentary, defaultDate, $"{BillingYear} complimentary membership", 0.00M);
                }
            }
            else
            {
                PersonWithFinancialStatus.MembershipFees = await CalculateMembershipFeeAsync(person);
                var fee = PersonWithFinancialStatus.MembershipFees;
                if (fee != 0)
                {
                    if (CalculateForTerm.HasValue) { fee = decimal.Round(fee / 4m * (decimal)CalculateForTerm, 2); }
                    AddFee(person,
                        MemberFeeSortOrder.MemberFee, defaultDate, $"{BillingYear} membership fee", fee);
                }
            }
            if (person.Communication != "Email")
            {
                if (isComplimentary && Settings.IncludeMailSurchargeInComplimentary)
                {
                    AddFee(person,
                        MemberFeeSortOrder.MailSurcharge, defaultDate, $"{BillingYear} complimentary mail surcharge", 0.00M);
                }
                else
                {
                    PersonWithFinancialStatus.MailSurcharge = Settings.MailSurcharge;
                    AddFee(person,
                        MemberFeeSortOrder.MailSurcharge, defaultDate, $"{BillingYear} mail surcharge", Settings.MailSurcharge);
                }
            }
            // course fees
            await AddCourseFeesAsync(person, isComplimentary);
            await AddLeadersFeesAsync(person);
            // add fee adjustments
            await AddFeesAsync(person);
            // less payments
            await SubtractReceiptsAsync(person);
            // total due
            result = MemberFees
                        .Where(x => x.PersonID == person.ID)
                        .Sum(x => x.Amount);
        }
        return result;
    }

    private async Task<bool> IsComplimentaryMembership(Person person)
    {
        bool result = false;
        if (person.IsLifeMember) { result = true; }
        else
        {
            // A zero-valued receipt is created when a person is given complimentary membership
            result = Receipts.Any(x => x.PersonID == person.ID
                                    && x.FinancialTo >= BillingYear
                                    && x.Amount == 0);
        }
        if (PersonWithFinancialStatus != null)
            PersonWithFinancialStatus.IsComplimentary = result;
        return result;
    }
    private async Task<DateTime?> GetComplimentaryCalculationDate(Person person)
    {
        DateTime? result = null;
        Receipt? receipt;
        receipt = Receipts.FirstOrDefault(x => x.PersonID == person.ID
                                        && x.Amount == 0);
        if (receipt != null) { result = receipt.Date; }
        return result;
    }

    private async Task<Term> GetBillingTerm()
    {
        Term result = null;
        result = BusinessRule.CurrentEnrolmentTerm(Terms, localTime);
        if (result == null)
        {
            result = Terms.FirstOrDefault(x => x.IsDefaultTerm);
            if (result.TermNumber == 4 && localTime > result.EndDate)
            {
                result = null; // End of year, no enrolment period - no man's land.
            }
        }
        if (result != null) { BillingTerm = result; BillingYear = result.Year; }
        return result;
    }

    public async Task<decimal> CalculateMinimumFeePayableAsync()
    {
        return await CalculateMinimumFeePayableAsync(People.FirstOrDefault(), null);
    }
    public async Task<decimal> CalculateMinimumFeePayableAsync(int CalculateForTerm)
    {
        return await CalculateMinimumFeePayableAsync(People.FirstOrDefault(), CalculateForTerm);
    }
    public async Task<decimal> CalculateMinimumFeePayableAsync(Person? person = null, int? CalculateForTerm = null)
    {
        var result = decimal.Zero;
        if (person == null) { person = People.FirstOrDefault(); }
        if (BillingTerm != null && !await IsComplimentaryMembership(person))
        {
            result = await CalculateMembershipFeeAsync(person);
            result += Fees
                        .Where(x => !x.Person.IsDeleted
                                    && x.PersonID == person.ID
                                    && x.IsMembershipFee)
                        .Sum(x => x.Amount);
            if (CalculateForTerm.HasValue) { result = decimal.Round(result / 4m * (decimal)CalculateForTerm, 2); }
        }
        return result;
    }

    private async Task<decimal> CalculateMembershipFeeAsync(Person person)
    {
        if (person is Contact) { return 0; }
        decimal result = 0;
        bool foundTerm = false;
        result = Settings.MembershipFee; // set the default
        DateTime? feeDueDate = null;
        if (person.DateJoined?.Year == BillingYear) feeDueDate = person.DateJoined; // New member; use join date
        if (feeDueDate == null) // Renewing member
        {
            var firstReceiptDate = await GetFirstReceiptDateAsync(person);
            feeDueDate = firstReceiptDate ?? localTime.Date;
        }
        if (Terms.Length > 1)
        {
            // Ues the term fee if fee due date is within term enrollment period
            for (int i = 1; i < Terms.Length; i++)
            { // for terms 2 thru 4
                var lastTerm = Terms[i - 1];
                var thisTerm = Terms[i];
                if (feeDueDate > lastTerm.EnrolmentEndDate && feeDueDate <= thisTerm.EnrolmentEndDate)
                {
                    result = GetTermFee(thisTerm.TermNumber);
                    foundTerm = true;
                    break; // exit for
                }
            }
            // otherwise, use the term fee if date is within the current term.
            if (!foundTerm)
            {
                for (int i = 1; i < Terms.Length; i++)
                { // for terms 2 thru 4
                    var lastTerm = Terms[i - 1];
                    var thisTerm = Terms[i];
                    if (feeDueDate > lastTerm.EndDate && feeDueDate <= thisTerm.EndDate)
                    {
                        result = GetTermFee(thisTerm.TermNumber);
                        break; // exit for
                    }
                }
            }
        }
        return result;
    }

    private async Task<DateTime?> GetFirstReceiptDateAsync(Person person)
    {
        DateTime? result = null;
        Receipt? receipt = null;
        receipt = Receipts.OrderBy(x => x.Date).FirstOrDefault(x => !x.IsDeleted
                                        && x.PersonID == person.ID
                                        && x.Amount != 0
                                        && x.ProcessingYear == BillingYear);
        if (receipt != null) { result = receipt.Date; }
        return result;
    }

    private decimal GetTermFee(int TermNumber)
    {
        var result = Settings.MembershipFee;
        switch (TermNumber)
        {
            case 2:
                result = Settings.MembershipFeeTerm2;
                break;
            case 3:
                result = Settings.MembershipFeeTerm3;
                break;
            case 4:
                result = Settings.MembershipFeeTerm4;
                break;
            default:
                break;
        }
        return result;
    }

    private async Task AddFeesAsync(Person person)
    {
        List<Fee> fees;
        fees = Fees.Where(x => x.PersonID == person.ID
                                && x.Amount != 0
                                && x.ProcessingYear == BillingYear).ToList();
        foreach (var f in fees)
        {
            AddFee(person,
                    MemberFeeSortOrder.AdditionalFee, f.Date, f.Description, f.Amount);
            PersonWithFinancialStatus.OtherFees += f.Amount;
        }
    }
    private async Task SubtractReceiptsAsync(Person person)
    {
        List<Receipt> receipts;
        receipts = Receipts.Where(x => !x.IsDeleted
                                        && x.PersonID == person.ID
                                        && x.Amount != 0
                                        && x.ProcessingYear == BillingYear)
                            .OrderBy(x => x.Date).ToList();
        foreach (var r in receipts)
        {
            var description = "Payment received - thank you";
            var sortOrder = MemberFeeSortOrder.Receipt;
            if (r.Amount < 0)
            {
                sortOrder = MemberFeeSortOrder.Refund;
                description = $"Refund: {r.Description}";
            }
            AddFee(person, sortOrder, r.Date, description, -r.Amount);
            if (PersonWithFinancialStatus != null)
            {
                PersonWithFinancialStatus.AmountReceived += r.Amount;
                //receipts are ordered by date Asc.
                PersonWithFinancialStatus.LastReceipt = r.Date;
            }
        }
    }
    private async Task AddCourseFeesAsync(Person person, bool IsComplimentary)
    {
        DateOnly today = DateOnly.FromDateTime(localTime);
        var courseFeeAdded = new List<Guid>();
        List<Enrolment> enrolments;
        enrolments = Enrolments.Where(x => !x.IsDeleted
                                        && x.PersonID == person.ID
                                        && x.Term.Year == BillingYear
                                        && !x.IsWaitlisted).ToList();
        foreach (var t in Terms)
        {
            foreach (var e in enrolments.Where(x => x.TermID == t.ID))
            {
                DateOnly dateEnrolled = DateOnly.FromDateTime(e.DateEnrolled.Value);
                DateOnly dateDue = e.Course.CourseFeePerYearDueDate ?? dateEnrolled;
                if (dateDue <= today)
                {
                    if (e.Course.CourseFeePerYear != 0 && !courseFeeAdded.Contains(e.CourseID))
                    {
                        var description = $"{e.Course.Name} course fee";
                        var amount = e.Course.CourseFeePerYear;
                        var includeInComplimentary = Settings.IncludeCourseFeePerYearInComplimentary;
                        if (e.Course.OverrideComplimentaryPerYearFee) includeInComplimentary = false;
                        if (IsComplimentary && includeInComplimentary)
                        {
                            amount = 0;
                        }
                        if (!string.IsNullOrWhiteSpace(e.Course.CourseFeePerYearDescription))
                        {
                            description += $": {e.Course.CourseFeePerYearDescription}";
                        }
                        AddFee(person,
                            MemberFeeSortOrder.CourseFee,
                                ConvertDateOnlyToDateTime(dateDue),
                                description, amount, e.Course.Name);
                        if (PersonWithFinancialStatus != null)
                            PersonWithFinancialStatus.CourseFeesPerYear += amount;
                        courseFeeAdded.Add(e.CourseID);
                    }
                }
                var dueDateAdjustment = e.Course.CourseFeePerTermDueWeeks ?? 0;
                dateDue = DateOnly.FromDateTime(t.StartDate.AddDays(dueDateAdjustment * 7));
                if (dateDue < dateEnrolled) dateDue = dateEnrolled;
                if (e.Course.HasTermFees && dateDue <= today)
                {
                    bool isTermFeeDue = false;
                    if (e.Class != null)
                    {
                        isTermFeeDue = isClassHeldThisTerm(e.Class, t);
                    }
                    else
                    {
                        foreach (var c in e.Course.Classes)
                        {
                            isTermFeeDue = isClassHeldThisTerm(c, t);
                            if (isTermFeeDue) { break; }
                        }
                    }
                    if (isTermFeeDue)
                    {
                        var description = $"{e.Course.Name} fee";
                        var amount = 0M;
                        MemberFeeSortOrder sortOrder = default;
                        switch (t.TermNumber)
                        {
                            case 1:
                                amount = e.Course.CourseFeeTerm1;
                                sortOrder = MemberFeeSortOrder.Term1Fee;
                                break;
                            case 2:
                                amount = e.Course.CourseFeeTerm2;
                                sortOrder = MemberFeeSortOrder.Term2Fee;
                                break;
                            case 3:
                                amount = e.Course.CourseFeeTerm3;
                                sortOrder = MemberFeeSortOrder.Term3Fee;
                                break;
                            case 4:
                                amount = e.Course.CourseFeeTerm4;
                                sortOrder = MemberFeeSortOrder.Term4Fee;
                                break;
                            default:
                                break;
                        }
                        var includeInComplimentary = Settings.IncludeCourseFeePerTermInComplimentary;
                        if (e.Course.OverrideComplimentaryPerTermFee) includeInComplimentary = false;
                        if (IsComplimentary && includeInComplimentary)
                        {
                            amount = 0;
                        }
                        if (!string.IsNullOrWhiteSpace(e.Course.CourseFeePerTermDescription))
                        {
                            description += $": {e.Course.CourseFeePerTermDescription}";
                        }
                        AddFee(person, sortOrder, ConvertDateOnlyToDateTime(dateDue),
                            $"{t.Name}: {description}", amount, e.Course.Name);
                        Log.Information($"Student: {person.FullName} {e.Course.Name} {t.TermNumber} {dateDue}");
                        if (PersonWithFinancialStatus != null)
                            PersonWithFinancialStatus.CourseFeesPerTerm += amount;
                    }
                }
            }
        }
    }

    private async Task AddLeadersFeesAsync(Person person)
    {
        DateOnly today = DateOnly.FromDateTime(localTime);
        var courseFeeAdded = new List<Guid>();
        List<Class> classesLead;
        classesLead = Classes.Where(x =>
                                        x.Course.Year == BillingYear && (x.Course.CourseFeePerTerm > 0 && x.Course.LeadersPayTermFee &&
                                        (x.LeaderID == person.ID ||
                                        x.Leader2ID == person.ID ||
                                        x.Leader3ID == person.ID))).ToList();
        DateOnly dueDate;
        foreach (var c in classesLead)
        {
            //Fees per year
            dueDate = c.Course.CourseFeePerYearDueDate ?? new DateOnly(BillingYear, 1, 1);
            if (dueDate <= today)
            {
                if (c.Course.CourseFeePerYear != 0 && c.Course.LeadersPayYearFee && !courseFeeAdded.Contains(c.CourseID))
                {
                    var description = $"{c.Course.Name} course fee";
                    var amount = c.Course.CourseFeePerYear;
                    if (!string.IsNullOrWhiteSpace(c.Course.CourseFeePerYearDescription))
                    {
                        description += $": {c.Course.CourseFeePerYearDescription}";
                    }
                    AddFee(person, MemberFeeSortOrder.CourseFee, ConvertDateOnlyToDateTime(dueDate),
                                description, amount, c.Course.Name);
                    if (PersonWithFinancialStatus != null)
                        PersonWithFinancialStatus.CourseFeesPerYear += amount;
                    courseFeeAdded.Add(c.CourseID);
                }
            }
        }
        //Fees per term
        List<(Class Class, Term Term)> classTerms = new();
        foreach (var t in Terms)
        {
            foreach (var c in classesLead)
            {
                if (classTerms.Contains((c, t))) continue;
                var dueDateAdjustment = c.Course.CourseFeePerTermDueWeeks ?? 0;
                dueDate = DateOnly.FromDateTime(t.StartDate.AddDays(dueDateAdjustment * 7));
                Log.Information($"Leader: {person.FullName} {c.Course.Name} {t.TermNumber} {dueDate}");
                if (c.Course.CourseFeePerTerm != 0
                        && c.Course.LeadersPayTermFee && dueDate <= today)
                {
                    if (isClassHeldThisTerm(c, t))
                    {
                        var description = $"{c.Course.Name} fee";
                        var amount = c.Course.CourseFeePerTerm;
                        if (!string.IsNullOrWhiteSpace(c.Course.CourseFeePerTermDescription))
                        {
                            description += $": {c.Course.CourseFeePerTermDescription}";
                        }
                        MemberFeeSortOrder sortOrder = default;
                        switch (t.TermNumber)
                        {
                            case 1:
                                sortOrder = MemberFeeSortOrder.Term1Fee;
                                break;
                            case 2:
                                sortOrder = MemberFeeSortOrder.Term2Fee;
                                break;
                            case 3:
                                sortOrder = MemberFeeSortOrder.Term3Fee;
                                break;
                            case 4:
                                sortOrder = MemberFeeSortOrder.Term4Fee;
                                break;
                            default:
                                break;
                        }
                        AddFee(person, sortOrder, ConvertDateOnlyToDateTime(dueDate),
                            $"{t.Name}: {description}", amount, c.Course.Name);
                        Log.Information($"Leader: {person.FullName} {c.Course.Name} {t.TermNumber} {dueDate}");
                        if (PersonWithFinancialStatus != null)
                            PersonWithFinancialStatus.CourseFeesPerTerm += amount;
                        classTerms.Add((c, t));
                    }
                }
            }
        }
    }

    private bool isClassHeldThisTerm(Class c, Term term)
    {
        bool result = false;
        if (term.TermNumber == 1 && c.OfferedTerm1) result = true;
        if (term.TermNumber == 2 && c.OfferedTerm2) result = true;
        if (term.TermNumber == 3 && c.OfferedTerm3) result = true;
        if (term.TermNumber == 4 && c.OfferedTerm4) result = true;
        return result;
    }

    private void AddFee(Person person,
        MemberFeeSortOrder sortOrder,
        DateTime? date,
        string description,
        decimal amount,
        string Course = "")
    {
        var value = decimal.Round(amount, 2);
        MemberFees.Add(new MemberFee
        {
            PersonID = person.ID,
            Person = person,
            SortOrder = sortOrder,
            Date = date,
            Description = description,
            Amount = value,
            Course = Course,
        });
    }

    public DateTime ConvertDateOnlyToDateTime(DateOnly date)
    {
        return new DateTime(date.Year, date.Month, date.Day);
    }
    private async Task<int> ActiveCourseCountAsync(Person person)
    {
        return Enrolments.Where(x => x.PersonID == person.ID &&
                                    x.TermID == BillingTerm.ID &&
                                    !x.Course.ExcludeFromLeaderComplimentaryCount &&
                                    !x.IsWaitlisted)
                        .DistinctBy(x => x.CourseID).Count();
    }
    private async Task<int> WaitlistedCourseCountAsync(Person person)
    {
        return Enrolments.Where(x => x.PersonID == person.ID &&
                                    x.TermID == BillingTerm.ID &&
                                    !x.Course.ExcludeFromLeaderComplimentaryCount &&
                                    x.IsWaitlisted)
                        .DistinctBy(x => x.CourseID).Count();
    }

    public List<MemberFee> AllocateMemberPayments(IEnumerable<MemberFee> ItemsToAllocate, Person person,
                            bool ShowFullAllocation, bool AddUnallocatedCredit = false)
    {
        List<MemberFee> result = new List<MemberFee>();
        List<MemberFee> allocatedItems = new List<MemberFee>();
        List<List<MemberFee>> combinations = new List<List<MemberFee>>();
        var fees = ItemsToAllocate.Where(x => x.PersonID == person.ID)
            .OrderBy(x => x.Date).ThenBy(x => x.SortOrder)
            .Where(x => x.Amount > 0).ToArray();
        var receipts = ItemsToAllocate.Where(x => x.PersonID == person.ID)
            .OrderBy(x => x.Date).ThenBy(x => x.SortOrder)
            .Where(x => x.Amount < 0).ToArray();

        int n = fees.Count();
        decimal totalDue = ItemsToAllocate.Sum(x => x.Amount);

        // Generate all subsets using bitwise operations
        for (int i = 1; i < (1 << n); i++)
        {
            List<MemberFee> subset = new List<MemberFee>();
            for (int j = 0; j < n; j++)
            {
                if ((i & (1 << j)) != 0)
                {
                    subset.Add(fees[j]);
                }
            }
            combinations.Add(subset);
        }

        // Allocate receipts to fees
        foreach (var receipt in receipts)
        {
            decimal receiptAmount = -receipt.Amount;
            List<MemberFee> allocatedSubset = new List<MemberFee>();
            // Sort combinations by total amount in descending order
            var sortedCombinations = combinations.OrderByDescending(x => x.Sum(y => y.Amount)).ToList();
            foreach (var subset in sortedCombinations)
            {
                decimal subsetTotal = subset.Sum(x => x.Amount);
                if (subsetTotal == receiptAmount && !allocatedItems.Any(x => subset.Contains(x)))
                {
                    allocatedSubset.AddRange(subset);
                    receiptAmount -= subsetTotal;
                    allocatedItems.AddRange(subset);
                }
                if (receiptAmount <= 0)
                {
                    allocatedItems.Add(receipt);
                    break;
                }
            }
            if (receiptAmount > 0) { receipt.Amount = -receiptAmount; }
        }

        var keys = allocatedItems.Select(x => x.ID);
        if (ShowFullAllocation)
        {
            result = allocatedItems.ToList();
            foreach (var item in ItemsToAllocate.Where(x => !keys.Contains(x.ID)))
            {
                if (totalDue != 0) { item.IsNotAllocated = true; } // Mark as not allocated
                result.Add(item);
            }
        }
        else foreach (var item in ItemsToAllocate.Where(x => !keys.Contains(x.ID)))
            {
                result.Add(item);
            }

        foreach (var item in result)
        {
            if (!item.IsNotAllocated && item.Amount > 0)
            {
                item.Allocated = item.Amount;
            }
        }

        var unallocatedFees = result.Where(x => x.IsNotAllocated && x.Amount > 0);
        var remainingCredits = result.Where(x => x.IsNotAllocated && x.Amount < 0).Sum(x => x.Amount);
        // Allocate remaining credits to unallocated fees
        if (remainingCredits < 0)
        {
            foreach (var fee in unallocatedFees.OrderBy(x => x.Date).ThenBy(x => x.SortOrder))
            {
                if (remainingCredits >= 0) break;
                var allocation = Math.Min(-remainingCredits, fee.Amount);
                fee.Allocated = allocation;
                remainingCredits += allocation;
            }
        }
        CalculateBalance(result, AddUnallocatedCredit);
        if (AddUnallocatedCredit)
        {
            result = result.Where(x => x.Amount > 0).ToList();
            if (totalDue < 0)
            {
                result.Add(new MemberFee()
                {
                    PersonID = person.ID,
                    Person = person,
                    SortOrder = MemberFeeSortOrder.UnallocatedCredit,
                    Date = DateTime.Now.Date,
                    Description = "Unallocated Credit",
                    Amount = totalDue,
                    Allocated = 0m,
                    Balance = totalDue,
                });
            }
        }
        return result;
    }

    void CalculateBalance(IEnumerable<MemberFee> memberFees, bool AddUnallocatedCredit)
    {
        var result = decimal.Zero;
        var itemCount = memberFees.Count();
        int i = 0;
        foreach (var item in memberFees)
        {
            i += 1;
            result += item.Amount;
            if (AddUnallocatedCredit)
            {
                item.Balance = item.Amount - item.Allocated.GetValueOrDefault();
            }
            else
            {
                item.Balance = ((item.Amount < 0 && result == 0) || i == itemCount) ? result : null;
            }
        }
    }

}
