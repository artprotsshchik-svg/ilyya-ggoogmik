using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ModpackWorkbench.Models;
using ModpackWorkbench.Services;

namespace ModpackWorkbench.ViewModels;

public sealed partial class ModDetailViewModel : ObservableObject
{
    private readonly MainWindowViewModel _main;

    public ModItem Mod { get; }
    public string UpdatedLabel { get; }
    public string HealthLabel { get; }
    public string HealthBadgeTone { get; }
    public GroupMembersByRole? GroupInfo { get; }
    public DualPortPair? DualPair { get; }
    public ModItem? DualForgeMod { get; }
    public ModItem? DualFabricMod { get; }
    public SupersederInfo? Superseder { get; }
    public RecipeBridgeStatus BridgeStatus { get; }
    public IReadOnlyList<ModItem> CoveredChildMods { get; }
    public IReadOnlyList<ModItem> Dependencies { get; }

    public bool IsSelectedInAssembly => _main.AssemblyModIds.Contains(Mod.Id, StringComparer.Ordinal);
    public string ToggleButtonText => IsSelectedInAssembly ? "Убрать из сборки" : "Добавить в сборку";
    public bool HasWarning => !string.IsNullOrWhiteSpace(Mod.Warning);
    public bool HasBestAlternative => !string.IsNullOrWhiteSpace(Mod.BestAlternative);
    public bool HasCoveredBy => !string.IsNullOrWhiteSpace(Mod.CoveredBy);
    public bool HasDualPair => DualPair is not null;
    public bool HasDependencies => Dependencies.Count > 0;
    public bool HasCoveredChildren => CoveredChildMods.Count > 0;
    public string TagsText => Mod.Tags is { Count: > 0 } ? string.Join(" · ", Mod.Tags) : string.Empty;
    public bool HasTags => !string.IsNullOrEmpty(TagsText);

    public event Action? CloseRequested;

    public ModDetailViewModel(ModItem mod, MainWindowViewModel main)
    {
        Mod = mod;
        _main = main;
        UpdatedLabel = ModDatabase.GetModUpdatedLabel(mod) ?? "—";

        var tier = ModAnalyzer.GetBaseHealthTier(mod);
        (HealthLabel, HealthBadgeTone) = tier switch
        {
            CheckerTier.Red => ("Конфликт / плацебо", "red"),
            CheckerTier.Orange => ("Риск / оговорка", "orange"),
            CheckerTier.Green => ("Стабильно", "green"),
            CheckerTier.Lime => ("Сносно / подходит", "lime"),
            _ => ("Условно / так себе", "yellow")
        };

        GroupInfo = !string.IsNullOrEmpty(mod.GroupId)
            ? ModAnalyzer.GetGroupMembersByRole(mod.GroupId)
            : null;

        DualPair = ModAnalyzer.GetDualPortPairForMod(mod.Id);
        if (DualPair is not null)
        {
            ModDatabase.ModById.TryGetValue(DualPair.ForgeModId, out var fm);
            ModDatabase.ModById.TryGetValue(DualPair.FabricModId, out var fbm);
            DualForgeMod = fm;
            DualFabricMod = fbm;
        }

        var contextSet = new HashSet<string>(_main.AssemblyModIds, StringComparer.Ordinal);
        Superseder = ModAnalyzer.GetModSuperseder(mod, contextSet, _main.BuilderPrefs, true);
        BridgeStatus = ModAnalyzer.GetRecipeBridgeStatus(mod, contextSet, _main.BuilderPrefs);

        CoveredChildMods = ModDatabase.ModsDatabase
            .Where(item => item.Id != mod.Id &&
                           !string.IsNullOrEmpty(item.CoveredBy) &&
                           (item.CoveredBy == mod.Name ||
                            item.CoveredBy.Contains(mod.Name, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        Dependencies = ModAnalyzer.GetDirectDependencies(mod.Id, contextSet)
            .Select(id => ModDatabase.ModById.GetValueOrDefault(id))
            .OfType<ModItem>()
            .ToList();
    }

    [RelayCommand]
    private void ToggleInAssembly()
    {
        _main.ToggleAssemblyMod(Mod.Id);
        OnPropertyChanged(nameof(IsSelectedInAssembly));
        OnPropertyChanged(nameof(ToggleButtonText));
    }

    [RelayCommand]
    private void ChooseDualVariant(ModItem? targetMod)
    {
        if (targetMod is null) return;
        _main.UpdateBuilderPrefs(p => p.FocusModId = targetMod.Id);
        var res = ModBuilder.BuildSmartAssembly(_main.BuilderPrefs);
        _main.SetAssemblyModIds(res.ModIds);
        OnPropertyChanged(nameof(IsSelectedInAssembly));
        OnPropertyChanged(nameof(ToggleButtonText));
    }

    [RelayCommand]
    private void OpenCurseForge()
    {
        ShellLauncher.OpenUrl(Mod.CurseforgeUrl);
    }

    [RelayCommand]
    private void Close()
    {
        CloseRequested?.Invoke();
    }
}
