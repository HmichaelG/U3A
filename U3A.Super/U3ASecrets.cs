namespace U3A.Super
{
    public class U3ASecrets
    {
        public U3ASecrets(IConfiguration config)
        {
            SQL_RESOURCE_GROUP = config.GetValue<string>("U3A_SQL_RESOURCE_GROUP")!;
            DNS_RESOURCE_GROUP = config.GetValue<string>("U3A_DNS_RESOURCE_GROUP")!;
            DNS_NAME = config.GetValue<string>("U3A_DNS_NAME")!;
            DNS_CNAME = config.GetValue<string>("U3A_DNS_CNAME")!;
            WEBAPP_RESOURCE_GROUP = config.GetValue<string>("U3A_WEBAPP_RESOURCE_GROUP")!;
            WEBAPP_SERVICE_PLAN = config.GetValue<string>("U3A_WEBAPP_SERVICE_PLAN")!;
            WEBAPP_NAME = config.GetValue<string>("U3A_WEBAPP_NAME")!;
            LOCATION = config.GetValue<string>("U3A_LOCATION")!;
            POSTMARK_ADMIN_KEY = config.GetValue<string>("PostmarkAdminAPIkey")!;
        }
        public string SQL_RESOURCE_GROUP { get; set; }
        public string DNS_RESOURCE_GROUP { get; set; }
        public string DNS_NAME { get; set; }
        public string DNS_CNAME { get; set; }
        public string WEBAPP_RESOURCE_GROUP { get; set; }
        public string WEBAPP_SERVICE_PLAN { get; set; }
        public string WEBAPP_NAME { get; set; }
        public string LOCATION { get; set; }
        public string POSTMARK_ADMIN_KEY { get; set; }

        public string[] ILLEGAL_SUBDOMAINS = { "super" };
    }
}
