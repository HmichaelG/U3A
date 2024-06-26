﻿namespace U3A.UI.Reports
{
    partial class MemberBadge
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MemberBadge));
            DevExpress.XtraReports.Parameters.DynamicListLookUpSettings dynamicListLookUpSettings1 = new DevExpress.XtraReports.Parameters.DynamicListLookUpSettings();
            this.objectDataSource1 = new DevExpress.DataAccess.ObjectBinding.ObjectDataSource(this.components);
            this.Detail = new DevExpress.XtraReports.UI.DetailBand();
            this.xrPanel2 = new DevExpress.XtraReports.UI.XRPanel();
            this.xrLine1 = new DevExpress.XtraReports.UI.XRLine();
            this.xrLabel3 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel2 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel1 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrTitle = new DevExpress.XtraReports.UI.XRLabel();
            this.xrPictureBox2 = new DevExpress.XtraReports.UI.XRPictureBox();
            this.xrU3AName = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel6 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrPictureBox1 = new DevExpress.XtraReports.UI.XRPictureBox();
            this.TopMargin = new DevExpress.XtraReports.UI.TopMarginBand();
            this.BottomMargin = new DevExpress.XtraReports.UI.BottomMarginBand();
            this.xrPageInfo1 = new DevExpress.XtraReports.UI.XRPageInfo();
            this.prmStartDate = new DevExpress.XtraReports.Parameters.Parameter();
            this.prmPersonID = new DevExpress.XtraReports.Parameters.Parameter();
            ((System.ComponentModel.ISupportInitialize)(this.objectDataSource1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            // 
            // objectDataSource1
            // 
            this.objectDataSource1.DataSource = typeof(global::U3A.Model.Person);
            this.objectDataSource1.Name = "objectDataSource1";
            // 
            // Detail
            // 
            this.Detail.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrPanel2});
            this.Detail.Dpi = 254F;
            this.Detail.HeightF = 940F;
            this.Detail.HierarchyPrintOptions.Indent = 50.8F;
            this.Detail.KeepTogether = true;
            this.Detail.MultiColumn.ColumnCount = 2;
            this.Detail.MultiColumn.ColumnSpacing = 25F;
            this.Detail.MultiColumn.ColumnWidth = 900F;
            this.Detail.MultiColumn.Layout = DevExpress.XtraPrinting.ColumnLayout.AcrossThenDown;
            this.Detail.MultiColumn.Mode = DevExpress.XtraReports.UI.MultiColumnMode.UseColumnCount;
            this.Detail.Name = "Detail";
            this.Detail.StylePriority.UseBackColor = false;
            this.Detail.StylePriority.UseBorders = false;
            // 
            // xrPanel2
            // 
            this.xrPanel2.BorderColor = System.Drawing.Color.Gray;
            this.xrPanel2.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrPanel2.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLine1,
            this.xrLabel3,
            this.xrLabel2,
            this.xrLabel1,
            this.xrTitle,
            this.xrPictureBox2,
            this.xrU3AName,
            this.xrLabel6,
            this.xrPictureBox1});
            this.xrPanel2.Dpi = 254F;
            this.xrPanel2.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
            this.xrPanel2.Name = "xrPanel2";
            this.xrPanel2.SizeF = new System.Drawing.SizeF(937.4999F, 925.3751F);
            this.xrPanel2.StylePriority.UseBorderColor = false;
            this.xrPanel2.StylePriority.UseBorders = false;
            // 
            // xrLine1
            // 
            this.xrLine1.BorderColor = System.Drawing.Color.Transparent;
            this.xrLine1.BorderWidth = 0F;
            this.xrLine1.Dpi = 254F;
            this.xrLine1.LineStyle = DevExpress.Drawing.DXDashStyle.Dash;
            this.xrLine1.LocationFloat = new DevExpress.Utils.PointFloat(0F, 550F);
            this.xrLine1.Name = "xrLine1";
            this.xrLine1.SizeF = new System.Drawing.SizeF(937.5F, 25F);
            this.xrLine1.StylePriority.UseBorderColor = false;
            this.xrLine1.StylePriority.UseBorderWidth = false;
            // 
            // xrLabel3
            // 
            this.xrLabel3.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel3.CanGrow = false;
            this.xrLabel3.Dpi = 254F;
            this.xrLabel3.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[ICEPhone]")});
            this.xrLabel3.Font = new DevExpress.Drawing.DXFont("Arial", 18F);
            this.xrLabel3.ForeColor = System.Drawing.Color.Black;
            this.xrLabel3.LocationFloat = new DevExpress.Utils.PointFloat(25.00017F, 805.375F);
            this.xrLabel3.Multiline = true;
            this.xrLabel3.Name = "xrLabel3";
            this.xrLabel3.Padding = new DevExpress.XtraPrinting.PaddingInfo(5, 5, 0, 0, 254F);
            this.xrLabel3.SizeF = new System.Drawing.SizeF(887.5F, 98F);
            this.xrLabel3.StylePriority.UseBorders = false;
            this.xrLabel3.StylePriority.UseFont = false;
            this.xrLabel3.StylePriority.UseForeColor = false;
            this.xrLabel3.StylePriority.UseTextAlignment = false;
            this.xrLabel3.Text = "xrLabel6";
            this.xrLabel3.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabel3.TextFitMode = DevExpress.XtraReports.UI.TextFitMode.ShrinkOnly;
            // 
            // xrLabel2
            // 
            this.xrLabel2.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel2.CanGrow = false;
            this.xrLabel2.Dpi = 254F;
            this.xrLabel2.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[ICEContact]")});
            this.xrLabel2.Font = new DevExpress.Drawing.DXFont("Arial", 18F);
            this.xrLabel2.LocationFloat = new DevExpress.Utils.PointFloat(24.99977F, 707.375F);
            this.xrLabel2.Multiline = true;
            this.xrLabel2.Name = "xrLabel2";
            this.xrLabel2.Padding = new DevExpress.XtraPrinting.PaddingInfo(5, 5, 0, 0, 254F);
            this.xrLabel2.SizeF = new System.Drawing.SizeF(887.5001F, 98F);
            this.xrLabel2.StylePriority.UseBorders = false;
            this.xrLabel2.StylePriority.UseFont = false;
            this.xrLabel2.StylePriority.UseTextAlignment = false;
            this.xrLabel2.Text = "xrLabel6";
            this.xrLabel2.TextAlignment = DevExpress.XtraPrinting.TextAlignment.BottomCenter;
            this.xrLabel2.TextFitMode = DevExpress.XtraReports.UI.TextFitMode.ShrinkOnly;
            // 
            // xrLabel1
            // 
            this.xrLabel1.BorderColor = System.Drawing.Color.Red;
            this.xrLabel1.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel1.CanGrow = false;
            this.xrLabel1.Dpi = 254F;
            this.xrLabel1.Font = new DevExpress.Drawing.DXFont("Arial", 18F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel1.ForeColor = System.Drawing.Color.Red;
            this.xrLabel1.LocationFloat = new DevExpress.Utils.PointFloat(24.99977F, 597.3751F);
            this.xrLabel1.Multiline = true;
            this.xrLabel1.Name = "xrLabel1";
            this.xrLabel1.Padding = new DevExpress.XtraPrinting.PaddingInfo(5, 5, 0, 0, 254F);
            this.xrLabel1.SizeF = new System.Drawing.SizeF(887.5001F, 110F);
            this.xrLabel1.StylePriority.UseBorderColor = false;
            this.xrLabel1.StylePriority.UseBorders = false;
            this.xrLabel1.StylePriority.UseFont = false;
            this.xrLabel1.StylePriority.UseForeColor = false;
            this.xrLabel1.StylePriority.UseTextAlignment = false;
            this.xrLabel1.Text = "In Case of Emergency";
            this.xrLabel1.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrTitle
            // 
            this.xrTitle.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrTitle.CanGrow = false;
            this.xrTitle.Dpi = 254F;
            this.xrTitle.Font = new DevExpress.Drawing.DXFont("Arial", 12F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTitle.LocationFloat = new DevExpress.Utils.PointFloat(24.99957F, 297.3526F);
            this.xrTitle.Name = "xrTitle";
            this.xrTitle.Padding = new DevExpress.XtraPrinting.PaddingInfo(5, 5, 0, 0, 254F);
            this.xrTitle.SizeF = new System.Drawing.SizeF(887.5003F, 79.7691F);
            this.xrTitle.StylePriority.UseBorders = false;
            this.xrTitle.StylePriority.UseFont = false;
            this.xrTitle.StylePriority.UseTextAlignment = false;
            this.xrTitle.Text = "xrTitle";
            this.xrTitle.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTitle.TextFitMode = DevExpress.XtraReports.UI.TextFitMode.ShrinkOnly;
            this.xrTitle.WordWrap = false;
            this.xrTitle.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.xrTitle_BeforePrint);
            // 
            // xrPictureBox2
            // 
            this.xrPictureBox2.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrPictureBox2.Dpi = 254F;
            this.xrPictureBox2.ImageAlignment = DevExpress.XtraPrinting.ImageAlignment.MiddleRight;
            this.xrPictureBox2.ImageSource = new DevExpress.XtraPrinting.Drawing.ImageSource("img", resources.GetString("xrPictureBox2.ImageSource"));
            this.xrPictureBox2.LocationFloat = new DevExpress.Utils.PointFloat(491.8127F, 10.58333F);
            this.xrPictureBox2.Name = "xrPictureBox2";
            this.xrPictureBox2.SizeF = new System.Drawing.SizeF(420.6873F, 84.66669F);
            this.xrPictureBox2.StylePriority.UseBorders = false;
            // 
            // xrU3AName
            // 
            this.xrU3AName.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrU3AName.CanGrow = false;
            this.xrU3AName.Dpi = 254F;
            this.xrU3AName.Font = new DevExpress.Drawing.DXFont("Arial", 12F, DevExpress.Drawing.DXFontStyle.Italic);
            this.xrU3AName.LocationFloat = new DevExpress.Utils.PointFloat(24.99957F, 464.4759F);
            this.xrU3AName.Multiline = true;
            this.xrU3AName.Name = "xrU3AName";
            this.xrU3AName.Padding = new DevExpress.XtraPrinting.PaddingInfo(5, 5, 0, 0, 254F);
            this.xrU3AName.SizeF = new System.Drawing.SizeF(887.5003F, 58.42001F);
            this.xrU3AName.StylePriority.UseBorders = false;
            this.xrU3AName.StylePriority.UseFont = false;
            this.xrU3AName.StylePriority.UseTextAlignment = false;
            this.xrU3AName.Text = "xrU3AName";
            this.xrU3AName.TextAlignment = DevExpress.XtraPrinting.TextAlignment.BottomCenter;
            this.xrU3AName.TextFitMode = DevExpress.XtraReports.UI.TextFitMode.ShrinkOnly;
            this.xrU3AName.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.xrU3AName_BeforePrint);
            // 
            // xrLabel6
            // 
            this.xrLabel6.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel6.CanGrow = false;
            this.xrLabel6.Dpi = 254F;
            this.xrLabel6.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[FirstAndLastName]")});
            this.xrLabel6.Font = new DevExpress.Drawing.DXFont("Arial", 24F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel6.LocationFloat = new DevExpress.Utils.PointFloat(24.99957F, 149.1442F);
            this.xrLabel6.Multiline = true;
            this.xrLabel6.Name = "xrLabel6";
            this.xrLabel6.Padding = new DevExpress.XtraPrinting.PaddingInfo(5, 5, 0, 0, 254F);
            this.xrLabel6.SizeF = new System.Drawing.SizeF(887.5003F, 148.2084F);
            this.xrLabel6.StylePriority.UseBorders = false;
            this.xrLabel6.StylePriority.UseFont = false;
            this.xrLabel6.StylePriority.UseTextAlignment = false;
            this.xrLabel6.Text = "xrLabel6";
            this.xrLabel6.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabel6.TextFitMode = DevExpress.XtraReports.UI.TextFitMode.ShrinkOnly;
            // 
            // xrPictureBox1
            // 
            this.xrPictureBox1.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrPictureBox1.Dpi = 254F;
            this.xrPictureBox1.ImageAlignment = DevExpress.XtraPrinting.ImageAlignment.MiddleLeft;
            this.xrPictureBox1.ImageSource = new DevExpress.XtraPrinting.Drawing.ImageSource("img", resources.GetString("xrPictureBox1.ImageSource"));
            this.xrPictureBox1.LocationFloat = new DevExpress.Utils.PointFloat(24.99999F, 10.58333F);
            this.xrPictureBox1.Name = "xrPictureBox1";
            this.xrPictureBox1.SizeF = new System.Drawing.SizeF(431.3125F, 84.66669F);
            this.xrPictureBox1.StylePriority.UseBorders = false;
            // 
            // TopMargin
            // 
            this.TopMargin.Dpi = 254F;
            this.TopMargin.HeightF = 56F;
            this.TopMargin.Name = "TopMargin";
            // 
            // BottomMargin
            // 
            this.BottomMargin.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrPageInfo1});
            this.BottomMargin.Dpi = 254F;
            this.BottomMargin.HeightF = 103.5625F;
            this.BottomMargin.Name = "BottomMargin";
            // 
            // xrPageInfo1
            // 
            this.xrPageInfo1.Dpi = 254F;
            this.xrPageInfo1.LocationFloat = new DevExpress.Utils.PointFloat(1420.354F, 25.00009F);
            this.xrPageInfo1.Name = "xrPageInfo1";
            this.xrPageInfo1.Padding = new DevExpress.XtraPrinting.PaddingInfo(5, 5, 0, 0, 254F);
            this.xrPageInfo1.SizeF = new System.Drawing.SizeF(383.6462F, 58.41999F);
            this.xrPageInfo1.StylePriority.UseTextAlignment = false;
            this.xrPageInfo1.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrPageInfo1.TextFormatString = "Page {0} of {1}";
            // 
            // prmStartDate
            // 
            this.prmStartDate.AllowNull = true;
            this.prmStartDate.Description = "Members joined since (None = all)";
            this.prmStartDate.Name = "prmStartDate";
            this.prmStartDate.Type = typeof(global::System.DateTime);
            // 
            // prmPersonID
            // 
            this.prmPersonID.AllowNull = true;
            this.prmPersonID.Description = "For Members (None = all)";
            this.prmPersonID.MultiValue = true;
            this.prmPersonID.Name = "prmPersonID";
            this.prmPersonID.Type = typeof(global::System.Guid);
            dynamicListLookUpSettings1.DataMember = null;
            dynamicListLookUpSettings1.DataSource = this.objectDataSource1;
            dynamicListLookUpSettings1.DisplayMember = "FullNameAlpha";
            dynamicListLookUpSettings1.FilterString = null;
            dynamicListLookUpSettings1.SortMember = "FullNameAlphaKey";
            dynamicListLookUpSettings1.ValueMember = "ID";
            this.prmPersonID.ValueSourceSettings = dynamicListLookUpSettings1;
            // 
            // MemberBadge
            // 
            this.Bands.AddRange(new DevExpress.XtraReports.UI.Band[] {
            this.Detail,
            this.TopMargin,
            this.BottomMargin});
            this.BorderColor = System.Drawing.Color.Transparent;
            this.ComponentStorage.AddRange(new System.ComponentModel.IComponent[] {
            this.objectDataSource1});
            this.DataSource = this.objectDataSource1;
            this.Dpi = 254F;
            this.Font = new DevExpress.Drawing.DXFont("Arial", 9.75F);
            this.Margins = new DevExpress.Drawing.DXMargins(100F, 100F, 56F, 103.5625F);
            this.PageHeight = 2970;
            this.PageWidth = 2100;
            this.PaperKind = DevExpress.Drawing.Printing.DXPaperKind.A4;
            this.ParameterPanelLayoutItems.AddRange(new DevExpress.XtraReports.Parameters.ParameterPanelLayoutItem[] {
            new DevExpress.XtraReports.Parameters.ParameterLayoutItem(this.prmPersonID, DevExpress.XtraReports.Parameters.Orientation.Vertical),
            new DevExpress.XtraReports.Parameters.ParameterLayoutItem(this.prmStartDate, DevExpress.XtraReports.Parameters.Orientation.Vertical)});
            this.Parameters.AddRange(new DevExpress.XtraReports.Parameters.Parameter[] {
            this.prmPersonID,
            this.prmStartDate});
            this.ReportPrintOptions.DetailCountOnEmptyDataSource = 12;
            this.ReportUnit = DevExpress.XtraReports.UI.ReportUnit.TenthsOfAMillimeter;
            this.SnapGridSize = 25F;
            this.Version = "23.2";
            this.ParametersRequestBeforeShow += new System.EventHandler<DevExpress.XtraReports.Parameters.ParametersRequestEventArgs>(this.MemberBadge_ParametersRequestBeforeShow);
            this.DataSourceDemanded += new System.EventHandler<System.EventArgs>(this.MaillingLabels_DataSourceDemanded);
            this.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.MemberBadge_BeforePrint);
            ((System.ComponentModel.ISupportInitialize)(this.objectDataSource1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();

        }

        #endregion

        private DevExpress.XtraReports.UI.DetailBand Detail;
        private DevExpress.XtraReports.UI.TopMarginBand TopMargin;
        private DevExpress.XtraReports.UI.BottomMarginBand BottomMargin;
        private DevExpress.DataAccess.ObjectBinding.ObjectDataSource objectDataSource1;
        private DevExpress.XtraReports.UI.XRPanel xrPanel2;
        private DevExpress.XtraReports.UI.XRPictureBox xrPictureBox1;
        private DevExpress.XtraReports.UI.XRLabel xrLabel6;
        private DevExpress.XtraReports.UI.XRLabel xrLabel1;
        private DevExpress.XtraReports.UI.XRLabel xrLabel3;
        private DevExpress.XtraReports.UI.XRLabel xrLabel2;
        private DevExpress.XtraReports.UI.XRLabel xrU3AName;
        private DevExpress.XtraReports.UI.XRPageInfo xrPageInfo1;
        private DevExpress.XtraReports.UI.XRPictureBox xrPictureBox2;
        private DevExpress.XtraReports.UI.XRLabel xrTitle;
        private DevExpress.XtraReports.Parameters.Parameter prmStartDate;
        private DevExpress.XtraReports.Parameters.Parameter prmPersonID;
        private DevExpress.XtraReports.UI.XRLine xrLine1;
    }
}
