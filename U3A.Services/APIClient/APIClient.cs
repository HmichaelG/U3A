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

public class APIClient
{
    private string _authToken = string.Empty;
    private string _apiBaseAddress = string.Empty;
    public APIClient(string serverToken, string apiBaseAddress = "")
    {
        _authToken = serverToken;
        _apiBaseAddress = apiBaseAddress;
    }

    public APIClient(string apiBaseUri = "")
    {
        _apiBaseAddress = apiBaseUri;
    }

    public async Task<string> DoBuildSchedule(string tenant = "")
    {
        var function = "DoBuildSchedule";
        var request = new HttpRequestMessage(HttpMethod.Get, ConstructQuery(function,tenant));
        var response = await SendAsync(request);
        return response;
    }

    private string ConstructQuery(string function, string tenant)
    {
        var query = string.Empty;
        if (string.IsNullOrWhiteSpace(tenant))
        {
            query = function;
        }else
        {
            query = $"{function}?tenant={tenant}";
        }
        return query;
    }

    private async Task<string> SendAsync(HttpRequestMessage requestMessage)
    {
        using (HttpClient client = new HttpClient())
        {
            var responseBody = string.Empty;
            // Set the base address and headers
            if (!string.IsNullOrEmpty(_apiBaseAddress))
            {
                client.BaseAddress = new Uri(_apiBaseAddress);
            }
            else
            {
                client.BaseAddress = new Uri("http://localhost:7071/api/");
            }
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Add authentication headers if needed
            if (!string.IsNullOrEmpty(_authToken))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _authToken);
            }

            try
            {
                HttpResponseMessage response = await client.SendAsync(requestMessage);
                response.EnsureSuccessStatusCode();
                responseBody = await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Request error: {e.Message}");
            }
            return responseBody;
        }
    }
}
