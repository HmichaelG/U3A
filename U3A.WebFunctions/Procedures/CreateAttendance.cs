using DevExpress.Blazor;
using Serilog;
using System.Data;
using U3A.BusinessRules;
using U3A.Database;
using U3A.Model;

namespace U3A.WebFunctions.Procedures
{
    public static class CreateAttendance
    {
        public static async Task Process(TenantInfo tenant)
        {
            using U3ADbContext dbc = new(tenant);
            dbc.UtcOffset = await Common.GetUtcOffsetAsync(dbc);
            DateTime today = await Common.GetTodayAsync(dbc);
            Term? term = await BusinessRule.FindTermAsync(dbc, today);
            if (term == null) { return; }

            // Get the schedule storage for the current term
            DxSchedulerDataStorage storage = await BusinessRule.GetCourseScheduleDataStorageAsync(dbc, term);

            // find today's classes starting from now
            DateTime end = today.AddDays(1);
            DxSchedulerDateTimeRange range = new(today, end);
            List<Class> classes = (from a in storage.GetAppointments(range)
                                   where a.CustomFields["Source"] != null
                                             && (int)a.LabelId != 9 // Cancelled/Postponed
                                   select a.CustomFields["Source"] as Class).ToList();
            if (classes?.Any() == true)
            {
                Log.Information(">>>> Create today's attendance records <<<");
                foreach (Class c in classes)
                {
                    Course? course = await dbc.Course.FindAsync(c.CourseID);
                    if (course != null)
                    {
                        if (course.IsOffScheduleActivity)
                        {
                            Log.Information($"{course.Name} at {c.StartTime:t} not created -  Off Schedule Activity.");
                        }
                        else
                        {
                            DateTime classDate = new(today.Year, today.Month, today.Day,
                                                    c.StartTime.Hour, c.StartTime.Minute, 0);
                            List<AttendClass> attendance = await BusinessRule.EditableAttendanceAsync(dbc, term, c.Course, c, classDate);
                            await ApplyStudentLeaveAsync(dbc, attendance, course);
                            Log.Information($"{course.Name} at {c.StartTime:t} created.");
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
            foreach (AttendClass? ac in ClassAttendance.Where(x => x.DateProcessed == null))
            {
                Leave leave = await BusinessRule.GetLeaveForPersonForCourseForClass(dbc, ac.Person, course, ac.Date.Date);
                if (leave != null)
                {
                    ac.AttendClassStatusID = (int)AttendClassStatusType.AbsentFromClassWithApology;
                    ac.AttendClassStatus = await dbc.AttendClassStatus.FindAsync(ac.AttendClassStatusID) ?? new();
                    ac.DateProcessed = now;
                    ac.Comment = leave.Reason;
                }
            }
        }

    }
}