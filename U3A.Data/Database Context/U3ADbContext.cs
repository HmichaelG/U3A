using DevExpress.Office.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
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
        public U3ADbContext(AuthenticationStateProvider? AuthStateProvider,
                    IDbContextFactory<TenantDbContext> TenantDbFactory = default,
                    IHttpContextAccessor HttpContextAccessor = default,
                    LocalTime localTime = null)
        {
            if (AuthStateProvider != null) authenticationStateProvider = AuthStateProvider;
            _httpContextAccessor = HttpContextAccessor;
            _TenantDbFactory = TenantDbFactory;
            GetTenantInfo();
            this.UtcOffset = localTime.UtcOffset;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //optionsBuilder.EnableSensitiveDataLogging(true);
            //optionsBuilder.EnableDetailedErrors();
            //optionsBuilder.LogTo(Serilog.Log.Information, Microsoft.Extensions.Logging.LogLevel.Information);

            GetTenantInfo();
            if (TenantInfo != null)
            {
                // Use the connection string to connect to the per-tenant database.
                optionsBuilder.UseSqlServer(TenantInfo.ConnectionString);
            }
        }

        private void GetTenantInfo()
        {
            if (useCachedTenantInfo) { return; }
            HostStrategy hs = new HostStrategy();

            if (_httpContextAccessor == null) { return; }
            if (_httpContextAccessor.HttpContext == null) { return; }
            if (_httpContextAccessor.HttpContext.Request == null) { return; }
            if (_httpContextAccessor.HttpContext.Request.Host == null) { return; }
            var identifier = hs.GetIdentifier(_httpContextAccessor.HttpContext.Request.Host.Host);
            using (var dbc = _TenantDbFactory.CreateDbContext())
            {
                TenantInfo = dbc.TenantInfo.AsNoTracking()
                    .FirstOrDefault(x => x.Identifier == identifier);
                if (TenantInfo == null)
                {
                    TenantInfo = dbc.TenantInfo
                        .AsNoTracking()
                        .FirstOrDefault(x => x.Identifier == "demo"); //finbuckle leftover
                }
            }
        }
    }
}