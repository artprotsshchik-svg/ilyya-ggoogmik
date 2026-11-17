using ModpackWorkbench.Models;
using ModpackWorkbench.Services;
using Xunit;

namespace ModpackWorkbench.Tests;

public class ModLogicTests
{
    [Fact]
    public void AllModsHaveValidCurseForgeUrlsAndGroupCoverage()
    {
        Assert.True(ModDatabase.ModsDatabase.Count > 50);
        var allGroupMemberIds = ModDatabase.ModGroups.SelectMany(g => g.MemberIds).ToHashSet(StringComparer.Ordinal);
        foreach (var mod in ModDatabase.ModsDatabase)
        {
            Assert.False(string.IsNullOrWhiteSpace(mod.Id));
            Assert.False(string.IsNullOrWhiteSpace(mod.Name));
            Assert.StartsWith("https://www.curseforge.com/", mod.CurseforgeUrl, StringComparison.Ordinal);
            Assert.Contains(mod.Id, allGroupMemberIds);
        }
    }

    [Fact]
    public void AllPresetsHaveZeroConflictsAndZeroRedOrYellowWarnings()
    {
        Assert.NotEmpty(ModDatabase.ModPresets);
        foreach (var preset in ModDatabase.ModPresets)
        {
            var analysis = ModAnalyzer.AnalyzeSelection(preset.ModIds);
            Assert.Empty(analysis.Conflicts);
            Assert.DoesNotContain(analysis.Warnings, w => w.Severity is "red" or "yellow");
        }
    }

    [Fact]
    public void IncompatibleMixTriggersExpectedConflictsAndRedWarnings()
    {
        var analysis = ModAnalyzer.AnalyzeSelection([
            "embeddium", "xenon", "optifine", "nvidium", "oculus", "lithium"
        ]);
        Assert.True(analysis.Conflicts.Count >= 2);
        Assert.Contains(analysis.Warnings, w => w.Severity == "red" && w.ModId == "lithium");
        Assert.Contains(analysis.Warnings, w => w.Severity == "red" && w.ModId == "nvidium");
    }

    [Fact]
    public void FabricOriginalsAreFlaggedRedWhileForgePortsAreGreen()
    {
        var lithium = ModDatabase.ModById["lithium"];
        var radium = ModDatabase.ModById["radium-re-reforged"];
        var krypton = ModDatabase.ModById["krypton"];
        var kryptonReno = ModDatabase.ModById["krypton-reno"];

        Assert.Equal(CheckerTier.Red, ModAnalyzer.GetBaseHealthTier(lithium));
        Assert.Equal(CheckerTier.Green, ModAnalyzer.GetBaseHealthTier(radium));
        Assert.Equal(CheckerTier.Red, ModAnalyzer.GetBaseHealthTier(krypton));
        Assert.Equal(CheckerTier.Green, ModAnalyzer.GetBaseHealthTier(kryptonReno));
    }

    [Fact]
    public void NvidiumConflictsWithAlloyiumAndOculusWhileEmberlensNeedsEmberbium()
    {
        var nvidiumAlloyium = ModAnalyzer.AnalyzeSelection(["embeddium", "nvidium", "alloyium", "sinytra-connector", "forgified-fabric-api"]);
        Assert.NotEmpty(nvidiumAlloyium.Conflicts);

        var emberlensMissing = ModAnalyzer.AnalyzeSelection(["emberlens"]);
        Assert.Contains(emberlensMissing.Warnings, w => w.ModId == "emberlens" && w.FixModIds?.Contains("emberbium") == true);
    }

    [Fact]
    public void BuildSmartAssemblyAdaptsToHardwareAndProducesZeroConflicts()
    {
        var configs = new[]
        {
            new AssemblyPreferences { Gpu = "nvidia", Mode = "singleplayer", Goal = "balanced", UseShaders = false, AllowSinytra = true },
            new AssemblyPreferences { Gpu = "nvidia", Mode = "singleplayer", Goal = "max-safe", UseShaders = true, AllowSinytra = true },
            new AssemblyPreferences { Gpu = "amd-intel", Mode = "server", Goal = "max-safe", UseShaders = false, AllowSinytra = false },
            new AssemblyPreferences { Gpu = "integrated", Mode = "singleplayer", Goal = "potato", UseShaders = true, AllowSinytra = false },
            new AssemblyPreferences { Gpu = "nvidia", Mode = "hybrid-server", Goal = "balanced", UseCreate = true, RecipeViewer = "emi" }
        };

        foreach (var cfg in configs)
        {
            var res = ModBuilder.BuildSmartAssembly(cfg);
            Assert.True(res.ModIds.Count >= 20);
            var analysis = ModAnalyzer.AnalyzeSelection(res.ModIds);
            Assert.Empty(analysis.Conflicts);
            Assert.DoesNotContain(analysis.Warnings, w => w.Severity == "red");
        }
    }

