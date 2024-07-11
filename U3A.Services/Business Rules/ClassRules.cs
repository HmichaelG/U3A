using DevExpress.Blazor;
using DevExpress.Blazor.Internal;
using DevExpress.Blazor.Popup.Internal;
using Eway.Rapid.Abstractions.Response;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using U3A.Database;
using U3A.Database.Migrations.U3ADbContextSeedMigrations;
using U3A.Model;
using U3A.Services;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Json;
using System.Text.Json.Serialization;
using System.Runtime.InteropServices;
using Twilio.Rest.Trunking.V1;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.Components;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Serilog;

namespace U3A.BusinessRules
{
    public static partial class BusinessRule
    {
        public static async Task<List<Class>> SelectableClassesInYearAsync(U3ADbContext dbc, int Year)
        {
            return await dbc.Class.AsNoTracking()
                            .Include(x => x.OnDay)
                            .Include(x => x.Course).ThenInclude(x => x.CourseType)
                            .Include(x => x.Course).ThenInclude(x => x.CourseParticipationType)
                            .Include(x => x.Leader)
                            .Include(x => x.Leader2)
                            .Include(x => x.Leader3)
                            .Include(x => x.Occurrence)
                            .Include(x => x.Venue)
                            .Where(x => x.Course.Year == Year)
                            .OrderBy(x => x.Course.Name).ThenBy(x => x.StartTime).ToListAsync();
        }
        public static async Task<List<Class>> SelectableClassesAsync(U3ADbContext dbc, Term term)
        {
            return dbc.Class.AsNoTracking()
                            .Include(x => x.OnDay)
                            .Include(x => x.Course)
                            .Include(x => x.Leader)
                            .Include(x => x.Leader2)
                            .Include(x => x.Leader3)
                            .Include(x => x.Occurrence)
                            .Include(x => x.Venue)
                            .Where(x => x.Course.Year == term.Year).AsEnumerable()
                            .Where(x => IsClassInTerm(x, term.TermNumber))
                            .OrderBy(x => x.Course.Name).ThenBy(x => x.StartTime).ToList();
        }
        public static async Task<List<Class>> SchedulledClassesAsync(U3ADbContext dbc, Term term)
        {
            // OccurenceID == 99 is an Unscheduled class
            var classes = await dbc.Class.AsNoTracking()
                            .Include(x => x.OnDay)
                            .Include(x => x.Course)
                            .Include(x => x.Occurrence)
                            .Include(x => x.Venue)
                            .Where(x => x.Course.Year == term.Year
                                            && (!x.Course.IsOffScheduleActivity)
                                            && x.OccurrenceID != 999)
                            .OrderBy(x => x.OnDayID).ThenBy(x => x.StartTime).ToListAsync();
            return classes;
        }
        public static async Task<List<Class>> SchedulledClassesWithCourseEnrolmentsAsync(U3ADbContext dbc, Term term)
        {
            // OccurenceID == 99 is an Unscheduled class
            var classes = await dbc.Class.AsNoTracking()
                            .Include(x => x.OnDay)
                            .Include(x => x.Course).ThenInclude(x => x.Enrolments)
                            .Include(x => x.Leader)
                            .Include(x => x.Occurrence)
                            .Include(x => x.Venue)
                            .Where(x => x.Course.Year == term.Year && x.OccurrenceID != 999)
                            .OrderBy(x => x.OnDayID).ThenBy(x => x.StartTime).ToListAsync();
            return classes;
        }

