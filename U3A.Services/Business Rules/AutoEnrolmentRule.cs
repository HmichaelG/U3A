using DevExpress.Blazor;
using DevExpress.Utils.Serializing;
using Eway.Rapid.Abstractions.Response;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text;
using Twilio.Rest.Trunking.V1;
using U3A.Database;
using U3A.Model;
using Serilog;
using System.Diagnostics.Eventing.Reader;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace U3A.BusinessRules
{
    public static partial class BusinessRule
    {

        public static List<string> AutoEnrolments { get; set; }

        // Are we in the period prior to the random allocation date.
        public static bool IsPreRandomAllocationDay(Term currentEnrolmentTerm,
                                            SystemSettings settings, DateTime Today)
        {
            DateTime allocationDate = GetThisTermAllocationDay(currentEnrolmentTerm, settings);
            return IsPreRandomCutoffDate(currentEnrolmentTerm, settings, allocationDate, Today);
        }

        // Are we in the period prior to the random allocation email send date.
        public static bool IsPreRandomAllocationEmailDay(Term currentEnrolmentTerm,
                                            SystemSettings settings, DateTime Today)
        {
            DateTime allocationDate = GetThisTermAllocationDay(currentEnrolmentTerm, settings);
            // add the review period
            allocationDate = allocationDate.AddDays(constants.RANDOM_ALLOCATION_PREVIEW);
            return IsPreRandomCutoffDate(currentEnrolmentTerm, settings, allocationDate, Today);
        }

        public static bool IsEnrolmentBlackoutPeriod(SystemSettings settings)
                      => settings != null && DateTime.UtcNow < settings.EnrolmentBlackoutEndsUTC.GetValueOrDefault();
        private static bool IsPreRandomCutoffDate(Term currentEnrolmentTerm,
                                            SystemSettings settings,
                                            DateTime CutoffDate,
                                            DateTime today)
        {
            bool result = false;
            var autoEnrollOccurrence = settings.AutoEnrolAllocationOccurs;
            switch (autoEnrollOccurrence)
            {
                case AutoEnrollOccurrence.Annually:
                    if (currentEnrolmentTerm.TermNumber == 1 && today < CutoffDate) result = true;
                    break;
                case AutoEnrollOccurrence.Semester:
                    if (currentEnrolmentTerm.TermNumber == 1 && today < CutoffDate) result = true;
                    if (currentEnrolmentTerm.TermNumber == 3 && today < CutoffDate) result = true;
                    break;
                case AutoEnrollOccurrence.Term:
                    if (today < CutoffDate) result = true; break;
                default: result = false; break;
            }
            return result;
        }

        public static bool IsRandomAllocationTerm(Term currentEnrolmentTerm,
                                            SystemSettings settings)
        {
            bool result = false;
            var autoEnrollOccurrence = settings.AutoEnrolAllocationOccurs;
            switch (autoEnrollOccurrence)
            {
                case AutoEnrollOccurrence.Annually:
                    if (currentEnrolmentTerm.TermNumber == 1) result = true;
                    break;
                case AutoEnrollOccurrence.Semester:
                    if (currentEnrolmentTerm.TermNumber == 1) result = true;
                    if (currentEnrolmentTerm.TermNumber == 3) result = true;
                    break;
                case AutoEnrollOccurrence.Term:
                    result = true; break;
                default: result = false; break;
            }
            return result;
        }

        // Return the date random allocation will occur, if it occurs this term.
        public static DateTime GetThisTermAllocationDay(Term currentEnrolmentTerm, SystemSettings settings)
        {
            var week = settings.AutoEnrolAllocationWeek;
            var onDay = (DayOfWeek)settings.AutoEnrolAllocationDay;
            var allocationDate = currentEnrolmentTerm.StartDate.Date.AddDays(7 * week);
            while (allocationDate.DayOfWeek != onDay)
            {
                allocationDate = allocationDate.AddDays(1);
            }
            return allocationDate;
        }

        // The day members are notified of allocation
        public static DateTime GetThisTermAllocationMailoutDay(Term currentEnrolmentTerm, SystemSettings settings)
        {
            DateTime allocationDate = GetThisTermAllocationDay(currentEnrolmentTerm, settings);
            return allocationDate.AddDays(constants.RANDOM_ALLOCATION_PREVIEW);
        }

        const double COURSE_NOT_RANKED_VALUE = 0.00000002;
        const double COURSE_RANK_NOT_REQD_VALUE = 0.00000001;
        public static async Task AutoEnrolParticipantsAsync(U3ADbContext dbc, Term SelectedTerm,
                              bool IsClassAllocationDone,
                              bool ForceEmailQueue,
                              DateTime? EmailDate = null)
        {

            using (LogContext.PushProperty("AutoEnrolParticipants", dbc.GetLocalTime().ToString("dd-MM-yyyy hh:mm:ss tt")))
            using (LogContext.PushProperty("Tenant", dbc.TenantInfo.Identifier))
            {
                {
                    Log.Information("Auto-Enrolment Allocation log as at {DateTime:dd-MM-yyyy hh:mm:ss tt}", dbc.GetLocalTime());
                    Log.Information("===============================================================");
                    // Do part paid first
                    await WaitListPartPaidMembers(dbc, SelectedTerm);
                    await BusinessRule.CreateEnrolmentSendMailAsync(dbc, EmailDate);
                    await dbc.SaveChangesAsync();

                    // and everybody else
                    var today = dbc.GetLocalTime().Date;
                    AutoEnrolments = new List<string>();
                    List<Enrolment> enrolmentsToProcess;
                    List<Person> CourseLeaders;
                    foreach (var kvp in (await GetRankedCourses(dbc, SelectedTerm)).Reverse())
                    {
                        int enrolledCount = 0;
                        var key = kvp.Key;
                        var course = kvp.Value;
                        if (key.Item1 != double.MinValue)
                        {
                            Log.Information("");
                            Log.Information("Rank: {Rank}\tMaxStudents: {MaxStudents}\t{CourseName}",
                                               (key.Item1 == COURSE_NOT_RANKED_VALUE)
                                                    ? "Not Weekly"
                                                    : (key.Item1 == COURSE_RANK_NOT_REQD_VALUE)
                                                        ? "Not Req'd"
                                                        : key.Item1.ToString("00000.00000000"),
                                                key.Item2.ToString("0000"), course.Name);
                        }

                        //A "new member" is one that has never been enrolled in a course
                        var peoplePreviouslyEnrolled = await dbc.Enrolment.AsNoTracking()
                                                    .Where(x => !x.IsWaitlisted)
                                                    .Select(x => x.PersonID)
                                                    .Distinct()
                                                    .ToListAsync();
                        CourseLeaders = new();
                        foreach (var c in course.Classes)
                        {
                            if (c.Leader != null && !CourseLeaders.Contains(c.Leader)) { CourseLeaders.Add(c.Leader); }
                            if (c.Leader2 != null && !CourseLeaders.Contains(c.Leader2)) { CourseLeaders.Add(c.Leader2); }
                            if (c.Leader3 != null && !CourseLeaders.Contains(c.Leader3)) { CourseLeaders.Add(c.Leader3); }
                        }
                        if (course.CourseParticipationTypeID == (int?)ParticipationType.SameParticipantsInAllClasses)
                        {
                            enrolmentsToProcess = await dbc.Enrolment
                                                        .Include(x => x.Course)
                                                        .Include(x => x.Term)
                                                        .Include(x => x.Person)
                                                        .Where(x => (x.TermID == SelectedTerm.ID ||
                                                                     (x.Course.AllowMultiCampsuFrom != null &&
                                                                     x.Course.AllowMultiCampsuFrom <= today.AddDays(-1)))
                                                                        && x.CourseID == course.ID
                                                                        && x.Person.DateCeased == null
                                                                        && !CourseLeaders.Contains(x.Person))
                                                        .ToListAsync();

                            enrolmentsToProcess = enrolmentsToProcess.Where(x => IsPersonFinancial(x.Person, SelectedTerm)).ToList();
                            if (enrolmentsToProcess.Any(x => x.IsWaitlisted))
                            {
                                enrolledCount = await ProcessEnrolments(dbc,
                                                   SelectedTerm,
                                                   course,
                                                   enrolmentsToProcess,
                                                   peoplePreviouslyEnrolled,
                                                   ForceEmailQueue);
                                await BusinessRule.CreateEnrolmentSendMailAsync(dbc, EmailDate);
                                await dbc.SaveChangesAsync();
                            }
                        }
                        else
                        {
                            foreach (var courseClass in course.Classes)
                            {
                                enrolmentsToProcess = await dbc.Enrolment
                                                            .Include(x => x.Course)
                                                            .Include(x => x.Term)
                                                            .Include(x => x.Person)
                                                            .Where(x => x.TermID == SelectedTerm.ID
                                                                            && x.ClassID == courseClass.ID
                                                                            && x.Person.DateCeased == null
                                                                            && !CourseLeaders.Contains(x.Person))
                                                        .ToListAsync();
                                enrolmentsToProcess = enrolmentsToProcess.Where(x => IsPersonFinancial(x.Person, SelectedTerm)).ToList();
                                if (enrolmentsToProcess.Any(x => x.IsWaitlisted))
                                {
                                    enrolledCount += await ProcessEnrolments(dbc,
                                                          SelectedTerm,
                                                          course,
                                                          enrolmentsToProcess,
                                                          peoplePreviouslyEnrolled,
                                                          ForceEmailQueue);
                                    await BusinessRule.CreateEnrolmentSendMailAsync(dbc, EmailDate);
                                    await dbc.SaveChangesAsync();
                                }
                            }
                        }
                        if (enrolledCount == 0)
                        {
                            Log.Information("There are no enrolments to process for this course.");
                        }
                    }
                    var term = await dbc.Term.FindAsync(SelectedTerm.ID);
                    await SetClassAllocationDone(dbc, term, IsClassAllocationDone);
                    await dbc.SaveChangesAsync();
                    Log.Information("");
                }
            }
        }

        private static async Task<SortedList<(double, int, Guid), Course>> GetRankedCourses(U3ADbContext dbc, Term term)
        {
            // rank courses by popularity
            (double rank, int maxStudents, Guid courseID) key;
            SortedList<(double, int, Guid), Course> rankedCourses = new();
            var enrolments = await dbc.Enrolment.AsNoTracking().Where(e => e.TermID == term.ID).ToListAsync();
            foreach (var course in await dbc.Course.AsNoTracking()
                                            .Include(x => x.Classes)
                                            .Where(x => x.Year == term.Year
                                                         && x.AllowAutoEnrol)
                                            .ToListAsync())
            {
                key = new()
                {
                    courseID = course.ID,
                    maxStudents = course.MaximumStudents,
                    rank = double.MinValue
                };
                if (course.MaximumStudents != 0)
                {
                    double classes = 1;
                    if (course.CourseParticipationTypeID == (int)ParticipationType.DifferentParticipantsInEachClass)
                    {
                        classes = course.Classes.Count;
                    }
                    double max = (double)course.MaximumStudents * classes;
                    double requests = enrolments.Where(e => e.CourseID == course.ID
                                                            && e.TermID == term.ID).Count();
                    // Calclate the rank. Set rank to MIN_VALUE if requests > 0 & <= max OR once only class
                    // MIN_VALUE courses will be logged but will not be considered popular (constrained).
                    if (requests > max)
                    {
                        key.rank = (requests / max) * requests;
                        foreach (var c in course.Classes)
                        {
                            if (c.StartDate == null && c.OccurrenceID == (int)OccurrenceType.Weekly)
                            {
                                continue;
                            }
                            key.rank = COURSE_NOT_RANKED_VALUE; break;
                        }
                    }
                    else
                    {
                        key.rank = COURSE_RANK_NOT_REQD_VALUE;
                    }
                }
                rankedCourses.Add(key, course);
            }
            return rankedCourses;
        }
        public static bool IsPersonFinancial(Person person, Term term)
        {
            var result = person.FinancialTo > term.Year
                    || (person.FinancialTo == term.Year
                        && (person.FinancialToTerm == null
                            || person.FinancialToTerm >= term.TermNumber));
            return result;
        }

        private static async Task SetClassAllocationDone(U3ADbContext dbc,
                                                        Term term,
                                                        bool IsClassAllocationDone)
        {
            var today = DateTime.UtcNow.Date; ;
            var settings = await dbc.SystemSettings.AsNoTracking()
                                    .OrderBy(x => x.ID)
                                    .FirstAsync();
            if (!IsRandomAllocationTerm(term, settings)) return;
            var allocationDate = GetThisTermAllocationDay(term, settings);
            var IsAllocationDone = IsClassAllocationDone || allocationDate >= today;
            var autoEnrollOccurrence = settings.AutoEnrolAllocationOccurs;
            IQueryable<Term> termsToUpdate;
            int[] termArray;
            switch (autoEnrollOccurrence)
            {
                case AutoEnrollOccurrence.Annually:
                    termArray = new int[] { 1, 2, 3, 4 };
                    break;
                case AutoEnrollOccurrence.Semester:
                    termArray = new int[] { term.TermNumber, term.TermNumber + 1 };
                    break;
                case AutoEnrollOccurrence.Term:
                    termArray = new int[] { term.TermNumber };
                    break;
                default: return;
            }
            foreach (var t in dbc.Term.Where(x => x.Year == term.Year))
            {
                if (termArray.Contains(t.TermNumber))
                {
                    t.IsClassAllocationFinalised = IsAllocationDone;
                    dbc.Update(t);
                }
            };
            // and for completeness...
            foreach (var t in dbc.Term.Where(x => x.Year < term.Year))
            {
                t.IsClassAllocationFinalised = IsAllocationDone;
                dbc.Update(t);
            };
        }
        private static async Task<int> ProcessEnrolments(U3ADbContext dbc,
                                    Term term,
                                    Course course,
                                    List<Enrolment> enrolments,
                                    List<Guid> PeoplePreviouslyEnrolled,
                                    bool ForceEmailQueue)
        {
            int count = 0;
            var today = DateTime.UtcNow.Date;
            var AlreadyEnrolledInCourse = new List<Guid>();
            if (course.EnforceOneStudentPerClass
                && course.CourseParticipationTypeID == (int?)ParticipationType.DifferentParticipantsInEachClass)
            {
                AlreadyEnrolledInCourse = await dbc.Enrolment.AsNoTracking().Where(x => x.CourseID == course.ID
                                                                && x.TermID == term.ID
                                                                && !x.IsWaitlisted)
                                                            .Select(x => x.PersonID).ToListAsync();
            }
            // Set the enrolment method
            var settings = await dbc.SystemSettings.AsNoTracking()
                                    .OrderBy(x => x.ID)
                                    .FirstAsync();
            if (string.IsNullOrWhiteSpace(settings.AutoEnrolRemainderMethod)) settings.AutoEnrolRemainderMethod = "Random";
            var isRandomEnrol = settings.AutoEnrolRemainderMethod.ToLower() == "random";

            // We will do a random allocation once per allocation period (annual, semester or term)
            // On completion IsClassAllocationFinalised is aet True.
            var DoRandomEnrol = !term.IsClassAllocationFinalised && isRandomEnrol &&
                                            today >= GetThisTermAllocationDay(term, settings);

            int enrolled = enrolments.Where(x => !x.IsWaitlisted).Count();
            if (enrolled >= course.MaximumStudents) { return 0; }
            int waitlisted = enrolments.Where(x => x.IsWaitlisted
                                && (!IsAlreadyEnrolledInCourse(x.PersonID, course, AlreadyEnrolledInCourse))).Count();
            // If available places is less than waitlisted then enrol everyone.
            if (waitlisted <= course.MaximumStudents - enrolled)
            {
                foreach (var e in enrolments.Where(x => x.IsWaitlisted && x.TermID == term.ID
                                && !IsAlreadyEnrolledInCourse(x.PersonID, course, AlreadyEnrolledInCourse)))
                {
                    e.IsWaitlisted = false;
                    LogEnrolment(e);
                }
                Log.Information("All students enrolled because Wautlist: {Waitlist} is less than Maximum Students: {MazStudents}", waitlisted, course.MaximumStudents);
            }
            else
            {
                int places = 0;
                // Enrol new participants first
                if (DoRandomEnrol)
                {
                    decimal percent = settings.AutoEnrolNewParticipantPercent;
                    if (percent > 0) { places = (int)(course.MaximumStudents * percent / 100); }
                    if (places > 0 && places <= course.MaximumStudents - enrolled)
                    {
                        Log.Information("Processing New Student Allocations");
                        Log.Information("----------------------------------");
                        foreach (var e in enrolments
                                            .OrderBy(x => x.Random)
                                            .Where(x => x.IsWaitlisted && x.Person.DateJoined?.Year >= term.Year - 1
                                                            && !PeoplePreviouslyEnrolled.Contains(x.PersonID)
                                                            && !IsAlreadyEnrolledInCourse(x.PersonID, course, AlreadyEnrolledInCourse))
                                            .Take(places))
                        {
                            count++;
                            Log.Information("{count:00} Joined: {Joined:dd-MMM-yyy}\tRandom: {random:00000000}\tEnrolled  {Student}", count, e.Person.DateJoined, e.Random, e.Person.FullName);
                            LogEnrolment(e);
                            e.IsWaitlisted = false;
                        }
                    }
                }
                // apply the remainder
                enrolled = enrolments.Where(x => !x.IsWaitlisted).Count();
                places = course.MaximumStudents - enrolled;
                if (places > 0)
                {
                    if (DoRandomEnrol)
                    {
                        Log.Information("Processing Random Allocations");
                        Log.Information("-----------------------------");
                        var rankedEnrolments = await GetRankedEnrolments(dbc, enrolments, term);
                        foreach (var kvp in rankedEnrolments
                                            .Where(x => x.Value.IsWaitlisted
                                                        && !IsAlreadyEnrolledInCourse(x.Value.PersonID, course, AlreadyEnrolledInCourse))
                                            .Take(places))
                        {
                            var e = kvp.Value;
                            var key = kvp.Key;
                            count++;
                            Log.Information("{count:00} Class#: {classes:000}\tRandom: {random:00000000}\tEnrolled  \t{student}", count, key.Item1, key.Item2, e.Person.FullName);
                            LogEnrolment(e);
                            e.IsWaitlisted = false;
                        }
                        foreach (var kvp in rankedEnrolments
                                            .Where(x => x.Value.IsWaitlisted))
                        {
                            var e = kvp.Value;
                            var key = kvp.Key;
                            count++;
                            Log.Information("{count:00} Class#: {classes:000}\tRandom: {random:00000000}\tWaitlisted\t{student}", count, key.Item1, key.Item2, e.Person.FullName);
                        }
                    }
                    else
                    {
                        Log.Information("Processing First-In-Wins Allocations");
                        Log.Information("------------------------------------");
                        foreach (var e in enrolments
                                            .OrderBy(x => x.Created)
                                            .Where(x => x.IsWaitlisted
                                                        && !IsAlreadyEnrolledInCourse(x.PersonID, course, AlreadyEnrolledInCourse))
                                            .Take(places))
                        {
                            Log.Information("Created: {created:dd-MM-yyyy hh:mm:ss tt}\tEnrolled  \t{student}", e.Created, e.Person.FullName);
                            LogEnrolment(e);
                            e.IsWaitlisted = false;
                        }
                        foreach (var e in enrolments
                                            .OrderBy(x => x.Created)
                                            .Where(x => x.IsWaitlisted))
                        {
                            Log.Information("Created: {created:dd-MM-yyyy hh:mm:ss tt}\tWaitlisted\t{student}", e.Created, e.Person.FullName);
                        }
                    }
                }
            }
            if (ForceEmailQueue)
            {
                foreach (var e in enrolments)
                {
                    if (dbc.Entry(e).State == EntityState.Unchanged) { dbc.Entry(e).State = EntityState.Modified; }
                }
            }
            return enrolments.Count(e => !e.IsWaitlisted);
        }

        private static async Task<SortedList<(int, long, Guid), Enrolment>> GetRankedEnrolments(U3ADbContext dbc, IEnumerable<Enrolment> enrolments, Term term)
        {
            (int courses, long random, Guid personID) key;
            SortedList<(int, long, Guid), Enrolment> rankedEnrolments = new();
            var people = enrolments.Select(x => x.PersonID).ToList();
            var courseCounts = await dbc.Enrolment
                                .Where(e => e.TermID == term.ID
                                                && people.Contains(e.PersonID)
                                                && !e.IsWaitlisted
                                                )
                                .GroupBy(g => new
                                {
                                    PersonID = g.PersonID,
                                })
                                .Select(g => new Tuple<Guid, int>(g.Key.PersonID, g.Count()))
                                .ToListAsync();
            foreach (var e in enrolments)
            {
                key = new()
                {
                    personID = e.PersonID,
                    random = e.Random,
                    courses = 0
                };
                var courseCount = courseCounts.Where(r => r.Item1 == e.PersonID).FirstOrDefault();
                if (courseCount != null) { key.courses = courseCount.Item2; }
                rankedEnrolments.Add(key, e);

            }
            return rankedEnrolments;
        }

        private static bool IsAlreadyEnrolledInCourse(Guid PersonID, Course course, List<Guid> enrolledPeopleID)
        {
            var result = false;
            if (course.CourseParticipationTypeID == (int?)ParticipationType.DifferentParticipantsInEachClass)
            {
                result = (enrolledPeopleID.Contains(PersonID)) ? true : false;
            }
            return result;
        }

        private static void LogEnrolment(Enrolment enrolment)
        {
            var person = enrolment.Person;
            var course = enrolment.Course;
            AutoEnrolments.Add($"{person.FullName} enrolled in {course.Name}.");
        }
    }
}