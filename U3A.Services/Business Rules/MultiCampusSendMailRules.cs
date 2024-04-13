using DevExpress.Blazor;
using DevExpress.Blazor.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using U3A.Database;
using U3A.Model;

namespace U3A.BusinessRules
{
    public static partial class BusinessRule
    {
        public static async Task<IEnumerable<SendMail>> GetMultiCampusMailAsync(TenantDbContext dbcT,string Tenant)
        {
            List<SendMail> result = new();
            var mcMail = await dbcT.MultiCampusSendMail.Where(x => x.TenantIdentifier == Tenant).ToListAsync();
            foreach (var m in mcMail)
            {
                var mcPerson = dbcT.MultiCampusPerson.Find(m.PersonID);
                if (mcPerson != null)
                {
                    result.Add(BusinessRule.GetSendMailFromMCSendMail(m,mcPerson));
                }
            }
            return result;
        }
        public static async Task CreateMultiCampusEnrolmentSendMailAsync(
                                            TenantDbContext dbcT, 
                                            IEnumerable<Class> classes,
                                            SystemSettings settings,
                                            DateTime? AsAt = null)
        {
            var list = new List<MultiCampusEnrolment>();
            var entries = dbcT.ChangeTracker.Entries<MultiCampusEnrolment>();
            foreach (var entry in entries)
                if (entry.Entity is MultiCampusEnrolment r)
                {
                    if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
                    {
                        list.Add(r);
                    }
                }
            await DoParticipantMultiCampusEnrolmentAsync(dbcT, list.ToArray(), AsAt);
            await DoLeaderMultiCampusEnrolmentAsync(dbcT, classes,settings, list.ToArray());
        }

        private static async Task DoParticipantMultiCampusEnrolmentAsync(
                                    TenantDbContext dbcT, MultiCampusEnrolment[] enrolments, DateTime? AsAt)
        {
            var reportName = "Participant Enrolment";
            foreach (var e in enrolments)
            {
                if (!(await dbcT.MultiCampusSendMail.Where(x => x.RecordKey == e.ID
                            && x.PersonID == e.PersonID
                            && x.DocumentName == reportName
                            && string.IsNullOrWhiteSpace(x.Status)).AnyAsync()))
                {
                    var mail = new MultiCampusSendMail()
                    {
                        TenantIdentifier = e.TenantIdentifier,
                        DocumentName = reportName,
                        PersonID = e.PersonID,
                        TermID = e.TermID,
                        RecordKey = e.ID,
                        CreatedOn = AsAt,
                    };
                    await dbcT.AddAsync(mail);
                }
            }
        }
        private static async Task DoLeaderMultiCampusEnrolmentAsync(
                                    TenantDbContext dbcT, 
                                    IEnumerable<Class> classes,
                                    SystemSettings settings,
                                    MultiCampusEnrolment[] enrolments)
        {
            var reportName = "Leader Report";
            var keys = new List<string>();
            foreach (var e in enrolments.Where(x => !x.IsWaitlisted))
            {
                var t = await dbcT.MultiCampusTerm.FindAsync(e.TermID);
                if (e.ClassID != null)
                {
                    // Different participants in each class
                    var c = classes.FirstOrDefault(x => x.ID ==e.ClassID);
                    foreach (var p in await BusinessRule.GetLeaderReportRecipients(dbcT, settings, t, c))
                    {
                        if (!(await dbcT.MultiCampusSendMail.Where(x => x.RecordKey == c.ID           // Record key is the classID
                                    && x.TermID == e.TermID
                                    && x.PersonID == p.ID
                                    && x.DocumentName == reportName
                                    && string.IsNullOrWhiteSpace(x.Status)).AnyAsync()))
                        {
                            var key = p.ID.ToString() + c.ID.ToString();
                            if (!keys.Contains(key))
                            {
                                var mail = new MultiCampusSendMail()
                                {
                                    TenantIdentifier = e.TenantIdentifier,
                                    DocumentName = reportName,
                                    PersonID = p.ID,
                                    RecordKey = c.ID,
                                    TermID = e.TermID
                                };
                                await dbcT.AddAsync(mail);
                                keys.Add(key);
                            }
                        }
                    }
                }
                else
                {
                    // Same participants in all classes.
                    // Each class can have a different leader, so make sure we get them all
                    var courseClasses = classes.Where(x => x.CourseID == e.CourseID).ToList();
                    foreach (var c in courseClasses)
                    {
                        foreach (var p in await BusinessRule.GetLeaderReportRecipients(dbcT, settings, t, c))
                        {
                            if (!(await dbcT.MultiCampusSendMail
                                    .Where(x => x.RecordKey == e.CourseID       //Record key is the CourseID
                                        && x.TermID == e.TermID
                                        && x.PersonID == p.ID
                                        && x.DocumentName == reportName
                                        && string.IsNullOrWhiteSpace(x.Status)).AnyAsync()))
                            {
                                var key = p.ID.ToString() + e.CourseID.ToString();
                                if (!keys.Contains(key))
                                {
                                    var mail = new MultiCampusSendMail()
                                    {
                                        TenantIdentifier = e.TenantIdentifier,
                                        DocumentName = reportName,
                                        PersonID = p.ID,
                                        RecordKey = e.CourseID,
                                        TermID = e.TermID
                                    };
                                    await dbcT.AddAsync(mail);
                                    keys.Add(key);
                                }
                            }
                        }
                    }
                }
            }
        }