    [Fact]
    public void ToggleModSmartPullsDependenciesAndCascadesRemovals()
    {
        var initial = new[] { "embeddium" };
        var added = ModBuilder.ToggleModSmart(initial, "nvidium", strict: true);
        Assert.Contains("nvidium", added.NextIds);
        Assert.Contains("sinytra-connector", added.NextIds);
        Assert.Contains("forgified-fabric-api", added.NextIds);

        var removed = ModBuilder.ToggleModSmart(added.NextIds, "sinytra-connector", strict: true);
        Assert.DoesNotContain("sinytra-connector", removed.NextIds);
        Assert.DoesNotContain("nvidium", removed.NextIds);
    }

    [Fact]
    public void SmartFixAssemblyRepairsBrokenMixAndEvaluatesMissingSlots()
    {
        var fixedIds = ModBuilder.SmartFixAssembly(["embeddium", "rubidium", "lithium", "optifine"]);
        Assert.Contains("embeddium", fixedIds);
        Assert.Contains("radium-re-reforged", fixedIds);
        Assert.DoesNotContain("rubidium", fixedIds);
        Assert.DoesNotContain("optifine", fixedIds);
        Assert.DoesNotContain("lithium", fixedIds);

        var health = ModAnalyzer.EvaluateAssemblyHealth(["embeddium"]);
        Assert.NotEmpty(health.MissingCoreSlots);
    }

    [Fact]
    public void GoldenStandardResolvesAllEntries()
    {
        Assert.NotEmpty(ModDatabase.GoldenStandardMods);
        foreach (var entry in ModDatabase.GoldenStandardMods)
        {
            var found = ModAnalyzer.FindModByDisplayName(entry.Name);
            Assert.NotNull(found);
        }
    }

    [Fact]
    public void TreeAndListImportRoundTripWorks()
    {
        var ids = new[] { "embeddium", "alloyium", "oculus", "ferritecore", "modernfix" };
        var text = ModTreeService.FormatAssemblyTreeText(ids, includeUrls: true);
        Assert.Contains("Embeddium", text);
        Assert.Contains("↳ Alloyium", text);

        var parsed = ModTreeService.ParseModNamesListText(text);
        foreach (var id in ids)
        {
            Assert.Contains(id, parsed.MatchedIds);
        }
    }

    [Fact]
    public void JarMatcherResolvesCommonAndDualPortJars()
    {
        var res = JarMatcherService.MatchJarFilenamesToMods([
            "embeddium-0.3.31+mc1.20.1.jar",
            "nvidium-0.2.6-beta.jar",
            "c2me-fabric-mc1.20.1-0.2.0.jar",
            "ferritecore-6.0.1-forge.jar",
            "unknown-custom-mod-1.0.jar"
        ]);

        Assert.Contains("embeddium", res.MatchedIds);
        Assert.Contains("nvidium", res.MatchedIds);
        Assert.Contains("c2me-concurrent-chunk-management-engine", res.MatchedIds);
        Assert.Contains("ferritecore", res.MatchedIds);
        Assert.Contains("unknown-custom-mod-1.0.jar", res.UnmatchedJars);
    }

    [Fact]
    public void RussianPurposeSearchFindsRelevantMods()
    {
        var fpsResults = ModAnalyzer.SearchModsByQuery("фпс", 10);
        Assert.NotEmpty(fpsResults);

        var leakResults = ModAnalyzer.SearchModsByQuery("утечки памяти", 10);
        Assert.Contains(leakResults, m => m.Id == "alltheleaks-memory-leak-fix");
    }

    [Fact]
    public void ThemeSanitizationClampsOutOfRangeValues()
    {
        var raw = new BuilderThemeConfig
        {
            Mode = "invalid",
            Angle = 999,
            Saturation = -50,
            PanelTransparency = 250,
            FractalOpacity = -10,
            FontScale = 123,
            AccentColor = "not-a-color"
        };
        var sanitized = ThemeService.SanitizeBuilderThemeConfig(raw);
        Assert.Equal("dark", sanitized.Mode);
        Assert.Equal(360, sanitized.Angle);
        Assert.Equal(0, sanitized.Saturation);
        Assert.Equal(100, sanitized.PanelTransparency);
        Assert.Equal(0, sanitized.FractalOpacity);
        Assert.Equal(100, sanitized.FontScale);
        Assert.Equal("auto", sanitized.AccentColor);
    }

    [Fact]
    public void PersistenceSanitizesUnknownModIdsAndTabs()
    {
        var json = """
        {
          "activeTab": "invalid-tab",
          "checkedModIds": ["embeddium", "non-existent-id"],
          "builderPrefs": { "gpu": "unknown", "mode": "server" }
        }
        """;
        var parsed = PersistenceService.ParsePersistedState(json);
        Assert.NotNull(parsed);
        Assert.Equal("assembler", parsed.ActiveTab);
        Assert.Equal(["embeddium"], parsed.CheckedModIds);
        Assert.Equal("nvidia", parsed.BuilderPrefs.Gpu);
        Assert.Equal("server", parsed.BuilderPrefs.Mode);
    }
}
