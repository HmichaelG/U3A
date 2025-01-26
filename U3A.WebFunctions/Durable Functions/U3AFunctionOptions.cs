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
    }
    public DurableActivity DurableActivity { get; set; }
    public string TenantIdentifier { get; set; } = string.Empty;
    public bool HasRandomAllocationExecuted { get; set; } = false;
    public bool IsDailyProcedure { get; set; } = false;
}

