using DevExpress.Office.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace U3A.Model
{
    public static class constants
    {
        //The number of days prior to the announcment of results
        //  that random allocation is actually performed.
        public const int RANDOM_ALLOCATION_PREVIEW = 3;

        public const string STD_DATE_FORMAT = "ddd dd-MMM-yyy";
        public const string STD_DATETIME_FORMAT = "ddd dd-MMM-yyy hh:mm tt";
        public const string STD_DATE_MONTH_ONLY_FORMAT = "ddd dd-MMM";
        public const string SHORT_DATE_FORMAT = "dd-MMM-yyy";
        public const int START_OF_TIME = 2020;
        public const string SYSTEM_DOMAIN = "u3admin.org.au";
        public const string NO_SMS = "(No SMS)";

        public static List<string> nameOfRoles = new List<string>() { "Security Administrator",
                                                "System Administrator",
                                                "Course and Class",
                                                "Membership",
                                                "Accounting",
                                                 "Report View",
                                                  "Office"};
        public const string superAdmin = "SuperAdmin@U3A.com.au";

        public static readonly int TenantIdMaxLength = 64;

        public static string TENANT_CONNECTION_STRING;
        public static string TENANT = "";

        public static bool IS_DEVELOPMENT;

#if DEBUG
        public const bool IS_DEBUG = true;
#else
            public const bool IS_DEBUG = false;
#endif
    }

}
