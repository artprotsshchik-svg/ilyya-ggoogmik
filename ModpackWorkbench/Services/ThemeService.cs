using System.Text.Json;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Media;
using ModpackWorkbench.Models;

namespace ModpackWorkbench.Services;

public static partial class ThemeService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static string ThemeFilePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ModpackWorkbench",
            "theme-config.json");

    public static readonly IReadOnlyList<(string Label, BuilderThemeConfig Config)> PresetThemes =
    [
        ("Жюлиа · Стекло", BuilderThemeConfig.CreateDefault()),
        ("Мандельброт · Объём", new BuilderThemeConfig
        {
            Colors = ["#00FF87", "#10B981", "#06B6D4", "#3B82F6", "#8B5CF6"],
            Angle = 135,
            Saturation = 24,
            PanelTransparency = 28,
            PanelBlur = 14,
            Fractal = FractalType.Mandelbrot,
            FractalOpacity = 28,
            BlockStyle = BlockStyle.Elevated
        }),
        ("Лоренц · Минимал", new BuilderThemeConfig
        {
            Colors = ["#38BDF8", "#818CF8", "#C084FC", "#F472B6", "#2DD4BF"],
            Fractal = FractalType.Lorenz,
            FractalOpacity = 32,
            BlockStyle = BlockStyle.Minimal,
            BlockRadius = BlockRadius.Sharp,
            RowDensity = RowDensity.Compact,
            PanelTransparency = 20,
            AccentColor = "#7C3AED"
        }),
        ("Дракон · Контур", new BuilderThemeConfig
        {
            Colors = ["#F59E0B", "#EF4444", "#EC4899", "#8B5CF6", "#3B82F6"],
            Fractal = FractalType.Dragon,
            MultiFractal = true,
            FractalSecondary = FractalType.Sierpinski,
            FractalTertiary = FractalType.None,
            FractalUncertainty = 38,
            FractalOpacity = 28,
            BlockStyle = BlockStyle.Bordered,
            BlockRadius = BlockRadius.Sharp,
            RowDensity = RowDensity.Compact,
            PanelTransparency = 14
        }),
        ("Строгий плоский", new BuilderThemeConfig
        {
            Colors = ["#334155", "#1E293B", "#475569", "#1E293B", "#0F172A"],
            Saturation = 0,
            PanelTransparency = 0,
            PanelBlur = 0,
            Animate = false,
            Fractal = FractalType.None,
            MultiFractal = false,
            FractalSecondary = FractalType.None,
            FractalTertiary = FractalType.None,
            FractalOpacity = 0,
            BlockStyle = BlockStyle.Flat,
            BlockRadius = BlockRadius.Medium,
            EdgeHighlight = EdgeHighlightMode.Edges
        })
    ];

    public static readonly IReadOnlyList<(string Label, string Value)> AccentPresets =
    [
        ("Авто", "auto"),
        ("Синий", "#2563EB"),
        ("Изумруд", "#059669"),
        ("Янтарь", "#D97706"),
        ("Фиолет", "#7C3AED"),
        ("Роза", "#E11D48")
    ];

    public static readonly IReadOnlyList<(FractalType Type, string Label)> FractalOptions =
    [
        (FractalType.None, "Нет"),
        (FractalType.Julia, "Жюлиа"),
        (FractalType.Mandelbrot, "Мандельброт"),
        (FractalType.Fbm, "Туман FBM"),
        (FractalType.Tree, "Дерево"),
        (FractalType.Koch, "Кох"),
        (FractalType.Sierpinski, "Серпинский"),
        (FractalType.Dragon, "Дракон"),
        (FractalType.Fern, "Папоротник"),
        (FractalType.Lorenz, "Лоренц")
    ];

    [GeneratedRegex(@"^#[0-9a-fA-F]{6}$")]
    private static partial Regex HexColorRegex();

    private static double Clamp(double value, double min, double max, double fallback)
    {
        if (double.IsNaN(value) || double.IsInfinity(value)) return fallback;
        return Math.Max(min, Math.Min(max, value));
    }

    public static BuilderThemeConfig SanitizeBuilderThemeConfig(BuilderThemeConfig? parsed)
    {
        var def = BuilderThemeConfig.CreateDefault();
        if (parsed is null) return def;

        var colors = new string[5];
        if (parsed.Colors is { Length: 5 })
        {
            for (int i = 0; i < 5; i++)
            {
                var c = parsed.Colors[i];
                colors[i] = !string.IsNullOrEmpty(c) && HexColorRegex().IsMatch(c)
                    ? c.ToUpperInvariant()
                    : def.Colors[i];
            }
        }
        else
        {
            colors = (string[])def.Colors.Clone();
        }

        var accent = parsed.AccentColor == "auto" ||
                     (!string.IsNullOrEmpty(parsed.AccentColor) && HexColorRegex().IsMatch(parsed.AccentColor))
            ? (parsed.AccentColor == "auto" ? "auto" : parsed.AccentColor.ToUpperInvariant())
            : def.AccentColor;

        return new BuilderThemeConfig
        {
            Mode = parsed.Mode == "light" ? "light" : "dark",
            Colors = colors,
            Angle = Clamp(parsed.Angle, 0, 360, def.Angle),
            Saturation = Clamp(parsed.Saturation, 0, 100, def.Saturation),
            PanelTransparency = Clamp(parsed.PanelTransparency, 0, 100, def.PanelTransparency),
            PanelBlur = Clamp(parsed.PanelBlur, 0, 24, def.PanelBlur),
            Animate = parsed.Animate,
            AnimationSpeed = Clamp(parsed.AnimationSpeed, 25, 200, def.AnimationSpeed),
            Fractal = Enum.IsDefined(parsed.Fractal) ? parsed.Fractal : def.Fractal,
            MultiFractal = parsed.MultiFractal,
            FractalSecondary = Enum.IsDefined(parsed.FractalSecondary) ? parsed.FractalSecondary : def.FractalSecondary,
            FractalTertiary = Enum.IsDefined(parsed.FractalTertiary) ? parsed.FractalTertiary : def.FractalTertiary,
            FractalUncertainty = Clamp(parsed.FractalUncertainty, 0, 100, def.FractalUncertainty),
            FractalOpacity = Clamp(parsed.FractalOpacity, 0, 100, def.FractalOpacity),
            FractalScale = Clamp(parsed.FractalScale, 10, 100, def.FractalScale),
            BlockStyle = Enum.IsDefined(parsed.BlockStyle) ? parsed.BlockStyle : def.BlockStyle,
            BlockRadius = Enum.IsDefined(parsed.BlockRadius) ? parsed.BlockRadius : def.BlockRadius,
            RowDensity = Enum.IsDefined(parsed.RowDensity) ? parsed.RowDensity : def.RowDensity,
            EdgeHighlight = Enum.IsDefined(parsed.EdgeHighlight) ? parsed.EdgeHighlight : def.EdgeHighlight,
            ContainerWidth = Enum.IsDefined(parsed.ContainerWidth) ? parsed.ContainerWidth : def.ContainerWidth,
            FontScale = parsed.FontScale is 90 or 100 or 110 ? parsed.FontScale : def.FontScale,
            AccentColor = accent,
            ShowDescriptions = parsed.ShowDescriptions,
            ShowRowMeta = parsed.ShowRowMeta,
            ShowRowNumbers = parsed.ShowRowNumbers
        };
    }

    public static BuilderThemeConfig LoadThemeConfig(string? customPath = null)
    {
        try
        {
            var path = customPath ?? ThemeFilePath;
            if (!File.Exists(path)) return BuilderThemeConfig.CreateDefault();
            var json = File.ReadAllText(path);
            var parsed = JsonSerializer.Deserialize<BuilderThemeConfig>(json, JsonOptions);
            return SanitizeBuilderThemeConfig(parsed);
        }
        catch
        {
            return BuilderThemeConfig.CreateDefault();
        }
    }

    public static void SaveThemeConfig(BuilderThemeConfig config, string? customPath = null)
    {
        try
        {
            var path = customPath ?? ThemeFilePath;
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(SanitizeBuilderThemeConfig(config), JsonOptions);
            File.WriteAllText(path, json);
        }
        catch
        {
            // Ignore disk write failures
        }
    }

    public static Color ParseHex(string hex, Color fallback)
    {
        return Color.TryParse(hex, out var color) ? color : fallback;
    }

    public static void ApplyToApplication(Application? app, BuilderThemeConfig config)
    {
        if (app is null) return;
        bool isDark = config.Mode != "light";
        app.RequestedThemeVariant = isDark ? Avalonia.Styling.ThemeVariant.Dark : Avalonia.Styling.ThemeVariant.Light;

        double transp = config.PanelTransparency / 100.0;
        double panelAlpha = Math.Clamp(1.0 - transp * 0.72, 0.25, 1.0);
        byte pAlpha = (byte)(panelAlpha * 255);

        var res = app.Resources;
        if (isDark)
        {
            res["SurfacePageBrush"] = new SolidColorBrush(Color.Parse("#0B0A0F"));
            res["SurfaceHeaderBrush"] = new SolidColorBrush(Color.FromArgb((byte)Math.Clamp(pAlpha + 25, 160, 245), 13, 12, 19));
            res["SurfacePanelBrush"] = new SolidColorBrush(Color.FromArgb(pAlpha, 15, 14, 22));
            res["SurfaceSolidBrush"] = new SolidColorBrush(Color.Parse("#16151E"));
            res["SurfaceControlBrush"] = new SolidColorBrush(Color.FromArgb(22, 255, 255, 255));
            res["SurfaceSubBrush"] = new SolidColorBrush(Color.FromArgb(14, 255, 255, 255));
            res["SurfaceHoverBrush"] = new SolidColorBrush(Color.FromArgb(28, 255, 255, 255));
            res["BorderLineBrush"] = new SolidColorBrush(Color.FromArgb(36, 255, 255, 255));
            res["BorderSoftBrush"] = new SolidColorBrush(Color.FromArgb(22, 255, 255, 255));
            res["Ink1Brush"] = new SolidColorBrush(Color.Parse("#F4F4F6"));
            res["Ink2Brush"] = new SolidColorBrush(Color.Parse("#B6B6C2"));
            res["Ink3Brush"] = new SolidColorBrush(Color.Parse("#848494"));
        }
        else
        {
            res["SurfacePageBrush"] = new SolidColorBrush(Color.Parse("#F2F2EE"));
            res["SurfaceHeaderBrush"] = new SolidColorBrush(Color.FromArgb((byte)Math.Clamp(pAlpha + 20, 195, 250), 255, 255, 255));
            res["SurfacePanelBrush"] = new SolidColorBrush(Color.FromArgb(pAlpha, 255, 255, 255));
            res["SurfaceSolidBrush"] = new SolidColorBrush(Color.Parse("#FFFFFF"));
            res["SurfaceControlBrush"] = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255));
            res["SurfaceSubBrush"] = new SolidColorBrush(Color.FromArgb(12, 23, 23, 23));
            res["SurfaceHoverBrush"] = new SolidColorBrush(Color.FromArgb(20, 23, 23, 23));
            res["BorderLineBrush"] = new SolidColorBrush(Color.Parse("#D8D8D2"));
            res["BorderSoftBrush"] = new SolidColorBrush(Color.Parse("#E5E5DF"));
            res["Ink1Brush"] = new SolidColorBrush(Color.Parse("#171717"));
            res["Ink2Brush"] = new SolidColorBrush(Color.Parse("#52524E"));
            res["Ink3Brush"] = new SolidColorBrush(Color.Parse("#73736D"));
        }

        var accentColor = config.AccentColor == "auto"
            ? ParseHex(isDark ? "#2563EB" : "#1D4ED8", Colors.RoyalBlue)
            : ParseHex(config.AccentColor, Colors.RoyalBlue);
        res["AccentBrush"] = new SolidColorBrush(accentColor);
        res["AccentTextBrush"] = new SolidColorBrush(config.AccentColor == "auto"
            ? ParseHex(isDark ? "#60A5FA" : "#1D4ED8", accentColor)
            : accentColor);

        double blockRadius = config.BlockRadius switch
        {
            BlockRadius.Sharp => 3,
            BlockRadius.Soft => 14,
            _ => 8
        };
        res["BlockCornerRadius"] = new CornerRadius(blockRadius);
        res["ControlCornerRadius"] = new CornerRadius(Math.Max(2, blockRadius - 2));
    }
}
