using DevExpress.Blazor.Internal;
using DevExpress.XtraRichEdit.Model;
using Microsoft.EntityFrameworkCore;
using Twilio.Rest.Trunking.V1;
using Twilio.TwiML.Messaging;
using U3A.Database;
using U3A.Model;

namespace U3A.BusinessRules
{
    public static partial class BusinessRule
    {
        public static async Task<List<Term>> EditableTermsAsync(U3ADbContext dbc)
        {
            return await dbc.Term
                            .OrderByDescending(x => x.Year)
                            .ThenByDescending(x => x.TermNumber).ToListAsync();
        }
        public static List<Term> SelectableTerms(U3ADbContext dbc)
        {
            return dbc.Term
                            .OrderByDescending(x => x.Year)
                            .ThenByDescending(x => x.TermNumber).ToList();
        }

        public static Term? CurrentTerm(U3ADbContext dbc)
        {
            return dbc.Term.Where(x => x.IsDefaultTerm).FirstOrDefault();
        }
        public static async Task<Term?> CurrentTermAsync(U3ADbContext dbc)
        {
            return await dbc.Term.Where(x => x.IsDefaultTerm).FirstOrDefaultAsync();
        }
        public static async Task<Term?> CurrentTermAsync(U3ADbContext dbc, DateTime today)
        {
            var terms = await dbc.Term
                            .OrderBy(x => x.StartDate)
                            .Where(x => today >= x.StartDate).ToListAsync();
            return terms.Where(x => today <= x.EndDate).FirstOrDefault();
        }

        public static async Task<Term?> CurrentEnrolmentTermOrNextAsync(U3ADbContext dbc)
        {
            var result = await CurrentEnrolmentTermAsync(dbc);
            var today = dbc.GetLocalTime().Date;
            if (result == null) { result = await CurrentTermAsync(dbc, today); }
            if (result == null) { result = await NextTermAsync(dbc, today); }
            return result;
        }

        public static async Task<Term?> NextTermAsync(U3ADbContext dbc, DateTime today)
        {
            return await dbc.Term
                .OrderBy(x => x.StartDate)
                .FirstOrDefaultAsync(x => x.StartDate > today);
        }

        public static Term? NextTerm(U3ADbContext dbc, DateTime today)
        {
            return dbc.Term
                .OrderBy(x => x.StartDate)
                .Where(x => x.StartDate > today).FirstOrDefault();
        }

        public static async Task<Term?> CurrentEnrolmentTermAsync(U3ADbContext dbc)
        {
            var today = dbc.GetLocalTime().Date;
            return (await dbc.Term.AsNoTracking()
                        .OrderByDescending(x => x.Year)
                        .ThenByDescending(x => x.TermNumber)
                        .ToListAsync())
                        .Where(x => today >= x.EnrolmentStartDate && today <= x.EnrolmentEndDate)
                        .FirstOrDefault();
        }
        
        public static Term? CurrentEnrolmentTerm(U3ADbContext dbc)
        {
            var today = dbc.GetLocalTime().Date;
            return dbc.Term.AsNoTracking()
                        .OrderByDescending(x => x.Year).ThenByDescending(x => x.TermNumber).AsEnumerable()
                        .Where(x => today >= x.EnrolmentStartDate && today <= x.EnrolmentEndDate)
                        .FirstOrDefault();
        }

        public static async Task<Term?> CurrentEnrolmentTermAsync(U3ADbContext dbc, DateTime LocalNow)
        {
            var today = LocalNow.Date;
            return (await dbc.Term
                        .OrderByDescending(x => x.Year).ThenByDescending(x => x.TermNumber)
                        .ToListAsync())
                        .Where(x => today >= x.EnrolmentStartDate && today <= x.EnrolmentEndDate)
                        .FirstOrDefault();
        }
        public static Term? CurrentEnrolmentTerm(U3ADbContext dbc, DateTime LocalNow)
        {
            var today = LocalNow.Date;
            return dbc.Term
                        .OrderByDescending(x => x.Year).ThenByDescending(x => x.TermNumber).AsEnumerable()
                        .Where(x => today >= x.EnrolmentStartDate && today <= x.EnrolmentEndDate)
                        .FirstOrDefault();
        }

        public static async Task<Term?> FirstTermNextYearAsync(U3ADbContext dbc, int CurrentYear)
        {
            return await dbc.Term
                            .Where(x => x.Year == CurrentYear + 1 && x.TermNumber == 1).FirstOrDefaultAsync();
        }
        public static async Task<int> MaxTermYearAsync(U3ADbContext dbc)
        {
            var term = await dbc.Term.OrderByDescending(x => x.Year).FirstOrDefaultAsync();
            return term.Year;
        }

        /// <summary>
        /// Returns all term records greater than or equal to the current term minus one year.
        /// </summary>
        /// <param name="dbc"></param>
        /// <returns></returns>
        public static async Task<List<Term>> SelectableRelaxedTermsAsync(U3ADbContext dbc)
        {
            var currentTerm = await CurrentTermAsync(dbc);
            var terms = new List<Term>();
            if (currentTerm != null)
            {
                terms = await dbc.Term.AsNoTracking()
                            .Where(x => x.Year >= currentTerm.Year - 1)
                            .OrderBy(x => x.Year).ThenBy(x => x.TermNumber).ToListAsync();
            }
            return terms;
        }

