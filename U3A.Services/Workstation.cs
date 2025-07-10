using AsyncAwaitBestPractices;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using U3A.Database;

namespace U3A.Services;

public enum ScreenSizes
{
    XSmall,
    Small,
    Medium,
    Large,
    XLarge,
    Unknown
}
public class WorkStation
{
    // size Changed event
    public event EventHandler ScreenSizeChanged;

    public bool UseTopMenu { get; set; }
    public string ID { get; private set; }
    public int SizeMode { get; set; }
    public ScreenSizes ScreenSize { get; set; }
    public bool IsSmallScreen => MenuBehavior == "Small" || (MenuBehavior == "Auto" && (ScreenSize == ScreenSizes.XSmall || ScreenSize == ScreenSizes.Small));
    public bool IsMediumScreen => MenuBehavior == "Medium" || (MenuBehavior == "Auto" && (ScreenSize == ScreenSizes.Medium || ScreenSize == ScreenSizes.Large));
    public bool IsLargeScreen => MenuBehavior == "Large" || (MenuBehavior == "Auto" && ScreenSize == ScreenSizes.XLarge);

    public string theme;
    public string AccentColor;
    public string SidebarImage;
    public string MenuBehavior;

    const string WORKSTATION_ID = "WorkstationID";
    const string USE_TOP_MENU_KEY = "use-topmenu";
    const string SIZE_MODE = "size-mode";
    const string THEME = "theme";
    const string ACCENT_COLOR = "accent-color";
    const string SIDEBAR_IMAGE = "sidebar-image";
    const string MENU_BEHAVIOR = "menu-behavior";

    public async Task<bool> GetWorkstationDetail(ILocalStorageService localStorage, IDbContextFactory<U3ADbContext> U3AdbFactory)
    {
        bool forceReload = false;
        //workstation ID
        var id = await localStorage.GetItemAsync<string>(WORKSTATION_ID);
        if (id == null)
        {
            id = Guid.NewGuid().ToString();
            await localStorage.SetItemAsync(WORKSTATION_ID, id);
        }
        ID = id;
        // Use top menu
        UseTopMenu = false;
        if (await localStorage.ContainKeyAsync(USE_TOP_MENU_KEY))
        {
            UseTopMenu = await localStorage.GetItemAsync<bool>(USE_TOP_MENU_KEY);
        }

        // size mode
        SizeMode = 0;
        if (await localStorage.ContainKeyAsync(SIZE_MODE))
        {
            SizeMode = await localStorage.GetItemAsync<int>(SIZE_MODE);
        }

        // theme
        theme = "light";
        if (await localStorage.ContainKeyAsync(THEME))
        {
            theme = await localStorage.GetItemAsync<string>(THEME);
        }
        if (theme != "light" && theme != "dark")
        {
            theme = "light";
        }

        // accent color
        AccentColor = string.Empty;
        if (await localStorage.ContainKeyAsync(ACCENT_COLOR))
        {
            // Attempt to get the accent color from local storage
            AccentColor = await localStorage.GetItemAsync<string>(ACCENT_COLOR) ?? string.Empty;
        }
        if (string.IsNullOrEmpty(AccentColor))
        {
            //Use Website primary color as default
            using (var db = U3AdbFactory.CreateDbContext())
            {
                var tenant = db.TenantInfo;
                if (tenant != null && !string.IsNullOrEmpty(tenant.PrimaryWebsiteColor))
                {
                    if (string.IsNullOrEmpty(AccentColor))
                    {
                        AccentColor = tenant.PrimaryWebsiteColor;
                        await localStorage.SetItemAsync<String>(ACCENT_COLOR, AccentColor);
                        forceReload = true; // Force reload to apply the new accent color
                    }
                }
            }
        }

        // sidebar image
        SidebarImage = "Random Image";
        if (await localStorage.ContainKeyAsync(SIDEBAR_IMAGE))
        {
            SidebarImage = await localStorage.GetItemAsync<string>(SIDEBAR_IMAGE);
        }

        // menu behavior
        MenuBehavior = "Auto";
        if (await localStorage.ContainKeyAsync(MENU_BEHAVIOR))
        {
            MenuBehavior = await localStorage.GetItemAsync<string>(MENU_BEHAVIOR);
        }

        return forceReload;
    }

    public async Task SetScreenSizeAsync(ScreenSizes size)
    {
        ScreenSizes currentSize = ScreenSize;
        if (ScreenSize != size)
        {
            ScreenSize = size;
            ScreenSizeChanged?.Invoke(this, EventArgs.Empty);
            Log.Information($"W/S Screen size changed from {currentSize} to: {size}");
            currentSize = size;
        }
    }

    public async Task SetWorkstationDetail(ILocalStorageService localStorage)
    {
        // Menu behavior
        await localStorage.SetItemAsync<String>(MENU_BEHAVIOR, MenuBehavior);
        // Use top menu
        await localStorage.SetItemAsync<bool>(USE_TOP_MENU_KEY, UseTopMenu);
        // size mode
        await localStorage.SetItemAsync<int>(SIZE_MODE, SizeMode);
        // theme
        await localStorage.SetItemAsync<String>(THEME, theme);
        // accent color
        if (!string.IsNullOrEmpty(AccentColor))
        {
            await localStorage.SetItemAsync<String>(ACCENT_COLOR, AccentColor);
        }
        // sidebar image
        await localStorage.SetItemAsync<String>(SIDEBAR_IMAGE, SidebarImage);
    }
}