        private static IQueryable<Class> GetSameParticipantClasses(U3ADbContext dbc,
                                                Term term, bool ExludeOffScheduleActivities, DateTime? LastScheduleUpdate)
        {
            return dbc.Class
                            .Include(x => x.OnDay)
                            .Include(x => x.Course).ThenInclude(x => x.CourseType)
                            .Include(x => x.Course)
                            .Include(x => x.Leader)
                            .Include(x => x.Leader2)
                            .Include(x => x.Leader3)
                            .Include(x => x.Occurrence)
                            .Include(x => x.Venue)
                            .Where(x => x.Course.Year == term.Year
                                            && (LastScheduleUpdate == null || x.UpdatedOn > LastScheduleUpdate.Value)
                                            && x.Course.CourseParticipationTypeID == (int)ParticipationType.SameParticipantsInAllClasses
                                            && (!ExludeOffScheduleActivities || (ExludeOffScheduleActivities
                                            && !x.Course.IsOffScheduleActivity)));
        }
        private static IQueryable<Class> GetDifferentParticipantClasses(U3ADbContext dbc,
                                            Term term, bool ExludeOffScheduleActivities, DateTime? LastScheduleUpdate)
        {
            return dbc.Class.AsNoTracking()
                            .Include(x => x.OnDay)
                            .Include(x => x.Course).ThenInclude(x => x.CourseType)
                            .Include(x => x.Leader)
                            .Include(x => x.Leader2)
                            .Include(x => x.Leader3)
                            .Include(x => x.Occurrence)
                            .Include(x => x.Venue)
                            .Where(x => x.Course.Year == term.Year
                                        && (LastScheduleUpdate == null || x.UpdatedOn > LastScheduleUpdate.Value)
                                        && x.Course.CourseParticipationTypeID == (int)ParticipationType.DifferentParticipantsInEachClass
                                        && (!ExludeOffScheduleActivities || (ExludeOffScheduleActivities
                                        && !x.Course.IsOffScheduleActivity)));
        }


        /// <summary>
        /// Get available classes for the current year
        /// </summary>
        /// <param name="dbc"></param>
        /// <param name="term"></param>
        /// <returns></returns>
        public static async Task<List<Class>> GetClassDetailsAsync(U3ADbContext dbc,
            Term term, SystemSettings settings, bool ExludeOffScheduleActivities = false, DateTime? LastScheduleUpdate = null)
        {
            var enrolments = await dbc.Enrolment
                                        .Include(x => x.Term)
                                        .Include(x => x.Person)
                                        .Where(x => x.Term.Year == term.Year).ToListAsync();
            var terms = await dbc.Term.AsNoTracking().ToListAsync();
            var defaultTerm = dbc.Term.AsNoTracking().FirstOrDefault(x => x.IsDefaultTerm);
            var classes = (await GetSameParticipantClasses(dbc, term, ExludeOffScheduleActivities, LastScheduleUpdate)
                            .ToListAsync());
            Parallel.ForEach(classes, c =>
                {
                    c.Course.Enrolments = enrolments
                             .Where(x => x.CourseID == c.CourseID
                                         && x.Term.TermNumber == GetNextTermOffered(c,defaultTerm.TermNumber)
                             ).ToList();
                }
            );
            var diffParticipants = await GetDifferentParticipantClasses(dbc, term, ExludeOffScheduleActivities, LastScheduleUpdate)
                            .ToListAsync();
            Parallel.ForEach(diffParticipants, c =>
                {
                    c.Enrolments = enrolments
                                   .Where(x => x.ClassID == c.ID
                                         && x.Term.TermNumber == GetNextTermOffered(c, defaultTerm.TermNumber)
                                   ).ToList();
                }
            );
            classes.AddRange(diffParticipants);

            var totalClasses = classes.Count();
            classes = classes
                .OrderBy(x => x.Course.Name)
                .Where(x => IsClassInRemainingYear(dbc, x, term, defaultTerm, terms)).ToList();
            var TotalClassesRemainingInYear = classes.Count();
            classes = classes.Where(x => IsClassInReportingPeriod(settings.ClassScheduleDisplayPeriod, x, term)).ToList();
            var TotalClassesInReportingPeriod = classes.Count();
            foreach (var c in classes)
            {
                AssignClassTerm(c, terms, term);
                AssignClassContacts(c, term, settings);
                AssignClassCounts(term, c);
            }
            classes = GetClassSummaries(classes,dbc.GetLocalTime()).ToList();
            Log.Information("");
            Log.Information("{p1} Total classes retrieved", totalClasses);
            Log.Information("{p1} Total classes remaing in year", TotalClassesRemainingInYear);
            Log.Information("{p1} Total classes in reporting period", TotalClassesRemainingInYear);

            classes = EnsureOneClassOnlyForSameParticipantsInEachClass(dbc, classes)
                        .OrderBy(x => x.OnDayID).ThenBy(x => x.Course.Name).ToList();

            Log.Information("{p1} Unique classes returned", TotalClassesRemainingInYear);
            return classes;
        }

