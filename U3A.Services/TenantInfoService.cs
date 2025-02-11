using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Drawing;
using System.Linq;
using System.Text;
using U3A.Database;
using U3A.Model;

namespace U3A.Services
{
    public class TenantInfoService
    {
        private readonly IDbContextFactory<TenantDbContext> TenantDbFactory;
        private readonly IHttpContextAccessor httpContextAccessor;
        public TenantInfoService(IDbContextFactory<TenantDbContext> TenantDbFactory,
                                IHttpContextAccessor httpContextAccessor)
        {
            this.TenantDbFactory = TenantDbFactory;
            this.httpContextAccessor = httpContextAccessor;
        }

        public string GetUserIdentity()
        {
            var result = "Anonymous(Public)";
            if (httpContextAccessor?.HttpContext?.User?.Identity == null) { return result; }
            result = httpContextAccessor.HttpContext.User.Identity.Name;
            return result;
        }
        public async Task<TenantInfo> GetTenantInfoAsync()
        {
            HostStrategy hs = new HostStrategy();
            var identifier = hs.GetIdentifier(httpContextAccessor.HttpContext.Request.Host.Host);
            TenantInfo tenantInfo = null;
            using (var dbc = TenantDbFactory.CreateDbContext())
            {
                tenantInfo = await dbc.TenantInfo.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Identifier == identifier);
                if (tenantInfo == null)
                {
                    tenantInfo = await dbc.TenantInfo
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Identifier == "demo"); //finbuckle leftover
                }
            }
            return tenantInfo;
        }

        public async Task<string> GetTenantIdentifierAsync()
        {
            var tInfo = await GetTenantInfoAsync();
            return tInfo.Identifier;
        }

    }
}
