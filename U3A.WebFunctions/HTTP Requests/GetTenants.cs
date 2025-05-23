using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.Net;
using System.Text.Json;

namespace U3A.WebFunctions
{
    public class GetTenants
    {
        private readonly IConfiguration _config;

        public GetTenants(IConfiguration config)
        {
            _config = config;
        }

        [Function("GetTenants")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            var tenant = (await GetTenantAsync()).ToArray();
            var jsonResponse = JsonSerializer.Serialize(tenant);
            await response.WriteStringAsync(jsonResponse);
            return response;
        }

        private async Task<List<TenantDetail>> GetTenantAsync()
        {
            var result = new List<TenantDetail>();
            var cn = _config.GetConnectionString("TenantConnectionString");
            using (var cnn = new SqlConnection(cn))
            {
                var cmdText = @"SELECT [Website]
                                  ,[State]
                                  ,[Identifier]
                                  ,[Name]
                              FROM [dbo].[TenantInfo] where 
                                    UsePostmarkTestEnviroment=0 or
                                    (UsePostmarkTestEnviroment=1 and PostmarkSandboxAPIKey is null)
                                    order by Name";
                using (var cmd = new SqlCommand(cmdText, cnn))
                {
                    try
                    {
                        await cnn.OpenAsync();
                        using (var rdr = await cmd.ExecuteReaderAsync())
                        {
                            while (rdr.Read())
                            {
                                var t = new TenantDetail()
                                {
                                    tenantID = rdr.GetString(2),
                                    name = rdr.GetString(3),
                                    website = rdr.GetString(0),
                                };
                                result.Add(t);
                            }
                            await rdr.CloseAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.Message, "Error reading TenantInfo");
                    }
                    finally
                    {
                        await cnn.CloseAsync();
                    }
                }
            }
            return result;
        }
    }

    public class TenantDetail
    {
        public string tenantID { get; set; } = "";
        public string name { get; set; } = "";
        public string website { get; set; } = "";
    }
}
