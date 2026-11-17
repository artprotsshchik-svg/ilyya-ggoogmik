using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ModpackWorkbench.Models;
using ModpackWorkbench.Services;

namespace ModpackWorkbench.ViewModels;

public sealed partial class BuilderModRowViewModel : ObservableObject
{
    private readonly AssemblyBuilderViewModel _parent;

    public ModItem Mod { get; }
    public string Name => Mod.Name;
    public string Platform => Mod.Platform;
    public string? UpdatedLabel { get; }
    public bool IsSelected { get; }
    public CheckerTier Tier { get; }
    public string ReasonText { get; }
    public string? DubiousReason { get; }
    public bool IsCoreExclusive { get; }
    public bool ShowSinytraTag { get; }

    public string TierColorHex => Tier switch
    {
        CheckerTier.Green => "#10B981",
        CheckerTier.Lime => "#84CC16",
        CheckerTier.Yellow => "#FBBF24",
        CheckerTier.Orange => "#F97316",
        CheckerTier.Red => "#FB7185",
        _ => "#848494"
    };

    public BuilderModRowViewModel(
        ModItem mod,
        bool isSelected,
        CheckerTier tier,
        string reasonText,
        string? dubiousReason,
        bool isCoreExclusive,
        bool showRowMeta,
        AssemblyBuilderViewModel parent)
    {
        Mod = mod;
        IsSelected = isSelected;
        Tier = tier;
        ReasonText = reasonText;
        DubiousReason = dubiousReason;
        IsCoreExclusive = isCoreExclusive;
        ShowSinytraTag = showRowMeta && mod.Platform.Contains("Sinytra", StringComparison.Ordinal);
        UpdatedLabel = ModDatabase.GetModUpdatedLabel(mod);
        _parent = parent;
    }

    [RelayCommand]
    private void Toggle() => _parent.ToggleMod(Mod);

    [RelayCommand]
    private void OpenDetails() => _parent.OpenDetails(Mod);

    [RelayCommand]
    private void OpenCurseForge() => ShellLauncher.OpenUrl(Mod.CurseforgeUrl);
}

public sealed partial class BuilderBucketViewModel : ObservableObject
{
    private readonly AssemblyBuilderViewModel _parent;

    public ModGroupInfo Group { get; }
    public string Title => Group.Title;
    public string Description => Group.Description;
    public bool ShowDescriptions { get; }
    public int SelectedCount { get; }
    public bool HasSelected => SelectedCount > 0;
    public bool IsMissingCore { get; }
    public bool HasExclusiveConflict { get; }
    public bool ShowSelectLeaderButton { get; }

    public ObservableCollection<BuilderModRowViewModel> PrimaryCoreMods { get; } = new();
    public ObservableCollection<BuilderModRowViewModel> AddonMods { get; } = new();
    public ObservableCollection<BuilderModRowViewModel> HiddenMods { get; } = new();

    public bool HasAddons => AddonMods.Count > 0;
    public bool HasHidden => HiddenMods.Count > 0;
    public string AddonsToggleLabel => $"Аддоны ({AddonMods.Count(a => a.IsSelected)}/{AddonMods.Count})";
    public string HiddenToggleLabel => IsHiddenExpanded ? "Скрыть" : $"Скрытые ({HiddenMods.Count})";

    [ObservableProperty]
    private bool _isAddonsExpanded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HiddenToggleLabel))]
    private bool _isHiddenExpanded;

    public BuilderBucketViewModel(
        ModGroupInfo group,
        int selectedCount,
        bool isMissingCore,
        bool hasExclusiveConflict,
        bool showSelectLeaderButton,
        bool showDescriptions,
        bool addonsExpanded,
        bool hiddenExpanded,
        AssemblyBuilderViewModel parent)
    {
        Group = group;
        SelectedCount = selectedCount;
        IsMissingCore = isMissingCore;
        HasExclusiveConflict = hasExclusiveConflict;
        ShowSelectLeaderButton = showSelectLeaderButton;
        ShowDescriptions = showDescriptions;
        _isAddonsExpanded = addonsExpanded;
        _isHiddenExpanded = hiddenExpanded;
        _parent = parent;
    }

    [RelayCommand]
    private void SelectLeader() => _parent.SelectSlotLeader(Group);

    [RelayCommand]
    private void ClearSlot() => _parent.ClearSlot(Group);

    [RelayCommand]
    private void ToggleAddons() => IsAddonsExpanded = !IsAddonsExpanded;

    [RelayCommand]
    private void ToggleHidden() => IsHiddenExpanded = !IsHiddenExpanded;
}

