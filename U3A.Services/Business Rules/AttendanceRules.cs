using DevExpress.Blazor;
using DevExpress.Data.Linq;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using U3A.Database;
using U3A.Model;

namespace U3A.BusinessRules
{
    public static partial class BusinessRule
    {
        public static async Task<List<AttendClassSummary>> GetClassAttendanceSummary(U3ADbContext dbc,
                                DateTime SummaryDate, Term selectedTerm, Class selectedClass)
        {
            var results = (await dbc.AttendClass.AsNoTracking().Include(ac => ac.Person)
                .Where(ac => ac.TermID == selectedTerm.ID &&
                                    ac.ClassID == selectedClass.ID &&
                                    ac.Date < SummaryDate.AddDays(1))
                .GroupBy(ac => ac.Person)
                .Select(g => new AttendClassSummary
                {
                    Person = g.Key,
                    PersonID = g.Key.ID,
                    Present = g.Count(ac => (AttendClassStatusType)ac.AttendClassStatusID == AttendClassStatusType.Present),
                    AbsentWithApology = g.Count(ac => (AttendClassStatusType)ac.AttendClassStatusID == AttendClassStatusType.AbsentFromClassWithApology),
                    AbsentWithoutApology = g.Count(ac => (AttendClassStatusType)ac.AttendClassStatusID == AttendClassStatusType.AbsentFromClassWithoutApology)
                }).ToListAsync()).OrderBy(x => x.Person.FullNameAlpha).ToList();
            return results;
        }

