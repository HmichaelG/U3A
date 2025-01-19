using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Text;
using U3A.BusinessRules;
using U3A.Database;
using U3A.Model;
using U3A.Services;
using U3A.Services.Email;

namespace U3A.WebFunctions.Procedures
{
    public static class ProcessMembershipAlertsEmail
    {

        public static async Task Process(TenantInfo tenant, ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(tenant.PostmarkAPIKey) && !tenant.UsePostmarkTestEnviroment) return;
            if (string.IsNullOrWhiteSpace(tenant.PostmarkSandboxAPIKey) && tenant.UsePostmarkTestEnviroment) return;

            using (var dbc = new U3ADbContext(tenant))
            {
                dbc.UtcOffset = await Common.GetUtcOffsetAsync(dbc);
                var msg = await CreateEmailMessage(dbc);
                if (!string.IsNullOrWhiteSpace(msg))
                {
                    var emailSender = EmailFactory.GetEmailSender(dbc);
                    string sendEmailAddress = "";
                    string sendEmailDisplayName = "";
                    var settings = await dbc.SystemSettings.OrderBy(x => x.ID).FirstOrDefaultAsync() ?? throw new ArgumentNullException(nameof(SystemSettings));
                    if (settings != null)
                    {
                        sendEmailAddress = settings.SendEmailAddesss;
                        sendEmailDisplayName = settings.SendEmailDisplayName;
                    }
                    // send to membership officer
                    await ProcessEmail(sendEmailAddress,
                                        sendEmailDisplayName,
                                        msg,
                                        emailSender!);
                    // send to System Postman CC addresses
                    var validator = new EmailAddressAttribute();
                    foreach (var address in settings!.SystemPostmanCCAddresses.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        sendEmailAddress = address.Trim();
                        if (validator.IsValid(sendEmailAddress))
                        {
                            await ProcessEmail(sendEmailAddress,
                                                string.Empty,
                                                msg,
                                                emailSender!);
                        }
                    }
                }
            }

        }

