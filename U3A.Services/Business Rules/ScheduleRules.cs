using Microsoft.EntityFrameworkCore;
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
using Microsoft.AspNetCore.Http;
using U3A.Database.Migrations.TenantStoreDb;
using DevExpress.XtraRichEdit.Import.OpenXml;

namespace U3A.BusinessRules
{
    public static partial class BusinessRule
    {
        public static List<Class> RestoreClassesFromSchedule(U3ADbContext dbc,
                                    TenantDbContext dbcT,
                                    Term term, SystemSettings settings,
                                    bool exludeOffScheduleActivities)
        {
            Task<List<Class>> syncTask = Task.Run(async () =>
            {
                return await RestoreClassesFromScheduleAsync(dbc, dbcT, term, settings, exludeOffScheduleActivities, true);
            });
            syncTask.Wait();
            return syncTask.Result;
        }
        public static async Task<List<Class>> RestoreClassesFromScheduleAsync(U3ADbContext dbc,
                                                    TenantDbContext dbcT,
                                                    Term term, SystemSettings settings,
                                                    bool exludeOffScheduleActivities,
                                                    bool IsFinancial)
        {
            var classes = new ConcurrentBag<Class>();
            // get the first recorded schedule
            var firstSchedule = await dbc.Schedule.AsNoTracking()
                                        .OrderBy(x => x.CreatedOn)
                                        .FirstOrDefaultAsync();
            TenantInfo tInfo = await dbcT.TenantInfo
                                    .FirstOrDefaultAsync(x => x.Identifier == firstSchedule.TenantIdentifier);
            // Get Class updates since cache creation
            foreach (var c in await GetClassDetailsAsync(dbc, term, settings, exludeOffScheduleActivities, firstSchedule.UpdatedOn))
            {
                classes.Add(c);
            }
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
                var c = JsonSerializer.Deserialize<Class>(s.jsonClass.Unzip());
                if (!exludeOffScheduleActivities || !c.Course.IsOffScheduleActivity)
                {
                    c.Enrolments = JsonSerializer.Deserialize<List<Enrolment>>(s.jsonClassEnrolments.Unzip());
                    c.Course.Enrolments = JsonSerializer.Deserialize<List<Enrolment>>(s.jsonCourseEnrolments.Unzip());
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
            // Remove deleted classes from cache
            var liveKeys = await dbc.Class
                            .AsNoTracking()
                            .Include(x => x.Course)
                            .Where(x => x.Course.Year == term.Year).Select(x => x.ID).ToListAsync();
            var deletions = new List<Class>();
            Parallel.ForEach(classes, c =>
            {
                if (!liveKeys.Contains(c.ID)) { deletions.Add(c); }
            });
            List<Class> result = classes.Except(deletions).ToList();


            // Get the multi-campus classes, only for financial members
            if (IsFinancial)
            {
                classes.Clear();
                var tenantInfo = await dbcT.TenantInfo.ToListAsync();
                var mcSchedule = await dbcT.ScheduleCache
                                    .AsNoTracking()
                                    .Where(x => x.TenantIdentifier != tInfo.Identifier)
                                    .ToListAsync();
                Parallel.ForEach(mcSchedule, async s =>
                {
                    var c = JsonSerializer.Deserialize<Class>(s.jsonClass.Unzip());
                    var id = result.FirstOrDefault(x => x.ID == c.ID);
                    if (id == null)
                    {
                        var mcTenant = tenantInfo.FirstOrDefault(x => x.Identifier == s.TenantIdentifier);
                        if (mcTenant != null) { c.Course.SponsoredBy = mcTenant.Name; }
                        classes.Add(c);
                    }
                });

                result.AddRange(classes);
            }

            // sort & return the result
            return result.OrderBy(x => x.OnDayID).ThenBy(x => x.Course.Name)
                .ToList();
        }
        public static async Task BuildScheduleAsync(U3ADbContext dbc,
                                        IDbContextFactory<TenantDbContext> TenantDbFactory,
                                        IHttpContextAccessor httpContextAccessor)
        {
            var tenantInfo = await BusinessRule.GetTenantInfoAsync(TenantDbFactory, httpContextAccessor);
            using (var dbcT = await TenantDbFactory.CreateDbContextAsync())
            {
                await BuildScheduleAsync(dbc, dbcT, tenantInfo.Identifier);
            }
        }
        public static async Task BuildScheduleAsync(U3ADbContext dbc,
                                                        TenantDbContext dbcT,
                                                        string TenantIdentifier)
        {
            List<ScheduleCache> schedules = new List<ScheduleCache>();
            List<ScheduleCache> multiCampusSchedules = new List<ScheduleCache>();
            var settings = await dbc.SystemSettings.OrderBy(x => x.ID).FirstOrDefaultAsync();
            var term = BusinessRule.CurrentEnrolmentTerm(dbc);
            if (term != null)
            {
                var classes = await BusinessRule.GetClassDetailsAsync(dbc, term, settings);
                foreach (var c in classes)
                {
                    schedules.Add(processClasses(c, settings, TenantIdentifier));
                    var now = TimezoneAdjustment.GetLocalTime().Date;
                    if (c.Course.AllowMultiCampsuFrom != null
                            && c.Course.AllowMultiCampsuFrom >= TimezoneAdjustment.GetLocalTime().Date
                            && !c.Course.IsOffScheduleActivity)
                    {
                        multiCampusSchedules.Add(processClasses(c, settings, TenantIdentifier));
                    }
                }

                // local campus
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

                //multi-campus
                await dbcT.Database.BeginTransactionAsync();
                try
                {
                    // Schedule cache
                    await dbcT.ScheduleCache.Where(x => x.TenantIdentifier == TenantIdentifier).ExecuteDeleteAsync();
                    await dbcT.ScheduleCache.AddRangeAsync(multiCampusSchedules);
                    // members
                    await UpdatePersonCache(dbc, dbcT, TenantIdentifier);
                    await dbcT.SaveChangesAsync();
                    await dbcT.Database.CommitTransactionAsync();
                }
                catch (Exception ex)
                {
                    await dbcT.Database.RollbackTransactionAsync();
                }
            }
        }

        private static async Task UpdatePersonCache(U3ADbContext dbc,
                                                        TenantDbContext dbcT,
                                                        string TenantIdentifier)
        {
            ConcurrentBag<MultiCampusPerson> deleted = new();
            ConcurrentBag<MultiCampusPerson> additions = new();
            ConcurrentBag<MultiCampusPerson> updates = new();
            var people = await BusinessRule.SelectableFinancialPeopleAsync(dbc);
            var mcPeople = await dbcT.MultiCampusPerson
                                .Where(x => x.TenantIdentifier == TenantIdentifier)
                                .ToListAsync();
            if (mcPeople != null && mcPeople.Count > 0)
            {
                Parallel.ForEach(mcPeople, async mcp =>
                {
                    if (!people.Any(x => x.ID == mcp.ID)) { deleted.Add(mcp); }
                    else
                    {
                        var p = people.FirstOrDefault(x => x.ID == mcp.ID);
                        mcp = UpdateMultiCampusPerson(mcp, p, TenantIdentifier);
                        updates.Add(mcp);
                    }
                });
            }
            if (people != null && people.Count > 0)
            {
                Parallel.ForEach(people, async p =>
                {
                    if (!mcPeople.Any(x => x.ID == p.ID))
                    {
                        var mcp = new MultiCampusPerson();
                        mcp = UpdateMultiCampusPerson(mcp, p, TenantIdentifier);
                        additions.Add(mcp);
                    }
                });
            }
            await dbcT.AddRangeAsync(additions);
            dbcT.RemoveRange(deleted);
            dbcT.UpdateRange(updates);
        }

        private static MultiCampusPerson UpdateMultiCampusPerson
                           (MultiCampusPerson mcp, Person p, string TenantIdentifier)
        {
            mcp.ID = p.ID;
            mcp.TenantIdentifier = TenantIdentifier;
            mcp.Email = p.Email;
            mcp.Communication = p.Communication;
            mcp.FirstName = p.FirstName;
            mcp.HomePhone = p.HomePhone;
            mcp.ICEContact = p.ICEContact;
            mcp.LastName = p.LastName;
            mcp.ICEPhone = p.ICEPhone;
            mcp.Mobile = p.Mobile;
            mcp.PostNominals = p.PostNominals;
            mcp.SMSOptOut = p.SMSOptOut;
            mcp.Title = p.Title;
            mcp.VaxCertificateViewed = p.VaxCertificateViewed;
            return mcp;
        }

        private static ScheduleCache processClasses(Class c, SystemSettings settings, string TenantIdentifier)
        {
            var cls = JsonSerializer.Serialize<Class>(c).Zip();
            var classEnrolments = JsonSerializer.Serialize<IEnumerable<Enrolment>>(c.Enrolments).Zip();
            var courseEnrolments = JsonSerializer.Serialize<IEnumerable<Enrolment>>(c.Course.Enrolments).Zip();
            var s = new ScheduleCache()
            {
                TenantIdentifier = TenantIdentifier,
                jsonClass = cls,
                jsonClassEnrolments = classEnrolments,
                jsonCourseEnrolments = courseEnrolments,
            };
            return s;
        }
    }
}