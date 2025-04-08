using AsyncAwaitBestPractices;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.JSInterop;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace U3A.Model
{
    public class WorkStation
    {
        public bool UseTopMenu { get; set; }
        public string ID { get; private set; }
        public int SizeMode { get; set; }

        public string Theme;
        public string SidebarImage;

        const string WORKSTATION_ID = "WorkstationID";
        const string USE_TOP_MENU_KEY = "use-topmenu";
        const string SIZE_MODE = "size-mode";
        const string THEME = "theme";
        const string SIDEBAR_IMAGE = "sidebar-image";

        public async Task GetWorkstationDetail(ILocalStorageService localStorage)
        {
            //workstation ID
            var id = await localStorage.GetItemAsync<string>(WORKSTATION_ID);
            if (id == null)
            {
                id = Guid.NewGuid().ToString();
                localStorage.SetItemAsync(WORKSTATION_ID, id)
                    .SafeFireAndForget(onException: ex => Log.Error("Error setting WORKSTATION_ID", ex));
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
            Theme = "blazing-berry";
            if (await localStorage.ContainKeyAsync(THEME))
            {
                Theme = await localStorage.GetItemAsync<string>(THEME);
            }
            // sidebar image
            SidebarImage = "Random Image";
            if (await localStorage.ContainKeyAsync(SIDEBAR_IMAGE))
            {
                SidebarImage = await localStorage.GetItemAsync<string>(SIDEBAR_IMAGE);
            }

        }
        public async Task SetWorkstationDetail(ILocalStorageService localStorage)
        {
            // Use top menu
            localStorage.SetItemAsync<bool>(USE_TOP_MENU_KEY, UseTopMenu)
                    .SafeFireAndForget(onException: ex => Log.Error("Error setting WORKSTATION_ID", ex));
            // size mode
            localStorage.SetItemAsync<int>(SIZE_MODE, SizeMode)
                    .SafeFireAndForget(onException: ex => Log.Error("Error setting SIZE_MODE", ex));
            // theme
            localStorage.SetItemAsync<String>(THEME, Theme)
                    .SafeFireAndForget(onException: ex => Log.Error("Error setting THEME", ex));
            // sidebar image
            localStorage.SetItemAsync<String>(SIDEBAR_IMAGE, SidebarImage)
                    .SafeFireAndForget(onException: ex => Log.Error("Error setting SIDEBAR_IMAGE", ex));
        }
    }
}
