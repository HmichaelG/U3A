using Microsoft.EntityFrameworkCore;
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

        public static async Task Process(TenantInfo tenant)
        {
            if (string.IsNullOrWhiteSpace(tenant.PostmarkAPIKey) && !tenant.UsePostmarkTestEnviroment)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(tenant.PostmarkSandboxAPIKey) && tenant.UsePostmarkTestEnviroment)
            {
                return;
            }

            using U3ADbContext dbc = new(tenant);
            dbc.UtcOffset = await Common.GetUtcOffsetAsync(dbc);
            string msg = await CreateEmailMessage(dbc);
            if (!string.IsNullOrWhiteSpace(msg))
            {
                IEmailService? emailSender = await EmailFactory.GetEmailSenderAsync(dbc);
                string sendEmailAddress = "";
                string sendEmailDisplayName = "";
                SystemSettings settings = await dbc.SystemSettings.OrderBy(x => x.ID).FirstOrDefaultAsync() ?? throw new ArgumentNullException(nameof(SystemSettings));
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
                EmailAddressAttribute validator = new();
                foreach (string address in settings!.SystemPostmanCCAddresses.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries))
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

        private static async Task ProcessEmail(string sendEmailAddress,
                                        string sendEmailDisplayName,
                                        string msg,
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
            string msg = "";
            DateTime now = await Common.GetNowAsync(dbc);
            DateTime today = now.Date;
            SystemSettings settings = await dbc.SystemSettings.OrderBy(x => x.ID).FirstOrDefaultAsync() ?? throw new ArgumentNullException(nameof(SystemSettings));

            //Random Allocation Day

            Term? currentTerm = await BusinessRule.CurrentEnrolmentTermAsync(dbc);
            bool DoEnrolledButUnfinancial = false;
            if (currentTerm != null)
            {
                if (BusinessRule.IsRandomAllocationTerm(currentTerm, settings))
                {
                    DateTime allocationDate = BusinessRule.GetThisTermAllocationDay(currentTerm, settings);
                    int days = (int)(allocationDate - today).TotalDays;
                    if (days is > 0 and < 8)
                    {
                        msg += $"<H3>Reminder: Random Enrollment Allocation Day in {days} days.<h3>";
                        DoEnrolledButUnfinancial |= true;
                    }
                    if (days == 0)
                    {
                        msg += $"<H3>Random Enrollment has been performed! You have {constants.RANDOM_ALLOCATION_PREVIEW} days to review before members are advised.<h3>";
                    }

                    if (days < 0 && Math.Abs(days) < constants.RANDOM_ALLOCATION_PREVIEW)
                    {
                        msg += $"<H3>Reminder: Random Enrollment has been performed! Member notification emails in {constants.RANDOM_ALLOCATION_PREVIEW + days} days.<h3>";
                    }
                }
            }

            // Email suppressions

            PostmarkService service = new(dbc.TenantInfo);
            TimeSpan tzOffSet = settings.UTCOffset;
            List<EmailSuppression> suppressions = await service.GetSuppressions(dbc, tzOffSet, (DateTime.UtcNow + tzOffSet).AddDays(-7));
            if (suppressions.Count > 0)
            {
                string introduction = $@"<p>The following email suppressions have been received in the last 7 days. Please investigate carefully.</p>";
                msg += FormatSuppressions(suppressions, introduction);
                msg += "<p></p>";
            }

            // public holidays
            List<DayOfWeek> DoW = [
                            DayOfWeek.Wednesday,
                            DayOfWeek.Thursday,
                            DayOfWeek.Friday ];
            if (DoW.Contains(today.DayOfWeek))
            {
                List<PublicHoliday> publicHolidays = await BusinessRule.PublicHolidaysThisWeekAsync(dbc, today);
                if (publicHolidays?.Any() == true)
                {
                    string introduction = $@"<p>The following public holidays will occur in the next week.
                                            <br/>If classes are to be held on these day(s) please delete from the Public Holidays list.</p>";
                    msg += FormatPublicHolidays(publicHolidays, introduction);
                    msg += "<p></p>";
                }
            }

            // new members still not financial

            List<Person> people = await dbc.Person
                                .Where(x => x.DateCeased == null
                                        && x.FinancialTo <= 2020
                                        && x.DateJoined < today.AddDays(-3))
                                .ToListAsync();
            if (people?.Any() == true)
            {
                string introduction = "<p>The following members have joined the U3A but are still unfinancial. Do you need to import Direct Payment details?</p>";
                msg += FormatPeople(people, introduction);
                msg += "<p></p>";
            }

            // new persons joined, financial but not enrolled

            Term? term = await BusinessRule.CurrentTermAsync(dbc);
            if (term != null)
            {
                people = await BusinessRule.SelectablePersonsNotEnrolledAsync(dbc,
                                term,
                                today.AddDays(-7),
                                today.AddDays(-14));
                if (people?.Any() == true)
                {
                    string introduction = $@"<p>The following members have joined the U3A in the last 14 days and are financial.
                                            <br/>However, they have yet to enroll in a course. Do they need assistance?</p>";
                    msg += FormatPeople(people, introduction);
                    msg += "<p></p>";
                }
                people = await BusinessRule.SelectableNewPersonsWaitlistedOnlyAsync(dbc,
                                term,
                                today,
                                today.AddDays(-1));
                if (people?.Any() == true)
                {
                    string introduction = $@"<p>The following members have joined the U3A in the last day and are financial.
                                            <br/>However, all their course enrollment requests have been waitlisted. Do they understand what this means?</p>";
                    msg += FormatPeople(people, introduction);
                    msg += "<p></p>";
                }
                if (DoEnrolledButUnfinancial)
                {
                    // Enrolled but not financial

                    people = BusinessRule.SelectablePersonsWithEnrolments(dbc, term.ID, null)
                                .Where(x => x.FinancialTo < term.Year).ToList();
                    if (people?.Any() == true)
                    {
                        string introduction = $@"<p>The following members have requested enrollment in class(es) but are not financial.
                                            <br/>Their course enrollment requests will remain waitlisted until payment is made.
                                            <br>For detail on classes requested review the report, <strong>Course By Participant List</strong>
                                                and filter by <strong>financial status</strong>.</p>";
                        msg += FormatPeopleAndClasses(people, introduction);
                        msg += "<p></p>";
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(msg))
            {
                msg = $@"<p>Good day!</p> {msg}
                        <p>Thank you<br/>
                        Please do not reply. This email address is not monitored.";

            }

            return msg;
        }

        private static string FormatSuppressions(IEnumerable<EmailSuppression> suppressions, string introduction)
        {
            StringBuilder txt = new();
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
            foreach (EmailSuppression? sup in suppressions.OrderBy(x => x.CreatedAt))
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
            StringBuilder txt = new();
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
            foreach (Course? course in courses.OrderBy(x => x.Name))
            {
                DateTime start = now.AddHours(-24);
                DateTime end = now;
                string user = string.Empty;
                string status = string.Empty;
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
                foreach (Class c in course.Classes)
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
            StringBuilder txt = new();
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
            foreach (Person? person in people.OrderBy(x => x.FullName))
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
            StringBuilder txt = new();
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
            foreach (Person? person in people.OrderBy(x => x.FullName))
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
            StringBuilder txt = new();
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
            foreach (PublicHoliday? h in holidays.OrderBy(x => x.Date))
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