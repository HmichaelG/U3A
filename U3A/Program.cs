using Blazored.LocalStorage;
using DevExpress.Blazor.RichEdit.SpellCheck;
using DevExpress.Drawing;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Filters;
using Serilog.Sinks.MSSqlServer;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.Reflection;
using U3A.Components;
using U3A.Components.Account;
using U3A.Data;
using U3A.Database;
using U3A.Model;
using U3A.Services;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Azure;
using DevExpress.Blazor.Reporting;
using DevExpress.AIIntegration.Blazor.Reporting.Viewer.Models;
using DevExpress.AIIntegration;
using Microsoft.Extensions.Options;
using DevExpress.XtraPrinting.Native;
using OpenAI;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
// Add services to the container.
builder.Services.AddRazorComponents(options =>
    options.DetailedErrors = builder.Environment.IsDevelopment())
    .AddInteractiveServerComponents();

// **** Start local modifications ****

builder.Services.AddScoped<LocalTime>();
builder.Services.AddScoped<TenantInfoService>();

string? tenantConnectionString = builder.Configuration.GetConnectionString("TenantConnectionString");
if (tenantConnectionString is null)
{
    tenantConnectionString = Environment.GetEnvironmentVariable("TenantConnectionString");
}

LoggingLevelSwitch FatalLevelSwitch
        = new LoggingLevelSwitch(LogEventLevel.Fatal);

var columnOptions = new ColumnOptions
{
    AdditionalColumns = new Collection<SqlColumn>
    {
        new SqlColumn
                { ColumnName = "Tenant", PropertyName = "Tenant", DataType = SqlDbType.NVarChar, DataLength = 64 },
        new SqlColumn
                { ColumnName = "User", PropertyName = "User", DataType = SqlDbType.NVarChar, DataLength = 64 },
        new SqlColumn
                { ColumnName = "LogEvent", PropertyName = "LogEvent", DataType = SqlDbType.NVarChar, DataLength = 64 },
    }
};

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", FatalLevelSwitch)
    .Enrich.FromLogContext()
    .Enrich.WithExceptionDetails()
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(Matching.WithProperty("AutoEnrolParticipants"))
        .WriteTo.MSSqlServer(connectionString: tenantConnectionString,
                                formatProvider: new CultureInfo("en-AU"),
                                sinkOptions: new MSSqlServerSinkOptions
                                {
                                    TableName = "LogAutoEnrol"
                                },
                                    columnOptions: columnOptions
                                )
        )
    .WriteTo.Console(formatProvider: new CultureInfo("en-AU"))
    .WriteTo.MSSqlServer(connectionString: tenantConnectionString,
                            formatProvider: new CultureInfo("en-AU"),
                            restrictedToMinimumLevel: LogEventLevel.Error,
                            sinkOptions: new MSSqlServerSinkOptions
                            {
                                TableName = "LogEvents"
                            },
                                columnOptions: columnOptions
                            )
    .CreateLogger();

builder.Host.UseSerilog(Log.Logger);

DevExpress.Utils.DeserializationSettings.RegisterTrustedAssembly(typeof(U3A.UI.Reports.ProFormaReportFactory).Assembly);
DevExpress.Utils.DeserializationSettings.RegisterTrustedAssembly(typeof(U3A.Model.Class).Assembly);
DevExpress.Drawing.Settings.DrawingEngine = DrawingEngine.Default;
foreach (var file in Directory.GetFiles(@"wwwroot/fonts"))
{
    DXFontRepository.Instance.AddFont(file);
}


// TenantDbContextFactory
builder.Services.AddDbContextFactory<TenantDbContext>(options =>
{
    options.UseSqlServer(tenantConnectionString);
}, ServiceLifetime.Scoped);

// U3ADbContextFactory
builder.Services.AddDbContext<U3ADbContext>();

builder.Services.AddDbContextFactory<U3ADbContext>(options =>
{
    options.UseSqlServer();
}, ServiceLifetime.Scoped);

