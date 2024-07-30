using System.Drawing;

public static class ColorThemeGenerator
{
    public static Color[] GenerateTheme(Color primaryColor)
    {
        // Define the light and dark variations of the primary color
        Color lightColor = BlendColors(primaryColor, Color.White, 0.3);
        Color darkColor = BlendColors(primaryColor, Color.Black, 0.3);

        // Define the saturation and brightness variations of the primary color
        Color lightSaturationColor = AdjustColorSaturation(primaryColor, 0.3);
        Color darkSaturationColor = AdjustColorSaturation(primaryColor, -0.3);
        Color lightBrightnessColor = AdjustColorBrightness(primaryColor, 0.3);
        Color darkBrightnessColor = AdjustColorBrightness(primaryColor, -0.3);

        // Create an array to hold the color theme
        Color[] theme = new Color[16];

        // Assign colors to each element in the theme array
        theme[0] = primaryColor;
        theme[1] = lightColor;
        theme[2] = darkColor;
        theme[3] = lightSaturationColor;
        theme[4] = darkSaturationColor;
        theme[5] = lightBrightnessColor;
        theme[6] = darkBrightnessColor;
        theme[7] = BlendColors(primaryColor, lightBrightnessColor, 0.5);
        theme[8] = BlendColors(primaryColor, darkBrightnessColor, 0.5);
        theme[9] = BlendColors(primaryColor, lightSaturationColor, 0.5);
        theme[10] = BlendColors(primaryColor, darkSaturationColor, 0.5);
        theme[11] = BlendColors(primaryColor, lightColor, 0.5);
        theme[12] = BlendColors(primaryColor, darkColor, 0.5);
        theme[13] = BlendColors(primaryColor, lightSaturationColor, 0.3);
        theme[14] = BlendColors(primaryColor, darkSaturationColor, 0.3);
        theme[15] = BlendColors(primaryColor, Color.Gray, 0.5);

        return theme;
    }

    public static String[] GenerateThemeStrings(Color primaryColor)
    {
        List<string> result = new();
        foreach (Color color in GenerateTheme(primaryColor)) { result.Add(ToHtmlHexadecimal(color)); }
        return result.ToArray();
    }

    public static string ToHtmlHexadecimal(this Color color)
    {
        return string.Format("#{0:X2}{1:X2}{2:X2}", color.R, color.G, color.B);
    }
    private static Color BlendColors(Color color1, Color color2, double percentage)
    {
        int r = (int)Math.Round(color1.R * (1 - percentage) + color2.R * percentage);
        int g = (int)Math.Round(color1.G * (1 - percentage) + color2.G * percentage);
        int b = (int)Math.Round(color1.B * (1 - percentage) + color2.B * percentage);
        return Color.FromArgb(r, g, b);
    }

    private static Color AdjustColorSaturation(Color color, double percentage)
    {
        float saturation = color.GetSaturation();
        float newSaturation = Math.Max(0, Math.Min(1, saturation + (float)percentage));
        return ColorFromAhsb(color.A, color.GetHue(), newSaturation, color.GetBrightness());
    }

    private static Color AdjustColorBrightness(Color color, double percentage)
    {
        float brightness = color.GetBrightness();
        float newBrightness = Math.Max(0, Math.Min(1, brightness + (float)percentage));
        return ColorFromAhsb(color.A, color.GetHue(), color.GetSaturation(), newBrightness);
    }

    private static Color ColorFromAhsb(int alpha, float hue, float saturation, float brightness)
    {
        if (saturation == 0)
        {
            return Color.FromArgb(alpha, (int)(brightness * 255), (int)(brightness * 255), (int)(brightness * 255));
        }

        float fMax, fMid, fMin;
        int iSextant, iMax, iMid, iMin;

        if (brightness <= 0.5)
        {
            fMid = brightness * (1.0f + saturation);
        }
        else
        {
            fMid = brightness + saturation - (brightness * saturation);
        }

        fMin = 2.0f * brightness - fMid;
        iSextant = (int)(hue / 60.0f);
        if (hue == 360.0f)
        {
            hue = 0.0f;
        }
        hue /= 60.0f;
        hue -= (float)iSextant;
        iMax = (int)(255.0f * brightness);
        iMid = (int)(255.0f * fMid);
        iMin = (int)(255.0f * fMin);

        switch (iSextant)
        {
            case 0:
                return Color.FromArgb(alpha, iMax, iMid, iMin);
            case 1:
                return Color.FromArgb(alpha, iMid, iMax, iMin);
            case 2:
                return Color.FromArgb(alpha, iMin, iMax, iMid);
            case 3:
                return Color.FromArgb(alpha, iMin, iMid, iMax);
            case 4:
                return Color.FromArgb(alpha, iMid, iMin, iMax);
            default:
                return Color.FromArgb(alpha, iMax, iMin, iMax);
        }
    }
}