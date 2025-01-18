using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using U3A.BusinessRules;
using U3A.Database;
using U3A.Model;
using U3A.WebFunctions.Procedures;
using System.Text.Json;
using System.ComponentModel;
using System.Text.Json.Serialization;
using System.IdentityModel.Tokens.Jwt;
using Serilog.Core;
using System.Reflection;


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
        if (options.DoFinalisePayments)
        { result = await context.CallActivityAsync<string>(nameof(DoFinalisePaymentsActivity), options.TenantIdentifier); }
        if (options.DoAutoEnrolment)
        { result = await context.CallActivityAsync<string>(nameof(DoAutoEnrolmentActivity), options.TenantIdentifier); }
        if (options.DoBringForwardEnrolments)
        { result = await context.CallActivityAsync<string>(nameof(DoBringForwardEnrolmentsActivity), options.TenantIdentifier); }
        if (options.DoCorrespondence)
        { result = await context.CallActivityAsync<string>(nameof(DoCorrespondenceActivity), options.TenantIdentifier); }
        if (options.DoBuildSchedule)
        { result = await context.CallActivityAsync<string>(nameof(DoBuildScheduleActivity), options.TenantIdentifier); }
    }

    async Task LogStartTime(ILogger logger,TenantInfo tenant) {
        using (var dbc = new U3ADbContext(tenant))
        {
            var utcOffset = await Common.GetUtcOffsetAsync(dbc);
            dbc.UtcOffset = utcOffset;
            logger.LogInformation($"[{tenant.Name}] Local Time: {DateTime.UtcNow + utcOffset}. UTC Offset: {utcOffset}");
        }
    }

    TenantInfo[] GetTenants(ILogger logger, string tenantToProcess, string connectionString)
    {
        var tenants = new List<TenantInfo>();
        Common.GetTenants(tenants, connectionString!, tenantToProcess);
        var name = (string.IsNullOrEmpty(tenantToProcess) ? "All Tenants" : tenantToProcess);
        logger.LogInformation($"Processing activity for: {name}.");
        logger.LogInformation($"{tenants.Count} tenants retrieved from database.");
        logger.LogInformation($"UTC Time is: {DateTime.UtcNow}");
        return tenants.ToArray();
    }

    [Function("DurableHourlyProcedureStart")]
    public static async Task DurableHourlyProcedureStart(
        [TimerTrigger("0 0 22-23,0-11 * * *"
    #if DEBUG
               //, RunOnStartup=true
    #endif            
                )]
                TimerInfo myTimer,
        [DurableClient] DurableTaskClient client,
        FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger(nameof(DurableHourlyProcedureStart));
        var options = new U3AFunctionOptions() 
        { 
            DoAutoEnrolment =true, 
            DoCorrespondence=true, 
            DoBuildSchedule = true
        };
        // Start the orchestration
        string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
            nameof(DurableFunctions),options);
        logger.LogInformation($"Started orchestration with ID = '{instanceId}'.");
    }


}

