using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ModpackWorkbench.Models;
using ModpackWorkbench.Services;

namespace ModpackWorkbench.ViewModels;

public sealed partial class CheckerRowViewModel : ObservableObject
{
    private readonly CheckerViewModel _parent;

    public ModItem Mod { get; }
    public CheckerTier Tier { get; }
    public string Reason { get; }
    public string? ViaBridge { get; }
    public string? DubiousReason { get; }
    public string? UpdatedLabel { get; }
    public bool IsChild { get; }
    public string? RelationLabel { get; }
    public int? RootIndex { get; }
    public bool ShowDescriptions { get; }
    public bool ShowRowMeta { get; }
    public bool ShowRowNumbers { get; }

    public string Name => Mod.Name;
    public string Platform => Mod.Platform;
    public string DisplayLine => Tier is CheckerTier.Green or CheckerTier.Lime ? Mod.Description : Reason;
    public bool HasReasonLine => Tier is CheckerTier.Red or CheckerTier.Orange or CheckerTier.Yellow || ShowDescriptions;
    public string TierColorHex => Tier switch
    {
        CheckerTier.Green => "#10B981",
        CheckerTier.Lime => "#84CC16",
        CheckerTier.Yellow => "#FBBF24",
        CheckerTier.Orange => "#F97316",
        CheckerTier.Red => "#FB7185",
        _ => "#848494"
    };

    public CheckerRowViewModel(
        ModItem mod,
        CheckerTier tier,
        string reason,
        string? viaBridge,
        string? dubiousReason,
        bool isChild,
        string? relationLabel,
        int? rootIndex,
        bool showDescriptions,
        bool showRowMeta,
        bool showRowNumbers,
        CheckerViewModel parent)
    {
        Mod = mod;
        Tier = tier;
        Reason = reason;
        ViaBridge = viaBridge;
        DubiousReason = dubiousReason;
        UpdatedLabel = ModDatabase.GetModUpdatedLabel(mod);
        IsChild = isChild;
        RelationLabel = relationLabel;
        RootIndex = rootIndex;
        ShowDescriptions = showDescriptions;
        ShowRowMeta = showRowMeta;
        ShowRowNumbers = showRowNumbers;
        _parent = parent;
    }

    [RelayCommand]
    private void Remove() => _parent.RemoveMod(Mod.Id);

    [RelayCommand]
    private void OpenDetails() => _parent.OpenDetails(Mod);

    [RelayCommand]
    private void OpenCurseForge() => ShellLauncher.OpenUrl(Mod.CurseforgeUrl);
}

public sealed partial class DiagnosticIssueViewModel : ObservableObject
{
    private readonly CheckerViewModel _parent;

    public DiagnosticIssue Issue { get; }
    public string Title => Issue.Title;
    public string Problem => Issue.Problem;
    public string BestSolution => Issue.BestSolution;
    public string BetweenText => string.Join(" ↔ ", Issue.BetweenNames);

    public string SeverityLabel => Issue.Kind switch
    {
        "runtime" => "Конфликт режима",
        "conflict" => "Конфликт",
        "dependency" => "Зависимость",
        "redundant" => "Дубль",
        _ => "Платформа"
    };

    public string SeverityColorHex => Issue.Severity switch
    {
        "red" => "#FB7185",
        "orange" => "#F97316",
        _ => "#FBBF24"
    };

    public bool IsChoiceConflict => Issue.Kind is "conflict" or "runtime" && Issue.BetweenIds.Count == 2;
    public bool HasAddFix => !IsChoiceConflict && Issue.AddIds is { Count: > 0 };
    public bool HasRemoveFix => !IsChoiceConflict && Issue.RemoveIds is { Count: > 0 };

    public sealed record ConflictChoiceOption(string Id, string Label, bool IsPreferred);

    public IReadOnlyList<ConflictChoiceOption> Choices { get; }

    public DiagnosticIssueViewModel(DiagnosticIssue issue, CheckerViewModel parent)
    {
        Issue = issue;
        _parent = parent;
        Choices = IsChoiceConflict
            ? issue.BetweenIds.Select(id => new ConflictChoiceOption(
                id,
                $"Оставить {(ModDatabase.ModById.TryGetValue(id, out var m) ? m.Name : id)}",
                id == issue.KeepPreferredId)).ToList()
            : [];
    }

    [RelayCommand]
    private void ApplyChoice(string? keepId) => _parent.ApplyDiagnosticFix(Issue, keepId);

    [RelayCommand]
    private void ApplyAddFix() => _parent.ApplyDiagnosticFix(Issue, null);

    [RelayCommand]
    private void ApplyRemoveFix()
    {
        if (Issue.RemoveIds is null) return;
        var removeSet = new HashSet<string>(Issue.RemoveIds, StringComparer.Ordinal);
        _parent.RemoveMods(removeSet);
    }
}

