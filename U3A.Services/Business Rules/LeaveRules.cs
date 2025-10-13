using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;
using System.Text;
using U3A.Database;
using U3A.Model;

namespace U3A.BusinessRules
{
    public static partial class BusinessRule
    {
        public static List<Leave> GetLeaveForPerson(U3ADbContext dbc, Person person, DateTime StartDate)
        {
            return dbc.Leave.Include(x => x.Person)
                            .Include(x => x.Course)
                            .Where(x => x.PersonID == person.ID)
                            .OrderBy(x => x.StartDate).ToList();
        }
        public static List<Leave> GetLeaveForPersonForCourse(U3ADbContext dbc,
                                    Person person,
                                    Course course,
                                    DateTime StartDate)
        {
            return dbc.Leave.Include(x => x.Person)
                            .Include(x => x.Course)
                            .Where(x => x.PersonID == person.ID & x.CourseID == course.ID)
                            .OrderBy(x => x.StartDate).ToList();
        }


        public static async Task<Leave> GetLeaveForPersonForCourseForClass(U3ADbContext dbc,
                                    Person person,
                                    Course course,
                                    DateTime classDate)
        {
            return await dbc.Leave.FirstOrDefaultAsync(x => x.PersonID == person.ID &&
                                (x.CourseID == null || x.CourseID == course.ID) &&
                                x.StartDate.Date <= classDate &&
                                x.EndDate.Date.AddDays(1) > classDate);
        }
        public static async Task<List<Leave>> GetLeaveForCourseForClass(U3ADbContext dbc,
                                    Course course,
                                    DateTime classDate)
        {
            return await dbc.Leave
                            .Include(x => x.Person)
                            .Where(x =>
                                (x.CourseID == null || x.CourseID == course.ID) &&
                                x.StartDate.Date <= classDate &&
                                x.EndDate.Date.AddDays(1) > classDate).ToListAsync();
        }
    }
}
