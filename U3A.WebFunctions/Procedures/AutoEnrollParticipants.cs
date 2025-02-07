using DevExpress.CodeParser;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using U3A.BusinessRules;
using U3A.Database;
using U3A.Model;

namespace U3A.WebFunctions.Procedures;

public static class AutoEnrollParticipants
{
    public static async Task<bool> Process(TenantInfo tenant,
                                        string tenantConnectionString,
                                        ILogger logger)
    {
        bool hasRandomAllocationExecuted = false; //Return value
        using (var dbc = new U3ADbContext(tenant))
        {
            dbc.UtcOffset = await Common.GetUtcOffsetAsync(dbc);
            var settings = await dbc.SystemSettings
                                .OrderBy(x => x.ID)
                                .FirstOrDefaultAsync();
            if (BusinessRule.IsEnrolmentBlackoutPeriod(settings!))
            {
                logger.LogInformation($"[{dbc.TenantInfo.Identifier}]: Allocation not performed - Enrolment Blackout till: {settings!.EnrolmentBlackoutEndsUTC.GetValueOrDefault().ToString(constants.STD_DATETIME_FORMAT)}");
                return hasRandomAllocationExecuted;
            }
            var IsClassAllocationFinalised = false;
            var forceEmailQueue = false;
            DateTime? emailDate = null;
            var today = await Common.GetTodayAsync(dbc);
            var now = await Common.GetNowAsync(dbc);
            DateTime? allocationDate = null;
            if (string.IsNullOrWhiteSpace(settings!.AutoEnrolRemainderMethod)) settings.AutoEnrolRemainderMethod = "Random";

            //get the current enrolment term
            var currentTerm = await BusinessRule.CurrentEnrolmentTermAsync(dbc);
            if (currentTerm == null) return hasRandomAllocationExecuted;
            if (settings.AutoEnrolRemainderMethod.ToLower() == "random")
            {
                //Allocation is random

                if (BusinessRule.IsRandomAllocationTerm(currentTerm, settings))
                {
                    allocationDate = BusinessRule.GetThisTermAllocationDay(currentTerm, settings);
                    if (BusinessRule.IsPreRandomAllocationDay(currentTerm, settings, today))
                    {
                        logger.LogInformation($"[{dbc.TenantInfo.Identifier}]: Allocation not performed - Date prior to allocation date: {allocationDate?.ToLongDateString()}");
                        return hasRandomAllocationExecuted;
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
                                            TimeSpan.FromHours((constants.RANDOM_ALLOCATION_PREVIEW * 24) - 2);
                            // Set the enrolment period blackout end date
                            settings.EnrolmentBlackoutEndsUTC = emailDate;
                            // force all members to get a report
                            forceEmailQueue = true;
                            // set TRUE to force all leaders to get a report
                            hasRandomAllocationExecuted = true;
                        }
#if DEBUG
                        forceEmailQueue = false;
#endif
                    }
                }
            }
            else
            {
                // Allocation is first in wins.
                IsClassAllocationFinalised = true;
                forceEmailQueue = false;
            }

            // process for participants
            await BusinessRule.AutoEnrolParticipantsAsync(dbc, currentTerm,
                                            IsClassAllocationFinalised,
                                            forceEmailQueue, emailDate);
            logger.LogInformation($">>>> [{dbc.TenantInfo.Identifier}]: {BusinessRule.AutoEnrolments.Count} Auto-Enrolments for {currentTerm.Year} term {currentTerm.TermNumber}. <<<<");
            foreach (var log in BusinessRule.AutoEnrolments)
            {
                logger.LogInformation(log);
            }
            BusinessRule.AutoEnrolments.Clear();

            // process for multi-campus visitors
            //  Make sure random allocation is complete
            if (today >= allocationDate?.AddDays(constants.RANDOM_ALLOCATION_PREVIEW))
            {
                await BusinessRule.AutoEnrolMultiCampus(tenant, tenantConnectionString, now);
            }
        }
        return hasRandomAllocationExecuted;
    }
    public static async Task ProcessByEnrolment(TenantInfo tenant,
                                        IEnumerable<Guid> IdsToProcess,
                                        string tenantConnectionString,
                                        ILogger logger)
    {
        using (var dbc = new U3ADbContext(tenant))
        {
            dbc.UtcOffset = await Common.GetUtcOffsetAsync(dbc);
            var settings = await dbc.SystemSettings
                                .OrderBy(x => x.ID)
                                .FirstOrDefaultAsync();
            if (BusinessRule.IsEnrolmentBlackoutPeriod(settings!))
            {
                logger.LogInformation($"[{dbc.TenantInfo.Identifier}]: Allocation not performed - Enrolment Blackout till: {settings!.EnrolmentBlackoutEndsUTC.GetValueOrDefault().ToString(constants.STD_DATETIME_FORMAT)}");
                return;
            }
            var today = await Common.GetTodayAsync(dbc);
            var now = await Common.GetNowAsync(dbc);
            DateTime allocationDate = DateTime.MinValue;
            if (string.IsNullOrWhiteSpace(settings!.AutoEnrolRemainderMethod)) settings.AutoEnrolRemainderMethod = "Random";

            //get the current enrolment term
            var currentTerm = await BusinessRule.CurrentEnrolmentTermAsync(dbc);
            if (currentTerm == null) return;
            if (settings.AutoEnrolRemainderMethod.ToLower() == "random")
            {
                //Allocation is random

                if (BusinessRule.IsRandomAllocationTerm(currentTerm, settings))
                {
                    allocationDate = BusinessRule.GetThisTermAllocationDay(currentTerm, settings);
                }
            }

            //  Make sure random allocation is complete
            if (today > allocationDate.AddDays(constants.RANDOM_ALLOCATION_PREVIEW))
            {
                // process for participants
                await BusinessRule.AutoEnrolParticipantsAsync(dbc, currentTerm,
                                            IsClassAllocationDone: false,
                                            ForceEmailQueue: false);
                logger.LogInformation($">>>> [{dbc.TenantInfo.Identifier}]: {BusinessRule.AutoEnrolments.Count} Auto-Enrolments for {currentTerm.Year} term {currentTerm.TermNumber}. <<<<");
                foreach (var log in BusinessRule.AutoEnrolments)
                {
                    logger.LogInformation(log);
                }
                BusinessRule.AutoEnrolments.Clear();

                // process for multi-campus visitors
                await BusinessRule.AutoEnrolMultiCampus(tenant, tenantConnectionString, now);
            }
        }
    }
}

