namespace ModpackWorkbench.Models;

public sealed record SelectOption<T>(T Key, string Label);

public enum CheckerTier
{
    Green,
    Lime,
    Yellow,
    Orange,
    Red
}

public sealed class ConflictDetection
{
    public IncompatibilityRule Rule { get; init; } = new();
    public List<string> MatchedIds { get; init; } = new();
    public List<string> MatchedNames { get; init; } = new();
}

public sealed class DependencyWarning
{
    public string ModId { get; init; } = string.Empty;
    public string ModName { get; init; } = string.Empty;
    public string Severity { get; init; } = "yellow"; // red | orange | yellow
    public string Message { get; init; } = string.Empty;
    public List<string>? FixModIds { get; init; }
}

public sealed class DiagnosticIssue
{
    public string Id { get; init; } = string.Empty;
    public string Kind { get; init; } = "conflict"; // conflict | runtime | dependency | redundant | platform
    public string Severity { get; init; } = "red"; // red | orange | yellow
    public string Title { get; init; } = string.Empty;
    public List<string> BetweenIds { get; init; } = new();
    public List<string> BetweenNames { get; init; } = new();
    public string Problem { get; init; } = string.Empty;
    public string BestSolution { get; init; } = string.Empty;
    public List<string>? AddIds { get; init; }
    public List<string>? RemoveIds { get; init; }
    public string? KeepPreferredId { get; init; }
}

public sealed class DualPortPair
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string ForgeModId { get; init; } = string.Empty;
    public string ForgeLabel { get; init; } = string.Empty;
    public string ForgeSummary { get; init; } = string.Empty;
    public string FabricModId { get; init; } = string.Empty;
    public string FabricLabel { get; init; } = string.Empty;
    public string FabricSummary { get; init; } = string.Empty;
    public bool SwitchableOnForge { get; init; }
    public List<string>? AltForgeIds { get; init; }
    public List<string>? AltFabricIds { get; init; }
}

public sealed class AssemblyPreferences
{
    public string Gpu { get; set; } = "nvidia"; // nvidia | amd-intel | integrated
    public string Mode { get; set; } = "singleplayer"; // singleplayer | server | hybrid-server
    public bool UseShaders { get; set; }
    public bool UseCreate { get; set; }
    public bool AllowSinytra { get; set; } = true;
    public string PortStrategy { get; set; } = "hybrid"; // native-forge | hybrid | fabric-sinytra
    public string RecipeViewer { get; set; } = "jei"; // none | jei | emi | emi-jei
    public string Goal { get; set; } = "balanced"; // balanced | max-safe | potato
    public string? FocusModId { get; set; }

    public AssemblyPreferences Clone() => new()
    {
        Gpu = Gpu,
        Mode = Mode,
        UseShaders = UseShaders,
        UseCreate = UseCreate,
        AllowSinytra = AllowSinytra,
        PortStrategy = PortStrategy,
        RecipeViewer = RecipeViewer,
        Goal = Goal,
        FocusModId = FocusModId
    };

    public static AssemblyPreferences CreateDefault() => new();
}

public sealed class ExcludedModNote
{
    public string Name { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
}

public sealed class SmartAssemblyResult
{
    public List<string> ModIds { get; init; } = new();
    public List<string> Notes { get; init; } = new();
    public List<ExcludedModNote> Excluded { get; init; } = new();
}

public sealed class SmartToggleResult
{
    public List<string> NextIds { get; init; } = new();
    public string? Notice { get; init; }
    public string NoticeTone { get; init; } = "info"; // info | warn
}

public sealed class MissingSlotWarning
{
    public string GroupId { get; init; } = string.Empty;
    public string GroupTitle { get; init; } = string.Empty;
    public string LeaderId { get; init; } = string.Empty;
    public string LeaderName { get; init; } = string.Empty;
}

public sealed class RecipeBridgeStatus
{
    public bool Compatible { get; init; } = true;
    public string? ViaBridge { get; init; }
    public string? Reason { get; init; }
}

public sealed class SupersederInfo
{
    public List<string> ByNames { get; init; } = new();
    public bool IsActiveInSelection { get; init; }
    public string Reason { get; init; } = string.Empty;
}

public sealed class RestrictionBadge
{
    public string Text { get; init; } = string.Empty;
    public string Tone { get; init; } = "sky"; // red | amber | violet | sky
}

public sealed class AssemblyAnalysisResult
{
    public List<ModItem> SelectedMods { get; init; } = new();
    public List<ConflictDetection> Conflicts { get; init; } = new();
    public HashSet<string> ConflictingIdSet { get; init; } = new();
    public HashSet<string> RuntimeOnlyConflictIdSet { get; init; } = new();
    public List<DependencyWarning> Warnings { get; init; } = new();
}

public sealed class AssemblyHealthResult
{
    public List<ModItem> SelectedMods { get; init; } = new();
    public List<ConflictDetection> Conflicts { get; init; } = new();
    public HashSet<string> ConflictingIdSet { get; init; } = new();
    public HashSet<string> RuntimeOnlyConflictIdSet { get; init; } = new();
    public List<DependencyWarning> Warnings { get; init; } = new();
    public List<MissingSlotWarning> MissingCoreSlots { get; init; } = new();
}

public sealed class GroupMembersByRole
{
    public ModGroupInfo Group { get; init; } = new();
    public ModItem? Leader { get; init; }
    public List<ModItem> Addons { get; init; } = new();
    public List<ModItem> RemovedAnalogs { get; init; } = new();
}

public sealed class AssemblyTreeChild
{
    public ModItem Mod { get; init; } = new();
    public string RelationLabel { get; init; } = string.Empty;
}

public sealed class AssemblyTreeNode
{
    public ModItem Parent { get; init; } = new();
    public List<AssemblyTreeChild> Children { get; init; } = new();
}

public sealed class JarMatchResult
{
    public List<string> MatchedIds { get; init; } = new();
    public List<string> UnmatchedJars { get; init; } = new();
}

public sealed class JarScanResult
{
    public bool Canceled { get; init; }
    public string FolderPath { get; init; } = string.Empty;
    public List<string> JarFiles { get; init; } = new();
    public bool HasConnectorFolder { get; init; }
    public bool HasEmbeddiumSodiumAlias { get; init; }
}

public sealed class ModListImportResult
{
    public List<string> MatchedIds { get; init; } = new();
    public List<string> UnmatchedLines { get; init; } = new();
}
