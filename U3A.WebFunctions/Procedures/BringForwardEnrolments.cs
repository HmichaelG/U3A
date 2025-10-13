using DevExpress.Blazor;
using Serilog;
using U3A.BusinessRules;
using U3A.Database;
using U3A.Model;

namespace U3A.WebFunctions.Procedures
{
    public static class BringForwardEnrolments
    {
        public static async Task Process(TenantInfo tenant)
        {
            using U3ADbContext dbc = new(tenant);
            dbc.UtcOffset = await Common.GetUtcOffsetAsync(dbc);
            DateTime today = await Common.GetTodayAsync(dbc);
            Term? sourceTerm = await BusinessRule.CurrentTermAsync(dbc);
            if (sourceTerm != null)
            {
                Term? targetTerm = await BusinessRule.GetNextTermInYear(dbc, sourceTerm);
                if (targetTerm != null)
                {
                    int dayDiff = (targetTerm.EnrolmentStartDate - today).Days;
                    // Run it multiple times to make sure it occurs
                    if (dayDiff is >= (-4) and < 4)
                    {
                        await BusinessRule.BringForwardEnrolmentsAsync(dbc, sourceTerm, targetTerm, false);
                        DateTime d = targetTerm.EnrolmentStartDate;
                        Log.Information($">>>> Enrollments brought forward prior to enrollment start {d:d} for Term {targetTerm.TermNumber}. <<<<");
                        Log.Information($"Current Term is unchanged (Term {sourceTerm.TermNumber}).");
                    }

                    // make sure we are in the enrollment period before term start
                    if (today < targetTerm.EnrolmentStartDate)
                    {
                        return;
                    }

                    if (today >= targetTerm.StartDate)
                    {
                        return;
                    }

                    // Setup new term if there are no more classes in the current term.
                    DxSchedulerDataStorage storage = await BusinessRule.GetCourseScheduleDataStorageAsync(dbc, sourceTerm);
                    DxSchedulerDateTimeRange range = new(today, targetTerm.StartDate);
                    if (range.IsValid)
                    {
                        bool AreClassesInRange = storage.GetAppointments(range)
                                                        .Any(x => x.CustomFields["Source"] != null
                                                        && (int)x.LabelId != 9);    // Cancelled/Postponed
                        if (!AreClassesInRange)
                        {
                            await BusinessRule.BringForwardEnrolmentsAsync(dbc, sourceTerm, targetTerm, true);
                            DateTime d = targetTerm.StartDate;
                            Log.Information($">>>> Enrollments brought forward prior to start date {d:d} for Term {targetTerm.TermNumber}. <<<<");
                            Log.Information($"Current Term brought forward to Term {targetTerm.TermNumber}.");
                        }
                    }
                }
                else
                {
                    // target is null. Must be a new year. Set the default term only
                    targetTerm = await BusinessRule.GetFirstTermNextYearAsync(dbc, sourceTerm);
                    if (targetTerm != null)
                    {
                        // make sure there are no classes left to run
                        DxSchedulerDataStorage storage = await BusinessRule.GetCourseScheduleDataStorageAsync(dbc, sourceTerm);
                        DxSchedulerDateTimeRange range = new(today, targetTerm.StartDate);
                        if (range.IsValid)
                        {
                            bool ClassesToBeHeld = storage.GetAppointments(range)
                                                            .Any(x => x.CustomFields["Source"] != null
                                                            && (int)x.LabelId != 9);    // Not Cancelled/Postponed
                            if (!ClassesToBeHeld)
                            {
                                Term? trm = await dbc.Term.FindAsync(targetTerm.ID);
                                if (trm != null && (!trm.IsDefaultTerm))
                                {
                                    foreach (Term? t in dbc.Term.Where(x => x.IsDefaultTerm)) { t.IsDefaultTerm = false; }
                                    trm.IsDefaultTerm = true;
                                    _ = dbc.SaveChanges();
                                    Log.Information($"Current Term brought forward to Term {targetTerm.TermNumber}.");
                                }
                            }
                        }

                    }

                }
            }
            return;
        }
    }
}
