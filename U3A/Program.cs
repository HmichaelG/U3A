using Blazored.LocalStorage;
using DevExpress.Blazor.RichEdit.SpellCheck;
using DevExpress.Drawing;
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

var builder = WebApplication.CreateBuilder(args);

// **** Start local modifications ****

builder.Services.AddScoped<LocalTime>();
builder.Services.AddScoped<TenantInfoService>();

string? tenantConnectionString = builder.Configuration.GetConnectionString("TenantConnectionString");
if (tenantConnectionString is null)
{
    tenantConnectionString = Environment.GetEnvironmentVariable("TenantConnectionString");
}

LoggingLevelSwitch WarningLevelSwitch
        = new LoggingLevelSwitch(LogEventLevel.Warning);

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
    .MinimumLevel.Override("Microsoft", WarningLevelSwitch)
    .Enrich.FromLogContext()
    .Enrich.WithExceptionDetails()
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

//.Services.AddSerilog();
builder.Host.UseSerilog(Log.Logger);

DevExpress.Utils.DeserializationSettings.RegisterTrustedAssembly(typeof(U3A.UI.Reports.ProFormaReportFactory).Assembly);
DevExpress.Drawing.Internal.DXDrawingEngine.ForceSkia();

foreach (var file in Directory.GetFiles(@"wwwroot/fonts"))
{
    DXFontRepository.Instance.AddFont(file);
}


// TenantDbContextFactory
builder.Services.AddDbContextFactory<TenantDbContext>(options =>
{
    _ = options.UseSqlServer(tenantConnectionString);
}, ServiceLifetime.Scoped);

// U3ADbContextFactory
builder.Services.AddDbContext<U3ADbContext>(options => options.UseSqlServer());
builder.Services.AddDbContextFactory<U3ADbContext>(options =>
{
    _ = options.UseSqlServer();
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

// ***
// If a DxComboBox does not displaye its bind value correctly,
// temporarially uncomment the following line.
// Correct the issue by overiding GetHashCode & Equals in the class.
// Refer to Term & Person for inspiration.
// ***

//DevExpress.Blazor.CompatibilitySettings.ComboBoxCompatibilityMode = true;

builder.Services.AddDevExpressServerSideBlazorReportViewer();
builder.Services.AddDevExpressBlazor(options =>
{
    options.BootstrapVersion = DevExpress.Blazor.BootstrapVersion.v5;
    options.SizeMode = DevExpress.Blazor.SizeMode.Medium;
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

if (!builder.Environment.IsDevelopment())
{
    _ = builder.Services.AddApplicationInsightsTelemetry(options =>
       options.ConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]);
}

var app = builder.Build();
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