        public static async Task<List<Term>> SelectableTermsInCurrentYearAsync(U3ADbContext dbc)
        {
            var currentTerm = await BusinessRule.CurrentEnrolmentTermOrNextAsync(dbc);
            return await SelectableTermsInCurrentYearAsync(dbc, currentTerm);
        }
        public static async Task<List<Term>> SelectableTermsInCurrentYearAsync(U3ADbContext dbc, Term CurrentTerm)
        {
            int termNoToTest = CurrentTerm.TermNumber;
            if (dbc.GetLocalTime().Date < CurrentTerm.StartDate) termNoToTest--;
            return await dbc.Term.AsNoTracking()
                .Where(x => x.Year == CurrentTerm.Year && x.TermNumber >= termNoToTest)
                .OrderBy(x => x.Year).ThenBy(x => x.TermNumber)
                .ToListAsync();
        }
        public static async Task<List<Term>> GetAllTermsInCurrentYearAsync(U3ADbContext dbc)
        {
            var currentTerm = await BusinessRule.CurrentTermAsync(dbc);
            return await GetAllTermsInCurrentYearAsync(dbc, currentTerm);
        }
        public static async Task<List<Term>> GetAllTermsInCurrentYearAsync(U3ADbContext dbc, Term CurrentTerm)
        {
            return await dbc.Term.AsNoTracking()
                .Where(x => x.Year == CurrentTerm.Year)
                .OrderBy(x => x.Year).ThenBy(x => x.TermNumber)
                .ToListAsync();
        }

        public static async Task<Term?> FindTermByDateAsync(U3ADbContext dbc, DateTime Date)
        {
            return await dbc.Term.AsNoTracking()
                        .OrderByDescending(x => x.StartDate)
                        .FirstOrDefaultAsync(x => x.StartDate <= Date);
        }
        public static Term FindFutureClassTermFromDate(U3ADbContext dbc, Class thisClass, int Year, DateTime FromDate)
        {
            Term result = null;
            var terms = dbc.Term.AsNoTracking().AsEnumerable()
                        .OrderByDescending(x => x.StartDate)
                        .Where(x => x.Year == Year && x.StartDate >= FromDate).ToList();
            foreach (var term in terms)
            {
                if (IsClassInTerm(thisClass, term.TermNumber))
                {
                    result = term;
                    break;
                }
            }
            if (result == null)
            {
                throw new Exception("Cannot find future term for class.");
            }
            return result;
        }

        public static async Task<Term?> FindTermAsync(U3ADbContext dbc, DateTime Date)
        {
            return await dbc.Term.AsNoTracking()
                        .OrderByDescending(x => x.StartDate)
                        .Where(x => x.StartDate <= Date).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Returns the term record with the highest Year / Term number.
        /// </summary>
        /// <param name="dbc"></param>
        /// <returns></returns>
        public static async Task<Term?> GetLastTermAsync(U3ADbContext dbc)
        {
            return await dbc.Term.OrderByDescending(x => x.Year)
                            .ThenByDescending(x => x.TermNumber).FirstOrDefaultAsync();
        }
        public static async Task<DateTime> GetLastAllowedClassDateForTermAsync(U3ADbContext dbc, Term CurrentTerm)
        {
            var year = CurrentTerm.Year;
            DateTime result = new DateTime(year, 12, 31);
            var nextTermNo = CurrentTerm.TermNumber + 1;
            if (nextTermNo < 4)
            {
                var nextTerm = await dbc.Term.Where(x => x.Year == year && x.TermNumber == nextTermNo).FirstOrDefaultAsync();
                result = (nextTerm != null) ? nextTerm.StartDate.AddDays(-1) : CurrentTerm.EndDate;
            }
            return result.AddDays(1);
        }
        public static async Task<Term?> GetSameTermLastYearAsync(U3ADbContext dbc, int Year, int TermNumber)
        {
            return await dbc.Term.Where(x => x.Year == Year - 1 && x.TermNumber == TermNumber).FirstOrDefaultAsync();
        }
        public static async Task<Term?> GetPreviousTermAsync(U3ADbContext dbc, int Year, int TermNumber)
        {
            int termNo = TermNumber - 1;
            int year = Year;
            if (termNo < 1) { termNo = 4; year--; }
            return await dbc.Term.FirstOrDefaultAsync(x => x.Year == year && x.TermNumber == termNo);
        }
        public static async Task<Term?> GetNextTermAsync(U3ADbContext dbc, int Year, int TermNumber)
        {
            int termNo = ++TermNumber;
            int year = Year;
            if (termNo > 4) { termNo = 1; year++; }
            return await dbc.Term.FirstOrDefaultAsync(x => x.Year == year && x.TermNumber == termNo);
        }
        public static DateTime StartOfWeek(this DateTime dt, DayOfWeek firstDayOfWeek)
        {
            int diff = (7 + (dt.DayOfWeek - firstDayOfWeek)) % 7;
            return dt.AddDays(-1 * diff).Date;
        }
    }
}
