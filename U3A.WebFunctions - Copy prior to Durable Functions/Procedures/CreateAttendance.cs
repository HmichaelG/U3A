using DevExpress.Blazor;
using Microsoft.Extensions.Logging;
using System.Data;
using U3A.BusinessRules;
using U3A.Database;
using U3A.Model;

namespace U3A.WebFunctions.Procedures
{
    public static class CreateAttendance
    {
        public static async Task Process(TenantInfo tenant, ILogger logger)
        {
            using (var dbc = new U3ADbContext(tenant))
            {
                dbc.UtcOffset = await Common.GetUtcOffsetAsync(dbc);
                var today = await Common.GetTodayAsync(dbc);
                var term = await BusinessRule.FindTermAsync(dbc, today);
                if (term == null) { return; }

                // Get the schedule storage for the current term
                var storage = await BusinessRule.GetCourseScheduleDataStorageAsync(dbc, term);

                // find today's classes starting from now
                var end = today.AddDays(1);
                var range = new DxSchedulerDateTimeRange(today, end);
                var classes = (from a in storage.GetAppointments(range)
                               where a.CustomFields["Source"] != null
                                         && (int)a.LabelId != 9 // Cancelled/Postponed
                               select a.CustomFields["Source"] as Class).ToList();
                if (classes?.Any() == true)
                {
                    logger.LogInformation(">>>> Create today's attendance records <<<");
                    foreach (var c in classes)
                    {
                        var course = await dbc.Course.FindAsync(c.CourseID);
                        if (course != null)
                        {
                            var classDate = new DateTime(today.Year, today.Month, today.Day,
                                                    c.StartTime.Hour, c.StartTime.Minute, 0);
                            var attendance = await BusinessRule.EditableAttendanceAsync(dbc, term, c.Course, c, classDate);
                            await ApplyStudentLeaveAsync(dbc, attendance, course);
                            logger.LogInformation($"{course.Name} at {c.StartTime.ToShortTimeString()} created.");
                        }
                    }
                }
            }
        }
        private static async Task ApplyStudentLeaveAsync(U3ADbContext dbc,
                                    List<AttendClass> ClassAttendance,
                                    Course course)
        {
            DateTime now = await Common.GetNowAsync(dbc);
            foreach (var ac in ClassAttendance.Where(x => x.DateProcessed == null))
            {
                var leave = await BusinessRule.GetLeaveForPersonForCourseForClass(dbc, ac.Person, course, ac.Date.Date);
                if (leave != null)
                {
                    ac.AttendClassStatusID = (int)AttendClassStatusType.AbsentFromClassWithApology;
                    ac.AttendClassStatus = await dbc.AttendClassStatus.FindAsync(ac.AttendClassStatusID);
                    ac.DateProcessed = now;
                    ac.Comment = leave.Reason;
                }
            }
        }

    }
}