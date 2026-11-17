using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ModpackWorkbench.Models;
using ModpackWorkbench.Services;

namespace ModpackWorkbench.ViewModels;

public sealed record ThemePresetOption(string Label, BuilderThemeConfig Config);

public sealed partial class ThemeConfiguratorViewModel : ObservableObject
{
    private readonly Action<BuilderThemeConfig> _onChanged;
    private BuilderThemeConfig _config;

    public IReadOnlyList<ThemePresetOption> Presets { get; } =
        ThemeService.PresetThemes.Select(p => new ThemePresetOption(p.Label, p.Config)).ToList();

    public IReadOnlyList<SelectOption<FractalType>> FractalOptions { get; } =
        ThemeService.FractalOptions.Select(f => new SelectOption<FractalType>(f.Type, f.Label)).ToList();

    public IReadOnlyList<SelectOption<string>> AccentPresets { get; } =
        ThemeService.AccentPresets.Select(a => new SelectOption<string>(a.Value, a.Label)).ToList();

    public bool IsDarkMode
    {
        get => _config.Mode != "light";
        set
        {
            _config.Mode = value ? "dark" : "light";
            NotifyAll();
        }
    }

    public double Angle
    {
        get => _config.Angle;
        set { _config.Angle = Math.Clamp(value, 0, 360); NotifyAll(); }
    }

    public double Saturation
    {
        get => _config.Saturation;
        set { _config.Saturation = Math.Clamp(value, 0, 100); NotifyAll(); }
    }

    public double PanelTransparency
    {
        get => _config.PanelTransparency;
        set { _config.PanelTransparency = Math.Clamp(value, 0, 100); NotifyAll(); }
    }

    public double FractalOpacity
    {
        get => _config.FractalOpacity;
        set { _config.FractalOpacity = Math.Clamp(value, 0, 100); NotifyAll(); }
    }

    public double FractalScale
    {
        get => _config.FractalScale;
        set { _config.FractalScale = Math.Clamp(value, 10, 100); NotifyAll(); }
    }

    public double FractalUncertainty
    {
        get => _config.FractalUncertainty;
        set { _config.FractalUncertainty = Math.Clamp(value, 0, 100); NotifyAll(); }
    }

    public bool Animate
    {
        get => _config.Animate;
        set { _config.Animate = value; NotifyAll(); }
    }

    public bool MultiFractal
    {
        get => _config.MultiFractal;
        set { _config.MultiFractal = value; NotifyAll(); }
    }

    public FractalType SelectedFractal
    {
        get => _config.Fractal;
        set { _config.Fractal = value; NotifyAll(); }
    }

    public FractalType SelectedFractalSecondary
    {
        get => _config.FractalSecondary;
        set { _config.FractalSecondary = value; NotifyAll(); }
    }

    public BlockStyle SelectedBlockStyle
    {
        get => _config.BlockStyle;
        set { _config.BlockStyle = value; NotifyAll(); }
    }

    public BlockRadius SelectedBlockRadius
    {
        get => _config.BlockRadius;
        set { _config.BlockRadius = value; NotifyAll(); }
    }

    public RowDensity SelectedRowDensity
    {
        get => _config.RowDensity;
        set { _config.RowDensity = value; NotifyAll(); }
    }

    public bool ShowDescriptions
    {
        get => _config.ShowDescriptions;
        set { _config.ShowDescriptions = value; NotifyAll(); }
    }

    public bool ShowRowMeta
    {
        get => _config.ShowRowMeta;
        set { _config.ShowRowMeta = value; NotifyAll(); }
    }

    public bool ShowRowNumbers
    {
        get => _config.ShowRowNumbers;
        set { _config.ShowRowNumbers = value; NotifyAll(); }
    }

    public string AccentColor
    {
        get => _config.AccentColor;
        set { _config.AccentColor = value; NotifyAll(); }
    }

    public IReadOnlyList<BlockStyle> BlockStyleOptions { get; } = Enum.GetValues<BlockStyle>();
    public IReadOnlyList<BlockRadius> BlockRadiusOptions { get; } = Enum.GetValues<BlockRadius>();
    public IReadOnlyList<RowDensity> RowDensityOptions { get; } = Enum.GetValues<RowDensity>();

    public ThemeConfiguratorViewModel(BuilderThemeConfig initial, Action<BuilderThemeConfig> onChanged)
    {
        _config = initial.Clone();
        _onChanged = onChanged;
    }

    [RelayCommand]
    private void ApplyPreset(BuilderThemeConfig? preset)
    {
        if (preset is null) return;
        _config = preset.Clone();
        NotifyAll();
    }

    [RelayCommand]
    private void SelectAccent(string? accent)
    {
        if (string.IsNullOrEmpty(accent)) return;
        AccentColor = accent;
    }

    [RelayCommand]
    private void ResetTheme()
    {
        _config = BuilderThemeConfig.CreateDefault();
        NotifyAll();
    }

    private void NotifyAll()
    {
        OnPropertyChanged(string.Empty);
        _onChanged(_config.Clone());
    }
}
