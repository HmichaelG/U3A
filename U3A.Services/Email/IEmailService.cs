using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using U3A.Database;
using U3A.Model;

namespace U3A.Services
{
    public interface IEmailService
    {
        public string Service { get; }

        public event EventHandler<BatchEmailSentEventArgs> BatchEmailSentEvent;
        public Task<string> SendEmailAsync(EmailType EmailType,
                                        string FromAddress,
                                        string FromDisplayName,
                                        string ToAddress,
                                        string ToDisplayName,
                                        string Subject,
                                        string HtmlMessage,
                                        string PlainTextMessage);
        public Task<string> SendEmailAsync(EmailType EmailType,
                                        string FromAddress,
                                        string FromDisplayName,
                                        string ToAddress,
                                        string ToDisplayName,
                                        string Subject,
                                        string HtmlMessage,
                                        string PlainTextMessage,
                                        IEnumerable<String>? PDFFileAttachments,
                                        IEnumerable<String>? PDFFileAttachmentFinalNames);
        public Task<string> SendEmailAsync(EmailType EmailType,
                                        string FromAddress,
                                        string FromDisplayName,
                                        string ToAddress,
                                        string ToDisplayName,
                                        string Subject,
                                        string HtmlMessage,
                                        string PlainTextMessage,
                                        IEnumerable<Byte[]>? PDFFileAttachments,
                                        IEnumerable<String>? PDFFileAttachmentFinalNames);

        public Task<string> SendEmailToMultipleRecipientsAsync(EmailType EmailType,
                                        string FromAddress,
                                        string FromDisplayName,
                                        List<string> ToAddress,
                                        List<string> ToDisplayName,
                                        string Subject,
                                        string HtmlMessage,
                                        string PlainTextMessage,
                                        IEnumerable<String>? PDFFileAttachments,
                                        IEnumerable<String>? PDFFileAttachmentFinalNames);
        public Task<string> SendEmailToMultipleRecipientsAsync(EmailType EmailType,
                                        string FromAddress,
                                        string FromDisplayName,
                                        List<string> ToAddress,
                                        List<string> ToDisplayName,
                                        string Subject,
                                        string HtmlMessage,
                                        string PlainTextMessage,
                                        IEnumerable<Byte[]>? PDFFileAttachments,
                                        IEnumerable<String>? PDFFileAttachmentFinalNames);

        public bool WasTransmissionSuccessful { get; set; }
    }

    public class BatchEmailSentEventArgs : EventArgs
    {
        public string FromEmailAddress { get; set; }
        public string? FailedEmailAddress { get; set; }
        public string Subject { get; set; }
        public int BatchNumber { get; set; }
        public int EmailSent { get; set; }
        public int EmailFailed { get; set; }
        public string Response { get; set; }
    }

    public enum EmailType
    {
        Transactional,
        Broadcast
    }

    public static class EmailFactory
    {
        public static IEmailService? GetEmailSender(U3ADbContext dbc)
        {
            IEmailService? result = null;
            SystemSettings settings = dbc.SystemSettings.OrderBy(x => x.ID).FirstOrDefault();
            var tenantInfo = dbc.TenantInfo;
            if (tenantInfo.PostmarkAPIKey != null && !tenantInfo.UsePostmarkTestEnviroment)
            {
                result = new PostmarkEmailSender(tenantInfo.PostmarkAPIKey,
                                tenantInfo.UsePostmarkTestEnviroment, settings);
            }
            if (result == null && tenantInfo.PostmarkSandboxAPIKey != null && tenantInfo.UsePostmarkTestEnviroment)
            {
                result = new PostmarkEmailSender(tenantInfo.PostmarkSandboxAPIKey,
                                tenantInfo.UsePostmarkTestEnviroment, settings);
            }
            return result;
        }

        public static IEmailService? GetIdentityEmailSender(U3ADbContext dbc)
        {
            IEmailService? result = null;
            SystemSettings settings = dbc.SystemSettings.OrderBy(x => x.ID).FirstOrDefault();
            var tenantInfo = dbc.TenantInfo;
            // Never use the test environment
            if (tenantInfo.PostmarkAPIKey != null)
            {
                result = new PostmarkEmailSender(tenantInfo.PostmarkAPIKey,
                                tenantInfo.UsePostmarkTestEnviroment, settings);
            }
            return result;
        }
        public static IEmailService? GetEmailSender(string PostmarkKey, bool UsePostmarkTestEnvironment)
        {
            IEmailService? result = null;
            result = new PostmarkEmailSender(PostmarkKey, UsePostmarkTestEnvironment, null);
            return result;
        }
    }


}