        static (int sameInClasses, int differentInClasses) GetEnrolmentTotals(IEnumerable<Class> classes)
        {
            var same = 0;
            var diff = 0;
            foreach (var c in classes)
            {
                same += c.Course.Enrolments.Count();
                diff += c.Enrolments.Count();
            }
            return (same, diff);
        }

        private static void AssignClassTerm(Class c, IEnumerable<Term> terms, Term term)
        {
            c.TermNumber = GetRequiredTerm(term.TermNumber, c);
            Parallel.ForEach(c.Course.Enrolments, e =>
            {
                e.Term = terms.FirstOrDefault(x => x.ID == e.TermID);
            });
            Parallel.ForEach(c.Enrolments, e =>
            {
                e.Term = terms.FirstOrDefault(x => x.ID == e.TermID);
            });
        }
        static IEnumerable<Class> GetClassSummaries(IEnumerable<Class> classes, DateTime localTime)
        {
            foreach (var c in classes)
            {
                c.Course.Classes.Add(c);
            }
            foreach (var c in classes.Where(x => x.Course.CourseParticipationTypeID == (int)ParticipationType.SameParticipantsInAllClasses))
            {
                var course = c.Course;
                course.ClassSummaries.Clear();
                foreach (var thisClass in course.Classes
                    .Where(x => x.StartDate >= localTime.Date)
                    .OrderBy(x => x.StartDate).ThenBy(x => x.StartTime))
                {
                    if (!course.ClassSummaries.Contains(thisClass.ClassDetail))
                    { course.ClassSummaries.Add(thisClass.ClassDetail); }
                }
            }
            return classes;
        }


        public static List<Class> GetClassDetails(U3ADbContext dbc,
                                            Term term,
                                            SystemSettings settings, bool ExludeOffScheduleActivities = false)
        {
            Task<List<Class>> syncTask = Task.Run(async () =>
            {
                return await GetClassDetailsAsync(dbc, term, settings, ExludeOffScheduleActivities);
            });
            syncTask.Wait();
            return syncTask.Result;
        }
        public static int GetRequiredTerm(int termNumber, Class c)
        {
            int Result = termNumber - 1;
            while (true)
            {
                Result++;
                if (Result == 1 && c.OfferedTerm1) return Result;
                if (Result == 2 && c.OfferedTerm2) return Result;
                if (Result == 3 && c.OfferedTerm3) return Result;
                if (Result == 4 && c.OfferedTerm4) return Result;
                if (Result > 4) { return termNumber; }
            }
        }
        private static IEnumerable<Class> EnsureOneClassOnlyForSameParticipantsInEachClass(U3ADbContext dbc, IEnumerable<Class> classes)
        {
            var result = new List<Class>();
            var tempStore = new Dictionary<Guid, Class>();
            Guid key;
            foreach (var c in classes
                        .OrderBy(c => c.StartDate).ThenBy(c => c.StartTime))
            {
                if (c.Course.CourseParticipationTypeID == (int)ParticipationType.SameParticipantsInAllClasses)
                {
                    key = c.Course.ID;
                    Class thisClass;
                    if (!tempStore.ContainsKey(key))
                    {
                        c.Course.Classes.Clear();
                        c.Course.Classes.AddRange(dbc.Class
                            .Include(x => x.Course)
                            .Include(x => x.Occurrence)
                            .Include(x => x.OnDay)
                            .Include(x => x.Venue)
                            .Where(x => x.Course.ID == c.CourseID).ToList());
                        bool t1 = false, t2 = false, t3 = false, t4 = false;
                        foreach (var c2 in c.Course.Classes)
                        {
                            if (c2.OfferedTerm1) t1 = true;
                            if (c2.OfferedTerm2) t2 = true;
                            if (c2.OfferedTerm3) t3 = true;
                            if (c2.OfferedTerm4) t4 = true;
                        }
                        if (!c.OfferedTerm1) c.OfferedTerm1 = t1;
                        c.OfferedTerm2 = t2;
                        c.OfferedTerm3 = t3;
                        c.OfferedTerm4 = t4;
                        tempStore.Add(key, c);
                    }
                }
                else { result.Add(c); }
            }
            result.AddRange(tempStore.Values);
            return result;
        }

