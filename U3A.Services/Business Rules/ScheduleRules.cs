using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using U3A.Database;
using U3A.Model;

namespace U3A.BusinessRules
{
    public static partial class BusinessRule
    {
        public static async Task BuildScheduleAsync(U3ADbContext dbc,
                    Term term, SystemSettings settings)
        {
            List<Schedule> schedules = new List<Schedule>();
            var classes = await BusinessRule.GetClassDetailsAsync(dbc, term, settings);
            foreach (var c in classes)
            {
                schedules.Add(processClasses(c, settings));
            }
            await dbc.Database.BeginTransactionAsync();
            await dbc.Database.ExecuteSqlAsync($"delete Schedule");
            await dbc.Schedule.AddRangeAsync(schedules);
            await dbc.SaveChangesAsync();
            await dbc.Database.CommitTransactionAsync();
        }

        private static Schedule processClasses(Class c, SystemSettings settings)
        {
            var s = new Schedule()
            {
                ClassID = c.ID,
                ClassSummary = c.ClassSummary,
                CourseCost = c.Course.CourseFeePerYear,
                CourseTermCost = c.Course.CourseFeePerTerm,
                CourseCostDescription = c.Course.CourseFeePerYearDescription ?? "",
                CourseCostTermDescription = c.Course.CourseFeePerTermDescription ?? "",
                CourseDescription = c.Course.Description,
                CourseID = c.CourseID,
                CourseMaximum = c.Course.MaximumStudents,
                CourseMinimu = c.Course.MaximumStudents,
                CourseName = c.Course.Name,
                CourseNumber = c.Course.ConversionID,
                CourseType = c.Course.CourseType.Name,
                IsOffScheduleActivity = c.Course.IsOffScheduleActivity,
                GuestLeader = c.GuestLeader ?? "",
                OnDayID = c.OnDayID,
                TermNumber = c.TermNumber,
                TermSummary = c.OfferedSummary,
                VenueAddress = c.Venue.Address,
                VenueName = c.Venue.Name,
            };
            var contactOrder = c.Course.CourseContactOrder ?? settings.CourseContactOrder;
            if (contactOrder == CourseContactOrder.LeadersThenClerks)
            {
                doLeaders(c, s);
                doClerks(c, s);
            }
            else
            {
                doClerks(c, s);
                doLeaders(c, s);
            }
            return s;
        }
        private static void doLeaders(Class c, Schedule s)
        {
            if (c.Leader != null)
            {
                s.LeaderName.Add(c.Leader.FullName);
                s.LeaderEmail.Add(c.Leader.AdjustedEmail ?? "");
                s.LeaderMobile.Add(c.Leader.AdjustedMobile ?? "");
                s.LeaderPhone.Add(c.Leader.AdjustedHomePhone ?? "");
                s.LeaderType.Add("Leader");
            }
            if (c.Leader2 != null)
            {
                s.LeaderName.Add(c.Leader2.FullName);
                s.LeaderEmail.Add(c.Leader2.AdjustedEmail ?? "");
                s.LeaderMobile.Add(c.Leader2.AdjustedMobile ?? "");
                s.LeaderPhone.Add(c.Leader2.AdjustedHomePhone ?? "");
                s.LeaderType.Add("Leader");
            }
            if (c.Leader3 != null)
            {
                s.LeaderName.Add(c.Leader3.FullName);
                s.LeaderEmail.Add(c.Leader3.AdjustedEmail ?? "");
                s.LeaderMobile.Add(c.Leader3.AdjustedMobile ?? "");
                s.LeaderPhone.Add(c.Leader3.AdjustedHomePhone ?? "");
                s.LeaderType.Add("Leader");
            }
        }

        private static void doClerks(Class c, Schedule s)
        {
            foreach (var p in c.Clerks)
            {
                s.LeaderName.Add(p.FullName);
                s.LeaderEmail.Add(p.AdjustedEmail ?? "");
                s.LeaderMobile.Add(p.AdjustedMobile ?? "");
                s.LeaderPhone.Add(p.AdjustedHomePhone ?? "");
                s.LeaderType.Add("Clerk");
            }
        }
    }
}