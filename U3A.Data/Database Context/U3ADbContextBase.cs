﻿using DevExpress.Office.Utils;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Collections.Generic;
using U3A.Data;
using U3A.Model;
using Serilog;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace U3A.Database
{
    public abstract class U3ADbContextBase : IdentityDbContext<ApplicationUser, IdentityRole, string>
    {
        //Set in U3ADbContext ctor
        internal AuthenticationStateProvider authenticationStateProvider;
        public TenantInfo TenantInfo { get; set; }
        public TimeSpan UtcOffset { get; set; } = TimeSpan.Zero;
        public DateTime GetLocalTime(DateTime time) => time + UtcOffset;
        public DateTime? GetLocalTime(DateTime? time) => (time == null) ? null : time.Value + UtcOffset;
        public DateTime GetLocalTime() => DateTime.UtcNow + UtcOffset;
        public DateTime GetLocalDate() => GetLocalTime().Date;
        public DateTime GetLocalDate(DateTime time) => GetLocalTime(time).Date;
        public DateTime? GetLocalDate(DateTime? time) => (time == null) ? null : GetLocalTime(time.Value).Date;

        public DbSet<SystemSettings> SystemSettings { get; set; }
        public DbSet<PublicHoliday> PublicHoliday { get; set; }
        public DbSet<Occurrence> Occurrence { get; set; }
        public DbSet<CourseParticipationType> CourseParticpationType { get; set; }
        public DbSet<Term> Term { get; set; }
        public DbSet<WeekDay> WeekDay { get; set; }
        public DbSet<Course> Course { get; set; }
        public DbSet<Class> Class { get; set; }
        public DbSet<CancelClass> CancelClass { get; set; }
        public DbSet<AttendClass> AttendClass { get; set; }
        public DbSet<AttendClassStatus> AttendClassStatus { get; set; }
        public DbSet<CourseType> CourseType { get; set; }
        public DbSet<Venue> Venue { get; set; }
        public DbSet<Person> Person { get; set; }
        public DbSet<Contact> Contact { get; set; }
        public DbSet<Tag> Tag { get; set; }
        public DbSet<PersonImport> PersonImport { get; set; }
        public DbSet<PersonImportError> PersonImportError { get; set; }
        public DbSet<Enrolment> Enrolment { get; set; }
        public DbSet<DocumentTemplate> DocumentTemplate { get; set; }
        public DbSet<DocumentType> DocumentType { get; set; }
        public DbSet<Committee> Committee { get; set; }
        public DbSet<Volunteer> Volunteer { get; set; }
        public DbSet<Receipt> Receipt { get; set; }
        public DbSet<Fee> Fee { get; set; }
        public DbSet<ReceiptDataImport> ReceiptDataImport { get; set; }
        public DbSet<SendMail> SendMail { get; set; }
        public DbSet<OnlinePaymentStatus> OnlinePaymentStatus { get; set; }
        public DbSet<Dropout> Dropout { get; set; }
        public DbSet<Leave> Leave { get; set; }
        public DbSet<Report> Report { get; set; }
        public DbSet<DocumentQueue> DocumentQueue { get; set; }
        public DbSet<DocumentQueueAttachment> DocumentQueueAttachment { get; set; }
        public DbSet<MultiCampusSchedule> Schedule { get; set; }
        public DbSet<LeaderHistory> LeaderHistory { get; set; }
        public DbSet<LuckyMemberDraw> LuckyMemberDraw { get; set; }
        public DbSet<LuckyMemberDrawEntrant> LuckyMemberDrawEntrant { get; set; }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            OnBeforeSaving();
            int result = 0;
            try
            {
                result = base.SaveChanges(acceptAllChangesOnSuccess);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // Use safe last write wins strategy for concurrency for objects of BaseEntity type
                // using the version column as the concurrency token.
                foreach (var entry in ex.Entries)
                {
                    var currentValues = entry.CurrentValues;
                    var databaseValues = entry.GetDatabaseValues();

                    if (databaseValues != null)
                    {
                        entry.OriginalValues.SetValues(databaseValues);
                        entry.CurrentValues.SetValues(databaseValues);
                    }
                }

                result = base.SaveChanges(); // Retry with latest data
            }
            return result;
        }

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
                                    CancellationToken cancellationToken = default(CancellationToken))
        {
            OnBeforeSaving();
            int result = 0;
            try
            {
                result = (await base.SaveChangesAsync(acceptAllChangesOnSuccess,
                          cancellationToken));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // Use safe last write wins strategy for concurrency.
                foreach (var entry in ex.Entries)
                {
                    var currentValues = entry.CurrentValues;
                    var databaseValues = await entry.GetDatabaseValuesAsync(cancellationToken);
                    if (databaseValues != null)
                    {
                        entry.OriginalValues.SetValues(databaseValues);
                    }
                }
                result = await base.SaveChangesAsync(cancellationToken); // Retry with latest data
            }
            return result;
        }

        public override EntityEntry<TEntity> Remove<TEntity>(TEntity entity)
        {
            if (entity is Enrolment)
            {
                CreateDropout(entity);
            }
            if (entity is Person)
            {
                SetLeaderNull((entity as Person).ID);
            }
            if (entity is Contact)
            {
                SetLeaderNull((entity as Contact).ID);
            }
            if (entity is Course)
            {
                base.RemoveRange(Class.Where(x => x.CourseID == (entity as Course).ID));
                base.RemoveRange(Leave.Where(x => x.CourseID == (entity as Course).ID));
            }
            return base.Remove(entity);
        }

        public override void RemoveRange(IEnumerable<object> entities)
        {
            foreach (var entity in entities)
            {
                if (entity is Person)
                {
                    SetLeaderNull((entity as Person).ID);
                }
                if (entity is Contact)
                {
                    SetLeaderNull((entity as Contact).ID);
                }
                if (entity is Enrolment)
                {
                    CreateDropout(entity);
                }
            }
            base.RemoveRange(entities);
        }

        private void SetLeaderNull(Guid PersonID)
        {
            foreach (var c in Class.Where(x => x.LeaderID == PersonID)) { c.LeaderID = null; }
            foreach (var c in Class.Where(x => x.Leader2ID == PersonID)) { c.Leader2ID = null; }
            foreach (var c in Class.Where(x => x.Leader3ID == PersonID)) { c.Leader3ID = null; }
            foreach (var c in Committee.Where(x => x.PersonID == PersonID)) { c.PersonID = null; }
        }
        private void CreateDropout<TEntity>(TEntity entity) where TEntity : class
        {
            var e = Enrolment.Find((entity as Enrolment).ID);
            if (e != null)
            {
                var d = new Dropout()
                {
                    CourseID = e.CourseID,
                    Created = e.Created,
                    DateEnrolled = e.DateEnrolled,
                    IsWaitlisted = e.IsWaitlisted,
                    PersonID = e.PersonID,
                    TermID = e.TermID
                };
                if (e.ClassID != null) { d.Class = Class.Find(e.ClassID); }
                if (authenticationStateProvider != null)
                {
                    d.DeletedBy = authenticationStateProvider.GetAuthenticationStateAsync().Result.User.Identity.Name;
                }
                base.Add(d);
            }
        }


        private void OnBeforeSaving()
        {
            var entries = ChangeTracker.Entries();
            var utcNow = DateTime.UtcNow;
            var rnd = new Random(utcNow.Millisecond);
            foreach (var entry in entries)
            {
                if (entry.Entity is Enrolment e)
                {
                    if (e.IsWaitlisted)
                    {
                        e.DateEnrolled = null;// Waitlisted, therefore not enrolled.
                        e.IsCourseClerk = false;
                    }
                    else
                    {
                        // Not Waitlisted, therefore set DateEnrolled if required.
                        if (e.DateEnrolled == null) { e.DateEnrolled = utcNow; }
                    }
                }
                // Soft delete entities implementing ISoftDelete.
                // We do this first so we also pick up the BaseEntity stuff.
                if (entry is { State: EntityState.Deleted, Entity: ISoftDelete delete })
                {
                    entry.State = EntityState.Modified;
                    delete.IsDeleted = true;
                    delete.DeletedAt = utcNow;
                    if (entry is { Entity: Person deletePerson })
                    {
                        foreach (var enrol in Enrolment.Where(x => x.PersonID == deletePerson.ID))
                        {
                            enrol.IsDeleted = true;
                            enrol.DeletedAt = utcNow;
                        }
                        foreach (var receipt in Receipt.Where(x => x.PersonID == deletePerson.ID))
                        {
                            receipt.IsDeleted = true;
                            receipt.DeletedAt = utcNow;
                        }
                        foreach (var fee in Fee.Where(x => x.PersonID == deletePerson.ID))
                        {
                            fee.IsDeleted = true;
                            fee.DeletedAt = utcNow;
                        }
                    }
                }
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
                            if (entry.Entity is Person) entry.Property("PersonID").IsModified = false;
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
                            if (trackable.CreatedOn == null) { trackable.CreatedOn = utcNow; }
                            if (trackable.UpdatedOn == null) { trackable.UpdatedOn = utcNow; }
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Automatically adding IsDeleted filter to 
            // all LINQ queries that use ISoftDelete
            modelBuilder.Entity<Course>()
                .HasQueryFilter(x => x.IsDeleted == false);
            modelBuilder.Entity<Class>()
                .HasQueryFilter(x => x.IsDeleted == false);
            modelBuilder.Entity<AttendClass>()
                .HasQueryFilter(x => x.Class.IsDeleted == false);
            modelBuilder.Entity<CancelClass>()
                .HasQueryFilter(x => x.Class.IsDeleted == false);
            modelBuilder.Entity<Dropout>()
                .HasQueryFilter(x => x.Class.IsDeleted == false);
            modelBuilder.Entity<Enrolment>()
                .HasQueryFilter(x => x.IsDeleted == false);
            modelBuilder.Entity<Person>()
                .HasQueryFilter(x => x.IsDeleted == false
                                        && EF.Property<string>(x, "Discriminator") == "Person");
            modelBuilder.Entity<Enrolment>()
                    .HasIndex(x => new { x.TermID, x.CourseID, x.ClassID, x.PersonID })
                    .HasDatabaseName("idxUniqueEnrolments")
                    .IsUnique(true)
            .HasFilter("IsDeleted = 0");
            modelBuilder.Entity<Enrolment>()
            .HasIndex(x => new { x.CourseID, x.IsDeleted })
            .IncludeProperties(p => new { p.ClassID, p.Created, p.CreatedOn, p.DateEnrolled, p.DeletedAt, p.IsCourseClerk, p.IsWaitlisted, p.PersonID, p.TermID, p.UpdatedOn, p.User })
                    .HasDatabaseName("idxCourseID_IsDeleted")
                    .IsUnique(false);
            modelBuilder.Entity<Fee>()
                .HasQueryFilter(x => x.Person.IsDeleted == false);
            modelBuilder.Entity<Leave>()
                .HasQueryFilter(x => x.Person.IsDeleted == false);
            modelBuilder.Entity<SendMail>()
                .HasQueryFilter(x => x.Person.IsDeleted == false);
            modelBuilder.Entity<Volunteer>()
                .HasQueryFilter(x => x.Person.IsDeleted == false);
            modelBuilder.Entity<Receipt>()
                .HasQueryFilter(x => x.IsDeleted == false && x.Person.IsDeleted == false);

            modelBuilder.Entity<DocumentType>()
                        .Property(x => x.ID).ValueGeneratedNever();
            modelBuilder.Entity<DocumentType>().HasData(
                        new DocumentType()
                        {
                            ID = 0,
                            Name = "Email",
                            IsEmail = true,
                            IsPostal = false,
                            IsSMS = false,
                            IsEnrolmentSubDocument = false,
                            IsREceiptSubDocument = false
                        },
                        new DocumentType()
                        {
                            ID = 1,
                            Name = "Postal",
                            IsEmail = false,
                            IsPostal = true,
                            IsSMS = false,
                            IsEnrolmentSubDocument = false,
                            IsREceiptSubDocument = false
                        },
                        new DocumentType()
                        {
                            ID = 2,
                            Name = "SMS",
                            IsEmail = false,
                            IsPostal = false,
                            IsSMS = true,
                            IsEnrolmentSubDocument = false,
                            IsREceiptSubDocument = false
                        },
                        new DocumentType()
                        {
                            ID = 3,
                            Name = "EnrolmentSubdoc",
                            IsEmail = false,
                            IsPostal = false,
                            IsSMS = false,
                            IsEnrolmentSubDocument = true,
                            IsREceiptSubDocument = false
                        },
                        new DocumentType()
                        {
                            ID = 4,
                            Name = "ReceiptSubdoc",
                            IsEmail = false,
                            IsPostal = false,
                            IsSMS = false,
                            IsEnrolmentSubDocument = false,
                            IsREceiptSubDocument = true
                        });
            modelBuilder.Entity<CourseParticipationType>()
                        .Property(x => x.ID).ValueGeneratedNever();
            modelBuilder.Entity<CourseParticipationType>().HasData(
                        new CourseParticipationType { ID = 0, Name = "Same participants in all classes" },
                        new CourseParticipationType { ID = 1, Name = "Different participants in each class" }
                       );
            modelBuilder.Entity<AttendClassStatus>()
                        .Property(x => x.ID).ValueGeneratedNever();
            modelBuilder.Entity<AttendClassStatus>().HasData(
                        new AttendClassStatus { ID = 0, Status = "Present" },
                        new AttendClassStatus { ID = 1, Status = "Absent without apology" },
                        new AttendClassStatus { ID = 2, Status = "Absent with apology" }
                       );
            modelBuilder.Entity<Occurrence>()
                        .Property(x => x.ID).ValueGeneratedNever();
            modelBuilder.Entity<Occurrence>().HasData(
                        new Occurrence { ID = 0, Name = "Once Only", ShortName = "Once" },
                        new Occurrence { ID = 1, Name = "Daily", ShortName = "Daily" },
                        new Occurrence { ID = 2, Name = "Weekly", ShortName = "Weekly" },
                        new Occurrence { ID = 3, Name = "Fortnightly", ShortName = "F'nightly" },
                        new Occurrence { ID = 4, Name = "1st Week of Month", ShortName = "Week 1" },
                        new Occurrence { ID = 5, Name = "2nd Week of Month", ShortName = "Week 2" },
                        new Occurrence { ID = 6, Name = "3rd Week of Month", ShortName = "Week 3" },
                        new Occurrence { ID = 7, Name = "4th Week of Month", ShortName = "Week 4" },
                        new Occurrence { ID = 8, Name = "Last Week of Month", ShortName = "Last Week" },
                        new Occurrence { ID = 9, Name = "Every 5 Weeks", ShortName = "5 Weeks" },
                        new Occurrence { ID = 10, Name = "Every 6 Weeks", ShortName = "6 Weeks" },
                        new Occurrence { ID = 11, Name = "1st & 3rd Week of Month", ShortName = "Weeks 1 & 3" },
                        new Occurrence { ID = 12, Name = "2nd & 4th Week of Month", ShortName = "Weeks 2 & 4" },
                        new Occurrence { ID = 999, Name = "Unscheduled (Varies)", ShortName = "Varies" }
                       );
            modelBuilder.Entity<WeekDay>()
                        .Property(x => x.ID).ValueGeneratedNever();
            modelBuilder.Entity<WeekDay>().HasData(
                        new WeekDay { ID = 0, Day = "Sunday", ShortDay = "Sun" },
                        new WeekDay { ID = 1, Day = "Monday", ShortDay = "Mon" },
                        new WeekDay { ID = 2, Day = "Tuesday", ShortDay = "Tue" },
                        new WeekDay { ID = 3, Day = "Wednesday", ShortDay = "Wed" },
                        new WeekDay { ID = 4, Day = "Thursday", ShortDay = "Thu" },
                        new WeekDay { ID = 5, Day = "Friday", ShortDay = "Fri" },
                        new WeekDay { ID = 6, Day = "Saturday", ShortDay = "Sat" }
                       );
        }
    }
}