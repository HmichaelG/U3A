﻿using DevExpress.Blazor;
using DevExpress.Utils;
using DevExpress.Utils.Serializing;
using Eway.Rapid.Abstractions.Response;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Context;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.Linq;
using System.Text;
using Twilio.Rest.Trunking.V1;
using U3A.Database;
using U3A.Model;

namespace U3A.BusinessRules
{
    public static partial class BusinessRule
    {

        public static List<string> AutoEnrolments { get; set; }

        // Are we in the period prior to the random allocation date.
        public static bool IsPreRandomAllocationDay(Term currentEnrolmentTerm,
                                            SystemSettings settings, DateTime Today)
        {
            DateTime allocationDate = GetThisTermAllocationDay(currentEnrolmentTerm, settings);
            return IsPreRandomCutoffDate(currentEnrolmentTerm, settings, allocationDate, Today);
        }

        // Are we in the period prior to the random allocation email send date.
        public static bool IsPreRandomAllocationEmailDay(Term currentEnrolmentTerm,
                                            SystemSettings settings, DateTime Today)
        {
            DateTime allocationDate = GetThisTermAllocationDay(currentEnrolmentTerm, settings);
            // add the review period
            allocationDate = allocationDate.AddDays(constants.RANDOM_ALLOCATION_PREVIEW);
            return IsPreRandomCutoffDate(currentEnrolmentTerm, settings, allocationDate, Today);
        }

        public static bool IsEnrolmentBlackoutPeriod(SystemSettings settings)
                      => settings != null && DateTime.UtcNow < settings.EnrolmentBlackoutEndsUTC.GetValueOrDefault();
        private static bool IsPreRandomCutoffDate(Term currentEnrolmentTerm,
                                            SystemSettings settings,
                                            DateTime CutoffDate,
                                            DateTime today)
        {
            bool result = false;
            var autoEnrollOccurrence = settings.AutoEnrolAllocationOccurs;
            switch (autoEnrollOccurrence)
            {
                case AutoEnrollOccurrence.Annually:
                    if (currentEnrolmentTerm.TermNumber == 1 && today < CutoffDate) result = true;
                    break;
                case AutoEnrollOccurrence.Semester:
                    if (currentEnrolmentTerm.TermNumber == 1 && today < CutoffDate) result = true;
                    if (currentEnrolmentTerm.TermNumber == 3 && today < CutoffDate) result = true;
                    break;
                case AutoEnrollOccurrence.Term:
                    if (today < CutoffDate) result = true; break;
                default: result = false; break;
            }
            return result;
        }

        public static bool IsRandomAllocationTerm(Term currentEnrolmentTerm,
                                            SystemSettings settings)
        {
            bool result = false;
            var autoEnrollOccurrence = settings.AutoEnrolAllocationOccurs;
            switch (autoEnrollOccurrence)
            {
                case AutoEnrollOccurrence.Annually:
                    if (currentEnrolmentTerm.TermNumber == 1) result = true;
                    break;
                case AutoEnrollOccurrence.Semester:
                    if (currentEnrolmentTerm.TermNumber == 1) result = true;
                    if (currentEnrolmentTerm.TermNumber == 3) result = true;
                    break;
                case AutoEnrollOccurrence.Term:
                    result = true; break;
                default: result = false; break;
            }
            return result;
        }

        // Return the date random allocation will occur, if it occurs this term.
        public static DateTime GetThisTermAllocationDay(Term currentEnrolmentTerm, SystemSettings settings)
        {
            var week = settings.AutoEnrolAllocationWeek;
            var onDay = (DayOfWeek)settings.AutoEnrolAllocationDay;
            var allocationDate = currentEnrolmentTerm.StartDate.Date.AddDays(7 * week);
            while (allocationDate.DayOfWeek != onDay)
            {
                allocationDate = allocationDate.AddDays(1);
            }
            return allocationDate;
        }

        // The day members are notified of allocation
        public static DateTime GetThisTermAllocationMailoutDay(Term currentEnrolmentTerm, SystemSettings settings)
        {
            DateTime allocationDate = GetThisTermAllocationDay(currentEnrolmentTerm, settings);
            return allocationDate.AddDays(constants.RANDOM_ALLOCATION_PREVIEW);
        }

