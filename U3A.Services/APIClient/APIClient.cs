using PostmarkDotNet.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Postmark.Model.MessageStreams;
using Postmark.Model.Suppressions;
using PostmarkDotNet.Model.Webhooks;
using U3A.Model;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using DevExpress.XtraRichEdit.Model;
using System.Diagnostics;

namespace U3A.Services.APIClient;

public class APIClient : APIClientBase
{
    public APIClient() : base() { }

    public async Task<string> DoBuildSchedule(string tenant)
    {
        return await sendAPIRequestAsync(DurableActivity.DoBuildSchedule, tenant);
    }
    public async Task<string> DoProcessQueuedDocuments(string tenant, Guid ProcessID)
    {
        return await sendAPIRequestAsync(DurableActivity.DoProcessQueuedDocuments, tenant, ProcessID);
    }
    public async Task<string> DoCorrespondence(string tenant, IEnumerable<Guid> ProcessIDs)
    {
        return await sendAPIRequestAsync(DurableActivity.DoCorrespondence, tenant, ProcessIDs);
    }
    public async Task<string> DoAutoEnrolment(string tenant, IEnumerable<Guid> ProcessIDs)
    {
        return await sendAPIRequestAsync(DurableActivity.DoAutoEnrolment, tenant, ProcessIDs);
    }
    public async Task<string> DoDoFinalisePayments(string tenant)
    {
        return await sendAPIRequestAsync(DurableActivity.DoFinalisePayments, tenant);
    }
    public async Task<string> DoBringForwardEnrolments(string tenant)
    {
        return await sendAPIRequestAsync(DurableActivity.DoBringForwardEnrolments, tenant);
    }
    public async Task<string> DoSendRequestedLeaderReports(string tenant, IEnumerable<Guid> ProcessIDs)
    {
        return await sendAPIRequestAsync(DurableActivity.DoSendRequestedLeaderReports, tenant, ProcessIDs);
    }
    public async Task<string> DoCreateAttendance(string tenant)
    {
        return await sendAPIRequestAsync(DurableActivity.DoCreateAttendance, tenant);
    }
    public async Task<string> DoDatabaseCleanup(string tenant)
    {
        return await sendAPIRequestAsync(DurableActivity.DoDatabaseCleanup, tenant);
    }
    public async Task<Byte[]> DoCreateCorrespondenceAsPdf(string tenant, SendMail printDoc)
    {
        return await DoCreateCorrespondenceAsPdf(tenant, null, printDoc);
    }
    public async Task<Byte[]> DoCreateCorrespondenceAsPdf(string tenant, 
                                IEnumerable<Guid> ProcessID = null, SendMail printDoc = null)
    {
        var functionName = nameof(DoCreateCorrespondenceAsPdf);
        var request = new HttpRequestMessage(HttpMethod.Get, ConstructQuery(functionName, tenant, ProcessID));
        if (printDoc != null)
        {
            request.Content = new StringContent(JsonSerializer.Serialize(printDoc), System.Text.Encoding.UTF8, "application/json");
        }
        var response = await GetPdfReportAsync(request);
        if (response.Length == 0)
        {
            throw new Exception(@"No data returned from API.<br/> 
                        Your report(s) could not be created usually because there were no enrolments in the requested course.");
        }
        return response;
    }



}
