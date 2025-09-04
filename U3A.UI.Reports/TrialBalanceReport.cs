using DevExpress.XtraReports.UI;
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using U3A.Model;

namespace U3A.UI.Reports
{
    public partial class TrialBalanceReport : DevExpress.XtraReports.UI.XtraReport
    {
        public TrialBalanceReport()
        {
            InitializeComponent();
        }

        private void TrialBalanceReport_ParametersRequestSubmit(object sender, DevExpress.XtraReports.Parameters.ParametersRequestEventArgs e)
        {
            xrCourse.CanGrow = !(bool)prmSummaryTextOnly.Value;
        }
    }
}