        const double COURSE_NOT_RANKED_VALUE = 0.00000002;
        const double COURSE_RANK_NOT_REQD_VALUE = 0.00000001;
        public static async Task AutoEnrolParticipantsAsync(U3ADbContext dbc, Term SelectedTerm,
                              bool IsClassAllocationDone,
                              bool ForceEmailQueue,
                              DateTime? EmailDate = null)
        {

            using (LogContext.PushProperty("AutoEnrolParticipants", dbc.GetLocalTime().ToString("dd-MM-yyyy hh:mm tt")))
            using (LogContext.PushProperty("Tenant", dbc.TenantInfo.Identifier))
            {
                {
                    Log.Information("Auto-Enrolment Allocation log as at {DateTime:dd-MM-yyyy hh:mm:ss tt}", dbc.GetLocalTime());
                    Log.Information("===============================================================");

                    // get all class dates for the year
                    var term1 = await GetFirstTermThisYearAsync(dbc, SelectedTerm);
                    var calendar = await GetCalendarDataStorageAsync(dbc, term1);

                    // Do part paid first
                    if (await WaitListPartPaidMembers(dbc, SelectedTerm) > 0)
                    {
                        await BusinessRule.CreateEnrolmentSendMailAsync(dbc, EmailDate);
                        await dbc.SaveChangesAsync();
                    }

                    // and everybody else
                    var today = dbc.GetLocalTime().Date;
                    AutoEnrolments = new List<string>();
                    List<Enrolment> enrolmentsToProcess = new();
                    List<Enrolment> waitlistNotFinancial = new();
                    List<Person> CourseLeaders = new();
                    foreach (var kvp in (await GetRankedCourses(dbc, SelectedTerm, calendar)).Reverse())
                    {
                        int enrolledCount = 0;
                        var key = kvp.Key;
                        var course = kvp.Value;
                        bool isFutureCourse = IsFutureCourse(today, course, calendar);
                        if (key.Item1 != double.MinValue)
                        {
                            Log.Information("");
                            Log.Information("Rank: {Rank}\tMaxStudents: {MaxStudents}\t{CourseName}",
                                               (key.Item1 == COURSE_NOT_RANKED_VALUE)
                                                    ? "Not Weekly"
                                                    : (key.Item1 == COURSE_RANK_NOT_REQD_VALUE)
                                                        ? "Not Req'd"
                                                        : key.Item1.ToString("00000.00000000"),
                                                key.Item2.ToString("0000"), course.Name);
                        }

                        //A "new member" is one that has never been enrolled in a course
                        var peoplePreviouslyEnrolled = await dbc.Enrolment.AsNoTracking()
                                                    .Where(x => !x.IsWaitlisted)
                                                    .Select(x => x.PersonID)
                                                    .Distinct()
                                                    .ToListAsync();
                        CourseLeaders = new();
                        foreach (var c in course.Classes)
                        {
                            if (c.Leader != null && !CourseLeaders.Contains(c.Leader)) { CourseLeaders.Add(c.Leader); }
                            if (c.Leader2 != null && !CourseLeaders.Contains(c.Leader2)) { CourseLeaders.Add(c.Leader2); }
                            if (c.Leader3 != null && !CourseLeaders.Contains(c.Leader3)) { CourseLeaders.Add(c.Leader3); }
                        }
                        if (course.CourseParticipationTypeID == (int?)ParticipationType.SameParticipantsInAllClasses)
                        {
                            enrolmentsToProcess = await dbc.Enrolment.IgnoreQueryFilters()
                                                        .Include(x => x.Course)
                                                        .Include(x => x.Term)
                                                        .Include(x => x.Person)
                                                        .Where(x => !x.IsDeleted && !x.Person.IsDeleted &&
                                                                        (x.TermID == SelectedTerm.ID || isFutureCourse ||
                                                                        (x.Course.AllowMultiCampsuFrom != null &&
                                                                        x.Course.AllowMultiCampsuFrom <= today.AddDays(-1)))
                                                                        && x.CourseID == course.ID
                                                                        && x.Person.DateCeased == null
                                                                        && !CourseLeaders.Contains(x.Person))
                                                        .ToListAsync();

                            waitlistNotFinancial = enrolmentsToProcess.Where(x => x.IsWaitlisted && !IsPersonFinancial(x.Person, SelectedTerm)).ToList();
                            enrolmentsToProcess = enrolmentsToProcess.Where(x => IsPersonFinancial(x.Person, SelectedTerm)).ToList();
                            if (enrolmentsToProcess.Any(x => x.IsWaitlisted))
                            {
                                enrolledCount = await ProcessEnrolments(dbc,
                                                   calendar,
                                                   SelectedTerm,
                                                   course,
                                                   enrolmentsToProcess,
                                                   peoplePreviouslyEnrolled,
                                                   ForceEmailQueue);
                                await BusinessRule.CreateEnrolmentSendMailAsync(dbc, EmailDate);
                                await dbc.SaveChangesAsync();
                            }
                        }
                        else
                        {
                            foreach (var courseClass in course.Classes)
                            {
                                enrolmentsToProcess = await dbc.Enrolment.IgnoreQueryFilters()
                                                            .Include(x => x.Course)
                                                            .Include(x => x.Term)
                                                            .Include(x => x.Person)
                                                            .Where(x => !x.IsDeleted && !x.Person.IsDeleted &&
                                                                            (x.TermID == SelectedTerm.ID || isFutureCourse)
                                                                            && x.ClassID == courseClass.ID
                                                                            && x.Person.DateCeased == null
                                                                            && !CourseLeaders.Contains(x.Person))
                                                        .ToListAsync();
                                waitlistNotFinancial = enrolmentsToProcess.Where(x => x.IsWaitlisted && !IsPersonFinancial(x.Person, SelectedTerm)).ToList();
                                enrolmentsToProcess = enrolmentsToProcess.Where(x => IsPersonFinancial(x.Person, SelectedTerm)).ToList();
                                if (enrolmentsToProcess.Any(x => x.IsWaitlisted))
                                {
                                    enrolledCount += await ProcessEnrolments(dbc,
                                                          calendar,
                                                          SelectedTerm,
                                                          course,
                                                          enrolmentsToProcess,
                                                          peoplePreviouslyEnrolled,
                                                          ForceEmailQueue);
                                    await BusinessRule.CreateEnrolmentSendMailAsync(dbc, EmailDate);
                                    await dbc.SaveChangesAsync();
                                }
                            }
                        }
                        if (enrolledCount == 0)
                        {
                            var waitlist = enrolmentsToProcess.Count(e => e.IsWaitlisted);
                            var notFinancial = waitlistNotFinancial.Count();
                            Log.Information("No enrolments processed. {waitlist} remain waitlisted.", waitlist + notFinancial);
                        }
                        if (waitlistNotFinancial.Count > 0)
                        {
                            Log.Information("{waitlistNotFinancial} Not Financial enrolments remain waitlisted.", waitlistNotFinancial.Count);
                            foreach (var e in waitlistNotFinancial)
                            {
                                Log.Information("Waitlisted\tFin-To: {FinTo}\t{Student}", e.Person.FinancialToBriefText, e.Person.FullName);
                            }
                        }
                    }
                    var term = await dbc.Term.FindAsync(SelectedTerm.ID);
                    await SetClassAllocationDone(dbc, term, IsClassAllocationDone);
                    await dbc.SaveChangesAsync();
                    Log.Information("");
                }
            }
        }

