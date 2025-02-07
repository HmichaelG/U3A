using DevExpress.Data.Helpers;
using DevExpress.DataAccess.ObjectBinding;
using DevExpress.ReportServer.ServiceModel.DataContracts;
using DevExpress.XtraPrinting.BarCode;
using DevExpress.XtraReports.UI;
using DevExpress.XtraRichEdit;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using U3A.BusinessRules;
using U3A.Database;
using U3A.Model;
using U3A.Services;

namespace U3A.UI.Reports
{
    public partial class ClassScheduleRpt
        : DevExpress.XtraReports.UI.XtraReport,
            IXtraReportWithDbContextAndTenantDbContext,
            IXtraReportWithNavManager
    {
        public ClassScheduleRpt()
        {
            InitializeComponent();
        }

        public U3ADbContext DbContext { get; set; }
        public TenantDbContext TenantDbContext { get; set; }
        public TenantInfoService TenantService { get; set; }
        public NavigationManager NavManager { get; set; }

        private void ClassSchedule_ParametersRequestBeforeShow(object sender, DevExpress.XtraReports.Parameters.ParametersRequestEventArgs e)
        {
            var terms = BusinessRule.SelectableTerms(DbContext);
            objectDataSource2.DataSource = terms;
            var term = BusinessRule.CurrentEnrolmentTerm(DbContext);
            if (term == null)
            {
                term = BusinessRule.CurrentTerm(DbContext);
                if (DateTime.Today > term.EndDate) { term = BusinessRule.NextTerm(DbContext, DateTime.Today); }
            }
            prmTerm.Value = term?.ID;
        }

        private void ClassSchedule_ParametersRequestSubmit(object sender, DevExpress.XtraReports.Parameters.ParametersRequestEventArgs e)
        {
            CreateReport();
        }

        SystemSettings settings;
        public void CreateReport()
        {
            Guid id = (Guid)prmTerm.Value;
            settings = DbContext.SystemSettings.FirstOrDefault();
            prmU3AGroup.Value = settings.U3AGroup;
            prmABN.Value = settings.ABN;
            var term = DbContext.Term.Find(id);
            List<Class> classes = new();
            if (term != null)
            {
                classes = BusinessRule.GetClassDetails(DbContext, term, settings, ExludeOffScheduleActivities: true);
            }
            foreach (var c in classes)
            {
                // Force featured classes into its own group
                if (c.Course.IsFeaturedCourse)
                {
                    c.OnDayID = -2;
                }
                // Force unscheduled class into its own group
                if ((OccurrenceType)c.OccurrenceID == OccurrenceType.Unscheduled)
                {
                    c.OnDayID = 99;
                }

            }
            DataSource = classes;
            prmTermSummary.Value = term?.TermSummary;
            lblWatermark.Text = (string)prmWatermark.Value;
            lblTitleWatermark.Text = (string)prmWatermark.Value;
            xrMessage.Text = @"<p><b>This report consists of many pages</b></p>
                                <p>Scroll down or use the page view controls on the menu to view report content</p>";
            // Sort
            OnDayGroupHeader.GroupFields.Clear();
            Detail.SortFields.Clear();
            var xrTableOfContentsLevel1 = this.xrTableOfContents1.Levels[0];
            if ((string)prmSort.Value == "DayHeld")
            {
                OnDayGroupHeader.Visible = true;
                GroupFooter1.Visible = true;
                OnDayGroupHeader.GroupFields.AddRange(new DevExpress.XtraReports.UI.GroupField[] {
                    new DevExpress.XtraReports.UI.GroupField("OnDayID", DevExpress.XtraReports.UI.XRColumnSortOrder.Ascending)});
                Detail.SortFields.AddRange(new DevExpress.XtraReports.UI.GroupField[] {
                    new DevExpress.XtraReports.UI.GroupField("Course.Name", DevExpress.XtraReports.UI.XRColumnSortOrder.Ascending)});
                this.tableCellCourseName.BookmarkParent = this.tableCellOnDay;
                xrTableOfContentsLevel1.Font = new DevExpress.Drawing.DXFont("Times New Roman", 9.75F, DevExpress.Drawing.DXFontStyle.Bold);
            }
            else
            {
                OnDayGroupHeader.Visible = false;
                GroupFooter1.Visible = false;
                Detail.SortFields.AddRange(new DevExpress.XtraReports.UI.GroupField[] {
                    new DevExpress.XtraReports.UI.GroupField("Course.Name", DevExpress.XtraReports.UI.XRColumnSortOrder.Ascending)});
                this.tableCellCourseName.BookmarkParent = default;
                xrTableOfContentsLevel1.Font = new DevExpress.Drawing.DXFont("Times New Roman", 9.75F, DevExpress.Drawing.DXFontStyle.Regular);
            }
        }

        private void rowLeaderDetail_BeforePrint(object sender, CancelEventArgs e)
        {
            var visible = true;
            if ((int)prmIntendedUse.Value == 0 && !settings.ShowLeaderOnPublicSchedule) { visible = false; }
            rowLeaderDetail.Visible = visible;
        }

        private void xrBarCode1_BeforePrint(object sender, CancelEventArgs e)
        {
            var Class = GetCurrentRow() as Class;
            if (Class == null) { return; }
            xrBarCode1.ShowText = false;
            xrBarCode1.Text = $"{NavManager.BaseUri}EnrolClass={Class.ID}";
            xrBarCode1.NavigateUrl = xrBarCode1.Text;
            // Adjust the properties specific to the barcode type.
            ((QRCodeGenerator)xrBarCode1.Symbology).CompactionMode = QRCodeCompactionMode.Byte;
            ((QRCodeGenerator)xrBarCode1.Symbology).ErrorCorrectionLevel = QRCodeErrorCorrectionLevel.Q;
            ((QRCodeGenerator)xrBarCode1.Symbology).Version = QRCodeVersion.AutoVersion;

        }

        private void tableCellClassDetail_BeforePrint(object sender, CancelEventArgs e)
        {
            var Class = GetCurrentRow() as Class;
            if (Class == null) { return; }
            var course = Class.Course;
            if (course.ClassSummaries.Count > 1)
            {
                string text = string.Empty;
                foreach (var thisClass in course.ClassSummaries)
                {
                    text = $"{text}{thisClass}{Environment.NewLine}";
                }
                if (text != string.Empty) tableCellClassDetail.Text = text;
            }
        }

        private void tableCellOnDay_BeforePrint(object sender, CancelEventArgs e)
        {
            Class c = (Class)GetCurrentRow();
            if (c == null) { return; }
            if ((OccurrenceType)c.OccurrenceID == OccurrenceType.Unscheduled)
            {
                tableCellOnDay.Text = "Unscheduled (Varies)";
            }
            else if (c.Course.IsFeaturedCourse)
            {
                tableCellOnDay.Text = "Featured Course";
            }
            else
            {
                tableCellOnDay.Text = c.OnDay.Day;
            }
            tableCellOnDay.Bookmark = tableCellOnDay.Text;
        }

        private void xrVenueRow_BeforePrint(object sender, CancelEventArgs e)
        {
            Class c = (Class)GetCurrentRow();
            if (c == null) { return; }
            xrVenueRow.Visible = (c.Course.ClassSummaries.Count <= 1) ? true : false;
        }

        private void xrRichText1_BeforePrint(object sender, CancelEventArgs e)
        {
            Class c = (Class)GetCurrentRow();
            if (c == null) { return; }
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

