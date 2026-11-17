using System.Reflection;
using System.Text.Json;
using ModpackWorkbench.Models;

namespace ModpackWorkbench.Services;

public static class ModDatabase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static string DatabaseUpdatedAt { get; }
    public static IReadOnlyList<ModItem> ModsDatabase { get; }
    public static IReadOnlyList<ModGroupInfo> ModGroups { get; }
    public static IReadOnlyList<ModPreset> ModPresets { get; }
    public static IReadOnlyList<IncompatibilityRule> IncompatibilityRules { get; }
    public static IReadOnlyList<GoldenStandardEntry> GoldenStandardMods { get; }
    public static IReadOnlyDictionary<string, string> ModLastUpdatedMap { get; }
    public static IReadOnlyDictionary<string, ModItem> ModById { get; }
    public static IReadOnlyDictionary<string, ModGroupInfo> GroupById { get; }

    static ModDatabase()
    {
        var meta = LoadEmbeddedJson<DbMeta>("meta.json") ?? new DbMeta();
        DatabaseUpdatedAt = string.IsNullOrWhiteSpace(meta.UpdatedAt) ? "16.11.2026" : meta.UpdatedAt;

        var allMods = new List<ModItem>();
        allMods.AddRange(LoadEmbeddedJson<List<ModItem>>("mods-render.json") ?? []);
        allMods.AddRange(LoadEmbeddedJson<List<ModItem>>("mods-memory-cpu.json") ?? []);
        allMods.AddRange(LoadEmbeddedJson<List<ModItem>>("mods-world-tps.json") ?? []);
        allMods.AddRange(LoadEmbeddedJson<List<ModItem>>("mods-net-recipes-misc.json") ?? []);
        ModsDatabase = allMods;

        ModGroups = LoadEmbeddedJson<List<ModGroupInfo>>("groups.json") ?? [];
        ModPresets = LoadEmbeddedJson<List<ModPreset>>("presets.json") ?? [];
        IncompatibilityRules = LoadEmbeddedJson<List<IncompatibilityRule>>("rules.json") ?? [];
        GoldenStandardMods = LoadEmbeddedJson<List<GoldenStandardEntry>>("golden.json") ?? [];
        ModLastUpdatedMap = LoadEmbeddedJson<Dictionary<string, string>>("mod-dates.json")
            ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var modMap = new Dictionary<string, ModItem>(StringComparer.Ordinal);
        foreach (var m in ModsDatabase)
        {
            modMap[m.Id] = m;
        }
        ModById = modMap;

        var groupMap = new Dictionary<string, ModGroupInfo>(StringComparer.Ordinal);
        foreach (var g in ModGroups)
        {
            groupMap[g.Id] = g;
        }
        GroupById = groupMap;
    }

    public static string? GetModUpdatedLabel(ModItem mod)
    {
        if (!string.IsNullOrWhiteSpace(mod.LastUpdated))
            return mod.LastUpdated;
        return ModLastUpdatedMap.TryGetValue(mod.Id, out var label) ? label : null;
    }

    private static T? LoadEmbeddedJson<T>(string fileName)
    {
        var asm = Assembly.GetExecutingAssembly();
        var suffix = $".{fileName}";
        var resName = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));

        if (resName is not null)
        {
            using var stream = asm.GetManifestResourceStream(resName);
            if (stream is not null)
            {
                return JsonSerializer.Deserialize<T>(stream, JsonOptions);
            }
        }

        // Fallback to disk path relative to BaseDirectory (useful for development/tests)
        var diskPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Data", "db", fileName);
        if (File.Exists(diskPath))
        {
            using var fs = File.OpenRead(diskPath);
            return JsonSerializer.Deserialize<T>(fs, JsonOptions);
        }

        return default;
    }
}
