using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using System.Net;

namespace U3A.WebFunctions
{
    public class ReCaptchaClient
    {
        private readonly IConfiguration _config;
        public ReCaptchaClient(IConfiguration config)
        {
            _config = config;
        }

        [Function("ReCaptchaClient")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {

            var reCaptchaSecret = _config.GetValue(typeof(string), "GoogleReCaptchaClientKey");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            if (reCaptchaSecret != null)
            {
                await response.WriteStringAsync((string)reCaptchaSecret);
            }
            return response;
        }
    }
}
