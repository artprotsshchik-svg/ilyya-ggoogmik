using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ModpackWorkbench.Models;
using ModpackWorkbench.Services;

namespace ModpackWorkbench.ViewModels;

public sealed partial class CatalogRowViewModel : ObservableObject
{
    private readonly CatalogViewModel _parent;

    public ModItem Mod { get; }
    public string Name => Mod.Name;
    public string Category => Mod.Category;
    public string GroupTitle => Mod.GroupTitle ?? Mod.Category;
    public string Platform => Mod.Platform;
    public string Description => Mod.Description;
    public string StatusLabel => Mod.StatusLabel;
    public string? UpdatedLabel { get; }
    public CheckerTier Tier { get; }
    public string? RestrictionText { get; }
    public string? RestrictionTone { get; }
    public string? DubiousReason { get; }
    public bool IsSelectedInAssembly { get; }
    public bool IsGroupLeader => Mod.IsGroupLeader;
    public bool ShowDescriptions { get; }
    public bool ShowRowMeta { get; }

    public IReadOnlyList<ModItem> Addons { get; }
    public IReadOnlyList<ModItem> Analogs { get; }
    public bool HasAddons => Addons.Count > 0;
    public bool HasAnalogs => Analogs.Count > 0;
    public int ActiveAddonsCount { get; }

    public string AddonsButtonText => ActiveAddonsCount > 0
        ? $"Аддоны ({ActiveAddonsCount}/{Addons.Count})"
        : $"Аддоны ({Addons.Count})";

    public string AnalogsButtonText => $"Аналоги ({Analogs.Count})";

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private bool _isAddonsExpanded;

    [ObservableProperty]
    private bool _isAnalogsExpanded;

    public string TierColorHex => Tier switch
    {
        CheckerTier.Green => "#10B981",
        CheckerTier.Lime => "#84CC16",
        CheckerTier.Yellow => "#FBBF24",
        CheckerTier.Orange => "#F97316",
        CheckerTier.Red => "#FB7185",
        _ => "#848494"
    };

    public CatalogRowViewModel(
        ModItem mod,
        CheckerTier tier,
        RestrictionBadge? restriction,
        string? dubiousReason,
        bool isSelectedInAssembly,
        IReadOnlySet<string> assemblySet,
        bool showDescriptions,
        bool showRowMeta,
        CatalogViewModel parent)
    {
        Mod = mod;
        Tier = tier;
        RestrictionText = restriction?.Text;
        RestrictionTone = restriction?.Tone;
        DubiousReason = dubiousReason;
        IsSelectedInAssembly = isSelectedInAssembly;
        ShowDescriptions = showDescriptions;
        ShowRowMeta = showRowMeta;
        _parent = parent;
        UpdatedLabel = ModDatabase.GetModUpdatedLabel(mod);

        var grpInfo = !string.IsNullOrEmpty(mod.GroupId)
            ? ModAnalyzer.GetGroupMembersByRole(mod.GroupId)
            : null;
        if (grpInfo is not null && mod.IsGroupLeader)
        {
            Addons = grpInfo.Addons;
            Analogs = grpInfo.RemovedAnalogs;
            ActiveAddonsCount = Addons.Count(a => assemblySet.Contains(a.Id));
        }
        else
        {
            Addons = [];
            Analogs = [];
        }
    }

    [RelayCommand]
    private void ToggleInAssembly() => _parent.ToggleModInAssembly(Mod.Id);

    [RelayCommand]
    private void OpenDetails() => _parent.OpenModDetail(Mod);

    [RelayCommand]
    private void ToggleExpanded() => IsExpanded = !IsExpanded;

    [RelayCommand]
    private void ToggleAddons() => IsAddonsExpanded = !IsAddonsExpanded;

    [RelayCommand]
    private void ToggleAnalogs() => IsAnalogsExpanded = !IsAnalogsExpanded;

    [RelayCommand]
    private void OpenCurseForge() => ShellLauncher.OpenUrl(Mod.CurseforgeUrl);

    [RelayCommand]
    private void ToggleSubMod(ModItem? sub)
    {
        if (sub is not null) _parent.ToggleModInAssembly(sub.Id);
    }
}

public sealed partial class CatalogViewModel : ObservableObject
{
    private readonly MainWindowViewModel _main;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private string _selectedCategory = "Все";

    [ObservableProperty]
    private string _healthFilter = "all"; // all | green | lime | yellow | orange | red

    [ObservableProperty]
    private bool _onlyLeaders = true;

    [ObservableProperty]
    private bool _hideUselessCovered = true;

    [ObservableProperty]
    private bool _onlyCompatibleInCatalog;

    [ObservableProperty]
    private string _sortBy = "leaders-first"; // leaders-first | best-first | name-asc

    [ObservableProperty]
    private int _totalFilteredCount;

    public ObservableCollection<string> Categories { get; } = new();
    public ObservableCollection<CatalogRowViewModel> Rows { get; } = new();

    public IReadOnlyList<SelectOption<string>> HealthOptions { get; } =
    [
        new("all", "Все статусы"),
        new("green", "Зелёные (Лидеры)"),
        new("lime", "Светло-зелёные"),
        new("yellow", "Жёлтые"),
        new("orange", "Оранжевые"),
        new("red", "Красные")
    ];

