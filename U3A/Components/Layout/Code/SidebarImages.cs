namespace U3A;

public class SidebarImages : List<SidebarImage>
{
    public SidebarImages()
    {
        Add(new SidebarImage { MenuName = "Monochrome Flower", Filename = "flower-light.svg" });
        Add(new SidebarImage { MenuName = "Colored Flower", Filename = "flower-color.svg" });
    }
}

public class SidebarImage
{
    public string MenuName { get; set; }
    public string Filename { get; set; }
    public string CssClass { get; set; } = "sidebar-image";

}