        public static async Task<List<MultiCampusPerson>> GetLeaderReportRecipients(TenantDbContext dbc,
                                    SystemSettings settings, MultiCampusTerm term, Class Class)
        {
            var result = new List<MultiCampusPerson>();
            var course = Class.Course;
            var sendReportsTo = settings.SendLeaderReportsTo;
            if (course.SendLeaderReportsTo != null) { sendReportsTo = course.SendLeaderReportsTo.Value; }
            switch (sendReportsTo)
            {
                case SendLeaderReportsTo.LeadersThenClerks:
                    result.AddRange(await GetMCLeaders(dbc, Class));
                    if (result.Count <= 0) { result.AddRange(await GetMCClerks(dbc, term, Class)); }
                    break;
                case SendLeaderReportsTo.ClerksThenLeaders:
                    result.AddRange(await GetMCClerks(dbc, term, Class));
                    if (result.Count <= 0) { result.AddRange(await GetMCLeaders(dbc, Class)); }
                    break;
                case SendLeaderReportsTo.Both:
                    result.AddRange(await GetMCLeaders(dbc, Class));
                    result.AddRange(await GetMCClerks(dbc, term, Class));
                    break;
                default:  // do not send
                    break;
            }
            return result;
        }

        private static async Task<IEnumerable<MultiCampusPerson>> GetMCLeaders(TenantDbContext dbc, Class Class)
        {
            var result = new List<MultiCampusPerson>();
            MultiCampusPerson person;
            if (Class.LeaderID != null)
            {
                person = await dbc.MultiCampusPerson.FindAsync(Class.LeaderID);
                if (person != null && person.Communication == "Email") { result.Add(person); }
            }
            if (Class.Leader2ID != null)
            {
                person = await dbc.MultiCampusPerson.FindAsync(Class.Leader2ID);
                if (person != null && person.Communication == "Email") { result.Add(person); }
            }
            if (Class.Leader3ID != null)
            {
                person = await dbc.MultiCampusPerson.FindAsync(Class.Leader3ID);
                if (person != null && person.Communication == "Email") { result.Add(person); }
            }
            return result;
        }

        private static async Task<List<MultiCampusPerson>> GetMCClerks(TenantDbContext dbc, MultiCampusTerm term, Class Class)
        {
            var result = new List<MultiCampusPerson>();
            var course = Class.Course;
            IEnumerable<MultiCampusEnrolment> clerkEnrolments = null;
            if (course != null)
            {
                var participationType = (ParticipationType)course.CourseParticipationTypeID;
                if (participationType == ParticipationType.SameParticipantsInAllClasses)
                {
                    clerkEnrolments = await dbc.MultiCampusEnrolment.Where(x => x.TermID == term.ID &&
                                                                x.CourseID == course.ID &&
                                                                x.IsCourseClerk && !x.IsWaitlisted).ToListAsync(); ;
                }
                else
                {
                    clerkEnrolments = await dbc.MultiCampusEnrolment.Where(x => x.TermID == term.ID &&
                                                                x.ClassID == Class.ID &&
                                                                x.IsCourseClerk && !x.IsWaitlisted).ToListAsync(); ;
                }
            }
            if (clerkEnrolments != null)
            {
                foreach (var e in clerkEnrolments)
                {
                    var person = dbc.MultiCampusPerson.Find(e.PersonID);
                    if (person.Communication == "Email") result.Add(person);
                }
            }
            return result;
        }


    }
}