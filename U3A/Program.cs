using U3A.Components;
using U3A.Components.Account;
using U3A.Data;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using U3A.Database;
using Blazored.LocalStorage;
using DevExpress.Drawing;
using Microsoft.AspNetCore.Components.Server.Circuits;
using U3A.Services;
using DevExpress.Blazor.RichEdit.SpellCheck;
using Microsoft.Extensions.FileProviders;
using System.Reflection;
using Azure.Identity;
using Microsoft.AspNetCore.Builder;
using DevExpress.XtraCharts;
using U3A.Model;
using System;

var builder = WebApplication.CreateBuilder(args);

//if (!builder.Environment.IsDevelopment())
//{
//    var uri = Environment.GetEnvironmentVariable("VaultUri");
//    if (uri != null )
//    {
//        var keyVaultEndpoint = new Uri(uri);
//        builder.Configuration.AddAzureKeyVault(keyVaultEndpoint, new DefaultAzureCredential());
//    }
//}

// **** Start local modifications ****

DevExpress.Utils.DeserializationSettings.RegisterTrustedAssembly(typeof(U3A.UI.Reports.ProFormaReportFactory).Assembly);
DevExpress.Drawing.Internal.DXDrawingEngine.ForceSkia();

foreach (var file in Directory.GetFiles(@"wwwroot/fonts"))
{
    DXFontRepository.Instance.AddFont(file);
}


// TenantDbContextFactory
string? MultiTenantConnectionString = builder.Configuration.GetConnectionString("TenantConnectionString");
if (MultiTenantConnectionString is null)
{
    MultiTenantConnectionString = Environment.GetEnvironmentVariable("TenantConnectionString");
}

builder.Services.AddDbContextFactory<TenantDbContext>(options =>
{
    options.UseSqlServer(MultiTenantConnectionString);
}, ServiceLifetime.Scoped);

// U3ADbContextFactory
builder.Services.AddDbContext<U3ADbContext>(options => options.UseSqlServer());
builder.Services.AddDbContextFactory<U3ADbContext>(options =>
{
    options.UseSqlServer();
}, ServiceLifetime.Scoped);

// Get / Set local storage data
builder.Services.AddBlazoredLocalStorage();

builder.Services.AddSingleton<CircuitHandler>(new CircuitHandlerService());

builder.Services.AddDevExpressBlazor().AddSpellCheck(opts =>
{
    opts.FileProvider = new EmbeddedFileProvider(Assembly.GetExecutingAssembly(), "U3A");
    opts.MaxSuggestionCount = 6;
    opts.AddToDictionaryAction = (word, culture) =>
    {
        //Write the selected word to a dictionary file
    };
    opts.Dictionaries.Add(new HunspellDictionary
    {
        DictionaryPath = Path.Combine("resources", "en_AU.dic"),
        GrammarPath = Path.Combine("resources", "en_AU.aff"),
        Culture = "en-AU"
    });
});


builder.Services.AddDevExpressServerSideBlazorReportViewer();
builder.Services.Configure<DevExpress.Blazor.Configuration.GlobalOptions>(options =>
{
    options.BootstrapVersion = DevExpress.Blazor.BootstrapVersion.v5;
    options.SizeMode = DevExpress.Blazor.SizeMode.Small;
});



builder.Services.Configure<reCAPTCHAVerificationOptions>(o =>
{
    o.Secret = builder.Configuration.GetValue<String>("GoogleReCAPTCHAv2Key")!;
});

builder.Services.AddTransient<ReCaptchaV2API>();
builder.Services.AddHttpClient();

builder.Services.AddRazorPages();

constants.IS_DEVELOPMENT = builder.Environment.IsDevelopment();

//**** End local modifications ****

// Add services to the container.
builder.Services.AddRazorComponents(options =>
    options.DetailedErrors = builder.Environment.IsDevelopment())
    .AddInteractiveServerComponents();

builder.Services.AddAuthorization();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
.AddIdentityCookies();

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.SignIn.RequireConfirmedAccount = true;
            options.User.RequireUniqueEmail = true;
        })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<U3ADbContext>()
    .AddSignInManager()
    .AddUserManager<UserManager<ApplicationUser>>()
    .AddDefaultTokenProviders();


builder.Services.AddScoped<IEmailSender<ApplicationUser>, IdentityEmailSender>();
builder.Services.AddScoped<TenantInfoService>();

if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddApplicationInsightsTelemetry(options =>
       options.ConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]);
}

var app = builder.Build();
app.UseRequestLocalization("en-AU");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.MapControllers();

//Error 404 fallback
app.MapFallbackToFile("/fallback.html");
app.Run();
