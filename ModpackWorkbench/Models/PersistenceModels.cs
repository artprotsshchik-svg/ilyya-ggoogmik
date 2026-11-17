using System.Text.Json.Serialization;

namespace ModpackWorkbench.Models;

public sealed class PersistedAppState
{
    [JsonPropertyName("dbUpdatedAt")]
    public string DbUpdatedAt { get; set; } = "16.11.2026";

    [JsonPropertyName("lastSavedDate")]
    public string LastSavedDate { get; set; } = string.Empty;

    [JsonPropertyName("activeTab")]
    public string ActiveTab { get; set; } = "assembler"; // catalog | assembler | presets | checker

    [JsonPropertyName("checkedModIds")]
    public List<string> CheckedModIds { get; set; } = new();

    [JsonPropertyName("builderModIds")]
    public List<string> BuilderModIds { get; set; } = new();

    [JsonPropertyName("builderPrefs")]
    public AssemblyPreferences BuilderPrefs { get; set; } = AssemblyPreferences.CreateDefault();

    [JsonPropertyName("focusModId")]
    public string? FocusModId { get; set; }
}

public enum FractalType
{
    None,
    Julia,
    Mandelbrot,
    Fbm,
    Tree,
    Koch,
    Sierpinski,
    Dragon,
    Fern,
    Lorenz
}

public enum BlockStyle
{
    Glass,
    Flat,
    Bordered,
    Elevated,
    Minimal
}

public enum BlockRadius
{
    Sharp,
    Medium,
    Soft
}

public enum RowDensity
{
    Compact,
    Normal,
    Comfortable
}

public enum EdgeHighlightMode
{
    Both,
    Edges,
    Soft
}

public enum ContainerWidthMode
{
    Narrow,
    Standard,
    Wide,
    Full
}

public sealed class BuilderThemeConfig
{
    public string Mode { get; set; } = "dark"; // dark | light
    public string[] Colors { get; set; } = ["#00FF52", "#FF5CA8", "#FF6B8A", "#3ED0FF", "#45F5C8"];
    public double Angle { get; set; } = 42;
    public double Saturation { get; set; } = 22;
    public double PanelTransparency { get; set; } = 26;
    public double PanelBlur { get; set; } = 12;
    public bool Animate { get; set; } = true;
    public double AnimationSpeed { get; set; } = 100;
    public FractalType Fractal { get; set; } = FractalType.Julia;
    public bool MultiFractal { get; set; } = true;
    public FractalType FractalSecondary { get; set; } = FractalType.Lorenz;
    public FractalType FractalTertiary { get; set; } = FractalType.Fern;
    public double FractalUncertainty { get; set; } = 44;
    public double FractalOpacity { get; set; } = 24;
    public double FractalScale { get; set; } = 52;
    public BlockStyle BlockStyle { get; set; } = BlockStyle.Glass;
    public BlockRadius BlockRadius { get; set; } = BlockRadius.Medium;
    public RowDensity RowDensity { get; set; } = RowDensity.Normal;
    public EdgeHighlightMode EdgeHighlight { get; set; } = EdgeHighlightMode.Both;
    public ContainerWidthMode ContainerWidth { get; set; } = ContainerWidthMode.Standard;
    public int FontScale { get; set; } = 100; // 90 | 100 | 110
    public string AccentColor { get; set; } = "auto";
    public bool ShowDescriptions { get; set; }
    public bool ShowRowMeta { get; set; } = true;
    public bool ShowRowNumbers { get; set; } = true;

    public BuilderThemeConfig Clone() => new()
    {
        Mode = Mode,
        Colors = (string[])Colors.Clone(),
        Angle = Angle,
        Saturation = Saturation,
        PanelTransparency = PanelTransparency,
        PanelBlur = PanelBlur,
        Animate = Animate,
        AnimationSpeed = AnimationSpeed,
        Fractal = Fractal,
        MultiFractal = MultiFractal,
        FractalSecondary = FractalSecondary,
        FractalTertiary = FractalTertiary,
        FractalUncertainty = FractalUncertainty,
        FractalOpacity = FractalOpacity,
        FractalScale = FractalScale,
        BlockStyle = BlockStyle,
        BlockRadius = BlockRadius,
        RowDensity = RowDensity,
        EdgeHighlight = EdgeHighlight,
        ContainerWidth = ContainerWidth,
        FontScale = FontScale,
        AccentColor = AccentColor,
        ShowDescriptions = ShowDescriptions,
        ShowRowMeta = ShowRowMeta,
        ShowRowNumbers = ShowRowNumbers
    };

    public static BuilderThemeConfig CreateDefault() => new();
}
