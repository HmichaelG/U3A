using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text;
using U3A.Database;
using U3A.Model;

namespace U3A.BusinessRules
{
    public static partial class BusinessRule
    {
        public static async Task<TenantInfo> GetTenantInfoAsync(
                                IDbContextFactory<TenantStoreDbContext> TenantDbFactory, 
                                IHttpContextAccessor httpContextAccessor)
        {
            HostStrategy hs = new HostStrategy();

            var identifirer = hs.GetIdentifier(httpContextAccessor.HttpContext.Request.Host.Host);
            TenantInfo tenantInfo = null;
            using (var dbc = TenantDbFactory.CreateDbContext())
            {
                tenantInfo = dbc.TenantInfo.AsNoTracking()
                    .FirstOrDefault(x => x.Identifier == identifirer);
                if (tenantInfo == null)
                {
                    tenantInfo = dbc.TenantInfo
                        .AsNoTracking()
                        .FirstOrDefault(x => x.Identifier == "demo"); //finbuckle leftover
                }
            }
            return tenantInfo;
        }

    }
}
