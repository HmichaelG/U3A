using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using DevExpress.Blazor;
using Ganss.Xss;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Twilio.Rest.Trunking.V1;

namespace U3A.Services;

public static class HtmlHelpers
{

    const int MAX_IMAGE_SIZE = 60; // in KB
    public static string SanitizeHtml(string Markup)
    {
        var sanitizer = new HtmlSanitizer();
        sanitizer.AllowedCssProperties.Remove("font-family");
        sanitizer.AllowedCssProperties.Remove("font-size");
        sanitizer.AllowedCssProperties.Remove("color");
        sanitizer.AllowedCssProperties.Remove("background-color");
        return sanitizer.SanitizeDocument(Markup, "");
    }

    public static string PrettyPrint(string html)
    {
        string result = string.Empty;
        if (string.IsNullOrWhiteSpace(html)) return result;
        var document = ParseHtml(html);
        if (document is null) return result;
        using (var writer = new StringWriter())
        {
            var pf = new PrettyMarkupFormatter();
            document.ToHtml(writer, pf);
            result = writer.ToString();
        }
        return result;
    }

    public static (bool isValid, string errorText) ValidateImages(string Html)
    {
        (bool isValid, string errorText) result = (true, string.Empty);
        if (Html is null) return result;
        var document = ParseHtml(Html);
        if (document is null) return result;
        var images = document.Images;
        foreach (var image in images)
        {
            //if source is embedded image, ensure it is base64 encoded & less than 100KB
            if (image.Source == null) continue;
            var imageLength = image.Source.Length * 0.754;
            if (image.Source is not null && image.Source.StartsWith("data:image/") && imageLength > MAX_IMAGE_SIZE * 1024)
            {
                result.errorText = $"Your image is {(imageLength / 1024).ToString("n2")}KB in size. Please use an image smaller than {MAX_IMAGE_SIZE}KB.";
                result.isValid = false;
                return result;
            }
        }
        return result;
    }

    public static (string WithImages, string WithImagesDarkMode, string WithoutImages) AdjustAndMinifyHtml(string Html)
    {
        (string WithImages, string WithImagesDarkMode, string WithoutImages) result = (string.Empty, string.Empty, string.Empty);
        if (string.IsNullOrWhiteSpace(Html)) return result;

        // First remove empty elements and minify the structure
        var minified = RemoveEmptyElements(Html);
        result.WithImages = minified;

        // Sanitize and remove images for the 'WithoutImages' variant
        var sanitizer = new HtmlSanitizer();
        sanitizer.AllowedTags.Remove("img");
        using var documentWithoutImages = ParseHtml(sanitizer.SanitizeDocument(minified, ""));
        result.WithoutImages = documentWithoutImages?.Minify() ?? string.Empty;

        // Remove white/black (and close variants) color/background-color declarations/attributes
        // and ADJUST other non-white/non-dark colors to "pop" in dark mode
        string documentWithColorRemoved = RemoveWhiteBlackColors(minified);
        using var documentDarkMode = ParseHtml(documentWithColorRemoved);
        result.WithImagesDarkMode = documentDarkMode.Minify() ?? string.Empty;

        return result;
    }

    private static readonly string[] VoidElements = { "img", "hr", "input", "iframe", "embed", "source" };

    public static string RemoveEmptyElements(string html)
    {
        var tagsToCheck = new[] { "p", "span", "div" };

        var parser = new HtmlParser();
        var document = parser.ParseDocument(html);
        if (document is null) return html;
        document.Minify();
        foreach (var tag in tagsToCheck)
        {
            var elements = document.QuerySelectorAll(tag).ToList();
            foreach (var element in elements)
            {
                if (IsEffectivelyEmpty(element))
                {
                    element.Remove();
                }
            }
        }

        return document.ToHtml();
    }

    private static bool IsEffectivelyEmpty(IElement element)
    {
        // Check for non-whitespace text
        if (!string.IsNullOrWhiteSpace(element.TextContent))
            return false;

        // Check for meaningful void/self-closing elements
        if (element.Children.Any(child => VoidElements.Contains(child.TagName.ToLower())))
            return false;

        // Recursively check children
        return !element.Children.Any(child => !IsEffectivelyEmpty(child));
    }

