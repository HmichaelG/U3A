using DevExpress.Drawing;
using DevExpress.Pdf;
using DevExpress.XtraPrinting;
using DevExpress.XtraRichEdit;
using DevExpress.XtraRichEdit.API.Native;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using U3A.BusinessRules;
using U3A.Database;
using U3A.Model;
using U3A.Services;
using U3A.Services.APIClient;

namespace U3A.Services;

public partial class DocumentServer : IDisposable
{

    public int SuccessTransmissionAttempts { get; set; } = 0;
    public int FailureTransmissionAttempts { get; set; } = 0;
    //public int BatchCount { get; set; } = 0;
    //public int BatchSuccessCount { get; set; } = 0;
    //public int BatchFailureCount { get; set; } = 0;
    public int BulkRecipientCount { get; set; } = 0;
    DateTime sendTime;
    public TimeSpan ElapsedTime { get; set; }

    public bool IsOvernightBatch { get; set; }
    string tenant;

    void ClearCounters()
    {
        SuccessTransmissionAttempts = 0;
        FailureTransmissionAttempts = 0;
        BulkRecipientCount = 0;
        sendTime = DateTime.UtcNow;
    }

    Dictionary<string, int> Result;

    public string GetHTMLResult()
    {
        return $"{SuccessTransmissionAttempts} email items have been queued for processing.";
    }

    struct DocumentText
    {
        public string HtmlText;
        public string PlainText;
    }

    RichEditDocumentServer server;
    RichEditDocumentServer resultServer;
    IEmailService IEmailSender;
    ISMSSender ISMSSender;
    U3ADbContext dbc;
    private bool disposedValue;

    public DocumentServer(U3ADbContext dbc)
    {
        this.dbc = dbc;
        Result = new Dictionary<string, int>();
        tenant = dbc.TenantInfo.Identifier;
        DevExpress.Utils.AzureCompatibility.Enable = true;
        ISMSSender = SMSFactory.GetSMSSender(dbc);
        server = new RichEditDocumentServer();
        resultServer = new RichEditDocumentServer();
        resultServer.Document.CalculateDocumentVariable += Document_CalculateDocumentVariable;
        IEmailSender = EmailFactory.GetEmailSender(dbc);
        server.Document.EmbedFonts = true;
        resultServer.Document.EmbedFonts = true;
    }

    private static void Report_QueryNotFoundFont(object sender, NotFoundFontEventArgs e)
    {
        if (e.RequestedFont == "Sankofa Display")
        {
            string font = Environment.CurrentDirectory + "\\Data\\SankofaDisplay-Regular.ttf";
            e.FontFileData = File.ReadAllBytes(font);
        }
    }
    public async Task ConvertDocx2Html(DocumentTemplate DocumentTemplate)
    {
        await Task.Run(() =>
        {
            server.DocmBytes = DocumentTemplate.Content;
            DocumentTemplate.HTML = server.HtmlText;
        });
    }
    public async Task MailMerge(U3ADbContext dbc,
                        DocumentTemplate documentTemplate,
                        List<Guid> PersonIDsToExport,
                        bool OverrideCommunicationPreference,
                        int DelayedStart)
    {
        IsOvernightBatch = false;
        if (documentTemplate == null) { return; }
        ClearCounters();
        var s = new Stopwatch();
        s.Start();
        if (documentTemplate.DocumentType.IsSMS)
        {
            await SendSMSAsync(documentTemplate, PersonIDsToExport, OverrideCommunicationPreference);
        }
        else if (HasMergeCodes(documentTemplate.Content) || PersonIDsToExport.Count() == 1)
        {
            await SendEmailToSingleRecipientAsync(documentTemplate, PersonIDsToExport, OverrideCommunicationPreference, DelayedStart);
        }
        else
        {
            await SendEmailToMultipleRecipients(documentTemplate, PersonIDsToExport, OverrideCommunicationPreference, DelayedStart);
        }
        ;
        s.Stop();
        ElapsedTime = s.Elapsed;
    }