        private static async Task ProcessEmail(string sendEmailAddress,
                                        string sendEmailDisplayName,
                                        String msg,
                                        IEmailService emailSender)
        {
            if (string.IsNullOrWhiteSpace(sendEmailAddress)) { return; }
            _ = await emailSender.SendEmailAsync(
                            EmailType.Transactional,
                            "system@u3admin.org.au",
                            "System Postman",
                            sendEmailAddress,
                            sendEmailDisplayName,
                            $"U3A Membership update",
                            msg,
                            string.Empty
                            );

        }
        private static async Task<string> CreateEmailMessage(U3ADbContext dbc)
        {
            var msg = "";
            var now = await Common.GetNowAsync(dbc);
            var today = now.Date;
            var settings = await dbc.SystemSettings.OrderBy(x => x.ID).FirstOrDefaultAsync() ?? throw new ArgumentNullException(nameof(SystemSettings));

            //Random Allocation Day

            var currentTerm = await BusinessRule.CurrentEnrolmentTermAsync(dbc);
            bool DoEnrolledButUnfinancial = false;
            if (currentTerm != null)
            {
                if (BusinessRule.IsRandomAllocationTerm(currentTerm, settings))
                {
                    DateTime allocationDate = BusinessRule.GetThisTermAllocationDay(currentTerm, settings);
                    int days = (int)(allocationDate - today).TotalDays;
                    if (days > 0 && days < 8)
                    {
                        msg += $"<H3>Reminder: Random Enrolment Allocation Day in {days} days.<h3>";
                        DoEnrolledButUnfinancial |= true;
                    }
                    if (days == 0) msg += $"<H3>Random Enrolment has been performed! You have {constants.RANDOM_ALLOCATION_PREVIEW} days to review before members are advised.<h3>";
                    if (days < 0 && Math.Abs(days) < constants.RANDOM_ALLOCATION_PREVIEW)
                    {
                        msg += $"<H3>Reminder: Random Enrolment has been performed! Member notification emails in {constants.RANDOM_ALLOCATION_PREVIEW + days} days.<h3>";
                    }
                }
            }

            // Email suppressions

            var service = new PostmarkService(dbc.TenantInfo);
            var tzOffSet = settings.UTCOffset;
            var suppressions = await service.GetSuppressions(dbc, tzOffSet, (DateTime.UtcNow + tzOffSet).AddDays(-7));
            if (suppressions.Count > 0)
            {
                var introduction = $@"<p>The following email suppressions have been received in the last 7 days. Please investigate carefully.</p>";
                msg += FormatSuppressions(suppressions, introduction);
            }

            // public holidays
            var DoW = new List<DayOfWeek>() {
                            DayOfWeek.Wednesday,
                            DayOfWeek.Thursday,
                            DayOfWeek.Friday };
            if (DoW.Contains(today.DayOfWeek))
            {
                var publicHolidays = await BusinessRule.PublicHolidaysThisWeekAsync(dbc, today);
                if (publicHolidays?.Any() == true)
                {
                    var introduction = $@"<p>The following public holidays will occur in the next week.
                                            <br/>If classes are to be held on these day(s) please delete from the Public Holidays list.</p>";
                    msg += FormatPublicHolidays(publicHolidays, introduction);
                }
            }

            // new members still unfinancial

            var people = await dbc.Person
                                .Where(x => x.DateCeased == null
                                        && x.FinancialTo <= 2020
                                        && x.DateJoined < today.AddDays(-3))
                                .ToListAsync();
            if (people?.Any() == true)
            {
                var introduction = "<p>The following members have joined the U3A but are still unfinancial. Do you need to import Direct Payment details?</p>";
                msg += FormatPeople(people, introduction);
            }

            // new persons joined, financial but not enrolled

            var term = await BusinessRule.CurrentTermAsync(dbc);
            if (term != null)
            {
                people = await BusinessRule.SelectablePersonsNotEnrolledAsync(dbc,
                                term,
                                today.AddDays(-7),
                                today.AddDays(-14));
                if (people?.Any() == true)
                {
                    var introduction = $@"<p>The following members have joined the U3A in the last 14 days and are financial.
                                            <br/>However, they have yet to enrol in a course. Do they need assistance?</p>";
                    msg += FormatPeople(people, introduction);
                }
                people = await BusinessRule.SelectableNewPersonsWaitlistedOnlyAsync(dbc,
                                term,
                                today,
                                today.AddDays(-1));
                if (people?.Any() == true)
                {
                    var introduction = $@"<p>The following members have joined the U3A in the last day and are financial.
                                            <br/>However, all their course enrolment requests have been waitlisted. Do they understand what this means?</p>";
                    msg += FormatPeople(people, introduction);
                }
                if (DoEnrolledButUnfinancial)
                {
                    // Enrolled but unfinancial

                    people = BusinessRule.SelectablePersonsWithEnrolments(dbc, term.ID, null)
                                .Where(x => x.FinancialTo < term.Year).ToList();
                    if (people?.Any() == true)
                    {
                        var introduction = $@"<p>The following members have requested enrolment in class(es) but are not financial.
                                            <br/>Their course enrolment requests will remain waitlisted until payment is made.
                                            <br>For detail on classes requested review the report, <strong>Course By Participant List</strong>
                                                and filter by <strong>financial status</strong>.</p>";
                        msg += FormatPeopleAndClasses(people, introduction);
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(msg))
            {
                msg = $@"<p>Good day!</p> {msg}
                        <p><p>Thank you<br/>
                        Please do not reply. This email address is not monitored.";

            }

            return msg;
        }

        private static string FormatSuppressions(IEnumerable<EmailSuppression> suppressions, string introduction)
        {
            var txt = new StringBuilder();
            _ = txt.AppendLine(introduction);
            _ = txt.AppendLine(@"<table style='width: 100%;
                                        border: 1pt solid black;'>");
            _ = txt.AppendLine(@"<tr>
                                    <th style='text-align: left;
                                            padding-left: 10pt;
                                            border-bottom: 1pt solid black;'>
                                            Date</th>
                                    <th style='text-align: left;border-bottom: 1pt solid black;'>Stream</th>
                                    <th style='text-align: left;border-bottom: 1pt solid black;'>Reason</th>
                                    <th style='text-align: left;border-bottom: 1pt solid black;'>Email</th>
                                    <th style='text-align: left;border-bottom: 1pt solid black;'>Name</th>
                                </tr>");
            foreach (var sup in suppressions.OrderBy(x => x.CreatedAt))
            {
                _ = txt.AppendLine(@$"<tr>
                                    <td style='text-align: left;
                                            padding-left: 10pt;'>
                                            {sup.CreatedAt.ToString("dd-MM-yy")}</td>
                                    <td>{sup.Stream}</td>
                                    <td>{sup.Reason}</td>
                                    <td>{sup.Email}</td>
                                    <td>{sup.Person?.FullName}</td>
                                    </tr>");
            }
            _ = txt.AppendLine("</table>");
            return txt.ToString();
        }

        private static string FormatCourse(IEnumerable<Course> courses, string introduction, DateTime now)
        {
            var txt = new StringBuilder();
            _ = txt.AppendLine(introduction);
            _ = txt.AppendLine(@"<table style='width: 100%;
                                        border: 1pt solid black;'>");
            _ = txt.AppendLine(@"<tr>
                                    <th style='text-align: left;
                                            padding-left: 10pt;
                                            border-bottom: 1pt solid black;'>
                                            Course / Class Details</th>
                                    <th style='text-align: left;border-bottom: 1pt solid black;'>Status</th>
                                    <th style='text-align: left;border-bottom: 1pt solid black;'>Last Change By</th>
                                </tr>");
            foreach (var course in courses.OrderBy(x => x.Name))
            {
                var start = now.AddHours(-24);
                var end = now;
                var user = string.Empty;
                var status = string.Empty;
                if (course.CreatedOn >= start && course.CreatedOn <= end)
                {
                    status = "Created";
                }
                if (course.UpdatedOn >= start && course.UpdatedOn <= end)
                {
                    status = "Modified";
                }
                if (course.User != null) { user = course.User; }
                _ = txt.AppendLine(@$"<tr>
                                    <td style='text-align: left;
                                            padding-left: 10pt;'>
                                            <strong>{course.Name}</strong></td>
                                    <td>{status}</td>
                                    <td>{user}</td>
                                    </tr>");
                foreach (var c in course.Classes)
                {
                    status = string.Empty;
                    if (c.CreatedOn >= start && c.CreatedOn <= end)
                    {
                        status = "Created";
                    }
                    if (c.UpdatedOn >= start && c.UpdatedOn <= end)
                    {
                        status = "Modified";
                    }
                    if (c.User != null) { user = c.User; }
                    _ = txt.AppendLine(@$"<tr>
                                    <td style='text-align: left;
                                            padding-left: 10pt;'>&emsp;
                                            Offered: {c.OfferedSummary}</td>
                                    <td>{status}</td>
                                    <td>{user}</td>
                                    </tr>");
                    _ = txt.AppendLine(@$"<tr>
                                    <td colspan='3' style='text-align: left;
                                            padding-left: 10pt;'>&emsp;
                                            {c.ClassDetailWithoutVenue}</td>
                                    </tr>");
                    _ = txt.AppendLine(@$"<tr>
                                    <td colspan='3' style='text-align: left;
                                            padding-left: 10pt;'>&emsp;
                                            {c.Venue.Name}</td>
                                    </tr>");
                    _ = txt.AppendLine(@$"<tr>
                                    <td colspan='3' style='text-align: left;
                                            padding-left: 10pt;'>&emsp;
                                            Leader: {c.Leader?.FullName}</td>
                                    </tr>");
                }
                _ = txt.AppendLine(@$"<tr>
                                    <td colspan='3' >
                                    </td>
                                    </tr>");
            }
            _ = txt.AppendLine("</table>");
            _ = txt.AppendLine("<div style='font-size: 8.75pt;' ><strong>NB: The above detail is a summary only. Consult the course/class record for full details.</strong></div>");
            return txt.ToString();
        }
        private static string FormatPeople(IEnumerable<Person> people, string introduction)
        {
            var txt = new StringBuilder();
            _ = txt.AppendLine(introduction);
            _ = txt.AppendLine(@"<table style='width: 100%;
                                        border: 1pt solid black;'>");
            _ = txt.AppendLine(@"<tr>
                                    <th style='text-align: left;
                                            padding-left: 10pt;
                                            border-bottom: 1pt solid black;'>
                                            Identity</th>
                                    <th style='text-align: left;border-bottom: 1pt solid black;'>Member</th>
                                    <th style='text-align: left;border-bottom: 1pt solid black;'>Date Joined</th>
                                    <th style='text-align: left;border-bottom: 1pt solid black;'>Mobile</th>
                                    <th style='text-align: left;border-bottom: 1pt solid black;'>Email</th>
                                </tr>");
            foreach (var person in people.OrderBy(x => x.FullName))
            {
                _ = txt.AppendLine(@$"<tr>
                                    <td style='text-align: left;
                                            padding-left: 10pt;'>
                                            {person.PersonIdentity}</td>
                                    <td>{person.FullName}</td>
                                    <td>{person.DateJoined?.ToString("dd-MMM-yyyy")}</td>
                                    <td>{person.Mobile}</td>
                                    <td>{person.Email}</td>
                                    </tr>");
            }
            _ = txt.AppendLine("</table>");
            return txt.ToString();
        }
        private static string FormatPeopleAndClasses(IEnumerable<Person> people, string introduction)
        {
            var txt = new StringBuilder();
            _ = txt.AppendLine(introduction);
            _ = txt.AppendLine(@"<table style='width: 100%;
                                        border: 1pt solid black;'>");
            _ = txt.AppendLine(@"<tr>
                                    <th style='text-align: left;
                                            padding-left: 10pt;
                                            border-bottom: 1pt solid black;'>
                                            Identity</th>
                                    <th style='text-align: left;border-bottom: 1pt solid black;'>Member</th>
                                    <th style='text-align: left;border-bottom: 1pt solid black;'>Classes</th>
                                    <th style='text-align: left;border-bottom: 1pt solid black;'>Mobile</th>
                                    <th style='text-align: left;border-bottom: 1pt solid black;'>Email</th>
                                </tr>");
            foreach (var person in people.OrderBy(x => x.FullName))
            {
                _ = txt.AppendLine(@$"<tr>
                                    <td style='text-align: left;
                                            padding-left: 10pt;'>
                                            {person.PersonIdentity}</td>
                                    <td>{person.FullName}</td>
                                    <td>{person.EnrolledClasses.Count.ToString()}</td>
                                    <td>{person.Mobile}</td>
                                    <td>{person.Email}</td>
                                    </tr>");
            }
            _ = txt.AppendLine(" </table>");
            return txt.ToString();
        }
        private static string FormatPublicHolidays(IEnumerable<PublicHoliday> holidays, string introduction)
        {
            var txt = new StringBuilder();
            _ = txt.AppendLine(introduction);
            _ = txt.AppendLine(@"<table style='width: 100%;
                                        border: 1pt solid black;'>");
            _ = txt.AppendLine(@"<tr>
                                    <th style='text-align: left;
                                            width: 30%;
                                            padding-left: 10pt;
                                            border-bottom: 1pt solid black;'>
                                            Date</th>
                                    <th style='text-align: left;border-bottom: 1pt solid black;'>Holiday</th>
                                </tr>");
            foreach (var h in holidays.OrderBy(x => x.Date))
            {
                _ = txt.AppendLine(@$"<tr>
                                    <td style='text-align: left;
                                            padding-left: 10pt;'>
                                            {h.Date.ToString("dddd, dd-MMMM-yyyy")}</td>
                                    <td>{h.Name}</td>
                                    </tr>");
            }
            _ = txt.AppendLine("</table>");
            return txt.ToString();
        }
    }
}