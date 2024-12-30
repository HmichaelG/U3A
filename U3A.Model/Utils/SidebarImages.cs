namespace U3A.Model;

public class SidebarImages : List<SidebarImage>
{
    public SidebarImages()
    {
        Add(new SidebarImage { MenuName = "Monochrome Flower", Filename = "flower-light.svg" });
        Add(new SidebarImage { MenuName = "Red Flower", Filename = "flower-color.svg" });
        Add(new SidebarImage { MenuName = "Pink Flower", Filename = "flower-pink.svg" });
        Add(new SidebarImage { MenuName = "Mandala", Filename = "mandala.svg" });
        Add(new SidebarImage { MenuName = "Elephant", Filename = "elephant.svg" });
        Add(new SidebarImage { MenuName = "Sacred Energy", Filename = "energy.svg" });
    }
}

public class SidebarImageMenuOptions : SidebarImages
{
    public SidebarImageMenuOptions()
    {
        // sort the base list by MenuName
        Sort((a, b) => a.MenuName.CompareTo(b.MenuName));

        Insert(0,new SidebarImage { MenuName = "Random Image", Filename = "" });
        Add(new SidebarImage { MenuName = "No Image", Filename = "" });
    }
}

public class SidebarImage
{
    public string MenuName { get; set; }
    public string Filename { get; set; }
    public string CssClass { get; set; } = "sidebar-image";

}
