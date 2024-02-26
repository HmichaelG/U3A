using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using U3A.BusinessRules;
using U3A.Database;
using U3A.Model;
using U3A.UI.Reports;

namespace U3A.WebFunctions.Procedures
{
    public static class SendLeaderReports
    {

        static ProFormaReportFactory reportFactory;
        public static async Task Process(TenantInfo tenant, ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(tenant.PostmarkAPIKey) && !tenant.UsePostmarkTestEnviroment) return;
            if (string.IsNullOrWhiteSpace(tenant.PostmarkSandboxAPIKey) && tenant.UsePostmarkTestEnviroment) return;

            using (var dbc = new U3ADbContext(tenant))
            {
                var today = await Common.GetTodayAsync(dbc);
                var selectedTerm = await BusinessRule.CurrentTermAsync(dbc);
                if (selectedTerm == null) { return; }
                var settings = dbc.SystemSettings.OrderBy(x => x.ID).FirstOrDefault();
                if (settings != null)
                {
                    reportFactory = new ProFormaReportFactory(tenant);
                    var Classes = await BusinessRule.SelectableClassesAsync(dbc, selectedTerm);
                    var count = 0;
                    foreach (var c in Classes)
                    {
                        var processDate = ((c.StartDate == null) ? selectedTerm.StartDate : c.StartDate).Value.AddDays(-2);
                        if (processDate != today) { continue; }
                        foreach (var p in await BusinessRule.GetLeaderReportRecipients(dbc, settings, selectedTerm, c))
                        {
                            await ProcessReportAsync(dbc, selectedTerm, p, c.Course, c);
                            logger.LogInformation($"Sent: {p.FullName} for {c.Course.Name}.");
                            count++;
                        }
                    }
                    if (count > 0)
                    {
                        logger.LogInformation($">>> {count} Leader's report packs sent at term start <<<<");
                    }
                }
            }
        }

        static async Task ProcessReportAsync(U3ADbContext dbc, Term selectedTerm, Person Leader, Course Course, Class Class)
        {
            var enrolments = await BusinessRule.GetEnrolmentIncludeLeadersAsync(dbc,Course,Class,selectedTerm);
            if (enrolments.Count > 0)
            {
                var PrintLeaderReport = true;
                var PrintAttendanceRecord = true;
                var PrintClassList = true;
                var PrintICEList = true;
                var PrintCSVFile = true;
                var PrintAttendanceAnalysis = false;
                await reportFactory.CreateLeaderReports(
                    PrintLeaderReport,
                    PrintAttendanceRecord,
                    PrintClassList,
                    PrintICEList,
                    PrintCSVFile,
                    PrintAttendanceAnalysis,
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
