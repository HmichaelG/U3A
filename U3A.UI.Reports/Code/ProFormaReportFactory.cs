using DevExpress.CodeParser;
using DevExpress.DataAccess.Json;
using DevExpress.DataAccess.ObjectBinding;
using DevExpress.DataAccess.Sql;
using DevExpress.ReportServer.ServiceModel.DataContracts;
using DevExpress.XtraCharts.Native;
using DevExpress.XtraPrinting;
using DevExpress.XtraReports;
using DevExpress.XtraReports.UI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using U3A.BusinessRules;
using U3A.Database;
using U3A.Model;
using U3A.Services;
using U3A.UI.Reports;
using static DevExpress.Xpo.Helpers.AssociatedCollectionCriteriaHelper;

namespace U3A.UI.Reports
{
    public class ProFormaReportFactory : IDisposable
    {

        public List<string> PostalReports { get; set; }

        IDbContextFactory<U3ADbContext> U3AdbFactory;
        U3ADbContext? dbc;
        CustomReportStorageWebExtension storage;

        IEmailService emailSender;
        PdfExportOptions options;
        bool isPreview;
        bool isAzureFunction;
        public string SendEmailAddress { get; set; }
        string sendEmailDisplayName;
        string tenantID;

        //Azure Functions only
        public ProFormaReportFactory(TenantInfo tenant)
        {
            dbc = new U3ADbContext(tenant);
            isAzureFunction = true;
            isPreview = false;
            tenantID = tenant.Identifier;
            storage = new CustomReportStorageWebExtension(tenant);
            var settings = dbc.SystemSettings.FirstOrDefault() ?? throw new ArgumentNullException(nameof(SystemSettings));
            if (settings != null)
            {
                SendEmailAddress = settings.SendEmailAddesss;
                sendEmailDisplayName = settings.SendEmailDisplayName;
            }
            emailSender = EmailFactory.GetEmailSender(dbc);
            options = new PdfExportOptions()
            {
                ImageQuality = PdfJpegImageQuality.Lowest,
            };
            PostalReports = new List<string>();
        }

        // Web app 
        public ProFormaReportFactory(IWebHostEnvironment env, IDbContextFactory<U3ADbContext> U3AdbFactory, bool IsPreview)
        {
            this.U3AdbFactory = U3AdbFactory ?? throw new ArgumentNullException(nameof(U3AdbFactory));
            storage = new CustomReportStorageWebExtension(env, this.U3AdbFactory);
            isPreview = IsPreview;
            dbc = this.U3AdbFactory.CreateDbContext();
            tenantID = dbc.TenantInfo.Identifier;
            var settings = dbc.SystemSettings.FirstOrDefault() ?? throw new ArgumentNullException(nameof(SystemSettings));
            if (settings != null)
            {
                SendEmailAddress = settings.SendEmailAddesss;
                sendEmailDisplayName = settings.SendEmailDisplayName;
            }
            emailSender = EmailFactory.GetEmailSender(dbc);
            options = new PdfExportOptions()
            {
                ImageQuality = PdfJpegImageQuality.Lowest,
            };
            PostalReports = new List<string>();
        }

