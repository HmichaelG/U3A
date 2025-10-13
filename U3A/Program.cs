using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using U3A.Components;
using U3A.Components.Account;
using U3A.Extensions.HostBuilder;
using U3A.Model;
using U3A.ServiceDefaults;
using U3A.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Required by Serilog
builder.Services.AddHttpContextAccessor();

// Environment
string? tenantConnectionString = builder.Configuration.GetConnectionString("TenantConnectionString");
tenantConnectionString ??= Environment.GetEnvironmentVariable("TenantConnectionString")
        ?? throw new ArgumentNullException("The TenantConnectionString is not defined.");
string? recaptureKey = builder.Configuration.GetValue<string>("GoogleReCAPTCHAv2Key");

// Add services to the container.
builder.Services.AddRazorComponents(options =>
    options.DetailedErrors = builder.Environment.IsDevelopment())
    .AddInteractiveServerComponents();

builder.Services.AddScoped<DxThemesService>();
builder.Services.AddScoped<LocalTime>();
builder.Services.AddScoped<TenantInfoService>();
builder.UseSerilogLogging(tenantConnectionString);
builder.AddDatabaseContext(tenantConnectionString);
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddSingleton<CircuitHandler, CircuitHandlerService>();
builder.Services.AddScoped<IErrorBoundaryLogger, ErrorBoundaryLoggingService>();
builder.AddDevExpressService();

if (recaptureKey != null)
{
    _ = builder.Services.Configure<reCAPTCHAVerificationOptions>(o => o.Secret = recaptureKey);
}

builder.Services.AddTransient<ReCaptchaV2API>();
builder.Services.AddHttpClient();
builder.Services.AddRazorPages();
builder.Services.AddScoped<WorkstationService>();
builder.AddAIChatService(AIChatServiceExtension.ChatServiceType.Azure);
builder.AddIdentityService();

constants.IS_DEVELOPMENT = builder.Environment.IsDevelopment();
_ = !builder.Environment.IsDevelopment()
    ? builder.Services.AddApplicationInsightsTelemetry(options =>
       options.ConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"])
    : builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.Configure<CookiePolicyOptions>(options =>
    {
        options.MinimumSameSitePolicy = SameSiteMode.None;
    });

// Aspire service defaults
builder.AddServiceDefaults();

builder.Services.AddAntiforgery();

WebApplication app = builder.Build();

app.MapDefaultEndpoints();

app.UseRequestLocalization("en-AU");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    _ = app.UseMigrationsEndPoint();
}
else
{
    _ = app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    _ = app.UseHsts();
}

app.UseHttpsRedirection();

app.MapStaticAssets();

app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

app.UseAntiforgery();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.MapControllers();

app.MapFallbackToFile("/fallback.html");

app.Run();


