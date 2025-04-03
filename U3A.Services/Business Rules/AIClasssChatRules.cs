using AngleSharp.Dom;
using AngleSharp.Html;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using DevExpress.Blazor;
using DevExpress.XtraRichEdit.Commands.Internal;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text;
using U3A.Database;
using U3A.Model;
using U3A.Services;
using static Azure.Core.HttpHeader;

namespace U3A.BusinessRules
{
    public static partial class BusinessRule
    {

        private static void AssignParticipants(Class c, List<Person> people, ScheduledClass? sc, Enrolment e)
        {
            if (!e.isLeader && !e.IsCourseClerk)
            {
                if (e.Person == null) { e.Person = people.Find(x => x.ID == e.PersonID); }
                if (e.Person != null)
                {
                    sc.People.Add(new ScheduledPerson()
                    {
                        Class = c.Course.Name,
                        SortOrder = e.Person.FullNameAlphaKey,
                        Name = e.Person.FullName,
                        Email = e.Person.Email,
                        Phone = e.Person.AdjustedHomePhone,
                        Mobile = e.Person.AdjustedMobile,
                        Roles = (e.IsWaitlisted)
                            ? new List<string>() { "Waitlisted" }
                            : new List<string>() { "Student" }
                    });
                }
            }
        }
        static async Task getCalendarForAiClass(U3ADbContext dbc, Term term,
            IEnumerable<Class> classes)
        {
            var dataStorage = await BusinessRule.GetCalendarDataStorageAsync(dbc, term);
            var range = new DxSchedulerDateTimeRange(dbc.GetLocalDate(), new DateTime(term.Year, 12, 31));
            Dictionary<Guid, List<DxSchedulerAppointmentItem>> classAppointments = new();
            foreach (var a in dataStorage?.GetAppointments(range))
            {
                Class c = (Class)a.CustomFields["Source"];
                if (c != null && (int)a.LabelId != 9)
                {
                    if (!classAppointments.ContainsKey(c.ID))
                    {
                        classAppointments.Add(c.ID, new List<DxSchedulerAppointmentItem>());
                    }
                    classAppointments[c.ID].Add(a);
                }
            }
            foreach (Class c in classes)
            {
                if (classAppointments.ContainsKey(c.ID))
                {
                    foreach (var a in classAppointments[c.ID])
                    {
                        c.ClassDates.Add(a.Start);
                    }
                }
            }

        }

        private static void GetContactsAsMarkdown(StringBuilder sb, Class c)
        {
            sb.AppendLine($"### Leaders");
            if (!string.IsNullOrWhiteSpace(c.GuestLeader))
            {
                sb.AppendLine($"- **Guest Leader**:  {c.GuestLeader}");
            }
            if (c.Leader != null)
            {
                sb.AppendLine("#### Leader 1");
                sb.AppendLine($"- **Name**: {c.Leader.FullName}");
                sb.AppendLine($"- **Email**: {c.Leader.Email}");
                sb.AppendLine($"- **Phone**: {c.Leader.AdjustedHomePhone}");
                sb.AppendLine($"- **Mobile**: {c.Leader.AdjustedMobile}");
            }
            if (c.Leader2 != null)
            {
                sb.AppendLine("#### Leader 2");
                sb.AppendLine($"- **Name**: {c.Leader2.FullName}");
                sb.AppendLine($"- **Email**: {c.Leader2.Email}");
                sb.AppendLine($"- **Phone**: {c.Leader2.AdjustedHomePhone}");
                sb.AppendLine($"- **Mobile**: {c.Leader2.AdjustedMobile}");
            }
            if (c.Leader3 != null)
            {
                sb.AppendLine("#### Leader 2");
                sb.AppendLine($"- **Name**: {c.Leader3.FullName}");
                sb.AppendLine($"- **Email**: {c.Leader3.Email}");
                sb.AppendLine($"- **Phone**: {c.Leader3.AdjustedHomePhone}");
                sb.AppendLine($"- **Mobile**: {c.Leader3.AdjustedMobile}");
            }
            sb.AppendLine($"### Clerks");
            if (c.Clerks.Count > 0)
            {
                var clerkCount = 0;
                foreach (var clerk in c.Clerks)
                {
                    clerkCount++;
                    sb.AppendLine($"#### Clerk {clerkCount}");
                    sb.AppendLine($"- **Name**: {clerk.FullName}");
                    sb.AppendLine($"- **Email**: {clerk.Email}");
                    sb.AppendLine($"- **Phone**: {clerk.AdjustedHomePhone}");
                    sb.AppendLine($"- **Mobile**: {clerk.AdjustedMobile}");
                }
            }
            else
            {
                sb.AppendLine("- No clerks assigned to this class.");
            }
        }