        public async Task<string> CreateCashReceiptProForma(Receipt receipt)
        {
            using (var cashReceiptProForma = new CashReceipt())
            {
                var detail = BusinessRule.GetReceiptDetail(dbc, receipt);
                var person = await dbc.Person.FindAsync(receipt.PersonID);
                var list = new List<ReceiptDetail>();
                list.Add(detail);
                cashReceiptProForma.DataSource = list;
                string pdfFilename = GetTempPdfFile();
                cashReceiptProForma.ExportToPdf(pdfFilename, options);
                if (!isPreview && person.Communication.ToLower() == "email")
                {
                    return await emailSender.SendEmailAsync(EmailType.Transactional,
                                    SendEmailAddress,
                                    sendEmailDisplayName,
                                    person.Email,
                                    person.FullName,
                                    $"U3A Cash Receipt: {person.FullName}",
                                    $"<p>Hello {person.FirstName},</p>" +
                                    $"<p>Please find your U3A cash receipt attached.</p>" +
                                    GetBlurb() +
                                    $"<p><p>Thank you<br/>" +
                                    $"{sendEmailDisplayName}</p>",
                                    string.Empty,
                                    new List<string>() { pdfFilename },
                                    new List<string>() { "Cash Receipt.pdf" }
                                    );
                }
                else
                {
                    PostalReports.Add(pdfFilename);
                    return (isPreview) ? String.Empty : "Accepted";
                }
            }
        }
        public async Task<Dictionary<Guid, string>> CreateEnrolmentProForma(Dictionary<Guid, List<Enrolment>> Enrolments)
        {
            var result = new Dictionary<Guid, string>();
            foreach (var kvp in Enrolments)
            {
                List<(Guid CourseID, Guid? ClassID)> onFile = new();
                List<Term> terms = new();
                var currentTerm = BusinessRule.CurrentTerm(dbc);
                if (currentTerm != null)
                {
                    terms = await dbc.Term.Where(x => x.Year == currentTerm.Year).ToListAsync();
                }
                var person = await dbc.Person.FindAsync(kvp.Key);
                var personsFiles = new List<string>();
                foreach (var enrolment in kvp.Value.OrderBy(x => x.Course.Name))
                {
                    (Guid, Guid?) onfileKey = (enrolment.CourseID, enrolment.ClassID);
                    if (!onFile.Contains(onfileKey)) // one report per enrolment / class
                    {
                        onFile.Add(onfileKey);
                        var detail = BusinessRule.GetEnrolmentDetail(dbc, enrolment);
                        using (var participantEnrolmentProForma = new ParticipantEnrolment())
                        {
                            participantEnrolmentProForma.DataSource = detail;
                            var dataSources = DataSourceManager.GetDataSources(participantEnrolmentProForma, includeSubReports: false);
                            foreach (var dataSource in dataSources)
                            {
                                if (dataSource  is ObjectDataSource)
                                {
                                   var ds = (dataSource as ObjectDataSource);
                                    if (ds.Name.ToLower() == "objectdatasource2") { ds.DataSource = terms; }
                                }
                            }
                            string pdf = GetTempPdfFile();
                            participantEnrolmentProForma.ExportToPdf(pdf, options);
                            personsFiles.Add(pdf);
                        }
                    }
                }
                var pdfFilename = CreateMergedPDF(personsFiles);
                if (string.IsNullOrWhiteSpace(pdfFilename)) { continue; }
                if (!isPreview && person.Communication.ToLower() == "email")
                {
                    result.Add(kvp.Key, await emailSender.SendEmailAsync(
                                   EmailType.Transactional,
                                   SendEmailAddress,
                                   sendEmailDisplayName,
                                   person.Email,
                                   person.FullName,
                                   $"U3A Enrolment: {person.FullName}",
                                   $"<p>Hello {person.FirstName},</p>" +
@"
<style>
table {font - family: arial, sans-serif;
  border-collapse: collapse;
}

td, th {border: 1px solid #dddddd;
  text-align: left;
  padding: 8px;
}

</style><p>Your U3A enrolment details are attached. 
Please review and note the status of your enrolment request...</p>
<table>
<tr>
<th style='width: 15%'>If Status is ...</th>
<th>The meaning is ...</th>
</tr>
<tr>
<td><strong>Enrolled</strong></td> 
<td>Your request has been accepted and you are now an active member in the class. Please attend class at the scheduled time.</td>
</tr>
<tr>
<td><strong>Waitlisted</strong></td> 
<td>Your request <strong>has not</strong> been accepted normally because... 
<ol>
<li>the class is full, or</li>
<li>Your requested class is not available this term. It is waitlisted until enrolment opens and allocation occurs, or</li>
<li>the class leader has requested no further enrolments, or</li>
<li>our records indicate that you are currently unfinancial.</li>
</ol>
<p>You are waitlisted and will be notified should a place become available.
Please <strong>do not</strong> attend class unless otherwise notified by email or directly by the class leader.</p>
</td>
</tr>
</table>" +
                                    GetBlurb() +
                                   $"<p><p>Thank you<br/>" +
                                   $"{sendEmailDisplayName}</p>",
                                   string.Empty,
                                   new List<string>() { pdfFilename },
                                   new List<string>() { "Your Enrolment Details.pdf" }
                                   )); ;
                }
                else
                {
                    PostalReports.Add(pdfFilename);
                    result.Add(kvp.Key, (isPreview) ? String.Empty : "Accepted");
                }
            }
            return result;
        }