        public static bool IsClassEndDateInInterTermPeriod(U3ADbContext dbc, Class c, Term t, DateTime startDate, DateTime endDate)
        {
            var d = BusinessRule.GetClassEndDate(c, t);
            return (d < endDate) && (d > startDate);
        }

        public static void AssignClassContacts(Class c, Term term, SystemSettings settings)
        {
            c.CourseContacts = new();
            List<Person> clerks;
            var course = c.Course;
            var contactOrder = (course.CourseContactOrder == null)
                ? settings.CourseContactOrder
                : course.CourseContactOrder;
            if (c.Course.CourseParticipationTypeID == (int?)ParticipationType.SameParticipantsInAllClasses)
            {
                clerks = course.Enrolments.Where(x => x.CourseID == course.ID &&
                                                  x.TermID == term.ID &&
                                                  x.IsCourseClerk && !x.IsWaitlisted)
                                                  .Select(x => x.Person).OrderBy(x => x?.FullNameAlpha).ToList();
            }
            else
            {
                clerks = course.Enrolments.Where(x => x.CourseID == course.ID && x.ClassID == c.ID &&
                                                x.TermID == term.ID &&
                                                x.IsCourseClerk && !x.IsWaitlisted)
                                                .Select(x => x.Person).OrderBy(x => x?.FullNameAlpha).ToList();
            }
            c.Clerks.AddRange(clerks);
            if (contactOrder == CourseContactOrder.LeadersThenClerks)
            {
                AddContact(c, CourseContactType.Leader, c.Leader);
                AddContact(c, CourseContactType.Leader, c.Leader2);
                AddContact(c, CourseContactType.Leader, c.Leader3);
                foreach (var clerk in clerks.OrderBy(x => x.FullNameAlpha))
                {
                    AddContact(c, CourseContactType.Clerk, clerk);
                }
            }
            else
            {
                foreach (var clerk in clerks)
                {
                    AddContact(c, CourseContactType.Clerk, clerk);
                }
                if (c.CourseContacts.Count <= 0)
                {
                    if (c.CourseContacts.Count <= 0) { AddContact(c, CourseContactType.Leader, c.Leader); }
                    if (c.CourseContacts.Count <= 0) { AddContact(c, CourseContactType.Leader, c.Leader2); }
                    if (c.CourseContacts.Count <= 0) { AddContact(c, CourseContactType.Leader, c.Leader3); }
                }
            }
        }

        private static void AddContact(Class c, CourseContactType contactType, Person? person)
        {
            if (person == null || person.SilentContact == SilentContact.Both)
            {
                return;
            }
            else
            {
                c.CourseContacts.Add(new CourseContact() { ContactType = contactType, Person = person });
            }
        }
        public static void AssignClassCounts(Term term, Class c)
        {
            var termOffered = GetNextTermOffered(c, term.TermNumber);
            double maxStudents = c.Course.MaximumStudents; ;
            c.ParticipationRate = 0;
            if (c.Course.CourseParticipationTypeID == (int?)ParticipationType.SameParticipantsInAllClasses)
            {
                c.TotalActiveStudents = c.Course.Enrolments
                                        .Where(e => !e.IsWaitlisted && e.Term?.TermNumber == termOffered).Count();
                c.TotalWaitlistedStudents = c.Course.Enrolments
                                        .Where(e => e.IsWaitlisted && e.Term?.TermNumber == termOffered).Count();
            }
            else
            {
                c.TotalActiveStudents = c.Enrolments
                                            .Where(e => !e.IsWaitlisted && e.Term?.TermNumber == termOffered).Count();
                c.TotalWaitlistedStudents = c.Enrolments
                                            .Where(e => e.IsWaitlisted && e.Term?.TermNumber == termOffered).Count();
            }
            if (maxStudents != 0) c.ParticipationRate = (double)((c.TotalActiveStudents + c.TotalWaitlistedStudents) / maxStudents);
        }

