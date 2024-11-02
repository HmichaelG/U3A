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

        public static async Task<List<LeaderReportRecipientsByClass>> GetLeaderReportRecipientsByClass(U3ADbContext dbc,
                                            SystemSettings settings, Term term, IEnumerable<Class> Classes)
        {
            var result = new List<LeaderReportRecipientsByClass>();
            foreach (var c in Classes)
            {
                var course = c.Course;
                if (course == null) { course = await dbc.Course.FindAsync(c.CourseID); }
                var sendReportsTo = settings.SendLeaderReportsTo;
                if (course.SendLeaderReportsTo != null) { sendReportsTo = course.SendLeaderReportsTo.Value; }
                IEnumerable<Person> recipients;
                CourseContactType contactType;
                int sortOrder = -1;
                switch (sendReportsTo)
                {
                    case SendLeaderReportsTo.LeadersThenClerks:
                        contactType = CourseContactType.Leader;
                        recipients = await GetLeaders(dbc, c);
                        if (recipients.Count() <= 0)
                        {
                            contactType = CourseContactType.Clerk;
                            recipients = await GetClerks(dbc, term, c);
                        }
                        foreach (var recipient in recipients)
                        {
                            sortOrder++;
                            result.Add(new() { Class = c, ContactType = contactType, SortOrder = sortOrder, Person = recipient });
                        }
                        break;
                    case SendLeaderReportsTo.ClerksThenLeaders:
                        contactType = CourseContactType.Clerk;
                        recipients = await GetClerks(dbc, term, c);
                        if (recipients.Count() <= 0)
                        {
                            contactType = CourseContactType.Leader;
                            recipients = await GetLeaders(dbc, c);
                        }
                        foreach (var recipient in recipients)
                        {
                            sortOrder++;
                            result.Add(new() { Class = c, ContactType = contactType, SortOrder = sortOrder, Person = recipient });
                        }
                        break;
                    case SendLeaderReportsTo.Both:
                        contactType = CourseContactType.Leader;
                        recipients = await GetLeaders(dbc, c);
                        foreach (var recipient in recipients)
                        {
                            result.Add(new() { Class = c, ContactType = contactType, Person = recipient });
                        }
                        contactType = CourseContactType.Clerk;
                        recipients = await GetClerks(dbc, term, c);
                        foreach (var recipient in recipients)
                        {
                            sortOrder++;
                            result.Add(new() { Class = c, ContactType = contactType, SortOrder = sortOrder, Person = recipient });
                        }
                        break;
                    default:  // do not send
                        break;
                }
            }
            return result;
        }
        public static async Task<List<Person>> GetLeaderReportRecipients(U3ADbContext dbc,
                                            SystemSettings settings, Term term, Class Class)
        {
            var result = new List<Person>();
            var course = Class.Course;
            if (course == null) { course = await dbc.Course.FindAsync(Class.CourseID); }
            var sendReportsTo = settings.SendLeaderReportsTo;
            if (course.SendLeaderReportsTo != null) { sendReportsTo = course.SendLeaderReportsTo.Value; }
            switch (sendReportsTo)
            {
                case SendLeaderReportsTo.LeadersThenClerks:
                    result.AddRange(await GetLeaders(dbc, Class));
                    if (result.Count <= 0) { result.AddRange(await GetClerks(dbc, term, Class)); }
                    break;
                case SendLeaderReportsTo.ClerksThenLeaders:
                    result.AddRange(await GetClerks(dbc, term, Class));
                    if (result.Count <= 0) { result.AddRange(await GetLeaders(dbc, Class)); }
                    break;
                case SendLeaderReportsTo.Both:
                    result.AddRange(await GetLeaders(dbc, Class));
                    result.AddRange(await GetClerks(dbc, term, Class));
                    break;
                default:  // do not send
                    break;
            }
            return result;
        }

        private static async Task<IEnumerable<Person>> GetLeaders(U3ADbContext dbc, Class Class)
        {
            var result = new List<Person>();
            Person person;
            if (Class.LeaderID != null)
            {
                person = await dbc.Person.FindAsync(Class.LeaderID);
                if (person != null && person.Communication == "Email") { result.Add(person); }
            }
            if (Class.Leader2ID != null)
            {
                person = await dbc.Person.FindAsync(Class.Leader2ID);
                if (person != null && person.Communication == "Email") { result.Add(person); }
            }
            if (Class.Leader3ID != null)
            {
                person = await dbc.Person.FindAsync(Class.Leader3ID);
                if (person != null && person.Communication == "Email") { result.Add(person); }
            }
            return result;
        }

        private static async Task<List<Person>> GetClerks(U3ADbContext dbc, Term term, Class Class)
        {
            var result = new List<Person>();
            var course = await dbc.Course.FindAsync(Class.CourseID);
            IEnumerable<Enrolment> clerkEnrolments = null;
            if (course != null)
            {
                var participationType = (ParticipationType)course.CourseParticipationTypeID;
                if (participationType == ParticipationType.SameParticipantsInAllClasses)
                {
                    clerkEnrolments = await dbc.Enrolment.Where(x => x.TermID == term.ID &&
                                                                x.CourseID == course.ID &&
                                                                x.IsCourseClerk && !x.IsWaitlisted).ToListAsync(); ;
                }
                else
                {
                    clerkEnrolments = await dbc.Enrolment.Where(x => x.TermID == term.ID &&
                                                                x.ClassID == Class.ID &&
                                                                x.IsCourseClerk && !x.IsWaitlisted).ToListAsync(); ;
                }
            }
            if (clerkEnrolments != null)
            {
                foreach (var e in clerkEnrolments)
                {
                    var person = await dbc.Person
                                    .IgnoreQueryFilters()
                                    .FirstOrDefaultAsync(p => p.ID == e.PersonID);
                    if (person != null && person.Communication == "Email") result.Add(person);
                }
            }
            return result;
        }
    }
}
