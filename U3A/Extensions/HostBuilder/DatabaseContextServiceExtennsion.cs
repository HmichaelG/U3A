using Microsoft.EntityFrameworkCore;
using U3A.Database;

namespace U3A.Extensions.HostBuilder;

public static class DatabaseContextServiceExtension
{
    public static WebApplicationBuilder AddDatabaseContext(this WebApplicationBuilder builder, string connectionString)
    {
        _ = builder.Services.AddDbContext<TenantDbContext>(options =>
        {
            _ = options.UseSqlServer(connectionString);
        }, ServiceLifetime.Scoped);

        _ = builder.Services.AddDbContext<U3ADbContext>(ServiceLifetime.Scoped);

        // TenantDbContextFactory

        _ = builder.Services.AddDbContextFactory<TenantDbContext>(options =>
        {
            _ = options.UseSqlServer(connectionString);
        }, ServiceLifetime.Scoped);

        // U3ADbContextFactory
        _ = builder.Services.AddDbContextFactory<U3ADbContext>(lifetime: ServiceLifetime.Scoped);

        // Enrich with Aspire extensions
        builder.EnrichSqlServerDbContext<U3ADbContext>();
        builder.EnrichSqlServerDbContext<TenantDbContext>();

        return builder;
    }
}
