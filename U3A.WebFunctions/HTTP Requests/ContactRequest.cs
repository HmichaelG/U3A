using HttpMultipartParser;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Serilog;
using System.Net;
using System.Text;
using U3A.Services;

namespace U3A.WebFunctions
{
    public class ContactRequest
    {
        private readonly IConfiguration _config;

        public ContactRequest(IConfiguration config)
        {
            _config = config;
        }

        [Function("ContactRequest")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous,
                    "get", "post")]
                    HttpRequestData req)
        {

            // get form-body
            MultipartFormDataParser parsedFormBody = await MultipartFormDataParser.ParseAsync(req.Body);

            // parse form-data
            string token = parsedFormBody.GetParameterValue("token");
            string name = parsedFormBody.GetParameterValue("name");
            string email = parsedFormBody.GetParameterValue("email");
            string phone = parsedFormBody.GetParameterValue("phone");
            string u3a = parsedFormBody.GetParameterValue("u3a");
            string message = parsedFormBody.GetParameterValue("message");
            phone ??= string.Empty;
            message ??= string.Empty;

            Log.Information($"Request: {name}, Email: {email}, Phone: {phone}, U3A: {u3a}");

            HttpResponseData response = req.CreateResponse();
            bool result = await IsReCaptchaOK(token);
            if (result)
            {
                // log to database
                await WriteRequestAsync(name, email, phone, message, u3a);
                // respond to request
                response.StatusCode = HttpStatusCode.OK;
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                response.Headers.Add("Access-Control-Allow-Origin", "*");
                // send initial response email
                await SendInitialResponseEmailAsync(name, email, phone, message, u3a);
            }
            else
            {
                response.StatusCode = HttpStatusCode.Forbidden;
            }
            return response;
        }

        public async Task SendInitialResponseEmailAsync(string name, string email, string phone, string message, string u3a)
        {
            string? postmarkKey = _config.GetValue<string>("PostmarkKey");
            string? supportEmail = _config.GetValue<string>("SupportEmailAddress");
            IEmailService? emailSender = EmailFactory.GetEmailSender(postmarkKey!, false);
            string table = BuildTable(name, email, phone, message, u3a);
            string HTMMessage = @$"
            <h3>Thank you for contacting U3Admin.org.au</h3>
            <p>This is an automated email. Please do not respond.</p>
            <p>Your request has been received and will be responded to. 
                    Please however be aware that we are all volunteers and there may be a short delay.</p>
            <p>The table below displays the information we have received from you if any is incorrect, 
                    please inform us when we contact you.
            {table}
            <p>Again, thank you for your interest in our software.</p>
            <p><strong>The U3Admin support Team</strong></p>
            ";
            _ = await emailSender!.SendEmailAsync(EmailType.Transactional,
                "system@u3admin.org.au",
                "U3Admin system support",
                email,
                name,
                "Your U3A Administration software information request",
                HTMMessage,
                string.Empty);
            HTMMessage = $@"<p>The following U3Admin information request has been received...</p>
                            {table}
                            <p>Please respond at your earliest convenience.</p>
                            ";
            _ = await emailSender.SendEmailAsync(EmailType.Transactional,
                "system@u3admin.org.au",
                "U3Admin system support",
                supportEmail!,
                string.Empty,
                "HEADS UP!!! A U3Admin information request has been received",
                HTMMessage,
                string.Empty);
        }

        private string BuildTable(string name, string email, string phone, string message, string u3a)
        {
            StringBuilder txt = new();
            _ = txt.AppendLine(@"<table style='width: 100%;
                                        border: 1pt solid black;'>");
            _ = txt.AppendLine(@"<tr>
                                    <th style='text-align: left;
                                            padding-left: 10pt;
                                            border-bottom: 1pt solid black;'>
                                            Field</th>
                                    <th style='text-align: left;border-bottom: 1pt solid black;'>Your response</th>
                                </tr>");
            _ = txt.AppendLine(@$"<tr>
                                    <td style='text-align: left;
                                            padding-left: 10pt;'>
                                            <strong>Name</strong></td>
                                    <td>{name}</td>
                                    </tr>");
            _ = txt.AppendLine(@$"<tr>
                                    <td style='text-align: left;
                                            padding-left: 10pt;'>
                                            <strong>Email</strong></td>
                                    <td>{email}</td>
                                    </tr>");
            _ = txt.AppendLine(@$"<tr>
                                    <td style='text-align: left;
                                            padding-left: 10pt;'>
                                            <strong>Phone Number</strong></td>
                                    <td>{phone}</td>
                                    </tr>");
            _ = txt.AppendLine(@$"<tr>
                                    <td style='text-align: left;
                                            padding-left: 10pt;'>
                                            <strong>Your U3A</strong></td>
                                    <td>{u3a}</td>
                                    </tr>");
            if (!string.IsNullOrWhiteSpace(message))
            {
                _ = txt.AppendLine(@$"<tr>
                                    <td style='text-align: left;
                                            padding-left: 10pt;'>
                                            <strong>Message</strong></td>
                                    <td>{message}</td>
                                    </tr>");
            }
            _ = txt.AppendLine(@$"<tr>
                                    <td colspan='3' >
                                    </td>
                                    </tr>");
            _ = txt.AppendLine("</table>");
            return txt.ToString();
        }
        public async Task<bool> IsReCaptchaOK(string token)
        {
            bool result = false;
            HttpClient httpClient = new();
            object? reCaptchaSecret = _config.GetValue(typeof(string), "GoogleReCaptchaKey");
            HttpResponseMessage res = await httpClient.GetAsync($"https://www.google.com/recaptcha/api/siteverify?secret={reCaptchaSecret}&response={token}");
            if (res.StatusCode != HttpStatusCode.OK)
            {
                return result;
            }
            string JSONresult = await res.Content.ReadAsStringAsync();
            dynamic JSONdata = JObject.Parse(JSONresult);

            if (JSONdata.success == "true")
            {
                if (JSONdata.score > 0.5m)
                {
                    Log.Information($"ReCaptcha passed - Score: {JSONdata.score}");
                    result = true;
                }
                else
                {
                    Log.Information($"ReCaptcha failed - Score: {JSONdata.score}");
                    result = false;
                }
            }
            else
            {
                string[] errors = JSONdata["error-codes"];
                Log.Information($"ReCaptcha failed - Error: {string.Concat(errors, " | ")}");
                result = false;
            }

            return result;
        }

        private async Task WriteRequestAsync(string name, string email, string phone, string message, string u3a)
        {
            string? cn = _config.GetConnectionString("TenantConnectionString");
            using SqlConnection cnn = new(cn);
            string cmdText = @"INSERT INTO [dbo].[ContactRequest]
                                   ([Id]
                                   ,[Name]
                                   ,[Email]
                                   ,[PhoneNumber]
                                   ,[Message]
                                   ,[CreatedOn]
                                   ,[UpdatedOn]
                                   ,[User]
                                   ,[U3A])
                             VALUES
                                   (@Id
                                   ,@Name
                                   ,@Email
                                   ,@PhoneNumber
                                   ,@Message
                                   ,@CreatedOn
                                   ,@UpdatedOn
                                   ,@User
                                   ,@U3A)";
            using SqlCommand cmd = new(cmdText, cnn);
            try
            {
                _ = cmd.Parameters.AddWithValue("Id", Guid.NewGuid());
                _ = cmd.Parameters.AddWithValue("Name", name);
                _ = cmd.Parameters.AddWithValue("Email", email);
                _ = cmd.Parameters.AddWithValue("PhoneNumber", phone);
                _ = cmd.Parameters.AddWithValue("Message", message);
                _ = cmd.Parameters.AddWithValue("@U3A", u3a);
                _ = cmd.Parameters.AddWithValue("@CreatedOn", DateTime.UtcNow);
                _ = cmd.Parameters.AddWithValue("@UpdatedOn", DateTime.UtcNow);
                _ = cmd.Parameters.AddWithValue("@User", "Initial Web Request");
                await cnn.OpenAsync();
                _ = await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, "Error writing ContactRequest");
            }
            finally
            {
                await cnn.CloseAsync();
            }
        }

    }
}