        public static async Task<List<AttendClassSummaryByWeek>> GetClassAttendanceSummaryByWeek(U3ADbContext dbc)
        {
            var today = dbc.GetLocalTime().Date;
            var endDate = today.AddDays(-(int)DateTime.Today.DayOfWeek + 6);
            var startDate = today.AddDays(-364).Date; // get the date 52 weeks ago
            startDate = startDate.AddDays(-(int)startDate.DayOfWeek + 6);
            // A class with valid attendance has at least 1 present
            var validAttendance = (await dbc.AttendClass
                .Where(ac => ac.Date >= startDate && ac.Date <= endDate) // filter by the past 52 weeks
                .ToListAsync())
                .GroupBy(ac => new { ac.ClassID, ac.Date.Date })
                .Select(g => new
                {
                    ClassDate = g.Key.Date,
                    Present = g.Count(ac => (AttendClassStatusType)ac.AttendClassStatusID == AttendClassStatusType.Present),
                    AbsentWithApology = g.Count(ac => (AttendClassStatusType)ac.AttendClassStatusID == AttendClassStatusType.AbsentFromClassWithApology),
                    AbsentWithoutApology = g.Count(ac => (AttendClassStatusType)ac.AttendClassStatusID == AttendClassStatusType.AbsentFromClassWithoutApology)
                })
                .Where(x => x.Present > 0)
                ;
            var result = validAttendance
                .GroupBy(ca => new { WeekEndDate = (ca.ClassDate).AddDays(-(int)ca.ClassDate.DayOfWeek + 6) })
                .Select(g => new AttendClassSummaryByWeek
                {
                    WeekEnd = g.Key.WeekEndDate,
                    Present = g.Sum(ac => ac.Present),
                    AbsentWithApology = g.Sum(ac => ac.AbsentWithApology),
                    AbsentWithoutApology = g.Sum(ac => ac.AbsentWithoutApology)
                })
                .OrderBy(x => x.WeekEnd).ToList();

            for (var i = 0; i < 52; i++)
            {
                var thisWeek = startDate.AddDays(i * 7);
                if (!result.Any(x => x.WeekEnd == thisWeek))
                {
                    result.Add(new AttendClassSummaryByWeek()
                    {
                        WeekEnd = thisWeek,
                        AbsentWithApology = 0,
                        AbsentWithoutApology = 0,
                        Present = 0
                    });
                }
            };
            return result.OrderBy(x => x.WeekEnd).ToList();
        }
        public static List<AttendClassDetailByWeek> GetClassAttendanceDetailByWeek(U3ADbContext dbc, int Year)
        {
            var classes = dbc.Class.Include(x => x.OnDay).ToImmutableList();
            var courses = dbc.Course.ToImmutableList();
            var courseTypes = dbc.CourseType.ToImmutableList();
            var endDate = new DateTime(Year, 12, 31);
            var startDate = new DateTime(Year, 1, 1);
            // A class with valid attendance has at least 1 present
            var validAttendance = (dbc.AttendClass.Include(x => x.Class)
                .Where(ac => ac.Date >= startDate && ac.Date <= endDate)
                .ToList())
                .GroupBy(ac => new { ac.ClassID, ac.Date.Date })
                .Select(g => new
                {
                    ClassID = g.Key.ClassID,
                    ClassDate = g.Key.Date,
                    Present = g.Count(ac => (AttendClassStatusType)ac.AttendClassStatusID == AttendClassStatusType.Present),
                    AbsentWithApology = g.Count(ac => (AttendClassStatusType)ac.AttendClassStatusID == AttendClassStatusType.AbsentFromClassWithApology),
                    AbsentWithoutApology = g.Count(ac => (AttendClassStatusType)ac.AttendClassStatusID == AttendClassStatusType.AbsentFromClassWithoutApology)
                })
                .Where(x => x.Present > 0)
                ;
            var result = validAttendance
                .GroupBy(ca => new { ca.ClassID, WeekEndDate = ca.ClassDate.AddDays(-(int)ca.ClassDate.DayOfWeek + 6) })
                .Select(g => new AttendClassDetailByWeek
                {
                    ClassID = g.Key.ClassID,
                    CourseID = classes.First(x => x.ID == g.Key.ClassID).CourseID,
                    WeekEnd = g.Key.WeekEndDate,
                    Present = g.Sum(ac => ac.Present),
                    AbsentWithApology = g.Sum(ac => ac.AbsentWithApology),
                    AbsentWithoutApology = g.Sum(ac => ac.AbsentWithoutApology)
                })
                .OrderBy(x => x.WeekEnd).ToList();

            Parallel.ForEach(result, a =>
            {
                var Class = classes.First(x => x.ID == a.ClassID);
                var course = courses.First(x => x.ID == Class.CourseID);
                var courseType = courseTypes.First(x => x.ID == course.CourseTypeID);
                a.CourseID = course.ID;
                a.CourseTypeID = courseType.ID;
                a.OccurrenceTypeID = (int)Class.OccurrenceID;
                a.CourseTypeDescription = courseType.Name;
                a.CourseDescription = $"{course.Name}: {Class.OccurrenceTextBrief}";
            });

            return result.OrderBy(x => x.CourseDescription).ThenBy(x => x.WeekEnd).ToList();
        }
        public static async Task<List<AttendClassSummaryByCourse>> GetClassAttendanceSummaryByCourse(U3ADbContext dbc, Term term, DateTime Now)
        {
            var validAttendance = (await dbc.AttendClass
                .Include(ac => ac.Class).ThenInclude(c => c.Course)
                .Where(ac => ac.TermID == term.ID && ac.Date.Date <= Now.Date)
                .ToListAsync())
                    .GroupBy(ac => new
                    {
                        ClassID = ac.ClassID,
                        ClassDate = ac.Date
                    })
                .Select(g => new
                {
                    ClassID = g.Key.ClassID,
                    ClassDate = g.Key.ClassDate,
                    Course = g.Max(x => x.Class.Course.Name),
                    Present = g.Count(ac => (AttendClassStatusType)ac.AttendClassStatusID == AttendClassStatusType.Present),
                    AbsentWithApology = g.Count(ac => (AttendClassStatusType)ac.AttendClassStatusID == AttendClassStatusType.AbsentFromClassWithApology),
                    AbsentWithoutApology = g.Count(ac => (AttendClassStatusType)ac.AttendClassStatusID == AttendClassStatusType.AbsentFromClassWithoutApology)
                })
                .Where(x => x.Present > 0 || x.AbsentWithApology > 0)
                ;


            var result = validAttendance
                    .GroupBy(ac => new
                    {
                        Course = ac.Course,
                        ClassID = ac.ClassID
                    })
                    .Select(g => new AttendClassSummaryByCourse
                    {
                        Course = g.Key.Course,
                        ClassID = g.Key.ClassID,
                        DateSummary = g.Max(ac => ac.ClassDate).ToString("ddd, hh:mm tt"),
                        ClassesRecorded = g.DistinctBy(ac => ac.ClassDate).Count(),
                        Present = g.Sum(ac => ac.Present),
                        AbsentWithApology = g.Sum(ac => ac.AbsentWithApology),
                        AbsentWithoutApology = g.Sum(ac => ac.AbsentWithoutApology)
                    }).OrderBy(x => x.Course).ToList();

            return result;
        }