    /// <summary>
    /// Removes color and background-color declarations/attributes when they represent white/black or close variants.
    /// Also adjusts non-white/non-black colors so they "pop" in dark mode (increase saturation/adjust lightness).
    /// Now additionally processes CSS rules inside &lt;style&gt; blocks (all selectors, classes, etc.).
    /// </summary>
    private static string RemoveWhiteBlackColors(string html)
    {
        var parser = new HtmlParser();
        var document = parser.ParseDocument(html);
        if (document is null) return html;

        // Iterate all elements for inline style/attributes
        foreach (var element in document.All)
        {
            // 1) Handle style attribute: remove color/background-color declarations that match white/black,
            //    adjust other colors so they "pop" in dark mode.
            if (element.HasAttribute("style"))
            {
                var style = element.GetAttribute("style") ?? string.Empty;
                var newStyle = ProcessStyleAttributeForDarkMode(style);
                if (string.IsNullOrWhiteSpace(newStyle))
                    element.RemoveAttribute("style");
                else
                    element.SetAttribute("style", newStyle);
            }

            // 2) Handle legacy color attribute (e.g., <font color="...">)
            if (element.HasAttribute("color"))
            {
                var val = element.GetAttribute("color") ?? string.Empty;
                if (IsCssColorWhiteOrBlack(val))
                {
                    element.RemoveAttribute("color");
                }
                else
                {
                    var adjusted = AdjustCssColorForDarkMode(val, isBackground: false);
                    if (!string.IsNullOrWhiteSpace(adjusted))
                        element.SetAttribute("color", adjusted);
                }
            }

            // 3) Handle bgcolor attribute
            if (element.HasAttribute("bgcolor"))
            {
                var val = element.GetAttribute("bgcolor") ?? string.Empty;
                if (IsCssColorWhiteOrBlack(val))
                {
                    element.RemoveAttribute("bgcolor");
                }
                else
                {
                    var adjusted = AdjustCssColorForDarkMode(val, isBackground: true);
                    if (!string.IsNullOrWhiteSpace(adjusted))
                        element.SetAttribute("bgcolor", adjusted);
                }
            }
        }

        // 4) Process <style> blocks: update color/background-color declarations inside CSS rules
        var styleElements = document.QuerySelectorAll("style").OfType<IElement>().ToList();
        foreach (var styleEl in styleElements)
        {
            var cssText = styleEl.TextContent ?? string.Empty;
            if (string.IsNullOrWhiteSpace(cssText)) continue;

            var processedCss = ProcessStyleBlockForDarkMode(cssText);
            // Replace content preserving element
            // AngleSharp allows setting TextContent
            styleEl.TextContent = processedCss;
        }

        return document.ToHtml();
    }

    private static string ProcessStyleAttributeForDarkMode(string style)
    {
        // Use regex to robustly parse declarations (handles spaces, missing trailing semicolon, etc.)
        var matches = Regex.Matches(style, @"(?<prop>[^:;]+)\s*:\s*(?<val>[^;]+)", RegexOptions.Compiled);
        var keep = new List<string>();

        foreach (Match m in matches)
        {
            var prop = m.Groups["prop"].Value.Trim().ToLowerInvariant();
            var val = m.Groups["val"].Value.Trim();

            // If it's color or background-color and the value is white/black-ish, drop it.
            // Otherwise, for non-white/non-dark colors adjust to "pop" in dark mode.
            if (prop == "color" || prop == "background-color")
            {
                if (IsCssColorWhiteOrBlack(val))
                {
                    continue;
                }
                else
                {
                    var adjusted = AdjustCssColorForDarkMode(val, isBackground: prop == "background-color");
                    if (!string.IsNullOrWhiteSpace(adjusted))
                        keep.Add($"{prop}: {adjusted}");
                    // else skip (safer to remove if can't parse)
                    continue;
                }
            }

            // preserve other properties unchanged
            keep.Add($"{prop}: {val}");
        }

        return string.Join("; ", keep);
    }

