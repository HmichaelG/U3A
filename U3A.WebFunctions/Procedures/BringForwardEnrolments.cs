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
            using (var dbc = new U3ADbContext(tenant))
            {
                dbc.UtcOffset = await Common.GetUtcOffsetAsync(dbc);
                var today = await Common.GetTodayAsync(dbc);
                var sourceTerm = await BusinessRule.CurrentTermAsync(dbc);
                if (sourceTerm != null)
                {
                    var targetTerm = await BusinessRule.GetNextTermInYear(dbc, sourceTerm);
                    if (targetTerm != null)
                    {
                        var dayDiff = (targetTerm.EnrolmentStartDate - today).Days;
                        // Run it 3 times to make sure it occurs
                        if (dayDiff > 0 && dayDiff < 4)
                        {
                            await BusinessRule.BringForwardEnrolmentsAsync(dbc, sourceTerm, targetTerm, false);
                            var d = targetTerm.EnrolmentStartDate;
                            Log.Information($">>>> Enrollments brought forward prior to enrollment start {d.ToShortDateString()} for Term {targetTerm.TermNumber}. <<<<");
                            Log.Information($"Current Term is unchanged (Term {sourceTerm.TermNumber}).");
                        }

                        // make sure we are in the enrollment period before term start
                        if (today < targetTerm.EnrolmentStartDate) return;
                        if (today >= targetTerm.StartDate) return;

                        // Setup new term if there are no more classes in the current term.
                        var storage = await BusinessRule.GetCourseScheduleDataStorageAsync(dbc, sourceTerm);
                        DxSchedulerDateTimeRange range = new DxSchedulerDateTimeRange(today, targetTerm.StartDate);
                        if (range.IsValid)
                        {
                            bool AreClassesInRange = storage.GetAppointments(range)
                                                            .Any(x => x.CustomFields["Source"] != null
                                                            && (int)x.LabelId != 9);    // Cancelled/Postponed
                            if (!AreClassesInRange)
                            {
                                await BusinessRule.BringForwardEnrolmentsAsync(dbc, sourceTerm, targetTerm, true);
                                var d = targetTerm.StartDate;
                                Log.Information($">>>> Enrollments brought forward prior to start date {d.ToShortDateString()} for Term {targetTerm.TermNumber}. <<<<");
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
                            var storage = await BusinessRule.GetCourseScheduleDataStorageAsync(dbc, sourceTerm);
                            DxSchedulerDateTimeRange range = new DxSchedulerDateTimeRange(today, targetTerm.StartDate);
                            if (range.IsValid)
                            {
                                bool ClassesToBeHeld = storage.GetAppointments(range)
                                                                .Any(x => x.CustomFields["Source"] != null
                                                                && (int)x.LabelId != 9);    // Not Cancelled/Postponed
                                if (!ClassesToBeHeld)
                                {
                                    var trm = await dbc.Term.FindAsync(targetTerm.ID);
                                    if (trm != null && (!trm.IsDefaultTerm))
                                    {
                                        foreach (var t in dbc.Term.Where(x => x.IsDefaultTerm)) { t.IsDefaultTerm = false; }
                                        trm.IsDefaultTerm = true;
                                        _ = dbc.SaveChanges();
                                        Log.Information($"Current Term brought forward to Term {targetTerm.TermNumber}.");
                                    }
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
