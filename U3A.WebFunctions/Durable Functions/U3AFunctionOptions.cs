using Microsoft.Azure.Functions.Worker.Http;
using U3A.Model;

namespace U3A.WebFunctions;

public class U3AFunctionOptions
{
    public U3AFunctionOptions() { }

    public U3AFunctionOptions(HttpRequestData req)
    {
        SetTenant(req);
        SetSendMailIdsToProcess(req);
    }

    public void SetTenant(HttpRequestData req)
    {
        string[]? queryStrings = req.Query.GetValues("tenant");
        if (queryStrings?.Count() > 0)
        {
            TenantIdentifier = queryStrings[0];
        }
    }

    public void SetSendMailIdsToProcess(HttpRequestData req)
    {
        string[]? queryStrings = req.Query.GetValues("processId");
        if (queryStrings?.Count() > 0)
        {
            string[] ids = queryStrings[0].Split(',');
            foreach (string id in ids)
            {
                SendMailIdsToProcess.Add(Guid.Parse(id));
            }
        }
    }
    public void SetEnrollmentIdsToProcess(HttpRequestData req)
    {
        string[]? queryStrings = req.Query.GetValues("processId");
        if (queryStrings?.Count() > 0)
        {
            string[] ids = queryStrings[0].Split(',');
            foreach (string id in ids)
            {
                EnrollmentIdsToProcess.Add(Guid.Parse(id));
            }
        }
    }

    public DurableActivity DurableActivity { get; set; }
    public string TenantIdentifier { get; set; } = string.Empty;
    public bool HasRandomAllocationExecuted { get; set; } = false;
    public bool IsDailyProcedure { get; set; } = false;
    public List<Guid> SendMailIdsToProcess { get; set; } = [];
    public List<Guid> EnrollmentIdsToProcess { get; set; } = [];
    public SendMail? PrintDoc { get; set; } = null; // Dummy SendMail containing Leader Report print options
}

