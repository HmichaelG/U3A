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
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Twilio.Rest.Api.V2010.Account.Usage.Record;
using U3A.BusinessRules;
using U3A.Database;
using U3A.Model;
using U3A.Model;

namespace U3A.Services;

public class MemberFeeCalculationService
{
    ConcurrentDictionary<Guid, ConcurrentBag<MemberFee>> MemberFees { get; set; }
    public int BillingYear { get; set; }
    public Term BillingTerm { get; set; }

    List<Person> People { get; set; } = null;
    SystemSettings Settings { get; set; } = null;
    List<Fee> Fees { get; set; } = null;
    Dictionary<Guid, List<Receipt>> Receipts { get; set; } = null;
    Term[] Terms { get; set; } = null;
    Dictionary<Guid, List<Enrolment>> Enrolments { get; set; } = null;
    Dictionary<Guid, (int Enrolled, int Waitlisted)> enrolmentCount;
    List<Class> Classes { get; set; } = null;
    ConcurrentBag<(Guid MemberID, Guid CourseID)> CourseFeeAdded;
    ConcurrentBag<(Guid MemberID, Guid CourseID, Guid TermID)> TermFeeAdded;

    DateTime localTime;
    TimeSpan UtcOffset;
    private MemberFeeCalculationService()
    {
        MemberFees = new();
        CourseFeeAdded = new();
        TermFeeAdded = new();
        People = null;
    }

    public static async Task<MemberFeeCalculationService> CreateAsync(U3ADbContext dbc, Term Term, IEnumerable<Person> PeopleToCalculate)
    {
        var instance = new MemberFeeCalculationService();
        instance.People = new();
        instance.People.AddRange(PeopleToCalculate);
        await instance.InitializeAsync(dbc, Term, null);
        return instance;
    }

    public static async Task<MemberFeeCalculationService> CreateAsync(U3ADbContext dbc, IEnumerable<Person> PeopleToCalculate)
    {
        var instance = new MemberFeeCalculationService();
        instance.People = new();
        instance.People.AddRange(PeopleToCalculate);
        await instance.InitializeAsync(dbc, null, null);
        return instance;
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
        await instance.InitializeAsync(dbc, null, Person);
        return instance;
    }

    private async Task InitializeAsync(U3ADbContext dbc, Term? Term, Person? person)
    {
        UtcOffset = dbc.UtcOffset;
        localTime = dbc.GetLocalTime(DateTime.UtcNow);
        var s = new Stopwatch();
        s.Start();
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
        else
        {
            Terms = await dbc.Term
                        .Where(x => x.Year == localTime.Year)
                        .OrderBy(x => x.TermNumber)
                        .ToArrayAsync();
            GetBillingTerm();
        }
        if (People == null)
        {
            People = await dbc.Person.AsNoTracking()
                            .Where(x => person == null || x.ID == person.ID)
                            .ToListAsync();
        }
        Fees = await dbc.Fee.AsNoTracking().IgnoreQueryFilters()
                            .Where(x => (person == null || x.PersonID == person.ID)
                                        && !x.IsDeleted && x.ProcessingYear >= BillingYear)
                            .ToListAsync();
        Receipts = await dbc.Receipt.AsNoTracking().IgnoreQueryFilters()
                            .Where(x => (person == null || x.PersonID == person.ID)
                                        && !x.IsDeleted && x.ProcessingYear >= BillingYear)
                            .GroupBy(x => x.PersonID)
                            .ToDictionaryAsync(g => g.Key, g => g.ToList());
        Enrolments = await dbc.Enrolment.AsNoTracking()
                            .AsSplitQuery()
                            .IgnoreQueryFilters()
                            .Where(x => (person == null || x.PersonID == person.ID)
                                && !x.IsDeleted && x.Term.Year == BillingYear
                                && (x.Course.CourseFeePerYear != 0
                                    || x.Course.CourseFeeTerm1 != 0
                                    || x.Course.CourseFeeTerm2 != 0
                                    || x.Course.CourseFeeTerm3 != 0
                                    || x.Course.CourseFeeTerm4 != 0
                                    ))
                            .Include(x => x.Term)
                            .Include(x => x.Course).ThenInclude(x => x.Classes)
                            .Include(x => x.Class)
                            .GroupBy(x => x.PersonID)
                            .ToDictionaryAsync(g => g.Key, g => g.ToList());
        enrolmentCount = await dbc.Enrolment.AsNoTracking()
                                .IgnoreQueryFilters()
                                .Where(x => (person == null || x.PersonID == person.ID)
                                    && !x.IsDeleted && x.Term.ID == BillingTerm.ID)
                                .GroupBy(x => x.PersonID)
                                .Select(g => new
                                {
                                    MemberID = g.Key,
                                    Enrolled = g.Count(x => !x.IsWaitlisted && !x.Course.ExcludeFromLeaderComplimentaryCount),
                                    Waitlisted = g.Count(x => x.IsWaitlisted && !x.Course.ExcludeFromLeaderComplimentaryCount)
                                })
                                .ToDictionaryAsync(
                                    k => k.MemberID,
                                    v => (v.Enrolled, v.Waitlisted));
        Classes = await dbc.Class
                    .Where(x => x.Course.Year == BillingYear &&
                                    (x.Course.CourseFeePerYear != 0
                                    || x.Course.CourseFeeTerm1 != 0
                                    || x.Course.CourseFeeTerm2 != 0
                                    || x.Course.CourseFeeTerm3 != 0
                                    || x.Course.CourseFeeTerm4 != 0)
                                    )
                    .Include(x => x.Course)
                    .ToListAsync();
        Log.Information("MemberFeeCalculationService: data loaded in {ElapsedMilliseconds} ms", s.ElapsedMilliseconds);
        s.Stop();
    }



