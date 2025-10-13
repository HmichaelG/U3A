using Microsoft.Azure.Functions.Worker;
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
        U3AFunctionOptions options = new(req);
        if (req.Body != null)
        {
            using StreamReader reader = new(req.Body);
            string body = await reader.ReadToEndAsync();
            if (!string.IsNullOrEmpty(body))
            {
                // retrieve SendMail from request body json text
                SendMail? printDoc = JsonSerializer.Deserialize<SendMail>(body);
                options.PrintDoc = printDoc;
            }
        }
        byte[]? pdf = await CreateMailPreview(options);
        HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/pdf");
        response.Headers.Add("Content-Disposition", "attachment; filename=GeneratedDocument.pdf");
        if (pdf != null) { await response.WriteBytesAsync(pdf); }
        return response;
    }

    private async Task<byte[]?> CreateMailPreview(U3AFunctionOptions options)
    {
        byte[]? Result = null;
        string? cn = config.GetConnectionString(Common.TENANT_CN_CONFIG);
        if (cn is null) { throw new NullReferenceException("Database Connection string is null"); }
        TenantInfo? tenant = Common.GetTenant(options.TenantIdentifier, cn);
        if (tenant is null) { throw new NullReferenceException("Tenant not found"); }

        Dictionary<Guid, List<Enrolment>> personEnrolments = [];
        List<(Guid, Guid, Guid?)> onFile = [];
        (Guid, Guid, Guid?) onFileKey;
        ProFormaReportFactory reportFactory = new(tenant, IsPreview: true);
        List<Enrolment>? enrolments = [];
        List<SendMail> mailItems = [];
        using U3ADbContext dbc = new(tenant);
        using TenantDbContext dbcT = new(cn);
        if (options.PrintDoc is not null) { mailItems.Add(options.PrintDoc); }
        else
        {
            mailItems = await dbc.SendMail.IgnoreQueryFilters()
                                 .Include(x => x.Person)
                                 .Where(x => !x.Person.IsDeleted &&
                                                 options.SendMailIdsToProcess.Contains(x.ID)).ToListAsync();
        }
        List<Enrolment> mcEnrolments = await BusinessRule.GetMultiCampusEnrolmentsAsync(dbc, dbcT, tenant.Identifier!);
        foreach (SendMail sm in mailItems)
        {
            List<Enrolment> scopedMCEnrolments = mcEnrolments;  //Don't know why - probably something to do with LINQ
            switch (sm.DocumentName)
            {
                case "Cash Receipt":
                    Receipt? receipt = await dbc.Receipt
                                        .Include(x => x.Person)
                                        .Where(x => x.ID == sm.RecordKey).FirstOrDefaultAsync();
                    if (receipt != null)
                    {
                        _ = await reportFactory.CreateCashReceiptProForma(receipt);
                    }
                    break;
                case "Participant Enrolment":
                    Enrolment? enrolment = await dbc.Enrolment.IgnoreQueryFilters()
                                        .Include(x => x.Course)
                                        .Include(x => x.Person)
                                        .Where(x => !x.IsDeleted && x.ID == sm.RecordKey).FirstOrDefaultAsync();
                    enrolment ??= await BusinessRule.GetMultiCampusEnrolmentAsync(dbc, dbcT, sm.RecordKey, tenant.Identifier!);
                    if (enrolment != null)
                    {
                        Guid key = enrolment.PersonID;
                        List<Enrolment>? theseEnrolments = [];
                        if (!personEnrolments.TryGetValue(key, out theseEnrolments))
                        {
                            theseEnrolments = [];
                            personEnrolments.Add(key, theseEnrolments);
                        }
                        theseEnrolments.Add(enrolment);
                    }
                    break;
                case "Leader Report":
                    string courseName = "";
                    Course? course = null;
                    Person? leader = await dbc.Person
                                        .Where(x => x.ID == sm.PersonID).FirstOrDefaultAsync();
                    if (dbc.Class.Any(x => x.ID == sm.RecordKey))
                    {
                        enrolments = await dbc.Enrolment.Where(x => x.ClassID == sm.RecordKey
                                                            && x.TermID == sm.TermID).ToListAsync();
                        enrolments.AddRange(scopedMCEnrolments.Where(x => x.ClassID == sm.RecordKey && x.TermID == sm.TermID));
                        Class? Class = await dbc.Class.FindAsync(sm.RecordKey);
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
                        if (course != null)
                        {
                            courseName = course.Name;
                        }

                        onFileKey = (leader.ID, enrolments[0].CourseID, enrolments[0].ClassID);
                        if (!onFile.Contains(onFileKey))
                        {
                            onFile.Add(onFileKey);
                            _ = await reportFactory.CreateLeaderReportProForma(leader, courseName, enrolments.ToArray());
                        }
                    }
                    break;
                case "U3A Leaders Reports":
                    Class? thisClass = await dbc.Class.FindAsync(sm.RecordKey);
                    if (thisClass is null) { continue; }
                    course = await dbc.Course.FindAsync(thisClass.CourseID);
                    Term? thisTerm = await dbc.Term.FindAsync(sm.TermID);
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
                        _ = await reportFactory.CreateLeaderReports(
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
        Dictionary<Guid, string> enrolmentResults = await reportFactory.CreateEnrolmentProForma(personEnrolments);
        string? pdfFilename = reportFactory.CreatePostalPDF();
        if (pdfFilename != null)
        {
            string pdfPath = Path.Combine(reportFactory.ReportStorage.TempDirectory, pdfFilename);
            Result = File.ReadAllBytes(pdfPath);
        }
        return Result;
    }

}