public sealed partial class CheckerViewModel : ObservableObject
{
    private readonly MainWindowViewModel _main;

    [ObservableProperty]
    private string _tierFilter = "all"; // all | green | lime | yellow | orange | red

    [ObservableProperty]
    private string _viewMode = "tree"; // tree | status

    [ObservableProperty]
    private bool _importOpen;

    [ObservableProperty]
    private string _importText = string.Empty;

    [ObservableProperty]
    private string _addSearchQuery = string.Empty;

    [ObservableProperty]
    private string? _feedbackBanner;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private int _greenCount;

    [ObservableProperty]
    private int _limeCount;

    [ObservableProperty]
    private int _yellowCount;

    [ObservableProperty]
    private int _orangeCount;

    [ObservableProperty]
    private int _redCount;

    [ObservableProperty]
    private int _criticalIssuesCount;

    [ObservableProperty]
    private int _orangeIssuesCount;

    [ObservableProperty]
    private int _yellowIssuesCount;

    public bool HasAnyIssues => Diagnostics.Count > 0;
    public bool HasMissingCoreSlots => MissingCoreSlots.Count > 0;

    public int ImportMatchedCount => ModTreeService.ParseModNamesListText(ImportText).MatchedIds.Count;
    public int ImportUnmatchedCount => ModTreeService.ParseModNamesListText(ImportText).UnmatchedLines.Count;

    public ObservableCollection<CheckerRowViewModel> Rows { get; } = new();
    public ObservableCollection<DiagnosticIssueViewModel> Diagnostics { get; } = new();
    public ObservableCollection<MissingSlotWarning> MissingCoreSlots { get; } = new();
    public ObservableCollection<ModItem> SearchSuggestions { get; } = new();

    public IReadOnlyList<ModPreset> Presets => ModDatabase.ModPresets;

    public CheckerViewModel(MainWindowViewModel main)
    {
        _main = main;
    }

    partial void OnTierFilterChanged(string value) => Refresh();
    partial void OnViewModeChanged(string value) => Refresh();
    partial void OnImportTextChanged(string value)
    {
        OnPropertyChanged(nameof(ImportMatchedCount));
        OnPropertyChanged(nameof(ImportUnmatchedCount));
    }
    partial void OnAddSearchQueryChanged(string value) => UpdateSearchSuggestions();

    private void UpdateSearchSuggestions()
    {
        SearchSuggestions.Clear();
        var q = AddSearchQuery.Trim();
        if (string.IsNullOrEmpty(q)) return;
        foreach (var m in ModAnalyzer.SearchModsByQuery(q, 10))
            SearchSuggestions.Add(m);
    }

    [RelayCommand]
    private void SetTierFilter(string filter) => TierFilter = filter;

    [RelayCommand]
    private void SetViewMode(string mode) => ViewMode = mode;

    [RelayCommand]
    private void ToggleImport() => ImportOpen = !ImportOpen;

    [RelayCommand]
    private void DismissFeedback() => FeedbackBanner = null;

    [RelayCommand]
    private void ApplyImportMerge()
    {
        var parsed = ModTreeService.ParseModNamesListText(ImportText);
        if (parsed.MatchedIds.Count == 0) return;
        _main.SetAssemblyModIds(_main.AssemblyModIds.Concat(parsed.MatchedIds));
        FeedbackBanner = $"Добавлено из списка: {parsed.MatchedIds.Count}";
        ImportText = string.Empty;
        ImportOpen = false;
    }

    [RelayCommand]
    private void ApplyImportReplace()
    {
        var parsed = ModTreeService.ParseModNamesListText(ImportText);
        if (parsed.MatchedIds.Count == 0) return;
        _main.SetAssemblyModIds(parsed.MatchedIds);
        FeedbackBanner = $"Сборка заменена: импортировано модов ({parsed.MatchedIds.Count})";
        ImportText = string.Empty;
        ImportOpen = false;
    }

    [RelayCommand]
    private async Task ScanModsFolderAsync()
    {
        if (_main.PickFolderAsync is null) return;
        var folder = await _main.PickFolderAsync();
        if (string.IsNullOrWhiteSpace(folder)) return;

        var scan = JarMatcherService.ScanFolder(folder);
        if (scan.Canceled) return;

        var match = JarMatcherService.MatchJarFilenamesToMods(scan.JarFiles);
        if (match.MatchedIds.Count > 0)
        {
            _main.SetAssemblyModIds(match.MatchedIds);
            var connectorNote = scan.HasConnectorFolder ? " · .connector найдена" : string.Empty;
            FeedbackBanner = $"Папка просканирована: распознано {match.MatchedIds.Count} из {scan.JarFiles.Count} .jar{connectorNote}";
        }
        else
        {
            FeedbackBanner = $"В папке найдено {scan.JarFiles.Count} .jar, но ни один мод оптимизации из базы не распознан.";
        }
    }

