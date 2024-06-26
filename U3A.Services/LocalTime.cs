﻿using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace U3A.Services
{
    public class LocalTime
    {
        private IJSRuntime jsRuntime;
        private TimeSpan? userTime;

        public LocalTime(IJSRuntime js)
        {
            jsRuntime = js;
        }

        public async Task<TimeSpan> GetTimezoneOffsetAsync()
        {
            if (userTime == null)
            {
                int timeDiffer = await jsRuntime.InvokeAsync<int>("eval", "(function(){try { return new Date().getTimezoneOffset(); } catch(e) {} return 0;}())");
                userTime = TimeSpan.FromMinutes(-timeDiffer);
            }
            return userTime.Value;
        }
        public async Task<DateTime> GetLocalTimeAsync()
        {
            if (userTime == null) { _= await GetTimezoneOffsetAsync(); }
            // Converting to local time using UTC and local time minute difference.
            return DateTimeOffset.UtcNow.ToOffset(userTime.Value).DateTime;
        }
        public async Task<DateTime> GetLocalDateAsync()
        {
            if (userTime == null) { _ = await GetTimezoneOffsetAsync(); }
            // Converting to local time using UTC and local time minute difference.
            return DateTimeOffset.UtcNow.ToOffset(userTime.Value).Date;
        }
        public async Task<DateTime> GetLocalTimeAsync(DateTime UTCTime)
        {
            if (userTime == null) { _ = await GetTimezoneOffsetAsync(); }
            // Converting to local time using UTC and local time minute difference.
            return UTCTime.Add(userTime.Value);
        }
    }
}
