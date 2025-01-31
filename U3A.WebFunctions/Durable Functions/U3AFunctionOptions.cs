using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Http;
using System;
using U3A.Model;

namespace U3A.WebFunctions;
public class U3AFunctionOptions
{
    public U3AFunctionOptions() { }
    public U3AFunctionOptions(HttpRequestData req)
    {
        var queryStrings = req.Query.GetValues("tenant");
        if (queryStrings?.Count() > 0)
        {
            TenantIdentifier = queryStrings[0];
        }
        queryStrings = req.Query.GetValues("processId");
        if (queryStrings?.Count() > 0)
        {
            var ids = queryStrings[0].Split(',');
            foreach (var id in ids)
            {
                IdToProcess.Add(Guid.Parse(id));
            }
        }
    }
    public U3AFunctionOptions(HttpRequest req)
    {
        if (req.Query.TryGetValue("tenant", out var tenantValues) && tenantValues.Count > 0)
        {
            TenantIdentifier = tenantValues[0]!;
        }
        if (req.Query.TryGetValue("processId", out var processIdValues) && processIdValues.Count > 0)
        {
            var ids = processIdValues[0]!.Split(',');
            foreach (var id in ids)
            {
                IdToProcess.Add(Guid.Parse(id));
            }
        }
    }

    public DurableActivity DurableActivity { get; set; }
    public string TenantIdentifier { get; set; } = string.Empty;
    public bool HasRandomAllocationExecuted { get; set; } = false;
    public bool IsDailyProcedure { get; set; } = false;
    public List<Guid> IdToProcess { get; set; } = new();
}