    [RelayCommand]
    private async Task CopyTreeListAsync()
    {
        if (_main.CopyToClipboardAsync is null) return;
        var text = ModTreeService.FormatAssemblyTreeText(_main.AssemblyModIds, includeUrls: true);
        await _main.CopyToClipboardAsync(text);
        FeedbackBanner = "Список со связями и ссылками CurseForge скопирован в буфер обмена.";
    }

    [RelayCommand]
    private async Task CopyNamesListAsync()
    {
        if (_main.CopyToClipboardAsync is null) return;
        var text = ModTreeService.FormatModNamesListText(_main.AssemblyModIds);
        await _main.CopyToClipboardAsync(text);
        FeedbackBanner = "Список названий модов скопирован в буфер обмена.";
    }

    [RelayCommand]
    private void ClearAssembly()
    {
        _main.SetAssemblyModIds([]);
        FeedbackBanner = "Список чекера очищен.";
    }

    [RelayCommand]
    private void LoadPreset(ModPreset? preset)
    {
        if (preset is null) return;
        _main.SetAssemblyModIds(preset.ModIds);
        FeedbackBanner = $"Загружен пресет «{preset.Name.Split('(')[0].Trim()}».";
    }

    [RelayCommand]
    private void AddQuickMod(ModItem? mod)
    {
        if (mod is null) return;
        _main.ToggleAssemblyMod(mod.Id, strict: false);
        AddSearchQuery = string.Empty;
        FeedbackBanner = $"Добавлен мод {mod.Name}.";
    }

    [RelayCommand]
    private void AddMissingSlotLeader(MissingSlotWarning? slot)
    {
        if (slot is null) return;
        _main.ToggleAssemblyMod(slot.LeaderId, strict: true);
        FeedbackBanner = $"Добавлен лидер слота «{slot.GroupTitle}»: {slot.LeaderName}.";
    }

    public void RemoveMod(string modId)
    {
        _main.SetAssemblyModIds(_main.AssemblyModIds.Where(id => id != modId));
    }

    public void RemoveMods(IReadOnlySet<string> removeSet)
    {
        _main.SetAssemblyModIds(_main.AssemblyModIds.Where(id => !removeSet.Contains(id)));
    }

    public void OpenDetails(ModItem mod) => _main.RequestOpenModDetail(mod);

    public void ApplyDiagnosticFix(DiagnosticIssue issue, string? keepId)
    {
        if (issue.Kind is "conflict" or "runtime")
        {
            var winnerId = keepId ?? issue.KeepPreferredId;
            var toRemove = winnerId is not null
                ? issue.BetweenIds.Where(id => id != winnerId).ToHashSet(StringComparer.Ordinal)
                : (issue.RemoveIds ?? []).ToHashSet(StringComparer.Ordinal);
            _main.SetAssemblyModIds(_main.AssemblyModIds.Where(id => !toRemove.Contains(id)));
            var winnerName = winnerId is not null && ModDatabase.ModById.TryGetValue(winnerId, out var wm)
                ? wm.Name : "конфликтный мод";
            FeedbackBanner = $"Применено решение: оставлен {winnerName}.";
            return;
        }

        if (issue.AddIds is { Count: > 0 } && keepId is null)
        {
            _main.SetAssemblyModIds(_main.AssemblyModIds.Concat(issue.AddIds));
            var addedNames = string.Join(", ", issue.AddIds.Select(id => ModDatabase.ModById.TryGetValue(id, out var am) ? am.Name : id));
            FeedbackBanner = $"Добавлены зависимости: {addedNames}.";
            return;
        }

        if (issue.RemoveIds is { Count: > 0 })
        {
            var removeSet = new HashSet<string>(issue.RemoveIds, StringComparer.Ordinal);
            _main.SetAssemblyModIds(_main.AssemblyModIds.Where(id => !removeSet.Contains(id)));
            FeedbackBanner = "Проблемный мод убран.";
        }
    }

