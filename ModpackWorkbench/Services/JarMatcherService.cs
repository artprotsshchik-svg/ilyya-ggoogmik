using System.Text.RegularExpressions;
using ModpackWorkbench.Models;

namespace ModpackWorkbench.Services;

public static partial class JarMatcherService
{
    private static readonly Dictionary<string, string> JarAliases = new(StringComparer.Ordinal)
    {
        ["embeddium"] = "embeddium",
        ["emberbium"] = "emberbium",
        ["nvidium"] = "nvidium",
        ["alloyium"] = "alloyium",
        ["oculus"] = "oculus",
        ["emberlens"] = "emberlens",
        ["flerovium"] = "flerovium",
        ["immediatelyfast"] = "immediatelyfast",
        ["modernfix"] = "modernfix",
        ["ferritecore"] = "ferritecore",
        ["alltheleaks"] = "alltheleaks-memory-leak-fix",
        ["badoptimizations"] = "badoptimizations",
        ["capabilityfixrevived"] = "capability-fix-revived",
        ["compactf3plus"] = "compact-f3-plus",
        ["culllessleavesreforged"] = "culllessleaves-reforged",
        ["culllessleaves"] = "culllessleaves-reforged",
        ["eber"] = "enhanced-block-entities-reloaded",
        ["enhancedblockentitiesreloaded"] = "enhanced-block-entities-reloaded",
        ["enhancedblockentities"] = "enhanced-block-entities-reloaded",
        ["fastsounds"] = "fast-sounds",
        ["soundgc"] = "soundgc",
        ["particlecore"] = "particle-core",
        ["fzzyconfig"] = "fzzy-config",
        ["kotlinforforge"] = "kotlin-for-forge",
        ["kffmod"] = "kotlin-for-forge",
        ["matlib"] = "matlib",
        ["immersiveoptimization"] = "immersive-optimization",
        ["ixeris"] = "ixeris",
        ["jasione"] = "jasione",
        ["kerria"] = "kerria",
        ["noisiumed"] = "noisiumed",
        ["packetfixer"] = "packet-fixer",
        ["radiumrereforged"] = "radium-re-reforged",
        ["radium"] = "radium-re-reforged",
        ["resourcefulconfig"] = "resourceful-config",
        ["connectorextras"] = "connector-extras",
        ["connector"] = "sinytra-connector",
        ["sinytraconnector"] = "sinytra-connector",
        ["forgifiedfabricapi"] = "forgified-fabric-api",
        ["fabricapi"] = "forgified-fabric-api",
        ["sodiumdynamiclights"] = "sodium-embeddium-dynamic-lights",
        ["sodiumoptionsapi"] = "sodium-embeddium-options-api",
        ["solix"] = "solix",
        ["structurelayoutoptimizer"] = "structure-layout-optimizer",
        ["c2mef"] = "concurrent-chunk-management-engine-forge-c2me",
        ["c2me"] = "concurrent-chunk-management-engine-forge-c2me",
        ["connectivity"] = "connectivity",
        ["cupboard"] = "cupboard",
        ["kryptonreno"] = "krypton-reno",
        ["servercore"] = "servercore",
        ["entityculling"] = "entity-culling",
        ["morecullingreforged"] = "more-culling-reforged",
        ["clumps"] = "clumps",
        ["dynamicfps"] = "dynamic-fps",
        ["acceleratedrendering"] = "accelerated-rendering",
        ["threadtweak"] = "threadtweak-reforged"
    };

    private static readonly Dictionary<string, ModItem> ModTokenIndex = new(StringComparer.Ordinal);

    static JarMatcherService()
    {
        foreach (var mod in ModDatabase.ModsDatabase)
        {
            var idNorm = NonAlnumRegex().Replace(mod.Id, string.Empty).ToLowerInvariant();
            var nameNorm = NonAlnumRegex().Replace(mod.Name, string.Empty).ToLowerInvariant();
            if (idNorm.Length >= 4 && !ModTokenIndex.ContainsKey(idNorm)) ModTokenIndex[idNorm] = mod;
            if (nameNorm.Length >= 4 && !ModTokenIndex.ContainsKey(nameNorm)) ModTokenIndex[nameNorm] = mod;
        }
    }

