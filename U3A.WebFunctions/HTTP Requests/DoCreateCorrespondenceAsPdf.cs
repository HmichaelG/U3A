﻿using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.Net;
using System.Text.Json;
using U3A.BusinessRules;
using U3A.Database;
using U3A.Model;
using U3A.UI.Reports;


namespace U3A.WebFunctions;

public class DoCreateCorrespondenceAsPdf
{
    private readonly IConfiguration config;
    public DoCreateCorrespondenceAsPdf(IConfiguration config)
    {
        this.config = config;
    }

    [Function("DoCreateCorrespondenceAsPdf")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        Log.Information("C# HTTP trigger function processed a request.");
        var options = new U3AFunctionOptions(req);
        // retrieve SendMail from request body json text
        SendMail? printDoc = null;
        if (req.Body != null)
        {
            using var reader = new StreamReader(req.Body);
            var body = await reader.ReadToEndAsync();
            if (!string.IsNullOrEmpty(body))
            {
                printDoc = JsonSerializer.Deserialize<SendMail>(body);
                options.PrintDoc = printDoc;
            }
        }
        Byte[]? pdf = await CreateMailPreview(options);
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/pdf");
        response.Headers.Add("Content-Disposition", "attachment; filename=GeneratedDocument.pdf");
        if (pdf != null) { await response.WriteBytesAsync(pdf); }
        return response;
    }

    async Task<Byte[]?> CreateMailPreview(U3AFunctionOptions options)
    {
        Byte[]? Result = null;
        var cn = config.GetConnectionString(Common.TENANT_CN_CONFIG);
        if (cn is null) { throw new NullReferenceException("Database Connection string is null"); }
        var tenant = Common.GetTenant(options.TenantIdentifier, cn);
        if (tenant is null) { throw new NullReferenceException("Tenant not found"); }

        var personEnrolments = new Dictionary<Guid, List<Enrolment>>();
        List<(Guid, Guid, Guid?)> onFile = new();
        (Guid, Guid, Guid?) onFileKey;
        var reportFactory = new ProFormaReportFactory(tenant, IsPreview: true);
        var enrolments = new List<Enrolment>();
        List<SendMail> mailItems = new();
        using (var dbc = new U3ADbContext(tenant))
        {
            using (var dbcT = new TenantDbContext(cn))
            {
                if (options.PrintDoc is not null) { mailItems.Add(options.PrintDoc); }
                else
                {
                    mailItems = await dbc.SendMail.IgnoreQueryFilters()
                                         .Include(x => x.Person)
                                         .Where(x => !x.Person.IsDeleted &&
                                                         options.SendMailIdsToProcess.Contains(x.ID)).ToListAsync();
                }
                var mcEnrolments = await BusinessRule.GetMultiCampusEnrolmentsAsync(dbc, dbcT, tenant.Identifier!);
                foreach (SendMail sm in mailItems)
                {
                    var scopedMCEnrolments = mcEnrolments;  //Don't know why - probably something to do with LINQ
                    switch (sm.DocumentName)
                    {
                        case "Cash Receipt":
                            var receipt = await dbc.Receipt
                                                .Include(x => x.Person)
                                                .Where(x => x.ID == sm.RecordKey).FirstOrDefaultAsync();
                            if (receipt != null)
                            {
                                await reportFactory.CreateCashReceiptProForma(receipt);
                            }
                            break;
                        case "Participant Enrolment":
                            var enrolment = await dbc.Enrolment.IgnoreQueryFilters()
                                                .Include(x => x.Course)
                                                .Include(x => x.Person)
                                                .Where(x => !x.IsDeleted && x.ID == sm.RecordKey).FirstOrDefaultAsync();
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
                            }
                            break;
                        case "Leader Report":
                            string courseName = "";
                            Course? course = null;
                            var leader = await dbc.Person
                                                .Where(x => x.ID == sm.PersonID).FirstOrDefaultAsync();
                            if (dbc.Class.Any(x => x.ID == sm.RecordKey))
                            {
                                enrolments = await dbc.Enrolment.Where(x => x.ClassID == sm.RecordKey
                                                                    && x.TermID == sm.TermID).ToListAsync();
                                enrolments.AddRange(scopedMCEnrolments.Where(x => x.ClassID == sm.RecordKey && x.TermID == sm.TermID));
                                var Class = await dbc.Class.FindAsync(sm.RecordKey);
                                if (Class != null)
                                {
                                    course = await dbc.Course.FindAsync(Class.CourseID);
                                }
                            }
                            else
                            {
                                course = dbc.Course.Find(sm.RecordKey);
                                if (course != null)
                                {
                                    enrolments = dbc.Enrolment.Where(x => x.CourseID == course.ID
                                                                          && x.TermID == sm.TermID).ToList();
                                    enrolments.AddRange(scopedMCEnrolments.Where(x => x.CourseID == course.ID && x.ClassID == null
                                                                          && x.TermID == sm.TermID));
                                }
                            }
                            ;
                            if (leader != null && enrolments != null && enrolments.Count > 0)
                            {
                                if (course != null) courseName = course.Name;
                                onFileKey = (leader.ID, enrolments[0].CourseID, enrolments[0].ClassID);
                                if (!onFile.Contains(onFileKey))
                                {
                                    onFile.Add(onFileKey);
                                    await reportFactory.CreateLeaderReportProForma(leader, courseName, enrolments.ToArray());
                                }
                            }
                            break;
                        case "U3A Leaders Reports":
                            Class? thisClass = await dbc.Class.FindAsync(sm.RecordKey);
                            if (thisClass is null) { continue; }
                            course = await dbc.Course.FindAsync(thisClass.CourseID);
                            var thisTerm = await dbc.Term.FindAsync(sm.TermID);
                            enrolments = await BusinessRule.GetEnrolmentIncludeLeadersAsync(dbc, course!, thisClass, thisTerm!);
                            if (course != null)
                            {
                                if (course.CourseParticipationTypeID == (int)ParticipationType.SameParticipantsInAllClasses)
                                {
                                    enrolments.AddRange(scopedMCEnrolments.Where(x => x.CourseID == course.ID && x.ClassID == null));
                                }
                                else
                                {
                                    enrolments.AddRange(scopedMCEnrolments.Where(x => x.CourseID == course.ID && x.ClassID == thisClass.ID));
                                }
                            }
                            bool isEnrolmentReqd = sm.PrintAttendanceRecord ||
                                                            sm.PrintClassList ||
                                                            sm.PrintCSVFile ||
                                                            sm.PrintICEList ||
                                                            sm.PrintLeaderReport;
                            if (enrolments?.Count > 0 || !isEnrolmentReqd)
                            {
                                await reportFactory.CreateLeaderReports(
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
                                     sm.Person ?? dbc.Person.Find(sm.PersonID) ?? new Person(),
                                     enrolments!.OrderBy(x => x.IsWaitlisted)
                                                                 .ThenBy(x => x.Person.LastName)
                                                                 .ThenBy(x => x.Person.FirstName).ToArray());
                            }
                            break;
                        default:
                            break;
                    }
                }
                // process enrolments because they receive one email per member
                var enrolmentResults = await reportFactory.CreateEnrolmentProForma(personEnrolments);
                var pdfFilename = reportFactory.CreatePostalPDF();
                if (pdfFilename != null)
                {
                    var pdfPath = Path.Combine(reportFactory.ReportStorage.TempDirectory, pdfFilename);
                    Result = File.ReadAllBytes(pdfPath);
                }
                return Result;
            }
        }
    }

}
