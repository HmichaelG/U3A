using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using U3A.BusinessRules;
using U3A.Database;
using U3A.Model;

namespace U3A.WebFunctions.Procedures
{
    public static class AutoEnrolParticipants
    {
        public static async Task Process(TenantInfo tenant, ILogger logger)
        {
            using (var dbc = new U3ADbContext(tenant))
            {
                var IsClassAllocationFinalised = false;
                var forceEmailQueue = false;
                DateTime? emailDate = null;
                // Get system settings
                var today = await Common.GetTodayAsync(dbc);
                var now = await Common.GetNowAsync(dbc);
                var settings = await dbc.SystemSettings
                                    .OrderBy(x => x.ID)
                                    .FirstOrDefaultAsync();
                if (string.IsNullOrWhiteSpace(settings!.AutoEnrolRemainderMethod)) settings.AutoEnrolRemainderMethod = "Random";

                //get the current enrolment term
                var currentTerm = BusinessRule.CurrentEnrolmentTerm(dbc);
                if (currentTerm == null) return;
                await BusinessRule.FixEnrolmentTerm(dbc, currentTerm);
                if (settings.AutoEnrolRemainderMethod.ToLower() == "random")
                {
                    //Allocation is random

                    DateTime allocationDate = BusinessRule.GetThisTermAllocationDay(currentTerm, settings);
                    if (BusinessRule.IsPreRandomAllocationDay(currentTerm, settings, today))
                    {
                        logger.LogInformation($"[{dbc.TenantInfo.Identifier}]: No Auto-Allocation performed - Date prior to allocation date: {allocationDate.ToLongDateString()}");
                        return;
                    }
                    else
                    {
                        IsClassAllocationFinalised = currentTerm.IsClassAllocationFinalised;
                        if (today == allocationDate)
                        {
                            logger.LogInformation($"!!! [{dbc.TenantInfo.Identifier}]: Random Auto-Allocation Day !!!");
                            var utcNow = DateTime.UtcNow;
                            // emailDate will be 3 days from now less two hours to ensure it occurs.
                            emailDate = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, utcNow.Hour, 0, 0, 0) +
                                            TimeSpan.FromHours(constants.RANDOM_ALLOCATION_PREVIEW * 24 - 2);
                            // force all members to get a report
                            forceEmailQueue = true;
                            // set TRUE to force all leaders to get a report
                            DailyProcedures.RandomAllocationExecuted[tenant.Identifier!] = true;
                        }
#if DEBUG
                        forceEmailQueue = false;
#endif
                    }
                }
                else
                {
                    // Allocation is first in wins.
                    IsClassAllocationFinalised = true;
                    forceEmailQueue = false;
                }
                await BusinessRule.AutoEnrolParticipantsAsync(dbc, currentTerm,
                                                IsClassAllocationFinalised,
                                                forceEmailQueue, emailDate);
                logger.LogInformation($">>>> [{dbc.TenantInfo.Identifier}]: {BusinessRule.AutoEnrolments.Count} Auto-Enrolments for {currentTerm.Year} term {currentTerm.TermNumber}. <<<<");
                foreach (var log in BusinessRule.AutoEnrolments)
                {
                    logger.LogInformation(log);
                }
                BusinessRule.AutoEnrolments.Clear();
            }
            return;
        }

    }
}

