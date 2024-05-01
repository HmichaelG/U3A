using DevExpress.Blazor.Internal;
using Eway.Rapid.Abstractions.Response;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text;
using U3A.Database;
using U3A.Database.Migrations.U3ADbContextSeedMigrations;
using U3A.Model;

namespace U3A.BusinessRules
{
    public static partial class BusinessRule
    {
        public static async Task<List<Course>> EditableCoursesAsync(U3ADbContext dbc, int Year)
        {
            var Courses = await dbc.Course
                        .Include(x => x.CourseType)
                        .Include(x => x.CourseParticipationType)
                        .Include(x => x.Classes).ThenInclude(x => x.Venue)
                        .Include(x => x.Classes).ThenInclude(x => x.OnDay)
                        .Include(x => x.Classes).ThenInclude(x => x.Leader)
                        .Include(x => x.Classes).ThenInclude(x => x.Leader2)
                        .Include(x => x.Classes).ThenInclude(x => x.Leader3)
                        .Include(x => x.Classes).ThenInclude(x => x.Occurrence)
                        .Where(x => x.Year == Year)
                        .OrderBy(x => x.Name)
                        .ToListAsync();
            foreach (var course in Courses)
            {
                course.Classes = course.Classes.OrderBy(x => x.StartDate).ThenBy(x => x.OnDayID).ThenBy(x => x.StartTime).ToList();
            }
            return Courses;
        }
        public static async Task<List<Course>> EditableDeletedCoursesAsync(U3ADbContext dbc, int Year)
        {
            var Courses = await dbc.Course.IgnoreQueryFilters()
                        .Include(x => x.CourseType)
                        .Include(x => x.CourseParticipationType)
                        .Include(x => x.Classes).ThenInclude(x => x.Venue)
                        .Include(x => x.Classes).ThenInclude(x => x.OnDay)
                        .Include(x => x.Classes).ThenInclude(x => x.Leader)
                        .Include(x => x.Classes).ThenInclude(x => x.Leader2)
                        .Include(x => x.Classes).ThenInclude(x => x.Leader3)
                        .Include(x => x.Classes).ThenInclude(x => x.Occurrence)
                        .Include(x => x.Enrolments)
                        .Where(x => x.Year == Year && x.IsDeleted)
                        .OrderBy(x => x.Name).ThenByDescending(x => x.DeletedAt)
                        .ToListAsync();
            foreach (var course in Courses)
            {
                course.Classes = course.Classes.OrderBy(x => x.StartDate).ThenBy(x => x.OnDayID).ThenBy(x => x.StartTime).ToList();
            }
            return Courses;
        }

