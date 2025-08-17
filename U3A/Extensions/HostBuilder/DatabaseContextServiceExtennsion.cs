using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;
using U3A.Database;

namespace U3A.Extensions.HostBuilder;

public static class DatabaseContextServiceExtension
{
    public static WebApplicationBuilder AddDatabaseContext(this WebApplicationBuilder builder, string connectionString)
    {
        builder.Services.AddDbContext<TenantDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        }, ServiceLifetime.Scoped);

        builder.Services.AddDbContext<U3ADbContext>(ServiceLifetime.Scoped);

        // TenantDbContextFactory

        builder.Services.AddDbContextFactory<TenantDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        }, ServiceLifetime.Scoped);

        // U3ADbContextFactory
        builder.Services.AddDbContextFactory<U3ADbContext>(lifetime: ServiceLifetime.Scoped);

        // Enrich with Aspire extensions
        builder.EnrichSqlServerDbContext<U3ADbContext>();
        builder.EnrichSqlServerDbContext<TenantDbContext>();

        return builder;
    }
}
