using Microsoft.Azure.Functions.Worker.Http;

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
    public string TenantIdentifier { get; set; } = string.Empty;

    public bool DoFinalisePayments { get; set; } = false;
    public bool DoAutoEnrolment { get; set; } = false;
    public bool DoBringForwardEnrolments { get; set; } = false;
    public bool DoCreateAttendance { get; set; } = false;
    public bool DoCorrespondence { get; set; } = false;
    public bool DoSendLeaderReports { get; set; } = false;
    public bool DoMembershipAlertsEmail { get; set; } = false;
    public bool DoProcessQueuedDocuments { get; set; } = false;
    public bool DoBuildSchedule { get; set; } = false;
    public bool DoDatabaseCleanup { get; set; } = false;

}

