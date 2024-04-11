using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using U3A.Database;
using U3A.Services;

namespace U3A.UI.Reports
{
    public interface IXtraReportWithDbContext
    {
        public U3ADbContext DbContext { get; set; }
    }
    public interface IXtraReportWithDbContextAndTenantDbContext
    {
        public U3ADbContext DbContext { get; set; }
        public TenantDbContext TenantDbContext { get; set; }
        public TenantInfoService TenantService { get; set; }
    }
}
