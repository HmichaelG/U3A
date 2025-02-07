using DevExpress.DataAccess.Json;
using DevExpress.DataAccess.ObjectBinding;
using DevExpress.XtraReports;
using DevExpress.XtraReports.UI;
using DevExpress.XtraRichEdit;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using U3A.BusinessRules;
using U3A.Database;

namespace U3A.UI.Reports
{
    public partial class ParticipantEnrolment : DevExpress.XtraReports.UI.XtraReport
    {
        public ParticipantEnrolment()
        {
            InitializeComponent();
        }

        private void xrRichText1_BeforePrint(object sender, CancelEventArgs e)
        {
            var o = GetCurrentRow();
            if (o == null) { return; }

            using (RichEditDocumentServer docServer = new RichEditDocumentServer())
            {
                docServer.RtfText = xrRichText1.Rtf;
                docServer.Document.DefaultCharacterProperties.FontName = "Times New Roman";
                docServer.Document.DefaultCharacterProperties.FontSize = (float?)10;
                xrRichText1.Rtf = docServer.RtfText;
            }
        }
    }
}
