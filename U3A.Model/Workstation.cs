using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace U3A.Model;

public class Workstation
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public string ID { get; set; }
    public bool UseTopMenu { get; set; } = false;
    public int SizeMode { get; set; } = 0;
    public ScreenSizes ScreenSize { get; set; } = ScreenSizes.XSmall;
    public string theme { get; set; } = "light";
    public string AccentColor { get; set; } = string.Empty;
    public string SidebarImage { get; set; } = "Random Image";
    public string MenuBehavior { get; set; } = "Auto";
    
    [Timestamp]
    public byte[] Version { get; set; }

}

public enum ScreenSizes
{
    XSmall,
    Small,
    Medium,
    Large,
    XLarge,
    Unknown
}
