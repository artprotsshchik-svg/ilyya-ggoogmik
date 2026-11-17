using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ModpackWorkbench.Models;
using ModpackWorkbench.Services;

namespace ModpackWorkbench.ViewModels;

public sealed partial class PresetCardViewModel : ObservableObject
{
    private readonly PresetsViewModel _parent;

    public ModPreset Preset { get; }
    public string Name => Preset.Name;
    public string Badge => Preset.Badge;
    public string Description => Preset.Description;
    public string CrashRisk => Preset.CrashRisk;
    public int ModCount => Preset.ModIds.Count;
    public string ExpandLabel => IsExpanded ? $"Скрыть состав ({ModCount})" : $"Показать состав ({ModCount} модов)";

    public IReadOnlyList<ModItem> ResolvedMods { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExpandLabel))]
    private bool _isExpanded;

    public PresetCardViewModel(ModPreset preset, PresetsViewModel parent)
    {
        Preset = preset;
        _parent = parent;
        ResolvedMods = preset.ModIds
            .Select(id => ModDatabase.ModById.GetValueOrDefault(id))
            .OfType<ModItem>()
            .ToList();
    }

    [RelayCommand]
    private void ToggleExpanded() => IsExpanded = !IsExpanded;

    [RelayCommand]
    private void LoadToBuilder() => _parent.LoadToBuilder(Preset);

    [RelayCommand]
    private void LoadToChecker() => _parent.LoadToChecker(Preset);

    [RelayCommand]
    private void OpenMod(ModItem? mod)
    {
        if (mod is not null) _parent.OpenModDetail(mod);
    }
}

public sealed partial class GoldenStandardItemViewModel : ObservableObject
{
    private readonly PresetsViewModel _parent;
    public GoldenStandardEntry Entry { get; }
    public ModItem? ResolvedMod { get; }
    public string Name => Entry.Name;
    public string Role => Entry.Role;
    public bool HasResolvedMod => ResolvedMod is not null;

    public GoldenStandardItemViewModel(GoldenStandardEntry entry, PresetsViewModel parent)
    {
        Entry = entry;
        _parent = parent;
        ResolvedMod = ModAnalyzer.FindModByDisplayName(entry.Name);
    }

    [RelayCommand]
    private void OpenDetail()
    {
        if (ResolvedMod is not null) _parent.OpenModDetail(ResolvedMod);
    }
}

public sealed partial class PresetsViewModel : ObservableObject
{
    private readonly MainWindowViewModel _main;

    public ObservableCollection<PresetCardViewModel> Presets { get; } = new();
    public ObservableCollection<GoldenStandardItemViewModel> GoldenStandard { get; } = new();

    public PresetsViewModel(MainWindowViewModel main)
    {
        _main = main;
        foreach (var p in ModDatabase.ModPresets)
            Presets.Add(new PresetCardViewModel(p, this));

        foreach (var g in ModDatabase.GoldenStandardMods)
            GoldenStandard.Add(new GoldenStandardItemViewModel(g, this));
    }

    public void LoadToBuilder(ModPreset preset) => _main.LoadPresetToBuilder(preset);
    public void LoadToChecker(ModPreset preset) => _main.LoadPresetToChecker(preset);
    public void OpenModDetail(ModItem mod) => _main.RequestOpenModDetail(mod);
}
