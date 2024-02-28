using DevExpress.DataAccess.Json;
using DevExpress.DataAccess.ObjectBinding;
using DevExpress.XtraReports;
using DevExpress.XtraReports.UI;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using U3A.BusinessRules;
using U3A.Database;
using U3A.Model;

namespace U3A.UI.Reports
{
    public partial class LeaderReport : DevExpress.XtraReports.UI.XtraReport
    {
        public LeaderReport()
        {
            InitializeComponent();
            prmFromDate.Value = TimezoneAdjustment.GetLocalTime().Date.AddDays(-7);
        }

    }
}
