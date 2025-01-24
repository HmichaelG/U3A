using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using U3A.Database;
using U3A.Model;
using System.Text.Json;


namespace U3A.WebFunctions;

public partial class DurableFunctions
{
    private readonly IConfiguration config;
    public DurableFunctions(IConfiguration config)
    {
        this.config = config;
    }

    [Function(nameof(DurableFunctions))]
    public async Task RunOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        ILogger logger = context.CreateReplaySafeLogger(nameof(DurableFunctions));
        var optionsData = context.GetInput<dynamic>();
        var json = JsonSerializer.Serialize(optionsData);
        var options = JsonSerializer.Deserialize<U3AFunctionOptions>(json);
        var result = string.Empty;
        switch (options.DurableActivity)
        {
            case DurableActivity.DoFinalisePayments:
                result = await context.CallActivityAsync<string>(nameof(DoFinalisePaymentsActivity), options.TenantIdentifier);
                break;
            case DurableActivity.DoAutoEnrolment:
                result = await context.CallActivityAsync<string>(nameof(DoAutoEnrolmentActivity), options.TenantIdentifier);
                break;
            case DurableActivity.DoBringForwardEnrolments:
                result = await context.CallActivityAsync<string>(nameof(DoBringForwardEnrolmentsActivity), options.TenantIdentifier);
                break;
            case DurableActivity.DoCorrespondence:
                result = await context.CallActivityAsync<string>(nameof(DoCorrespondenceActivity), options.TenantIdentifier);
                break;
            case DurableActivity.DoSendLeaderReports:
                result = await context.CallActivityAsync<string>(nameof(DoSendLeaderReportsActivity), options.TenantIdentifier);
                break;
            case DurableActivity.DoMembershipAlertsEmail:
                result = await context.CallActivityAsync<string>(nameof(DoMembershipAlertsEmailActivity), options.TenantIdentifier);
                break;
            case DurableActivity.DoProcessQueuedDocuments:
                result = await context.CallActivityAsync<string>(nameof(DoProcessQueuedDocumentsActivity), options.TenantIdentifier);
                break;
            case DurableActivity.DoCreateAttendance:
                result = await context.CallActivityAsync<string>(nameof(DoCreateAttendanceActivity), options.TenantIdentifier);
                break;
            case DurableActivity.DoBuildSchedule:
                result = await context.CallActivityAsync<string>(nameof(DoBuildScheduleActivity), options.TenantIdentifier);
                break;
            case DurableActivity.DoDatabaseCleanup:
                result = await context.CallActivityAsync<string>(nameof(DoDatabaseCleanupActivity), options.TenantIdentifier);
                break;
        }
    }

    async Task LogStartTime(ILogger logger, TenantInfo tenant)
    {
        using (var dbc = new U3ADbContext(tenant))
        {
            var utcOffset = await Common.GetUtcOffsetAsync(dbc);
            dbc.UtcOffset = utcOffset;
            logger.LogInformation($"[{tenant.Name}] Local Time: {DateTime.UtcNow + utcOffset}. UTC Offset: {utcOffset}");
        }
    }

    static async Task<HttpResponseData> ScheduleFunctionAsync(DurableTaskClient client,
                                     FunctionContext executionContext,
                                     HttpRequestData req,
                                     U3AFunctionOptions options,
                                     bool WaitForCompletion = false)
    {
        var activity = Enum.GetName<DurableActivity>(options.DurableActivity);
        if (activity == null)
        {
            throw new ArgumentException("Invalid activity specified.");
        }
        ILogger logger = executionContext.GetLogger(activity);
        var instanceId = $"{activity}_{options.TenantIdentifier}";
        // Wait for current instance to finish
        var existingInstance = await client.GetInstanceAsync(instanceId);
        if (!(existingInstance == null
            || existingInstance.RuntimeStatus == OrchestrationRuntimeStatus.Completed
            || existingInstance.RuntimeStatus == OrchestrationRuntimeStatus.Failed
            || existingInstance.RuntimeStatus == OrchestrationRuntimeStatus.Terminated))
        {
            var result = await client.WaitForInstanceCompletionAsync(instanceId);
        }
        // Start the orchestration
        StartOrchestrationOptions startOrchestrationOptions = new StartOrchestrationOptions()
        {
            InstanceId = instanceId
        };
        instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
            nameof(DurableFunctions), options, startOrchestrationOptions);
        logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

        // wait for completion, if necessary
        if (WaitForCompletion)
        {
         var result = await client.WaitForInstanceCompletionAsync(instanceId);
        }

        // Returns an HTTP 202 response with an instance management payload.
        // See https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-http-api#start-orchestration
        return await client.CreateCheckStatusResponseAsync(req, instanceId);
    }

    string GetInstanceId(U3AFunctionOptions options)
    {
        return $"{options.DurableActivity}_{options.TenantIdentifier}";
    }

    TenantInfo? GetTenant(ILogger logger, string tenantToProcess, string connectionString)
    {
        var tenants = new List<TenantInfo>();
        Common.GetTenants(tenants, connectionString!, tenantToProcess);
        return (tenants.Count > 0) ? tenants.ToArray()[0] : null;
    }

    [Function(nameof(DoHourlyProcedures))]
    public async Task DoHourlyProcedures(
        [TimerTrigger("0 0 22-23,0-11 * * *"
    //#if DEBUG
           //    , RunOnStartup=true
    //#endif            
                )]
                TimerInfo myTimer,
        [DurableClient] DurableTaskClient client,
        FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger(nameof(DoHourlyProcedures));

        string instanceId;
        var tenants = new List<TenantInfo>();
        //Retrieve the tenants
        var cn = config.GetConnectionString(Common.TENANT_CN_CONFIG);
        Common.GetTenants(tenants, cn!);
        
        foreach (var tenant in tenants)
        {
            instanceId = await ScheduleTimerFunction(logger, tenant.Identifier!, DurableActivity.DoFinalisePayments, client);
            instanceId = await ScheduleTimerFunction(logger, tenant.Identifier!, DurableActivity.DoAutoEnrolment, client);
            instanceId = await ScheduleTimerFunction(logger, tenant.Identifier!, DurableActivity.DoCorrespondence, client);
            instanceId = await ScheduleTimerFunction(logger, tenant.Identifier!, DurableActivity.DoProcessQueuedDocuments, client);
            instanceId = await ScheduleTimerFunction(logger, tenant.Identifier!, DurableActivity.DoBuildSchedule, client);
        }
    }

    [Function(nameof(DoDailyProcedures))]
    public async Task DoDailyProcedures(
        [TimerTrigger("0 0 17 * * *"
    //#if DEBUG
               , RunOnStartup=true
    //#endif            
                )]
                TimerInfo myTimer,
        [DurableClient] DurableTaskClient client,
        FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger(nameof(DoDailyProcedures));

        string instanceId;
        var tenants = new List<TenantInfo>();
        //Retrieve the tenants
        var cn = config.GetConnectionString(Common.TENANT_CN_CONFIG);
        Common.GetTenants(tenants, cn!);

        foreach (var tenant in tenants)
        {
            instanceId = await ScheduleTimerFunction(logger, tenant.Identifier!, DurableActivity.DoFinalisePayments, client);
            instanceId = await ScheduleTimerFunction(logger, tenant.Identifier!, DurableActivity.DoAutoEnrolment, client);
            instanceId = await ScheduleTimerFunction(logger, tenant.Identifier!, DurableActivity.DoBringForwardEnrolments, client);
            instanceId = await ScheduleTimerFunction(logger, tenant.Identifier!, DurableActivity.DoCreateAttendance, client);
            instanceId = await ScheduleTimerFunction(logger, tenant.Identifier!, DurableActivity.DoCorrespondence, client);
            instanceId = await ScheduleTimerFunction(logger, tenant.Identifier!, DurableActivity.DoSendLeaderReports, client);
            instanceId = await ScheduleTimerFunction(logger, tenant.Identifier!, DurableActivity.DoMembershipAlertsEmail, client);
            instanceId = await ScheduleTimerFunction(logger, tenant.Identifier!, DurableActivity.DoProcessQueuedDocuments, client);
            instanceId = await ScheduleTimerFunction(logger, tenant.Identifier!, DurableActivity.DoBuildSchedule, client);
            instanceId = await ScheduleTimerFunction(logger, tenant.Identifier!, DurableActivity.DoDatabaseCleanup, client);
        }
        await client.PurgeInstancesAsync(createdFrom: new DateTime(1900,1,1),
                                            createdTo: DateTime.UtcNow.AddHours(-1));
    }

    async Task<string> ScheduleTimerFunction(ILogger logger, string tenantIdentifier, 
                                            DurableActivity durableActivity, 
                                            DurableTaskClient client)
    {
        U3AFunctionOptions options;
        string instanceId;
        StartOrchestrationOptions startOrchestrationOptions;
        options = new U3AFunctionOptions()
        {
            TenantIdentifier = tenantIdentifier!,
            DurableActivity = durableActivity
        };
        instanceId = GetInstanceId(options);
        // Wait for current instance to finish
        var existingInstance = await client.GetInstanceAsync(instanceId);
        if (!(existingInstance == null
            || existingInstance.RuntimeStatus == OrchestrationRuntimeStatus.Completed
            || existingInstance.RuntimeStatus == OrchestrationRuntimeStatus.Failed
            || existingInstance.RuntimeStatus == OrchestrationRuntimeStatus.Terminated))
        {
            var result = await client.WaitForInstanceCompletionAsync(instanceId);
        }
       
        // Start the orchestration
        startOrchestrationOptions = new StartOrchestrationOptions()
        {
            InstanceId = instanceId            
        };
        instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
                        nameof(DurableFunctions), options, startOrchestrationOptions);
        logger.LogInformation($"Started orchestration with ID = '{instanceId}'.");
        _ = await client.WaitForInstanceCompletionAsync(instanceId);
        return instanceId;
    }



}

