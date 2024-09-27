using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;

namespace U3A.WebFunctions
{
    public class ReCaptchaClient
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _config;
        public ReCaptchaClient(ILoggerFactory loggerFactory,
                            IConfiguration config)
        {
            _logger = loggerFactory.CreateLogger<ReCaptchaClient>();
            _config = config;
        }

        [Function("ReCaptchaClient")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {

            var reCaptchaSecret = _config.GetValue(typeof(string), "GoogleReCaptchaClientKey");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            if (reCaptchaSecret != null)
            {
                response.WriteString((string)reCaptchaSecret);
            }
            return response;
        }
    }
}
