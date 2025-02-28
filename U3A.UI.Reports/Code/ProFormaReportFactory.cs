﻿using DevExpress.DataAccess.ObjectBinding;
using DevExpress.XtraReports;
using DevExpress.XtraReports.UI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Logging;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using System;
using System.Collections.Concurrent;
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

namespace U3A.UI.Reports
{
    public class ProFormaReportFactory : IDisposable
    {

        private readonly ILogger log;
        public List<string> PostalReports { get; set; }

        IDbContextFactory<U3ADbContext> U3AdbFactory;
        U3ADbContext? dbc;
        public CustomReportStorageWebExtension ReportStorage { get; set; }

        IEmailService emailSender;
        bool isPreview;
        bool isAzureFunction;
        public string SendEmailAddress { get; set; }
        string sendEmailDisplayName;
        string tenantID;

        //Azure Functions only
        public ProFormaReportFactory(TenantInfo tenant, ILogger logger, bool IsPreview=false)
        {
            log = logger;
            dbc = new U3ADbContext(tenant);
            isAzureFunction = true;
            isPreview = IsPreview;
            tenantID = tenant.Identifier;
            ReportStorage = new CustomReportStorageWebExtension(tenant);
            var settings = dbc.SystemSettings.FirstOrDefault() ?? throw new ArgumentNullException(nameof(SystemSettings));
            if (settings != null)
            {
                SendEmailAddress = settings.SendEmailAddesss;
                sendEmailDisplayName = settings.SendEmailDisplayName;
            }
            emailSender = EmailFactory.GetEmailSender(dbc);
            PostalReports = new List<string>();
        }

        // Web app 
        public ProFormaReportFactory(ILogger logger,
                                        IWebHostEnvironment env,
                                        IDbContextFactory<U3ADbContext> U3AdbFactory,
                                        bool IsPreview)
        {
            log = logger;
            this.U3AdbFactory = U3AdbFactory ?? throw new ArgumentNullException(nameof(U3AdbFactory));
            ReportStorage = new CustomReportStorageWebExtension(env, this.U3AdbFactory);
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
            PostalReports = new List<string>();
        }

        public async Task<string> CreateCashReceiptProForma(Receipt receipt)
        {
            using (var cashReceiptProForma = new CashReceipt())
            {
                try
                {
                    var detail = BusinessRule.GetReceiptDetail(dbc, receipt);
                    var person = await dbc.Person.FindAsync(receipt.PersonID);
                    var list = new List<ReceiptDetail>();
                    list.Add(detail);
                    cashReceiptProForma.DataSource = list;
                    cashReceiptProForma.CreateDocument();
                    string pdfFilename = GetTempPdfFile();
                    cashReceiptProForma.ExportToPdf(pdfFilename);
                    if (!isPreview && !string.IsNullOrWhiteSpace(person.Email))
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
                catch (Exception ex)
                {
                    log.LogError(ex, $"Tenant: {tenantID} Receipt: {receipt.ID}");
                    return "Error";
                }
            }
        }
        public async Task<Dictionary<Guid, string>> CreateEnrolmentProForma(Dictionary<Guid, List<Enrolment>> Enrolments)
        {
            var result = new Dictionary<Guid, string>();
            foreach (var kvp in Enrolments)
            {
                try
                {
                    List<(Guid CourseID, Guid? ClassID)> onFile = new();
                    List<Term> terms = new();
                    var currentTerm = await BusinessRule.CurrentEnrolmentTermAsync(dbc);
                    if (currentTerm == null) { currentTerm = await BusinessRule.CurrentTermAsync(dbc); }
                    if (currentTerm != null)
                    {
                        terms = await dbc.Term.Where(x => x.Year == currentTerm.Year).ToListAsync();
                    }
                    var personsFiles = new List<string>();
                    Person person = null;
                    foreach (var enrolment in kvp.Value.OrderBy(x => x.Course.Name))
                    {
                        person = enrolment.Person;
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
                                    if (dataSource is ObjectDataSource)
                                    {
                                        var ds = (dataSource as ObjectDataSource);
                                        if (ds.Name.ToLower() == "objectdatasource2") { ds.DataSource = terms; }
                                    }
                                }
                                participantEnrolmentProForma.CreateDocument();
                                string pdf = GetTempPdfFile();
                                participantEnrolmentProForma.ExportToPdf(pdf);
                                personsFiles.Add(pdf);
                            }
                        }
                    }
                    var pdfFilename = CreateMergedPDF(personsFiles);
                    if (string.IsNullOrWhiteSpace(pdfFilename)) { continue; }
                    if (!isPreview && !string.IsNullOrWhiteSpace(person.Email))
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
                catch (Exception ex)
                {
                    log.LogError(ex, $"Tenant: {tenantID} Enrolment: {kvp.Key}");
                    result.Add(kvp.Key, "Error");
                }
            }
            return result;
        }

