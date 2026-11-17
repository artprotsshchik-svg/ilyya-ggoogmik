using Avalonia.Headless.XUnit;
using ModpackWorkbench.Services;
using ModpackWorkbench.ViewModels;
using ModpackWorkbench.Views;
using Xunit;

namespace ModpackWorkbench.Tests;

public class AvaloniaUiTests
{
    [AvaloniaFact]
    public void MainWindowRendersAndSwitchesTabsCleanly()
    {
        var vm = new MainWindowViewModel(loadFromDisk: false);
        var window = new MainWindow { DataContext = vm };
        window.Show();

        Assert.True(vm.IsAssemblerTab);
        Assert.True(vm.AssemblyCount > 0);
        Assert.NotEmpty(vm.AssemblyBuilder.OptimizationBuckets);

        vm.SelectTabCommand.Execute("catalog");
        Assert.True(vm.IsCatalogTab);
        Assert.NotEmpty(vm.Catalog.Rows);

        vm.SelectTabCommand.Execute("presets");
        Assert.True(vm.IsPresetsTab);
        Assert.NotEmpty(vm.Presets.Presets);

        vm.SelectTabCommand.Execute("checker");
        Assert.True(vm.IsCheckerTab);
        Assert.NotEmpty(vm.Checker.Rows);

        // Load preset into checker and verify tier counts match
        var firstPreset = ModDatabase.ModPresets[0];
        vm.LoadPresetToChecker(firstPreset);
        Assert.Equal(firstPreset.ModIds.Count, vm.Checker.TotalCount);
        Assert.Equal(0, vm.Checker.CriticalIssuesCount);

        window.Close();
    }

    [AvaloniaFact]
    public void ModDetailViewModelResolvesDualPortsAndTogglesAssembly()
    {
        var vm = new MainWindowViewModel(loadFromDisk: false);
        var nvidium = ModDatabase.ModById["nvidium"];
        var detailVm = vm.CreateModDetailViewModel(nvidium);

        Assert.True(detailVm.HasDualPair);
        Assert.NotNull(detailVm.DualForgeMod);
        Assert.NotNull(detailVm.DualFabricMod);

        bool before = detailVm.IsSelectedInAssembly;
        detailVm.ToggleInAssemblyCommand.Execute(null);
        Assert.NotEqual(before, detailVm.IsSelectedInAssembly);
    }
}
