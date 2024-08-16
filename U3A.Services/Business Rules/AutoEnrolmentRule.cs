using DevExpress.Blazor;
using DevExpress.Utils.Serializing;
using Eway.Rapid.Abstractions.Response;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text;
using Twilio.Rest.Trunking.V1;
using U3A.Database;
using U3A.Model;

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

        public static async Task AutoEnrolParticipantsAsync(U3ADbContext dbc, Term SelectedTerm,
                              bool IsClassAllocationDone,
                              bool ForceEmailQueue,
                              DateTime? EmailDate = null)
        {
            // Do part paid first
            await WaitListPartPaidMembers(dbc, SelectedTerm);
            await BusinessRule.CreateEnrolmentSendMailAsync(dbc, EmailDate);
            await dbc.SaveChangesAsync();

            // and everybody else
            var today = dbc.GetLocalTime().Date;
            AutoEnrolments = new List<string>();
            List<Enrolment> enrolmentsToProcess;
            List<Person> CourseLeaders;
            var peoplePreviouslyEnrolled = await dbc.Enrolment.AsNoTracking()
                                        .Select(x => x.PersonID).Distinct()
                                        .ToListAsync();
            foreach (var course in await GetRankedCourses(dbc, SelectedTerm))
            {
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
                        await ProcessEnrolments(dbc,
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
                            await ProcessEnrolments(dbc,
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
            }
            var term = await dbc.Term.FindAsync(SelectedTerm.ID);
            await SetClassAllocationDone(dbc, term, IsClassAllocationDone);
            await dbc.SaveChangesAsync();
        }

        private static async Task<IEnumerable<Course>> GetRankedCourses(U3ADbContext dbc, Term term)
        {
            // rank courses by popularity
            (double rank, int maxStudents, Guid courseID) key;
            SortedList<(double, int,Guid), Course> rankedCourses = new();
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
                    rank = 0
                };
                if (course.MaximumStudents != 0)
                {
                    double classes = 1;
                    if (course.CourseParticipationTypeID == (int)ParticipationType.DifferentParticipantsInEachClass)
                    {
                        classes = course.Classes.Count;
                    }
                    double requests = enrolments.Where(e => e.CourseID == course.ID).Count();
                    key.rank = requests / ((double)course.MaximumStudents * classes);
                }
                rankedCourses.Add(key, course);
            }
            return rankedCourses.Values.Reverse();
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
        private static async Task ProcessEnrolments(U3ADbContext dbc,
                                    Term term,
                                    Course course,
                                    List<Enrolment> enrolments,
                                    List<Guid> PeoplePreviouslyEnrolled,
                                    bool ForceEmailQueue)
        {
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
            if (enrolled >= course.MaximumStudents) { return; }
            int waitlisted = enrolments.Where(x => x.IsWaitlisted
                                && (!IsAlreadyEnrolledInCourse(x.PersonID, course, AlreadyEnrolledInCourse))).Count();
            // If available places is less than waitlisted then enrol everyone.
            if (waitlisted <= course.MaximumStudents - enrolled)
            {
                foreach (var e in enrolments.Where(x => x.IsWaitlisted
                                && !IsAlreadyEnrolledInCourse(x.PersonID, course, AlreadyEnrolledInCourse)))
                {
                    e.IsWaitlisted = false;
                    LogEnrolment(e);
                }
            }
            else
            {
                int places = 0;
                // Enrol new participants first
                if (DoRandomEnrol)
                {
                    decimal percent = settings.AutoEnrolNewParticipantPercent;
                    if (percent > 0) { places = (int)(enrolments.Count * percent / 100); }
                    if (places > 0 && places <= course.MaximumStudents - enrolled)
                    {
                        foreach (var e in enrolments
                                            .OrderBy(x => x.Random)
                                            .Where(x => x.IsWaitlisted
                                                            && !PeoplePreviouslyEnrolled.Contains(x.PersonID)
                                                            && !IsAlreadyEnrolledInCourse(x.PersonID, course, AlreadyEnrolledInCourse))
                                            .Take(places))
                        {
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
                        foreach (var e in (await GetRankedEnrolments(dbc, enrolments, term))
                                            .Where(x => x.IsWaitlisted
                                                        && !IsAlreadyEnrolledInCourse(x.PersonID, course, AlreadyEnrolledInCourse))
                                            .Take(places))
                        {
                            LogEnrolment(e);
                            e.IsWaitlisted = false;
                        }
                    }
                    else
                    {
                        foreach (var e in enrolments
                                            .OrderBy(x => x.Created)
                                            .Where(x => x.IsWaitlisted
                                                        && !IsAlreadyEnrolledInCourse(x.PersonID, course, AlreadyEnrolledInCourse))
                                            .Take(places))
                        {
                            LogEnrolment(e);
                            e.IsWaitlisted = false;
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
        }

        private static async Task<List<Enrolment>> GetRankedEnrolments(U3ADbContext dbc, IEnumerable<Enrolment> enrolments, Term term)
        {
            (int courses, long random, Guid personID) key;
            SortedList< (int, long, Guid),Enrolment > rankedEnrolments = new();
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
                                .Select(g => new Tuple<Guid,int>(g.Key.PersonID, g.Count()))
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
                rankedEnrolments.Add(key,e);
                
            }
            return rankedEnrolments.Values.ToList();
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