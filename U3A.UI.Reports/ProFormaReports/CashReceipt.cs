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

namespace U3A.UI.Reports
{
    public partial class CashReceipt : DevExpress.XtraReports.UI.XtraReport
    {
        public CashReceipt()
        {
            InitializeComponent();
        }

    }
}
