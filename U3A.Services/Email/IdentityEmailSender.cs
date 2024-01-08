using DevExpress.Pdf;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using System.Linq;
using System.Net;
using System.Reactive.Subjects;
using Twilio.Http;
using U3A.Data;
using U3A.Database;
using U3A.Model;
using static DevExpress.Data.Filtering.Helpers.SubExprHelper.ThreadHoppingFiltering;

namespace U3A.Services
{
    public class IdentityEmailSender : IEmailSender<ApplicationUser>
    {
       readonly IDbContextFactory<U3ADbContext> dbFactory;
        public IdentityEmailSender(IDbContextFactory<U3ADbContext> contextFactory)
        {
            dbFactory = contextFactory;
        }
        public async Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
        {
            var emailText = $"<a href='{confirmationLink}'>Click here to confirm your changed email address.</a>";
            await SendEmailAsync(email,"U3A Member Portal email confirmation", emailText);
        }

        public async Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
        {
            await SendEmailAsync(email, "U3A Member Portal password reset", resetCode);
        }

        public async Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
        {
            var emailText = $"<a href='{ resetLink}'>Click here to confirm your U3A Member Protal account.</a>";
            await SendEmailAsync(email, "U3A Member Portal password reset", emailText);
        }
        private async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            using (var dbc = await dbFactory.CreateDbContextAsync())
            {
                var settings = await dbc.SystemSettings
                                .OrderBy(x => x.ID)
                                .FirstOrDefaultAsync() ?? throw new ArgumentNullException(nameof(SystemSettings));
                var sender = EmailFactory.GetIdentityEmailSender(dbc);
                var result = await sender.SendEmailAsync(EmailType.Transactional,
                                                       settings.SendEmailAddesss,
                                                       settings.SendEmailDisplayName,
                                                       email, email, subject, GetEmailText(settings, htmlMessage),
                                                       htmlMessage);
            }
        }
        private string GetEmailText(SystemSettings settings, string HtmLMessgae)
        {
            string result = $"<h3>{settings.U3AGroup}</h3><p>ABN: {settings.ABN}<br/>" +
                                $"{settings.OfficeLocation}<br/>Phone: {settings.Phone}</p>" +
                                $"<p>{HtmLMessgae}</p>";
            return result;
        }
    }
    //public class IdentityEmailSenderOLD : IEmailSender
    //{

    //    IDbContextFactory<U3ADbContext> dbFactory;
    //    public IdentityEmailSender(IDbContextFactory<U3ADbContext> contextFactory)
    //    {
    //        dbFactory = contextFactory;
    //    }
    //    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    //    {
    //        using (var dbc = await dbFactory.CreateDbContextAsync())
    //        {
    //            var settings = await dbc.SystemSettings
    //                            .OrderBy(x => x.ID)
    //                            .FirstOrDefaultAsync() ?? throw new ArgumentNullException(nameof(SystemSettings));
    //            var sender = EmailFactory.GetIdentityEmailSender(dbc);
    //            var result = await sender.SendEmailAsync(EmailType.Transactional,
    //                                                   settings.SendEmailAddesss,
    //                                                   settings.SendEmailDisplayName,
    //                                                   email, email, subject, GetEmailText(settings, htmlMessage),
    //                                                   htmlMessage);
    //        }
    //    }

    //    private string GetEmailText(SystemSettings settings, string HtmLMessgae)
    //    {
    //        string result = $"<h3>{settings.U3AGroup}</h3><p>ABN: {settings.ABN}<br/>" +
    //                            $"{settings.OfficeLocation}<br/>Phone: {settings.Phone}</p>" +
    //                            $"<p>{HtmLMessgae}</p>";
    //        return result;
    //    }

    //}

}
