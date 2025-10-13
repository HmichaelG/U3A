using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using U3A.Components.Account;
using U3A.Data;
using U3A.Database;
using U3A.Services;

namespace U3A.Extensions.HostBuilder;

public static class IdentityServiceExtension
{
    public static WebApplicationBuilder AddIdentityService(this WebApplicationBuilder builder)
    {

        _ = builder.Services.AddCascadingAuthenticationState();
        _ = builder.Services.AddScoped<IdentityUserAccessor>();
        _ = builder.Services.AddScoped<IdentityRedirectManager>();
        _ = builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

        _ = builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = IdentityConstants.ApplicationScheme;
            options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
        })
        .AddIdentityCookies(options =>
            options.TwoFactorRememberMeCookie!.Configure(a => a.ExpireTimeSpan = TimeSpan.FromDays(30)));

        _ = builder.Services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.SignIn.RequireConfirmedAccount = true;
            options.User.RequireUniqueEmail = true;
            options.Password.RequireDigit = false;
            options.Password.RequiredLength = 8;
            options.Password.RequiredUniqueChars = 5;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;
            options.Lockout.AllowedForNewUsers = false;
            options.Lockout.MaxFailedAccessAttempts = int.MaxValue;
        })
            .AddTop100000PasswordValidator<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<U3ADbContext>()
            .AddSignInManager()
            .AddUserManager<UserManager<ApplicationUser>>()
            .AddDefaultTokenProviders();

        _ = builder.Services.AddScoped<IEmailSender<ApplicationUser>, IdentityEmailSender>();

        return builder;
    }
}