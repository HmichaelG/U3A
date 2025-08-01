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
    public MemberFeeCalculationService()
    {
        MemberFees = new ConcurrentBag<MemberFee>();
    }

    public static async Task<MemberFeeCalculationService> CreateAsync(U3ADbContext dbc, Term Term)
    {
        var instance = new MemberFeeCalculationService();
        await instance.InitializeAsync(dbc,Term);
        return instance;
    }

    private async Task InitializeAsync(U3ADbContext dbc, Term Term)
    {
        BillingTerm = Term;
        BillingYear = Term.Year;
        Settings = await dbc.SystemSettings.FirstOrDefaultAsync();
        People = await dbc.Person.AsNoTracking().ToListAsync();
        Terms = await dbc.Term.Where(x => x.Year == BillingYear).OrderBy(x => x.TermNumber).ToArrayAsync();
        Fees = await dbc.Fee.AsNoTracking().IgnoreQueryFilters()
                            .Where(x => !x.IsDeleted && x.ProcessingYear >= BillingYear)
                            .ToListAsync();
        Receipts = await dbc.Receipt.AsNoTracking().IgnoreQueryFilters()
                            .Where(x => !x.IsDeleted && x.ProcessingYear >= BillingYear)
                            .ToListAsync();
        Enrolments = await dbc.Enrolment.AsNoTracking().IgnoreQueryFilters()
                            .Include(x => x.Term)
                            .Include(x => x.Course).ThenInclude(x => x.Classes)
                            .Include(x => x.Class)
                            .Where(x => !x.IsDeleted && x.Term.Year == BillingYear).ToListAsync();
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

    public async Task<List<MemberPaymentAvailable>> GetAvailableMemberPaymentsAsync(U3ADbContext dbc, Person person)
    {
        var result = new List<MemberPaymentAvailable>();
        var settings = Settings ?? await dbc.SystemSettings.FirstOrDefaultAsync();
        if (settings.AllowedMemberFeePaymentTypes == MemberFeePaymentType.PerYearAndPerSemester)
        {
            var term = BillingTerm ?? await GetBillingTermAsync(dbc);
            if (await IsComplimentaryMembership(dbc, person, term.Year)) { return result; }
            decimal yearlyFee = GetTermFee(settings, term.TermNumber)
                                    + await GetTotalOtherMembershipFees(dbc, person, term);
            decimal semesterFee = decimal.Round(yearlyFee / 2, 2);
            var totalFee = await CalculateFeeAsync(dbc, person, term);
            if (totalFee < yearlyFee) { return result; }
            if (term.TermNumber <= 2)
            {
                result.Add(new MemberPaymentAvailable()
                {
                    Amount = totalFee,
                    Description = $"{totalFee.ToString("c2")} {term.Year} Full Year Fee",
                    TermsPaid = null
                });
                result.Add(new MemberPaymentAvailable()
                {
                    Amount = totalFee - semesterFee,
                    Description = $"{(totalFee - semesterFee).ToString("c2")} {term.Year} Semester (Term 1 & 2) Fee",
                    TermsPaid = 2
                }); ;
            }
        }
        return result;
    }
    async Task<decimal> GetTotalOtherMembershipFees(U3ADbContext dbc, Person person, Term term)
    {
        var result = 0m;
        if (Fees == null)
        {
            result = await dbc.Fee.AsNoTracking().IgnoreQueryFilters()
                                .Where(x => !x.Person.IsDeleted
                                        && x.PersonID == person.ID
                                        && x.IsMembershipFee
                                        && x.ProcessingYear == term.Year)
                                .Select(x => x.Amount).SumAsync();
        }
        else
        {
            result = Fees
                        .Where(x => !x.Person.IsDeleted
                                    && x.PersonID == person.ID
                                    && x.IsMembershipFee)
                        .Sum(x => x.Amount);
        }
        return result;
    }

    /// <summary>
    /// Calculate fees for an individual. Used for Member Portal calculations
    /// </summary>
    /// <param name="U3Adbfactory"></param>
    /// <param name="person"></param>
    /// <returns></returns>
    public async Task<decimal> CalculateFeeAsync(IDbContextFactory<U3ADbContext> U3Adbfactory,
                                    Person person, int? CalclateForTerm = null)
    {
        var result = decimal.Zero;
        using (var dbc = await U3Adbfactory.CreateDbContextAsync())
        {
            var term = await GetBillingTermAsync(dbc);
            if (term != null) { result = await CalculateFeeAsync(dbc, person, term, CalclateForTerm); }
        }
        return result;
    }
    public async Task<decimal> CalculateFeeAsync(IDbContextFactory<U3ADbContext> U3Adbfactory,
                                    Person person, Term term, int? CalculateForTerm = null)
    {
        var result = decimal.Zero;
        using (var dbc = await U3Adbfactory.CreateDbContextAsync())
        {
            if (term != null) { result = await CalculateFeeAsync(dbc, person, term, CalculateForTerm); }
        }
        return result;
    }

    public async Task<decimal> CalculateFeeAsync(U3ADbContext dbc,
                                    Person person, Term term, int? CalculateForTerm = null)
    {
        var result = decimal.Zero;
        BillingTerm = term;
        BillingYear = term.Year;
        var defaultDate = new DateTime(term.Year, 1, 1);
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
            Enrolments = await ActiveCourseCountAsync(dbc, person, term),
            Waitlisted = await WaitlistedCourseCountAsync(dbc, person, term)
        };
        if (term != null)
        {
            var settings = Settings ?? await dbc.SystemSettings.FirstOrDefaultAsync();
            if (settings != null)
            {
                var isComplimentary = await IsComplimentaryMembership(dbc, person, term.Year);
                // membership fees
                if (isComplimentary && settings.IncludeMembershipFeeInComplimentary)
                {
                    var complimentaryCalcDate = await GetComplimentaryCalculationDate(dbc, person, term.Year);
                    if (complimentaryCalcDate != null)
                    {
                        AddFee(person,
                            MemberFeeSortOrder.Complimentary, defaultDate, $"{complimentaryCalcDate?.ToString("dd-MMM-yyyy")} {term.Year} complimentary membership", 0.00M);
                    }
                    else
                    {
                        AddFee(person,
                            MemberFeeSortOrder.Complimentary, defaultDate, $"{term.Year} complimentary membership", 0.00M);
                    }
                }
                else
                {
                    PersonWithFinancialStatus.MembershipFees = await CalculateMembershipFeeAsync(dbc, term, person);
                    var fee = PersonWithFinancialStatus.MembershipFees;
                    if (fee != 0)
                    {
                        if (CalculateForTerm.HasValue) { fee = decimal.Round(fee / 4m * (decimal)CalculateForTerm, 2); }
                        AddFee(person,
                            MemberFeeSortOrder.MemberFee, defaultDate, $"{term.Year} membership fee", fee);
                    }
                }
                if (person.Communication != "Email")
                {
                    if (isComplimentary && settings.IncludeMailSurchargeInComplimentary)
                    {
                        AddFee(person,
                            MemberFeeSortOrder.MailSurcharge, defaultDate, $"{term.Year} complimentary mail surcharge", 0.00M);
                    }
                    else
                    {
                        PersonWithFinancialStatus.MailSurcharge = settings.MailSurcharge;
                        AddFee(person,
                            MemberFeeSortOrder.MailSurcharge, defaultDate, $"{term.Year} mail surcharge", settings.MailSurcharge);
                    }
                }
                // course fees
                await AddCourseFeesAsync(dbc, person, term, settings, isComplimentary);
                await AddLeadersFeesAsync(dbc, person, term, settings);
                // add fee adjustments
                await AddFeesAsync(dbc, person, term);
                // less payments
                await SubtractReceiptsAsync(dbc, person, term);
                // total due
                result = MemberFees
                            .Where(x => x.PersonID == person.ID)
                            .Sum(x => x.Amount);
            }
        }
        return result;
    }

    private async Task<bool> IsComplimentaryMembership(U3ADbContext dbc, Person person, int Yaer)
    {
        bool result = false;
        if (person.IsLifeMember) { result = true; }
        else
        {
            // A zero-valued receipt is created when a person is given complimentary membership
            if (Receipts == null)
            {
                result = await dbc.Receipt.AnyAsync(x => x.PersonID == person.ID
                                            && x.FinancialTo >= BillingYear
                                            && x.Amount == 0);
            }
            else
            {
                result = Receipts.Any(x => x.PersonID == person.ID
                                        && x.FinancialTo >= BillingYear
                                        && x.Amount == 0);
            }
        }
        if (PersonWithFinancialStatus != null)
            PersonWithFinancialStatus.IsComplimentary = result;
        return result;
    }
    private async Task<DateTime?> GetComplimentaryCalculationDate(U3ADbContext dbc, Person person, int Yaer)
    {
        DateTime? result = null;
        Receipt? receipt;
        if (Receipts == null)
        {
            receipt = await dbc.Receipt.FirstOrDefaultAsync(x => x.PersonID == person.ID
                                            && x.FinancialTo >= BillingYear
                                            && x.Amount == 0);
        }
        else
        {
            receipt = Receipts.FirstOrDefault(x => x.PersonID == person.ID
                                            && x.Amount == 0);
        }
        if (receipt != null) { result = receipt.Date; }
        return result;
    }

    private async Task<Term> GetBillingTermAsync(U3ADbContext dbc)
    {
        Term result = null;
        result = await BusinessRule.CurrentEnrolmentTermAsync(dbc);
        if (result == null)
        {
            result = await BusinessRule.CurrentTermAsync(dbc);
            if (result.TermNumber == 4 && dbc.GetLocalTime(DateTime.UtcNow) > result.EndDate)
            {
                result = null; // End of year, no enrolment period - no man's land.
            }
        }
        if (result != null) { BillingTerm = result; BillingYear = result.Year; }
        return result;
    }

    public async Task<decimal> CalculateMinimumFeePayableAsync(IDbContextFactory<U3ADbContext> U3Adbfactory,
                                    Person person, int? CalclateForTerm = null)
    {
        var result = decimal.Zero;
        using (var dbc = await U3Adbfactory.CreateDbContextAsync())
        {
            result = await CalculateMinimumFeePayableAsync(dbc, person, CalclateForTerm);
        }
        return result;
    }
    public async Task<decimal> CalculateMinimumFeePayableAsync(U3ADbContext dbc,
                                    Person person, int? CalculateForTerm = null)
    {
        var result = decimal.Zero;
        var term = BillingTerm ?? await GetBillingTermAsync(dbc);
        if (term != null && !await IsComplimentaryMembership(dbc, person, term.Year))
        {
            result = await CalculateMembershipFeeAsync(dbc, term, person);
            if (Fees == null)
            {
                result += dbc.Fee.IgnoreQueryFilters()
                                            .Where(x => !x.Person.IsDeleted
                                             && x.PersonID == person.ID
                                             && x.IsMembershipFee
                                             && x.ProcessingYear == term.Year).Select(x => x.Amount).Sum();
            }
            else
            {
                result += Fees
                            .Where(x => !x.Person.IsDeleted
                                        && x.PersonID == person.ID
                                        && x.IsMembershipFee)
                            .Sum(x => x.Amount);
            }
            if (CalculateForTerm.HasValue) { result = decimal.Round(result / 4m * (decimal)CalculateForTerm, 2); }
        }
        return result;
    }

    private async Task<decimal> CalculateMembershipFeeAsync(U3ADbContext dbc, Term term, Person person)
    {
        if (person is Contact) { return 0; }
        decimal result = 0;
        bool foundTerm = false;
        var settings = Settings ?? await dbc.SystemSettings.FirstOrDefaultAsync();
        if (settings != null)
        {
            result = settings.MembershipFee; // set the default
            DateTime? feeDueDate = null;
            if (person.DateJoined?.Year == term.Year) feeDueDate = person.DateJoined; // New member; use join date
            if (feeDueDate == null) // Renewing member
            {
                var firstReceiptDate = await GetFirstReceiptDateAsync(dbc, term, person);
                feeDueDate = firstReceiptDate ?? dbc.GetLocalDate();
            }
            var terms = Terms ?? await dbc.Term.Where(x => x.Year == term.Year).OrderBy(x => x.TermNumber).ToArrayAsync();
            if (terms.Length > 1)
            {
                // Ues the term fee if fee due date is within term enrollment period
                for (int i = 1; i < terms.Length; i++)
                { // for terms 2 thru 4
                    var lastTerm = terms[i - 1];
                    var thisTerm = terms[i];
                    if (feeDueDate > lastTerm.EnrolmentEndDate && feeDueDate <= thisTerm.EnrolmentEndDate)
                    {
                        result = GetTermFee(settings, thisTerm.TermNumber);
                        foundTerm = true;
                        break; // exit for
                    }
                }
                // otherwise, use the term fee if date is within the current term.
                if (!foundTerm)
                {
                    for (int i = 1; i < terms.Length; i++)
                    { // for terms 2 thru 4
                        var lastTerm = terms[i - 1];
                        var thisTerm = terms[i];
                        if (feeDueDate > lastTerm.EndDate && feeDueDate <= thisTerm.EndDate)
                        {
                            result = GetTermFee(settings, thisTerm.TermNumber);
                            break; // exit for
                        }
                    }
                }
            }
        }
        return result;
    }

    private async Task<DateTime?> GetFirstReceiptDateAsync(U3ADbContext dbc, Term term, Person person)
    {
        DateTime? result = null;
        Receipt? receipt = null;
        if (Receipts == null)
        {
            receipt = await dbc.Receipt.AsNoTracking().IgnoreQueryFilters()
                                        .OrderBy(x => x.Date)
                                        .Where(x => !x.IsDeleted
                                            && x.PersonID == person.ID
                                            && x.Amount != 0
                                            && x.ProcessingYear == term.Year)
                                        .FirstOrDefaultAsync();
        }
        else
        {
            receipt = Receipts.OrderBy(x => x.Date).FirstOrDefault(x => !x.IsDeleted
                                            && x.PersonID == person.ID
                                            && x.Amount != 0
                                            && x.ProcessingYear == term.Year);
        }
        if (receipt != null) { result = receipt.Date; }
        return result;
    }

    private decimal GetTermFee(SystemSettings settings, int TermNumber)
    {
        var result = settings.MembershipFee;
        switch (TermNumber)
        {
            case 2:
                result = settings.MembershipFeeTerm2;
                break;
            case 3:
                result = settings.MembershipFeeTerm3;
                break;
            case 4:
                result = settings.MembershipFeeTerm4;
                break;
            default:
                break;
        }
        return result;
    }

    private async Task AddFeesAsync(U3ADbContext dbc, Person person, Term term)
    {
        List<Fee> fees;
        if (Fees == null)
        {
            fees = await dbc.Fee.IgnoreQueryFilters().AsNoTracking()
                                        .Where(x => !x.Person.IsDeleted
                                                && x.PersonID == person.ID
                                                && x.Amount != 0
                                                && x.ProcessingYear == term.Year).ToListAsync();
        }
        else
        {
            fees = Fees.Where(x => x.PersonID == person.ID
                                    && x.Amount != 0
                                    && x.ProcessingYear == term.Year).ToList();
        }
        foreach (var f in fees)
        {
            AddFee(person,
                    MemberFeeSortOrder.AdditionalFee, f.Date, f.Description, f.Amount);
            PersonWithFinancialStatus.OtherFees += f.Amount;
        }
    }
    private async Task SubtractReceiptsAsync(U3ADbContext dbc, Person person, Term term)
    {
        List<Receipt> receipts;
        if (Receipts == null)
        {
            receipts = await dbc.Receipt.AsNoTracking().IgnoreQueryFilters()
                                        .OrderBy(x => x.Date)
                                        .Where(x => !x.IsDeleted
                                            && x.PersonID == person.ID
                                            && x.Amount != 0
                                            && x.ProcessingYear == term.Year).ToListAsync();
        }
        else
        {
            receipts = Receipts.Where(x => !x.IsDeleted
                                            && x.PersonID == person.ID
                                            && x.Amount != 0
                                            && x.ProcessingYear == term.Year)
                                .OrderBy(x => x.Date).ToList();
        }
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
    private async Task AddCourseFeesAsync(U3ADbContext dbc,
                            Person person,
                            Term term,
                            SystemSettings settings,
                            bool IsComplimentary)
    {
        DateOnly today = DateOnly.FromDateTime(dbc.GetLocalDate());
        var terms = Terms ?? await dbc.Term.AsNoTracking()
                            .Where(x => x.Year == term.Year)
                            .OrderBy(x => x.TermNumber)
                            .ToArrayAsync();
        var courseFeeAdded = new List<Guid>();
        List<Enrolment> enrolments;
        if (Enrolments == null)
        {
            enrolments = await dbc.Enrolment.AsNoTracking().IgnoreQueryFilters()
                                .Include(x => x.Course).ThenInclude(x => x.Classes)
                                .Include(x => x.Class)
                                .Where(x => !x.IsDeleted
                                            && x.PersonID == person.ID
                                            && x.Term.Year == term.Year
                                            && !x.IsWaitlisted).ToListAsync();
        }
        else
        {
            enrolments = Enrolments.Where(x => !x.IsDeleted
                                            && x.PersonID == person.ID
                                            && x.Term.Year == BillingYear
                                            && !x.IsWaitlisted).ToList();
        }
        foreach (var t in terms)
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
                        var includeInComplimentary = settings.IncludeCourseFeePerYearInComplimentary;
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
                        var includeInComplimentary = settings.IncludeCourseFeePerTermInComplimentary;
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

    private async Task AddLeadersFeesAsync(U3ADbContext dbc,
                            Person person,
                            Term term,
                            SystemSettings settings)
    {
        DateOnly today = DateOnly.FromDateTime(dbc.GetLocalDate());
        var terms = Terms ?? await dbc.Term.AsNoTracking()
                            .Where(x => x.Year == term.Year)
                            .OrderBy(x => x.TermNumber)
                            .ToArrayAsync();
        var courseFeeAdded = new List<Guid>();
        List<Class> classesLead;
        if (Classes == null)
        {
            classesLead = await dbc.Class
                                    .Include(x => x.Course)
                                    .Where(x =>
                                            x.Course.Year == term.Year && (x.Course.CourseFeePerTerm > 0 && x.Course.LeadersPayTermFee &&
                                            (x.LeaderID == person.ID ||
                                            x.Leader2ID == person.ID ||
                                            x.Leader3ID == person.ID))).ToListAsync();
        }
        else
        {
            classesLead = Classes.Where(x =>
                                            x.Course.Year == term.Year && (x.Course.CourseFeePerTerm > 0 && x.Course.LeadersPayTermFee &&
                                            (x.LeaderID == person.ID ||
                                            x.Leader2ID == person.ID ||
                                            x.Leader3ID == person.ID))).ToList();
        }
        DateOnly dueDate;
        foreach (var c in classesLead)
        {
            //Fees per year
            dueDate = c.Course.CourseFeePerYearDueDate ?? new DateOnly(term.Year, 1, 1);
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
        foreach (var t in terms)
        {
            foreach (var c in classesLead)
            {
                if (classTerms.Contains((c,t))) continue;
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
    private async Task<int> ActiveCourseCountAsync(U3ADbContext dbc, Person person, Term SelectedTerm)
    {
        if (Enrolments == null)
        {
            return (await dbc.Enrolment.Include(x => x.Course)
                            .Where(x => x.PersonID == person.ID &&
                                x.TermID == SelectedTerm.ID &&
                                !x.Course.ExcludeFromLeaderComplimentaryCount &&
                                !x.IsWaitlisted).ToListAsync()).DistinctBy(x => x.CourseID).Count();
        }
        else
        {
            return Enrolments.Where(x => x.PersonID == person.ID &&
                                        x.TermID == SelectedTerm.ID &&
                                        !x.Course.ExcludeFromLeaderComplimentaryCount &&
                                        !x.IsWaitlisted)
                            .DistinctBy(x => x.CourseID).Count();
        }
    }
    private async Task<int> WaitlistedCourseCountAsync(U3ADbContext dbc, Person person, Term SelectedTerm)
    {
        if (Enrolments == null)
        {
            return (await dbc.Enrolment.Include(x => x.Course)
                            .Where(x => x.PersonID == person.ID &&
                                x.TermID == SelectedTerm.ID &&
                                !x.Course.ExcludeFromLeaderComplimentaryCount &&
                                x.IsWaitlisted).ToListAsync()).DistinctBy(x => x.CourseID).Count();
        }
        else
        {
            return Enrolments.Where(x => x.PersonID == person.ID &&
                                        x.TermID == SelectedTerm.ID &&
                                        !x.Course.ExcludeFromLeaderComplimentaryCount &&
                                        x.IsWaitlisted)
                            .DistinctBy(x => x.CourseID).Count();
        }
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
