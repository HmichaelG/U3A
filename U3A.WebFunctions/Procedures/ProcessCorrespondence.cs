using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using U3A.BusinessRules;
using U3A.Database;
using U3A.Model;
using U3A.UI.Reports;

namespace U3A.WebFunctions.Procedures
{
    public static class ProcessCorrespondence
    {
        public static async Task Process(TenantInfo tenant, 
            string tenantConnectionString,
            ILogger logger, bool IsHourlyProcedure)
        {
            if (string.IsNullOrWhiteSpace(tenant.PostmarkAPIKey) && !tenant.UsePostmarkTestEnviroment) return;
            if (string.IsNullOrWhiteSpace(tenant.PostmarkSandboxAPIKey) && tenant.UsePostmarkTestEnviroment) return;
            var reportFactory = new ProFormaReportFactory(tenant);
            var personEnrolments = new Dictionary<Guid, List<Enrolment>>();
            IList<SendMail> mailItems;
            using (var dbc = new U3ADbContext(tenant))
            {
                using (var dbcT = new TenantDbContext(tenantConnectionString))
                {
                    List<(Guid, Guid, Guid?)> onFile = new();
                    (Guid, Guid, Guid?) onFileKey;
                    var today = await Common.GetTodayAsync(dbc);
                    var utcTime = DateTime.UtcNow;
                    mailItems = await dbc.SendMail
                                            .Include(x => x.Person)
                                            .Where(x => string.IsNullOrWhiteSpace(x.Status)
                                                    && utcTime >= x.CreatedOn).ToListAsync();
                    foreach (var sm in await BusinessRule.GetMultiCampusMailAsync(dbcT, tenant.Identifier!)) 
                    {
                        if (string.IsNullOrWhiteSpace(sm.Status)) { mailItems.Add(sm); }
                    }
                    var mcEnrolments = await BusinessRule.GetMultiCampusEnrolmentsAsync(dbc, dbcT, tenant.Identifier!);
                    foreach (SendMail sm in mailItems)
                    {
                        var p = sm.Person;
                        switch (sm.DocumentName)
                        {
                            case "Cash Receipt":
                                if (IsHourlyProcedure) { break; }
                                var receipt = await dbc.Receipt
                                                    .Include(x => x.Person)
                                                    .Where(x => x.ID == sm.RecordKey).FirstOrDefaultAsync();
                                if (receipt != null)
                                {
                                    sm.Status = await reportFactory.CreateCashReceiptProForma(receipt);
                                    logger.LogInformation($"{sm.DocumentName} sent to: {p.FullName} via {p.Communication}.");
                                }
                                else sm.Status = "Receipt not found";
                                break;
                            case "Participant Enrolment":
                                var enrolment = await dbc.Enrolment
                                                    .Include(x => x.Course)
                                                    .Include(x => x.Person)
                                                    .Where(x => x.ID == sm.RecordKey).FirstOrDefaultAsync();
                                if (enrolment == null)
                                {
                                    enrolment = await BusinessRule.GetMultiCampusEnrolmentAsync(dbc, dbcT, sm.RecordKey, tenant.Identifier!);
                                }
                                if (enrolment != null)
                                {
                                    var key = enrolment.PersonID;
                                    var theseEnrolments = new List<Enrolment>();
                                    if (!personEnrolments.TryGetValue(key, out theseEnrolments))
                                    {
                                        theseEnrolments = new List<Enrolment>();
                                        personEnrolments.Add(key, theseEnrolments);
                                    }
                                    theseEnrolments.Add(enrolment);
                                    logger.LogInformation($"{sm.DocumentName} for {p.FullName} added to queue.");
                                }
                                else sm.Status = "Enrolment not found";
                                break;
                            case "Leader Report":
                                if (IsHourlyProcedure) { break; }
                                string courseName = "";
                                Course? course = null;
                                var leader = await dbc.Person
                                                    .Where(x => x.ID == sm.PersonID).FirstOrDefaultAsync();
                                var enrolments = new List<Enrolment>();
                                var todayID = (int)today.DayOfWeek;
                                int classOnDayID = -1;
                                if (dbc.Class.Any(x => x.ID == sm.RecordKey))
                                {
                                    enrolments = dbc.Enrolment.Where(x => x.ClassID == sm.RecordKey
                                                                        && x.TermID == sm.TermID).ToList();
                                    enrolments.AddRange(mcEnrolments.Where(x => x.ClassID == sm.RecordKey && x.TermID == sm.TermID));
                                    var Class = dbc.Class.Find(sm.RecordKey);
                                    course = dbc.Course.Find(Class!.CourseID);
                                    classOnDayID = Class.OnDayID;
                                }
                                else
                                {
                                    course = dbc.Course.Find(sm.RecordKey);
                                    if (course != null)
                                    {
                                        enrolments = dbc.Enrolment.Where(x => x.CourseID == course.ID
                                                                              && x.TermID == sm.TermID).ToList();
                                        enrolments.AddRange(mcEnrolments.Where(x => x.CourseID == course.ID && x.ClassID == null
                                                                              && x.TermID == sm.TermID));
                                        foreach (var c in dbc.Class.Where(x => x.CourseID == course.ID).OrderBy(x => x.OnDayID))
                                        {
                                            if (c.OnDayID >= todayID) { classOnDayID = c.OnDayID; break; }
                                        }
                                    }
                                }
                                if (leader != null && enrolments.Count > 0)
                                {
                                    onFileKey = (leader.ID, enrolments[0].CourseID, enrolments[0].ClassID);
                                    if (!onFile.Contains(onFileKey))
                                    {
                                        onFile.Add(onFileKey);
                                        if (DailyProcedures.RandomAllocationExecuted[tenant.Identifier!] ||
                                            (classOnDayID >= todayID && classOnDayID <= todayID + 1))
                                        {
                                            if (course != null) courseName = course.Name;
                                            sm.Status = await reportFactory.CreateLeaderReportProForma(leader,
                                                                courseName,
                                                                enrolments.ToArray(),
                                                                DailyProcedures.RandomAllocationExecuted[tenant.Identifier!]);
                                            logger.LogInformation($"{sm.DocumentName} sent to: {leader.FullName} via {leader.Communication}.");
                                        }
                                    }
                                }
                                else { sm.Status = "Enrolments not found."; }
                                break;
                            case "U3A Leaders Reports":
                                if (IsHourlyProcedure && !sm.IsUserRequested) { break; }
                                var thisClass = await dbc.Class.FindAsync(sm.RecordKey);
                                course = await dbc.Course.FindAsync(thisClass!.CourseID);
                                var thisTerm = await dbc.Term.FindAsync(sm.TermID);
                                enrolments = await BusinessRule.GetEnrolmentIncludeLeadersAsync(dbc, course!, thisClass, thisTerm!);
                                if (course!.CourseParticipationTypeID == (int)ParticipationType.SameParticipantsInAllClasses)
                                {
                                    enrolments.AddRange(mcEnrolments.Where(x => x.CourseID == course.ID && x.ClassID == null));
                                }
                                else
                                {
                                    enrolments.AddRange(mcEnrolments.Where(x => x.CourseID == course.ID && x.ClassID == thisClass.ID));
                                }
                                if (enrolments?.Count > 0)
                                {
                                    sm.Status = await reportFactory.CreateLeaderReports(
                                              sm.PrintLeaderReport,
                                              sm.PrintAttendanceRecord,
                                              sm.PrintClassList,
                                              sm.PrintICEList,
                                              sm.PrintCSVFile,
                                              sm.PrintAttendanceAnalysis,
                                              sm.PrintMemberBadges,
                                              course!.ID,
                                              "U3A Report Package",
                                              course.Name,
                                              sm.Person, enrolments.OrderBy(x => x.IsWaitlisted)
                                                                          .ThenBy(x => x.Person.LastName)
                                                                          .ThenBy(x => x.Person.FirstName).ToArray());
                                    logger.LogInformation($"{sm.DocumentName} sent to: {p.FullName} via {p.Communication}.");
                                }
                                else { sm.Status = "Enrolments not found."; }
                                break;
                            default:
                                break;
                        }
                        await dbc.SaveChangesAsync();
                    }

                    // process enrolments because they receive one email per member
                    var enrolmentResults = await reportFactory.CreateEnrolmentProForma(personEnrolments);
                    logger.LogInformation($"Processed {personEnrolments.Count} Participant Enrolment correspondence.");
                    foreach (var kvp in enrolmentResults)
                    {
                        foreach (var sm in dbc.SendMail.Where(x => x.PersonID == kvp.Key))
                        {
                            if (string.IsNullOrWhiteSpace(sm.Status)) { sm.Status = kvp.Value; }
                        }
                        foreach (var sm in dbcT.MultiCampusSendMail.Where(x => x.PersonID == kvp.Key))
                        {
                            if (string.IsNullOrWhiteSpace(sm.Status)) { sm.Status = kvp.Value; }
                        }
                    }
                    await dbc.SaveChangesAsync();
                    await dbcT.SaveChangesAsync();
                    var postalCount = reportFactory.PostalReports.Count;
                    if (postalCount > 0)
                    {
                        await reportFactory.CreateAndSendPostalPDF();
                        logger.LogInformation($"Sent {postalCount} postal report(s) to {reportFactory.SendEmailAddress}.");
                    }
                    // Delete expired records
                    dbc.RemoveRange(dbc.SendMail.AsEnumerable()
                        .Where(x => !string.IsNullOrWhiteSpace(x.Status) &&
                                        (today - x.CreatedOn.GetValueOrDefault()).Days > 30));
                    dbcT.RemoveRange(dbcT.MultiCampusSendMail.AsEnumerable()
                        .Where(x => !string.IsNullOrWhiteSpace(x.Status) &&
                                        (today - x.CreatedOn.GetValueOrDefault()).Days > 30));
                    var deleted = dbc.ChangeTracker.Entries().Where(x => x.State == EntityState.Deleted).Count();
                    deleted += dbcT.ChangeTracker.Entries().Where(x => x.State == EntityState.Deleted).Count();
                    if (deleted > 0)
                    {
                        logger.LogInformation($"Deleted {deleted} correspondence queue records because they are more than 30 days old.");
                    }
                    await dbc.SaveChangesAsync();
                    await dbcT.SaveChangesAsync();
                }
            }
        }
    }
}
