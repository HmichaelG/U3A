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
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using U3A.Database;
using U3A.Model;

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

public class WorkstationService
{
    private readonly IJSRuntime js;
    private readonly ILocalStorageService localStorage;
    private readonly IDbContextFactory<U3ADbContext> U3AdbFactory;
    private readonly TenantInfoService tenantInfoService;

    public WorkstationService() { } // for Json deserialize
    public WorkstationService(IJSRuntime js,
        ILocalStorageService localStorage,
        TenantInfoService tenantInfoService)
    {
        this.js = js;
        this.localStorage = localStorage;
        this.tenantInfoService = tenantInfoService;
    }

    // size Changed event
    public event EventHandler ScreenSizeChanged;

    public bool UseTopMenu { get; set; } = false;
    public string ID { get; private set; }
    public int SizeMode { get; set; } = 0;
    public ScreenSizes ScreenSize { get; set; } = ScreenSizes.XSmall;
    public bool IsSmallScreen => MenuBehavior == "Small" || (MenuBehavior == "Auto" && (ScreenSize == ScreenSizes.XSmall || ScreenSize == ScreenSizes.Small));
    public bool IsMediumScreen => MenuBehavior == "Medium" || (MenuBehavior == "Auto" && (ScreenSize == ScreenSizes.Medium || ScreenSize == ScreenSizes.Large));
    public bool IsLargeScreen => MenuBehavior == "Large" || (MenuBehavior == "Auto" && ScreenSize == ScreenSizes.XLarge);

    public string Theme { get; set; } = "light";
    public string AccentColor { get; set; } = null;
    public string SidebarImage { get; set; } = "Random Image";
    public string MenuBehavior { get; set; } = "Auto";

    private const string DEFAULT_COLOR = "royalblue";

    public const string WORKSTATION_ID = "WorkstationID";
    public const string WORKSTATION_KEY = "workstation";
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

        var json = await localStorage.GetItemAsStringAsync(WORKSTATION_KEY) ?? string.Empty;
        if (json != string.Empty)
        {
            var workstation = JsonSerializer.Deserialize<WorkstationService>(json);
            UseTopMenu = workstation.UseTopMenu;
            SizeMode = workstation.SizeMode;
            Theme = workstation.Theme;
            AccentColor = workstation.AccentColor;
            SidebarImage = workstation.SidebarImage;
            MenuBehavior = workstation.MenuBehavior;
        }

        if (string.IsNullOrEmpty(AccentColor))
        {
            //Use Website primary color as default
            var tenant = await tenantInfoService.GetTenantInfoAsync();
            if (tenant != null)
            {
                if (string.IsNullOrEmpty(tenant.PrimaryWebsiteColor))
                {
                    AccentColor = DEFAULT_COLOR;
                }
                else
                {
                    AccentColor = tenant.PrimaryWebsiteColor;
                }
            }
            else
            {
                AccentColor = DEFAULT_COLOR;
            }
        }

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

    public async Task SetWorkstationDetail(bool IsPortal = false)
    {
        if (IsPortal)
        {
            // Reload from local storage to avoid overwriting non-portal settings
            var currentValues = await localStorage.GetItemAsStringAsync(WORKSTATION_KEY) ?? string.Empty;
            if (currentValues != string.Empty)
            {
                var workstation = JsonSerializer.Deserialize<WorkstationService>(currentValues);
                UseTopMenu = workstation.UseTopMenu;
                SidebarImage = workstation.SidebarImage;
                MenuBehavior = workstation.MenuBehavior;
            }
        }
        await RefreshCookies();
        var json = JsonSerializer.Serialize<WorkstationService>(this);
        await localStorage.SetItemAsStringAsync(WORKSTATION_KEY, json);
    }

    private async Task RefreshCookies()
    {
        await js.InvokeVoidAsync("cookieInterop.setCookie", WORKSTATION_ID, ID, 999999);
        await js.InvokeVoidAsync("cookieInterop.setCookie", THEME, Theme, 999999);
        await js.InvokeVoidAsync("cookieInterop.setCookie", ACCENT_COLOR, AccentColor, 999999);
    }
}