// Get / Set local storage data
builder.Services.AddBlazoredLocalStorage();

builder.Services.AddSingleton<CircuitHandler, CircuitHandlerService>();
builder.Services.AddScoped<IErrorBoundaryLogger, ErrorBoundaryLoggingService>();

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
builder.Services.AddDevExpressBlazor(options =>
{
    options.BootstrapVersion = DevExpress.Blazor.BootstrapVersion.v5;
});

builder.Services.AddDevExpressServerSideBlazorPdfViewer();

builder.Services.Configure<reCAPTCHAVerificationOptions>(o =>
{
    o.Secret = builder.Configuration.GetValue<String>("GoogleReCAPTCHAv2Key")!;
});

builder.Services.AddTransient<ReCaptchaV2API>();
builder.Services.AddHttpClient();

builder.Services.AddRazorPages();

constants.IS_DEVELOPMENT = builder.Environment.IsDevelopment();

builder.Services.AddScoped<WorkStation>();

string AIuri = builder.Configuration.GetValue<String>("AzureAIEndpoint")!;
string AIkey = builder.Configuration.GetValue<String>("AzureAIKey")!;
string openAIkey = builder.Configuration.GetValue<String>("OpenAIKey")!;
string model = "gpt-4o-mini";

IChatClient chatClient = new AzureOpenAIClient(
    new Uri(AIuri),
    new AzureKeyCredential(AIkey)).AsChatClient(model);

IChatClient client = new ChatClientBuilder(chatClient)
         .ConfigureOptions(x => {
             x.Temperature = 0.2f;
             x.TopP = 0.1f;
             x.MaxOutputTokens = 10|000;       
         })
         .Build();

var aiAssistant = new OpenAI.OpenAIClient(openAIkey);
builder.Services.AddDevExpressBlazor();
builder.Services.AddChatClient(client);
builder.Services.AddDevExpressAI(config =>
{
    config.RegisterOpenAIAssistants(aiAssistant, "gpt-4o-mini");
    config.AddBlazorReportingAIIntegration(options =>
    {
        options.Languages = new List<LanguageItem>() {
            new LanguageItem() { Key = "en", Text = "English" },
            new LanguageItem() { Key = "ch", Text = "Chinese" },
            new LanguageItem() { Key = "fr", Text = "French" },
            new LanguageItem() { Key = "de", Text = "German" },
            new LanguageItem() { Key = "gr", Text = "Greek" },
            new LanguageItem() { Key = "it", Text = "Italian" },
            new LanguageItem() { Key = "jp", Text = "Japanese" },
            new LanguageItem() { Key = "es", Text = "Spanish" },
            new LanguageItem() { Key = "vn", Text = "Vietnamese" }
            };
        options.SummarizationMode = SummarizationMode.Abstractive;
    });
});


//**** End local modifications ****


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
            options.Password.RequireDigit = false;
            options.Password.RequiredLength = 8;
            options.Password.RequiredUniqueChars = 5;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;
        })
    .AddTop100000PasswordValidator<ApplicationUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<U3ADbContext>()
    .AddSignInManager()
    .AddUserManager<UserManager<ApplicationUser>>()
    .AddDefaultTokenProviders();


builder.Services.AddScoped<IEmailSender<ApplicationUser>, IdentityEmailSender>();

if (!builder.Environment.IsDevelopment())
{
    _ = builder.Services.AddApplicationInsightsTelemetry(options =>
       options.ConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]);
}

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.None;
});
builder.Services.AddAntiforgery(options =>
{
    // options.Cookie.Expiration = TimeSpan.FromSeconds(0);
});

var app = builder.Build();

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
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (BadHttpRequestException ex)
    {
        // Handle the exception
        if (ex.InnerException is AntiforgeryValidationException)
        {
            context.Response.Redirect("/");
        }
    }
});

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.MapControllers();

app.MapFallbackToFile("/fallback.html");

app.Run();


