using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using System.Data.Common;
using U3A.Model;

namespace U3A.Database
{
    public class U3ADbContext : U3ADbContextBase
    {

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IDbContextFactory<TenantStoreDbContext> _TenantDbFactory;

        public U3ADbContext(TenantInfo tenantInfo)
        {
            TenantInfo = tenantInfo;
        }

        [ActivatorUtilitiesConstructor] // force DI to use this constructor
        public U3ADbContext(AuthenticationStateProvider? AuthStateProvider,
                    IDbContextFactory<TenantStoreDbContext> TenantDbFactory = default,
                    IHttpContextAccessor HttpContextAccessor = default)
        {
            if (AuthStateProvider != null) authenticationStateProvider = AuthStateProvider;
            _httpContextAccessor = HttpContextAccessor;
            _TenantDbFactory = TenantDbFactory;
            GetTenantInfo();
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (TenantInfo == null)
            {
                GetTenantInfo();
            }
            // Use the connection string to connect to the per-tenant database.
            optionsBuilder.EnableSensitiveDataLogging(true);
            optionsBuilder.UseSqlServer(TenantInfo.ConnectionString);
        }

        private void GetTenantInfo()
        {
            HostStrategy hs = new HostStrategy();

            var identifirer = hs.GetIdentifier(_httpContextAccessor.HttpContext.Request.Host.Host);
            using (var dbc = _TenantDbFactory.CreateDbContext())
            {
                TenantInfo = dbc.TenantInfo.AsNoTracking()
                    .FirstOrDefault(x => x.Identifier == identifirer);
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