        Stopwatch sw = new();
        public async Task<string> CreateLeaderReports(
                                        bool DoLeaderReport,
                                        bool DoLeaderAttendanceList,
                                        bool DoLeaderClassList,
                                        bool DoLeaderICEList,
                                        bool DoLeaderCSVFile,
                                        bool DoAttendanceAnalysis,
                                        bool DoMemberBadges,
                                        Guid CourseID,
                                        string ReportName,
                                        string CourseName,
                                        Person Leader, Enrolment[] Enrolments)
        {
            var result = string.Empty;
            (var enrolmentDetails,var leaderDetails, var settings, var term) = await GetEnrolmentDetails(Leader, Enrolments);
            var createdFilenames = new List<string>();
            var reportNames = new List<string>();
            if (DoLeaderReport || !(Enrolments.Where(x => !x.IsWaitlisted).Any()))
            {
                var report = "Leader's Report.pdf";
                sw.Start();
                log.LogInformation($"Creating {report} at {sw.Elapsed}");
                try
                {
                    using (var leaderReportProForma = new LeaderReport())
                    {
                        log.LogInformation($"Instantiated {report} at {sw.Elapsed}");
                        var filename = await CreateLeaderReportAsync(leaderReportProForma,settings,term,leaderDetails,enrolmentDetails.AsEnumerable());
                        if (!string.IsNullOrWhiteSpace(filename))
                        {
                            createdFilenames.Add(filename);
                            reportNames.Add(report);
                        }
                        log.LogInformation($"Completed {report} at {sw.Elapsed}");
                    }
                }
                catch (Exception ex)
                {
                    log.LogError(ex, $"Tenant: {tenantID} Leader: {Leader.ID} Report: {report}");
                }
            }
            if (Enrolments.Any(x => !x.IsWaitlisted))
            {
                if (DoLeaderAttendanceList)
                {
                    var report = "Student Attendance Record.pdf";
                    sw.Start();
                    log.LogInformation($"Creating {report} at {sw.Elapsed}");
                    try
                    {
                        using (var leaderAttendanceList = new LeaderAttendanceList())
                        {
                            log.LogInformation($"Instantiated {report} at {sw.Elapsed}");
                            var filename = await CreateLeaderReportAsync(leaderAttendanceList,
                                                    settings, term, leaderDetails, 
                                                    enrolmentDetails.Where(x => !x.EnrolmentIsWaitlisted && !x.EnrolmentIsLeader));
                            if (!string.IsNullOrWhiteSpace(filename))
                            {
                                createdFilenames.Add(filename);
                                reportNames.Add(report);
                            }
                            log.LogInformation($"Completed {report} at {sw.Elapsed}");
                        }
                    }
                    catch (Exception ex)
                    {
                        log.LogError(ex, $"Tenant: {tenantID} Leader: {Leader.ID} Report: {report}");
                    }
                }
                if (DoLeaderClassList)
                {
                    var report = "Class Contact Listing.pdf";
                    sw.Restart();
                    log.LogInformation($"Creating {report} at {sw.Elapsed}");
                    try
                    {
                        using (var leaderClassList = new LeaderClassList())
                        {
                            log.LogInformation($"Instantiated {report} at {sw.Elapsed}");
                            var filename = await CreateLeaderReportAsync(leaderClassList, 
                                                        settings, term, leaderDetails, 
                                                        enrolmentDetails.Where(x => !x.EnrolmentIsWaitlisted));
                            if (!string.IsNullOrWhiteSpace(filename))
                            {
                                createdFilenames.Add(filename);
                                reportNames.Add(report);
                            }
                            log.LogInformation($"Completed {report} at {sw.Elapsed}");
                        }
                    }
                    catch (Exception ex)
                    {
                        log.LogError(ex, $"Tenant: {tenantID} Leader: {Leader.ID} Report: {report}");
                    }
                }
                if (DoLeaderICEList)
                {
                    var report = "Class ICE Listing.pdf";
                    sw.Restart();
                    log.LogInformation($"Creating {report} at {sw.Elapsed}");
                    try
                    {
                        using (var leaderICEList = new LeaderICEList())
                        {
                            log.LogInformation($"Instantiated {report} at {sw.Elapsed}");
                            var filename = await CreateLeaderReportAsync(leaderICEList,
                                                        settings, term, leaderDetails,
                                                        enrolmentDetails.Where(x => !x.EnrolmentIsWaitlisted));
                            if (!string.IsNullOrWhiteSpace(filename))
                            {
                                createdFilenames.Add(filename);
                                reportNames.Add(report);
                                log.LogInformation($"Completed {report} at {sw.Elapsed}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        log.LogError(ex, $"Tenant: {tenantID} Leader: {Leader.ID} Report: {report}");
                    }
                }
                if (DoMemberBadges)
                {
                    var report = "Member Badges.pdf";
                    sw.Restart();
                    log.LogInformation($"Creating {report} at {sw.Elapsed}");
                    try
                    {
                        var fileName = await CreateMemberBadgesReport(Leader,
                            Enrolments.Where(x => !x.IsWaitlisted).ToArray());
                        if (!string.IsNullOrWhiteSpace(fileName))
                        {
                            createdFilenames.Add(fileName);
                            reportNames.Add(report);
                        }
                        log.LogInformation($"Completed {report} at {sw.Elapsed}");
                    }
                    catch (Exception ex)
                    {
                        log.LogError(ex, $"Tenant: {tenantID} Leader: {Leader.ID} Report: {report}");
                    }
                }
                if (DoAttendanceAnalysis)
                {
                    var report = "Attendance Analysis.pdf";
                    sw.Restart();
                    log.LogInformation($"Creating {report} at {sw.Elapsed}");
                    try
                    {
                        using (var AttendanceAnalysis = new AttendanceAnalysis())
                        {
                            log.LogInformation($"Instantiated {report} at {sw.Elapsed}");
                            var filename = CreateAttendanceAnalysisReport(AttendanceAnalysis,
                                                    Leader, CourseID);
                            if (!string.IsNullOrWhiteSpace(filename))
                            {
                                createdFilenames.Add(filename);
                                reportNames.Add("Attendance Analysis.pdf");
                            }
                        }
                        log.LogInformation($"Completed {report} at {sw.Elapsed}");
                    }
                    catch (Exception ex)
                    {
                        log.LogError(ex, $"Tenant: {tenantID} Leader: {Leader.ID} Report: {report}");
                    }
                }
                if (DoLeaderCSVFile && !isPreview)
                {
                    var report = "Class List.csv";
                    try
                    {
                        var filename = CreateCSVFile(Enrolments.Where(x => !x.IsWaitlisted).ToArray());
                        if (!string.IsNullOrWhiteSpace(filename))
                        {
                            createdFilenames.Add(filename);
                            reportNames.Add(report);
                        }
                    }
                    catch (Exception ex)
                    {
                        log.LogError(ex, $"Tenant: {tenantID} Leader: {Leader.ID} Report: {report}");
                    }
                }
            }
            try
            {
                if (createdFilenames.Count > 0)
                {
                    result = await ProcessLeaderReport(Leader, ReportName, CourseName, createdFilenames.ToArray(), reportNames.ToArray());
                }
                else
                {
                    result = "Requested report(s) not available";
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Tenant: {tenantID} Leader: {Leader.ID} Process {ReportName}");
            }
            return result;
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
                                    bool RandomAllocationExecuted = false)
        {
            using (var leaderReportProForma = new LeaderReport())
            {
                (var enrolmentDetails, var leaderDetails, var settings, var term) = await GetEnrolmentDetails(Leader, Enrolments);
                var report = await CreateLeaderReportAsync(leaderReportProForma, settings, term, leaderDetails, enrolmentDetails);
                return await ProcessLeaderReport(Leader,
                                "U3A Leader's Report",
                                CourseName,
                                new string[] { report },
                                new string[] { "Leader Report.pdf" },
                                RandomAllocationExecuted);
            }
        }
        async Task<string> CreateMemberBadgesReport(Person Leader,
                Enrolment[] Enrolments)
        {
            var pdfFilename = string.Empty;
            var report = new MemberBadge();
            var list = new List<Guid>();
            foreach (var enrollment in Enrolments)
            {
                list.Add(enrollment.PersonID);
            }
            var people = await BusinessRule.SelectableFinancialPeopleAsync(dbc);
            var term = await BusinessRule.CurrentEnrolmentTermAsync(dbc);
            var settings = dbc.SystemSettings.OrderBy(x => x.ID).FirstOrDefault();
            if (list.Count > 0)
            {
                people = people.Where(x => list.Contains(x.ID)).ToList();
                report.SetParameters(people, settings, term);
                report.CreateDocument();
                pdfFilename = GetTempPdfFile();
                report.ExportToPdf(pdfFilename);
            }
            return pdfFilename;
        }
        string CreateAttendanceAnalysisReport(AttendanceAnalysis report,
                Person Leader,
                Guid CourseID)
        {
            if (U3AdbFactory != null) report.U3Adbfactory = U3AdbFactory;
            if (dbc != null) report.DbContext = dbc;
            report.Parameters["prmCourseID"].Value = CourseID;
            string pdfFilename = GetTempPdfFile();
            report.ExportToPdf(pdfFilename);
            return pdfFilename;
        }
        async Task<string> CreateLeaderReportAsync(XtraReport report,
                SystemSettings settings,
                Term term,
                IEnumerable<LeaderDetail> leaderDetail,
                IEnumerable<EnrolmentDetail> enrolmentDetails)
        {
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
            report.ExportToPdf(pdfFilename);
            return pdfFilename;
        }

        private async Task<(IEnumerable<EnrolmentDetail>,IEnumerable<LeaderDetail>, SystemSettings,Term)> GetEnrolmentDetails(Person Leader, Enrolment[] Enrolments)
        {
            List<EnrolmentDetail> enrolmentDetails = new();
            List<LeaderDetail> leaderDetails = new();
            var settings = await dbc.SystemSettings.OrderBy(x => x.ID).FirstOrDefaultAsync();
            var term = dbc.Term.Find(Enrolments[0].TermID);
            if (Enrolments.Length == 0) { return (enrolmentDetails,leaderDetails, settings,term); }

            leaderDetails = BusinessRule.GetLeaderDetail(settings, Leader, term);
            enrolmentDetails = new List<EnrolmentDetail>();
            var totalEnrolled = 0.00;
            var totalWaitListed = 0.00;
            bool isMultiCampus = false;
            foreach (var enrolment in Enrolments)
            {
                if (enrolment.Person != null && enrolment.Person.IsMultiCampusVisitor) { isMultiCampus = true; }
                if (enrolment.IsWaitlisted) { totalWaitListed++; } else { totalEnrolled++; }
                enrolmentDetails.AddRange(await BusinessRule.GetEnrolmentDetailAsync(dbc, enrolment));
            }
            if (isMultiCampus)
            {   // we need to recalculate totals
                foreach (var ed in enrolmentDetails)
                {
                    ed.CourseTotalActiveStudents = (int)totalEnrolled;
                    ed.CourseTotalWaitlistedStudents = (int)totalWaitListed;
                    if (ed.CourseMaximumStudents > 0)
                    {
                        ed.CourseParticipationRate = (totalEnrolled + totalWaitListed) / (double)ed.CourseMaximumStudents;
                    }
                }
            }
            return (enrolmentDetails,leaderDetails,settings,term);
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

            if (!isPreview && !string.IsNullOrEmpty(Leader.Email))
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
                if (outputDocument.PageCount > 0)
                {
                    var outputFile = GetTempPdfFile();
                    outputDocument.Save(outputFile);
                    return outputFile;
                }
            }
            log.LogWarning("No PDF files to merge. The requested reports contained no printed pages usually because there were no enrolments.");
            return null;
        }

        private string GetTempPdfFile()
        {
            return Path.Combine(ReportStorage.TempDirectory, Guid.NewGuid() + ".pdf");
        }
        private string GetTempCSVFile()
        {
            return Path.Combine(ReportStorage.TempDirectory, Guid.NewGuid() + ".csv");
        }
        public void Dispose()
        {
            dbc?.Dispose();
        }
    }
}