public sealed partial class AssemblyBuilderViewModel : ObservableObject
{
    private readonly MainWindowViewModel _main;
    private static readonly HashSet<string> OtherGroupIds = new(StringComparer.Ordinal)
    {
        "animations-compat", "potato-tweaks", "profilers"
    };
    private static readonly string[] MultiplayerModIds =
    [
        "connectivity", "cupboard", "krypton-reno", "servercore"
    ];

    [ObservableProperty]
    private string? _actionNotice;

    [ObservableProperty]
    private string _actionNoticeTone = "info";

    [ObservableProperty]
    private string _slotSearch = string.Empty;

    [ObservableProperty]
    private string _builderSection = "all"; // all | optimization_emi | other

    [ObservableProperty]
    private string _sortMode = "smart"; // smart | green-first | alpha

    [ObservableProperty]
    private bool _strictExclusive = true;

    [ObservableProperty]
    private bool _multiplayerOpen = true;

    [ObservableProperty]
    private bool _smartConfigOpen = true;

    [ObservableProperty]
    private int _issueCount;

    [ObservableProperty]
    private int _selectedCount;

    [ObservableProperty]
    private int _activeMultiplayerCount;

    public ObservableCollection<string> SmartNotes { get; } = new();
    public ObservableCollection<ExcludedModNote> SmartExcluded { get; } = new();
    public ObservableCollection<BuilderModRowViewModel> MultiplayerMods { get; } = new();
    public ObservableCollection<BuilderBucketViewModel> OptimizationBuckets { get; } = new();
    public ObservableCollection<BuilderBucketViewModel> OtherBuckets { get; } = new();

    public IReadOnlyList<ModPreset> Presets => ModDatabase.ModPresets;

    // Smart Builder Preference properties
    public string Gpu
    {
        get => _main.BuilderPrefs.Gpu;
        set { if (!string.IsNullOrEmpty(value)) _main.UpdateBuilderPrefs(p => p.Gpu = value); }
    }

    public string Mode
    {
        get => _main.BuilderPrefs.Mode;
        set { if (!string.IsNullOrEmpty(value)) _main.UpdateBuilderPrefs(p => p.Mode = value); }
    }

    public string Goal
    {
        get => _main.BuilderPrefs.Goal;
        set { if (!string.IsNullOrEmpty(value)) _main.UpdateBuilderPrefs(p => p.Goal = value); }
    }

    public string PortStrategy
    {
        get => _main.BuilderPrefs.PortStrategy;
        set { if (!string.IsNullOrEmpty(value)) _main.UpdateBuilderPrefs(p => p.PortStrategy = value); }
    }

    public string RecipeViewer
    {
        get => _main.BuilderPrefs.RecipeViewer;
        set { if (!string.IsNullOrEmpty(value)) _main.UpdateBuilderPrefs(p => p.RecipeViewer = value); }
    }

    public bool UseShaders
    {
        get => _main.BuilderPrefs.UseShaders;
        set => _main.UpdateBuilderPrefs(p => p.UseShaders = value);
    }

    public bool UseCreate
    {
        get => _main.BuilderPrefs.UseCreate;
        set => _main.UpdateBuilderPrefs(p => p.UseCreate = value);
    }

    public bool AllowSinytra
    {
        get => _main.BuilderPrefs.AllowSinytra;
        set => _main.UpdateBuilderPrefs(p => p.AllowSinytra = value);
    }

    public IReadOnlyList<SelectOption<string>> GpuOptions { get; } =
    [
        new("nvidia", "NVIDIA (GTX 16xx / RTX)"),
        new("amd-intel", "AMD / Intel Arc"),
        new("integrated", "Встроенная GPU")
    ];

    public IReadOnlyList<SelectOption<string>> ModeOptions { get; } =
    [
        new("singleplayer", "Одиночная игра"),
        new("server", "Сервер / Мультиплеер"),
        new("hybrid-server", "Гибрид-сервер (Mohist/Arclight)")
    ];

    public IReadOnlyList<SelectOption<string>> GoalOptions { get; } =
    [
        new("balanced", "Баланс"),
        new("max-safe", "Макс. FPS (Безопасно)"),
        new("potato", "Слабый ПК")
    ];

