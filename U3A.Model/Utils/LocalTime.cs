using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace U3A.Model
{
    public class LocalTime
    {
        private IJSRuntime jsRuntime;
        private TimeSpan? _utcOfffset;

        public TimeSpan UtcOffset
        {
            get
            {
                return (_utcOfffset == null) ? TimeSpan.Zero : _utcOfffset.Value;
            }
            set
            {
                _utcOfffset = value;
            }
        }
        public LocalTime(IJSRuntime js)
        {
            jsRuntime = js;
        }
        public LocalTime(TimeSpan UTCOffset)
        {
            this.UtcOffset = UTCOffset;
        }

        public async Task<TimeSpan> GetTimezoneOffsetAsync()
        {
            if (_utcOfffset == null)
            {
                try
                {
                    int timeDiffer = await jsRuntime.InvokeAsync<int>("eval", "(function(){try { return new Date().getTimezoneOffset(); } catch(e) {} return 0;}())");
                    _utcOfffset = TimeSpan.FromMinutes(-timeDiffer);
                }
                catch { _utcOfffset = TimeSpan.Zero; }
            }
            return _utcOfffset.Value;
        }
        public async Task<DateTime> GetLocalTimeAsync()
        {
            if (_utcOfffset == null) { _ = await GetTimezoneOffsetAsync(); }
            // Converting to local time using UTC and local time minute difference.
            return DateTimeOffset.UtcNow.ToOffset(_utcOfffset.Value).DateTime;
        }
        public async Task<DateTime> GetLocalDateAsync()
        {
            if (_utcOfffset == null) { _ = await GetTimezoneOffsetAsync(); }
            // Converting to local time using UTC and local time minute difference.
            return DateTimeOffset.UtcNow.ToOffset(_utcOfffset.Value).Date;
        }
        public async Task<DateTime> GetLocalTimeAsync(DateTime UTCTime)
        {
            if (_utcOfffset == null) { _ = await GetTimezoneOffsetAsync(); }
            // Converting to local time using UTC and local time minute difference.
            return UTCTime.Add(_utcOfffset.Value);
        }
    }
}
