using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using U3A.Model;

namespace U3A.Database
{
    public class U3ADbContextSeed : U3ADbContextBase
    {

        static IConfigurationRoot _configuration;

        public U3ADbContextSeed() : base() { }

        public TenantInfo TenantInfo { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                string cnstr;
                if (TenantInfo == null)
                {
                    var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddUserSecrets<U3ADbContextSeed>()
                    .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);
                    _configuration = builder.Build();
                    cnstr = _configuration.GetConnectionString("SeedConnectionString");
                    optionsBuilder.EnableSensitiveDataLogging(true);
                }
                else { cnstr = TenantInfo.ConnectionString; }
                optionsBuilder.UseSqlServer(cnstr, sqlServerOptions => sqlServerOptions.CommandTimeout(120));
            }
        }
    }
}