    public IReadOnlyList<SelectOption<string>> PortStrategyOptions { get; } =
    [
        new("hybrid", "Гибрид (Умный баланс)"),
        new("native-forge", "Нативный Forge"),
        new("fabric-sinytra", "Fabric + Sinytra")
    ];

    public IReadOnlyList<SelectOption<string>> RecipeViewerOptions { get; } =
    [
        new("jei", "JEI (Классика)"),
        new("emi", "EMI + TMRV"),
        new("emi-jei", "EMI + JEI вместе"),
        new("none", "Без рецептов")
    ];

    public AssemblyBuilderViewModel(MainWindowViewModel main)
    {
        _main = main;
    }

    partial void OnSlotSearchChanged(string value) => Refresh();
    partial void OnBuilderSectionChanged(string value) => Refresh();
    partial void OnSortModeChanged(string value) => Refresh();

    public void SetNotice(string text, string tone = "info")
    {
        ActionNotice = text;
        ActionNoticeTone = tone;
    }

    [RelayCommand]
    private void DismissNotice() => ActionNotice = null;

    [RelayCommand]
    private void ToggleSmartConfig() => SmartConfigOpen = !SmartConfigOpen;

    [RelayCommand]
    private void ToggleMultiplayerSection() => MultiplayerOpen = !MultiplayerOpen;

    [RelayCommand]
    private void ToggleStrictExclusive() => StrictExclusive = !StrictExclusive;

    [RelayCommand]
    private void BuildSmart()
    {
        var result = ModBuilder.BuildSmartAssembly(_main.BuilderPrefs);
        _main.SetAssemblyModIds(result.ModIds);
        SmartNotes.Clear();
        foreach (var note in result.Notes) SmartNotes.Add(note);
        SmartExcluded.Clear();
        foreach (var ex in result.Excluded) SmartExcluded.Add(ex);
        SetNotice($"Умная сборка пересобрана: {result.ModIds.Count} модов без конфликтов.");
    }

    [RelayCommand]
    private void LoadPreset(ModPreset? preset)
    {
        if (preset is null) return;
        _main.SetAssemblyModIds(preset.ModIds);
        SetNotice($"Загружен пресет «{preset.Name.Split('(')[0].Trim()}» ({preset.ModIds.Count} модов).");
    }

    [RelayCommand]
    private void FixIssues()
    {
        var fixedIds = ModBuilder.SmartFixAssembly(_main.AssemblyModIds, _main.BuilderPrefs);
        _main.SetAssemblyModIds(fixedIds);
        SetNotice($"Сборка автоматически очищена от конфликтов ({fixedIds.Count} модов).");
    }

    [RelayCommand]
    private void ResetAssembly()
    {
        _main.UpdateBuilderPrefs(p =>
        {
            var def = AssemblyPreferences.CreateDefault();
            p.Gpu = def.Gpu;
            p.Mode = def.Mode;
            p.Goal = def.Goal;
            p.PortStrategy = def.PortStrategy;
            p.RecipeViewer = def.RecipeViewer;
            p.UseShaders = def.UseShaders;
            p.UseCreate = def.UseCreate;
            p.AllowSinytra = def.AllowSinytra;
            p.FocusModId = null;
        });
        var result = ModBuilder.BuildSmartAssembly(_main.BuilderPrefs);
        _main.SetAssemblyModIds(result.ModIds);
        SetNotice("Сброшено к рекомендованной базовой сборке.");
    }

    [RelayCommand]
    private void ToggleMultiplayerPack()
    {
        var current = new HashSet<string>(_main.AssemblyModIds, StringComparer.Ordinal);
        bool allActive = MultiplayerModIds.All(id => current.Contains(id));
        if (allActive)
        {
            var (nextIds, _) = ModBuilder.RemoveModsCascade(current, MultiplayerModIds);
            _main.SetAssemblyModIds(nextIds);
            SetNotice("Серверные и сетевые моды убраны.");
        }
        else
        {
            foreach (var id in MultiplayerModIds) current.Add(id);
            var fixedIds = ModBuilder.SmartFixAssembly(current, _main.BuilderPrefs);
            _main.SetAssemblyModIds(fixedIds);
            SetNotice("Добавлен пакет для мультиплеера (Connectivity, Cupboard, Krypton Reno, ServerCore).");
        }
    }

