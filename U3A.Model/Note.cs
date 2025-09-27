using DevExpress.XtraRichEdit.Import.OpenXml;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace U3A.Model;

public class Note : BaseEntity
{
    public Guid Id { get; set; }
    public Guid PersonID { get; set; }
    [Required] public Person Person { get; set; }
    [Required] public string Content { get; set; } = string.Empty;
    [Required] public DateOnly Expires { get; set; } = new DateOnly(DateTime.UtcNow.Year,12,31);
}