    /// <summary>
    /// Process CSS inside a &lt;style&gt; block:
    /// - strips comments
    /// - iterates rules and adjusts/removes color and background-color declarations using same logic as inline styles
    /// This method aims to be tolerant rather than a full CSS parser.
    /// </summary>
    private static string ProcessStyleBlockForDarkMode(string css)
    {
        if (string.IsNullOrWhiteSpace(css)) return css;

        // remove comments /* ... */
        var withoutComments = Regex.Replace(css, @"/\*.*?\*/", "", RegexOptions.Singleline);

        var sb = new StringBuilder();
        // Match selectors and declaration blocks
        var ruleRegex = new Regex(@"(?<selector>[^{]+)\{(?<decls>[^}]+)\}", RegexOptions.Singleline);
        var pos = 0;
        foreach (Match m in ruleRegex.Matches(withoutComments))
        {
            // append any text between previous pos and this rule start (to keep @-rules or whitespace)
            if (m.Index > pos)
                sb.Append(withoutComments.Substring(pos, m.Index - pos));

            var selector = m.Groups["selector"].Value.Trim();
            var decls = m.Groups["decls"].Value;

            // parse declarations
            var declMatches = Regex.Matches(decls, @"(?<prop>[^:;]+)\s*:\s*(?<val>[^;]+)", RegexOptions.Compiled);
            var keepDecls = new List<string>();

            foreach (Match dm in declMatches)
            {
                var prop = dm.Groups["prop"].Value.Trim().ToLowerInvariant();
                var val = dm.Groups["val"].Value.Trim();

                if (prop == "color" || prop == "background-color")
                {
                    if (IsCssColorWhiteOrBlack(val))
                    {
                        // drop the declaration
                        continue;
                    }
                    else
                    {
                        var adjusted = AdjustCssColorForDarkMode(val, isBackground: prop == "background-color");
                        if (!string.IsNullOrWhiteSpace(adjusted))
                            keepDecls.Add($"{prop}: {adjusted}");
                        // else skip
                        continue;
                    }
                }

                // preserve other properties unchanged
                keepDecls.Add($"{prop}: {val}");
            }

            sb.Append(selector);
            sb.Append(" { ");
            sb.Append(string.Join("; ", keepDecls));
            if (keepDecls.Count > 0)
                sb.Append("; ");
            sb.Append("}"); // close rule

            pos = m.Index + m.Length;
        }

        // append any trailing text after last match
        if (pos < withoutComments.Length)
            sb.Append(withoutComments.Substring(pos));

        return sb.ToString();
    }

    /// <summary>
    /// Adjusts a css color string to be more vibrant (pop) in dark mode.
    /// Returns a hex color string like #rrggbb when parsed, otherwise returns original trimmed string.
    /// isBackground flag allows slightly different adjustment for background colors (a tad darker).
    /// </summary>
    private static string AdjustCssColorForDarkMode(string cssColor, bool isBackground)
    {
        if (string.IsNullOrWhiteSpace(cssColor)) return cssColor;
        var parsed = ParseCssColor(cssColor);
        if (parsed is null) return cssColor; // leave unknown tokens untouched

        var (r, g, b) = parsed.Value;

        // Convert to HSL
        ColorToHsl(r, g, b, out double h, out double s, out double l);

        // Increase saturation to make color more vivid
        s = Math.Clamp(s * 1.25 + 0.03, 0.0, 1.0);

        // Adjust lightness:
        // - For text colors: lift very dark colors, slightly reduce very light colors.
        // - For background colors: reduce lightness slightly so they don't glow too much.
        if (isBackground)
        {
            l = Math.Clamp(l * 0.90, 0.03, 0.85);
        }
        else
        {
            if (l < 0.28) l = Math.Clamp(0.36, 0.0, 1.0);
            else l = Math.Clamp(l * 1.10, 0.20, 0.92);
        }

        // Convert back to RGB
        var (nr, ng, nb) = HslToColor(h, s, l);
        return $"#{nr:X2}{ng:X2}{nb:X2}";
    }

