using DevExpress.XtraPrinting;
using Newtonsoft.Json.Converters;

namespace U3A.Model
{
    public class EmailOutboundOverviewStats
    {
        public DateTime StartDateUTC { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDateUTC { get; set; }
        public DateTime EndDate { get; set; }
        public string Period { get; set; }
        public int Sent { get; set; }
        public int Bounced { get; set; }
        public double BounceRate { get; set; }
        public int SpamComplaints { get; set; }
        public double SpamComplaintsRate { get; set; }
        public int Opens { get; set; }
        public int UniqueOpens { get; set; }

    }

    public class EmailBounce
    {
        public long ID { get; set; }

        public string Details { get; set; }

        public string Email { get; set; }
        public Person Person { get; set; }

        public DateTime BouncedAt { get; set; }

        public bool DumpAvailable { get; set; }

        public bool Inactive { get; set; }

        public bool CanActivate { get; set; }

        public Guid MessageID { get; set; }

        public string Description { get; set; }

        public string Tag { get; set; }

        public string Subject { get; set; }

        public string From { get; set; }

        public int ServerID { get; set; }

        public string Name { get; set; }
    }

    public class EmailSuppression
    {
        public string Stream { get; set; }
        public string Email { get; set; }
        public string Reason { get; set; }
        public DateTime CreatedAt { get; set; }
        public Person Person { get; set; }

    }

    public class EmailMessage
    {
        public string MessageID { get; set; }
        public DateTime ReceivedAt { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string Subject { get; set; }
        public int Attachments { get; set; }
        public List<string> Recipients { get; set; } = new();
        public string Status { get; set; }

    }

    public class EmailMessageEvent
    {
        public string Type { get; set; }
        public DateTime ReceivedAt { get; set; }

    }
}
