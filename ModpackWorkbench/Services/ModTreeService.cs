using System.Text.RegularExpressions;
using ModpackWorkbench.Models;

namespace ModpackWorkbench.Services;

public static partial class ModTreeService
{
    private static readonly Dictionary<string, (string[] Parents, string Relation)> AddonParentPriority = new(StringComparer.Ordinal)
    {
        ["alloyium"] = (["embeddium", "emberbium"], "Аддон рендера"),
        ["flerovium"] = (["embeddium", "emberbium"], "Аддон рендера"),
        ["embeddium-extras"] = (["embeddium"], "Аддон настроек"),
        ["sodium-chloride"] = (["embeddium"], "Аддон Embeddium"),
        ["sodium-embeddium-options-api"] = (["embeddium", "emberbium"], "Options API"),
        ["sodium-embeddium-dynamic-lights"] = (["sodium-embeddium-options-api", "embeddium", "emberbium"], "Дин. освещение"),
        ["oculus"] = (["embeddium"], "Шейдеры"),
        ["faster-iris-shadow-mapper"] = (["oculus", "embeddium"], "Оптимизация теней"),
        ["gputape"] = (["oculus", "emberlens", "embeddium", "emberbium"], "Память GPU"),
        ["lightstar"] = (["embeddium"], "Кэш света"),
        ["amd-fsr-upscaler-render-scale-for-embeddium"] = (["embeddium"], "Апскейлер FSR"),
        ["accelerated-rendering"] = (["embeddium", "emberbium"], "GPU-инстансинг"),
        ["accelerated-rendering-turned"] = (["embeddium", "emberbium"], "GPU-инстансинг"),
        ["accelerated-recoiling"] = (["embeddium", "emberbium"], "GPU-инстансинг"),
        ["immediatelyfast"] = (["embeddium", "emberbium"], "Рендер GUI/HUD"),
        ["emberlens"] = (["emberbium"], "Шейдеры Emberbium"),
        ["forgified-fabric-api"] = (["sinytra-connector"], "Мост Fabric API"),
        ["connector-extras"] = (["sinytra-connector"], "Мост совместимости"),
        ["nvidium"] = (["sinytra-connector", "embeddium", "emberbium"], "Mesh Shaders (Sinytra)"),
        ["nvidium-forged"] = (["nvidium", "sinytra-connector", "embeddium", "emberbium"], "Порт Nvidium"),
        ["c2me-concurrent-chunk-management-engine"] = (["sinytra-connector"], "Чанки Fabric (Sinytra)"),
        ["very-many-players-vmp"] = (["sinytra-connector"], "Онлайн Fabric (Sinytra)"),
        ["voxy"] = (["sinytra-connector", "embeddium"], "LOD Fabric (Sinytra)"),
        ["distant-horizons-memory-fix-dh"] = (["distant-horizons"], "Фикс памяти DH"),
        ["solix-chunk-lighting"] = (["solix"], "Аддон Solix"),
        ["matlib"] = (["enhanced-block-entities-reloaded"], "Зависимость EBER"),
        ["kotlin-for-forge"] = (["particle-core", "fzzy-config"], "Зависимость Kotlin"),
        ["fzzy-config"] = (["particle-core"], "Конфиг Particle Core"),
        ["soundgc"] = (["fast-sounds"], "Очистка звука"),
        ["reload-looping-sound-fix"] = (["fast-sounds"], "Фикс зацикливания звука"),
        ["vanillin"] = (["flywheel"], "Модуль Flywheel"),
        ["cupboard"] = (["connectivity"], "Зависимость Connectivity"),
        ["chunky-border"] = (["chunky"], "Аддон границ"),
        ["drd-dynamic-render-distance"] = (["dynamicrenderdistance"], "Вариант DRD"),
        ["config-preset-switch-low-medium-high-f10"] = (["dynamicrenderdistance"], "Пресеты F10"),
        ["tmrv-updated"] = (["emi"], "Мост плагинов JEI"),
        ["emi-loot"] = (["emi"], "Плагин дропа"),
        ["emi-ores"] = (["emi"], "Плагин руд"),
        ["emiffect"] = (["emi"], "Плагин эффектов"),
        ["emi-trades"] = (["emi"], "Плагин торгов"),
        ["emi-professions"] = (["emi"], "Плагин профессий"),
        ["jeioptimizer"] = (["just-enough-items-jei"], "Оптимизатор JEI"),
        ["just-enough-freezes"] = (["just-enough-items-jei"], "Фикс фризов JEI"),
        ["just-enough-threads"] = (["just-enough-items-jei"], "Потоки JEI"),
        ["jeir-jei-rewired"] = (["just-enough-items-jei"], "Поиск JEI"),
        ["just-enough-resources-jer"] = (["just-enough-items-jei", "tmrv-updated", "emi"], "Плагин JEI"),
        ["just-enough-professions-jep"] = (["just-enough-items-jei", "tmrv-updated", "emi"], "Плагин JEI"),
        ["just-enough-effect-descriptions-jeed"] = (["just-enough-items-jei", "tmrv-updated", "emi"], "Плагин JEI"),
        ["fastali"] = (["advanced-loot-info-ali"], "Оптимизатор ALI")
    };

