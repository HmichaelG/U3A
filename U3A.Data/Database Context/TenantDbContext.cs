// Copyright Finbuckle LLC, Andrew White, and Contributors.
// Refer to the solution LICENSE file for more information.

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using U3A.Model;

namespace U3A.Database
{

    public class TenantDbContext : DbContext
    {
        internal AuthenticationStateProvider authenticationStateProvider;
        private readonly string cnnStr;

        public TenantDbContext(String ConnectionString) { 
            cnnStr = ConnectionString;
        }

        [ActivatorUtilitiesConstructor] // force DI to use this constructor
        public TenantDbContext(DbContextOptions<TenantDbContext> options,
                          AuthenticationStateProvider? AuthStateProvider) : base(options)
        {
            authenticationStateProvider = AuthStateProvider;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.EnableSensitiveDataLogging(true);
                optionsBuilder.UseSqlServer(cnnStr);
            }
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

        public DbSet<ContactRequest> ContactRequest { get; set; }
        public DbSet<MultiCampusSchedule> MultiCampusSchedule { get; set; }
        public DbSet<MultiCampusPerson> MultiCampusPerson { get; set; }
        public DbSet<MultiCampusEnrolment> MultiCampusEnrolment { get; set; }
        public DbSet<MultiCampusSendMail> MultiCampusSendMail { get; set; }
        public DbSet<MultiCampusTerm> MultiCampusTerm { get; set; }
        public DbSet<TenantInfo> TenantInfo { get; set; }
        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
                            CancellationToken cancellationToken = default(CancellationToken))
        {
            OnBeforeSaving();
            return (await base.SaveChangesAsync(acceptAllChangesOnSuccess,
                          cancellationToken));
        }

        private void OnBeforeSaving()
        {
            var entries = ChangeTracker.Entries();
            var utcNow = DateTime.Now;
            foreach (var entry in entries)
            {
                // for entities that inherit from BaseEntity,
                // set UpdatedOn / CreatedOn appropriately
                if (entry.Entity is BaseEntity trackable)
                {
                    switch (entry.State)
                    {
                        case EntityState.Modified:
                            // set the updated date to "now"
                            trackable.UpdatedOn = utcNow;

                            // mark property as "don't touch"
                            // we don't want to update on a Modify operation
                            entry.Property("CreatedOn").IsModified = false;
                            if (authenticationStateProvider != null)
                            {
                                try
                                {
                                    trackable.User = authenticationStateProvider.GetAuthenticationStateAsync().Result.User.Identity.Name;
                                }
                                catch
                                {
                                    trackable.User = "ASPNET Identity";
                                }
                            }
                            break;

                        case EntityState.Added:
                            // set both updated and created date to "now"
                            trackable.CreatedOn = utcNow;
                            trackable.UpdatedOn = utcNow;
                            if (authenticationStateProvider != null)
                            {
                                try
                                {
                                    trackable.User = authenticationStateProvider.GetAuthenticationStateAsync().Result.User.Identity.Name;
                                }
                                catch
                                {
                                    trackable.User = "ASPNET Identity";
                                }
                            }
                            break;
                    }
                }
            }
        }


    }
}