        public static async Task AutoEnrolParticipantsAsync(U3ADbContext dbc, Term SelectedTerm,
                                    IEnumerable<Guid> EnrolmentIdsToProcess)
        {
            // get all class dates for the year
            var term1 = await GetFirstTermThisYearAsync(dbc, SelectedTerm);
            var calendar = await GetCalendarDataStorageAsync(dbc, term1);

            List<Enrolment> enrollments = await dbc.Enrolment.IgnoreQueryFilters()
                                    .Include(x => x.Course).ThenInclude(x => x.Classes)
                                    .Include(x => x.Term)
                                    .Include(x => x.Person)
                                    .Where(x => EnrolmentIdsToProcess.Contains(x.ID))
                                    .ToListAsync();
            if (enrollments == null || enrollments.Count == 0)
            {
                Log.Information("No enrolments to process.");
                return;
            }
            //update UpdatedOn to identify that allocation has been done
            enrollments.ForEach(x => x.UpdatedOn = DateTime.UtcNow.AddSeconds(1));
            await dbc.SaveChangesAsync();

            var today = dbc.GetLocalTime().Date;
            AutoEnrolments = new List<string>();
            List<Enrolment> enrolmentsToProcess = new();
            List<Enrolment> waitlistNotFinancial = new();
            List<Person> CourseLeaders = new();
            foreach (var enrollment in enrollments)
            {
                var course = enrollment.Course;
                var isFutureCourse = IsFutureCourse(today, course, calendar);
                if (!course.AllowAutoEnrol) { continue; }
                int enrolledCount = 0;
                CourseLeaders = new();
                foreach (var c in course.Classes)
                {
                    if (c.Leader != null && !CourseLeaders.Contains(c.Leader)) { CourseLeaders.Add(c.Leader); }
                    if (c.Leader2 != null && !CourseLeaders.Contains(c.Leader2)) { CourseLeaders.Add(c.Leader2); }
                    if (c.Leader3 != null && !CourseLeaders.Contains(c.Leader3)) { CourseLeaders.Add(c.Leader3); }
                }
                if (course.CourseParticipationTypeID == (int?)ParticipationType.SameParticipantsInAllClasses)
                {
                    enrolmentsToProcess = await dbc.Enrolment.IgnoreQueryFilters()
                                                .Include(x => x.Course)
                                                .Include(x => x.Term)
                                                .Include(x => x.Person)
                                                .Where(x => !x.IsDeleted && !x.Person.IsDeleted &&
                                                             (x.TermID == SelectedTerm.ID ||
                                                             (x.Course.AllowMultiCampsuFrom != null &&
                                                             x.Course.AllowMultiCampsuFrom <= today.AddDays(-1)))
                                                                && x.CourseID == course.ID
                                                                && x.Person.DateCeased == null
                                                                && !CourseLeaders.Contains(x.Person))
                                                .ToListAsync();
                    enrolmentsToProcess = await dbc.Enrolment.IgnoreQueryFilters()
                                                .Include(x => x.Course)
                                                .Include(x => x.Term)
                                                .Include(x => x.Person)
                                                .Where(x => !x.IsDeleted && !x.Person.IsDeleted &&
                                                             (x.TermID == SelectedTerm.ID || isFutureCourse ||
                                                             (x.Course.AllowMultiCampsuFrom != null &&
                                                             x.Course.AllowMultiCampsuFrom <= today.AddDays(-1)))
                                                                && x.CourseID == course.ID
                                                                && x.Person.DateCeased == null
                                                                && !CourseLeaders.Contains(x.Person))
                                                .ToListAsync();

                    waitlistNotFinancial = enrolmentsToProcess.Where(x => x.IsWaitlisted && !IsPersonFinancial(x.Person, SelectedTerm)).ToList();
                    enrolmentsToProcess = enrolmentsToProcess.Where(x => IsPersonFinancial(x.Person, SelectedTerm)).ToList();
                    if (enrolmentsToProcess.Any(x => x.IsWaitlisted))
                    {
                        enrolledCount = await ProcessEnrolments(dbc,
                                           calendar,
                                           SelectedTerm,
                                           course,
                                           enrolmentsToProcess);
                        await BusinessRule.CreateEnrolmentSendMailAsync(dbc);
                        await dbc.SaveChangesAsync();
                    }
                }
                else
                {
                    foreach (var courseClass in course.Classes)
                    {
                        enrolmentsToProcess = await dbc.Enrolment.IgnoreQueryFilters()
                                                    .Include(x => x.Course)
                                                    .Include(x => x.Term)
                                                    .Include(x => x.Person)
                                                    .Where(x => !x.IsDeleted && x.Person.IsDeleted &&
                                                                    (x.TermID == SelectedTerm.ID || isFutureCourse)
                                                                    && x.ClassID == courseClass.ID
                                                                    && x.Person.DateCeased == null
                                                                    && !CourseLeaders.Contains(x.Person))
                                                .ToListAsync();
                        waitlistNotFinancial = enrolmentsToProcess.Where(x => x.IsWaitlisted && !IsPersonFinancial(x.Person, SelectedTerm)).ToList();
                        enrolmentsToProcess = enrolmentsToProcess.Where(x => IsPersonFinancial(x.Person, SelectedTerm)).ToList();
                        if (enrolmentsToProcess.Any(x => x.IsWaitlisted))
                        {
                            enrolledCount += await ProcessEnrolments(dbc, calendar,
                                                  SelectedTerm,
                                                  course,
                                                  enrolmentsToProcess);
                            await BusinessRule.CreateEnrolmentSendMailAsync(dbc);
                            await dbc.SaveChangesAsync();
                        }
                    }
                }
            }
            await dbc.SaveChangesAsync();
        }