        public static void AssignClassClerks(U3ADbContext dbc, Term term, IEnumerable<Class> classes)
        {
            var enrolments = dbc.Enrolment
                                .Include(x => x.Person)
                                .Where(x => x.TermID == term.ID && x.IsCourseClerk).ToList();
            Parallel.ForEach(classes, c =>
            {
                foreach (var e in enrolments.Where(x => x.CourseID == c.Course.ID))
                {
                    if (c.Course.CourseParticipationTypeID == (int)ParticipationType.SameParticipantsInAllClasses)
                    {
                        c.Clerks.Add(e.Person);
                    }
                    else
                    {
                        if (e.ClassID != null && e.ClassID == c.ID)
                        {
                            c.Clerks.Add(e.Person);
                        }
                    }
                }
            });
        }

        public static List<Class> GetClassDetailsForStudent(IEnumerable<Class> Classes, Person Student)
        {
            ConcurrentBag<Class> result = new();
            foreach (var c in Classes)
            {
                c.IsSelected = false;
                c.IsSelectedByEnrolment = null;
                // same participants in each class
                foreach (var e in c.Course.Enrolments.Where(x => x.PersonID == Student.ID))
                {
                    c.IsSelected = true;
                    c.IsSelectedByEnrolment = e;
                    if (!result.Contains(c)) { result.Add(c); }
                }
                // different participants in each class
                foreach (var e in c.Enrolments.Where(x => x.PersonID == Student.ID))
                {
                    c.IsSelected = true;
                    c.IsSelectedByEnrolment = e;
                    if (!result.Contains(c)) { result.Add(c); }
                }
            }
            return result.OrderBy(x => x.OnDayID).ToList();
        }
        public static bool IsCourseInTerm(Course course, Term term)
        {
            bool result = false;
            foreach (var c in course.Classes)
            {
                if (IsClassInTerm(c, term.TermNumber)) { result = true; break; }
            }
            return result;
        }

        public static async Task<List<Person>> GetPersonsInClassAsync(U3ADbContext dbc, Guid ClassID)
        {
            List<Person> result = new List<Person>();
            var course = dbc.Class.Include(x => x.Course).Where(x => x.ID == ClassID).Select(x => x.Course).FirstOrDefault();
            if (course != null)
            {
                if (course.CourseParticipationTypeID == (int?)ParticipationType.SameParticipantsInAllClasses)
                {
                    result = await dbc.Enrolment
                                    .Include(x => x.Person)
                                    .Where(x => x.CourseID == course.ID && !x.IsWaitlisted)
                                    .Select(x => x.Person).ToListAsync();
                }
                else
                {
                    result = await dbc.Enrolment
                                    .Include(x => x.Person)
                                    .Where(x => x.CourseID == course.ID && x.ClassID == ClassID && !x.IsWaitlisted)
                                    .Select(x => x.Person).ToListAsync();
                }
            }
            return result;
        }

        public static int GetNextTermOffered(Class Class, int TermNumber)
        {
            int result = 0;
            // Find the 1st term >= the current term
            for (int i = TermNumber; i <= 4; i++)
            {
                if (i == 1 && Class.OfferedTerm1) { result = i; break; }
                if (i == 2 && Class.OfferedTerm2) { result = i; break; }
                if (i == 3 && Class.OfferedTerm3) { result = i; break; }
                if (i == 4 && Class.OfferedTerm4) { result = i; break; }
            }
            if (result == 0)
            {
                // Find the 1st term <= the current term
                for (int i = TermNumber; i > 0; i--)
                {
                    if (i == 4 && Class.OfferedTerm4) { result = i; break; }
                    if (i == 3 && Class.OfferedTerm3) { result = i; break; }
                    if (i == 2 && Class.OfferedTerm2) { result = i; break; }
                    if (i == 1 && Class.OfferedTerm1) { result = i; break; }
                }
            }
            return result;
        }