        public async Task<string> CreateLeaderReports(
                                        bool DoLeaderReport,
                                        bool DoLeaderAttendanceList,
                                        bool DoLeaderClassList,
                                        bool DoLeaderICEList,
                                        bool DoLeaderCSVFile,
                                        bool DoAttendanceAnalysis,
                                        Guid CourseID,
                                        string ReportName,
                                        string CourseName,
                                        Person Leader, Enrolment[] Enrolments)
        {
            var result = string.Empty;
            var createdFilenames = new List<string>();
            var reportNames = new List<string>();
            if (DoLeaderReport || !(Enrolments.Where(x => !x.IsWaitlisted).Any()))
            {
                using (var leaderReportProForma = new LeaderReport())
                {
                    createdFilenames.Add(await CreateLeaderReportAsync(leaderReportProForma, Leader, Enrolments));
                    reportNames.Add("Leader's Report.pdf");
                }
            }
            if (Enrolments.Where(x => !x.IsWaitlisted).Any())
            {
                if (DoLeaderAttendanceList)
                {
                    using (var leaderAttendanceList = new LeaderAttendanceList())
                    {
                        createdFilenames.Add(await CreateLeaderReportAsync(leaderAttendanceList,
                                                Leader, Enrolments
                                                            .Where(x => !x.IsWaitlisted && !x.isLeader)
                                                            .ToArray()));
                        reportNames.Add("Student Attendance Record.pdf");
                    }
                }
                if (DoLeaderClassList)
                {
                    using (var leaderClassList = new LeaderClassList())
                    {
                        createdFilenames.Add(await CreateLeaderReportAsync(leaderClassList,
                                                Leader, Enrolments.Where(x => !x.IsWaitlisted).ToArray()));
                        reportNames.Add("Class Contact Listing.pdf");
                    }
                }
                if (DoLeaderICEList)
                {
                    using (var leaderICEList = new LeaderICEList())
                    {
                        createdFilenames.Add(await CreateLeaderReportAsync(leaderICEList,
                                                Leader, Enrolments.Where(x => !x.IsWaitlisted).ToArray()));
                        reportNames.Add("Class ICE Listing.pdf");
                    }
                }
                if (DoAttendanceAnalysis)
                {
                    using (var AttendanceAnalysis = new AttendanceAnalysis())
                    {
                        createdFilenames.Add(CreateAttendanceAnalysisReport(AttendanceAnalysis,
                                                Leader, CourseID));
                        reportNames.Add("Attendance Analysis.pdf");
                    }
                }
                if (DoLeaderCSVFile && !isPreview)
                {
                    createdFilenames.Add(CreateCSVFile(Enrolments.Where(x => !x.IsWaitlisted).ToArray()));
                    reportNames.Add("Class List.csv");
                }
            }
            return await ProcessLeaderReport(Leader, ReportName, CourseName, createdFilenames.ToArray(), reportNames.ToArray());
        }

        string CreateCSVFile(Enrolment[] Enrolments)
        {
            var sb = new StringBuilder();
            sb.AppendLine("\"WaitListed?\",\"LastName\",\"FirstName\",\"Email\",\"Mobile\",\"Home\",\"Address\",\"City\",\"State\"");
            foreach (var e in Enrolments)
            {
                var p = e.Person; if (p != null)
                {
                    sb.AppendLine($"\"{e.IsWaitlisted}\",\"{p.LastName}\",\"{p.FirstName}\",\"{p.Email}\",\"{p.HomePhone}\",\"{p.Mobile}\",\"{p.Address}\",\"{p.City}\",\"{p.State}\"");
                }
            }
            var outputFilename = GetTempCSVFile();
            using (var sw = new StreamWriter(outputFilename))
            {
                sw.Write(sb.ToString());
                sw.Flush();
                sw.Close();
            }
            return outputFilename;
        }
        public async Task<string> CreateLeaderReportProForma(Person Leader,
                                    string CourseName,
                                    Enrolment[] Enrolments,
                                    bool RandomAllocationExecuted = true)
        {
            using (var leaderReportProForma = new LeaderReport())
            {
                var report = await CreateLeaderReportAsync(leaderReportProForma, Leader, Enrolments);
                return await ProcessLeaderReport(Leader,
                                "U3A Leader's Report",
                                CourseName,
                                new string[] { report },
                                new string[] { "Leader Report.pdf" },
                                RandomAllocationExecuted);
            }
        }
        string CreateAttendanceAnalysisReport(AttendanceAnalysis report,
                Person Leader,
                Guid CourseID)
        {
            report.DbContext = dbc;
            report.Parameters["prmCourseID"].Value = CourseID;
            string pdfFilename = GetTempPdfFile();
            report.ExportToPdf(pdfFilename, options);
            return pdfFilename;
        }
        async Task<string> CreateLeaderReportAsync(XtraReport report,
                Person Leader,
                Enrolment[] Enrolments)
        {
            var term = dbc.Term.Find(Enrolments[0].TermID);
            var leaderDetail = BusinessRule.GetLeaderDetail(dbc, Leader, term);
            var enrolmentDetails = new List<EnrolmentDetail>();
            foreach (var enrolment in Enrolments)
            {
                enrolmentDetails.AddRange(BusinessRule.GetEnrolmentDetail(dbc, enrolment));
            }
            var dataSources = DataSourceManager.GetDataSources<ObjectDataSource>(
                report: report,
                includeSubReports: true
            );
            foreach (var ds in dataSources)
            {
                ds.DataMember = String.Empty;
                if (ds.Name == "objectDataSource1")
                    ds.DataSource = leaderDetail;
                else
                    ds.DataSource = enrolmentDetails;
            }
            string pdfFilename = GetTempPdfFile();
            report.ExportToPdf(pdfFilename, options);
            return pdfFilename;
        }