    public void ToggleMod(ModItem mod)
    {
        _main.ToggleAssemblyMod(mod.Id, StrictExclusive);
    }

    public void OpenDetails(ModItem mod) => _main.RequestOpenModDetail(mod);

    public void SelectSlotLeader(ModGroupInfo group)
    {
        var res = ModBuilder.ToggleModSmart(_main.AssemblyModIds, group.LeaderId, true, _main.BuilderPrefs);
        _main.SetAssemblyModIds(res.NextIds);
        if (!string.IsNullOrEmpty(res.Notice)) SetNotice(res.Notice, res.NoticeTone);
    }

    public void ClearSlot(ModGroupInfo group)
    {
        var (nextIds, _) = ModBuilder.RemoveModsCascade(_main.AssemblyModIds, group.MemberIds);
        _main.SetAssemblyModIds(nextIds);
        SetNotice($"Слот «{group.Title}» очищен.");
    }

    public void Refresh()
    {
        var selectedIds = _main.AssemblyModIds;
        var selectedSet = new HashSet<string>(selectedIds, StringComparer.Ordinal);
        var prefs = _main.BuilderPrefs;
        SelectedCount = selectedIds.Count;

        var diagnostics = ModAnalyzer.BuildCheckerDiagnostics(selectedIds);
        IssueCount = diagnostics.Count;

        var analysis = ModAnalyzer.EvaluateAssemblyHealth(selectedIds);
        var warningByMod = new Dictionary<string, (string Severity, List<string> Messages)>(StringComparer.Ordinal);
        foreach (var w in analysis.Warnings)
        {
            if (warningByMod.TryGetValue(w.ModId, out var prev))
            {
                var sev = prev.Severity == "red" || w.Severity == "red" ? "red"
                    : prev.Severity == "orange" || w.Severity == "orange" ? "orange" : "yellow";
                prev.Messages.Add(w.Message);
                warningByMod[w.ModId] = (sev, prev.Messages);
            }
            else
            {
                warningByMod[w.ModId] = (w.Severity, [w.Message]);
            }
        }

        BuilderModRowViewModel BuildRow(ModItem mod, bool isCoreExclusive)
        {
            warningByMod.TryGetValue(mod.Id, out var w);
            var checker = ModAnalyzer.GetCheckerTier(mod, analysis.ConflictingIdSet, w.Severity, analysis.RuntimeOnlyConflictIdSet);
            var restriction = ModAnalyzer.GetModRestrictionBadge(mod, selectedSet, prefs);
            var superseder = ModAnalyzer.GetModSuperseder(mod, selectedSet, prefs, true);
            bool isIncompatible = ModAnalyzer.IsModIncompatibleWithSelection(mod, selectedSet, prefs);

            var status = checker.Tier;
            string reason = mod.Description;

            if (checker.Tier == CheckerTier.Red || isIncompatible || ModAnalyzer.IsUnsupportedOnForge(mod) || restriction?.Tone == "red")
            {
                if (checker.Tier == CheckerTier.Orange && mod.Id is "emberlens" or "nvidium")
                {
                    status = CheckerTier.Orange;
                    reason = checker.Reason;
                }
                else
                {
                    status = CheckerTier.Red;
                    reason = w.Messages?.FirstOrDefault()
                        ?? (restriction?.Tone == "red" ? restriction.Text : null)
                        ?? (isIncompatible ? "Конфликт или не работает на Forge." : null)
                        ?? checker.Reason;
                }
            }
            else if (checker.Tier == CheckerTier.Orange)
            {
                status = CheckerTier.Orange;
                reason = w.Messages?.FirstOrDefault()
                    ?? (!string.IsNullOrEmpty(mod.CoveredBy) ? $"Встроено в {mod.CoveredBy}" : null)
                    ?? superseder?.Reason
                    ?? checker.Reason;
            }
            else if (checker.Tier == CheckerTier.Yellow || !string.IsNullOrEmpty(mod.CoveredBy) || superseder is not null || restriction?.Tone == "amber")
            {
                status = CheckerTier.Yellow;
                reason = w.Messages?.FirstOrDefault()
                    ?? (!string.IsNullOrEmpty(mod.CoveredBy) ? $"Встроено в {mod.CoveredBy}" : null)
                    ?? superseder?.Reason
                    ?? (restriction?.Tone == "amber" ? restriction.Text : null)
                    ?? checker.Reason;
            }
            else if (restriction?.Tone == "sky")
            {
                reason = restriction.Text;
            }

            var dubious = ModAnalyzer.GetDubiousModReason(mod, selectedSet, prefs);
            return new BuilderModRowViewModel(
                mod,
                selectedSet.Contains(mod.Id),
                status,
                reason,
                dubious,
                isCoreExclusive,
                _main.ThemeConfig.ShowRowMeta,
                this);
        }

        // Multiplayer section
        ActiveMultiplayerCount = MultiplayerModIds.Count(id => selectedSet.Contains(id));
        MultiplayerMods.Clear();
        foreach (var mpId in MultiplayerModIds)
        {
            if (ModDatabase.ModById.TryGetValue(mpId, out var mpMod))
            {
                MultiplayerMods.Add(BuildRow(mpMod, mpMod.Id == "krypton-reno"));
            }
        }

        // Group buckets
        OptimizationBuckets.Clear();
        OtherBuckets.Clear();
        var q = SlotSearch.Trim();
        var missingGroupIds = new HashSet<string>(analysis.MissingCoreSlots.Select(m => m.GroupId), StringComparer.Ordinal);
        bool showDesc = _main.ThemeConfig.ShowDescriptions;

        foreach (var group in ModDatabase.ModGroups)
        {
            bool isOther = OtherGroupIds.Contains(group.Id);
            if (BuilderSection == "optimization_emi" && isOther) continue;
            if (BuilderSection == "other" && !isOther) continue;

            var members = group.MemberIds
                .Select(id => ModDatabase.ModById.GetValueOrDefault(id))
                .OfType<ModItem>()
                .Where(m => string.IsNullOrEmpty(q) || ModAnalyzer.MatchesModPurposeQuery(m, q))
                .ToList();

            if (members.Count == 0) continue;

            var addonSet = new HashSet<string>(group.AddonIds, StringComparer.Ordinal);
            var primaryCore = new List<ModItem>();
            var addons = new List<ModItem>();
            var hidden = new List<ModItem>();

            foreach (var m in members)
            {
                bool isAddon = addonSet.Contains(m.Id);
                bool shouldHide = ModAnalyzer.ShouldHideMod(m, selectedSet, prefs, hideIncompatible: true, hideUselessCovered: true);
                if (!selectedSet.Contains(m.Id) && shouldHide && string.IsNullOrEmpty(q))
                {
                    hidden.Add(m);
                }
                else if (isAddon)
                {
                    addons.Add(m);
                }
                else
                {
                    primaryCore.Add(m);
                }
            }

            int selectedInGroup = group.MemberIds.Count(id => selectedSet.Contains(id));
            int selectedCoreCount = group.MemberIds.Count(id => !addonSet.Contains(id) && selectedSet.Contains(id));
            bool hasConflict = group.Exclusive && selectedCoreCount > 1;
            bool isMissingCore = missingGroupIds.Contains(group.Id);

            var bucket = new BuilderBucketViewModel(
                group,
                selectedInGroup,
                isMissingCore,
                hasConflict,
                !selectedSet.Contains(group.LeaderId),
                showDesc,
                addonsExpanded: addons.Any(a => selectedSet.Contains(a.Id)) || !string.IsNullOrEmpty(q),
                hiddenExpanded: !string.IsNullOrEmpty(q),
                this);

            foreach (var pm in primaryCore) bucket.PrimaryCoreMods.Add(BuildRow(pm, group.Exclusive));
            foreach (var am in addons) bucket.AddonMods.Add(BuildRow(am, false));
            foreach (var hm in hidden) bucket.HiddenMods.Add(BuildRow(hm, group.Exclusive && !addonSet.Contains(hm.Id)));

            if (isOther) OtherBuckets.Add(bucket);
            else OptimizationBuckets.Add(bucket);
        }

        OnPropertyChanged(nameof(Gpu));
        OnPropertyChanged(nameof(Mode));
        OnPropertyChanged(nameof(Goal));
        OnPropertyChanged(nameof(PortStrategy));
        OnPropertyChanged(nameof(RecipeViewer));
        OnPropertyChanged(nameof(UseShaders));
        OnPropertyChanged(nameof(UseCreate));
        OnPropertyChanged(nameof(AllowSinytra));
    }
}