        public static async Task<List<Course>> SelectableCoursesAsync(U3ADbContext dbc, int Year)
        {
            var Courses = await dbc.Course
                        .Include(x => x.CourseType)
                        .Include(x => x.CourseParticipationType)
                        .Include(x => x.Classes).ThenInclude(x => x.Venue)
                        .Include(x => x.Classes).ThenInclude(x => x.OnDay)
                        .Include(x => x.Classes).ThenInclude(x => x.Leader)
                        .Include(x => x.Classes).ThenInclude(x => x.Leader2)
                        .Include(x => x.Classes).ThenInclude(x => x.Leader3)
                        .Include(x => x.Classes).ThenInclude(x => x.Occurrence)
                        .Where(x => x.Year == Year)
                        .OrderBy(x => x.Name)
                        .ToListAsync();
            foreach (var course in Courses)
            {
                course.Classes = course.Classes.OrderBy(x => x.StartDate).ThenBy(x => x.OnDayID).ThenBy(x => x.StartTime).ToList();
            }
            return Courses;
        }
        public static async Task<List<Course>> SelectableModifiedCourseAndClassesAsync(U3ADbContext dbc,
                        DateTime DateModified)
        {
            DateTime start = DateModified.AddHours(-24);
            DateTime end = DateModified;
            var Courses = new List<Course>();
            var classes = await dbc.Class
                        .Include(x => x.Course)
                        .Where(x => (x.CreatedOn >= start && x.CreatedOn < end) ||
                                    (x.UpdatedOn >= start & x.UpdatedOn < end)).ToListAsync();
            foreach (var c in classes)
            {
                var course = await dbc.Course
                                .Include(x => x.Classes).ThenInclude(x => x.Venue)
                                .Include(x => x.Classes).ThenInclude(x => x.OnDay)
                                .Include(x => x.Classes).ThenInclude(x => x.Leader)
                                .Include(x => x.Classes).ThenInclude(x => x.Leader2)
                                .Include(x => x.Classes).ThenInclude(x => x.Leader3)
                                .Include(x => x.Classes).ThenInclude(x => x.Occurrence)
                                .FirstOrDefaultAsync(x => x.ID == c.CourseID);
                if (course != null) Courses.Add(c.Course);
            }
            foreach (var crs in await dbc.Course
                            .Where(x => (x.CreatedOn >= start && x.CreatedOn < end) ||
                                        (x.UpdatedOn >= start & x.UpdatedOn < end)).ToListAsync())
            {
                if (!Courses.Any(x => x.ID == crs.ID)) Courses.Add(crs);
            }
            return Courses;
        }
        public static List<Course> SelectableCourses(U3ADbContext dbc, Term term)
        {
            var courses = dbc.Course.AsNoTracking()
                        .Include(x => x.CourseType)
                        .Include(x => x.Enrolments).ThenInclude(x => x.Person)
                        .Include(x => x.CourseParticipationType)
                        .Include(x => x.Classes).ThenInclude(x => x.Venue)
                        .Include(x => x.Classes).ThenInclude(x => x.OnDay)
                        .Include(x => x.Classes).ThenInclude(x => x.Leader)
                        .Include(x => x.Classes).ThenInclude(x => x.Leader2)
                        .Include(x => x.Classes).ThenInclude(x => x.Leader3)
                        .Include(x => x.Classes).ThenInclude(x => x.Occurrence)
                        .Where(x => x.Year == term.Year & x.Classes.Any())
                        .OrderBy(x => x.Name)
                        .ToList();
            var termEnrolments = dbc.Enrolment.Where(x => x.TermID == term.ID).ToList();
            Parallel.ForEach(courses, course =>
            {
                Parallel.ForEach(course.Classes, c =>
                {
                    SetCourseParticipationDetails(dbc, c, termEnrolments);
                });
            });
            return courses;
        }
        public static async Task<List<Course>> SelectableCoursesForLeader(U3ADbContext dbc,
                                            Term term, Person Leader)
        {
            var allCourses = await SelectableCoursesByTermAsync(dbc, term.Year, term.TermNumber);
            var courses = new List<Course>();
            bool isCourseLeader;
            foreach (var course in allCourses)
            {
                foreach (var c in course.Classes)
                {
                    isCourseLeader = false;
                    if (c.Leader != null && Leader.ID == c.LeaderID)
                    {
                        isCourseLeader = true;
                    }
                    if (c.Leader != null && Leader.ID == c.Leader2ID)
                    {
                        isCourseLeader = true;
                    }
                    if (c.Leader != null && Leader.ID == c.Leader3ID)
                    {
                        isCourseLeader = true;
                    }
                    if (isCourseLeader && !courses.Contains(course)) { courses.Add(course); }
                }
            }
            foreach (var course in await GetClassDetailsForClerk(dbc, Leader, term))
            {
                if (!courses.Contains(course)) { courses.Add(course); }
            }
            foreach (var course in courses)
            {
                AssignClassClerks(dbc, term, course.Classes);
            }
            return courses.OrderBy(x => x.Name).ToList();
        }

        public static async Task<List<Course>> GetClassDetailsForClerk(U3ADbContext dbc, Person Student, Term term)
        {
            List<Course> result;
            result = await dbc.Course
                                .Include(c => c.Enrolments)
                                .Where(c => c.Enrolments.Any(e => e.PersonID == Student.ID &&
                                    e.TermID == term.ID &&
                                    e.IsCourseClerk && !e.IsWaitlisted)).ToListAsync();
            return result;
        }

