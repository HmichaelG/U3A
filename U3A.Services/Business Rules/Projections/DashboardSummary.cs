﻿using DevExpress.Blazor.Internal.Editors;
using DevExpress.Drawing.Internal.Fonts.Interop;
using DevExpress.Xpo.Logger;
using Eway.Rapid.Abstractions.Response;
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
            var today = dbc.GetLocalTime().Date;
            var to = today.AddMonths(1).AddDays(-today.Day);
            var start = to.AddMonths(-25).AddDays(1);
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
            return monthlyTotals.OrderBy(x => x.Year).ThenBy(x => x.Month).ToList(); ;
        }
        public static async Task<List<EnrolmentSummary>> GetEnrolmentSummaryByTerm(U3ADbContext dbc, Term term)
        {
            var today = dbc.GetLocalTime().Date;
            var to = today.AddMonths(1).AddDays(-today.Day);
            var start = to.AddMonths(-13).AddDays(1);
            while (start.Day != 1) { start = start.AddDays(1); }

            // a summary of enrolments by CourseType then month
            var monthlyTotals = await dbc.Enrolment
                .Include(x => x.Course).ThenInclude(x => x.CourseType)
                .Where(x => x.Created > start && x.Created <= to && !x.IsWaitlisted)
                .Select(x => new
                {
                    Period = x.Created.Date.AddMonths(1).AddDays(-x.Created.Day),
                    CourseType = x.Course.CourseType.ShortName
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
                Group = "Retained members",
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
                    CourseType = x.Course.CourseType.ShortName
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
                result.Add(new MemberSummary
                {
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

        public static IEnumerable<SankeyDataPoint> GetMemberParticipationSummary(U3ADbContext dbc, Term term)
        {
            (DateTime?, string, string) e = new();
            var today = dbc.GetLocalTime().Date;
            var enrolments = dbc.Enrolment
                .Include(x => x.Term)
                .Include(x => x.Person)
                .Include(x => x.Course).ThenInclude(x => x.CourseType)
                .Where(x => x.Term.ID == term.ID)
                .Select(x => new { x.Person.BirthDate, x.Course.CourseType.ShortName, x.Person.Gender });
            var result = enrolments.AsEnumerable()
                .GroupBy(x => new
                {
                    Age = (x.BirthDate == null) ? int.MinValue : GetAge(x.BirthDate.Value, today),
                    Gender = x.Gender,
                })
                .Select(x => new SankeyDataPoint
                {
                    Group = "Age",
                    Source = (x.Key.Age == int.MinValue) ? "Unknown" : GetBirthDateRange(x.Key.Age),
                    Target = x.Key.Gender,
                    Count = x.Count(),
        })
                .ToList();

            result.AddRange(enrolments.AsEnumerable()
                .GroupBy(x => new
                {
                    Gender = x.Gender,
                    CourseType = x.ShortName,
                })
                .Select(x => new SankeyDataPoint
                {
                    Group = "Course",
                    Source = x.Key.Gender,
                    Target = x.Key.CourseType,
                    Count = x.Count(),
                }));
            var t = result.Sum(x => x.Count);
            return result
                .Where(x => x.Count != null && x.Count > 0)
                .OrderBy(x => x.Source);
        }
        private static int GetAge(DateTime birthDate, DateTime LocalTime)
        {
            var today = LocalTime.Date;

            // Calculate the age.
            var age = today.Year - birthDate.Year;

            // Go back to the year in which the person was born in case of a leap year
            if (birthDate.Date > today.AddYears(-age)) age--;
            return age;
        }
        public static List<MemberSummary> GetMemberSummaryByMshipLength(U3ADbContext dbc, Term term)
        {
            var today = dbc.GetLocalTime().Date;
            var result = dbc.Person
                .Where(p => p.DateJoined != null && p.FinancialTo >= term.Year).AsEnumerable()
                .GroupBy(p => GetAge(p.DateJoined.Value, dbc.GetLocalTime()))
                .Select(g => new MemberSummary
                {
                    Year = g.Key,
                    Group = GetJoiningYearRange(g.Key),
                    Count = g.Count()
                })
                .OrderBy(r => r.Year).ToList();
            return result;
        }
        public static List<MemberSummary> GetMemberSummaryByDOB(U3ADbContext dbc, Term term)
        {
            var today = dbc.GetLocalTime().Date;
            var result = dbc.Person
                .Where(p => p.BirthDate != null && p.FinancialTo >= term.Year).AsEnumerable()
                .GroupBy(p => GetAge(p.BirthDate.Value, dbc.GetLocalTime()))
                .Select(g => new MemberSummary
                {
                    Year = g.Key,
                    Group = GetBirthDateRange(g.Key),
                    Count = g.Count()
                })
                .OrderBy(r => r.Group).ToList();
            return result;
        }

        private static string GetBirthDateRange(int years)
        {
            if (years <= 50)
            {
                return " <= 50 years";
            }
            else if (years >= 51 && years <= 55)
            {
                return " 51-55 years";
            }
            else if (years >= 56 && years <= 60)
            {
                return " 56-60 years";
            }
            else if (years >= 61 && years <= 65)
            {
                return " 61-65 years";
            }
            else if (years >= 66 && years <= 70)
            {
                return " 66-70 years";
            }
            else if (years >= 71 && years <= 75)
            {
                return " 71-75 years";
            }
            else if (years >= 76 && years <= 80)
            {
                return " 76-80 years";
            }
            else if (years >= 81 && years <= 85)
            {
                return " 81-85 years";
            }
            else if (years >= 86 && years <= 90)
            {
                return " 86-90 years";
            }
            else if (years >= 91 && years <= 95)
            {
                return " 91-95 years";
            }
            else if (years >= 96 && years <= 100)
            {
                return " 96-100 years";
            }
            else
            {
                return "100+ years";
            }
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
                return "20+ years";
            }
        }


        public static async Task<List<MemberSummary>> GetNewMemberSummaryByMonth(U3ADbContext dbc)
        {
            var today = dbc.GetLocalTime();
            var endDate = today.AddMonths(1).AddDays(-today.Day);
            var startDate = endDate.AddMonths(-25).AddDays(1);
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

            var allMonths = Enumerable.Range(0, 25)
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

            var emptyMonths = new List<MemberSummary>();
            while (startDate < endDate)
            {
                if (!result.Any(x => x.Year == startDate.Year && x.Month == startDate.Month))
                {
                    emptyMonths.Add(new()
                    {
                        Count = 0,
                        Year = startDate.Year,
                        Month = startDate.Month,
                        Group = "Male"
                    });
                    emptyMonths.Add(new()
                    {
                        Count = 0,
                        Year = startDate.Year,
                        Month = startDate.Month,
                        Group = "Female"
                    });
                }
                startDate = startDate.AddMonths(1);
            }
            result.AddRange(emptyMonths);
            return result.OrderBy(x => x.Year).ThenBy(x => x.Month).ToList();
        }


    }
}