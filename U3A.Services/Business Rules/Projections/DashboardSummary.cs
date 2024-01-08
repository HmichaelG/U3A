using DevExpress.Xpo.Logger;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Twilio.TwiML.Fax;
using Twilio.TwiML.Voice;
using U3A.Database;
using U3A.Model;

namespace U3A.BusinessRules
{
    public static partial class BusinessRule
    {
        public static List<ReceiptSummary> GetReceiptSummaryByMonth(U3ADbContext dbc)
        {
            var to = DateTime.Today.AddMonths(1).AddDays(-DateTime.Today.Day);
            var start = to.AddMonths(-13).AddDays(1);
            while (start.Day != 1) { start = start.AddDays(1); }
            var monthlyTotals = dbc.Receipt
                .Where(r => r.Date >= start).AsEnumerable()
                .GroupBy(r => new
                {
                    Month = r.Date.Month,
                    Year = r.Date.Year,
                    Type = (r.IsOnlinePayment) ? "Online" : "Other"
                })
                .Select(g => new ReceiptSummary
                {
                    Month = g.Key.Month,
                    Year = g.Key.Year,
                    Type = g.Key.Type,
                    Total = g.Sum(r => r.Amount)
                }).ToList();

            var emptyMonths = new List<ReceiptSummary>();
            while (start < to)
            {
                if (!monthlyTotals.Any(x => x.Year == start.Year && x.Month == start.Month))
                {
                    emptyMonths.Add(new()
                    {
                        Total = 0,
                        Year = start.Year,
                        Month = start.Month,
                        Type = "Online"
                    });
                    emptyMonths.Add(new()
                    {
                        Total = 0,
                        Year = start.Year,
                        Month = start.Month,
                        Type = "Other"
                    });
                }
                start = start.AddMonths(1);
            }
            monthlyTotals.AddRange(emptyMonths);
            return monthlyTotals;
        }
        public static async Task<List<EnrolmentSummary>> GetEnrolmentSummaryByTerm(U3ADbContext dbc, Term term)
        {
            var to = DateTime.Today.AddMonths(1).AddDays(-DateTime.Today.Day);
            var start = to.AddMonths(-13).AddDays(1);
            while (start.Day != 1) { start = start.AddDays(1); }

            // a summary of enrolments by CourseType then month
            var monthlyTotals = await dbc.Enrolment
                .Include(x => x.Course).ThenInclude(x => x.CourseType)
                .Where(x => x.Created > start && x.Created <= to && !x.IsWaitlisted)
                .Select(x => new
                {
                    Period = x.Created.Date.AddMonths(1).AddDays(-x.Created.Day),
                    CourseType = x.Course.CourseType.Name
                })
                .GroupBy(x => new
                {
                    Period = x.Period,
                    CourseType = x.CourseType
                })
                .Select(x => new EnrolmentSummary
                {
                    Period = new DateTime(x.Key.Period.Year, x.Key.Period.Month, 1),
                    CourseType = x.Key.CourseType,
                    IsDropout = false,
                    Count = x.Count()
                }).ToListAsync();


            // a summary of dropouts by CourseType then month           
            var dropouts = await dbc.Dropout
                .Include(x => x.Course).ThenInclude(x => x.CourseType)
                .Where(x => x.Created > start && x.Created <= to)
                .Select(x => new
                {
                    Period = x.Created.Date.AddMonths(1).AddDays(-x.Created.Day),
                    CourseType = x.Course.CourseType.Name
                })
                .GroupBy(x => new
                {
                    Period = x.Period,
                    CourseType = x.CourseType
                })
                .Select(x => new EnrolmentSummary
                {
                    Period = new DateTime(x.Key.Period.Year, x.Key.Period.Month, 1),
                    CourseType = x.Key.CourseType,
                    IsDropout = true,
                    Count = x.Count()
                }).ToListAsync();

            //add the dropouts to monthly totals
            monthlyTotals.AddRange(dropouts);

            //and, now subtract them
            var withdrawn = await dbc.Dropout
                .Include(x => x.Course).ThenInclude(x => x.CourseType)
                .Where(x => x.DropoutDate > start && x.DropoutDate <= to)
                .Select(x => new
                {
                    Period = x.Created.Date.AddMonths(1).AddDays(-x.Created.Day)
                })
                .GroupBy(x => new
                {
                    Period = x.Period
                })
                .Select(x => new EnrolmentSummary
                {
                    Period = new DateTime(x.Key.Period.Year, x.Key.Period.Month, 1),
                    CourseType = "Withdrawn",
                    IsDropout = true,
                    Count = -x.Count()
                }).ToListAsync();
            monthlyTotals.AddRange(withdrawn);

            var emptyMonths = new List<EnrolmentSummary>();
            while (start < to)
            {
                if (!monthlyTotals.Any(x => x.Period == new DateTime(start.Year, start.Month, 1)))
                {
                    emptyMonths.Add(new()
                    {
                        Count = 0,
                        Period = start,
                        CourseType = "Withdrawn"
                    }
                    );
                }
                start = start.AddMonths(1);
            }
            monthlyTotals.AddRange(emptyMonths);
            return monthlyTotals;
        }
        public static async Task<List<MemberSummary>> GetAttritionSummary(U3ADbContext dbc, Term term)
        {
            var result = new List<MemberSummary>();
            var didNotRenew = await dbc.Person
                            .CountAsync(x => x.FinancialTo == term.Year - 1);
            var joined = dbc.Person.AsEnumerable()
                            .Count(x => x.FinancialTo >= term.Year &&
                                                (x.DateJoined.GetValueOrDefault().Year == term.Year ||
                                                 x.DateJoined.GetValueOrDefault().Year == term.Year - 1));
            result.Add(new MemberSummary
            {
                Year = 0,
                Group = "Did not renew",
                Count = -didNotRenew
            });
            result.Add(new MemberSummary
            {
                Year = 1,
                Group = "New members",
                Count = joined
            });
            result.Add(new MemberSummary
            {
                Year = 2,
                Group = "Net Gain/Loss",
                Count = joined - didNotRenew
            });
            return result;
        }
        public static async Task<List<MemberSummary>> GetEnrolmentSummary(U3ADbContext dbc, Term term)
        {
            return await GetEnrolmentSummary(dbc, term, false);
        }
        public static async Task<List<MemberSummary>> GetWaitlistSummary(U3ADbContext dbc, Term term)
        {
            return await GetEnrolmentSummary(dbc, term, true);
        }
        private static async Task<List<MemberSummary>> GetEnrolmentSummary(U3ADbContext dbc, Term term, bool IsWaitlisted)
        {
            var result = await dbc.Enrolment
                .Include(x => x.Term)
                .Include(x => x.Course).ThenInclude(x => x.CourseType)
                .Where(x => x.Term.ID == term.ID && x.IsWaitlisted == IsWaitlisted)
                .Select(x => new
                {
                    CourseType = x.Course.CourseType.Name
                })
                .GroupBy(x => new
                {
                    CourseType = x.CourseType
                })
                .Select(x => new MemberSummary
                {
                    Group = x.Key.CourseType,
                    Count = x.Count()
                })
                .OrderBy(x => x.Group)
                .ToListAsync();
            if (result.Count == 0)
            {
                result.Add(new MemberSummary { 
                    Count = 0,
                    Month = 1,
                    Group = "Enrolments",
                    Year = term.Year,
                    });
            }
            return result;
        }
        public static async Task<List<MemberSummary>> GetMemberSummaryByGender(U3ADbContext dbc, Term term)
        {
            var result = await dbc.Person
                .Where(x => x.FinancialTo >= term.Year)
                .GroupBy(x => new
                {
                    Gender = x.Gender,
                })
                .Select(x => new MemberSummary
                {
                    Group = x.Key.Gender,
                    Count = x.Count()
                })
                .OrderBy(x => x.Group)
                .ToListAsync();
            return result;
        }
        public static async Task<List<MemberSummary>> GetMemberSummaryByMshipLength(U3ADbContext dbc, Term term)
        {
            var today = DateTime.Today;
            var result = await dbc.Person
                .Where(p => p.DateJoined != null && p.FinancialTo >= term.Year)
                .GroupBy(p => today.Year - p.DateJoined.Value.Year)
                .Select(g => new MemberSummary
                {
                    Year = g.Key,
                    Group = GetJoiningYearRange(g.Key),
                    Count = g.Count()
                })
                .OrderBy(r => r.Year).ToListAsync();
            return result;
        }

