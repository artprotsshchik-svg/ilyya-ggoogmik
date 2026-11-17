using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ModpackWorkbench.Models;
using ModpackWorkbench.Services;

namespace ModpackWorkbench.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    public static readonly string[] DefaultInitialAssemblyIds =
    [
        "embeddium",
        "sodium-chloride",
        "immediatelyfast",
        "entity-culling",
        "more-culling-reforged",
        "enhanced-block-entities-reloaded",
        "culllessleaves-reforged",
        "modernfix",
        "ferritecore",
        "alltheleaks-memory-leak-fix",
        "jasione",
        "put-a-plug-in-it-papi",
        "radium-re-reforged",
        "badoptimizations",
        "noisiumed",
        "ixeris",
        "dynamic-fps",
        "packet-fixer",
        "fastvancements",
        "clumps",
        "let-me-despawn",
        "cloth-config-v12",
        "matlib",
        "krypton-reno",
        "smooth-boot-reloaded",
        "just-enough-items-jei",
        "jei-emi-optimizations",
        "fast-recipe-search",
        "alloyium",
        "distant-horizons",
        "distant-horizons-memory-fix-dh",
        "sinytra-connector",
        "forgified-fabric-api",
        "connector-extras"
    ];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCatalogTab))]
    [NotifyPropertyChangedFor(nameof(IsAssemblerTab))]
    [NotifyPropertyChangedFor(nameof(IsPresetsTab))]
    [NotifyPropertyChangedFor(nameof(IsCheckerTab))]
    private string _activeTab = "assembler";

    [ObservableProperty]
    private string _dbUpdatedAt = ModDatabase.DatabaseUpdatedAt;

    [ObservableProperty]
    private string _lastSavedDate = PersistenceService.FormatRuDate();

    [ObservableProperty]
    private bool _isThemeDrawerOpen;

    [ObservableProperty]
    private BuilderThemeConfig _themeConfig;

    private List<string> _assemblyModIds = new();
    private AssemblyPreferences _builderPrefs = AssemblyPreferences.CreateDefault();

    public IReadOnlyList<string> AssemblyModIds => _assemblyModIds;
    public AssemblyPreferences BuilderPrefs => _builderPrefs;

    public int CatalogCount => ModDatabase.ModsDatabase.Count;
    public int PresetsCount => ModDatabase.ModPresets.Count;
    public int AssemblyCount => _assemblyModIds.Count;

    public bool IsCatalogTab => ActiveTab == "catalog";
    public bool IsAssemblerTab => ActiveTab == "assembler";
    public bool IsPresetsTab => ActiveTab == "presets";
    public bool IsCheckerTab => ActiveTab == "checker";

    public CatalogViewModel Catalog { get; }
    public AssemblyBuilderViewModel AssemblyBuilder { get; }
    public PresetsViewModel Presets { get; }
    public CheckerViewModel Checker { get; }
    public ThemeConfiguratorViewModel ThemeConfigurator { get; }

    public event Action<ModItem>? OpenModDetailRequested;
    public Func<Task<string?>>? PickFolderAsync { get; set; }
    public Func<string, Task>? CopyToClipboardAsync { get; set; }

    public MainWindowViewModel() : this(loadFromDisk: true) { }

    public MainWindowViewModel(bool loadFromDisk)
    {
        var saved = loadFromDisk ? PersistenceService.LoadAppState() : null;
        _themeConfig = loadFromDisk ? ThemeService.LoadThemeConfig() : BuilderThemeConfig.CreateDefault();

        _builderPrefs = saved?.BuilderPrefs ?? AssemblyPreferences.CreateDefault();
        if (saved is { BuilderModIds.Count: > 0 })
        {
            _assemblyModIds = saved.BuilderModIds;
        }
        else if (saved is { CheckedModIds.Count: > 0 })
        {
            _assemblyModIds = saved.CheckedModIds;
        }
        else
        {
            _assemblyModIds = ModBuilder.BuildSmartAssembly(_builderPrefs).ModIds;
        }

        if (saved is not null)
        {
            if (!string.IsNullOrWhiteSpace(saved.DbUpdatedAt)) _dbUpdatedAt = saved.DbUpdatedAt;
            if (!string.IsNullOrWhiteSpace(saved.LastSavedDate)) _lastSavedDate = saved.LastSavedDate;
            if (saved.ActiveTab is "catalog" or "assembler" or "presets" or "checker")
                _activeTab = saved.ActiveTab;
        }

        Catalog = new CatalogViewModel(this);
        AssemblyBuilder = new AssemblyBuilderViewModel(this);
        Presets = new PresetsViewModel(this);
        Checker = new CheckerViewModel(this);
        ThemeConfigurator = new ThemeConfiguratorViewModel(_themeConfig, OnThemeChanged);

        RefreshAllTabs();
    }

    [RelayCommand]
    private void SelectTab(string tab)
    {
        if (tab is "catalog" or "assembler" or "presets" or "checker")
        {
            ActiveTab = tab;
            SaveState();
        }
    }

    [RelayCommand]
    private void ToggleThemeDrawer()
    {
        IsThemeDrawerOpen = !IsThemeDrawerOpen;
    }

    public void SetAssemblyModIds(IEnumerable<string> nextIds, bool save = true)
    {
        _assemblyModIds = nextIds
            .Where(id => ModDatabase.ModById.ContainsKey(id))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        OnPropertyChanged(nameof(AssemblyModIds));
        OnPropertyChanged(nameof(AssemblyCount));
        RefreshAllTabs();
        if (save) SaveState();
    }

    public void ToggleAssemblyMod(string modId, bool strict = true)
    {
        var res = ModBuilder.ToggleModSmart(_assemblyModIds, modId, strict, _builderPrefs);
        SetAssemblyModIds(res.NextIds);
        if (!string.IsNullOrEmpty(res.Notice))
        {
            AssemblyBuilder.SetNotice(res.Notice, res.NoticeTone);
        }
    }

    public void UpdateBuilderPrefs(Action<AssemblyPreferences> mutate)
    {
        mutate(_builderPrefs);
        OnPropertyChanged(nameof(BuilderPrefs));
        RefreshAllTabs();
        SaveState();
    }

    public void LoadPresetToBuilder(ModPreset preset)
    {
        SetAssemblyModIds(preset.ModIds);
        ActiveTab = "assembler";
        SaveState();
    }

    public void LoadPresetToChecker(ModPreset preset)
    {
        SetAssemblyModIds(preset.ModIds);
        ActiveTab = "checker";
        SaveState();
    }

    public void RequestOpenModDetail(ModItem mod)
    {
        OpenModDetailRequested?.Invoke(mod);
    }

    public ModDetailViewModel CreateModDetailViewModel(ModItem mod)
    {
        return new ModDetailViewModel(mod, this);
    }

    private void OnThemeChanged(BuilderThemeConfig updated)
    {
        ThemeConfig = updated;
        ThemeService.SaveThemeConfig(updated);
        ThemeService.ApplyToApplication(Avalonia.Application.Current, updated);
        RefreshAllTabs();
    }

    private void RefreshAllTabs()
    {
        Catalog.Refresh();
        AssemblyBuilder.Refresh();
        Checker.Refresh();
    }

    private void SaveState()
    {
        LastSavedDate = PersistenceService.FormatRuDate();
        PersistenceService.SaveAppState(new PersistedAppState
        {
            DbUpdatedAt = DbUpdatedAt,
            LastSavedDate = LastSavedDate,
            ActiveTab = ActiveTab,
            CheckedModIds = _assemblyModIds,
            BuilderModIds = _assemblyModIds,
            BuilderPrefs = _builderPrefs,
            FocusModId = _builderPrefs.FocusModId
        });
    }
}