        public static async Task<List<AttendanceRecorded>> GetAttendanceNotRecordedAsync(U3ADbContext dbc,
                    Term selectedTerm, DateTime Now)
        {
            return await GetAttendanceAsync(dbc, selectedTerm, Now, false);
        }
        public static async Task<List<AttendanceRecorded>> GetAttendanceRecordedAsync(U3ADbContext dbc,
                    Term selectedTerm, DateTime Now)
        {
            return await GetAttendanceAsync(dbc, selectedTerm, Now, true);
        }
        private static async Task<List<AttendanceRecorded>> GetAttendanceAsync(U3ADbContext dbc,
                    Term selectedTerm, DateTime Now, bool GetRecorded)
        {

            // Class attendance this term
            var attendance = await dbc.AttendClass
                .Include(ac => ac.Class).ThenInclude(c => c.Course)
                .Where(ac => ac.TermID == selectedTerm.ID)
                .ToListAsync();

            // Class dates this term
            IEnumerable<Class> classes;
            var lastAllowedClassDate = await BusinessRule.GetLastAllowedClassDateForTermAsync(dbc, selectedTerm);
            var storage = await GetCourseScheduleDataStorageAsync(dbc, selectedTerm);
            DateTime start = selectedTerm.StartDate;
            DateTime end = Now;
            end = (end > start && end <= lastAllowedClassDate) ? end : lastAllowedClassDate;
            DxSchedulerDateTimeRange range = new DxSchedulerDateTimeRange(start, end);

            // for each appointment find attendance recorded / not recorded
            var appointments = (from a in storage.GetAppointments(range)
                                where a.CustomFields["Source"] != null
                                select a);
            ConcurrentBag<AttendanceRecorded> bag = new ConcurrentBag<AttendanceRecorded>();
            Parallel.ForEach(appointments, a =>
            {
                var thisClass = a.CustomFields["Source"] as Class;
                if (BusinessRule.IsClassInTerm(thisClass, selectedTerm.TermNumber))
                {
                    var thisStart = a.Start;
                    var isCancelled = ((int)a.LabelId == 9) ? true : false;
                    var attendanceCount = attendance.Count(x => x.ClassID == thisClass.ID &&
                                                            x.Date == thisStart);
                    var presentCount = attendance.Count(x => x.ClassID == thisClass.ID &&
                                                            x.Date == thisStart &&
                                                            (AttendClassStatusType)x.AttendClassStatusID == AttendClassStatusType.Present);
                    var absentWithCount = attendance.Count(x => x.ClassID == thisClass.ID &&
                                                            x.Date == thisStart &&
                                                            (AttendClassStatusType)x.AttendClassStatusID == AttendClassStatusType.AbsentFromClassWithApology);
                    var absentWithoutCount = attendance.Count(x => x.ClassID == thisClass.ID &&
                                                            x.Date == thisStart &&
                                                            (AttendClassStatusType)x.AttendClassStatusID == AttendClassStatusType.AbsentFromClassWithoutApology);
                    if ((GetRecorded) ||
                        (!GetRecorded && (attendanceCount == 0 || (attendanceCount > 0 && presentCount == 0))))
                    {
                        var o = new AttendanceRecorded()
                        {
                            ClassID = thisClass.ID,
                            Present = presentCount,
                            AbsentWithApology = absentWithCount,
                            AbsentWithoutApology = absentWithoutCount,
                            ClassDate = thisStart,
                            CourseDetail = $"{a.Subject}: {thisClass.LeaderSummary}",
                            CourseName = a.Subject,
                            IsCancelled = isCancelled,
                        };
                        bag.Add(o);
                    }
                }
            });

            return bag.OrderBy(x => x.CourseDetail).ThenBy(x => x.ClassDate).ToList();
        }
        public static async Task<List<ClassDate>> SelectableAttendanceDatesAsync(U3ADbContext dbc,
                    Term selectedTerm, Class selectedClass, DateTime Today)
        {
            var cutoffDate = Today.Date.AddDays(1).AddSeconds(-1);
            List<ClassDate> result = new();

            // a list of attendance records already created up till today's date
            List<ClassDate> onFileDates = await dbc.AttendClass
                                                    .Where(x => x.TermID == selectedTerm.ID
                                                                && x.ClassID == selectedClass.ID
                                                                && x.Date <= cutoffDate)
                                                    .Select(x => new ClassDate()
                                                    {
                                                        Date = x.Date,
                                                        TermStart = selectedTerm.StartDate
                                                    }).Distinct().ToListAsync();

            // Calculate the date range for class schedule calculation ( start & end)

            // the last class date for term
            var lastAllowedClassDate = await BusinessRule.GetLastAllowedClassDateForTermAsync(dbc, selectedTerm);
            var storage = await GetCourseScheduleDataStorageAsync(dbc, selectedTerm);
            // set the start date to the cutoff date
            DateTime start = cutoffDate;
            // if there are no classes between startof term & cutoff, set start to start of term.
            // this accounts for a change in term start date.
            if (!onFileDates.Any(x => x.Date >= selectedTerm.StartDate && x.Date < cutoffDate)) { start = selectedTerm.StartDate; }
            DateTime end = start.AddDays(700); // an arbitarially large number
            end = (end > start && end <= lastAllowedClassDate) ? end : lastAllowedClassDate;

            // calculate class dates
            DxSchedulerDateTimeRange range = new DxSchedulerDateTimeRange(start, end);
            result = (from a in storage.GetAppointments(range)
                      where (a.CustomFields["Source"] != null
                                && (int)a.LabelId != 9    // Cancelled/Postponed
                                && selectedClass.ID == (a.CustomFields["Source"] as Class).ID)
                      select new ClassDate() { TermStart = selectedTerm.StartDate, Date = a.Start }).ToList();

            // merge what is already on file & sort
            result.AddRange(onFileDates);
            return result.OrderBy(a => a.Date).ToList();
        }