        private static string GetJoiningYearRange(int years)
        {
            if (years == 0)
            {
                return "< 1 year";
            }
            else if (years >= 1 && years <= 5)
            {
                return "1-5 years";
            }
            else if (years >= 6 && years <= 10)
            {
                return "6-10 years";
            }
            else if (years >= 11 && years <= 15)
            {
                return "11-15 years";
            }
            else if (years >= 16 && years <= 20)
            {
                return "16-20 years";
            }
            else
            {
                return "> 20 years";
            }
        }


        public static async Task<List<MemberSummary>> GetNewMemberSummaryByMonth(U3ADbContext dbc)
        {

            var endDate = DateTime.Today.AddMonths(1).AddDays(-DateTime.Today.Day);
            var startDate = endDate.AddMonths(-13).AddDays(1);
            while (startDate.Day != 1) { startDate = startDate.AddDays(1); }

            var counts = await dbc.Person
                .Where(p => p.DateJoined.HasValue && p.DateJoined.Value >= startDate && p.DateJoined.Value <= endDate)
                .GroupBy(p => new { p.DateJoined.Value.Year, p.DateJoined.Value.Month, p.Gender })
                .Select(g => new MemberSummary
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Group = g.Key.Gender,
                    Count = g.Count()
                })
                .ToListAsync();

            var allMonths = Enumerable.Range(0, 13)
                .Select(i => startDate.AddMonths(i))
                .Select(d => new { Year = d.Year, Month = d.Month })
                .Distinct()
                .ToList();

            var result = allMonths
                .SelectMany(m => counts.DefaultIfEmpty(new MemberSummary
                {
                    Year = m.Year,
                    Month = m.Month,
                    Group = "Male",
                    Count = 0
                })
                .Where(c => c.Year == m.Year && c.Month == m.Month)
                .Select(c => new MemberSummary
                {
                    Year = c.Year,
                    Month = c.Month,
                    Group = c.Group,
                    Count = c.Count
                }))
                .OrderBy(c => c.Year)
                .ThenBy(c => c.Month)
                .ThenBy(c => c.Group)
                .ToList();

            return result;
        }


    }
}