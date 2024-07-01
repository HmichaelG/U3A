using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections;
using System.Linq;
using U3A.Database;
using U3A.Model;

namespace U3A.BusinessRules
{
    public static partial class BusinessRule
    {
        public static async Task<int> AssignLifeComplimentaryMembership(U3ADbContext dbc, int FinancialTo, DateTime Now)
        {
            int result = 0;
            var persons = await BusinessRule.SelectablePersonsIncludeUnfinancialAsync(dbc);
            foreach (var p in persons.Where(x => x.IsLifeMember))
            {
                result += await CreateCashReceipt(dbc, p, "Complimentary Life Membership", FinancialTo, Now);
            }
            await dbc.SaveChangesAsync();
            return result;
        }
        public static async Task<int> AssignCommitteeComplimentaryMembership(U3ADbContext dbc, int FinancialTo, DateTime Now)
        {
            int result = 0;
            var persons = await BusinessRule.SelectablePersonsIncludeUnfinancialAsync(dbc);
            foreach (var p in persons.Where(x => x.IsCommitteeMember))
            {
                result += await CreateCashReceipt(dbc, p, "Complimentary Committee Membership", FinancialTo, Now);
            }
            await dbc.SaveChangesAsync();
            return result;
        }
        public static async Task<Tuple<int, int>> AssignCourseLeaderComplimentaryMembership(U3ADbContext dbc, int FinancialTo, DateTime Now)
        {
            var added = 0;
            var removed = 0;
            var reason = "Complimentary Course Leader Membership";
            var settings = await dbc.SystemSettings.FirstOrDefaultAsync();
            var maxComplimentaryCourses = settings.LeaderMaxComplimentaryCourses;
            if (settings != null)
            {
                var persons = await BusinessRule.SelectablePersonsIncludeUnfinancialAsync(dbc);
                foreach (var p in persons.Where(x => BusinessRule.IsCourseLeader(dbc,x,Now)))
                {
                    if (maxComplimentaryCourses == 0 || GetCourseCount(dbc, p, FinancialTo) <= maxComplimentaryCourses)
                    {
                        added += await CreateCashReceipt(dbc, p, reason, FinancialTo, Now);
                    }
                    else
                    {
                        var q = dbc.Receipt.Where(x => x.FinancialTo == FinancialTo
                                                                && x.PersonID == p.ID
                                                                && x.Description == reason);
                        if (q.Any())
                        {
                            dbc.RemoveRange(q);
                            removed = q.Count();
                        }
                    }
                }
                await dbc.SaveChangesAsync();
            }
            return Tuple.Create(added, removed);
        }

        private static int GetCourseCount(U3ADbContext dbc, Person person, int FinancialTo)
        {
            int result = 0;
            foreach (var e in dbc.Enrolment.Where(x => x.PersonID == person.ID)
                                     .Include(x => x.Course).ThenInclude(x => x.Classes)
                                     .Include(x => x.Term).AsEnumerable()
                                     .Where(x => x.Term.Year == FinancialTo && !x.IsWaitlisted)
                                     .DistinctBy(x => x.CourseID))
            {
                var c = e.Course;
                if (!c.ExcludeFromLeaderComplimentaryCount)
                {
                    if (!c.Classes.Any(x => x.LeaderID == person.ID || x.Leader2ID == person.ID || x.Leader3ID == person.ID))
                    {
                        result++;
                    }
                }
            };
            return result;
        }
        public static async Task<int> AssignVolunteerComplimentaryMembership(U3ADbContext dbc, int FinancialTo, string Activity, DateTime Now)
        {
            var result = 0;
            var activities = await dbc.Volunteer.Where(x => x.Activity == Activity).ToListAsync();
            foreach (var a in activities)
            {
                var person = await dbc.Person.FindAsync(a.PersonID);
                if (person != null)
                {
                    result += await CreateCashReceipt(dbc, person, $"Complimentary Membership - {Activity}", FinancialTo, Now);
                }
            }
            await dbc.SaveChangesAsync();
            return result;
        }
        public static async Task<int> AssignOtherComplimentaryMembership(U3ADbContext dbc,
                                            int FinancialTo, IEnumerable<Person> people, string Reason, DateTime Now)
        {
            var result = 0;
            foreach (var p in people)
            {
                var person = await dbc.Person.FindAsync(p.ID);
                if (person != null)
                {
                    result += await CreateCashReceipt(dbc, person, $"Complimentary Membership - {Reason}", FinancialTo, Now);
                }
            }
            await dbc.SaveChangesAsync();
            return result;
        }
        public static async Task<int> RemoveOtherComplimentaryMembership(U3ADbContext dbc,
                                            int FinancialTo, IEnumerable<Person> people)
        {
            var result = 0;
            foreach (var p in people)
            {
                var person = await dbc.Person.FindAsync(p.ID);
                if (person != null)
                {
                    await dbc.Receipt.Where(x => x.PersonID == person.ID
                                        && x.FinancialTo >= FinancialTo
                                        && x.Amount == 0).ExecuteDeleteAsync();
                    if (person.FinancialTo >= FinancialTo) { person.FinancialTo = FinancialTo-1; }
                    if (person.FinancialTo < constants.START_OF_TIME) { person.FinancialTo = constants.START_OF_TIME; }
                    result++;
                }
            }
            await dbc.SaveChangesAsync();
            return result;
        }

        private static async Task<int> CreateCashReceipt(U3ADbContext dbc, Person person, string Description, int FinancialTo , DateTime Now)
        {
            int result = 0;
            var isOnFile = await dbc.Receipt.AnyAsync(x => x.PersonID == person.ID
                                && x.FinancialTo >= FinancialTo
                                && x.Amount == 0);
            if (!isOnFile)
            {
                result = 1;
                var receipt = new Receipt()
                {
                    Description = Description,
                    FinancialTo = FinancialTo,
                    Amount = 0,
                    Date = Now.Date,
                    PersonID = person.ID,
                    ProcessingYear = FinancialTo,
                };
                if (person.DateJoined != null)
                {
                    receipt.DateJoined = person.DateJoined.Value;
                    person.PreviousDateJoined = person.DateJoined.Value;
                }
                if (FinancialTo > person.FinancialTo)
                {
                    person.PreviousFinancialTo = person.FinancialTo;
                    person.FinancialTo = FinancialTo;
                }
                dbc.Update(person);
                dbc.Add(receipt);
            }
            return result;
        }
    }
}