    // Convert RGB (0..255) to HSL (h in degrees 0..360, s,l in 0..1)
    private static void ColorToHsl(int r, int g, int b, out double h, out double s, out double l)
    {
        double rn = r / 255.0;
        double gn = g / 255.0;
        double bn = b / 255.0;

        double max = Math.Max(rn, Math.Max(gn, bn));
        double min = Math.Min(rn, Math.Min(gn, bn));
        l = (max + min) / 2.0;

        if (Math.Abs(max - min) < 1e-9)
        {
            s = 0.0;
            h = 0.0;
        }
        else
        {
            double d = max - min;
            s = (l > 0.5) ? d / (2.0 - max - min) : d / (max + min);

            if (max == rn) h = (gn - bn) / d + (gn < bn ? 6.0 : 0.0);
            else if (max == gn) h = (bn - rn) / d + 2.0;
            else h = (rn - gn) / d + 4.0;

            h *= 60.0;
        }
    }

    // Convert HSL (h degrees 0..360, s,l in 0..1) to RGB (0..255)
    private static (int r, int g, int b) HslToColor(double h, double s, double l)
    {
        double r, g, b;

        if (s == 0.0)
        {
            r = g = b = l; // achromatic
        }
        else
        {
            double q = l < 0.5 ? l * (1.0 + s) : l + s - l * s;
            double p = 2.0 * l - q;
            double hk = (h % 360.0) / 360.0;

            double[] t = new double[3] { hk + 1.0 / 3.0, hk, hk - 1.0 / 3.0 };
            double[] clr = new double[3];

            for (int i = 0; i < 3; i++)
            {
                double tc = t[i];
                if (tc < 0) tc += 1.0;
                if (tc > 1) tc -= 1.0;
                if (tc < 1.0 / 6.0) clr[i] = p + ((q - p) * 6.0 * tc);
                else if (tc < 1.0 / 2.0) clr[i] = q;
                else if (tc < 2.0 / 3.0) clr[i] = p + ((q - p) * (2.0 / 3.0 - tc) * 6.0);
                else clr[i] = p;
            }

            r = clr[0];
            g = clr[1];
            b = clr[2];
        }

        int ri = (int)Math.Round(Math.Clamp(r * 255.0, 0.0, 255.0));
        int gi = (int)Math.Round(Math.Clamp(g * 255.0, 0.0, 255.0));
        int bi = (int)Math.Round(Math.Clamp(b * 255.0, 0.0, 255.0));
        return (ri, gi, bi);
    }

    /// <summary>
    /// Determines whether a css color value represents white or black (including close variants).
    /// Uses proper sRGB linearization per WCAG before computing relative luminance,
    /// and performs an additional quick contrast check against pure white/black.
    /// </summary>
    private static bool IsCssColorWhiteOrBlack(string cssColor)
    {
        if (string.IsNullOrWhiteSpace(cssColor)) return false;
        var parsed = ParseCssColor(cssColor);
        if (parsed is null) return false;
        var (r, g, b) = parsed.Value;

        // Normalize to 0..1
        double rn = r / 255.0;
        double gn = g / 255.0;
        double bn = b / 255.0;

        // Linearize sRGB channels (gamma expansion) per WCAG
        double rLin = SrgbToLinear(rn);
        double gLin = SrgbToLinear(gn);
        double bLin = SrgbToLinear(bn);

        // Relative luminance (WCAG)
        double luminance = 0.2126 * rLin + 0.7152 * gLin + 0.0722 * bLin;

        // Tight luminance thresholds for "very close" white/black
        if (luminance >= 0.95) return true; // white-ish
        if (luminance <= 0.05) return true; // black-ish

        // Also check contrast ratio to pure white and pure black for borderline cases.
        // contrast = (L1 + 0.05) / (L2 + 0.05) where L1 >= L2.
        double contrastToWhite = (1.0 + 0.05) / (luminance + 0.05);
        double contrastToBlack = (luminance + 0.05) / (0.0 + 0.05);

        // If color is so close that its contrast to white or black is very low (≈1.0),
        // treat it as white/black. Adjust threshold as needed (1.1..1.3).
        const double contrastThreshold = 1.16;
        if (contrastToWhite < contrastThreshold) return true;
        if (contrastToBlack < contrastThreshold) return true;

        return false;
    }

    // Convert sRGB channel value in [0,1] to linear value per WCAG (gamma expansion)
    private static double SrgbToLinear(double v)
    {
        if (v <= 0.04045)
            return v / 12.92;
        return Math.Pow((v + 0.055) / 1.055, 2.4);
    }

