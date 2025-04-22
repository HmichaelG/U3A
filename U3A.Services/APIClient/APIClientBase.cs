using Microsoft.AspNetCore.Http;
using Postmark.Model.MessageStreams;
using Postmark.Model.Suppressions;
using PostmarkDotNet.Model;
using PostmarkDotNet.Model.Webhooks;
using System;
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Linq;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using U3A.Model;

namespace U3A.Services.APIClient;

public class U3AFunctionsHttpClient
{
    private const string variableName = "U3A-FUNCTIONS-KEY";
    private static readonly string _apiBaseAddress = (constants.IS_DEVELOPMENT)
            ? $"{Environment.GetEnvironmentVariable("aspire-webfunctions-url")}/api/"
            : "https://u3a-functions.azurewebsites.net/api/"
            ;
    private static readonly Lazy<HttpClient> lazyHttpClient = new Lazy<HttpClient>(() => new HttpClient());

    private U3AFunctionsHttpClient()
    {
    }

    public static HttpClient Instance()
    {
        var client = lazyHttpClient.Value;
        if (client.BaseAddress == null)
        {
            var e = Environment.GetEnvironmentVariable("aspire-webfunctions-url");
            var authToken = Environment.GetEnvironmentVariable(variableName);
            client.BaseAddress = new Uri(_apiBaseAddress);
            // Add authentication headers if needed
            if (!string.IsNullOrEmpty(authToken))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            }
        }
        return client;
    }
}
public abstract class APIClientBase : IDisposable
{
    readonly HttpClient _httpClient;
    public APIClientBase()
    {
        _httpClient = U3AFunctionsHttpClient.Instance();
    }

    internal async Task<string> sendAPIRequestAsync(DurableActivity durableActivity, string tenant)
    {
        var functionName = Enum.GetName<DurableActivity>(durableActivity);
        var request = new HttpRequestMessage(HttpMethod.Get, ConstructQuery(functionName, tenant));
        var response = await SendAsync(request);
        return response;
    }
    internal async Task<string> sendAPIRequestAsync(DurableActivity durableActivity, string tenant, Guid ProcessID)
    {
        var functionName = Enum.GetName<DurableActivity>(durableActivity);
        var request = new HttpRequestMessage(HttpMethod.Get,
                            ConstructQuery(functionName, tenant, new List<Guid> { ProcessID }));
        var response = await SendAsync(request);
        return response;
    }
    internal async Task<string> sendAPIRequestAsync(DurableActivity durableActivity, string tenant, IEnumerable<Guid> ProcessID = null)
    {
        var functionName = Enum.GetName<DurableActivity>(durableActivity);
        var request = new HttpRequestMessage(HttpMethod.Get, ConstructQuery(functionName, tenant, ProcessID));
        var response = await SendAsync(request);
        return response;
    }

    internal async Task<string> SendAsync(HttpRequestMessage requestMessage)
    {
        var responseBody = string.Empty;
        AddJsonMediaType();
        HttpResponseMessage response = await _httpClient.SendAsync(requestMessage);
        response.EnsureSuccessStatusCode(); // Throw if not a success code.
        responseBody = await response.Content.ReadAsStringAsync();
        return responseBody;
    }
    internal async Task<Byte[]> GetPdfReportAsync(HttpRequestMessage requestMessage)
    {
        Byte[] pdf = null;
        AddJsonMediaType();
        HttpResponseMessage response = await _httpClient.SendAsync(requestMessage);
        response.EnsureSuccessStatusCode(); // Throw if not a success code.
        pdf = await response.Content.ReadAsByteArrayAsync();
        return pdf;
    }
    internal string ConstructQuery(string function, string tenant, IEnumerable<Guid>? ProcessIDs = null)
    {
        var query = string.Empty;
        //construct the query string
        if (string.IsNullOrWhiteSpace(tenant))
        {
            query = function;
        }
        else
        {
            query = $"{function}?tenant={tenant}";
            if (ProcessIDs != null)
            {
                var csv = string.Join(",", ProcessIDs);
                query += $"&processId={csv}";
            }
        }
        return query;
    }

    private void AddJsonMediaType()
    {
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public void Dispose()
    {
    }

}