        async Task<string> ProcessLeaderReport(Person Leader,
                                    string ReportName,
                                    string CourseName,
                                    string[] CreatedFilenames,
                                    string[] ReportsNames,
                                    bool RandomAllocationExecuted = false)
        {
            string randomAllocationMsg = (RandomAllocationExecuted)
                ?
                    $@"<p>You have received this email because the random allocation of 
                            student enrolment requests was performed early this morning.
                            You now have {constants.RANDOM_ALLOCATION_PREVIEW} days to review this allocation
                            and request changes prior to your students being informed by email.</p>
                            <p>Please keep enrolment details confidential until students have received their email.</p>"
                : "";

            if (!isPreview && Leader.Communication.ToLower() == "email")
            {
                return await emailSender.SendEmailAsync(
                                EmailType.Transactional,
                                SendEmailAddress,
                                sendEmailDisplayName,
                                Leader.Email,
                                Leader.FullName,
                                $"{ReportName}: {CourseName}",
                                $"<p>Hello {Leader.FirstName},</p>" +
                                $"<p>Please find your U3A Leader's report for <strong>{CourseName}</strong> attached.</p>" +
                                randomAllocationMsg +
                                GetBlurb() +
                                $"<p><p>Thank you<br/>" +
                                $"{sendEmailDisplayName}</p>",
                                string.Empty,
                                CreatedFilenames, ReportsNames
                                );
            }
            else
            {
                PostalReports.AddRange(CreatedFilenames);
                return (isPreview) ? String.Empty : "Accepted";
            }
        }

        string GetBlurb()
        {
            return "<p>Use the <strong>U3A Member Portal</strong> to pay membership fees, change your membership details, enrol in classes and more. " +
                    "Click the button below to access the portal...</p>" +
                    @$"<table border=""0"" cellspacing=""0"" cellpadding=""0"">
                        <tr>
                        <td style=""padding: 12px 18px 12px 18px; border-radius:5px; background-color: #1F7F4C;"" align=""center"">
                        <a rel=""noopener"" target=""_blank"" href=""https://{tenantID}.u3admin.org.au"" target=""_blank"" style=""font-size: 18px; font-family: Helvetica, Arial, sans-serif; font-weight: bold; color: #ffffff; text-decoration: none; display: inline-block;"">U3A Member Portal &rarr;</a>
                        </td>
                        </tr>
                        </table>";
        }
        public string? CreatePostalPDF()
        {
            return Path.GetFileName(CreateMergedPDF(PostalReports));
        }
        public async Task CreateAndSendPostalPDF()
        {
            var filename = CreateMergedPDF(PostalReports);
            if (!string.IsNullOrEmpty(filename))
            {
                await emailSender.SendEmailAsync(
                                EmailType.Transactional,
                                "system@u3admin.org.au",
                                "System Postman",
                                SendEmailAddress,
                                sendEmailDisplayName,
                                $"U3A Reports to be posted",
                                $"<p>Hello,</p>" +
                                $"<p>Please find your U3A postal report(s) attached.</p>" +
                                $"<p><p>Thank you<br/>" +
                                $"{sendEmailDisplayName}</p>" +
                                "Please do not reply. This email address is not monitored.",
                                string.Empty,
                                new string[] { filename },
                                new string[] { "Postal Reports.pdf" }
                                );
            }
        }
        public string? CreateMergedPDF(List<string> pdfFilenames)
        {
            if (pdfFilenames.Count > 0)
            {
                PdfDocument outputDocument = new PdfDocument();
                foreach (string file in pdfFilenames.Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    PdfDocument inputDocument = PdfReader.Open(file, PdfDocumentOpenMode.Import);
                    int count = inputDocument.PageCount;
                    for (int idx = 0; idx < count; idx++)
                    {
                        PdfPage page = inputDocument.Pages[idx];
                        outputDocument.AddPage(page);
                    }
                }
                outputDocument.Close();
                // Save the document...
                var outputFile = GetTempPdfFile();
                outputDocument.Save(outputFile);
                return outputFile;
            }
            else return null;
        }

        private string GetTempPdfFile()
        {
            return Path.Combine(storage.TempDirectory, Guid.NewGuid() + ".pdf");
        }
        private string GetTempCSVFile()
        {
            return Path.Combine(storage.TempDirectory, Guid.NewGuid() + ".csv");
        }
        public void Dispose()
        {
            dbc?.Dispose();
        }
    }
}