        public static bool IsClassInTerm(Class Class, int TermNumber)
        {
            bool result = false;
            switch (TermNumber)
            {
                case 1:
                    result = Class.OfferedTerm1;
                    break;
                case 2:
                    result = Class.OfferedTerm2;
                    break;
                case 3:
                    result = Class.OfferedTerm3;
                    break;
                case 4:
                    result = Class.OfferedTerm4;
                    break;
            }
            return result;
        }
        public static bool IsClassInYear(U3ADbContext dbc, Class Class)
        {
            return (Class.OfferedTerm1 || Class.OfferedTerm2 || Class.OfferedTerm3 || Class.OfferedTerm4);
        }
        public static bool IsClassInReportingPeriod(ClassScheduleDisplayPeriod reportPeriod,
                            Class Class, Term term)
        {
            List<bool> termsOffered = new();
            termsOffered.Add(Class.OfferedTerm1);
            termsOffered.Add(Class.OfferedTerm2);
            termsOffered.Add(Class.OfferedTerm3);
            termsOffered.Add(Class.OfferedTerm4);
            bool result = true;
            switch (reportPeriod)
            {
                case ClassScheduleDisplayPeriod.CurrentSemester:
                    if (term.TermNumber <= 2)
                    {
                        // zero based array
                        if (!termsOffered[0] && !termsOffered[1]) { result = false; }
                    }
                    else
                    {
                        if (!termsOffered[2] && !termsOffered[3]) { result = false; }
                    }
                    break;
                case ClassScheduleDisplayPeriod.CurrentTerm:
                    if (!termsOffered[term.TermNumber - 1]) { result = false; }
                    break;
            }
            return result;
        }
        public static bool IsClassInRemainingYear(Class Class, int termNumber)
        {
            bool result = false;
            switch (termNumber)
            {
                case 1:
                    result = (Class.OfferedTerm1 || Class.OfferedTerm2 || Class.OfferedTerm3 || Class.OfferedTerm4);
                    break;
                case 2:
                    result = Class.OfferedTerm2 || Class.OfferedTerm3 || Class.OfferedTerm4;
                    break;
                case 3:
                    result = Class.OfferedTerm3 || Class.OfferedTerm4;
                    break;
                case 4:
                    result = Class.OfferedTerm4;
                    break;
            }
            return result;
        }
        public static bool IsClassInRemainingYear(U3ADbContext dbc,
                            Class Class, Term term, Term defaultTerm, IEnumerable<Term> allTerms = null)
        {

            Log.Information("Processing {p0}...", Class.Course.Name);
            LogClassDetails(Class, term);

            bool result = false;
            var startDate = Class.StartDate;
            var recurrence = Class.Recurrence;
            var occurrence = (OccurrenceType)Class.OccurrenceID;
            var today = dbc.GetLocalTime().Date;

            // Return true if startDate is in future
            if (startDate != null && startDate >= today)
            {
                Console.BackgroundColor = ConsoleColor.Green;
                Log.Information("    Return {p0} because start {p1} >= today {p2}."
                        , true, DateOnly.FromDateTime(startDate.Value), DateOnly.FromDateTime(today));
                return true;
            }

            // Return false if single day activity && startDate is prior to today
            if (startDate != null && startDate < today && occurrence == OccurrenceType.OnceOnly)
            {
                Console.BackgroundColor = ConsoleColor.Red;
                Log.Information("    Return {p0} because start {p1} < today {p2} && {p3}.",
                    false, DateOnly.FromDateTime(startDate.Value), DateOnly.FromDateTime(today), "Once-only activity");
                return false;
            }

            // return True if term number is current or future
            // && startDate/recurrence are null
            switch (term.TermNumber)
            {
                case 1:
                    result = Class.OfferedTerm1 || Class.OfferedTerm2 || Class.OfferedTerm3 || Class.OfferedTerm4;
                    break;
                case 2:
                    result = Class.OfferedTerm2 || Class.OfferedTerm3 || Class.OfferedTerm4;
                    break;
                case 3:
                    result = Class.OfferedTerm3 || Class.OfferedTerm4;
                    break;
                case 4:
                    result = Class.OfferedTerm4;
                    break;
            }
            if (result && startDate == null && recurrence == null)
            {
                Console.BackgroundColor = ConsoleColor.Green;
                Log.Information("    Return {p0} because term {p1} is current or in future {p2}."
                    , true, term.Name, Class.OfferedSummary);
                return true;
            }

            // Return FALSE if in previous term (result == false) 
            // && startDate/recurrence are null
            if (!result && startDate == null && recurrence == null)
            {
                Console.BackgroundColor = ConsoleColor.Red;
                Log.Information("    Return {p0} because class {p1} is prior to current term {p2}.",
                        false, Class.OfferedSummary, term.Name);
                return false;
            }


            // no more tests if term is in previous year
            if (term.Year < defaultTerm.Year) return result;

            DateTime? endDate;

            // if Recurrence is null, the end date is the final date in the term offered.
            if (Class.Recurrence == null)
            {
                var offeredTermNumber = GetNextTermOffered(Class, term.TermNumber);
                var offeredTerm = allTerms.FirstOrDefault(x => x.TermNumber == offeredTermNumber);
                if (offeredTerm != null)
                {
                    endDate = offeredTerm.EndDate;
                    if (endDate < today)
                    {
                        Console.BackgroundColor = ConsoleColor.Red;
                        Log.Information("    Return {p0} because class end: {p1} is prior to today: {p2}.",
                                false, endDate, startDate);
                        return false;
                    }
                }
            }

            Log.Warning("    Dropped thru initial tests.");

            // failed all simple tests - calculate the end date
            endDate = GetClassEndDate(Class, term);
            if (endDate == null || endDate <= term.StartDate) result = false; else result = true;
            Log.Information("    Calculated EndDate: {p}", endDate);
            if (result)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Log.Information("    Return {p0} because term Start {p1} is prior to class End {p2}",
                                    true, term.StartDate, endDate);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Log.Information("    Return {p0} because term Start {p1} is later class End {p2}",
                                        false, term.StartDate, endDate);
            }
            return result;
        }

