using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using U3A.Database;

namespace U3A.UI.Reports
{
    public interface IXtraReportWithNavManager
    {
        public NavigationManager NavManager { get; set; }

    }
}