    public ConcurrentBag<PersonFinancialStatus> PeopleWithFinancialStatus { get; set; } = new();

    public List<MemberFee> GetMemberFees(Guid PersonID)
    {
        var fees = MemberFees.TryGetValue(PersonID, out var list) ? list : new ConcurrentBag<MemberFee>();
        return fees
                .Where(x => x.PersonID == PersonID)
                .OrderBy(x => x.Date)
                .ThenBy(x => x.SortOrder)
                .ToList();
    }
    public List<MemberFee> GetAllocatedMemberFees(Person person)
    {
        var fees = MemberFees.TryGetValue(person.ID, out var list) ? list : new ConcurrentBag<MemberFee>();
        return AllocateMemberPayments(fees, person,
                ShowFullAllocation: true, AddUnallocatedCredit: true)
                .ToList();
    }
    public List<MemberFee> GetTransactionDetail(Person person)
    {
        var fees = MemberFees.TryGetValue(person.ID, out var list) ? list : new ConcurrentBag<MemberFee>();
        return AllocateMemberPayments(fees, person,
                ShowFullAllocation: true, AddUnallocatedCredit: false)
                .ToList();
    }

    public List<MemberPaymentAvailable> GetAvailableMemberPayments(Person person = null)
    {
        var result = new List<MemberPaymentAvailable>();
        if (person == null) { person = People.FirstOrDefault(); }
        if (Settings.AllowedMemberFeePaymentTypes == MemberFeePaymentType.PerYearAndPerSemester)
        {
            if (IsComplimentaryMembership(person)) { return result; }
            decimal yearlyFee = GetTermFee(BillingTerm.TermNumber)
                                    + GetTotalOtherMembershipFees(person);
            decimal semesterFee = decimal.Round(yearlyFee / 2, 2);
            var totalFee = CalculateFee(person);
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
    private decimal GetTotalOtherMembershipFees(Person person)
    {
        var result = 0m;
        result = Fees
                    .Where(x => x.PersonID == person.ID
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
    public decimal CalculateFee()
    {
        return CalculateFee(null, null);
    }
    public decimal CalculateFee(int CalculateForTerm)
    {
        return CalculateFee(null, CalculateForTerm);
    }

    object lockObject = new();
    public decimal CalculateFee(Person? person = null,
                                int? CalculateForTerm = null)
    {
        var result = decimal.Zero;
        if (person == null) { person = People.FirstOrDefault(); }
        var defaultDate = new DateTime(BillingYear, 1, 1);

        // Make PersonWithFinancialStatus a local variable (thread-safe)
        var personFinancialStatus = new PersonFinancialStatus()
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
        };
        var enrolmentCounts = enrolmentCount.TryGetValue(person.ID, out var counts) ? counts : (0, 0);
        personFinancialStatus.Enrolments = counts.Enrolled;
        personFinancialStatus.Waitlisted = counts.Waitlisted;
        var isComplimentary = IsComplimentaryMembership(person);
        personFinancialStatus.IsComplimentary = isComplimentary;
        // membership fees
        if (isComplimentary && Settings.IncludeMembershipFeeInComplimentary)
        {
            var complimentaryCalcDate = GetComplimentaryCalculationDate(person);
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
            var fee = CalculateMembershipFee(person);
            personFinancialStatus.MembershipFees = fee;
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
                personFinancialStatus.MailSurcharge = Settings.MailSurcharge;
                AddFee(person,
                    MemberFeeSortOrder.MailSurcharge, defaultDate, $"{BillingYear} mail surcharge", Settings.MailSurcharge);
            }
        }
        // course fees
        var courseFeeTotal = AddCourseFees(person, isComplimentary);
        personFinancialStatus.CourseFeesPerYear = courseFeeTotal.TotalCourseFeesPerYear;
        personFinancialStatus.CourseFeesPerTerm = courseFeeTotal.TotalCourseFeesPerTerm;
        var leaderFeeTotal = AddLeadersFees(person, isComplimentary);
        personFinancialStatus.CourseFeesPerYear += leaderFeeTotal.TotalCourseFeesPerYear;
        personFinancialStatus.CourseFeesPerTerm += leaderFeeTotal.TotalCourseFeesPerTerm;
        // add fee adjustments
        personFinancialStatus.OtherFees = AddFees(person);
        // less payments
        var receiptTotal = SubtractReceipts(person);
        personFinancialStatus.AmountReceived = receiptTotal.TotalReceipts;
        personFinancialStatus.LastReceipt = receiptTotal.LastReceiptDate ?? defaultDate;
        // total due
        var fees = MemberFees.TryGetValue(person.ID, out var list) ? list : new ConcurrentBag<MemberFee>();
        result = fees.Sum(x => x.Amount);
        PeopleWithFinancialStatus.Add(personFinancialStatus);
        return result;
    }

    private bool IsComplimentaryMembership(Person person)
    {
        bool result = false;
        if (person.IsLifeMember) { result = true; }
        else
        {
            // A zero-valued receipt is created when a person is given complimentary membership
            var receipts = Receipts.TryGetValue(person.ID, out var list) ? list : new List<Receipt>();
            result = receipts.Any(x => x.PersonID == person.ID
                                    && x.FinancialTo >= BillingYear
                                    && x.Amount == 0);
        }
        return result;
    }
    private DateTime? GetComplimentaryCalculationDate(Person person)
    {
        DateTime? result = null;
        Receipt? receipt;
        var receipts = Receipts.TryGetValue(person.ID, out var list) ? list : new List<Receipt>();
        receipt = receipts.OrderBy(x => x.Date).FirstOrDefault(x => x.Amount == 0);
        if (receipt != null) { result = receipt.Date; }
        return result;
    }

    private Term GetBillingTerm()
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

    public decimal CalculateMinimumFeePayable()
    {
        return CalculateMinimumFeePayable(null, null);
    }
    public decimal CalculateMinimumFeePayable(int CalculateForTerm)
    {
        return CalculateMinimumFeePayable(null, CalculateForTerm);
    }
    public decimal CalculateMinimumFeePayable(Person? person = null, int? CalculateForTerm = null)
    {
        var result = decimal.Zero;
        if (person == null) { person = People.FirstOrDefault(); }
        if (BillingTerm != null && !IsComplimentaryMembership(person))
        {
            result = CalculateMembershipFee(person);
            result += Fees
                        .Where(x => x.PersonID == person.ID
                                    && x.IsMembershipFee)
                        .Sum(x => x.Amount);
            if (CalculateForTerm.HasValue) { result = decimal.Round(result / 4m * (decimal)CalculateForTerm, 2); }
        }
        return result;
    }

    private decimal CalculateMembershipFee(Person person)
    {
        if (person is Contact) { return 0; }
        decimal result = 0;
        bool foundTerm = false;
        result = Settings.MembershipFee; // set the default
        DateTime? feeDueDate = null;
        if (person.DateJoined?.Year == BillingYear) feeDueDate = person.DateJoined; // New member; use join date
        if (feeDueDate == null) // Renewing member
        {
            var firstReceiptDate = GetFirstReceiptDate(person);
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

    private DateTime? GetFirstReceiptDate(Person person)
    {
        DateTime? result = null;
        Receipt? receipt = null;
        var receipts = Receipts.TryGetValue(person.ID, out var list) ? list : new List<Receipt>();
        receipt = receipts.OrderBy(x => x.Date).FirstOrDefault(x => !x.IsDeleted
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

    private decimal AddFees(Person person)
    {
        decimal result = 0;
        List<Fee> fees;
        fees = Fees.Where(x => x.PersonID == person.ID
                                && x.Amount != 0
                                && x.ProcessingYear == BillingYear).ToList();
        foreach (var f in fees)
        {
            AddFee(person,
                    MemberFeeSortOrder.AdditionalFee, f.Date, f.Description, f.Amount);
            result += f.Amount;
        }
        return result;
    }
    private (decimal TotalReceipts, DateTime? LastReceiptDate) SubtractReceipts(Person person)
    {
        (decimal TotalReceipts, DateTime? LastReceiptDate) result = (0, null);
        var receipts = Receipts.TryGetValue(person.ID, out var list) ? list : new List<Receipt>();
        receipts = receipts.Where(x => !x.IsDeleted
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
            result.TotalReceipts += r.Amount;
            result.LastReceiptDate = r.Date;
        }
        return result;
    }

    private (decimal TotalCourseFeesPerYear, decimal TotalCourseFeesPerTerm) AddCourseFees(Person person, bool IsComplimentary)
    {
        (decimal TotalCourseFeesPerYear, decimal TotalCourseFeesPerTerm) result = (0, 0);
        DateTime today = localTime.Date;
        List<Enrolment> enrolments = Enrolments.TryGetValue(person.ID, out var list) ? list : new List<Enrolment>();
        enrolments = enrolments.Where(x => !x.IsDeleted
                                        && x.PersonID == person.ID
                                        && x.Term.Year == BillingYear
                                        && !x.IsWaitlisted).ToList();
        foreach (var t in Terms)
        {
            foreach (var e in enrolments.Where(x => x.TermID == t.ID))
            {
                DateTime dateEnrolled = e.DateEnrolled.Value + UtcOffset;
                dateEnrolled = new DateTime(dateEnrolled.Year, dateEnrolled.Month, dateEnrolled.Day);
                DateTime dateDue = (e.Course.CourseFeePerYearDueDate.HasValue)
                                        ? e.Course.CourseFeePerYearDueDate.Value.ToDateTime(TimeOnly.MinValue)
                                        : dateEnrolled;
                if (dateDue <= today)
                {
                    if (e.Course.CourseFeePerYear != 0 && !CourseFeeAdded.Contains((person.ID, e.CourseID)))
                    {
                        var description = $"{e.Course.Name} course fee";
                        decimal amount = e.Course.CourseFeePerYear;
                        var includeInComplimentary = Settings.IncludeCourseFeePerYearInComplimentary;
                        if (e.Course.OverrideComplimentaryPerYearFee) includeInComplimentary = !includeInComplimentary;
                        if (!string.IsNullOrWhiteSpace(e.Course.CourseFeePerYearDescription))
                        {
                            description += $": {e.Course.CourseFeePerYearDescription}";
                        }
                        if (IsComplimentary && includeInComplimentary)
                        {
                            description = $"(Complimentary) {description}";
                            amount = 0.00M;
                        }
                        AddFee(person,
                            MemberFeeSortOrder.CourseFee,
                                dateDue, description, amount, e.Course.Name, e.CourseID);
                        result.TotalCourseFeesPerYear += amount;
                        CourseFeeAdded.Add((person.ID, e.CourseID));
                    }
                }
                var dueDateAdjustment = e.Course.CourseFeePerTermDueWeeks ?? 0;
                dateDue = t.StartDate.AddDays(dueDateAdjustment * 7);
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
                        decimal amount = 0.00M;
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
                        if (e.Course.OverrideComplimentaryPerTermFee) includeInComplimentary = !includeInComplimentary;
                        if (!string.IsNullOrWhiteSpace(e.Course.CourseFeePerTermDescription))
                        {
                            description += $": {e.Course.CourseFeePerTermDescription}";
                        }
                        if (IsComplimentary && includeInComplimentary)
                        {
                            amount = 0.00M;
                            description = $"Complimentary) {description}";
                        }
                        AddFee(person, sortOrder, dateDue,
                            $"{t.Name}: {description}", amount, e.Course.Name, e.CourseID);
                        result.TotalCourseFeesPerTerm += amount;
                        TermFeeAdded.Add((e.PersonID, e.CourseID, e.TermID));
                    }
                }
            }
        }
        return result;
    }

    private (decimal TotalCourseFeesPerYear, decimal TotalCourseFeesPerTerm) AddLeadersFees(Person person, bool isComplimentary)
    {
        (decimal TotalCourseFeesPerYear, decimal TotalCourseFeesPerTerm) result = (0, 0);
        DateTime today = localTime.Date;
        List<Class> classesLead;
        classesLead = Classes.Where(x =>
                                        x.Course.LeadersPayYearFee &&
                                        (x.LeaderID == person.ID ||
                                        x.Leader2ID == person.ID ||
                                        x.Leader3ID == person.ID)).ToList();
        DateTime dueDate;
        foreach (var c in classesLead)
        {
            //Fees per year
            dueDate = (c.Course.CourseFeePerYearDueDate.HasValue)
                ? c.Course.CourseFeePerYearDueDate.Value.ToDateTime(TimeOnly.MinValue)
                : (c.StartDate.HasValue) ? c.StartDate.Value : new DateTime(BillingYear, 1, 1);
            if (dueDate <= today)
            {
                if (c.Course.CourseFeePerYear != 0
                        && c.Course.LeadersPayYearFee
                        && !CourseFeeAdded.Contains((person.ID, c.CourseID)))
                {
                    var description = $"{c.Course.Name} course fee";
                    decimal amount = c.Course.CourseFeePerYear;
                    if (!string.IsNullOrWhiteSpace(c.Course.CourseFeePerYearDescription))
                    {
                        description += $": {c.Course.CourseFeePerYearDescription}";
                    }
                    var includeInComplimentary = Settings.IncludeCourseFeePerYearInComplimentary;
                    if (c.Course.OverrideComplimentaryPerYearFee) includeInComplimentary = !includeInComplimentary;
                    if (isComplimentary && includeInComplimentary)
                    {
                        amount = 0.00M;
                        description = $"(Complimentary) {description}";
                    }
                    AddFee(person, MemberFeeSortOrder.CourseFee, dueDate,
                                description, amount, c.Course.Name, c.CourseID);
                    result.TotalCourseFeesPerYear += amount;
                    CourseFeeAdded.Add((person.ID, c.CourseID));
                }
            }
        }
        //Fees per term
        foreach (var t in Terms)
        {
            foreach (var c in classesLead)
            {
                if (TermFeeAdded.Contains((person.ID, c.CourseID, t.ID))) continue;
                var dueDateAdjustment = c.Course.CourseFeePerTermDueWeeks ?? 0;
                dueDate = t.StartDate.AddDays(dueDateAdjustment * 7);
                if (c.Course.LeadersPayTermFee
                        && dueDate <= today)
                {
                    if (isClassHeldThisTerm(c, t))
                    {
                        var description = $"{c.Course.Name} fee";
                        decimal amount = 0.00M;
                        if (!string.IsNullOrWhiteSpace(c.Course.CourseFeePerTermDescription))
                        {
                            description += $": {c.Course.CourseFeePerTermDescription}";
                        }
                        MemberFeeSortOrder sortOrder = default;
                        switch (t.TermNumber)
                        {
                            case 1:
                                amount = c.Course.CourseFeeTerm1;
                                sortOrder = MemberFeeSortOrder.Term1Fee;
                                break;
                            case 2:
                                amount = c.Course.CourseFeeTerm2;
                                sortOrder = MemberFeeSortOrder.Term2Fee;
                                break;
                            case 3:
                                amount = c.Course.CourseFeeTerm3;
                                sortOrder = MemberFeeSortOrder.Term3Fee;
                                break;
                            case 4:
                                amount = c.Course.CourseFeeTerm4;
                                sortOrder = MemberFeeSortOrder.Term4Fee;
                                break;
                            default:
                                break;
                        }
                        var includeInComplimentary = Settings.IncludeCourseFeePerTermInComplimentary;
                        if (c.Course.OverrideComplimentaryPerTermFee) includeInComplimentary = !includeInComplimentary;
                        if (isComplimentary && includeInComplimentary)
                        {
                            amount = 0.00M;
                            description = $"(Complimentary) {description}";
                        }
                        AddFee(person, sortOrder, dueDate,
                            $"{t.Name}: {description}", amount, c.Course.Name, c.CourseID);
                        result.TotalCourseFeesPerTerm += amount;
                        TermFeeAdded.Add((person.ID, c.CourseID, t.ID));
                    }
                }
            }
        }
        return result;
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
        string Course = "",
        Guid? CourseID = null)
    {
        var value = decimal.Round(amount, 2);
        var fee = (new MemberFee
        {
            PersonID = person.ID,
            Person = person,
            SortOrder = sortOrder,
            Date = date,
            Description = description,
            Amount = value,
            Course = Course,
            CourseID = CourseID
        });
        ConcurrentBag<MemberFee> fees;
        if (!MemberFees.TryGetValue(person.ID, out fees))
        {
            fees = new();
            MemberFees.TryAdd(person.ID, fees);
        }
        fees.Add(fee);
    }

    public List<MemberFee> AllocateMemberPayments(IEnumerable<MemberFee> ItemsToAllocate, Person person,
                            bool ShowFullAllocation, bool AddUnallocatedCredit = false)
    {
        List<MemberFee> result = new List<MemberFee>();
        List<MemberFee> allocatedItems = new List<MemberFee>();
        List<List<MemberFee>> combinations = new List<List<MemberFee>>();
        foreach (var item in ItemsToAllocate)
        {
            item.Allocated = item.Balance = null;
            item.IsNotAllocated = false;
        }
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
