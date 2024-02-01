using DevExpress.XtraRichEdit.Layout;
using Eway.Rapid.Abstractions.Response;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blazored.LocalStorage;

namespace U3A.Services
{
    public static class Window
    {

        public static async Task<string> GetHeight(IJSRuntime js, int Offset)
        {
            var dimension = await GetWindowSize(js);
            return $"{dimension.Height - Offset}px";
        }
        public static async Task<string> GetHeight(IJSRuntime js, double OffsetPercent)
        {
            var dimension = await GetWindowSize(js);
            return $"{(int)(dimension.Height * OffsetPercent)}px";
        }
        public static async Task<string> GetWidth(IJSRuntime js, int Offset)
        {
            var dimension = await GetWindowSize(js);
            return $"{dimension.Width - Offset}px";
        }
        public static async Task<string> GetWidth(IJSRuntime js, double OffsetPercent)
        {
            var dimension = await GetWindowSize(js);
            return $"{(int)(dimension.Width * OffsetPercent)}px";
        }
        public static async Task<string> FitToRemainingHeight(IJSRuntime js, string ElementID, string currentHeight)
        {
            return await FitToRemainingHeight(js, ElementID, currentHeight, 0.03);
        }

        public static async Task<string> FitToRemainingHeight(IJSRuntime js, string ElementID, string currentHeight, double Offset)
        {
            string result = currentHeight;
            var size = await Window.GetWindowSize(js);
            var bounds = await Window.GetElementBounds(js, ElementID);
            double remainingHeight = 0;
            if (bounds != null)
            {
                if (bounds.Top > 0)
                {
                    if (Offset < 1)
                    {
                        //offset < 1, treat as a percent.
                        remainingHeight = size.Height - bounds.Top - ((size.Height - bounds.Top) * Offset);
                    }
                    else
                    {
                        //offset >= 1, treat as pixels
                        remainingHeight = size.Height - bounds.Top - Offset;
                    }
                    result = $"{(int)remainingHeight}px";
                }
            }
            return result;
        }

        public static async Task<WindowSize> GetWindowSize(IJSRuntime js)
        {
            return await js.InvokeAsync<WindowSize>("getWindowDimensions");
        }
        public static async Task<WindowBounds> GetElementBounds(IJSRuntime js, string DOMelement)
        {
            return await js.InvokeAsync<WindowBounds>("getWindowBounds", DOMelement);
        }



    }
    public class WindowSize
    {
        public int Width { get; set; }
        public int Height { get; set; }
    }
    public class WindowBounds
    {
        public double Top { get; set; }
        public double Left { get; set; }
        public double Bottom { get; set; }
        public double Right { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
    }

}
