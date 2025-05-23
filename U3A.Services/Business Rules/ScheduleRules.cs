﻿using DevExpress.Blazor;
using DevExpress.XtraRichEdit.Import.OpenXml;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using U3A.Database;
using U3A.Database.Migrations.TenantStoreDb;
using U3A.Database.Migrations.U3ADbContextSeedMigrations;
using U3A.Model;
using U3A.Services;

namespace U3A.BusinessRules
{
    public static partial class BusinessRule
    {
        public static List<Class> RestoreClassesFromSchedule(U3ADbContext dbc,
                                    TenantDbContext dbcT,
                                    TenantInfoService tenantService,
                                    Term term, SystemSettings settings,
                                    bool excludeOffScheduleActivities)
        {
            Task<List<Class>> syncTask = Task.Run(async () =>
            {
                return await RestoreClassesFromScheduleAsync(dbc, dbcT, tenantService, term, settings, excludeOffScheduleActivities, true);
            });
            syncTask.Wait();
            return syncTask.Result;
        }
        public static async Task<List<Class>> RestoreClassesFromScheduleAsync(U3ADbContext dbc,
                                                    TenantDbContext dbcT,
                                                    TenantInfoService tenantService,
                                                    Term term, SystemSettings settings,
                                                    bool excludeOffScheduleActivities,
                                                    bool IsFinancial)
        {
            var classes = new ConcurrentBag<Class>();
            var localTenant = await tenantService.GetTenantInfoAsync();
            // get the last recorded schedule
            var lastSchedule = await dbc.Schedule.AsNoTracking()
                                        .OrderByDescending(x => x.UpdatedOn)
                                        .FirstOrDefaultAsync();
            if (lastSchedule == null) { return new List<Class>(); }

            TenantInfo tInfo = await tenantService.GetTenantInfoAsync();
            // Get all current Enrolment keys
            var enrolmentKeys = await dbc.Enrolment
                                                .AsNoTracking()
                                                .Include(x => x.Term)
                                                .Where(x => x.Term.Year == term.Year ||
                                                        (x.Term.Year == term.Year - 1 && x.Term.TermNumber == 4))
                                                .Select(x => x.ID).ToListAsync();
            enrolmentKeys.AddRange(await dbcT.MultiCampusEnrolment
                                                .AsNoTracking()
                                                .Select(x => x.ID).ToListAsync());
            // Get the new enrolments
            var newEnrolments = await dbc.Enrolment.AsNoTracking()
                                .Include(x => x.Term)
                                .Where(x => x.UpdatedOn > lastSchedule.UpdatedOn).ToListAsync();
            // get new dropouts
            var newDropouts = await dbc.Dropout.AsNoTracking()
                                .Include(x => x.Term)
                                .Where(x => x.DropoutDate > lastSchedule.UpdatedOn).ToListAsync();
            var schedule = await dbc.Schedule.AsNoTracking().ToListAsync();
            Parallel.ForEach(schedule, s =>
            {
                var c = JsonSerializer.Deserialize<Class>(s.jsonClass.Unzip());
                if (!excludeOffScheduleActivities || !c.Course.IsOffScheduleActivity)
                {
                    c.Enrolments = JsonSerializer.Deserialize<List<Enrolment>>(s.jsonClassEnrolments.Unzip());
                    c.Course.Enrolments = JsonSerializer.Deserialize<List<Enrolment>>(s.jsonCourseEnrolments.Unzip());
                    // add new enrolments
                    c.Enrolments.AddRange(newEnrolments.Where(x => x.ClassID != null && x.ClassID == c.ID));
                    c.Course.Enrolments.AddRange(newEnrolments.Where(x => x.ClassID == null && x.CourseID == c.CourseID));
                    // remove any enrolments deleted by the offering U3A
                    c.Course.Enrolments.RemoveAll(x =>
                                        !enrolmentKeys.Contains(x.ID));
                    c.Enrolments.RemoveAll(x => !enrolmentKeys.Contains(x.ID));
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
                            .Where(x => (x.Course.Year == term.Year ||
                                                x.StartDate != null && x.StartDate >= dbc.GetLocalDate()))
                            .Select(x => x.ID).ToListAsync();
            var deletions = new List<Class>();
            Parallel.ForEach(classes, c =>
            {
                if (!liveKeys.Contains(c.ID)) { deletions.Add(c); }
            });
            List<Class> result = classes.Except(deletions).ToList();

            Parallel.ForEach(result, c =>
            {
                AssignClassCounts(term, c);
            });

            // Enrolments by other U3A into local courses
            var localEnrolments = await dbcT.MultiCampusEnrolment
                                    .Where(x => x.TenantIdentifier == localTenant.Identifier)
                                    .ToListAsync();
            foreach (var mcE in localEnrolments)
            {
                MultiCampusPerson mcP = await dbcT.MultiCampusPerson.FirstOrDefaultAsync(x => x.ID == mcE.PersonID);
                MultiCampusTerm mcT = await dbcT.MultiCampusTerm.FirstOrDefaultAsync(x => x.ID == mcE.TermID);
                Class c;
                if (mcP != null && mcT != null)
                {
                    if (mcE.ClassID == null)
                    {
                        c = classes.Where(x => x.CourseID == mcE.CourseID).FirstOrDefault();
                    }
                    else
                    {
                        c = classes.Where(x => x.ID == mcE.ClassID).FirstOrDefault();
                    }
                    if (c == null) { continue; }
                    Enrolment e = GetEnrolmentFromMCEnrolment(mcE, mcP, c, mcT);
                    if (c.Course.CourseParticipationTypeID == (int)ParticipationType.SameParticipantsInAllClasses)
                    {
                        foreach (var classToUpdate in classes.Where(x => x.CourseID == c.Course.ID))
                        {
                            if (e.IsWaitlisted) { classToUpdate.TotalWaitlistedStudents++; } else { classToUpdate.TotalActiveStudents++; }
                            classToUpdate.Course.Enrolments.Add(e);
                        }
                    }
                    else
                    {
                        if (e.IsWaitlisted) { c.TotalWaitlistedStudents++; } else { c.TotalActiveStudents++; }
                        c.Enrolments.Add(e);
                    }
                }
            }

            // Other U3A Classes & Enrolments allowed by our U3A
            result.AddRange(await RestoreClassesFromMultiCampusScheduleAsync(
                                    dbc,
                                    dbcT,
                                    tenantService,
                                    term,
                                    settings));

            // sort & return the result
            return result.OrderByDescending(x => x.Course.IsFeaturedCourse).ThenBy(x => x.OnDayID).ThenBy(x => x.Course.Name)
                .ToList();
        }
        public static async Task<List<Class>> RestoreClassesFromMultiCampusScheduleAsync(U3ADbContext dbc,
                                                    TenantDbContext dbcT,
                                                    TenantInfoService tenantService,
                                                    Term term, SystemSettings settings)
        {
            List<Class> result = new();
            var classes = new ConcurrentBag<Class>();
            var localTenant = await tenantService.GetTenantInfoAsync();
            TenantInfo tInfo = await tenantService.GetTenantInfoAsync();

            // Other U3A Classes & Enrolments allowed by our U3A
            if (tInfo.EnableMultiCampusExtension && settings.AllowMultiCampusExtensions)
            {
                classes.Clear();
                var tenantInfo = await dbcT.TenantInfo.ToListAsync();
                var mcSchedule = await dbcT.MultiCampusSchedule
                                    .AsNoTracking()
                                    .Where(x => x.TenantIdentifier != tInfo.Identifier
                                                && settings.MultiCampusU3AAllowed.Contains(x.TenantIdentifier))
                                                .ToListAsync();
                Parallel.ForEach(mcSchedule, async s =>
                {
                    var c = JsonSerializer.Deserialize<Class>(s.jsonClass.Unzip());
                    c.TenantIdentifier = s.TenantIdentifier;
                    var id = result.FirstOrDefault(x => x.ID == c.ID);
                    if (id == null)
                    {
                        var mcTenant = tenantInfo.FirstOrDefault(x => x.Identifier == s.TenantIdentifier);
                        if (mcTenant != null) { c.Course.OfferedBy = mcTenant.Name; }
                        classes.Add(c);
                    }
                });

                var mcEnrolments = await dbcT.MultiCampusEnrolment
                                        .Where(x => x.TenantIdentifier != localTenant.Identifier
                                                    && settings.MultiCampusU3AAllowed.Contains(x.TenantIdentifier))
                                        .ToListAsync();
                foreach (var mcE in mcEnrolments)
                {
                    MultiCampusPerson mcP = await dbcT.MultiCampusPerson.FirstOrDefaultAsync(x => x.ID == mcE.PersonID);
                    MultiCampusTerm mcT = await dbcT.MultiCampusTerm.FirstOrDefaultAsync(x => x.ID == mcE.TermID);
                    Class c;
                    if (mcP != null && mcT != null)
                    {
                        if (mcE.ClassID == null)
                        {
                            c = classes.Where(x => x.CourseID == mcE.CourseID).FirstOrDefault();
                        }
                        else
                        {
                            c = classes.Where(x => x.ID == mcE.ClassID).FirstOrDefault();
                        }
                        if (c == null) { continue; }
                        Enrolment e = GetEnrolmentFromMCEnrolment(mcE, mcP, c, mcT);
                        if (c.Course.CourseParticipationTypeID == (int)ParticipationType.SameParticipantsInAllClasses)
                        {
                            foreach (var classToUpdate in classes.Where(x => x.CourseID == c.Course.ID))
                            {
                                if (e.IsWaitlisted) { classToUpdate.TotalWaitlistedStudents++; } else { classToUpdate.TotalActiveStudents++; }
                                classToUpdate.Course.Enrolments.Add(e);
                            }
                        }
                        else
                        {
                            if (e.IsWaitlisted) { c.TotalWaitlistedStudents++; } else { c.TotalActiveStudents++; }
                            c.Enrolments.Add(e);
                        }
                    }
                }
                result.AddRange(classes);
            }

            // sort & return the result
            return result.OrderBy(x => x.OnDayID).ThenBy(x => x.Course.Name)
                .ToList();
        }
        public static async Task BuildScheduleAsync(U3ADbContext dbc,
                                        IDbContextFactory<TenantDbContext> TenantDbFactory,
                                        string TenantIdentifier)
        {
            using (var dbcT = await TenantDbFactory.CreateDbContextAsync())
            {
                await BuildScheduleAsync(dbc, dbcT, TenantIdentifier);
            }
        }
        public static async Task BuildScheduleAsync(U3ADbContext dbc,
                                                        TenantDbContext dbcT,
                                                        string TenantIdentifier)
        {
            Log.Information("*** Building schedule for {TenantIdentifier} ***", TenantIdentifier);
            List<MultiCampusSchedule> schedules = new List<MultiCampusSchedule>();
            List<MultiCampusSchedule> multiCampusSchedules = new List<MultiCampusSchedule>();
            var settings = await dbc.SystemSettings.OrderBy(x => x.ID).FirstOrDefaultAsync();
            var term = await BusinessRule.CurrentEnrolmentTermAsync(dbc);
            if (term != null)
            {
                var now = dbc.GetLocalDate();
                var classes = await BusinessRule.GetClassDetailsAsync(dbc, term, settings);
                foreach (var c in classes)
                {
                    schedules.Add(processClasses(c, term, settings, TenantIdentifier));
                    if (c.Course.AllowMultiCampsuFrom != null
                                && now >= c.Course.AllowMultiCampsuFrom
                                && !c.Course.IsOffScheduleActivity)
                    {
                        multiCampusSchedules.Add(processClasses(c, term, settings, TenantIdentifier));
                    }
                }

                // local campus
                dbc.ChangeTracker.Clear();
                await UpdateScheduleCache(dbc, schedules, TenantIdentifier);
                await dbc.SaveChangesAsync();
                Log.Information("Schedule cache created for {TenantIdentifier}, {classes} active classes.", 
                                    TenantIdentifier, schedules.Count);

                //multi-campus
                var tInfo = await dbcT.TenantInfo.FirstOrDefaultAsync(x => x.Identifier == TenantIdentifier);
                if (tInfo != null)
                {
                    // Multi-campus extensions must be enabled at the tenant & the client level
                    if (tInfo.EnableMultiCampusExtension && settings.AllowMultiCampusExtensions)
                    {
                        // Schedule cache
                        await UpdateMultiCampusScheduleCache(dbcT, multiCampusSchedules, TenantIdentifier);
                        // Terms
                        await UpdateTermCache(dbc, dbcT, TenantIdentifier);
                        // members
                        await UpdatePersonCache(dbc, dbcT, TenantIdentifier);
                        await dbcT.SaveChangesAsync();
                        Log.Information("Multi-campus schedule cache created for {TenantIdentifier}, {classes} active classes.", 
                                            TenantIdentifier,multiCampusSchedules.Count);
                    }
                }
            }
        }

        private static async Task UpdateTermCache(U3ADbContext dbc,
                                                        TenantDbContext dbcT,
                                                        string TenantIdentifier)
        {
            ConcurrentBag<MultiCampusTerm> deleted = new();
            ConcurrentBag<MultiCampusTerm> additions = new();
            ConcurrentBag<MultiCampusTerm> updates = new();
            var Terms = await dbc.Term.ToListAsync();
            var mcTerms = await dbcT.MultiCampusTerm
                                .Where(x => x.TenantIdentifier == TenantIdentifier)
                                .ToListAsync();
            if (mcTerms != null && mcTerms.Count > 0)
            {
                Parallel.ForEach(mcTerms, async mct =>
                {
                    if (!Terms.Any(x => x.ID == mct.ID)) { deleted.Add(mct); }
                    else
                    {
                        var p = Terms.FirstOrDefault(x => x.ID == mct.ID);
                        mct = UpdateMultiCampusTerm(mct, p, TenantIdentifier);
                        updates.Add(mct);
                    }
                });
            }
            if (Terms != null && Terms.Count > 0)
            {
                Parallel.ForEach(Terms, async p =>
                {
                    if (!mcTerms.Any(x => x.ID == p.ID))
                    {
                        var mcp = new MultiCampusTerm();
                        mcp = UpdateMultiCampusTerm(mcp, p, TenantIdentifier);
                        additions.Add(mcp);
                    }
                });
            }
            await dbcT.AddRangeAsync(additions);
            dbcT.RemoveRange(deleted);
            dbcT.UpdateRange(updates);
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

        private static MultiCampusTerm UpdateMultiCampusTerm
                           (MultiCampusTerm mct, Term t, string TenantIdentifier)
        {
            mct.ID = t.ID;
            mct.Duration = t.Duration;
            mct.IsDefaultTerm = t.IsDefaultTerm;
            mct.TenantIdentifier = TenantIdentifier;
            mct.EnrolmentEnds = t.EnrolmentEnds;
            mct.EnrolmentStarts = t.EnrolmentStarts;
            mct.IsClassAllocationFinalised = t.IsClassAllocationFinalised;
            mct.StartDate = t.StartDate;
            mct.TermNumber = t.TermNumber;
            mct.Year = t.Year;
            return mct;
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
            mcp.Address = p.Address;
            mcp.City = p.City;
            mcp.State = p.State;
            mcp.Postcode = p.Postcode;
            return mcp;
        }

        private static MultiCampusSchedule processClasses(Class c, Term t, SystemSettings settings, string TenantIdentifier)
        {
            var cls = JsonSerializer.Serialize<Class>(c).Zip();
            var classEnrolments = JsonSerializer.Serialize<IEnumerable<Enrolment>>(c.Enrolments).Zip();
            var courseEnrolments = JsonSerializer.Serialize<IEnumerable<Enrolment>>(c.Course.Enrolments).Zip();
            var s = new MultiCampusSchedule()
            {
                TenantIdentifier = TenantIdentifier,
                ClassID = c.ID,
                TermId = t.ID,
                jsonClass = cls,
                jsonClassEnrolments = classEnrolments,
                jsonCourseEnrolments = courseEnrolments,
            };
            return s;
        }

        private static async Task UpdateScheduleCache(U3ADbContext dbc,
                                                IEnumerable<MultiCampusSchedule> NewSchedule,
                                                string TenantIdentifier)
        {
            ConcurrentBag<MultiCampusSchedule> deleted = new();
            ConcurrentBag<MultiCampusSchedule> additions = new();
            ConcurrentBag<MultiCampusSchedule> updates = new();
            var currentSchedule = await dbc.Schedule.ToListAsync();
            if (currentSchedule != null && currentSchedule.Count > 0)
            {
                Parallel.ForEach(currentSchedule, async s =>
                {
                    if (!NewSchedule.Any(x => x.TenantIdentifier == s.TenantIdentifier
                                            && x.ClassID == s.ClassID
                                            && x.TermId == s.TermId)) { deleted.Add(s); }
                    else
                    {
                        var newValues = NewSchedule.FirstOrDefault(x => x.TenantIdentifier == s.TenantIdentifier
                                                                    && x.ClassID == s.ClassID
                                                                    && x.TermId == s.TermId);
                        if (s.jsonClass != newValues.jsonClass
                                 || s.jsonClassEnrolments != newValues.jsonClassEnrolments
                                 || s.jsonCourseEnrolments != newValues.jsonCourseEnrolments)
                        {
                            s.jsonClass = newValues.jsonClass;
                            s.jsonClassEnrolments = newValues.jsonClassEnrolments;
                            s.jsonCourseEnrolments = newValues.jsonCourseEnrolments;
                            updates.Add(s);
                        }
                    }
                });
            }
            if (NewSchedule != null && NewSchedule.Count() > 0)
            {
                Parallel.ForEach(NewSchedule, async s =>
                {
                    if (!currentSchedule.Any(x => x.TenantIdentifier == s.TenantIdentifier
                                                                    && x.ClassID == s.ClassID
                                                                    && x.TermId == s.TermId))
                    {
                        var newSch = new MultiCampusSchedule()
                        {
                            jsonClass = s.jsonClass,
                            jsonClassEnrolments = s.jsonClassEnrolments,
                            jsonCourseEnrolments = s.jsonCourseEnrolments,
                            TermId = s.TermId,
                            ClassID = s.ClassID,
                            TenantIdentifier = s.TenantIdentifier,
                        };
                        additions.Add(newSch);
                    }
                });
            }
            await dbc.AddRangeAsync(additions);
            dbc.RemoveRange(deleted);
            dbc.UpdateRange(updates);
        }

        private static async Task UpdateMultiCampusScheduleCache(TenantDbContext dbcT,
                                                IEnumerable<MultiCampusSchedule> NewSchedule,
                                                string TenantIdentifier)
        {
            ConcurrentBag<MultiCampusSchedule> deleted = new();
            ConcurrentBag<MultiCampusSchedule> additions = new();
            ConcurrentBag<MultiCampusSchedule> updates = new();
            var currentSchedule = await dbcT.MultiCampusSchedule
                                            .Where(x => x.TenantIdentifier == TenantIdentifier)
                                            .ToListAsync();
            if (currentSchedule != null && currentSchedule.Count > 0)
            {
                foreach (var s in currentSchedule)
                {
                    if (!NewSchedule.Any(x => x.ClassID == s.ClassID
                                                && x.TermId == s.TermId)) { deleted.Add(s); }
                    else
                    {
                        var newValues = NewSchedule.FirstOrDefault(x => x.ClassID == s.ClassID
                                                                        && x.TermId == s.TermId);
                        if (s.jsonClass != newValues.jsonClass
                                 || s.jsonClassEnrolments != newValues.jsonClassEnrolments
                                 || s.jsonCourseEnrolments != newValues.jsonCourseEnrolments)
                        {
                            s.jsonClass = newValues.jsonClass;
                            s.jsonClassEnrolments = newValues.jsonClassEnrolments;
                            s.jsonCourseEnrolments = newValues.jsonCourseEnrolments;
                            updates.Add(s);
                        }
                    }
                }
            }
            if (NewSchedule != null && NewSchedule.Count() > 0)
            {
                foreach (var s in NewSchedule)
                {
                    if (!currentSchedule.Any(x => x.ClassID == s.ClassID
                                                  && x.TermId == s.TermId))
                    {
                        var newSch = new MultiCampusSchedule()
                        {
                            jsonClass = s.jsonClass,
                            jsonClassEnrolments = s.jsonClassEnrolments,
                            jsonCourseEnrolments = s.jsonCourseEnrolments,
                            TermId = s.TermId,
                            ClassID = s.ClassID,
                            TenantIdentifier = s.TenantIdentifier,
                        };
                        additions.Add(newSch);
                    }
                }
            }
            await dbcT.AddRangeAsync(additions);
            dbcT.RemoveRange(deleted);
            dbcT.UpdateRange(updates);
        }

    }

}