﻿using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
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
        public TenantInfoService(
                                IDbContextFactory<TenantDbContext> TenantDbFactory,
                                IHttpContextAccessor httpContextAccessor)
        { 
            this.TenantDbFactory = TenantDbFactory;
            this.httpContextAccessor = httpContextAccessor;
        }
        public async Task<TenantInfo> GetTenantInfoAsync()
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