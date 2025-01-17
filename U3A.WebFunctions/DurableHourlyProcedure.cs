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
using System.Threading;


namespace U3A.WebFunctions
{

    public class DurableHourlyProcedure
    {
        private readonly IConfiguration config;
        public DurableHourlyProcedure(IConfiguration config)
        {
            this.config = config;
        }

        [Function(nameof(DurableHourlyProcedure))]
        public async Task RunOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context)
        {
            ILogger logger = context.CreateReplaySafeLogger(nameof(DurableHourlyProcedure));
            // Replace name and input with values relevant for your Durable Functions Activity
            var data = context.GetInput<dynamic>();
            await context.CallActivityAsync<string>(nameof(DurableHourlyProcedureWorker), data);
        }

        [Function(nameof(DurableHourlyProcedureWorker))]
        public async Task<string> DurableHourlyProcedureWorker([ActivityTrigger] string tenant, FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger(nameof(DurableHourlyProcedureWorker));
            var tenantName = (string.IsNullOrEmpty(tenant) ? "All Tenants" : tenant);
            logger.LogInformation("Hourly Procedures started for: {name}.", tenantName);
            await DoWork(logger, tenant);
            return $"Hourly Procedures completed for: {tenantName}";
        }

        private async Task DoWork(ILogger logger, string tenantToProcess)
        {

            //Retrieve the tenants
            var tenants = new List<TenantInfo>();
            var cn = config.GetConnectionString(Common.TENANT_CN_CONFIG);
            Common.GetTenants(tenants, cn!, tenantToProcess);

            logger.LogInformation($"{tenants.Count} tenants retrieved from database.");
            logger.LogInformation($"UTC Time is: {DateTime.UtcNow}");

            bool isBackgroundProcessingEnabled = true;
            List<Task> TaskList = new List<Task>();

            TimeSpan utcOffset;
            foreach (var tenant in tenants)
            {
                logger.LogInformation($"****** Processing AutoEnrollParticipants for {tenant.Identifier}: {tenant.Name}. ******");
                try
                {
                    using (var dbc = new U3ADbContext(tenant))
                    {
                        utcOffset = await Common.GetUtcOffsetAsync(dbc);
                        dbc.UtcOffset = utcOffset;
                        logger.LogInformation($"[{tenant.Identifier}] Local Time: {DateTime.UtcNow + utcOffset}. UTC Offset: {utcOffset}");
                    }
                    await AutoEnrollParticipants.Process(tenant, cn!, logger);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Error processing AutoEnrollParticipants for {tenant.Identifier}");
                }
            }

            foreach (var tenant in tenants)
            {
                logger.LogInformation($"****** Processing Email for {tenant.Identifier}: {tenant.Name}. ******");
                try
                {
                    isBackgroundProcessingEnabled = !await Common.isBackgroundProcessingDisabled(tenant);
                    if (isBackgroundProcessingEnabled)
                    {
                        await ProcessCorrespondence.Process(tenant, cn!, logger, IsHourlyProcedure: true);
                    }
                    else
                    {
                        logger.LogInformation($"[{tenant.Identifier}]: Email not sent because background processing is disabled. Enable via Admin | Organisation Details");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Error processing Email for {tenant.Identifier}");
                }
            }

            if (string.IsNullOrEmpty(tenantToProcess))
            {
                // long running process, don't run if processing a single tenant (HTTP trigger)
                foreach (var tenant in tenants)
                {
                    try
                    {
                        using (var dbc = new U3ADbContext(tenant))
                        {
                            dbc.UtcOffset = await Common.GetUtcOffsetAsync(dbc);
                            using (var dbcT = new TenantDbContext(cn!))
                            {
                                await BusinessRule.BuildScheduleAsync(dbc, dbcT, tenant.Identifier!);
                            }
                            logger.LogInformation($"Class Schedule cache created for: {tenant.Identifier}.");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Error processing Class Schedule cache for {tenant.Identifier}");
                    }
                }
            }
            logger.LogInformation($"Hourly Procedures complete at: {DateTime.UtcNow} UTC");
        }

//        [Function("DurableHourlyProcedureTimerStart")]
//        public static async Task Run(
//            [TimerTrigger("0 0 22-23,0-11 * * *"
//#if DEBUG
//           //, RunOnStartup=true
//#endif            
//            )]
//            TimerInfo myTimer,
//            [DurableClient] DurableTaskClient client,
//            FunctionContext executionContext)
//        {
//            ILogger logger = executionContext.GetLogger("DurableHourlyProcedureTimerStart");

//            // Start the orchestration
//            string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
//                nameof(DurableHourlyProcedure));
//            logger.LogInformation($"Started orchestration with ID = '{instanceId}'.");
//        }

        [Function("DurableHourlyProcedureHttpStart")]
        public static async Task<HttpResponseData> Ht11tpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
            [DurableClient] DurableTaskClient client,
            FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("DurableHourlyProcedureHttpStart");
            var tenant = string.Empty;
            var queryStrings = req.Query.GetValues("tenant");
            if (queryStrings?.Count() > 0)
            {
                tenant = queryStrings[0];
            }
            // Function input comes from the request content.
            string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
                nameof(DurableHourlyProcedure), tenant);

            logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

            // Returns an HTTP 202 response with an instance management payload.
            // See https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-http-api#start-orchestration
            return await client.CreateCheckStatusResponseAsync(req, instanceId);
        }
    }
}
