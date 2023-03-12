using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.X509Certificates;
using Twilio.TwiML.Fax;
using U3A.Database;
using U3A.Model;

namespace U3A.BusinessRules
{
    public static partial class BusinessRule
    {
        public static List<ReceiptSummary> GetReceiptSummaryByMonth(U3ADbContext dbc) {
            var last12Months = Enumerable.Range(0, 12)
                                    .Select(i => DateTime.Now.AddMonths(-i))
                                    .Select(d => new {
                                        Month = d.Month,
                                        Year = d.Year
                                    });

            var monthlyTotals = dbc.Receipt
                .Where(r => r.Date >= DateTime.Now.AddMonths(-12))
                .GroupBy(r => new {
                    Month = r.Date.Month,
                    Year = r.Date.Year
                })
                .Select(g => new {
                    Month = g.Key.Month,
                    Year = g.Key.Year,
                    Total = g.Sum(r => r.Amount)
                });

            var result = last12Months
                .GroupJoin(monthlyTotals,
                    lm => new { Month = lm.Month, Year = lm.Year },
                    mt => new { Month = mt.Month, Year = mt.Year },
                    (lm, mt) => new ReceiptSummary {
                        Month = lm.Month,
                        Year = lm.Year,
                        Total = mt.Any() ? mt.First().Total : 0
                    })
                .OrderByDescending(r => r.Year)
                .ThenByDescending(r => r.Month)
                .ToList();
            return result;
        }
        public static async Task<List<EnrolmentSummary>> GetEnrolmentSummaryByTerm(U3ADbContext dbc, Term term) {
            var result = await dbc.Enrolment
                .Include(x => x.Term)
                .Include(x => x.Course).ThenInclude(x => x.CourseType)
                .Where(x => x.Term.Year == term.Year && !x.IsWaitlisted)
                .Select(x => new {
                    TermNumber = x.Term.TermNumber,
                    TermYear = x.Term.Year,
                    CourseType = x.Course.CourseType.Name
                })
                .GroupBy(x => new {
                    Year = x.TermYear,
                    TermNumber = x.TermNumber,
                    CourseType = x.CourseType
                })
                .Select(x => new EnrolmentSummary {
                    Year = x.Key.Year,
                    Term = x.Key.TermNumber,
                    CourseType = x.Key.CourseType,
                    Count = x.Count()
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Term)
                .ThenBy(x => x.CourseType)
                .ToListAsync();
            return result;
        }
        public static async Task<List<MemberSummary>> GetAttritionSummary(U3ADbContext dbc, Term term) {
            var result = new List<MemberSummary>();
            var didNotRenew = await dbc.Person
                            .CountAsync(x => x.FinancialTo == term.Year - 1);
            var joined = dbc.Person.AsEnumerable()
                            .Count(x => x.FinancialTo == term.Year &&
                                                (x.DateJoined.GetValueOrDefault().Year == term.Year ||
                                                 x.DateJoined.GetValueOrDefault().Year == term.Year-1));
            result.Add(new MemberSummary {
                Year = 0,
                Group = "Did not renew",
                Count = -didNotRenew
            });
            result.Add(new MemberSummary {
                Year = 1,
                Group = "New members",
                Count = joined
            });
            result.Add(new MemberSummary {
                Year = 2,
                Group = "Net Gain/Loss",
                Count = joined - didNotRenew
            });
            return result;
        }
        public static async Task<List<MemberSummary>> GetEnrolmentSummary(U3ADbContext dbc, Term term) {
            return await GetEnrolmentSummary(dbc, term, false);
        }
        public static async Task<List<MemberSummary>> GetWaitlistSummary(U3ADbContext dbc, Term term) {
            return await GetEnrolmentSummary(dbc, term, true);
        }
        private static async Task<List<MemberSummary>> GetEnrolmentSummary(U3ADbContext dbc, Term term, bool IsWaitlisted) {
            var result = await dbc.Enrolment
                .Include(x => x.Term)
                .Include(x => x.Course).ThenInclude(x => x.CourseType)
                .Where(x => x.Term.ID == term.ID && x.IsWaitlisted == IsWaitlisted)
                .Select(x => new {
                    CourseType = x.Course.CourseType.Name
                })
                .GroupBy(x => new {
                    CourseType = x.CourseType
                })
                .Select(x => new MemberSummary {
                    Group = x.Key.CourseType,
                    Count = x.Count()
                })
                .OrderBy(x => x.Group)
                .ToListAsync();
            return result;
        }
        public static async Task<List<MemberSummary>> GetMemberSummaryByGender(U3ADbContext dbc, Term term) {
            var result = await dbc.Person
                .Where(x => x.FinancialTo == term.Year)
                .GroupBy(x => new {
                    Gender = x.Gender,
                })
                .Select(x => new MemberSummary {
                    Group = x.Key.Gender,
                    Count = x.Count()
                })
                .OrderBy(x => x.Group)
                .ToListAsync();
            return result;
        }
        public static async Task<List<MemberSummary>> GetMemberSummaryByMshipLength(U3ADbContext dbc, Term term) {
            var today = DateTime.Today;
            var result = await dbc.Person
                .Where(p => p.DateJoined != null && p.FinancialTo == term.Year)
                .GroupBy(p => today.Year - p.DateJoined.Value.Year)
                .Select(g => new MemberSummary {
                    Year = g.Key,
                    Group = GetJoiningYearRange(g.Key),
                    Count = g.Count()
                })
                .OrderBy(r => r.Year).ToListAsync();
            return result;
        }

        private static string GetJoiningYearRange(int years) {
            if (years == 0) {
                return "< 1 year";
            }
            else if (years >= 1 && years <= 5) {
                return "1-5 years";
            }
            else if (years >= 6 && years <= 10) {
                return "6-10 years";
            }
            else if (years >= 11 && years <= 15) {
                return "11-15 years";
            }
            else if (years >= 16 && years <= 20) {
                return "16-20 years";
            }
            else {
                return "> 20 years";
            }
        }


        public static async Task<List<MemberSummary>> GetNewMemberSummaryByMonth(U3ADbContext dbc) {

            var startDate = DateTime.Now.Date.AddMonths(-11);
            var endDate = DateTime.Now.Date;

            var counts = await dbc.Person
                .Where(p => p.DateJoined.HasValue && p.DateJoined.Value >= startDate && p.DateJoined.Value <= endDate)
                .GroupBy(p => new { p.DateJoined.Value.Year, p.DateJoined.Value.Month, p.Gender })
                .Select(g => new MemberSummary {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Group = g.Key.Gender,
                    Count = g.Count()
                })
                .ToListAsync();

            var allMonths = Enumerable.Range(0, 12)
                .Select(i => startDate.AddMonths(i))
                .Select(d => new { Year = d.Year, Month = d.Month })
                .Distinct()
                .ToList();

            var result = allMonths
                .SelectMany(m => counts.DefaultIfEmpty(new MemberSummary {
                    Year = m.Year,
                    Month = m.Month,
                    Group = "Male",
                    Count = 0
                })
                .Where(c => c.Year == m.Year && c.Month == m.Month)
                .Select(c => new MemberSummary {
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