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

namespace U3A.Services.APIClient;

public class APIClient : APIClientBase
{
    public APIClient() : base() { }

    public async Task<string> DoBuildSchedule(string tenant = "")
    {
        var function = "DoBuildSchedule";
        return await sendAPIRequestAsync(function, tenant);
    }
    public async Task<string> DoProcessQueuedDocuments(string tenant = "")
    {
        var function = "DoProcessQueuedDocuments";
        return await sendAPIRequestAsync(function, tenant);
    }


}
