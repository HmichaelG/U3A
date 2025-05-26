using DevExpress.XtraRichEdit.Import.Rtf;
using DevExpress.XtraRichEdit.Layout;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
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
using Serilog;
using Microsoft.AspNetCore.Hosting;

namespace U3A.Services;

public class MemberFeeCalculationService
{
    ConcurrentBag<MemberFee> MemberFees { get; set; } = new();
    public int BillingYear { get; set; }
    public Term BillingTerm { get; set; }

    public MemberFeeCalculationService()
    {
        MemberFees = new ConcurrentBag<MemberFee>();
    }

    public PersonFinancialStatus? PersonWithFinancialStatus { get; set; }

    public List<MemberFee> GetMemberFees(Guid PersonID) => MemberFees
                                                  .Where(x => x.PersonID == PersonID)
                                                  .OrderBy(x => x.Date)
                                                  .ThenBy(x => x.SortOrder)
                                                  .ToList();
    public async Task<List<MemberPaymentAvailable>> GetAvailableMemberPaymentsAsync(U3ADbContext dbc, Person person)
    {
        var result = new List<MemberPaymentAvailable>();
        var settings = await dbc.SystemSettings.FirstOrDefaultAsync();
        if (settings.AllowedMemberFeePaymentTypes == MemberFeePaymentType.PerYearAndPerSemester)
        {
            var term = await GetBillingTermAsync(dbc);
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
        return await dbc.Fee.AsNoTracking().IgnoreQueryFilters()
                                .Where(x => !x.Person.IsDeleted
                                        && x.PersonID == person.ID
                                        && x.IsMembershipFee
                                        && x.ProcessingYear == term.Year)
                                .Select(x => x.Amount).SumAsync();
    }

    async Task<decimal> GetTotalReceipts(U3ADbContext dbc, Person person, Term term)
    {
        return await dbc.Receipt.AsNoTracking().IgnoreQueryFilters()
                            .Where(x => !x.Person.IsDeleted
                                && x.PersonID == person.ID
                                && x.ProcessingYear == term.Year)
                            .Select(x => x.Amount).SumAsync();
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
        var fees = new ConcurrentBag<MemberFee>();
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
            var settings = await dbc.SystemSettings.FirstOrDefaultAsync();
            if (settings != null)
            {
                var isComplimentary = await IsComplimentaryMembership(dbc, person, term.Year);
                // membership fees
                if (isComplimentary && settings.IncludeMembershipFeeInComplimentary)
                {
                    var complimentaryCalcDate = await GetComplimentaryCalculationDate(dbc, person, term.Year);
                    if (complimentaryCalcDate != null)
                    {
                        AddFee(person.ID,
                            MemberFeeSortOrder.Complimentary, null, $"{complimentaryCalcDate?.ToString("dd-MMM-yyyy")} {term.Year} complimentary membership", 0.00M);
                    }
                    else
                    {
                        AddFee(person.ID,
                            MemberFeeSortOrder.Complimentary, null, $"{term.Year} complimentary membership", 0.00M);
                    }
                }
                else
                {
                    PersonWithFinancialStatus.MembershipFees = await CalculateMembershipFeeAsync(dbc, person.DateJoined.GetValueOrDefault(), term, person);
                    var fee = PersonWithFinancialStatus.MembershipFees;
                    if (fee != 0)
                    {
                        if (CalculateForTerm.HasValue) { fee = decimal.Round(fee / 4m * (decimal)CalculateForTerm, 2); }
                        AddFee(person.ID,
                            MemberFeeSortOrder.MemberFee, null, $"{term.Year} membership fee", fee);
                    }
                }
                if (person.Communication != "Email")
                {
                    if (isComplimentary && settings.IncludeMailSurchargeInComplimentary)
                    {
                        AddFee(person.ID,
                            MemberFeeSortOrder.MailSurcharge, null, $"{term.Year} complimentary mail surcharge", 0.00M);
                    }
                    else
                    {
                        PersonWithFinancialStatus.MailSurcharge = settings.MailSurcharge;
                        AddFee(person.ID,
                            MemberFeeSortOrder.MailSurcharge, null, $"{term.Year} mail surcharge", settings.MailSurcharge);
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
            result = await dbc.Receipt.AnyAsync(x => x.PersonID == person.ID
                                        && x.FinancialTo >= BillingYear
                                        && x.Amount == 0);
        }
        if (PersonWithFinancialStatus != null)
            PersonWithFinancialStatus.IsComplimentary = result;
        return result;
    }
    private async Task<DateTime?> GetComplimentaryCalculationDate(U3ADbContext dbc, Person person, int Yaer)
    {
        DateTime? result = null;
        var receipt = await dbc.Receipt.FirstOrDefaultAsync(x => x.PersonID == person.ID
                                        && x.FinancialTo >= BillingYear
                                        && x.Amount == 0);
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
        if (result != null) BillingYear = result.Year;
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
                                    Person person, int? CalclateForTerm = null)
    {
        var result = decimal.Zero;
        var term = await GetBillingTermAsync(dbc);
        if (term != null && !await IsComplimentaryMembership(dbc, person, term.Year))
        {
            result = await CalculateMembershipFeeAsync(dbc, person.DateJoined.GetValueOrDefault(), term, person);
            result += dbc.Fee.IgnoreQueryFilters()
                                        .Where(x => !x.Person.IsDeleted
                                         && x.PersonID == person.ID
                                         && x.Amount != 0
                                         && x.IsMembershipFee
                                         && x.ProcessingYear == term.Year).Select(x => x.Amount).Sum();
            if (CalclateForTerm.HasValue) { result = decimal.Round(result / 4m * (decimal)CalclateForTerm, 2); }
        }
        return result;
    }

    private async Task<decimal> CalculateMembershipFeeAsync(U3ADbContext dbc, DateTime DateJoined, Term term, Person person)
    {
        if (person is Contact) { return 0; }
        decimal result = 0;
        bool foundTerm = false;
        var settings = await dbc.SystemSettings.FirstOrDefaultAsync();
        if (settings != null)
        {
            result = settings.MembershipFee; // set the default
            if (DateJoined.Year == term.Year)
            {
                var terms = await dbc.Term.Where(x => x.Year == term.Year).OrderBy(x => x.TermNumber).ToArrayAsync();
                if (terms.Length > 1)
                {
                    // current enrolment term has precedence
                    for (int i = 1; i < terms.Length; i++)
                    { // for terms 2 thru 4
                        var lastTerm = terms[i - 1];
                        var thisTerm = terms[i];
                        if (DateJoined > lastTerm.EnrolmentEndDate && DateJoined <= thisTerm.EnrolmentEndDate)
                        {
                            result = GetTermFee(settings, thisTerm.TermNumber);
                            foundTerm = true;
                            break; // exit for
                        }
                    }
                    // otherwise, use current term
                    if (!foundTerm)
                    {
                        for (int i = 1; i < terms.Length; i++)
                        { // for terms 2 thru 4
                            var lastTerm = terms[i - 1];
                            var thisTerm = terms[i];
                            if (DateJoined > lastTerm.EndDate && DateJoined <= thisTerm.EndDate)
                            {
                                result = GetTermFee(settings, thisTerm.TermNumber);
                                break; // exit for
                            }
                        }
                    }
                }
            }
        }
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
        foreach (var r in await dbc.Fee.IgnoreQueryFilters().AsNoTracking()
                                        .Where(x => !x.Person.IsDeleted
                                                && x.PersonID == person.ID
                                                && x.Amount != 0
                                                && x.ProcessingYear == term.Year).ToArrayAsync())
        {
            AddFee(person.ID,
                    MemberFeeSortOrder.AdditionalFee, r.Date, r.Description, r.Amount);
            PersonWithFinancialStatus.OtherFees += r.Amount;
        }
    }
    private async Task SubtractReceiptsAsync(U3ADbContext dbc, Person person, Term term)
    {
        foreach (var r in await dbc.Receipt.AsNoTracking().IgnoreQueryFilters()
                                        .OrderBy(x => x.Date)
                                        .Where(x => !x.IsDeleted
                                            && x.PersonID == person.ID
                                            && x.Amount != 0
                                            && x.ProcessingYear == term.Year).ToArrayAsync())
        {
            AddFee(person.ID,
                    MemberFeeSortOrder.Receipt, r.Date,"Payment received - thank you", -r.Amount);
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
        var terms = await dbc.Term.AsNoTracking()
                            .Where(x => x.Year == term.Year)
                            .OrderBy(x => x.TermNumber)
                            .ToArrayAsync();
        var courseFeeAdded = new List<Guid>();
        foreach (var t in terms)
        {
            foreach (var e in await dbc.Enrolment.AsNoTracking().IgnoreQueryFilters()
                                .Include(x => x.Course).ThenInclude(x => x.Classes)
                                .Include(x => x.Class)
                                .Where(x => !x.IsDeleted && !x.Person.IsDeleted
                                            && x.PersonID == person.ID
                                            && x.TermID == t.ID
                                            && !x.IsWaitlisted).ToArrayAsync())
            {
                DateOnly dueDate = e.Course.CourseFeePerYearDueDate ?? DateOnly.FromDateTime(e.DateEnrolled.Value);
                if (dueDate <= today)
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
                        AddFee(person.ID,
                            MemberFeeSortOrder.CourseFee, ConvertDateOnlyToDateTime(dueDate),description,amount);
                        if (PersonWithFinancialStatus != null)
                            PersonWithFinancialStatus.CourseFeesPerYear += amount;
                        courseFeeAdded.Add(e.CourseID);
                    }
                }
                var dueDateAdjustment = e.Course.CourseFeePerTermDueWeeks ?? 0;
                dueDate = DateOnly.FromDateTime(t.StartDate.AddDays(dueDateAdjustment * 7));
                if (e.Course.CourseFeePerTerm != 0 && dueDate <= today)
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
                        var amount = e.Course.CourseFeePerTerm;
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
                        AddFee(person.ID,
                            MemberFeeSortOrder.TermFee, ConvertDateOnlyToDateTime(dueDate),
                            $"{t.Name}: {description}", amount);
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
        var terms = await dbc.Term.AsNoTracking()
                            .Where(x => x.Year == term.Year)
                            .OrderBy(x => x.TermNumber)
                            .ToArrayAsync();
        var courseFeeAdded = new List<Guid>();
        var classesLead = await dbc.Class
                                .Include(x => x.Course)
                                .Where(x =>
                                        x.Course.Year == term.Year && (x.Course.CourseFeePerTerm > 0 && x.Course.LeadersPayTermFee &&
                                        (x.LeaderID == person.ID ||
                                        x.Leader2ID == person.ID ||
                                        x.Leader3ID == person.ID))).ToListAsync();
        foreach (var c in classesLead)
        {
            //Fees per year
            DateOnly dueDate = c.Course.CourseFeePerYearDueDate ?? new DateOnly(term.Year,1,1);
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
                    AddFee(person.ID, MemberFeeSortOrder.TermFee, ConvertDateOnlyToDateTime(dueDate),
                                description, amount);
                    if (PersonWithFinancialStatus != null)
                        PersonWithFinancialStatus.CourseFeesPerYear += amount;
                    courseFeeAdded.Add(c.CourseID);
                }
            }

            //Fees per term
            foreach (var t in terms)
            {
                var dueDateAdjustment = c.Course.CourseFeePerTermDueWeeks ?? 0;
                dueDate = DateOnly.FromDateTime(t.StartDate.AddDays(dueDateAdjustment * 7));
                Log.Information($"{c.Course.Name} {t.TermNumber} {dueDate}");
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
                        AddFee(person.ID, MemberFeeSortOrder.TermFee, ConvertDateOnlyToDateTime(dueDate),
                            $"{t.Name}: {description}", amount);
                        if (PersonWithFinancialStatus != null)
                            PersonWithFinancialStatus.CourseFeesPerTerm += amount;
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

    private void AddFee(Guid personID, MemberFeeSortOrder sortOrder, DateTime? date, string description, decimal amount)
    {
        var value = decimal.Round(amount, 2);
            MemberFees.Add(new MemberFee
            {
                PersonID = personID,
                SortOrder = sortOrder,
                Date = date,
                Description = description,
                Amount = value
            });
    }

    public DateTime ConvertDateOnlyToDateTime(DateOnly date)
    {
        return new DateTime(date.Year, date.Month, date.Day);
    }
    private static async Task<int> ActiveCourseCountAsync(U3ADbContext dbc, Person person, Term SelectedTerm)
    {
        return (await dbc.Enrolment.Include(x => x.Course)
                        .Where(x => x.PersonID == person.ID &&
                            x.TermID == SelectedTerm.ID &&
                            !x.Course.ExcludeFromLeaderComplimentaryCount &&
                            !x.IsWaitlisted).ToListAsync()).DistinctBy(x => x.CourseID).Count();
    }
    private static async Task<int> WaitlistedCourseCountAsync(U3ADbContext dbc, Person person, Term SelectedTerm)
    {
        return (await dbc.Enrolment.Include(x => x.Course)
                        .Where(x => x.PersonID == person.ID &&
                            x.TermID == SelectedTerm.ID &&
                            !x.Course.ExcludeFromLeaderComplimentaryCount &&
                            x.IsWaitlisted).ToListAsync()).DistinctBy(x => x.CourseID).Count();
    }

    public List<MemberFee> AllocateMemberPayments(IEnumerable<MemberFee> ItemsToAllocate, bool ShowFullAllocation)
    {
        List<MemberFee> result = new List<MemberFee>();
        List<MemberFee> allocatedItems = new List<MemberFee>();
        List<List<MemberFee>> combinations = new List<List<MemberFee>>();
        var fees = ItemsToAllocate.Where(x => x.SortOrder != MemberFeeSortOrder.Receipt).ToArray();
        var receipts = ItemsToAllocate.Where(x => x.SortOrder == MemberFeeSortOrder.Receipt).ToArray();

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
                if (totalDue != 0) { item.IsNotAllocated = true; }// Mark as not allocated
                result.Add(item);
            }
        }
        else foreach (var item in ItemsToAllocate.Where(x => !keys.Contains(x.ID)))
            {
                result.Add(item);
            }
        CalculateBalance(result);
        return result;
    }

    void CalculateBalance(IEnumerable<MemberFee> memberFees)
    {
        var result = decimal.Zero;
        var itemCount = memberFees.Count();
        int i = 0;
        foreach (var item in memberFees)
        {
            i += 1;
            result += item.Amount;
            item.Balance = ((item.SortOrder == MemberFeeSortOrder.Receipt && result == 0) || i == itemCount) ? result : null;
        }
    }

}
