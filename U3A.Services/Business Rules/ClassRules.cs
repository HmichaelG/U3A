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

namespace U3A.BusinessRules
{
    public static partial class BusinessRule
    {
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

        public static List<Class> GetClassDetailsWhereNotLeader(U3ADbContext dbc, Term term, Person person)
        {
            var classes = dbc.Class.AsNoTracking()
                            .Include(x => x.OnDay)
                            .Include(x => x.Course).ThenInclude(x => x.Enrolments.Where(x => x.TermID == term.ID))
                            .Include(x => x.Leader)
                            .Include(x => x.Occurrence)
                            .Include(x => x.Venue)
                            .Where(x => x.Course.Year == term.Year)
                            .OrderBy(x => x.OnDayID).ThenBy(x => x.StartTime).ToList();
            var termEnrolments = dbc.Enrolment.AsNoTracking().Where(x => x.TermID == term.ID).ToList();
            Parallel.ForEach(classes, c =>
            {
                BusinessRule.SetCourseParticipationDetails(dbc, c, termEnrolments);
            });
            return classes;
        }

        /// <summary>
        /// Get available classes for the current year
        /// </summary>
        /// <param name="dbc"></param>
        /// <param name="term"></param>
        /// <returns></returns>
        public static async Task<List<Class>> GetClassDetailsAsync(U3ADbContext dbc, Term term, SystemSettings settings)
        {
            var terms = await dbc.Term.AsNoTracking().ToListAsync();
            var defaultTerm = dbc.Term.AsNoTracking().FirstOrDefault(x => x.IsDefaultTerm);
            var people = await dbc.Person.AsNoTracking().ToListAsync();
            var classes = dbc.Class.AsNoTracking()
                            .Include(x => x.OnDay)
                            .Include(x => x.Course).ThenInclude(x => x.CourseType)
                            .Include(x => x.Course)
                                .ThenInclude(x => x.Enrolments.Where(x => x.TermID >= term.ID))
                            .Include(x => x.Leader)
                            .Include(x => x.Occurrence)
                            .Include(x => x.Venue)
                            .Where(x => x.Course.Year == term.Year)
                            .AsEnumerable().Where(x => IsClassInRemainingYear(dbc, x, term, defaultTerm)).ToList();
            Parallel.ForEach(classes, c =>
            {
                c.TermNumber = GetRequiredTerm( term.TermNumber,c);
                Parallel.ForEach(c.Course.Enrolments, e =>
                {
                    e.Term = terms.FirstOrDefault(x => x.ID == e.TermID);
                    e.Person = people.FirstOrDefault(x => x.ID == e.PersonID);
                });
            });
            AssignClassContacts(classes, term, settings);
            await AssignClassCountsAsync(dbc, term, classes);
            var prevTerm = await GetPreviousTermAsync(dbc, term.Year, term.TermNumber);
            if (prevTerm != null)
            {
                //OccurenceID == 99 is Unscheduled.
                var prevTermShoulderClasses = dbc.Class.AsNoTracking()
                                .Include(x => x.OnDay)
                                .Include(x => x.Course).ThenInclude(x => x.CourseType)
                                .Include(x => x.Course)
                                    .ThenInclude(x => x.Enrolments.Where(x => x.TermID == prevTerm.ID))
                                .Include(x => x.Leader)
                                .Include(x => x.Occurrence)
                                .Include(x => x.Venue)
                                .Where(x => x.Course.Year == prevTerm.Year && x.OccurrenceID != 999)
                                .AsEnumerable()
                                .Where(x => IsClassEndDateInInterTermPeriod(dbc, x, prevTerm, prevTerm.EndDate, term.StartDate)
                                                && IsClassInRemainingYear(dbc, x, prevTerm, defaultTerm)).ToList();
                Parallel.ForEach(prevTermShoulderClasses, c =>
                {
                    c.TermNumber = prevTerm.TermNumber;
                    Parallel.ForEach(c.Course.Enrolments, e =>
                    {
                        e.Term = terms.FirstOrDefault(x => x.ID == e.TermID);
                        e.Person = people.FirstOrDefault(x => x.ID == e.PersonID);
                    });
                });
                AssignClassContacts(prevTermShoulderClasses, prevTerm, settings);
                await AssignClassCountsAsync(dbc, prevTerm, prevTermShoulderClasses);
                classes.AddRange(prevTermShoulderClasses);
            }
            return EnsureOneClassOnlyForSameParticipantsInEachClass(dbc, classes)
                        .OrderBy(x => x.OnDayID).ThenBy(x => x.Course.Name).ToList();
        }

