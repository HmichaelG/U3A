using DevExpress.XtraRichEdit.Forms;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Text.Json.Serialization;
using U3A.Model;

namespace U3A.Model
{
    [Index(nameof(LastName), nameof(FirstName), nameof(Email))]
    public class MultiCampusPerson : BaseEntity
    {
        [Key]
        public Guid ID { get; set; }

        public string TenantIdentifier { get; set; }

        public string? Title { get; set; }

        public string? PostNominals { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }

        public int Postcode { get; set; }
        public string? Email { get; set; }

        public string? HomePhone { get; set; }

        public string? Mobile { get; set; }

        public bool SMSOptOut { get; set; }

        public string ICEContact { get; set; }
        public string ICEPhone { get; set; }

        public bool VaxCertificateViewed { get; set; }

        public string Communication { get; set; } = "Email";

    }

}