        public static async Task<List<AttendClass>> GetAttendanceDetailAsync(U3ADbContext dbc, Term SelectedTerm, Guid ClassID, DateTime ClassDate)
        {
            Class thisClass = await dbc.Class.FindAsync(ClassID);
            Course thisCourse = await dbc.Course.FindAsync(thisClass.CourseID);
            return await EditableAttendanceAsync(dbc, SelectedTerm, thisCourse, thisClass, ClassDate);
        }

        public static async Task<List<AttendClass>> EditableAttendanceAsync(U3ADbContext dbc,
                                    Term SelectedTerm, Course SelectedCourse, Class SelectedClass, DateTime ClassDate)
        {
            List<AttendClass> attendance = await GetAttendanceAsync(dbc, SelectedTerm, SelectedClass, ClassDate);
            var enrolments = await BusinessRule.EditableEnrolmentsAsync(dbc, SelectedTerm, SelectedCourse, SelectedClass);
            AttendClass? a;
            foreach (var e in enrolments.Where(x => !x.IsWaitlisted))
            {
                if (SelectedCourse.CourseParticipationTypeID == (int?)ParticipationType.SameParticipantsInAllClasses)
                {
                    a = attendance.Where(a => a.TermID == e.TermID
                                            && e.CourseID == SelectedCourse.ID
                                            && a.Date == ClassDate
                                            && a.PersonID == e.PersonID).FirstOrDefault();
                }
                else
                {
                    a = attendance.Where(a => a.TermID == e.TermID
                                            && a.ClassID == e.ClassID
                                            && a.Date == ClassDate
                                            && a.PersonID == e.PersonID).FirstOrDefault();
                }
                if (a == null)
                {
                    a = new AttendClass()
                    {
                        TermID = SelectedTerm.ID,
                        Term = await dbc.Term.FindAsync(SelectedTerm.ID),
                        ClassID = SelectedClass.ID,
                        Class = await dbc.Class.FindAsync(SelectedClass.ID),
                        Date = ClassDate,
                        Person = await dbc.Person.FindAsync(e.PersonID),
                        PersonID = e.PersonID,
                        AttendClassStatus = await dbc.AttendClassStatus.FindAsync((int)AttendClassStatusType.AbsentFromClassWithoutApology),
                        AttendClassStatusID = (int)AttendClassStatusType.AbsentFromClassWithoutApology,
                        Comment = String.Empty
                    };
                    a.Class.Venue = await dbc.Venue.FindAsync(a.Class.VenueID);
                    a.Class.Leader = await dbc.Person.FindAsync(a.Class.LeaderID);
                    a.Class.OnDay = await dbc.WeekDay.FindAsync(a.Class.OnDayID);
                    a.Class.Occurrence = await dbc.Occurrence.FindAsync(a.Class.OccurrenceID);
                    a.Class.Course = await dbc.Course.FindAsync(a.Class.CourseID);
                    a.Class.Course.CourseType = await dbc.CourseType.FindAsync(a.Class.Course.CourseTypeID);
                    a.Class.Course.CourseParticipationType = await dbc.CourseParticpationType.FindAsync(a.Class.Course.CourseParticipationTypeID);
                    attendance.Add(a);
                    await dbc.AttendClass.AddAsync(a);
                }
            }
            await dbc.SaveChangesAsync();
            return await GetAttendanceAsync(dbc, SelectedTerm, SelectedClass, ClassDate);
        }

