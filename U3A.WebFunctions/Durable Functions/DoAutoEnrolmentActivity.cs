using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using U3A.WebFunctions.Procedures;
using U3A.Model;
using Serilog.Core;
using U3A.Database;
using DevExpress.Xpo;


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
            var tenant = GetTenant(options.TenantIdentifier, cn);
            if (tenant != null)
            {
                logger.LogInformation($"****** Started {nameof(DoAutoEnrolmentActivity)} for {tenant.Identifier}: {tenant.Name}. ******");
                try
                {
                    await LogStartTime(logger, tenant);
                    if (options.EnrollmentIdsToProcess.Count > 0)
                    {
                        await AutoEnrollParticipants.ProcessByEnrolment(tenant, options.EnrollmentIdsToProcess, cn!, logger);
                        hasRandomAllocationExecuted = false;
                    }
                    else
                    {
                        hasRandomAllocationExecuted = await AutoEnrollParticipants.Process(tenant, cn!, logger);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Error processing {nameof(DoAutoEnrolmentActivity)} for {tenant.Identifier}");
                }
            }
            else { throw new NullReferenceException("Database connection string is null"); }
        }
        return hasRandomAllocationExecuted;
    }

    [Function("DoAutoEnrolment")]
    public async Task<HttpResponseData> DoAutoEnrolment(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
    [DurableClient] DurableTaskClient client,
    FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger(nameof(DoAutoEnrolment));
        var options = new U3AFunctionOptions()
        {
            DurableActivity = DurableActivity.DoAutoEnrolment
        };
        options.SetTenant(req);
        options.SetEnrollmentIdsToProcess(req);

        List<Guid> enrolmentIDs = new();
        enrolmentIDs.AddRange(options.EnrollmentIdsToProcess);

        // Get the list of SendMail ID's to process correspondence
        var cn = config.GetConnectionString(Common.TENANT_CN_CONFIG);
        if (cn != null)
        {
            var tenant = GetTenant(options.TenantIdentifier, cn);
            if (tenant != null)
            {
                using (var dbc = new U3ADbContext(tenant))
                {
                    foreach (var enrolment in dbc.Enrolment
                            .Where(x => enrolmentIDs.Contains(x.ID)))
                    {
                        using (var dbc1 = new U3ADbContext(tenant))
                        {
                            foreach (var sendMail in dbc1.SendMail
                                        .Where(x => x.RecordKey == enrolment.ID
                                                    && x.PersonID == enrolment.PersonID
                                                    && string.IsNullOrWhiteSpace(x.Status)))
                            {
                                options.SendMailIdsToProcess.Add(sendMail.ID);
                            }
                        }
                    }
                }
            }
        }

        return await ScheduleFunctionAsync(client, executionContext, req, options);
    }

}

