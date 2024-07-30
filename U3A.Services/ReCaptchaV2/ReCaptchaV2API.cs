using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace U3A.Services
{
    public class ReCaptchaV2API
    {
        private IHttpClientFactory HttpClientFactory { get; }

        private IOptionsMonitor<reCAPTCHAVerificationOptions> reCAPTCHAVerificationOptions { get; }

        public ReCaptchaV2API(IHttpClientFactory httpClientFactory, IOptionsMonitor<reCAPTCHAVerificationOptions> reCAPTCHAVerificationOptions)
        {
            this.HttpClientFactory = httpClientFactory;
            this.reCAPTCHAVerificationOptions = reCAPTCHAVerificationOptions;
        }

        public async Task<(bool Success, string[] ErrorCodes)> Post(string reCAPTCHAResponse)
        {
            // { "secret", this.reCAPTCHAVerificationOptions.CurrentValue.Secret},
            var s = "6LcT11spAAAAAG_OoyJ7eFHpHpSTYXuMyKJd5Bjb";
            var url = "https://www.google.com/recaptcha/api/siteverify";
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                {"secret", s},
                {"response", reCAPTCHAResponse}
            });

            var httpClient = this.HttpClientFactory.CreateClient();
            var response = await httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var verificationResponse = await response.Content.ReadAsAsync<reCAPTCHAVerificationResponse>();
            if (verificationResponse.Success) return (Success: true, ErrorCodes: new string[0]);

            return (
                Success: false,
                ErrorCodes: verificationResponse.ErrorCodes.Select(err => err.Replace('-', ' ')).ToArray());
        }
    }
}
