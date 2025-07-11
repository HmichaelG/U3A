using AsyncAwaitBestPractices;
using Blazored.LocalStorage;
using DevExpress.XtraRichEdit.Layout.Engine;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using U3A.Database;
using U3A.Model;

namespace U3A.Services;

public class WorkstationService(IJSRuntime js,
                ILocalStorageService localStorage,
                IDbContextFactory<U3ADbContext> U3AdbFactory)
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

    public string theme { get; set; }
    public string AccentColor { get; set; }
    public string SidebarImage { get; set; }
    public string MenuBehavior { get; set; }

    private const string DEFAULT_COLOR = "royalblue";

    public const string WORKSTATION_ID = "WorkstationID";
    public const string THEME = "theme";
    public const string ACCENT_COLOR = "accentColor";

    public async Task GetWorkstationDetail()
    {
        //workstation ID
        {
            var id = await localStorage.GetItemAsync<string>(WORKSTATION_ID);
            if (id == null)
            {
                id = Guid.NewGuid().ToString();
                await localStorage.SetItemAsync(WORKSTATION_ID, id);
            }
            ID = id;
        }

        using var dbc = await U3AdbFactory.CreateDbContextAsync();
        var workstation = await dbc.Workstation.FindAsync(ID);
        if (workstation == null)
        {
            workstation = new() { ID = ID };
            await dbc.AddAsync(workstation);
            await dbc.SaveChangesAsync();
        }
        // Use top menu
        UseTopMenu = workstation.UseTopMenu;

        // size mode
        SizeMode = workstation.SizeMode;

        // theme
        theme = workstation.theme;

        // accent color
        AccentColor = workstation.AccentColor;
        if (string.IsNullOrEmpty(AccentColor))
        {
            //Use Website primary color as default
            var tenant = dbc.TenantInfo;
            if (string.IsNullOrEmpty(tenant.PrimaryWebsiteColor))
            {
                AccentColor = DEFAULT_COLOR;
            }
            else
            {
                AccentColor = tenant.PrimaryWebsiteColor;
            }
        }

        // Sidebar image
        SidebarImage = workstation.SidebarImage;

        // menu behavior
        MenuBehavior = workstation.MenuBehavior;

        // Force refresh of cookies
        await RefreshCookies();
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

    public async Task SetWorkstationDetail()
    {
        await RefreshCookies();

        using var dbc = await U3AdbFactory.CreateDbContextAsync();
        var workstation = await dbc.Workstation.FindAsync(ID);
        // Menu behavior
        workstation.MenuBehavior = MenuBehavior;
        // Use top menu
        workstation.UseTopMenu = UseTopMenu;
        // size mode
        workstation.SizeMode = SizeMode;
        // theme
        workstation.theme = theme;
        // accent color
        if (string.IsNullOrEmpty(AccentColor)) { AccentColor = DEFAULT_COLOR; }
        workstation.AccentColor = AccentColor;
        // sidebar image
        workstation.SidebarImage = SidebarImage;
        await dbc.SaveChangesAsync();
    }

    private async Task RefreshCookies()
    {
        await js.InvokeVoidAsync("cookieInterop.setCookie", WORKSTATION_ID, ID, 999999);
        await js.InvokeVoidAsync("cookieInterop.setCookie", THEME, theme, 999999);
        await js.InvokeVoidAsync("cookieInterop.setCookie", ACCENT_COLOR, AccentColor, 999999);

    }
}