    private static readonly Dictionary<string, int> RootPriorityOrder = new(StringComparer.Ordinal)
    {
        ["emberbium"] = 10,
        ["embeddium"] = 11,
        ["oculus"] = 20,
        ["emberlens"] = 21,
        ["sinytra-connector"] = 30,
        ["distant-horizons"] = 35,
        ["solix"] = 40,
        ["enhanced-block-entities-reloaded"] = 45,
        ["particle-core"] = 50,
        ["fast-sounds"] = 55,
        ["flywheel"] = 60,
        ["emi"] = 65,
        ["just-enough-items-jei"] = 66,
        ["connectivity"] = 70,
        ["chunky"] = 75,
        ["dynamicrenderdistance"] = 80
    };

    [GeneratedRegex(@"^\d+[.)]\s*")]
    private static partial Regex NumberingPrefixRegex();

    [GeneratedRegex(@"^[↳•\-*]+\s*")]
    private static partial Regex BulletPrefixRegex();

    [GeneratedRegex(@"\s+—\s+https?://\S+$", RegexOptions.IgnoreCase)]
    private static partial Regex TrailingUrlRegex();

    [GeneratedRegex(@"\s+\[обн\.[^\]]*\]", RegexOptions.IgnoreCase)]
    private static partial Regex UpdatedBracketRegex();

    [GeneratedRegex(@"\([^)]*(аддон|зависимость|мост|обн\.|фикс|плагин|forge|fabric|sinytra|vram|fps)", RegexOptions.IgnoreCase)]
    private static partial Regex MetaParenHintRegex();

    [GeneratedRegex(@"\s*\([^)]*\)\s*$")]
    private static partial Regex TrailingParenRegex();

    public static List<AssemblyTreeNode> OrganizeAssemblyTree(IEnumerable<string> modIds)
    {
        var uniqueIds = modIds.Distinct(StringComparer.Ordinal).ToList();
        var selectedSet = new HashSet<string>(uniqueIds, StringComparer.Ordinal);
        var mods = uniqueIds
            .Select(id => ModDatabase.ModById.GetValueOrDefault(id))
            .OfType<ModItem>()
            .ToList();

        var parentForChild = new Dictionary<string, (string ParentId, string Relation)>(StringComparer.Ordinal);

        bool WouldCreateCycle(string childId, string candidateParentId)
        {
            string? curr = candidateParentId;
            var visited = new HashSet<string>(StringComparer.Ordinal) { childId };
            while (curr is not null)
            {
                if (!visited.Add(curr)) return true;
                curr = parentForChild.TryGetValue(curr, out var p) ? p.ParentId : null;
            }
            return false;
        }

        foreach (var mod in mods)
        {
            if (AddonParentPriority.TryGetValue(mod.Id, out var spec))
            {
                var parentId = spec.Parents.FirstOrDefault(pid =>
                    pid != mod.Id && selectedSet.Contains(pid) && !WouldCreateCycle(mod.Id, pid));
                if (parentId is not null)
                {
                    parentForChild[mod.Id] = (parentId, spec.Relation);
                    continue;
                }
            }

            if (mod.RecipeEcosystem == "jei-plugin")
            {
                string[] hosts = ["just-enough-items-jei", "tmrv-updated", "emi"];
                var fallbackParent = hosts.FirstOrDefault(pid =>
                    pid != mod.Id && selectedSet.Contains(pid) && !WouldCreateCycle(mod.Id, pid));
                if (fallbackParent is not null)
                {
                    parentForChild[mod.Id] = (fallbackParent, "Плагин рецептов");
                    continue;
                }
            }

            var hardDeps = ModAnalyzer.ModHardDependencies.TryGetValue(mod.Id, out var hd) ? hd : [];
            if (hardDeps.Length == 1 &&
                selectedSet.Contains(hardDeps[0]) &&
                hardDeps[0] != mod.Id &&
                !AddonParentPriority.ContainsKey(hardDeps[0]) &&
                !WouldCreateCycle(mod.Id, hardDeps[0]))
            {
                parentForChild[mod.Id] = (hardDeps[0], "Аддон");
            }
        }

        (string RootId, string Relation)? ResolveTopRoot(string startId)
        {
            if (!parentForChild.TryGetValue(startId, out var direct)) return null;
            string curr = startId;
            var seen = new HashSet<string>(StringComparer.Ordinal) { startId };
            while (parentForChild.TryGetValue(curr, out var nextPair))
            {
                if (!seen.Add(nextPair.ParentId)) return null;
                curr = nextPair.ParentId;
            }
            return curr != startId ? (curr, direct.Relation) : null;
        }

        var childrenByRoot = new Dictionary<string, List<AssemblyTreeChild>>(StringComparer.Ordinal);
        var childSet = new HashSet<string>(StringComparer.Ordinal);
        foreach (var mod in mods)
        {
            var resolved = ResolveTopRoot(mod.Id);
            if (resolved is not null)
            {
                childSet.Add(mod.Id);
                if (!childrenByRoot.TryGetValue(resolved.Value.RootId, out var list))
                {
                    list = [];
                    childrenByRoot[resolved.Value.RootId] = list;
                }
                list.Add(new AssemblyTreeChild { Mod = mod, RelationLabel = resolved.Value.Relation });
            }
        }

        var ruCulture = System.Globalization.CultureInfo.GetCultureInfo("ru-RU");
        var roots = mods.Where(m => !childSet.Contains(m.Id)).ToList();
        roots.Sort((a, b) =>
        {
            bool aHasChildren = childrenByRoot.TryGetValue(a.Id, out var ac) && ac.Count > 0;
            bool bHasChildren = childrenByRoot.TryGetValue(b.Id, out var bc) && bc.Count > 0;
            if (aHasChildren != bHasChildren) return aHasChildren ? -1 : 1;

            int prioA = RootPriorityOrder.GetValueOrDefault(a.Id, 999);
            int prioB = RootPriorityOrder.GetValueOrDefault(b.Id, 999);
            if (prioA != prioB) return prioA.CompareTo(prioB);

            return string.Compare(a.Name, b.Name, ruCulture, System.Globalization.CompareOptions.IgnoreCase);
        });

        return roots.Select(parent => new AssemblyTreeNode
        {
            Parent = parent,
            Children = childrenByRoot.GetValueOrDefault(parent.Id, [])
        }).ToList();
    }

