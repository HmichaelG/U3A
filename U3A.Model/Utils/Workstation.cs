using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using AsyncAwaitBestPractices;

namespace U3A.Model
{
    public static class Workstation
    {
        public static string ID { get; private set; }

        const string WORKSTATION_ID = "WorkstationID";
        public static async Task SetWorkstationID(IJSRuntime js, ILocalStorageService localStorage)
        {
           var id = await localStorage.GetItemAsync<string>(WORKSTATION_ID);
            if (id == null)
            {
                id = Guid.NewGuid().ToString();
                localStorage.SetItemAsync(WORKSTATION_ID, id).SafeFireAndForget();
            }
            ID = id;
        }
    }
}
