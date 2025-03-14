using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using U3A.Model;

namespace U3A.Services
{
    public partial class DocumentServer
    {

        public async Task<int> SendQueuedEmailToMultipleRecipientsAsync(DocumentTemplate documentTemplate,
                                        List<ExportData> exportData,
                                        bool OverrideCommunicationPreference)
        {
            DocumentText bodyText = GetBodyText(documentTemplate);

            List<string> toAddressList, toDisplayNameList;
            GetAddressList(documentTemplate, exportData, OverrideCommunicationPreference,
                                                out toAddressList, out toDisplayNameList);

            List<string>? filenames;
            List<Byte[]>? PDFFileContents;
            GetAttachments(documentTemplate, out filenames, out PDFFileContents);

            // Send It!
            var response = await IEmailSender.SendEmailToMultipleRecipientsAsync(
                                        EmailType.Broadcast,
                                        documentTemplate.FromEmailAddress,
                                        documentTemplate.FromDisplayName,
                                        toAddressList,
                                        toDisplayNameList,
                                        documentTemplate.Subject,
                                        bodyText.HtmlText,
                                        bodyText.PlainText,
                                        PDFFileContents, filenames);
            return toAddressList.Count();
        }

        public async Task<int> SendQueuedEmailToSingleRecipientAsync(DocumentTemplate documentTemplate,
                        List<ExportData> ExportData,
                        bool OverrideCommunicationPreference)
        {

            List<string> toAddressList, toDisplayNameList;
            GetAddressList(documentTemplate, ExportData, OverrideCommunicationPreference,
                                                out toAddressList, out toDisplayNameList);

            List<string>? filenames;
            List<Byte[]>? PDFFileContents;
            GetAttachments(documentTemplate, out filenames, out PDFFileContents);

            var docsSent = 0;
            foreach (var mergeItem in FilterPreference(documentTemplate,
                                            ExportData,
                                            OverrideCommunicationPreference))
            {
                DocumentText bodyText = MergeDocument(documentTemplate, mergeItem);
                var response = await IEmailSender.SendEmailAsync(EmailType.Broadcast,
                                            documentTemplate.FromEmailAddress,
                                            documentTemplate.FromDisplayName,
                                            mergeItem.P_Email,
                                            mergeItem.P_FullName,
                                            documentTemplate.Subject,
                                            bodyText.HtmlText,
                                            bodyText.PlainText,
                                            PDFFileContents, filenames);
                docsSent++;
            }
            return docsSent;
        }

        private void GetAttachments(DocumentTemplate documentTemplate,
                        out List<string>? filenames,
                        out List<Byte[]> PDFFileContents)
        {
            // Get Attachments
            PDFFileContents = new List<Byte[]>();
            filenames = new List<string>();
            if (documentTemplate.Attachments != null && documentTemplate.Attachments.Count() > 0)
            {
                var files = documentTemplate.Attachments;
                var pdfContent = documentTemplate.AttachmentBytes;
                if (files != null)
                {
                    // The PDF content
                    foreach (var bytes in pdfContent)
                    {
                        PDFFileContents.Add(bytes);
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

    }
}