        private static async Task<SortedList<(double, int, Guid), Course>> GetRankedCourses(U3ADbContext dbc, Term term, DxSchedulerDataStorage calendar)
        {
            // rank courses by popularity
            (double rank, int maxStudents, Guid courseID) key;
            SortedList<(double, int, Guid), Course> rankedCourses = new();
            foreach (var course in await dbc.Course.AsNoTracking()
                                            .Include(x => x.Classes)
                                            .Where(x => x.Year == term.Year
                                                         && x.AllowAutoEnrol)
                                            .ToListAsync())
            {
                bool isFutureCourse = IsFutureCourse(dbc.GetLocalDate(), course, calendar);
                key = new()
                {
                    courseID = course.ID,
                    maxStudents = course.MaximumStudents,
                    rank = double.MinValue
                };
                var enrolments = await dbc.Enrolment.AsNoTracking()
                                            .Where(e => (e.TermID == term.ID || isFutureCourse)
                                                            && e.CourseID == course.ID).ToListAsync();
                int waitlisted = enrolments.Count(e => e.IsWaitlisted);
                if (course.MaximumStudents > 0 && waitlisted > 0)
                {
                    double classes = 1;
                    if (course.CourseParticipationTypeID == (int)ParticipationType.DifferentParticipantsInEachClass)
                    {
                        classes = course.Classes.Count;
                    }
                    double max = (double)course.MaximumStudents * classes;
                    double requests = enrolments.Count();
                    // Calclate the rank. Set rank to MIN_VALUE if requests > 0 & <= max OR once only class
                    // MIN_VALUE courses will be logged but will not be considered popular (constrained).
                    if (requests > max)
                    {
                        key.rank = (requests / max) * requests;
                        foreach (var c in course.Classes)
                        {
                            if (c.StartDate == null && c.OccurrenceID == (int)OccurrenceType.Weekly)
                            {
                                continue;
                            }
                            key.rank = COURSE_NOT_RANKED_VALUE; break;
                        }
                    }
                    else
                    {
                        key.rank = COURSE_RANK_NOT_REQD_VALUE;
                    }
                }
                if (key.rank != double.MinValue) { rankedCourses.Add(key, course); }
            }
            return rankedCourses;
        }
        public static bool IsPersonFinancial(Person person, Term term)
        {
            var result = person.FinancialTo > term.Year
                    || (person.FinancialTo == term.Year
                        && (person.FinancialToTerm == null
                            || person.FinancialToTerm >= term.TermNumber));
            return result;
        }