    public void Refresh()
    {
        var ids = _main.AssemblyModIds;
        var checkedSet = new HashSet<string>(ids, StringComparer.Ordinal);
        var health = ModAnalyzer.EvaluateAssemblyHealth(ids);
        var diagList = ModAnalyzer.BuildCheckerDiagnostics(ids);

        Diagnostics.Clear();
        foreach (var d in diagList) Diagnostics.Add(new DiagnosticIssueViewModel(d, this));
        CriticalIssuesCount = diagList.Count(d => d.Severity == "red");
        OrangeIssuesCount = diagList.Count(d => d.Severity == "orange");
        YellowIssuesCount = diagList.Count(d => d.Severity == "yellow");
        OnPropertyChanged(nameof(HasAnyIssues));

        MissingCoreSlots.Clear();
        foreach (var slot in health.MissingCoreSlots) MissingCoreSlots.Add(slot);
        OnPropertyChanged(nameof(HasMissingCoreSlots));

        var warningMap = new Dictionary<string, (string Severity, List<string> Messages)>(StringComparer.Ordinal);
        foreach (var w in health.Warnings)
        {
            if (warningMap.TryGetValue(w.ModId, out var prev))
            {
                var sev = prev.Severity == "red" || w.Severity == "red" ? "red"
                    : prev.Severity == "orange" || w.Severity == "orange" ? "orange" : "yellow";
                prev.Messages.Add(w.Message);
                warningMap[w.ModId] = (sev, prev.Messages);
            }
            else
            {
                warningMap[w.ModId] = (w.Severity, [w.Message]);
            }
        }

        var rawRows = new Dictionary<string, (ModItem Mod, CheckerTier Tier, string Reason, string? ViaBridge, string? DubiousReason)>(StringComparer.Ordinal);
        int g = 0, l = 0, y = 0, o = 0, r = 0;

        foreach (var mod in health.SelectedMods)
        {
            warningMap.TryGetValue(mod.Id, out var w);
            var checker = ModAnalyzer.GetCheckerTier(mod, health.ConflictingIdSet, w.Severity, health.RuntimeOnlyConflictIdSet);
            var bridge = ModAnalyzer.GetRecipeBridgeStatus(mod, checkedSet);
            var dubious = ModAnalyzer.GetDubiousModReason(mod, checkedSet);
            var reason = w.Messages?.FirstOrDefault() ?? checker.Reason;
            rawRows[mod.Id] = (mod, checker.Tier, reason, bridge.Compatible ? bridge.ViaBridge : null, dubious);

            switch (checker.Tier)
            {
                case CheckerTier.Green: g++; break;
                case CheckerTier.Lime: l++; break;
                case CheckerTier.Yellow: y++; break;
                case CheckerTier.Orange: o++; break;
                case CheckerTier.Red: r++; break;
            }
        }

        TotalCount = health.SelectedMods.Count;
        GreenCount = g;
        LimeCount = l;
        YellowCount = y;
        OrangeCount = o;
        RedCount = r;

        Rows.Clear();
        bool showDesc = _main.ThemeConfig.ShowDescriptions;
        bool showMeta = _main.ThemeConfig.ShowRowMeta;
        bool showNum = _main.ThemeConfig.ShowRowNumbers;

        bool MatchesFilter(CheckerTier tier) =>
            TierFilter == "all" || tier.ToString().ToLowerInvariant() == TierFilter;

        if (ViewMode == "tree")
        {
            var tree = ModTreeService.OrganizeAssemblyTree(ids);
            int rootIdx = 0;
            foreach (var node in tree)
            {
                if (!rawRows.TryGetValue(node.Parent.Id, out var pData)) continue;
                var visibleChildren = node.Children
                    .Where(c => rawRows.TryGetValue(c.Mod.Id, out var cData) && MatchesFilter(cData.Tier))
                    .ToList();

                if (!MatchesFilter(pData.Tier) && visibleChildren.Count == 0) continue;

                rootIdx++;
                Rows.Add(new CheckerRowViewModel(
                    pData.Mod, pData.Tier, pData.Reason, pData.ViaBridge, pData.DubiousReason,
                    isChild: false, relationLabel: null, rootIndex: rootIdx,
                    showDesc, showMeta, showNum, this));

                foreach (var child in visibleChildren)
                {
                    var cData = rawRows[child.Mod.Id];
                    Rows.Add(new CheckerRowViewModel(
                        cData.Mod, cData.Tier, cData.Reason, cData.ViaBridge, cData.DubiousReason,
                        isChild: true, relationLabel: child.RelationLabel, rootIndex: null,
                        showDesc, showMeta, showNum, this));
                }
            }
        }
        else
        {
            int idx = 0;
            int TierOrder(CheckerTier t) => t switch
            {
                CheckerTier.Red => 0,
                CheckerTier.Orange => 1,
                CheckerTier.Yellow => 2,
                CheckerTier.Lime => 3,
                _ => 4
            };
            var ruCulture = System.Globalization.CultureInfo.GetCultureInfo("ru-RU");
            foreach (var item in rawRows.Values
                .Where(x => MatchesFilter(x.Tier))
                .OrderBy(x => TierOrder(x.Tier))
                .ThenBy(x => x.Mod.Name, StringComparer.Create(ruCulture, true)))
            {
                idx++;
                Rows.Add(new CheckerRowViewModel(
                    item.Mod, item.Tier, item.Reason, item.ViaBridge, item.DubiousReason,
                    isChild: false, relationLabel: null, rootIndex: idx,
                    showDesc, showMeta, showNum, this));
            }
        }
    }
}
