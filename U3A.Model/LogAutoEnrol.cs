using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace U3A.Model
{
    public class LogAutoEnrol
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public string MessageTemplate { get; set; }
        public string Level { get; set; }
        public DateTime TimeStamp { get; set; }
        public string? Exception { get; set; }
        public string? Properties { get; set; }
        public string? LogEvent { get; set; }
        public string? Tenant { get; set; }
        public string? User { get; set; }

        [NotMapped]
        public string? Instance
        {
            get
            {
                string? result = null;
                var doc = new XmlDocument();
                doc.LoadXml(Properties);
                foreach (XmlNode n in doc.SelectNodes("/properties/property"))
                {
                    var key = n.Attributes.GetNamedItem("key");
                    if (key != null && key.Value == "AutoEnrolParticipants")
                    {
                        XmlNode c = n.FirstChild;
                        if (c != null)
                        {
                            result = c.InnerText;
                            Console.WriteLine($"{key.Value} {c.InnerText}");
                        }
                    }
                }

                return result;
            }
        }
    }
}
