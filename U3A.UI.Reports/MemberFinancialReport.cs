using DevExpress.XtraReports.UI;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Drawing;
using U3A.BusinessRules;
using U3A.Database;
using U3A.Model;
using U3A.Services;

namespace U3A.UI.Reports
{
    public partial class MemberFinancialReport 
        : DevExpress.XtraReports.UI.XtraReport
    {
        public MemberFinancialReport()
        {
            InitializeComponent();
        }

        private void MemberFinancialReport_ParametersRequestSubmit(object sender, DevExpress.XtraReports.Parameters.ParametersRequestEventArgs e)
        {
            if ((bool)prmShowCourseFeesOnly.Value)
            {
                FilterString = "[Course] != ''";
            }
            xrCourse.CanGrow = !(bool)prmSummaryTextOnly.Value;
            xrDescription.CanGrow = !(bool)prmSummaryTextOnly.Value;
        }
    }
}