    public IReadOnlyList<SelectOption<string>> SortOptions { get; } =
    [
        new("leaders-first", "Сначала лидеры"),
        new("best-first", "По рейтингу"),
        new("name-asc", "По алфавиту")
    ];

    public CatalogViewModel(MainWindowViewModel main)
    {
        _main = main;
        Categories.Add("Все");
        foreach (var cat in ModDatabase.ModsDatabase.Select(m => m.Category).Distinct())
        {
            Categories.Add(cat);
        }
    }

    partial void OnSearchQueryChanged(string value) => Refresh();
    partial void OnSelectedCategoryChanged(string value) => Refresh();
    partial void OnHealthFilterChanged(string value) => Refresh();
    partial void OnOnlyLeadersChanged(bool value) => Refresh();
    partial void OnHideUselessCoveredChanged(bool value) => Refresh();
    partial void OnOnlyCompatibleInCatalogChanged(bool value) => Refresh();
    partial void OnSortByChanged(string value) => Refresh();

    [RelayCommand]
    private void ClearSearch()
    {
        SearchQuery = string.Empty;
    }

    public void ToggleModInAssembly(string modId) => _main.ToggleAssemblyMod(modId);

    public void OpenModDetail(ModItem mod) => _main.RequestOpenModDetail(mod);

    public void Refresh()
    {
        var assemblySet = new HashSet<string>(_main.AssemblyModIds, StringComparer.Ordinal);
        var prefs = _main.BuilderPrefs;
        var q = SearchQuery.Trim();
        var baseList = !string.IsNullOrEmpty(q)
            ? ModAnalyzer.SearchModsByQuery(q, ModDatabase.ModsDatabase.Count)
            : ModDatabase.ModsDatabase.ToList();

        var selectedCat = string.IsNullOrEmpty(SelectedCategory) ? "Все" : SelectedCategory;
        var healthFilter = string.IsNullOrEmpty(HealthFilter) ? "all" : HealthFilter;
        var sortBy = string.IsNullOrEmpty(SortBy) ? "leaders-first" : SortBy;

        var filtered = new List<(ModItem Mod, CheckerTier Tier, RestrictionBadge? Restriction)>();
        foreach (var mod in baseList)
        {
            if (selectedCat != "Все" && mod.Category != selectedCat) continue;

            var baseTier = ModAnalyzer.GetBaseHealthTier(mod);
            if (healthFilter != "all" && baseTier.ToString().ToLowerInvariant() != healthFilter) continue;

            bool incompatible = ModAnalyzer.IsModIncompatibleWithSelection(mod, assemblySet, prefs);
            if (OnlyCompatibleInCatalog && incompatible) continue;

            var restriction = ModAnalyzer.GetModRestrictionBadge(mod, assemblySet, prefs);
            if (string.IsNullOrEmpty(q))
            {
                if (HideUselessCovered &&
                    (!string.IsNullOrEmpty(mod.CoveredBy) ||
                     mod.Status == "danger" ||
                     ModAnalyzer.IsUnsupportedOnForge(mod) ||
                     ModAnalyzer.GetModSuperseder(mod, assemblySet, prefs, true) is not null))
                {
                    continue;
                }
                if (OnlyLeaders && !string.IsNullOrEmpty(mod.GroupId) && !mod.IsGroupLeader)
                {
                    continue;
                }
            }

            var tier = baseTier;
            if (baseTier == CheckerTier.Red || incompatible || restriction?.Tone == "red")
                tier = CheckerTier.Red;
            else if (baseTier == CheckerTier.Orange)
                tier = CheckerTier.Orange;
            else if (baseTier == CheckerTier.Yellow || !string.IsNullOrEmpty(mod.CoveredBy) || restriction?.Tone == "amber")
                tier = CheckerTier.Yellow;

            filtered.Add((mod, tier, restriction));
        }

        if (string.IsNullOrEmpty(q))
        {
            var ruCulture = System.Globalization.CultureInfo.GetCultureInfo("ru-RU");
            filtered.Sort((a, b) =>
            {
                if (sortBy == "name-asc")
                    return string.Compare(a.Mod.Name, b.Mod.Name, ruCulture, System.Globalization.CompareOptions.IgnoreCase);
                if (sortBy == "leaders-first")
                {
                    int la = a.Mod.IsGroupLeader ? 1 : 0;
                    int lb = b.Mod.IsGroupLeader ? 1 : 0;
                    if (lb != la) return lb - la;
                }
                return ModAnalyzer.GetModEffectivenessScore(b.Mod).CompareTo(ModAnalyzer.GetModEffectivenessScore(a.Mod));
            });
        }

        TotalFilteredCount = filtered.Count;
        Rows.Clear();
        bool showDesc = _main.ThemeConfig.ShowDescriptions;
        bool showMeta = _main.ThemeConfig.ShowRowMeta;

        foreach (var (mod, tier, restriction) in filtered)
        {
            var dubious = ModAnalyzer.GetDubiousModReason(mod, assemblySet, prefs);
            Rows.Add(new CatalogRowViewModel(
                mod,
                tier,
                restriction,
                dubious,
                assemblySet.Contains(mod.Id),
                assemblySet,
                showDesc,
                showMeta,
                this));
        }
    }
}