    public static bool HasMergeCodesAndAttachments(DocumentTemplate DocumentTemplate)
    {
        if (DocumentTemplate?.Content == null) { return false; }
        if (!DocumentTemplate.Attachments.Any()) return false;
        var server = new RichEditDocumentServer();
        server.RtfText = System.Text.Encoding.UTF8.GetString(DocumentTemplate.Content);
        return (server.Document.Fields.Count - server.Document.Hyperlinks.Count) > 0;
    }
    private bool HasMergeCodes(byte[] content)
    {
        return MergeCodeCount(content) > 0;
    }
    public int MergeCodeCount(byte[] content)
    {
        var server = new RichEditDocumentServer();
        server.RtfText = System.Text.Encoding.UTF8.GetString(content);
        return (server.Document.Fields.Count - server.Document.Hyperlinks.Count);
    }

    async Task SendEmailToMultipleRecipients(DocumentTemplate documentTemplate,
                                            List<Guid> personIDsToExport,
                                            bool OverrideCommunicationPreference,
                                            int DelayedStart)
    {
        if (!documentTemplate.DocumentType.IsEmail) return;
        IsOvernightBatch = true;
        var sendToMultipleRecipients = true;
        await CreateDocumentQueuedItem(documentTemplate,
                                        personIDsToExport,
                                        OverrideCommunicationPreference,
                                        sendToMultipleRecipients,
                                        DelayedStart);
        SuccessTransmissionAttempts = personIDsToExport.Count;
    }

    async Task SendEmailToSingleRecipientAsync(DocumentTemplate documentTemplate,
                List<Guid> personIDsToExport,
                bool OverrideCommunicationPreference,
                int delayedStart)
    {
        IsOvernightBatch = true;
        var sendToMultipleRecipients = false;
        await CreateDocumentQueuedItem(documentTemplate, personIDsToExport, OverrideCommunicationPreference, sendToMultipleRecipients, delayedStart);
        SuccessTransmissionAttempts = personIDsToExport.Count;
    }

    private void GetAttachments(DocumentTemplate documentTemplate,
                            out List<string>? filenames,
                            out List<string> pathnames)
    {
        // Get Attachments
        pathnames = new List<string>();
        filenames = new List<string>();
        if (documentTemplate.Attachments != null && documentTemplate.Attachments.Count() > 0)
        {
            var files = documentTemplate.Attachments;
            if (files != null)
            {
                // The full path
                foreach (var filename in files)
                {
                    pathnames.Add(filename);
                }
                //The displayed name
                var replaceText = $"{tenant}_";
                string thisFile;
                foreach (var filename in files)
                {
                    thisFile = Path.GetFileName(filename);
                    filenames.Add(thisFile.Replace(replaceText, ""));
                }
            }
        }
    }

    private void GetAddressList(DocumentTemplate documentTemplate,
                    List<ExportData> exportData,
                    bool OverrideCommunicationPreference,
                    out List<string> toAddressList,
                    out List<string> toDisplayNameList)
    {

        // Get the email address list
        toAddressList = new List<string>();
        toDisplayNameList = new List<string>();
        foreach (var mergeItem in FilterPreference(documentTemplate,
                                        exportData,
                                        OverrideCommunicationPreference))
        {
            toAddressList.Add(mergeItem.P_Email);
            toDisplayNameList.Add(mergeItem.P_FullName);
            if (!string.IsNullOrWhiteSpace(mergeItem.P_CarerSendToEmail))
            {
                toAddressList.Add(mergeItem.P_CarerSendToEmail);
                toDisplayNameList.Add(mergeItem.P_CarerName);
            }
        }
    }

    private DocumentText GetBodyText(DocumentTemplate documentTemplate)
    {
        // Get the body text (HTML & plain)
        server.RtfText = System.Text.Encoding.UTF8.GetString(documentTemplate.Content);
        var bodyText = new DocumentText() { HtmlText = server.HtmlText, PlainText = server.Text };
        return bodyText;
    }
    public string GetHTMLText(Byte[] content)
    {
        string result = string.Empty;
        if (content != null)
        {
            server.RtfText = System.Text.Encoding.UTF8.GetString(content);
            result = server.HtmlText;
        }
        return result;
    }

