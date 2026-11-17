using System.Text.Json;
using ModpackWorkbench.Models;

namespace ModpackWorkbench.Services;

public static class PersistenceService
{
    private static readonly HashSet<string> ValidTabs = new(StringComparer.Ordinal)
    {
        "catalog", "assembler", "presets", "checker"
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static string StateFilePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ModpackWorkbench",
            "workbench-state.json");

    public static string FormatRuDate(DateTime? date = null)
    {
        var d = date ?? DateTime.Now;
        return d.ToString("dd.MM.yyyy");
    }

    public static List<string>? SanitizeModIds(IEnumerable<string>? ids)
    {
        if (ids is null) return null;
        return ids
            .Where(id => !string.IsNullOrEmpty(id) && ModDatabase.ModById.ContainsKey(id))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    public static AssemblyPreferences SanitizeBuilderPrefs(AssemblyPreferences? r)
    {
        var def = AssemblyPreferences.CreateDefault();
        if (r is null) return def;

        return new AssemblyPreferences
        {
            Gpu = r.Gpu is "nvidia" or "amd-intel" or "integrated" ? r.Gpu : def.Gpu,
            Mode = r.Mode is "singleplayer" or "server" or "hybrid-server" ? r.Mode : def.Mode,
            Goal = r.Goal is "balanced" or "max-safe" or "potato" ? r.Goal : def.Goal,
            PortStrategy = r.PortStrategy is "native-forge" or "hybrid" or "fabric-sinytra" ? r.PortStrategy : def.PortStrategy,
            RecipeViewer = r.RecipeViewer is "none" or "jei" or "emi" or "emi-jei" ? r.RecipeViewer : def.RecipeViewer,
            UseShaders = r.UseShaders,
            UseCreate = r.UseCreate,
            AllowSinytra = r.AllowSinytra,
            FocusModId = !string.IsNullOrEmpty(r.FocusModId) && ModDatabase.ModById.ContainsKey(r.FocusModId)
                ? r.FocusModId
                : null
        };
    }

    public static PersistedAppState? ParsePersistedState(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            var raw = JsonSerializer.Deserialize<PersistedAppState>(json, JsonOptions);
            if (raw is null) return null;
            return SanitizeState(raw);
        }
        catch
        {
            return null;
        }
    }

    public static PersistedAppState SanitizeState(PersistedAppState raw)
    {
        return new PersistedAppState
        {
            DbUpdatedAt = !string.IsNullOrWhiteSpace(raw.DbUpdatedAt) ? raw.DbUpdatedAt.Trim() : ModDatabase.DatabaseUpdatedAt,
            LastSavedDate = !string.IsNullOrWhiteSpace(raw.LastSavedDate) ? raw.LastSavedDate.Trim() : FormatRuDate(),
            ActiveTab = ValidTabs.Contains(raw.ActiveTab) ? raw.ActiveTab : "assembler",
            CheckedModIds = SanitizeModIds(raw.CheckedModIds) ?? [],
            BuilderModIds = SanitizeModIds(raw.BuilderModIds) ?? [],
            BuilderPrefs = SanitizeBuilderPrefs(raw.BuilderPrefs),
            FocusModId = !string.IsNullOrEmpty(raw.FocusModId) && ModDatabase.ModById.ContainsKey(raw.FocusModId)
                ? raw.FocusModId
                : null
        };
    }

    public static PersistedAppState? LoadAppState(string? customPath = null)
    {
        try
        {
            var path = customPath ?? StateFilePath;
            if (!File.Exists(path)) return null;
            var raw = File.ReadAllText(path);
            return ParsePersistedState(raw);
        }
        catch
        {
            return null;
        }
    }

    public static bool SaveAppState(PersistedAppState state, string? customPath = null)
    {
        try
        {
            var path = customPath ?? StateFilePath;
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(SanitizeState(state), JsonOptions);
            File.WriteAllText(path, json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
