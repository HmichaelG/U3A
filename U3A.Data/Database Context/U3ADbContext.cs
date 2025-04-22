using DevExpress.Office.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System.ComponentModel;
using System.Data.Common;
using U3A.Model;
using static DevExpress.Office.Utils.HdcOriginModifier;

namespace U3A.Database
{
    public class U3ADbContext : U3ADbContextBase
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IDbContextFactory<TenantDbContext> _TenantDbFactory;
        private readonly bool useCachedTenantInfo = false;

        public U3ADbContext(TenantInfo tenantInfo)
        {
            TenantInfo = tenantInfo;
            useCachedTenantInfo = true;
        }

        [ActivatorUtilitiesConstructor] // force DI to use this constructor
        public U3ADbContext(
            AuthenticationStateProvider? AuthStateProvider,
            IDbContextFactory<TenantDbContext> TenantDbFactory = default,
            IHttpContextAccessor HttpContextAccessor = default,
            LocalTime localTime = null)
        {
            if (AuthStateProvider != null)
                authenticationStateProvider = AuthStateProvider;
            _httpContextAccessor = HttpContextAccessor;
            _TenantDbFactory = TenantDbFactory;
            GetTenantInfo();
            this.UtcOffset = localTime.UtcOffset;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.EnableSensitiveDataLogging(true);
            optionsBuilder.EnableDetailedErrors();
            optionsBuilder.UseLoggerFactory(LoggerFactory.Create(builder =>
            {
                builder.AddSerilog();
            }));
            GetTenantInfo();
            if (TenantInfo != null)
            {
                // Use the connection string to connect to the per-tenant database.
                optionsBuilder.UseSqlServer(TenantInfo.ConnectionString);
            }
        }

        private void GetTenantInfo()
        {
            if (useCachedTenantInfo)
            {
                return;
            }
            HostStrategy hs = new HostStrategy();

            if (_httpContextAccessor == null)
            {
                return;
            }
            if (_httpContextAccessor.HttpContext == null)
            {
                return;
            }
            if (_httpContextAccessor.HttpContext.Request == null)
            {
                return;
            }
            if (_httpContextAccessor.HttpContext.Request.Host == null)
            {
                return;
            }
            var identifier = hs.GetIdentifier(_httpContextAccessor.HttpContext.Request.Host.Host);
            if (identifier == "bs-local") identifier = "localhost"; // BrowserStack
            using (var dbc = _TenantDbFactory.CreateDbContext())
            {
                // Redirect console output to null
                Console.SetOut(TextWriter.Null);

                TenantInfo = dbc.TenantInfo.AsNoTracking().FirstOrDefault(x => x.Identifier == identifier);
                if (TenantInfo is null)
                {
                    TenantInfo = dbc.TenantInfo.AsNoTracking().FirstOrDefault(x => x.Identifier == "demo");
                }

                // Redirect console output back to standard output
                var standardConsole = new StreamWriter(Console.OpenStandardOutput());
                standardConsole.AutoFlush = true;
                Console.SetOut(standardConsole);
            }
        }
    }
}