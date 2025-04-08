using DevExpress.Pdf;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using System;
using System.Linq;
using System.Net;
using System.Reactive.Subjects;
using System.Reflection;
using System.Security.Cryptography.Pkcs;
using Twilio.Http;
using U3A.Data;
using U3A.Database;
using U3A.Model;
using static DevExpress.Data.Filtering.Helpers.SubExprHelper.ThreadHoppingFiltering;

namespace U3A.Services;

public class IdentityEmailSender : IEmailSender<ApplicationUser>
{
    readonly IDbContextFactory<U3ADbContext> dbFactory;
    public IdentityEmailSender(IDbContextFactory<U3ADbContext> contextFactory)
    {
        dbFactory = contextFactory;
    }
    public async Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
    {
        var emailTemplate = ReadEmailTemplate("emailConfirmEmail");
        await SendEmailAsync(email, "U3A Member Portal email confirmation", emailTemplate, confirmationLink);
    }

    public async Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
    {
        var emailTemplate = ReadEmailTemplate("passwordResetEmail");
        await SendEmailAsync(email, "U3A Member Portal password reset",emailTemplate, resetCode);
    }

    public async Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
    {
        var emailTemplate = ReadEmailTemplate("passwordResetEmail");
        await SendEmailAsync(email, "U3A Member Portal password reset", emailTemplate,resetLink);
    }
    private async Task SendEmailAsync(string email, string subject, string emailTemplate,string identityLink)
    {
        using (var dbc = await dbFactory.CreateDbContextAsync())
        {
            var settings = await dbc.SystemSettings
                            .OrderBy(x => x.ID)
                            .FirstOrDefaultAsync() ?? throw new ArgumentNullException(nameof(SystemSettings));
            var sender = EmailFactory.GetIdentityEmailSender(dbc);
            var emailText = emailTemplate
                                .Replace("{identityLink}", identityLink)
                                .Replace("{u3aName}", settings.U3AGroup)
                                .Replace("{copyrightYear}", dbc.GetLocalDate().Year.ToString("D"))
                                .Replace("{tenantID}", dbc.TenantInfo.Identifier)
                                .Replace("{sendEmailDisplayName}", settings.SendEmailDisplayName);
            var result = await sender.SendEmailAsync(EmailType.Transactional,
                                                   settings.SendEmailAddesss,
                                                   settings.SendEmailDisplayName,
                                                   email, email, subject, emailText,
                                                   emailText);
        }
    }

    string ReadEmailTemplate(string templateName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"U3A.Services.Email.{templateName}.html";

        using (Stream stream = assembly.GetManifestResourceStream(resourceName))
        using (StreamReader reader = new StreamReader(stream))
        {
            return reader.ReadToEnd();
        }
    }

}