    [GeneratedRegex(@"\.jar(\.disabled)?$", RegexOptions.IgnoreCase)]
    private static partial Regex JarSuffixRegex();

    [GeneratedRegex(@"[-_+]mc?1\.\d+(\.\d+)?", RegexOptions.IgnoreCase)]
    private static partial Regex McVersionRegex();

    [GeneratedRegex(@"[-_+](forge|neoforge|fabric|quilt|universal)\b", RegexOptions.IgnoreCase)]
    private static partial Regex LoaderSuffixRegex();

    [GeneratedRegex(@"[-_+ ]v?\d+\.\d+.*$", RegexOptions.IgnoreCase)]
    private static partial Regex ModVersionTailRegex();

    [GeneratedRegex(@"[^a-z0-9]+")]
    private static partial Regex NonAlnumRegex();

    private static string NormalizeJarToken(string raw)
    {
        var s = raw.ToLowerInvariant();
        s = JarSuffixRegex().Replace(s, string.Empty);
        s = McVersionRegex().Replace(s, string.Empty);
        s = LoaderSuffixRegex().Replace(s, string.Empty);
        s = ModVersionTailRegex().Replace(s, string.Empty);
        return NonAlnumRegex().Replace(s, string.Empty);
    }

    public static JarMatchResult MatchJarFilenamesToMods(IEnumerable<string> jarFiles)
    {
        var matchedSet = new HashSet<string>(StringComparer.Ordinal);
        var unmatchedJars = new List<string>();

        foreach (var jar in jarFiles)
        {
            var rawLower = jar.ToLowerInvariant();
            if (rawLower.Contains("c2me", StringComparison.Ordinal) && rawLower.Contains("fabric", StringComparison.Ordinal))
            {
                matchedSet.Add("c2me-concurrent-chunk-management-engine");
                continue;
            }
            if (rawLower.Contains("vmp", StringComparison.Ordinal) && rawLower.Contains("fabric", StringComparison.Ordinal))
            {
                matchedSet.Add("very-many-players-vmp");
                continue;
            }

            var token = NormalizeJarToken(jar);
            if (string.IsNullOrEmpty(token))
            {
                unmatchedJars.Add(jar);
                continue;
            }

            if (JarAliases.TryGetValue(token, out var directAliasId))
            {
                matchedSet.Add(directAliasId);
                continue;
            }

            if (ModTokenIndex.TryGetValue(token, out var exact))
            {
                matchedSet.Add(exact.Id);
                continue;
            }

            bool partialHit = false;
            foreach (var (aliasKey, modId) in JarAliases)
            {
                if (token.StartsWith(aliasKey, StringComparison.Ordinal) && token.Length - aliasKey.Length <= 6)
                {
                    matchedSet.Add(modId);
                    partialHit = ModDatabase.ModById.ContainsKey(modId);
                    break;
                }
            }

            if (!partialHit)
                unmatchedJars.Add(jar);
        }

        return new JarMatchResult
        {
            MatchedIds = matchedSet.ToList(),
            UnmatchedJars = unmatchedJars
        };
    }

    public static JarScanResult ScanFolder(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
        {
            return new JarScanResult { Canceled = true };
        }

        var dirInfo = new DirectoryInfo(folderPath);
        var jarFiles = dirInfo.GetFiles("*.jar", SearchOption.TopDirectoryOnly)
            .Select(f => f.Name)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();

        bool hasConnectorFolder = dirInfo.GetDirectories()
            .Any(d => d.Name.Contains("connector", StringComparison.OrdinalIgnoreCase));

        bool hasEmbeddiumSodiumAlias = jarFiles.Any(name =>
        {
            var low = name.ToLowerInvariant();
            return low.Contains("embeddium", StringComparison.Ordinal) ||
                   low.Contains("sodium", StringComparison.Ordinal);
        });

        return new JarScanResult
        {
            Canceled = false,
            FolderPath = folderPath,
            JarFiles = jarFiles,
            HasConnectorFolder = hasConnectorFolder,
            HasEmbeddiumSodiumAlias = hasEmbeddiumSodiumAlias
        };
    }
}
