using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace U3A.Model
{
    public class TenantInfo
    {

        private string? id;

        public string? Id
        {
            get
            {
                return id;
            }
            set
            {
                if (value != null)
                {
                    if (value.Length > constants.TenantIdMaxLength)
                    {
                        throw new Exception($"The tenant id cannot exceed {constants.TenantIdMaxLength} characters.");
                    }

                    id = value;
                }
            }
        }

        public string? Identifier { get; set; }

        public string? Name { get; set; }

        public string? ConnectionString { get; set; }
        public string Website { get; set; }
        public string State { get; set; }
        public string? SendGridAPIKey { get; set; }
        public bool UseEmailTestEnviroment { get; set; }
        public string? PostmarkAPIKey { get; set; }
        public string? PostmarkSandboxAPIKey { get; set; }
        public bool UsePostmarkTestEnviroment { get; set; }
        public string? TwilioAccountSID { get; set; }
        public string? TwilioAuthToken { get; set; }
        public string? TwilioPhoneNo { get; set; }
        public bool UseSMSTestEnviroment { get; set; }
        public string? EwayAPIKey { get; set; }
        public string? EwayPassword { get; set; }
        public bool UseEwayTestEnviroment { get; set; }
    }
}
