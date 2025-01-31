using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using U3A.WebFunctions.Procedures;
using U3A.Model;


namespace U3A.WebFunctions;

public partial class DurableFunctions
{

    [Function(nameof(DoAutoEnrolmentActivity))]
    public async Task<bool> DoAutoEnrolmentActivity([ActivityTrigger] U3AFunctionOptions options, FunctionContext executionContext)
    {
        bool hasRandomAllocationExecuted = false; //Return value
        ILogger logger = executionContext.GetLogger(nameof(DoAutoEnrolmentActivity));
        var cn = config.GetConnectionString(Common.TENANT_CN_CONFIG);
        if (cn != null)
        {
            var tenant = GetTenant(logger, options.TenantIdentifier, cn);
            if (tenant != null)
            {
                {
                    logger.LogInformation($"****** Started {nameof(DoAutoEnrolmentActivity)} for {tenant.Identifier}: {tenant.Name}. ******");
                    try
                    {
                        await LogStartTime(logger, tenant);
                        hasRandomAllocationExecuted = await AutoEnrollParticipants.Process(tenant, cn!, logger);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Error processing {nameof(DoAutoEnrolmentActivity)} for {tenant.Identifier}");
                    }
                }
            }
            else { throw new NullReferenceException("Database connection string is null"); }
        }
        return hasRandomAllocationExecuted;
    }

}