        public static List<Person> SelectableCourseLeaders(Course SelectedCourse, Class? SelectedClass)
        {
            List<Person> result = new List<Person>();
            List<Class> classes = new List<Class>();
            if (SelectedClass != null)
            {
                classes.Add(SelectedClass);
            }
            else
            {
                classes.AddRange(SelectedCourse.Classes);
            }
            foreach (var c in classes)
            {
                if (c.Leader != null && !result.Any(x => x.ID == c.LeaderID))
                {
                    result.Add(c.Leader);
                }
                if (c.Leader2 != null && !result.Any(x => x.ID == c.Leader2ID))
                {
                    result.Add(c.Leader2);
                }
                if (c.Leader3 != null && !result.Any(x => x.ID == c.Leader3ID))
                {
                    result.Add(c.Leader3);
                }
            }
            return result;
        }

        public static Class SetCourseParticipationDetails(U3ADbContext dbc, Class Class, IEnumerable<Enrolment> termEnrolments)
        {
            double maxStudents = Class.Course.MaximumStudents; ;
            Class.ParticipationRate = 0;
            if (Class.Course.CourseParticipationTypeID == (int?)ParticipationType.SameParticipantsInAllClasses)
            {
                Class.TotalActiveStudents = termEnrolments.Where(x => x.CourseID == Class.CourseID &&
                                                    !x.IsWaitlisted).Count();
                Class.TotalWaitlistedStudents = termEnrolments.Where(x => x.CourseID == Class.CourseID &&
                                                    x.IsWaitlisted).Count();
            }
            else
            {
                Class.TotalActiveStudents = termEnrolments.Where(x => x.ClassID == Class.ID &&
                                                !x.IsWaitlisted).Count();
                Class.TotalWaitlistedStudents = termEnrolments.Where(x => x.ClassID == Class.ID
                                                && x.IsWaitlisted).Count();
            }
            if (maxStudents != 0) Class.ParticipationRate = (double)((Class.TotalActiveStudents + Class.TotalWaitlistedStudents) / maxStudents);
            return Class;
        }

        public static async Task<List<Course>> SelectableCoursesByTermAsync(U3ADbContext dbc, int Year, int TermNumber)
        {
            var courses = await SelectableCoursesAsync(dbc, Year);
            switch (TermNumber)
            {
                case 1:
                    courses = (from p in courses
                               where p.Classes.Any(c => c.OfferedTerm1 == true)
                               select p).ToList();
                    break;
                case 2:
                    courses = (from p in courses
                               where p.Classes.Any(c => c.OfferedTerm2 == true)
                               select p).ToList();
                    break;
                case 3:
                    courses = (from p in courses
                               where p.Classes.Any(c => c.OfferedTerm3 == true)
                               select p).ToList();
                    break;
                case 4:
                    courses = (from p in courses
                               where p.Classes.Any(c => c.OfferedTerm4 == true)
                               select p).ToList();
                    break;
            }
            return courses;
        }

        public static async Task<List<CourseParticipationType>> SelectableCourseParticipationTypesAsync(U3ADbContext dbc)
        {
            return await dbc.CourseParticpationType.AsNoTracking().ToListAsync();
        }
        public static async Task<string> DuplicateMarkUpAsync(U3ADbContext dbc, Course course)
        {
            string result = String.Empty;
            Course? duplicate = await DuplicateCourse(dbc, course);
            if (duplicate != null)
            {
                result = $"<p>The course [{duplicate.Name.Trim()}] is already on file.</p>";
            }
            return result;
        }

        public static async Task<bool> IsCourseNumberUnique(U3ADbContext dbc, Course course, int Year)
        {
            if (course.ConversionID == 0) return true;
            return !await dbc.Course.Where(x =>
                            x.ConversionID == course.ConversionID && x.Year == Year &&
                            x.ID != course.ID).AnyAsync();
        }
        static async Task<Course?> DuplicateCourse(U3ADbContext dbc, Course course)
        {
            return await dbc.Course.AsNoTracking()
                            .Where(x => x.ID != course.ID &&
                                        (x.Year == course.Year &&
                                            x.Name.Trim().ToUpper() == course.Name.Trim().ToUpper())).FirstOrDefaultAsync();
        }
    }
}