        private static void LogClassDetails(Class Class, Term term)
        {
            Log.Information("    Start Date:         {p}", Class.StartDate);
            Log.Information("    Occurence:          {p}", (OccurrenceType)Class.OccurrenceID);
            Log.Information("    Recurence:          {p}", Class.Recurrence);
            Log.Information("    Current Term:       {p}", term.Name);
            Log.Information("    Offered:            {p}", Class.OfferedSummary);
        }

        public static async Task AssignTermForOnceOnlyClass(U3ADbContext dbc, Class c)
        {
            if (c.StartDate == null) { return; }
            if (c.OccurrenceID != (int)OccurrenceType.OnceOnly) { return; }
            Term reqdTerm = default;
            var terms = await dbc.Term.OrderByDescending(x => x.Year).ThenByDescending(x => x.TermNumber).ToListAsync();
            foreach (var t in terms)
            {
                if (c.StartDate >= t.StartDate && c.StartDate <= t.EndDate)
                {
                    reqdTerm = t;
                    break;
                }
            }
            if (reqdTerm != null)
            {
                c.OfferedTerm1 = c.OfferedTerm2 = c.OfferedTerm3 = c.OfferedTerm4 = false;
                switch (reqdTerm.TermNumber)
                {
                    case 1: c.OfferedTerm1 = true; break;
                    case 2: c.OfferedTerm2 = true; break;
                    case 3: c.OfferedTerm3 = true; break;
                    case 4: c.OfferedTerm4 = true; break;
                }
            }
        }

