﻿using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using U3A.Database;
using U3A.Model;
using U3A.Services;
using DevExpress.Blazor;
using System.Text.Json;

namespace U3A.BusinessRules
{
    public static partial class BusinessRule
    {
        public static List<Class> RestoreClassesFromSchedule(U3ADbContext dbc, bool exludeOffScheduleActivities)
            {
            Task<List<Class>> syncTask = Task.Run(async () =>
            {
                return await RestoreClassesFromScheduleAsync(dbc, exludeOffScheduleActivities);
            });
            syncTask.Wait();
            return syncTask.Result;
        }
        public static async Task<List<Class>> RestoreClassesFromScheduleAsync(U3ADbContext dbc, bool exludeOffScheduleActivities)
        {
            var classes = new ConcurrentBag<Class>();
            // get the first recorded schedule
            var firstSchedule = await dbc.Schedule.AsNoTracking()
                                        .OrderBy(x => x.CreatedOn)
                                        .FirstOrDefaultAsync();
            // Get the new enrolments
            var newEnrolments = await dbc.Enrolment.AsNoTracking()
                                .Include(x => x.Term)
                                .Where(x => x.Created > firstSchedule.CreatedOn).ToListAsync();
            // get new dropouts
            var newDropouts = await dbc.Dropout.AsNoTracking()
                                .Include(x => x.Term)
                                .Where(x => x.DropoutDate > firstSchedule.CreatedOn).ToListAsync();

            var schedule = await dbc.Schedule.AsNoTracking().ToListAsync();
            Parallel.ForEach(schedule, s =>
            {
                var c = JsonSerializer.Deserialize<Class>(s.jsonClass);
                if (!exludeOffScheduleActivities || !c.Course.IsOffScheduleActivity)
                {
                    c.Enrolments = JsonSerializer.Deserialize<List<Enrolment>>(s.jsonClassEnrolments);
                    c.Course.Enrolments = JsonSerializer.Deserialize<List<Enrolment>>(s.jsonCourseEnrolments);
                    // add new enrolments
                    c.Enrolments.AddRange(newEnrolments.Where(x => x.ClassID != null && x.ClassID == c.ID));
                    c.Course.Enrolments.AddRange(newEnrolments.Where(x => x.ClassID == null && x.CourseID == c.CourseID));
                    // and remove new dropouts
                    foreach (var d in newDropouts)
                    {
                        c.Enrolments.RemoveAll(x => x.ClassID != null 
                                                        && x.ClassID == d.ID
                                                        && x.PersonID == d.PersonID
                                                        && x.Created == d.Created);
                        c.Course.Enrolments.RemoveAll(x => x.ClassID == null 
                                                        && x.CourseID == d.CourseID
                                                        && x.PersonID == d.PersonID
                                                        && x.Created == d.Created);
                    }
                    classes.Add(c);
                }
            });
            return classes
                .OrderBy(x => x.OnDayID).ThenBy(x => x.Course.Name)
                .ToList();
        }
        public static async Task BuildScheduleAsync(U3ADbContext dbc)
        {
            List<Schedule> schedules = new List<Schedule>();
            var settings = await dbc.SystemSettings.OrderBy(x => x.ID).FirstOrDefaultAsync();
            var term = BusinessRule.CurrentEnrolmentTerm(dbc);
            if (term != null)
            {
                var classes = await BusinessRule.GetClassDetailsAsync(dbc, term, settings);
                foreach (var c in classes)
                {
                    schedules.Add(processClasses(c, settings));
                }
                await dbc.Database.BeginTransactionAsync();
                try
                {
                    await dbc.Schedule.ExecuteDeleteAsync();
                    await dbc.Schedule.AddRangeAsync(schedules);
                    await dbc.SaveChangesAsync();
                    await dbc.Database.CommitTransactionAsync();
                }
                catch (Exception ex)
                {
                    await dbc.Database.RollbackTransactionAsync();
                }
            }
        }

        private static Schedule processClasses(Class c, SystemSettings settings)
        {
            var cls = JsonSerializer.Serialize<Class>(c);
            var classEnrolments = JsonSerializer.Serialize<IEnumerable<Enrolment>>(c.Enrolments);
            var courseEnrolments = JsonSerializer.Serialize<IEnumerable<Enrolment>>(c.Course.Enrolments);
            var s = new Schedule()
            {
                jsonClass = cls,
                jsonClassEnrolments = classEnrolments,
                jsonCourseEnrolments = courseEnrolments,
            };
            return s;
        }
    }
}