    /// <summary>
    /// Parses simple CSS color strings into RGB. Returns null if not recognized.
    /// Supported: #rgb, #rrggbb, rgb(), rgba(), and basic named colors 'white'/'black' plus some common near variants.
    /// This parser is tolerant of both comma- and space-separated rgb() values.
    /// </summary>
    private static (int r, int g, int b)? ParseCssColor(string cssColor)
    {
        var s = cssColor.Trim().ToLowerInvariant();

        // Strip !important if present
        s = Regex.Replace(s, @"\s*!important\s*$", "", RegexOptions.IgnoreCase).Trim();

        // hex formats
        if (s.StartsWith("#"))
        {
            var hex = s.Substring(1);
            if (hex.Length == 3)
            {
                // e.g., #abc => aa, bb, cc
                var r = Convert.ToInt32(new string(hex[0], 2), 16);
                var g = Convert.ToInt32(new string(hex[1], 2), 16);
                var b = Convert.ToInt32(new string(hex[2], 2), 16);
                return (r, g, b);
            }
            else if (hex.Length == 6)
            {
                var r = Convert.ToInt32(hex.Substring(0, 2), 16);
                var g = Convert.ToInt32(hex.Substring(2, 2), 16);
                var b = Convert.ToInt32(hex.Substring(4, 2), 16);
                return (r, g, b);
            }
            // other lengths not supported
            return null;
        }

        // rgb(...) or rgba(...) - be tolerant of commas or spaces (CSS4 allows space-separated)
        if (s.StartsWith("rgb(") || s.StartsWith("rgba("))
        {
            var inside = s.Substring(s.IndexOf('(') + 1).TrimEnd(')');
            // normalize separators: replace commas with spaces, remove multiple spaces
            inside = inside.Replace(",", " ");
            inside = Regex.Replace(inside, @"\s+\/\s+|\s+/", " / "); // keep possible alpha separator
            var parts = inside.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                              .Where(p => p != "/")
                              .ToArray();
            if (parts.Length >= 3)
            {
                bool successR = TryParseCssComponent(parts[0], out int r);
                bool successG = TryParseCssComponent(parts[1], out int g);
                bool successB = TryParseCssComponent(parts[2], out int b);
                if (successR && successG && successB) return (r, g, b);
            }
            return null;
        }

        // named colors: handle common white/black variants
        switch (s)
        {
            case "white":
            case "snow":
            case "ghostwhite":
            case "whitesmoke":
            case "ivory":
            case "azure":
            case "lightyellow":
                return (255, 255, 255);

            case "black":
            case "gray":
            case "grey":
            case "dimgray":
            case "dimgrey":
            case "onyx":
            case "charcoal":
                return (0, 0, 0);

            default:
                break;
        }

        return null;
    }

    private static bool TryParseCssComponent(string component, out int value)
    {
        value = 0;
        component = component.Trim();
        // percentage
        if (component.EndsWith("%"))
        {
            var numPart = component.TrimEnd('%');
            if (double.TryParse(numPart, NumberStyles.Float, CultureInfo.InvariantCulture, out double pct))
            {
                value = (int)Math.Round(255.0 * Math.Clamp(pct / 100.0, 0.0, 1.0));
                return true;
            }
            return false;
        }

        // numeric 0..255 or float 0..1
        if (int.TryParse(component, NumberStyles.Integer, CultureInfo.InvariantCulture, out int iv))
        {
            value = Math.Clamp(iv, 0, 255);
            return true;
        }
        if (double.TryParse(component, NumberStyles.Float, CultureInfo.InvariantCulture, out double dv))
        {
            // if fractional 0..1 => scale
            if (dv <= 1.0)
            {
                value = (int)Math.Round(255.0 * Math.Clamp(dv, 0.0, 1.0));
                return true;
            }
            // otherwise try to clamp to 0..255
            value = (int)Math.Clamp(dv, 0.0, 255.0);
            return true;
        }

        return false;
    }

    static IHtmlDocument? ParseHtml(string html)
    {
        IHtmlDocument result = null!;
        var parser = new HtmlParser();
        result = parser.ParseDocument(html);
        return result;
    }

}