using System.Text.Json.Serialization;

namespace ModpackWorkbench.Models;

public sealed class DbMeta
{
    [JsonPropertyName("updatedAt")]
    public string UpdatedAt { get; set; } = "16.11.2026";

    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; set; } = 2;
}

public sealed class ModItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("platform")]
    public string Platform { get; set; } = string.Empty;

    [JsonPropertyName("installed")]
    public bool Installed { get; set; }

    [JsonPropertyName("lastUpdated")]
    public string? LastUpdated { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "useful";

    [JsonPropertyName("statusLabel")]
    public string StatusLabel { get; set; } = string.Empty;

    [JsonPropertyName("healthTier")]
    public string HealthTier { get; set; } = "yellow";

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("bestAlternative")]
    public string? BestAlternative { get; set; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }

    [JsonPropertyName("warning")]
    public string? Warning { get; set; }

    [JsonPropertyName("curseforgeUrl")]
    public string CurseforgeUrl { get; set; } = string.Empty;

    [JsonPropertyName("groupId")]
    public string? GroupId { get; set; }

    [JsonPropertyName("groupTitle")]
    public string? GroupTitle { get; set; }

    [JsonPropertyName("isGroupLeader")]
    public bool IsGroupLeader { get; set; }

    [JsonPropertyName("isGroupAddon")]
    public bool IsGroupAddon { get; set; }

    [JsonPropertyName("addons")]
    public List<string>? Addons { get; set; }

    [JsonPropertyName("coveredBy")]
    public string? CoveredBy { get; set; }

    [JsonPropertyName("supersededBy")]
    public List<string>? SupersededBy { get; set; }

    [JsonPropertyName("supersededWhenAll")]
    public List<string>? SupersededWhenAll { get; set; }

    [JsonPropertyName("recipeEcosystem")]
    public string? RecipeEcosystem { get; set; }
}

public sealed class ModGroupInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("exclusive")]
    public bool Exclusive { get; set; }

    [JsonPropertyName("leaderId")]
    public string LeaderId { get; set; } = string.Empty;

    [JsonPropertyName("addonIds")]
    public List<string> AddonIds { get; set; } = new();

    [JsonPropertyName("memberIds")]
    public List<string> MemberIds { get; set; } = new();

    [JsonPropertyName("conflictReason")]
    public string? ConflictReason { get; set; }
}

public sealed class IncompatibilityRule
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("conflictingMods")]
    public List<string> ConflictingMods { get; set; } = new();

    [JsonPropertyName("explanation")]
    public string Explanation { get; set; } = string.Empty;

    [JsonPropertyName("recommendation")]
    public string Recommendation { get; set; } = string.Empty;
}

public sealed class ModPreset
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("badge")]
    public string Badge { get; set; } = string.Empty;

    [JsonPropertyName("badgeColor")]
    public string BadgeColor { get; set; } = "emerald";

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("crashRisk")]
    public string CrashRisk { get; set; } = string.Empty;

    [JsonPropertyName("modIds")]
    public List<string> ModIds { get; set; } = new();
}

public sealed class GoldenStandardEntry
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;
}
