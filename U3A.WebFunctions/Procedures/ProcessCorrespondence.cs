using Microsoft.EntityFrameworkCore;
using Serilog;
using U3A.BusinessRules;
using U3A.Database;
using U3A.Model;
using U3A.UI.Reports;

namespace U3A.WebFunctions.Procedures
{
    public static class ProcessCorrespondence
    {
        public static async Task Process(TenantInfo tenant,
            string tenantConnectionString, U3AFunctionOptions options)
        {
            bool isAdHocReport = false;
            if (string.IsNullOrWhiteSpace(tenant.PostmarkAPIKey) && !tenant.UsePostmarkTestEnviroment) return;
            if (string.IsNullOrWhiteSpace(tenant.PostmarkSandboxAPIKey) && tenant.UsePostmarkTestEnviroment) return;
            var reportFactory = new ProFormaReportFactory(tenant);
            var personEnrolments = new Dictionary<Guid, List<Enrolment>>();
            IList<SendMail> mailItems;
            using (var dbc = new U3ADbContext(tenant))
            {
                dbc.UtcOffset = await Common.GetUtcOffsetAsync(dbc);
                var settings = await dbc.SystemSettings.OrderBy(x => x.ID).FirstOrDefaultAsync();
                var currentTerm = await BusinessRule.CurrentEnrolmentTermAsync(dbc);
                using (var dbcT = new TenantDbContext(tenantConnectionString))
                {
                    List<(Guid, Guid, Guid?)> onFile = new();
                    (Guid, Guid, Guid?) onFileKey;
                    var today = await Common.GetTodayAsync(dbc);
                    var utcTime = DateTime.UtcNow;
                    if (options.SendMailIdsToProcess != null && options.SendMailIdsToProcess.Count > 0)
                    {
                        mailItems = await dbc.SendMail.IgnoreQueryFilters()
                                                .Include(x => x.Person)
                                                .Where(x => !x.Person.IsDeleted &&
                                                                options.SendMailIdsToProcess.Contains(x.ID)).ToListAsync();
                        isAdHocReport = true; // generated via HTTP request
                    }
                    else
                    {
                        mailItems = await dbc.SendMail.IgnoreQueryFilters()
                                                .Include(x => x.Person)
                                                .Where(x => !x.Person.IsDeleted && string.IsNullOrWhiteSpace(x.Status)
                                                        && utcTime >= x.CreatedOn).ToListAsync();
                        foreach (var sm in await BusinessRule.GetMultiCampusMailAsync(dbcT, tenant.Identifier!))
                        {
                            if (string.IsNullOrWhiteSpace(sm.Status)) { mailItems.Add(sm); }
                        }
                        isAdHocReport = false;
                    }
                    var mcEnrolments = await BusinessRule.GetMultiCampusEnrolmentsAsync(dbc, dbcT, tenant.Identifier!);
                    foreach (SendMail sm in mailItems)
                    {
                        var p = sm.Person;
                        string courseName = "";
                        Course? course = null;
                        var enrolments = new List<Enrolment>();
                        switch (sm.DocumentName)
                        {
                            case "Cash Receipt":
                                var receipt = await dbc.Receipt.IgnoreQueryFilters()
                                                    .Include(x => x.Person)
                                                    .Where(x => x.ID == sm.RecordKey).FirstOrDefaultAsync();
                                if (receipt != null)
                                {
                                    sm.Status = await reportFactory.CreateCashReceiptProForma(receipt);
                                    Log.Information($"{sm.DocumentName} sent to: {p.FullName} via {p.Communication}.");
                                }
                                else sm.Status = "Receipt not found";
                                break;
                            case "Participant Enrolment":
                                var enrolment = await dbc.Enrolment.IgnoreQueryFilters()
                                                    .Include(x => x.Course)
                                                    .Include(x => x.Person)
                                                    .Where(x => !x.IsDeleted && x.ID == sm.RecordKey).FirstOrDefaultAsync();
                                if (enrolment == null && currentTerm != null && settings != null)
                                {
                                    if (BusinessRule.IsRandomAllocationTerm(currentTerm, settings))
                                    {
                                        if (!BusinessRule.IsPreRandomAllocationEmailDay(currentTerm, settings, today))
                                        {
                                            enrolment = await BusinessRule.GetMultiCampusEnrolmentAsync(dbc, dbcT, sm.RecordKey, tenant.Identifier!);
                                        }
                                    }
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
                                    Log.Information($"{sm.DocumentName} for {p.FullName} added to queue.");
                                }
                                else sm.Status = "Enrollment not found";
                                break;
                            case "Leader Report":
                                if (options.IsDailyProcedure || isAdHocReport)
                                {
                                    var leader = await dbc.Person.IgnoreQueryFilters()
                                                        .Where(x => !x.IsDeleted && x.ID == sm.PersonID).FirstOrDefaultAsync();
                                    var todayID = (int)today.DayOfWeek;
                                    int classOnDayID = -1;
                                    if (await dbc.Class.AnyAsync(x => x.ID == sm.RecordKey))
                                    {
                                        enrolments = await dbc.Enrolment.IgnoreQueryFilters()
                                                            .Where(x => !x.IsDeleted && x.ClassID == sm.RecordKey
                                                                   && x.TermID == sm.TermID).ToListAsync();
                                        enrolments.AddRange(mcEnrolments
                                                            .Where(x => x.ClassID == sm.RecordKey && x.TermID == sm.TermID));
                                        var Class = await dbc.Class.FindAsync(sm.RecordKey);
                                        course = await dbc.Course.FindAsync(Class!.CourseID);
                                        classOnDayID = Class.OnDayID;
                                    }
                                    else
                                    {
                                        course = await dbc.Course.FindAsync(sm.RecordKey);
                                        if (course != null)
                                        {
                                            enrolments = await dbc.Enrolment.IgnoreQueryFilters()
                                                                .Where(x => !x.IsDeleted && x.CourseID == course.ID
                                                                                  && x.TermID == sm.TermID).ToListAsync();
                                            enrolments.AddRange(mcEnrolments.Where(x => x.CourseID == course.ID && x.ClassID == null
                                                                                  && x.TermID == sm.TermID));
                                            foreach (var c in await dbc.Class.Where(x => x.CourseID == course.ID).OrderBy(x => x.OnDayID).ToListAsync())
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
                                            if (isAdHocReport || options.HasRandomAllocationExecuted ||
                                                (classOnDayID >= todayID && classOnDayID <= todayID + 1))
                                            {
                                                if (course != null) courseName = course.Name;
                                                sm.Status = await reportFactory.CreateLeaderReportProForma(leader,
                                                                    courseName,
                                                                    enrolments.ToArray(),
                                                                    options.HasRandomAllocationExecuted);
                                                Log.Information($"{sm.DocumentName} sent to: {leader.FullName} via {leader.Communication}.");
                                            }
                                        }
                                        else
                                        {
                                            Log.Information($"{sm.DocumentName} already sent to: {leader.FullName}.");
                                            sm.Status = "Accepted";
                                        }
                                    }
                                    else { sm.Status = "Enrollments not found."; }
                                }
                                break;
                            case "U3A Leaders Reports":
                                if (options.IsDailyProcedure || sm.IsUserRequested)
                                {
                                    var thisClass = await dbc.Class.FindAsync(sm.RecordKey);
                                    if (thisClass == null)
                                    {
                                        sm.Status = "Class not found - deleted!";
                                        continue;
                                    }
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
                                        Log.Information($"{sm.DocumentName} sent to: {p.FullName} via {p.Communication}.");
                                    }
                                    else { sm.Status = "Enrollments not found."; }
                                }
                                break;
                            default:
                                break;
                        }
                        await dbc.SaveChangesAsync();
                    }

                    // process enrollments because they receive one email per member
                    var enrolmentResults = await reportFactory.CreateEnrolmentProForma(personEnrolments);
                    Log.Information($"Processed {personEnrolments.Count} Participant Enrollment correspondence.");
                    foreach (var kvp in enrolmentResults)
                    {
                        foreach (var sm in await dbc.SendMail.IgnoreQueryFilters().Where(x => x.PersonID == kvp.Key).ToListAsync())
                        {
                            sm.Status = kvp.Value;
                        }
                        foreach (var sm in await dbcT.MultiCampusSendMail.Where(x => x.PersonID == kvp.Key).ToListAsync())
                        {
                            sm.Status = kvp.Value;
                        }
                    }
                    await dbc.SaveChangesAsync();
                    await dbcT.SaveChangesAsync();

                    var postalCount = reportFactory.PostalReports.Count;
                    if (postalCount > 0)
                    {
                        await reportFactory.CreateAndSendPostalPDF();
                        Log.Information($"Sent {postalCount} postal report(s) to {reportFactory.SendEmailAddress}.");
                    }
                    // Delete expired records
                    dbc.RemoveRange(dbc.SendMail.AsEnumerable()
                                            .Where(x => (today - x.CreatedOn.GetValueOrDefault()).Days > 30));
                    dbcT.RemoveRange(dbcT.MultiCampusSendMail.AsEnumerable()
                        .Where(x => (today - x.CreatedOn.GetValueOrDefault()).Days > 30));
                    var deleted = dbc.ChangeTracker.Entries().Where(x => x.State == EntityState.Deleted).Count();
                    deleted += dbcT.ChangeTracker.Entries().Where(x => x.State == EntityState.Deleted).Count();
                    if (deleted > 0)
                    {
                        Log.Information($"Deleted {deleted} correspondence queue records because they are more than 30 days old.");
                    }
                    await dbc.SaveChangesAsync();
                    await dbcT.SaveChangesAsync();
                }
            }
        }
    }
}