        public static int GetRequiredTerm(int termNumber, Class c)
        {
            int Result = termNumber-1;
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
            var d = BusinessRule.GetClassEndDate(dbc, c, t);
            return (d < endDate) && (d > startDate);
        }

        public static void AssignClassContacts(List<Class> classes, Term term, SystemSettings settings)
        {
            //Parallel.ForEach(classes, c =>
            foreach (var c in classes)
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
            //});
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

        internal record EnrolmentCount
        {
            internal Guid ID;
            internal int Year;
            internal int TermNumber;
            internal bool IsWaitlisted;
            internal int Count;
        }

        private static IQueryable<EnrolmentCount> GetEnrolmentCountByCourse(U3ADbContext dbc, Term term)
        {
            return dbc.Enrolment
                        .Include(x => x.Term)
                        .Where(x => x.Term.Year == term.Year && x.Term.TermNumber >= term.TermNumber &&
                                x.ClassID == null)
                        .GroupBy(e => new
                        {
                            ID = e.CourseID,
                            Year = e.Term.Year,
                            TermNumber = e.Term.TermNumber,
                            IsWaitlisted = e.IsWaitlisted
                        })
                        .Select(g => new EnrolmentCount
                        {
                            ID = g.Key.ID,
                            Year = g.Key.Year,
                            TermNumber = g.Key.TermNumber,
                            IsWaitlisted = g.Key.IsWaitlisted,
                            Count = g.Count()
                        });
        }
        private static IQueryable<EnrolmentCount> GetEnrolmentCountByCLass(U3ADbContext dbc, Term term)
        {
            return dbc.Enrolment
                        .Include(x => x.Term)
                        .Where(x => x.Term.Year == term.Year && x.Term.TermNumber >= term.TermNumber &&
                                x.ClassID != null)
                        .GroupBy(e => new
                        {
                            ID = e.ClassID,
                            Year = e.Term.Year,
                            TermNumber = e.Term.TermNumber,
                            IsWaitlisted = e.IsWaitlisted
                        })
                        .Select(g => new EnrolmentCount
                        {
                            ID = g.Key.ID.Value,
                            Year = g.Key.Year,
                            TermNumber = g.Key.TermNumber,
                            IsWaitlisted = g.Key.IsWaitlisted,
                            Count = g.Count()
                        });
        }
        private static async Task AssignClassCountsAsync(U3ADbContext dbc, Term term, List<Class> Classes)
        {
            var enrolmentCountByCourse = GetEnrolmentCountByCourse(dbc, term);
            var enrolmentCountByCLass = GetEnrolmentCountByCLass(dbc, term);
            foreach (var c in Classes)
            {
                var nextTerm = GetNextTermOffered(c, term.TermNumber);
                double maxStudents = c.Course.MaximumStudents; ;
                c.ParticipationRate = 0;
                if (c.Course.CourseParticipationTypeID == (int?)ParticipationType.SameParticipantsInAllClasses)
                {
                    var result = await enrolmentCountByCourse.FirstOrDefaultAsync(x => x.ID == c.CourseID &&
                                                                            x.Year == term.Year &&
                                                                            x.TermNumber == nextTerm &&
                                                                            !x.IsWaitlisted);
                    c.TotalActiveStudents = (result == null) ? 0 : result.Count;
                    result = await enrolmentCountByCourse.FirstOrDefaultAsync(x => x.ID == c.CourseID &&
                                                                            x.Year == term.Year &&
                                                                            x.TermNumber == nextTerm &&
                                                                            x.IsWaitlisted);
                    c.TotalWaitlistedStudents = (result == null) ? 0 : result.Count;
                }
                else
                {
                    var result = await enrolmentCountByCLass.FirstOrDefaultAsync(x => x.ID == c.ID &&
                                                                            x.Year == term.Year &&
                                                                            x.TermNumber == nextTerm &&
                                                                            !x.IsWaitlisted);
                    c.TotalActiveStudents = (result == null) ? 0 : result.Count;
                    result = await enrolmentCountByCLass.FirstOrDefaultAsync(x => x.ID == c.ID &&
                                                                            x.Year == term.Year &&
                                                                            x.TermNumber == nextTerm &&
                                                                            x.IsWaitlisted);
                    c.TotalWaitlistedStudents = (result == null) ? 0 : result.Count;
                }
                if (maxStudents != 0) c.ParticipationRate = (double)((c.TotalActiveStudents + c.TotalWaitlistedStudents) / maxStudents);
            }
        }

        private static void AssignClassCounts(U3ADbContext dbc, Term term, List<Class> Classes)
        {
            var enrolmentCountByCourse = GetEnrolmentCountByCourse(dbc, term);
            var enrolmentCountByCLass = GetEnrolmentCountByCLass(dbc, term);
            foreach (var c in Classes)
            {
                var nextTerm = GetNextTermOffered(c, term.TermNumber);
                double maxStudents = c.Course.MaximumStudents; ;
                c.ParticipationRate = 0;
                if (c.Course.CourseParticipationTypeID == (int?)ParticipationType.SameParticipantsInAllClasses)
                {
                    var result = enrolmentCountByCourse.FirstOrDefault(x => x.ID == c.CourseID &&
                                                                            x.Year == term.Year &&
                                                                            x.TermNumber == nextTerm &&
                                                                            !x.IsWaitlisted);
                    c.TotalActiveStudents = (result == null) ? 0 : result.Count;
                    result = enrolmentCountByCourse.FirstOrDefault(x => x.ID == c.CourseID &&
                                                                            x.Year == term.Year &&
                                                                            x.TermNumber == nextTerm &&
                                                                            x.IsWaitlisted);
                    c.TotalWaitlistedStudents = (result == null) ? 0 : result.Count;
                }
                else
                {
                    var result = enrolmentCountByCLass.FirstOrDefault(x => x.ID == c.ID &&
                                                                            x.Year == term.Year &&
                                                                            x.TermNumber == nextTerm &&
                                                                            !x.IsWaitlisted);
                    c.TotalActiveStudents = (result == null) ? 0 : result.Count;
                    result = enrolmentCountByCLass.FirstOrDefault(x => x.ID == c.ID &&
                                                                            x.Year == term.Year &&
                                                                            x.TermNumber == nextTerm &&
                                                                            x.IsWaitlisted);
                    c.TotalWaitlistedStudents = (result == null) ? 0 : result.Count;
                }
                if (maxStudents != 0) c.ParticipationRate = (double)((c.TotalActiveStudents + c.TotalWaitlistedStudents) / maxStudents);
            }
        }
        public static void AssignClassClerks(U3ADbContext dbc, Term term, IEnumerable<Class> classes)
        {
            var enrolments = dbc.Enrolment
                                .Include(x => x.Person)
                                .Where(x => x.TermID == term.ID && x.IsCourseClerk).ToList();
            Parallel.ForEach(classes, c =>
            {
                var clerkDetails = string.Empty;
                foreach (var e in enrolments.Where(x => x.CourseID == c.Course.ID))
                {
                    c.Clerks.Add(e.Person);
                }
            });
        }


        public static List<Class> GetClassDetails(U3ADbContext dbc, Term term, SystemSettings settings)
        {
            var terms = dbc.Term.AsNoTracking().Where(x => x.Year == term.Year &&
                            x.TermNumber >= term.TermNumber);
            var defaultTerm = dbc.Term.AsNoTracking().FirstOrDefault(x => x.IsDefaultTerm);
            var people = dbc.Person.AsNoTracking().ToList();
            var classes = dbc.Class.AsNoTracking()
                            .Include(x => x.OnDay)
                            .Include(x => x.Course).ThenInclude(x => x.CourseType)
                            .Include(x => x.Course)
                                .ThenInclude(x => x.Enrolments.Where(x => terms.Contains(x.Term)))
                                .ThenInclude(e => e.Person)
                            .Include(x => x.Leader)
                            .Include(x => x.Leader2)
                            .Include(x => x.Leader3)
                            .Include(x => x.Occurrence)
                            .Include(x => x.Venue)
                            .Where(x => x.Course.Year == term.Year)
                            .OrderBy(x => x.OnDayID).ThenBy(x => x.StartTime)
                            .AsEnumerable().Where(x => IsClassInRemainingYear(dbc, x, term, defaultTerm)).ToList();
            Parallel.ForEach(classes, c =>
            {
                Parallel.ForEach(c.Course.Enrolments, e =>
                {
                    e.Person = people.FirstOrDefault(x => x.ID == e.PersonID);
                });
            });
            AssignClassContacts(classes, term, settings);
            AssignClassCounts(dbc, term, classes);
            AssignClassClerks(dbc, term, classes);
            var prevTerm = GetPreviousTerm(dbc, term.Year, term.TermNumber);
            if (prevTerm != null)
            {
                var prevTermShoulderClasses = dbc.Class.AsNoTracking()
                                .Include(x => x.OnDay)
                                .Include(x => x.Course).ThenInclude(x => x.CourseType)
                                .Include(x => x.Course)
                                    .ThenInclude(x => x.Enrolments.Where(x => x.TermID == prevTerm.ID))
                                .Include(x => x.Leader)
                                .Include(x => x.Occurrence)
                                .Include(x => x.Venue)
                                .Where(x => x.Course.Year == prevTerm.Year)
                                .OrderBy(x => x.OnDayID).ThenBy(x => x.Course.Name)
                                .AsEnumerable()
                                .Where(x => x.StartDate.GetValueOrDefault() > prevTerm.EndDate
                                                && x.StartDate.GetValueOrDefault() < term.StartDate
                                                && IsClassInRemainingYear(dbc, x, prevTerm, defaultTerm)).ToList();
                Parallel.ForEach(prevTermShoulderClasses, c =>
                {
                    Parallel.ForEach(c.Course.Enrolments, e =>
                    {
                        e.Person = people.FirstOrDefault(x => x.ID == e.PersonID);
                    });
                });
                AssignClassContacts(prevTermShoulderClasses, prevTerm, settings);
                AssignClassCounts(dbc, prevTerm, prevTermShoulderClasses);
                AssignClassClerks(dbc, prevTerm, prevTermShoulderClasses);
                classes.AddRange(prevTermShoulderClasses);
            }
            return EnsureOneClassOnlyForSameParticipantsInEachClass(dbc, classes).ToList();
        }

        public static List<Class> GetClassDetailsForStudent(IEnumerable<Class> Classes, Person Student)
        {
            List<Class> result;
            result = Classes.Where(c => c.Course.Enrolments
                                .Any(e => e.PersonID == Student.ID &&
                                    (e.ClassID == null || e.ClassID == c.ID))).ToList();
            return result;
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
            for (int i = TermNumber; i < 4; i++ )
            {
                if (i == 1 && Class.OfferedTerm1) { result = i; break; }
                if (i == 2 && Class.OfferedTerm2) { result = i; break; }
                if (i == 3 && Class.OfferedTerm3) { result = i; break; }
                if (i == 4 && Class.OfferedTerm4) { result = i; break; }
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
        public static bool IsClassInRemainingYear(U3ADbContext dbc, Class Class, Term term, Term defaultTerm)
        {
            bool result = false;
            switch (term.TermNumber)
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
            // no more tests if term is in previous year
            if (term.Year < defaultTerm.Year) return result;
            // otherwise, only print classes that have not yet ended
            DateTime? endDate = GetClassEndDate(dbc, Class, term);
            var localTime = TimezoneAdjustment.GetLocalTime();
            if (endDate == null || endDate <= localTime) result = false;
            return result;
        }

        public static async Task DeleteLeadershipRole(U3ADbContext dbc, Guid PersonID)
        {
            foreach (var c in await dbc.Class.Where(x => x.LeaderID == PersonID).ToListAsync())
            {
                c.LeaderID = null;
                c.Leader = null;
            }
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
    }
}