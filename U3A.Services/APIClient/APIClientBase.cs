using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
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

public abstract class APIClientBase :IDisposable
{
    readonly HttpClient _httpClient;
    readonly string _authToken;
    readonly string _apiBaseAddress;
    public APIClientBase()
    {
        _httpClient = new HttpClient();
        _authToken = string.Empty;
        _apiBaseAddress = (constants.IS_DEVELOPMENT)
            ? "http://localhost:7071/api/"
            : "http://localhost:7071/api/"
            ;
        _httpClient.BaseAddress = new Uri(_apiBaseAddress);
        // Add authentication headers if needed
        if (!string.IsNullOrEmpty(_authToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _authToken);
        }
    }

    internal async Task<string> sendAPIRequestAsync(string APIFunctionName, string tenant)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, ConstructQuery(APIFunctionName, tenant));
        var response = await SendAsync(request);
        return response;
    }

    private async Task<string> SendAsync(HttpRequestMessage requestMessage)
    {
        var responseBody = string.Empty;
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        try
        {
            HttpResponseMessage response = await _httpClient.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();
            responseBody = await response.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"Request error: {e.Message}");
        }
        return responseBody;
    }
    private string ConstructQuery(string function, string tenant)
    {
        var query = string.Empty;
        if (string.IsNullOrWhiteSpace(tenant))
        {
            query = function;
        }
        else
        {
            query = $"{function}?tenant={tenant}";
        }
        return query;
    }

    public void Dispose()
    {
        if (_httpClient != null) { _httpClient.Dispose(); }
    }

}