        private static async Task<List<AttendClass>> GetAttendanceAsync(U3ADbContext dbc,
                                    Term SelectedTerm, Class SelectedClass, DateTime ClassDate)
        {
            return await GetAttendance(dbc, SelectedTerm)
                            .Where(x => x.TermID == SelectedTerm.ID
                                            && x.ClassID == SelectedClass.ID
                                            && x.Date == ClassDate)
                            .OrderBy(x => x.Person.LastName)
                                        .ThenBy(x => x.Person.FirstName)
                            .ToListAsync();
        }
        public static IQueryable<AttendClass> GetAttendanceSummary(U3ADbContext dbc, int Year)
        {
            return dbc.AttendClass
                            .Include(x => x.Term)
                            .Include(x => x.Class)
                            .Include(x => x.Class).ThenInclude(x => x.Course).ThenInclude(x => x.CourseType)
                            .Include(x => x.AttendClassStatus)
                            .Where(x => x.Term.Year == Year);
        }

        public static IQueryable<AttendClass> GetAttendance(U3ADbContext dbc, Term SelectedTerm)
        {
            return dbc.AttendClass
                            .Include(x => x.Term)
                            .Include(x => x.Class)
                            .Include(x => x.Class).ThenInclude(x => x.Venue)
                            .Include(x => x.Class).ThenInclude(x => x.Leader)
                            .Include(x => x.Class).ThenInclude(x => x.OnDay)
                            .Include(x => x.Class).ThenInclude(x => x.Occurrence)
                            .Include(x => x.Class).ThenInclude(x => x.Course).ThenInclude(x => x.CourseType)
                            .Include(x => x.Class).ThenInclude(x => x.Course).ThenInclude(x => x.CourseParticipationType)
                            .Include(x => x.Person)
                            .Include(x => x.AttendClassStatus)
                            .Where(x => x.TermID == SelectedTerm.ID);
        }
        public static IQueryable<AttendClass> GetAttendance(U3ADbContext dbc, IEnumerable<Guid> peopleID)
        {
            return dbc.AttendClass
                            .Include(x => x.Term)
                            .Include(x => x.Class)
                            .Include(x => x.Class).ThenInclude(x => x.Venue)
                            .Include(x => x.Class).ThenInclude(x => x.Leader)
                            .Include(x => x.Class).ThenInclude(x => x.OnDay)
                            .Include(x => x.Class).ThenInclude(x => x.Occurrence)
                            .Include(x => x.Class).ThenInclude(x => x.Course).ThenInclude(x => x.CourseType)
                            .Include(x => x.Class).ThenInclude(x => x.Course).ThenInclude(x => x.CourseParticipationType)
                            .Include(x => x.Person)
                            .Include(x => x.AttendClassStatus)
                            .Where(x => peopleID.Contains(x.PersonID));
        }

        public static async Task<List<AttendClass>> GetAttendanceAsync(U3ADbContext dbc,
                                Term SelectedTerm, DateTime PeriodEndDate)
        {
            var venues = await dbc.Venue.ToListAsync();
            var persons = await dbc.Person.ToListAsync();
            var courses = await dbc.Course.Include(x => x.CourseParticipationType).ToListAsync();
            var courseTypes = await dbc.CourseType.ToListAsync();
            var classes = await dbc.Class
                            .Include(x => x.Occurrence)
                            .Include(x => x.OnDay)
                            .ToListAsync();
            Parallel.ForEach(courses, c => { c.CourseType = courseTypes.Find(x => x.ID == c.CourseTypeID); });
            Parallel.ForEach(classes, c =>
            {
                if (c.Leader != null) { c.Leader = persons.Find(x => x.ID == c.LeaderID); }
                if (c.Venue != null) { c.Venue = venues.Find(x => x.ID == c.VenueID); }
            });
            var attendClass = await dbc.AttendClass
                                .Include(x => x.AttendClassStatus)
                                .Where(x => x.TermID == SelectedTerm.ID && x.Date <= PeriodEndDate).ToListAsync();
            Parallel.ForEach(attendClass, a =>
            {
                a.Term = SelectedTerm;
                a.Person = persons.Find(x => x.ID == a.PersonID);
                a.Class = classes.Find(x => x.ID == a.ClassID);
            });
            return attendClass;
        }
    }
}