        private static async Task SetClassAllocationDone(U3ADbContext dbc,
                                                        Term term,
                                                        bool IsClassAllocationDone)
        {
            var today = DateTime.UtcNow.Date; ;
            var settings = await dbc.SystemSettings.AsNoTracking()
                                    .OrderBy(x => x.ID)
                                    .FirstAsync();
            if (!IsRandomAllocationTerm(term, settings)) return;
            var allocationDate = GetThisTermAllocationDay(term, settings);
            var IsAllocationDone = IsClassAllocationDone || allocationDate >= today;
            var autoEnrollOccurrence = settings.AutoEnrolAllocationOccurs;
            IQueryable<Term> termsToUpdate;
            int[] termArray;
            switch (autoEnrollOccurrence)
            {
                case AutoEnrollOccurrence.Annually:
                    termArray = new int[] { 1, 2, 3, 4 };
                    break;
                case AutoEnrollOccurrence.Semester:
                    termArray = new int[] { term.TermNumber, term.TermNumber + 1 };
                    break;
                case AutoEnrollOccurrence.Term:
                    termArray = new int[] { term.TermNumber };
                    break;
                default: return;
            }
            foreach (var t in dbc.Term.Where(x => x.Year == term.Year))
            {
                if (termArray.Contains(t.TermNumber))
                {
                    t.IsClassAllocationFinalised = IsAllocationDone;
                    dbc.Update(t);
                }
            }
            ;
            // and for completeness...
            foreach (var t in dbc.Term.Where(x => x.Year < term.Year))
            {
                t.IsClassAllocationFinalised = IsAllocationDone;
                dbc.Update(t);
            }
            ;
        }
        private static async Task<int> ProcessEnrolments(U3ADbContext dbc,
                                    DxSchedulerDataStorage calendar,
                                    Term term,
                                    Course course,
                                    List<Enrolment> enrolments)
        {
            return await ProcessEnrolments(dbc, calendar, term, course, enrolments, new List<Guid>(), false, true);
        }
        private static async Task<int> ProcessEnrolments(U3ADbContext dbc,
                                    DxSchedulerDataStorage calendar,
                                    Term enrolmentTerm,
                                    Course course,
                                    List<Enrolment> enrolments,
                                    List<Guid> PeoplePreviouslyEnrolled,
                                    bool ForceEmailQueue,
                                    bool DisableRandomEnrolment = false)
        {

            int count = 0;
            var today = DateTime.UtcNow.Date;
            var settings = await dbc.SystemSettings.AsNoTracking()
                                    .OrderBy(x => x.ID)
                                    .FirstAsync();
            if (enrolments.Any(x => IsInFutureRandomAllocationPeriod(x, enrolmentTerm, settings))) { return 0; }
            bool isFutureCourse = IsFutureCourse(today, course, calendar);
            var AlreadyEnrolledInCourse = new List<Guid>();
            if (course.EnforceOneStudentPerClass
                && course.CourseParticipationTypeID == (int?)ParticipationType.DifferentParticipantsInEachClass)
            {
                AlreadyEnrolledInCourse = await dbc.Enrolment.AsNoTracking().Where(x => x.CourseID == course.ID
                                                                && (x.TermID == enrolmentTerm.ID || isFutureCourse)
                                                                && !x.IsWaitlisted)
                                                            .Select(x => x.PersonID).ToListAsync();
            }

            // Set the enrolment method
            if (string.IsNullOrWhiteSpace(settings.AutoEnrolRemainderMethod)) settings.AutoEnrolRemainderMethod = "Random";
            var isRandomEnrol = settings.AutoEnrolRemainderMethod.ToLower() == "random";

            // We will do a random allocation once per allocation period (annual, semester or term)
            // On completion IsClassAllocationFinalised is aet True.
            var DoRandomEnrol = (DisableRandomEnrolment)
                                    ? false
                                    : !enrolmentTerm.IsClassAllocationFinalised && isRandomEnrol &&
                                            today >= GetThisTermAllocationDay(enrolmentTerm, settings);
            int enrolled = enrolments.Where(x => !x.IsWaitlisted).Count();
            if (enrolled >= course.MaximumStudents) { return 0; }
            int waitlisted = enrolments.Where(x => x.IsWaitlisted
                                && (!IsAlreadyEnrolledInCourse(x.PersonID, course, AlreadyEnrolledInCourse))).Count();
            // If available places is less than waitlisted then enroll everyone.
            if (waitlisted <= course.MaximumStudents - enrolled)
            {
                Log.Information("All students enrolled because Waitlist: {Waitlist} is less than Maximum Students: {MazStudents}", waitlisted, course.MaximumStudents);
                foreach (var e in enrolments.Where(x => x.IsWaitlisted && (x.TermID == enrolmentTerm.ID || isFutureCourse)
                                && !IsAlreadyEnrolledInCourse(x.PersonID, course, AlreadyEnrolledInCourse)))
                {
                    e.IsWaitlisted = false;
                    Log.Information("Enrolled:\t{student}", e.Person.FullName);
                    LogEnrolment(e);
                }
            }
            else
            {
                int places = 0;
                // Enrol new participants first
                if (DoRandomEnrol)
                {
                    decimal percent = settings.AutoEnrolNewParticipantPercent;
                    if (percent > 0) { places = (int)(course.MaximumStudents * percent / 100); }
                    if (places > 0 && places <= course.MaximumStudents - enrolled)
                    {
                        Log.Information("Processing New Student Allocations");
                        Log.Information("----------------------------------");
                        foreach (var e in enrolments
                                            .OrderBy(x => x.Random)
                                            .Where(x => x.IsWaitlisted && x.Person.DateJoined?.Year >= enrolmentTerm.Year - 1
                                                            && !PeoplePreviouslyEnrolled.Contains(x.PersonID)
                                                            && !IsAlreadyEnrolledInCourse(x.PersonID, course, AlreadyEnrolledInCourse))
                                            .Take(places))
                        {
                            count++;
                            Log.Information("{count:00} Joined: {Joined:dd-MMM-yyy}\tRandom: {random:00000000}\tEnrolled  {Student}", count, e.Person.DateJoined, e.Random, e.Person.FullName);
                            LogEnrolment(e);
                            e.IsWaitlisted = false;
                        }
                    }
                }
                // apply the remainder
                enrolled = enrolments.Where(x => !x.IsWaitlisted).Count();
                places = course.MaximumStudents - enrolled;
                if (places > 0)
                {
                    if (DoRandomEnrol)
                    {
                        Log.Information("Processing Random Allocations");
                        Log.Information("-----------------------------");
                        var rankedEnrolments = await GetRankedEnrolments(isFutureCourse, dbc, enrolments, enrolmentTerm);
                        foreach (var kvp in rankedEnrolments
                                            .Where(x => x.Value.IsWaitlisted
                                                        && !IsAlreadyEnrolledInCourse(x.Value.PersonID, course, AlreadyEnrolledInCourse))
                                            .Take(places))
                        {
                            var e = kvp.Value;
                            var key = kvp.Key;
                            count++;
                            Log.Information("{count:00} Class#: {classes:000}\tRandom: {random:00000000}\tEnrolled  \t{student}", count, key.Item1, key.Item2, e.Person.FullName);
                            LogEnrolment(e);
                            e.IsWaitlisted = false;
                        }
                        foreach (var kvp in rankedEnrolments
                                            .Where(x => x.Value.IsWaitlisted))
                        {
                            var e = kvp.Value;
                            var key = kvp.Key;
                            count++;
                            Log.Information("{count:00} Class#: {classes:000}\tRandom: {random:00000000}\tWaitlisted\t{student}", count, key.Item1, key.Item2, e.Person.FullName);
                        }
                    }
                    else
                    {
                        Log.Information("Processing First-In-Wins Allocations");
                        Log.Information("------------------------------------");
                        foreach (var e in enrolments
                                            .OrderBy(x => x.Created)
                                            .Where(x => x.IsWaitlisted
                                                        && !IsAlreadyEnrolledInCourse(x.PersonID, course, AlreadyEnrolledInCourse))
                                            .Take(places))
                        {
                            Log.Information("Created: {created:dd-MM-yyyy hh:mm:ss tt}\tEnrolled  \t{student}", e.Created, e.Person.FullName);
                            LogEnrolment(e);
                            e.IsWaitlisted = false;
                        }
                        foreach (var e in enrolments
                                            .OrderBy(x => x.Created)
                                            .Where(x => x.IsWaitlisted))
                        {
                            Log.Information("Created: {created:dd-MM-yyyy hh:mm:ss tt}\tWaitlisted\t{student}", e.Created, e.Person.FullName);
                        }
                    }
                }
            }
            if (ForceEmailQueue)
            {
                foreach (var e in enrolments)
                {
                    if (dbc.Entry(e).State == EntityState.Unchanged) { dbc.Entry(e).State = EntityState.Modified; }
                }
            }
            return enrolments.Count(e => !e.IsWaitlisted);
        }

