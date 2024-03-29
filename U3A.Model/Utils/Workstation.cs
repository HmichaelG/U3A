﻿using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using AsyncAwaitBestPractices;
using Microsoft.AspNetCore.DataProtection.KeyManagement;

namespace U3A.Model
{
    public static class Workstation
    {
        public static bool UseTopMenu { get; private set; }
        public static string ID { get; private set; }

        const string WORKSTATION_ID = "WorkstationID";
        const string USE_TOP_MENU_KEY = "use-topmenu";

        public static async Task SetWorkstationDetail(IJSRuntime js, ILocalStorageService localStorage)
        {
            //workstation ID
            var id = await localStorage.GetItemAsync<string>(WORKSTATION_ID);
            if (id == null)
            {
                id = Guid.NewGuid().ToString();
                localStorage.SetItemAsync(WORKSTATION_ID, id).SafeFireAndForget();
            }
            ID = id;
            // Use top menu
            UseTopMenu = false;
            if (await localStorage.ContainKeyAsync(USE_TOP_MENU_KEY))
            {
                UseTopMenu = await localStorage.GetItemAsync<bool>(USE_TOP_MENU_KEY);
            }
        }
    }
}