    public static string FormatAssemblyTreeText(IEnumerable<string> modIds, bool includeUrls = true)
    {
        var tree = OrganizeAssemblyTree(modIds);
        var lines = new List<string>();
        for (int idx = 0; idx < tree.Count; idx++)
        {
            var node = tree[idx];
            var pUpdated = ModDatabase.GetModUpdatedLabel(node.Parent);
            var pMeta = !string.IsNullOrEmpty(pUpdated) ? $" [обн. {pUpdated}]" : string.Empty;
            var pUrl = includeUrls ? $" — {node.Parent.CurseforgeUrl}" : string.Empty;
            lines.Add($"{idx + 1}. {node.Parent.Name}{pMeta}{pUrl}");
            foreach (var child in node.Children)
            {
                var cUpdated = ModDatabase.GetModUpdatedLabel(child.Mod);
                var cMeta = !string.IsNullOrEmpty(cUpdated) ? $", обн. {cUpdated}" : string.Empty;
                var cUrl = includeUrls ? $" — {child.Mod.CurseforgeUrl}" : string.Empty;
                lines.Add($"   ↳ {child.Mod.Name} ({child.RelationLabel}{cMeta}){cUrl}");
            }
        }
        return string.Join("\n", lines);
    }

    public static string FormatModNamesListText(IEnumerable<string> modIds)
    {
        var tree = OrganizeAssemblyTree(modIds);
        var names = new List<string>();
        foreach (var node in tree)
        {
            names.Add(node.Parent.Name);
            foreach (var child in node.Children)
                names.Add(child.Mod.Name);
        }
        return string.Join("\n", names);
    }

    public static ModListImportResult ParseModNamesListText(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            return new ModListImportResult();

        bool hasNewlines = rawText.Contains('\n') || rawText.Contains('\r');
        var rawEntries = hasNewlines
            ? rawText.Split(["\r\n", "\n", "\r"], StringSplitOptions.None)
            : rawText.Split([',', ';'], StringSplitOptions.None);

        var matchedSet = new HashSet<string>(StringComparer.Ordinal);
        var unmatchedLines = new List<string>();

        foreach (var entry in rawEntries)
        {
            var original = entry.Trim();
            if (string.IsNullOrEmpty(original)) continue;

            var cleaned = NumberingPrefixRegex().Replace(original, string.Empty);
            cleaned = BulletPrefixRegex().Replace(cleaned, string.Empty);
            cleaned = TrailingUrlRegex().Replace(cleaned, string.Empty);
            cleaned = UpdatedBracketRegex().Replace(cleaned, string.Empty).Trim();

            if (string.IsNullOrEmpty(cleaned)) continue;

            var found = ModAnalyzer.FindModByDisplayName(cleaned);
            if (found is null && MetaParenHintRegex().IsMatch(cleaned))
            {
                var strippedMeta = TrailingParenRegex().Replace(cleaned, string.Empty).Trim();
                if (!string.IsNullOrEmpty(strippedMeta))
                    found = ModAnalyzer.FindModByDisplayName(strippedMeta);
            }

            if (found is null)
            {
                var jarCandidate = cleaned.EndsWith(".jar", StringComparison.OrdinalIgnoreCase)
                    ? cleaned
                    : $"{cleaned}.jar";
                var jarRes = JarMatcherService.MatchJarFilenamesToMods([jarCandidate]);
                if (jarRes.MatchedIds.Count > 0)
                {
                    foreach (var id in jarRes.MatchedIds) matchedSet.Add(id);
                    continue;
                }
            }

            if (found is not null)
                matchedSet.Add(found.Id);
            else
                unmatchedLines.Add(original);
        }

        return new ModListImportResult
        {
            MatchedIds = matchedSet.ToList(),
            UnmatchedLines = unmatchedLines
        };
    }
}
