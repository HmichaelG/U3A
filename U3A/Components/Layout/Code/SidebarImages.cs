namespace U3A;

public class SidebarImages : List<SidebarImage>
{
    public SidebarImages()
    {
        Add(new SidebarImage { MenuName = "Monochrome Flower", Filename = "flower-light.svg" });
        Add(new SidebarImage { MenuName = "Red Flower", Filename = "flower-color.svg" });
        Add(new SidebarImage { MenuName = "Pink Flower", Filename = "flower-pink.svg" });
        Add(new SidebarImage { MenuName = "Mandala", Filename = "mandala.svg" });
        Add(new SidebarImage { MenuName = "Elephant", Filename = "elephant.svg" });
    }
}

public class SidebarImage
{
    public string MenuName { get; set; }
    public string Filename { get; set; }
    public string CssClass { get; set; } = "sidebar-image";

}
