using DevExpress.Blazor;
using DevExpress.Xpo.DB;
using Eway.Rapid.Abstractions.Response;
using Microsoft.EntityFrameworkCore;
using System;
using System.Runtime.CompilerServices;
using System.Text;
using Twilio.TwiML.Voice;
using U3A.Database;
using U3A.Model;

namespace U3A.BusinessRules
{
    public static partial class BusinessRule
    {

        public static async Task<List<SignInOrOut>> GetClassesToSignInAsync(U3ADbContext dbc, Person person, DateTime DateNow)
        {
            var result = new List<SignInOrOut>();
            var start = new DateTime(DateNow.Year, DateNow.Month, DateNow.Day);
            var end = new DateTime(DateNow.Year, DateNow.Month, DateNow.Day, 23, 59, 59);

            // Find the term in which today's date falls.
            var term = FindTerm(dbc, start);
            if (term == null) { return null; }

            // Get all classes for today
            var ClassesToday = await GetClassesToday(dbc, start, term);
            if (ClassesToday == null) { return null; }

            // find the attendance for this person
            foreach (var c in ClassesToday)
            {
                //force the population of attendance records
                var classStart = new DateTime(start.Year, start.Month, start.Day, c.StartTime.Hour, c.StartTime.Minute, 0);
                _ = await BusinessRule.EditableAttendanceAsync(dbc, term, c.Course, c, classStart);
                var a = await dbc.AttendClass
                           .FirstOrDefaultAsync(x => x.TermID == term.ID &&
                                                    x.ClassID == c.ID &&
                                                    x.Date == classStart &&
                                                    x.PersonID == person.ID);
                if (a == null) { continue; }
                var e = ((await BusinessRule.EditableEnrolmentsAsync(dbc, term, c.Course, c))
                        .Where(x => x.PersonID == person.ID)).FirstOrDefault();
                var onfile = result.Any(x => x.Class.ID == c.ID &&
                                            x.AttendClass.ID == a.ID &&
                                            x.Enrolment.ID == e.ID);
                if (!onfile) result.Add(new SignInOrOut()
                {
                    Class = c,
                    AttendClass = a,
                    Enrolment = e
                });
            }
            return result;
        }

        static async Task<List<Class>> GetClassesToday(U3ADbContext dbc, DateTime DateNow, Term term)
        {

            // Get the schedule storage for the current term
            var storage = await GetCourseScheduleDataStorageAsync(dbc, term);

            // find today's classes starting from now
            var end = new DateTime(DateNow.Year, DateNow.Month, DateNow.Day, 23, 59, 59);
            var range = new DxSchedulerDateTimeRange(DateNow, end);
            return (from a in storage.GetAppointments(range)
                    where (a.CustomFields["Source"] != null
                              && (int)a.LabelId != 9) // Cancelled/Postponed
                    select a.CustomFields["Source"] as Class).ToList();

        }
        public static async Task<List<MemberClassToday>> GetMemberClassesToday(U3ADbContext dbc, Person person, DateTime DateNow)
        {
            if (person == null) return null;
            var result = new List<MemberClassToday>();

            // Find the term in which today's date falls.
            var term = FindTerm(dbc, DateNow.Date);
            if (term == null) { return null; }

            // Get the schedule storage for the current term
            var storage = await GetCourseScheduleDataStorageAsync(dbc, term);

            // find today's classes starting from now
            var end = new DateTime(DateNow.Year, DateNow.Month, DateNow.Day, 23, 59, 59);
            var range = new DxSchedulerDateTimeRange(DateNow, end);
            var classes = (from a in storage.GetAppointments(range)
                           where (a.CustomFields["Source"] != null)
                           select new
                           {
                               Class = a.CustomFields["Source"] as Class,
                               Label = (int)a.LabelId
                           }).ToList();
            if (classes == null || classes.Count() <= 0) { return null; }
            // find the enrolments for this person
            foreach (var c in classes)
            {
                var addClass = false;
                var e = ((await BusinessRule.EditableEnrolmentsAsync(dbc, term, c.Class.Course, c.Class))
                        .Where(x => x.PersonID == person.ID)).FirstOrDefault();
                if (e != null) { addClass = true; }
                if (c.Class.LeaderID == person.ID) { addClass = true; }
                if (c.Class.Leader2ID == person.ID) { addClass = true; }
                if (c.Class.Leader3ID == person.ID) { addClass = true; }
                if (addClass)
                {
                    result.Add(new MemberClassToday()
                    {
                        IsCancelled = (c.Label == 9) ? true : false,
                        Name = c.Class.Course.Name,
                        StartTime = ((c.Label == 9)
                                    ? "Cancelled"
                                    : c.Class.StartTime.ToString("hh:mmtt"))
                    });
                }
            }
            return (result.Count() > 0) ? result : null;
        }

    }

    public class MemberClassToday
    {
        public bool IsCancelled { get; set; }
        public string Name { get; set; }
        public string StartTime { get; set; }
    }
    public class SignInOrOut
    {
        public Class Class { get; set; }
        public Enrolment Enrolment { get; set; }
        public AttendClass AttendClass { get; set; }

        public string AbsentReason
        {
            get { return AttendClass.Comment; }
            set { AttendClass.Comment = value; }
        }
        public string Name
        {
            get { return Class.Course.Name; }
        }
        public string Period
        {
            get { return $"{Class.StartTime.ToString("hh:mm tt")} to {Class.EndTime?.ToString("hh:mm tt")}"; }
        }
        public string Venue
        {
            get { return Class.Venue?.Name; }
        }
        public string Address
        {
            get { return Class.Venue?.Address; }
        }
        public string Leader
        {
            get { return Class.Leader?.FullName; }
        }
        public string SignedIn
        {
            get
            {
                return (AttendClass.SignIn == null)
                    ? "" : AttendClass.SignIn.Value.ToString("dd-MMM-yyyy hh:mm tt");
            }
        }
        public string SignedOut
        {
            get
            {
                return (AttendClass.SignOut == null)
                    ? "" : AttendClass.SignOut.Value.ToString("dd-MMM-yyyy hh:mm tt");
            }
        }
    }
}