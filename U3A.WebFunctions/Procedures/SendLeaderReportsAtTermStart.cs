using Microsoft.EntityFrameworkCore;
using Serilog;
using U3A.BusinessRules;
using U3A.Database;
using U3A.Model;
using U3A.UI.Reports;

namespace U3A.WebFunctions.Procedures
{
    public static class SendLeaderReportsAtTermStart
    {

        private static ProFormaReportFactory? reportFactory;
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
            DateTime today = await Common.GetTodayAsync(dbc);
            Term? selectedTerm = await BusinessRule.CurrentTermAsync(dbc);
            if (selectedTerm == null) { return; }
            SystemSettings? settings = await dbc.SystemSettings.OrderBy(x => x.ID).FirstOrDefaultAsync();
            if (settings != null)
            {
                reportFactory = new ProFormaReportFactory(tenant);
                List<Class> Classes = await BusinessRule.SelectableClassesAsync(dbc, selectedTerm);
                int count = 0;
                foreach (Class c in Classes)
                {
                    DateTime processDate = ((c.StartDate == null) ? selectedTerm.StartDate : c.StartDate).Value.AddDays(-2);
                    if (processDate != today) { continue; }
                    foreach (Person p in await BusinessRule.GetLeaderReportRecipients(dbc, settings, selectedTerm, c))
                    {
                        await ProcessReportAsync(dbc, selectedTerm, p, c.Course, c);
                        Log.Information($"Sent: {p.FullName} for {c.Course.Name}.");
                        count++;
                    }
                }
                if (count > 0)
                {
                    Log.Information($">>> {count} Leader's report packs sent at term start <<<<");
                }
            }
        }

        private static async Task ProcessReportAsync(U3ADbContext dbc, Term selectedTerm, Person Leader, Course Course, Class Class)
        {
            List<Enrolment> enrolments = await BusinessRule.GetEnrolmentIncludeLeadersAsync(dbc, Course, Class, selectedTerm);
            if (enrolments.Count > 0)
            {
                bool PrintLeaderReport = true;
                bool PrintAttendanceRecord = true;
                bool PrintClassList = true;
                bool PrintICEList = true;
                bool PrintCSVFile = true;
                bool PrintMemberBadges = true;
                bool PrintAttendanceAnalysis = false;
                _ = await reportFactory!.CreateLeaderReports(
                    PrintLeaderReport,
                    PrintAttendanceRecord,
                    PrintClassList,
                    PrintICEList,
                    PrintCSVFile,
                    PrintAttendanceAnalysis,
                    PrintMemberBadges,
                    Course.ID,
                    $"U3A {selectedTerm.Year} Term {selectedTerm.TermNumber} Report Package",
                    Course.Name,
                    Leader, enrolments.OrderBy(x => x.IsWaitlisted)
                                                .ThenBy(x => x.Person.LastName)
                                                .ThenBy(x => x.Person.FirstName).ToArray());
            }
        }

    }
}