    private async Task CreateDocumentQueuedItem(DocumentTemplate documentTemplate,
                                            List<Guid> personIDsToExport,
                                            bool OverrideCommunicationPreference,
                                            bool SendToMultipleRecipients,
                                            int DelayedStart)
    {
        documentTemplate.AttachmentBytes = new List<byte[]>(); //to be sure
        var jsonDocumentTemplate = JsonSerializer.Serialize(documentTemplate);
        var jsonExportData = JsonSerializer.Serialize(personIDsToExport);
        var docQueue = new DocumentQueue()
        {
            ID = Guid.NewGuid(),
            DocumentTemplateJSON = jsonDocumentTemplate,
            MemberIdToExport = jsonExportData,
            ExportDataJSON = string.Empty,
            SendToMultipleRecipients = SendToMultipleRecipients,
            Status = DocumentQueueStatus.ReadyToSend,
            OverrideCommunicationPreference = OverrideCommunicationPreference,
            DelayInHours = DelayedStart,
            Result = string.Empty
        };
        foreach (var pathname in documentTemplate.Attachments)
        {
            var docQueueAttachment = new DocumentQueueAttachment()
            {
                DocumentQueue = docQueue,
                Attachment = File.ReadAllBytes(pathname)
            };
            docQueue.DocumentAttachments.Add(docQueueAttachment);
        }
        await dbc.AddAsync(docQueue);
        await dbc.SaveChangesAsync();
        if (DelayedStart <= 0)
        {
            using (var client = new APIClient.APIClient())
            {
                try
                {
                    await client.DoProcessQueuedDocuments(dbc.TenantInfo.Identifier, docQueue.ID);
                }
                catch (HttpRequestException e)
                {
                    docQueue.Result = e.Message;
                }
                catch (Exception ex)
                {
                    docQueue.Status = DocumentQueueStatus.Error;
                    docQueue.Result = ex.Message;
                    await dbc.SaveChangesAsync();
                }
                finally
                {
                    await dbc.SaveChangesAsync();
                }
            }
        }
    }

    async Task SendSMSAsync(DocumentTemplate documentTemplate,
                                        List<Guid> PersonIDsToSend,
                                        bool OverrideCommunicationPreference)
    {
        if (!documentTemplate.DocumentType.IsSMS) return;
        List<ExportData> exportData = await BusinessRule.GetExportDataAsync(dbc, PersonIDsToSend);
        foreach (var mergeItem in FilterPreference(documentTemplate,
                                        exportData,
                                        OverrideCommunicationPreference))
        {
            DocumentText bodyText = MergeDocument(documentTemplate, mergeItem);
            var response = ISMSSender.SendSMS(documentTemplate.FromDisplayName,
                                    mergeItem.P_Mobile,
                                    bodyText.PlainText);
            if (ISMSSender.WasTransmissionSuccessful) { SuccessTransmissionAttempts += 1; } else { FailureTransmissionAttempts += 1; }
            if (!Result.ContainsKey(response)) { Result.Add(response, 0); }
            Result[response] += 1;
        }
    }

    private DocumentText MergeDocument(DocumentTemplate documentTemplate, ExportData mergeItem)
    {
        DocumentText result = new DocumentText();
        server.RtfText = System.Text.Encoding.UTF8.GetString(documentTemplate.Content);
        server.Options.MailMerge.ViewMergedData = true;
        server.Options.Export.Html.EmbedImages = true;
        var l = new List<ExportData>() { mergeItem };
        server.Options.MailMerge.DataSource = l;
        server.MailMerge(resultServer.Document);
        result.PlainText = resultServer.Text;
        if (!string.IsNullOrWhiteSpace(documentTemplate.EmailPreheader))
        {
            result.HtmlText = resultServer.HtmlText.Replace("<body>", ReadPreheaderTemplate().Replace("{Preheader}", documentTemplate.EmailPreheader));
        }
        else
            result.HtmlText = resultServer.HtmlText;
        return result;
    }

    string ReadPreheaderTemplate()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"U3A.Services.Email.Preheader.html";