        static IHtmlDocument? ParseHtml(string html)
        {
            IHtmlDocument result = null!;
            var parser = new HtmlParser();
            try
            {
                result = parser.ParseDocument(html);
            }
            catch (Exception e) { }
            return result;
        }

        static string RemoveHtmlTags(string html)
        {
            var result = string.Empty;
            using (var document = ParseHtml(html))
            {
                result = document.Body.TextContent;
            }
            return result;
        }

        public static async Task<AIChatClassData> GetJsonAIChatClassDataAsync(U3ADbContext dbc,
                                                        TenantDbContext dbcT,
                                                        TenantInfoService tenantService)
        {
            var thisTenant = dbc.TenantInfo.Identifier;
            var people = await dbc.Person.IgnoreQueryFilters().ToListAsync();
            var otherU3APeople = await dbcT.MultiCampusPerson.Where(x => x.TenantIdentifier != thisTenant).ToListAsync();
            foreach (var p in otherU3APeople)
            {
                if (people.Find(x => x.ID == p.ID) == null)
                {
                    people.Add(GetPersonFromMCPerson(p));
                }
            }
            var data = new AIChatClassData();
            // settings
            var settings = await dbc.SystemSettings.OrderBy(x => x.ID).FirstOrDefaultAsync();

            // Terms
            data.Terms = await BusinessRule.SelectableTermsInCurrentYearAsync(dbc);

            // Classes
            var term = await BusinessRule.CurrentEnrolmentTermAsync(dbc);
            if (term == null)
            {
                var currentTerm = await BusinessRule.CurrentTermAsync(dbc);
            }
            if (term != null)
            {

                var prevTerm = await BusinessRule.GetPreviousTermAsync(dbc, term.Year, term.TermNumber) ?? term;

                // Fast lookup from Schedule cache
                var classes = await BusinessRule.RestoreClassesFromScheduleAsync(dbc, dbcT, tenantService, term, settings, true, true);
                // Get the schedule
                await getCalendarForAiClass(dbc, term, classes);
                // Transpose to ScheduledClasses
                data.Classes = classes.Select(x => new ScheduledClass()
                {
                    ID = x.ID,

                    // From Course

                    Name = x.Course.Name,
                    Description = RemoveHtmlTags(x.Course.Description),
                    Featured = x.Course.IsFeaturedCourse,
                    FeePerYear = x.Course.CourseFeePerYear,
                    FeePerYearDescription = x.Course.CourseFeePerYearDescription,
                    FeePerTerm = x.Course.CourseFeePerTerm,
                    FeePerTermDescription = x.Course.CourseFeePerTermDescription,
                    Duration = x.Course.Duration,
                    RequiredStudents = x.Course.RequiredStudents,
                    MaximumStudents = x.Course.MaximumStudents,
                    AllowAutoEnroll = x.Course.AllowAutoEnrol,
                    Category = x.Course.CourseType.Name,
                    ProvidedBy = (x.Course.OfferedBy == null)
                                ? dbc.TenantInfo.Name : x.Course.OfferedBy,

                    // From Class

                    OfferedTerm1 = x.OfferedTerm1,
                    OfferedTerm2 = x.OfferedTerm2,
                    OfferedTerm3 = x.OfferedTerm3,
                    OfferedTerm4 = x.OfferedTerm4,
                    StartDate = (x.StartDate != null)
                                    ? DateOnly.FromDateTime(x.StartDate.Value)
                                    : DateOnly.FromDateTime(x.ClassDates.OrderBy(x => x).FirstOrDefault()),
                    StartTime = TimeOnly.FromDateTime(x.StartTime),
                    EndTime = (x.EndTime != null) ? TimeOnly.FromDateTime(x.EndTime.Value)
                                                  : null,
                    Occurs = x.Occurrence.Name,
                    Repeats = x.Recurrence ?? term.Duration,
                    Day = x.OnDay.Day,
                    Venue = x.Venue.Name,
                    VenueAddress = x.Venue.Address,
                    ClassSummary = x.OccurrenceText,
                    TotalActiveStudents = x.TotalActiveStudents,
                    TotalWaitlisted = x.TotalWaitlistedStudents,
                    ParticipationRate = x.ParticipationRate

                }).ToList();

                classes.ForEach(delegate (Class c)
                {
                    var sc = data.Classes.Find(x => x.ID == c.ID);
                    if (sc != null)
                    {
                        if (!string.IsNullOrWhiteSpace(c.GuestLeader)) sc.People.Add(new ScheduledPerson()
                        {
                            Class = c.Course.Name,
                            SortOrder = c.GuestLeader,
                            Name = c.GuestLeader,
                            Roles = new List<string>() { "Guest Leader" }
                        });
                        if (c.Leader != null) sc.People.Add(new ScheduledPerson()
                        {
                            Class = c.Course.Name,
                            SortOrder = c.Leader.FullNameAlphaKey,
                            Name = c.Leader.FullName,
                            Email = c.Leader.Email,
                            Phone = c.Leader.AdjustedHomePhone,
                            Mobile = c.Leader.AdjustedMobile,
                            Roles = new List<string>() { "Leader" }
                        });
                        if (c.Leader2 != null) sc.People.Add(new ScheduledPerson()
                        {
                            Class = c.Course.Name,
                            SortOrder = c.Leader2.FullNameAlphaKey,
                            Name = c.Leader2.FullName,
                            Email = c.Leader2.Email,
                            Phone = c.Leader2.AdjustedHomePhone,
                            Mobile = c.Leader2.AdjustedMobile,
                            Roles = new List<string>() { "Leader" }
                        });
                        if (c.Leader3 != null) sc.People.Add(new ScheduledPerson()
                        {
                            Class = c.Course.Name,
                            SortOrder = c.Leader3.FullNameAlphaKey,
                            Name = c.Leader3.FullName,
                            Email = c.Leader3.Email,
                            Phone = c.Leader3.AdjustedHomePhone,
                            Mobile = c.Leader3.AdjustedMobile,
                            Roles = new List<string>() { "Leader" }

                        });
                        foreach (var clerk in c.Clerks)
                        {
                            sc.People.Add(new ScheduledPerson()
                            {
                                Class = c.Course.Name,
                                SortOrder = clerk.FullNameAlphaKey,
                                Name = clerk.FullName,
                                Email = clerk.Email,
                                Phone = clerk.AdjustedHomePhone,
                                Mobile = clerk.AdjustedMobile,
                                Roles = new List<string>() { "Clerk", "Student" }
                            });
                        }
                        if (c.Course.CourseParticipationTypeID == (int)ParticipationType.SameParticipantsInAllClasses)
                        {
                            foreach (var e in c.Course.Enrolments)
                                AssignParticipants(c, people, sc, e);
                        }
                        else
                        {
                            foreach (var e in c.Enrolments)
                                AssignParticipants(c, people, sc, e);
                        }
                        sc.People = sc.People.OrderBy(x => x.SortOrder).ToList();
                    }
                });
            }
            return data;
        }
        public static async Task<string> GetMarkdownAIChatDataAsync(U3ADbContext dbc,
                                                        TenantDbContext dbcT,
                                                        TenantInfoService tenantService)
        {
            StringBuilder sb = new();
            var thisTenant = dbc.TenantInfo.Identifier;
            var people = await dbc.Person.IgnoreQueryFilters().ToListAsync();
            var otherU3APeople = await dbcT.MultiCampusPerson.Where(x => x.TenantIdentifier != thisTenant).ToListAsync();
            foreach (var p in otherU3APeople)
            {
                if (people.Find(x => x.ID == p.ID) == null)
                {
                    people.Add(GetPersonFromMCPerson(p));
                }
            }
            var data = new AIChatClassData();
            // settings
            var settings = await dbc.SystemSettings.OrderBy(x => x.ID).FirstOrDefaultAsync();

            // Terms
            var terms = await BusinessRule.SelectableTermsInCurrentYearAsync(dbc);

            // Classes
            var term = await BusinessRule.CurrentEnrolmentTermAsync(dbc);
            if (term == null)
            {
                term = await BusinessRule.CurrentTermAsync(dbc);
            }
            if (term != null)
            {

                var prevTerm = await BusinessRule.GetPreviousTermAsync(dbc, term.Year, term.TermNumber) ?? term;

                // Fast lookup from Schedule cache
                var classes = await BusinessRule.RestoreClassesFromScheduleAsync(dbc, dbcT, tenantService, term, settings, true, true);
                // Get the schedule
                await getCalendarForAiClass(dbc, term, classes);
                // create the markdown
                sb.AppendLine($"# Class Schedule for {settings.U3AGroup}");

                sb.AppendLine("## Summary");
                sb.Append($"In {term.Year}, {settings.U3AGroup} ");
                sb.AppendLine($"offers a total of {classes.Count} classes categorised as follows...");
                foreach (var c in classes
                            .GroupBy(x => new { Name = x.Course.CourseType.Name })
                            .Select(g => new { Name = g.Key.Name, Total = g.Count() }))
                {
                    sb.AppendLine($"- {c.Name}: {c.Total} classes");
                }

                sb.AppendLine(string.Empty);
                sb.Append("Unless otherwise stated, classes are held during during term. ");
                sb.AppendLine($"For {term.Year} the term dates are... ");
                foreach (var t in terms)
                {
                    sb.AppendLine($"- {t.Name}: {t.StartDate.ToString("d")} to {t.EndDate.ToString("d")}");
                }
                sb.AppendLine("### Membership Fees");
                sb.AppendLine($"- A renewing member or a member that joins in Term 1: {settings.MembershipFee.ToString("c2")}");
                sb.AppendLine($"- A member that joins in Term 2: {settings.MembershipFeeTerm2.ToString("c2")}");
                sb.AppendLine($"- A member that joins in Term 3: {settings.MembershipFeeTerm3.ToString("c2")}");
                sb.AppendLine($"- A member that joins in Term 4: {settings.MembershipFeeTerm4.ToString("c2")}");
                if (settings.MailSurcharge != 0)
                {
                    sb.AppendLine($"- If mail is receive via post rather than email, a mail surcharge of {settings.MailSurcharge.ToString("c2")} applies.");
                }
                sb.AppendLine(string.Empty);
                sb.AppendLine("*Notes*");
                if (settings.AllowedMemberFeePaymentTypes == MemberFeePaymentType.PerYearOnly)
                {
                    sb.AppendLine($"1. {term.Year} member fees must be paid in full prior to enrolemnt in classes.");
                }
                else
                {
                    sb.AppendLine($"1. {term.Year} member fees may be paid in full or in two equal instalments due on or before Term 1 and Term 3.");
                }
                sb.AppendLine("1. Additional Course fees may apply depending on courses enrolled.");
                sb.AppendLine(string.Empty);
                sb.AppendLine("___");
                sb.AppendLine(string.Empty);

                foreach (var c in classes.OrderBy(x => x.Course.Name))
                {
                    // From Course
                    sb.AppendLine($"## Class: {c.Course.Name}");
                    sb.AppendLine($"### Description:{Environment.NewLine}{RemoveHtmlTags(c.Course.Description)}");
                    GetContactsAsMarkdown(sb, c);
                    sb.AppendLine($"### Details");
                    sb.AppendLine($"- **Category**:  {c.Course.CourseType.Name}");
                    sb.AppendLine($"- **Featured Course?**: {c.Course.IsFeaturedCourse}");
                    sb.AppendLine($"- **Fee Per Year**:  {c.Course.CourseFeePerYear.ToString("c2")}");
                    sb.AppendLine($"- **Fee Per Year Description**:  {c.Course.CourseFeePerYearDescription}");
                    sb.AppendLine($"- **Fee Per Term**:  {c.Course.CourseFeePerTerm.ToString("c2")}");
                    sb.AppendLine($"- **Fee Per Term Description**:  {c.Course.CourseFeePerTermDescription}");
                    sb.AppendLine($"- **Duration**:  {c.Course.Duration.ToString("n2")}");
                    sb.AppendLine($"- **Required Students**:  {c.Course.RequiredStudents}");
                    sb.AppendLine($"- **Maximum Students**:  {c.Course.MaximumStudents}");
                    sb.AppendLine($"- **Allow Automatic Enrolment**:  {c.Course.AllowAutoEnrol}");
                    sb.AppendLine($"- **Provided By**:  {((c.Course.OfferedBy == null) ? settings.U3AGroup : c.Course.OfferedBy)}");

                    // From Class

                    sb.AppendLine($"- **Offered Term 1**:  {c.OfferedTerm1}");
                    sb.AppendLine($"- **Offered Term 2**:  {c.OfferedTerm2}");
                    sb.AppendLine($"- **Offered Term 3**:  {c.OfferedTerm3}");
                    sb.AppendLine($"- **Offered Term 4**:  {c.OfferedTerm4}");
                    sb.Append($"- **Class Dates**: ");
                    sb.AppendLine(string.Join(", ", c.ClassDates));
                    sb.AppendLine($"- **Start Time**:  {TimeOnly.FromDateTime(c.StartTime).ToString()}");
                    sb.AppendLine($"- **End Time**:  {((c.EndTime != null) ? TimeOnly.FromDateTime(c.EndTime.Value).ToString() : string.Empty)}");
                    sb.AppendLine($"- **Occurs**:  {c.Occurrence.Name}");
                    sb.AppendLine($"- **Repeats**:  {c.Recurrence ?? term.Duration}");
                    sb.AppendLine($"- **On Day**:  {c.OnDay.Day}");
                    sb.AppendLine($"- **Venue**:  {c.Venue.Name}, {c.Venue.Address}");
                    sb.AppendLine($"- **Occurs**:  {c.OccurrenceText}");
                    sb.AppendLine($"- **Total Active Students**:  {c.TotalActiveStudents}");
                    sb.AppendLine($"- **Total Waitlisted Students**:  {c.TotalWaitlistedStudents}");
                    sb.AppendLine($"- **Participation Rate**:  {c.ParticipationRate.ToString("p2")}");
                    sb.Append("- **Status**: ");
                    if (c.ParticipationRate >= 1)
                    {
                        sb.AppendLine("Class is Full. New enrolment requests will be Waitlisted.");
                    }
                    else if (!c.Course.AllowAutoEnrol)
                    {
                        sb.AppendLine("Class is Closed. New enrolment requests will be Waitlisted.");
                    }
                    else
                    {
                        sb.AppendLine("Class is Open. New enrolment requests will be accepted.");
                    }
                    sb.AppendLine(string.Empty);
                    sb.AppendLine("___");
                    sb.AppendLine(string.Empty);

                    //        if (c.Course.CourseParticipationTypeID == (int)ParticipationType.SameParticipantsInAllClasses)
                    //        {
                    //            foreach (var e in c.Course.Enrolments)
                    //                AssignParticipants(c, people, sc, e);
                    //        }
                    //        else
                    //        {
                    //            foreach (var e in c.Enrolments)
                    //                AssignParticipants(c, people, sc, e);
                    //        }
                    //        sc.People = sc.People.OrderBy(x => x.SortOrder).ToList();
                    //    }
                    //});
                }
            }
            return sb.ToString();
        }


    }
}