        private static bool IsFutureCourse(DateTime today, Course course, DxSchedulerDataStorage calendar)
        {
            bool result = false;
            result = course.Classes.All(x => x.OccurrenceID == (int)OccurrenceType.OnceOnly && x.TermsOfferedCount == 1);
            if (!result)
            {
                var start = new DateTime(today.Year, 1, 1);
                var end = new DateTime(today.Year, 12, 31);
                var range = new DxSchedulerDateTimeRange(start, end);
                Dictionary<Guid, List<DxSchedulerAppointmentItem>> classAppointments = new();
                DateTime firstClass = calendar.GetAppointments(range)
                        .Where(x => course.Classes.Contains((Class)x.CustomFields["Source"]))
                        .Select(x => x.Start).FirstOrDefault();
                result = (firstClass >= today);
            }
            return result;
        }
        private static List<Class> GetFutureClasses(DateTime today, Course course, DxSchedulerDataStorage calendar)
        {
            List<Class> result = new();
            if (course.Classes.All(x => x.OccurrenceID == (int)OccurrenceType.OnceOnly))
            {
                var start = new DateTime(today.Year, 1, 1);
                var end = new DateTime(today.Year, 12, 31);
                var range = new DxSchedulerDateTimeRange(start, end);
                Dictionary<Guid, List<DxSchedulerAppointmentItem>> classAppointments = new();
                result.AddRange( calendar.GetAppointments(range)
                        .Where(x => course.Classes.Contains((Class)x.CustomFields["Source"]))
                        .Select(x => (Class)x.CustomFields["Source"]).ToList());
            }
            return result;
        }