        using (Stream stream = assembly.GetManifestResourceStream(resourceName))
        using (StreamReader reader = new StreamReader(stream))
        {
            return reader.ReadToEnd();
        }
    }
    public byte[] MergeDocumentAsPdf(DocumentTemplate DocumentTemplate, List<ExportData> mergeData,
                                    bool OverrideCommunicationPreference)
    {
        byte[] result = default;
        server.RtfText = System.Text.Encoding.UTF8.GetString(DocumentTemplate.Content);
        server.Options.Export.Html.EmbedImages = true;
        resultServer.Options.MailMerge.ViewMergedData = true;
        server.Options.MailMerge.DataSource = FilterPreference(DocumentTemplate, mergeData, OverrideCommunicationPreference);
        var opt = resultServer.CreateMailMergeOptions();
        opt.MergeMode = MergeMode.NewSection;
        opt.FirstRecordIndex = 0;
        server.MailMerge(opt, resultServer.Document);
        using (var ms = new MemoryStream())
        {
            var opts = new PdfExportOptions() { ImageQuality = PdfJpegImageQuality.Highest };
            resultServer.ExportToPdf(ms);
            ms.Position = 0;
            result = ms.ToArray();
        }
        return result;
    }

    private List<ExportData> FilterPreference(DocumentTemplate documentTemplate,
                        List<ExportData> mergeData, bool OverrideCommunicationPreference)
    {
        var result = new List<ExportData>();
        if (OverrideCommunicationPreference)
        {
            if (documentTemplate.DocumentType.IsEmail)
            {
                result = mergeData.Where(x => !string.IsNullOrWhiteSpace(x.P_Email)).ToList();
            }
            else
            {
                if (documentTemplate.DocumentType.IsSMS)
                {
                    result = mergeData.Where(x => !string.IsNullOrWhiteSpace(x.P_Mobile)).ToList();
                }
                else result = mergeData.ToList();
            }
        }
        else
        {
            if (documentTemplate.DocumentType.IsEmail)
            {
                result = mergeData.Where(x => x.P_Communication == "Email" &&
                                                !string.IsNullOrWhiteSpace(x.P_Email)).ToList();
            }
            if (documentTemplate.DocumentType.IsPostal)
            {
                result = mergeData.Where(x => x.P_Communication == "Post").ToList();
            }
            if (documentTemplate.DocumentType.IsSMS)
            {
                result = mergeData.Where(x => (x.P_SMSOptOut == false) && string.IsNullOrWhiteSpace(x.P_Mobile) == false).ToList();
            }
        }
        return result;
    }
    public void Document_CalculateDocumentVariable(object sender, CalculateDocumentVariableEventArgs e)
    {
        var dstServer = new RichEditDocumentServer();
        Guid ParticipantID;
        if (e.Arguments.Count > 0 && Guid.TryParse(e.Arguments[0].Value, out ParticipantID))
        {
            var subDocTemplate = dbc.DocumentTemplate
                                    .Include(x => x.DocumentType)
                                    .Where(x => x.Name == e.VariableName).FirstOrDefault();
            if (subDocTemplate != null)
            {
                if (subDocTemplate.DocumentType.Name == "EnrolmentSubdoc")
                {
                    GetEnrolmentDetails(subDocTemplate, dstServer, ParticipantID);
                }
            }
            else { dstServer.Text = $"***** [{e.VariableName}] not found in document templates *****"; }
        }
        else
        {
            dstServer.Text = $"***** Participant key is not first argument to DOCVARIABLE in [{e.VariableName}] *****";
        }
        e.Value = dstServer;
        e.Handled = true;
    }

    public void GetEnrolmentDetails(DocumentTemplate subDocTemplate,
                RichEditDocumentServer dstServer, Guid ParticipantID)
    {
        var mergeItems = BusinessRule.GetEnrolmentExportDataByPerson(dbc, ParticipantID).ToList();
        if (mergeItems.Count > 0)
        {
            var src = new RichEditDocumentServer();
            src.RtfText = System.Text.Encoding.UTF8.GetString(subDocTemplate.Content);
            src.Options.MailMerge.KeepLastParagraph = false;
            src.Options.MailMerge.ViewMergedData = true;
            src.Options.Export.Html.EmbedImages = true;
            src.Options.MailMerge.DataSource = mergeItems;
            src.MailMerge(dstServer.Document);
        }
        else dstServer.Text = "*** You have no enrolments in the current Term ***";
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
                server.Dispose();
                resultServer.Dispose();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~DocumentServer()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

public class DocumentSentEventArgs //: EventArgs
{
    public int DocumentsSent { get; set; }
}

