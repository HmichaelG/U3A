using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System.Security.Claims;
using System.Text.Json;
using U3A.Components.Account.Pages;
using U3A.Components.Account.Pages.Manage;
using U3A.Data;

namespace U3A.Components.Account
{
    // These endpoints are required by the Identity Razor components defined in the /Components/Account/Pages directory of this project.
    internal static class IdentityComponentsEndpointRouteBuilderExtensions
    {
        public static IEndpointConventionBuilder MapAdditionalIdentityEndpoints(this IEndpointRouteBuilder endpoints)
        {
            ArgumentNullException.ThrowIfNull(endpoints);

            RouteGroupBuilder accountGroup = endpoints.MapGroup("/Account");

            _ = accountGroup.MapPost("/PerformExternalLogin", async (
                HttpContext context,
                [FromServices] IAntiforgery antiforgery,
                [FromServices] SignInManager<ApplicationUser> signInManager,
                [FromForm] string provider,
                [FromForm] string returnUrl) =>
            {
                // Validate antiforgery token on incoming POST
                await antiforgery.ValidateRequestAsync(context);

                IEnumerable<KeyValuePair<string, StringValues>> query = [
                    new("ReturnUrl", returnUrl),
                    new("Action", ExternalLogin.LoginCallbackAction)];

                string redirectUrl = UriHelper.BuildRelative(
                    context.Request.PathBase,
                    "/Account/ExternalLogin",
                    QueryString.Create(query));

                AuthenticationProperties properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
                return TypedResults.Challenge(properties, [provider]);
            });

            _ = accountGroup.MapPost("/Logout", async (
                HttpContext context,
                ClaimsPrincipal user,
                [FromServices] IAntiforgery antiforgery,
                [FromServices] SignInManager<ApplicationUser> signInManager,
                [FromForm] string returnUrl) =>
            {
                // Validate antiforgery token on incoming POST
                await antiforgery.ValidateRequestAsync(context);

                await signInManager.SignOutAsync();
                return TypedResults.LocalRedirect($"~/{returnUrl}");
            });

            RouteGroupBuilder manageGroup = accountGroup.MapGroup("/Manage").RequireAuthorization();

            _ = manageGroup.MapPost("/LinkExternalLogin", async (
                HttpContext context,
                [FromServices] IAntiforgery antiforgery,
                [FromServices] SignInManager<ApplicationUser> signInManager,
                [FromForm] string provider) =>
            {
                // Validate antiforgery token on incoming POST
                await antiforgery.ValidateRequestAsync(context);

                // Clear the existing external cookie to ensure a clean login process
                await context.SignOutAsync(IdentityConstants.ExternalScheme);

                string redirectUrl = UriHelper.BuildRelative(
                    context.Request.PathBase,
                    "/Account/Manage/ExternalLogins",
                    QueryString.Create("Action", ExternalLogins.LinkLoginCallbackAction));

                AuthenticationProperties properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl, signInManager.UserManager.GetUserId(context.User));
                return TypedResults.Challenge(properties, [provider]);
            });

            ILoggerFactory loggerFactory = endpoints.ServiceProvider.GetRequiredService<ILoggerFactory>();
            ILogger downloadLogger = loggerFactory.CreateLogger("DownloadPersonalData");

            _ = manageGroup.MapPost("/DownloadPersonalData", async (
                HttpContext context,
                [FromServices] IAntiforgery antiforgery,
                [FromServices] UserManager<ApplicationUser> userManager,
                [FromServices] AuthenticationStateProvider authenticationStateProvider) =>
            {
                // Validate antiforgery token on incoming POST
                await antiforgery.ValidateRequestAsync(context);

                ApplicationUser? user = await userManager.GetUserAsync(context.User);
                if (user is null)
                {
                    return Results.NotFound($"Unable to load user with ID '{userManager.GetUserId(context.User)}'.");
                }

                string userId = await userManager.GetUserIdAsync(user);
                downloadLogger.LogInformation("User with ID '{UserId}' asked for their personal data.", userId);

                // Only include personal data for download
                Dictionary<string, string> personalData = [];
                IEnumerable<System.Reflection.PropertyInfo> personalDataProps = typeof(ApplicationUser).GetProperties().Where(
                    prop => Attribute.IsDefined(prop, typeof(PersonalDataAttribute)));
                foreach (System.Reflection.PropertyInfo? p in personalDataProps)
                {
                    personalData.Add(p.Name, p.GetValue(user)?.ToString() ?? "null");
                }

                IList<UserLoginInfo> logins = await userManager.GetLoginsAsync(user);
                foreach (UserLoginInfo l in logins)
                {
                    personalData.Add($"{l.LoginProvider} external login provider key", l.ProviderKey);
                }

                personalData.Add("Authenticator Key", (await userManager.GetAuthenticatorKeyAsync(user))!);
                byte[] fileBytes = JsonSerializer.SerializeToUtf8Bytes(personalData);

                _ = context.Response.Headers.TryAdd("Content-Disposition", "attachment; filename=PersonalData.json");
                return TypedResults.File(fileBytes, contentType: "application/json", fileDownloadName: "PersonalData.json");
            });

            return accountGroup;
        }
    }
}
