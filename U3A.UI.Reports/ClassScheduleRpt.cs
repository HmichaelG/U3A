using DevExpress.Data.Helpers;
using DevExpress.DataAccess.ObjectBinding;
using DevExpress.ReportServer.ServiceModel.DataContracts;
using DevExpress.XtraPrinting.BarCode;
using DevExpress.XtraReports.UI;
using Microsoft.AspNetCore.Components;
using PdfSharpCore.Drawing.BarCodes;
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using U3A.BusinessRules;
using U3A.Database;
using U3A.Model;

namespace U3A.UI.Reports
{
    public partial class ClassScheduleRpt : DevExpress.XtraReports.UI.XtraReport, IXtraReportWithDbContext, IXtraReportWithNavManager
    {
        public ClassScheduleRpt()
        {
            InitializeComponent();
        }

        public U3ADbContext DbContext { get; set; }
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
            if (term != null)
            {
                DataSource = BusinessRule.GetClassDetails(DbContext, term, settings);
            }
            prmTermSummary.Value = term?.TermSummary;
            lblWatermark.Text = (string)prmWatermark.Value;
            lblTitleWatermark.Text = (string)prmWatermark.Value;
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
            if (course.CourseParticipationTypeID == (int)ParticipationType.SameParticipantsInAllClasses && course.Classes.Count > 1 ) {
                string text = string.Empty;
                foreach (var thisClass in course.Classes.OrderBy(x => x.StartDate))
                {
                    text = $"{text}{thisClass.ClassDetail}{Environment.NewLine}";
                }
                if (text != string.Empty) tableCellClassDetail.Text  = text;
            }
        }
    }


}