        public async static Task<bool> IsOutOfTermClassOK(U3ADbContext dbc, Class c)
        {
            bool result = false;
            // *** we assume IsOutOfTermClass has already tested (see below) ***
            // An out of term class must have an Occurrence of Once Only && No recurrence
            if (c.Recurrence == null &&
                (OccurrenceType)c.Occurrence?.ID == OccurrenceType.OnceOnly) result = true;
            return result;
        }
        public async static Task<bool> IsOutOfTermClassTermOK(U3ADbContext dbc, Class c)
        {
            bool result = true;
            // *** we assume IsOutOfTermClass has already tested (see below) ***
            // An out of term class must have one only term
            var count = 0;
            if (c.OfferedTerm1 == true) { count++; }
            if (c.OfferedTerm2 == true) { count++; }
            if (c.OfferedTerm3 == true) { count++; }
            if (c.OfferedTerm4 == true) { count++; }
            if (count != 1) { result = false; }
            // Set the correct term;
            var terms = await dbc.Term.OrderByDescending(x => x.Year).ThenByDescending(x => x.TermNumber).ToListAsync();
            Term reqdTerm = default;
            foreach (var t in terms)
            {
                if (c.StartDate > t.EndDate)
                {
                    reqdTerm = t;
                    break;
                }
            }
            if (reqdTerm != null)
            {
                c.OfferedTerm1 = c.OfferedTerm2 = c.OfferedTerm3 = c.OfferedTerm4 = false;
                if (c.StartDate.Value.Year > reqdTerm.StartDate.Year)
                {
                    c.OfferedTerm1 = true;
                }
                else
                {
                    switch (reqdTerm.TermNumber)
                    {
                        case 1: c.OfferedTerm1 = true; break;
                        case 2: c.OfferedTerm2 = true; break;
                        case 3: c.OfferedTerm3 = true; break;
                        case 4: c.OfferedTerm4 = true; break;
                    }
                }
            }
            return result;
        }
        public async static Task<bool> IsOutOfTermClass(U3ADbContext dbc, Class c)
        {
            if (c.StartDate == null) return false;
            bool result = true;
            var terms = await dbc.Term.OrderByDescending(x => x.Year).ThenByDescending(x => x.TermNumber).ToListAsync();
            foreach (var t in terms)
            {
                if (c.StartDate >= t.StartDate && c.StartDate <= t.EndDate)
                {
                    result = false;
                    break;
                }
            }
            return result;
        }

        public static async Task ReassignClassScheduleValues(U3ADbContext dbc, Class c)
        {
            var onDayModified = c.EntityPropertyChanges(dbc, nameof(Class.OnDayID));
            var onOccurenceModified = c.EntityPropertyChanges(dbc, nameof(Class.OccurrenceID));
            var startTimeModified = c.EntityPropertyChanges(dbc, nameof(Class.StartTime));
            var startDateModified = c.EntityPropertyChanges(dbc, nameof(Class.StartDate));
            var recurrenceModified = c.EntityPropertyChanges(dbc, nameof(Class.Recurrence));
            var offeredT1Modified = c.EntityPropertyChanges(dbc, nameof(Class.OfferedTerm1));
            var offeredT2Modified = c.EntityPropertyChanges(dbc, nameof(Class.OfferedTerm2));
            var offeredT3Modified = c.EntityPropertyChanges(dbc, nameof(Class.OfferedTerm3));
            var offeredT4Modified = c.EntityPropertyChanges(dbc, nameof(Class.OfferedTerm4));
            if (onDayModified == null
                && startTimeModified == null
                && onOccurenceModified == null
                && startDateModified == null
                && recurrenceModified == null
                && offeredT1Modified == null
                && offeredT2Modified == null
                && offeredT3Modified == null
                && offeredT4Modified == null) { return; }
            {
                var itesmToRenove = dbc.AttendClass
                          .Where(x => x.ClassID == c.ID
                                  && x.Date > dbc.GetLocalTime().Date).ToList();
                dbc.RemoveRange(itesmToRenove);
            }

        }
    }
}