        private static bool IsInFutureRandomAllocationPeriod(Enrolment enrolment, Term enrolmentTerm, SystemSettings settings)
        {
            var randomAllocationPeriod = settings.AutoEnrolAllocationOccurs;
            var thisTermNumber = enrolment.Term.TermNumber;
            switch (randomAllocationPeriod)
            {
                case AutoEnrollOccurrence.Annually:
                    return false;
                case AutoEnrollOccurrence.Semester:
                    return (enrolmentTerm.TermNumber <= 2 && thisTermNumber > 2);
                case AutoEnrollOccurrence.Term:
                    return true;
                default: throw new InvalidOperationException();
            }
        }
        private static async Task<SortedList<(int, long, Guid), Enrolment>> GetRankedEnrolments(bool isFutureCourse, U3ADbContext dbc, IEnumerable<Enrolment> enrolments, Term term)
        {
            (int courses, long random, Guid personID) key;
            SortedList<(int, long, Guid), Enrolment> rankedEnrolments = new();
            var people = enrolments.Select(x => x.PersonID).ToList();
            var courseCounts = await dbc.Enrolment
                                .Where(e => (e.TermID == term.ID || isFutureCourse)
                                                && people.Contains(e.PersonID)
                                                && !e.IsWaitlisted
                                                )
                                .GroupBy(g => new
                                {
                                    PersonID = g.PersonID,
                                })
                                .Select(g => new Tuple<Guid, int>(g.Key.PersonID, g.Count()))
                                .ToListAsync();
            foreach (var e in enrolments)
            {
                key = new()
                {
                    personID = e.PersonID,
                    random = e.Random,
                    courses = 0
                };
                var courseCount = courseCounts.Where(r => r.Item1 == e.PersonID).FirstOrDefault();
                if (courseCount != null) { key.courses = courseCount.Item2; }
                rankedEnrolments.Add(key, e);

            }
            return rankedEnrolments;
        }

        private static bool IsAlreadyEnrolledInCourse(Guid PersonID, Course course, List<Guid> enrolledPeopleID)
        {
            var result = false;
            if (course.CourseParticipationTypeID == (int?)ParticipationType.DifferentParticipantsInEachClass)
            {
                result = (enrolledPeopleID.Contains(PersonID)) ? true : false;
            }
            return result;
        }

        private static void LogEnrolment(Enrolment enrolment)
        {
            var person = enrolment.Person;
            var course = enrolment.Course;
            AutoEnrolments.Add($"{person.FullName} enrolled in {course.Name}.");
        }
    }
}