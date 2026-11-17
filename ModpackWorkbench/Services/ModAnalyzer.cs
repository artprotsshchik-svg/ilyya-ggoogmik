using System.Text.RegularExpressions;
using ModpackWorkbench.Models;

namespace ModpackWorkbench.Services;

public static partial class ModAnalyzer
{
    private sealed class SearchIndexEntry
    {
        public required ModItem Mod { get; init; }
        public required string NameLower { get; init; }
        public required string Slug { get; init; }
        public required string TagsLower { get; init; }
        public required string DescLower { get; init; }
        public required string GroupTitleLower { get; init; }
        public required string GroupDescLower { get; init; }
        public required string AltLower { get; init; }
        public required string[] TagsWords { get; init; }
        public required string[] DescWords { get; init; }
        public required string[] AltWords { get; init; }
        public required string[] GroupDescWords { get; init; }
        public required string[] GroupTitleWords { get; init; }
        public required HashSet<string> WordsSet { get; init; }
        public required string[] WordsList { get; init; }
        public required string AllText { get; init; }
    }

    private sealed class RuleMeta
    {
        public required IncompatibilityRule Rule { get; init; }
        public required HashSet<string> SlugSet { get; init; }
        public required bool IsOptiFine { get; init; }
        public required bool IsNvidiumShader { get; init; }
    }

    private static readonly Dictionary<string, int> ModScoreById = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, string> ModSlugById = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, ModItem> ModBySlug = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, ModItem> ModByLowerName = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, string> ModCoveredByLower = new(StringComparer.Ordinal);
    private static readonly List<SearchIndexEntry> SearchIndex = new();
    private static readonly Dictionary<string, SearchIndexEntry> SearchIndexById = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, (string GroupId, List<string> PoolIds)> ExclusivePoolByModId = new(StringComparer.Ordinal);
    private static readonly List<RuleMeta> PrecomputedRuleMeta = new();
    private static readonly Dictionary<string, HashSet<string>> PrecomputedRuleConflictSets = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, List<string>> PrecomputedRuleConflicts = new(StringComparer.Ordinal);
    private static readonly IReadOnlyList<string> EmptyConflictList = Array.Empty<string>();

    private static readonly HashSet<string> PassiveLibTags = new(StringComparer.Ordinal)
    {
        "library", "api", "dependency", "библиотека", "зависимость"
    };

    private static readonly HashSet<string> OrangeCaveatModIds = new(StringComparer.Ordinal)
    {
        "accelerated-rendering",
        "accelerated-rendering-turned",
        "accelerated-recoiling",
        "tritium",
        "c2mer",
        "concurrent-chunk-management-engine-forge-c2me",
        "bobby-reforged",
        "structure-dfu-cache"
    };

    private static readonly Dictionary<string, string> DubiousExplicitReasons = new(StringComparer.Ordinal)
    {
        ["accelerated-rendering"] = "OpenGL 4.6: повышает расход VRAM и может вызывать артефакты с некоторыми модами/шейдерами",
        ["accelerated-rendering-turned"] = "Форк Accelerated Rendering: высокий расход VRAM и риск багов в сложных сценах",
        ["accelerated-recoiling"] = "Экспериментальный форк Accelerated Rendering для совместимости",
        ["tritium"] = "Экспериментальный комбайн оптимизаций — возможны перекрытия и конфликты миксинов",
        ["c2mer"] = "Экспериментальный порт C2ME — нестабильнее основного C2MEF",
        ["nvidium-forged"] = "Неофициальный порт Nvidium — менее проверен, чем связка Sinytra + Nvidium",
        ["structure-dfu-cache"] = "Ускоряет старт, но на тяжёлых модпаках требует проверки кеша структур",
        ["soundgc"] = "Агрессивная очистка звуковых буферов — в редких случаях обрывает длинные треки",
        ["capability-fix-revived"] = "Сторонний фикс-порт — используйте только при реальных лагах capability в профайлере",
        ["memory-leak-fix-modern-port"] = "В основном перекрыт ModernFix и AllTheLeaks",
        ["quick-pack"] = "Спорная оптимизация — требует проверки совместимости",
        ["fastchest-revived"] = "Переключает рендер сундуков на статический (перекрывается EBER)",
        ["bobby-reforged"] = "Старый порт 2023 года без обновлений — уступает Distant Horizons",
        ["minimal-backup-zstd"] = "Мод бэкапов, а не ускоритель FPS — полезен только для сохранений",
        ["config-preset-switch-low-medium-high-f10"] = "Переключатель пресетов графики, сам по себе FPS не поднимает"
    };

    public static readonly IReadOnlyDictionary<string, string[]> ModHardDependencies = new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        ["embeddium-extras"] = ["embeddium"],
        ["sodium-chloride"] = ["embeddium"],
        ["alloyium"] = ["embeddium"],
        ["oculus"] = ["embeddium"],
        ["lightstar"] = ["embeddium"],
        ["amd-fsr-upscaler-render-scale-for-embeddium"] = ["embeddium"],
        ["faster-iris-shadow-mapper"] = ["embeddium", "oculus"],
        ["sodium-embeddium-dynamic-lights"] = ["sodium-embeddium-options-api"],
        ["emberlens"] = ["emberbium"],
        ["vanillin"] = ["flywheel"],
        ["distant-horizons-memory-fix-dh"] = ["distant-horizons"],
        ["nvidium"] = ["sinytra-connector", "forgified-fabric-api"],
        ["nvidium-forged"] = ["nvidium", "sinytra-connector", "forgified-fabric-api"],
        ["c2me-concurrent-chunk-management-engine"] = ["sinytra-connector", "forgified-fabric-api"],
        ["voxy"] = ["sinytra-connector", "forgified-fabric-api", "embeddium"],
        ["very-many-players-vmp"] = ["sinytra-connector", "forgified-fabric-api"],
        ["connector-extras"] = ["sinytra-connector"],
        ["particle-core"] = ["kotlin-for-forge", "fzzy-config"],
        ["fzzy-config"] = ["kotlin-for-forge"],
        ["enhanced-block-entities-reloaded"] = ["matlib"],
        ["connectivity"] = ["cupboard"],
        ["fastali"] = ["advanced-loot-info-ali"],
        ["tmrv-updated"] = ["emi"],
        ["emi-loot"] = ["emi"],
        ["emi-ores"] = ["emi"],
        ["emiffect"] = ["emi"],
        ["emi-trades"] = ["emi"],
        ["emi-professions"] = ["emi"],
        ["jeioptimizer"] = ["just-enough-items-jei"],
        ["just-enough-freezes"] = ["just-enough-items-jei"],
        ["just-enough-threads"] = ["just-enough-items-jei"],
        ["jeir-jei-rewired"] = ["just-enough-items-jei"]
    };

    public static readonly IReadOnlyDictionary<string, string[][]> ModAlternativeDependencies = new Dictionary<string, string[][]>(StringComparer.Ordinal)
    {
        ["gputape"] = [["embeddium", "emberbium"]],
        ["nvidium"] = [["embeddium", "emberbium"]],
        ["nvidium-forged"] = [["embeddium", "emberbium"]],
        ["flerovium"] = [["embeddium", "emberbium"]],
        ["sodium-embeddium-options-api"] = [["embeddium", "emberbium"]],
        ["sodium-embeddium-dynamic-lights"] = [["embeddium", "emberbium"]],
        ["accelerated-rendering"] = [["embeddium", "emberbium"]]
    };

    public static readonly IReadOnlyDictionary<string, string> FabricToForgeSwap = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["lithium"] = "radium-re-reforged",
        ["krypton"] = "krypton-reno",
        ["sodium"] = "embeddium",
        ["iris-shaders"] = "oculus",
        ["very-many-players-vmp"] = "vmp-forge",
        ["c2me-concurrent-chunk-management-engine"] = "concurrent-chunk-management-engine-forge-c2me",
        ["dashloader"] = "modernfix",
        ["exordium"] = "immediatelyfast"
    };

    public static readonly IReadOnlyList<DualPortPair> DualPortPairs =
    [
        new DualPortPair
        {
            Id = "gpu-chunk-renderer",
            Title = "GPU-рендер чанков (без шейдеров)",
            ForgeModId = "alloyium",
            ForgeLabel = "Alloyium (Нативный Forge)",
            ForgeSummary = "OpenGL 4.3 Compute без Sinytra; +1.3×–1.6× FPS на любых GPU (NVIDIA/AMD/Intel) и с шейдерами.",
            FabricModId = "nvidium",
            FabricLabel = "Nvidium (Fabric + Sinytra)",
            FabricSummary = "NVIDIA Mesh Shaders (3×–10× FPS) через Sinytra Connector + алиас Embeddium (только без шейдеров).",
            SwitchableOnForge = true,
            AltForgeIds = ["revidium-accedium"],
            AltFabricIds = ["nvidium-forged"]
        },
        new DualPortPair
        {
            Id = "c2me",
            Title = "Многопоточные чанки (C2ME)",
            ForgeModId = "concurrent-chunk-management-engine-forge-c2me",
            ForgeLabel = "C2ME-Forge (C2MEF)",
            ForgeSummary = "Нативный порт без Sinytra Connector — избегает конфликтов шины событий Forge 1.20.1.",
            FabricModId = "c2me-concurrent-chunk-management-engine",
            FabricLabel = "C2ME (Fabric + Sinytra)",
            FabricSummary = "Оригинальный Fabric C2ME от ishland через Sinytra (нужны Connector Extras и noTickViewDistance = false).",
            SwitchableOnForge = true,
            AltForgeIds = ["c2mer"]
        },
        new DualPortPair
        {
            Id = "vmp",
            Title = "Высокий онлайн / игроки (VMP)",
            ForgeModId = "vmp-forge",
            ForgeLabel = "Very Many Players (Forge)",
            ForgeSummary = "Нативный Forge-порт без Sinytra и без конфликтов миксинов трекинга сущностей.",
            FabricModId = "very-many-players-vmp",
            FabricLabel = "VMP (Fabric + Sinytra)",
            FabricSummary = "Оригинальный Fabric VMP через Sinytra Connector (может конфликтовать миксинами в тяжёлых паках).",
            SwitchableOnForge = true
        },
        new DualPortPair
        {
            Id = "zip-loading",
            Title = "Чтение .zip паков (ModernFix ↔ Quick Pack)",
            ForgeModId = "modernfix",
            ForgeLabel = "ModernFix (Нативный Forge)",
            ForgeSummary = "Ускоряет загрузку ресурсов нативно в Forge 1.20.1 без сторонних мостов.",
            FabricModId = "quick-pack",
            FabricLabel = "Quick Pack (Fabric / Quick Pack-Fix)",
            FabricSummary = "Оригинал drexhd под Fabric — на Forge 1.20.1 требует Quick Pack-Fix или Sinytra Connector.",
            SwitchableOnForge = true
        },
        new DualPortPair
        {
            Id = "lithium-tps",
            Title = "Физика и ИИ (Radium ↔ Lithium)",
            ForgeModId = "radium-re-reforged",
            ForgeLabel = "Radium Re-Reforged (Нативный Forge)",
            ForgeSummary = "Стабильный порт Lithium с патчами под Create, C2ME и Forge-краши NBT.",
            FabricModId = "lithium",
            FabricLabel = "Lithium (Fabric — несовм. с Forge)",
            FabricSummary = "Оригинальный Fabric Lithium падает даже с Sinytra Connector — ставьте Radium Re-Reforged.",
            SwitchableOnForge = false
        },
        new DualPortPair
        {
            Id = "krypton-net",
            Title = "Сеть Netty (Krypton Reno / Krypton Hybrid ↔ Krypton)",
            ForgeModId = "krypton-reno",
            ForgeLabel = "Krypton Reno / Krypton Hybrid (Нативный Forge)",
            ForgeSummary = "Нативный сетевой стек для Forge 1.20.1 (Krypton Hybrid для Mohist/Arclight; Pluto остановился на 1.18.2).",
            FabricModId = "krypton",
            FabricLabel = "Krypton (Fabric — несовм. с Forge)",
            FabricSummary = "Velocity-нативки Fabric Krypton не работают через Sinytra — нужен Krypton Reno.",
            SwitchableOnForge = false,
            AltForgeIds = ["krypton-hybrid", "pluto"]
        },
        new DualPortPair
        {
            Id = "sodium-core",
            Title = "Базовый 3D-рендер (Embeddium ↔ Sodium)",
            ForgeModId = "embeddium",
            ForgeLabel = "Embeddium (Нативный Forge)",
            ForgeSummary = "Официальный стандарт для Forge 1.20.1 (с алиасом \"embeddium\": \"sodium\" для Nvidium в Sinytra).",
            FabricModId = "sodium",
            FabricLabel = "Sodium (Fabric — несовм. с Forge)",
            FabricSummary = "Fabric-оригинал Sodium крашнет Forge даже с Sinytra Connector.",
            SwitchableOnForge = false,
            AltForgeIds = ["emberbium"]
        },
        new DualPortPair
        {
            Id = "iris-shaders",
            Title = "Шейдеры (Oculus / Emberlens ↔ Iris)",
            ForgeModId = "oculus",
            ForgeLabel = "Oculus / Emberlens (Нативный Forge)",
            ForgeSummary = "Нативный порт Iris Shaders для Embeddium (либо Emberlens поверх Emberbium).",
            FabricModId = "iris-shaders",
            FabricLabel = "Iris Shaders (Fabric — несовм. с Forge)",
            FabricSummary = "Fabric Iris несовместим с Forge 1.20.1 — используйте Oculus или Emberlens.",
            SwitchableOnForge = false,
            AltForgeIds = ["emberlens"]
        },
        new DualPortPair
        {
            Id = "worldgen-noisium",
            Title = "Шум генерации мира (Noisiumed ↔ Noisium)",
            ForgeModId = "noisiumed",
            ForgeLabel = "Noisiumed (Актуальный форк Forge)",
            ForgeSummary = "Поддерживаемое продолжение архивного Noisium для Forge 1.20.1.",
            FabricModId = "noisium",
            FabricLabel = "Noisium (Архивный порт)",
            FabricSummary = "Прекращённый предшественник Noisiumed — оставляйте только один из них.",
            SwitchableOnForge = true
        }
    ];

    private static readonly (Regex Pattern, string[] Synonyms)[] PurposeSynonyms =
    [
        (new Regex(@"^(фпс|fps|кадр|кадры|кадров|производ)", RegexOptions.IgnoreCase | RegexOptions.Compiled), ["fps", "render", "рендер", "culling", "куллинг", "чанк"]),
        (new Regex(@"^(озу|рам|ram|память|памяти|оператив)", RegexOptions.IgnoreCase | RegexOptions.Compiled), ["ram", "память", "памяти", "memory", "утечк", "leak", "dedup", "дедуплик"]),
        (new Regex(@"^(утечк|leak|утечки)", RegexOptions.IgnoreCase | RegexOptions.Compiled), ["утечк", "leak", "memory", "память", "памяти", "alltheleaks"]),
        (new Regex(@"^(лаг|лаги|лагов|фриз|фризы|фризов|статтер|подвис|микрофриз|tps|тпс|тик)", RegexOptions.IgnoreCase | RegexOptions.Compiled), ["tps", "stutter", "фриз", "лаг", "тиках", "физик", "radium", "spark", "профил"]),
        (new Regex(@"^(моб|мобы|мобов|существ|сущност|entity|entities|животн|жител)", RegexOptions.IgnoreCase | RegexOptions.Compiled), ["entity", "entities", "моб", "сущност", "ии", "ai", "villager", "жител", "culling"]),
        (new Regex(@"^(ии|ai|интеллект|мозг)", RegexOptions.IgnoreCase | RegexOptions.Compiled), ["ai", "brain", "ии", "моб", "сущност", "radium", "villager"]),
        (new Regex(@"^(тен|тени|теней|shadow)", RegexOptions.IgnoreCase | RegexOptions.Compiled), ["shadow", "тени", "теней", "тень", "iris", "oculus"]),
        (new Regex(@"^(свет|света|освещ|ярк|light|fullbright)", RegexOptions.IgnoreCase | RegexOptions.Compiled), ["light", "освещ", "свет", "dynamic", "solix", "lightstar", "fullbright"]),
        (new Regex(@"^(шейдер|шейдеры|шейдеров|shader)", RegexOptions.IgnoreCase | RegexOptions.Compiled), ["shader", "шейдер", "oculus", "iris", "emberlens", "dlss"]),
        (new Regex(@"^(чанк|чанки|чанков|chunk|прогруз)", RegexOptions.IgnoreCase | RegexOptions.Compiled), ["chunk", "чанк", "c2me", "noisium", "render", "прогруз"]),
        (new Regex(@"^(дальност|прорисов|лод|lod|distance)", RegexOptions.IgnoreCase | RegexOptions.Compiled), ["distance", "дальност", "lod", "distant", "horizons", "voxy", "nvidium", "bobby"]),
        (new Regex(@"^(звук|звуки|звука|аудио|sound|audio)", RegexOptions.IgnoreCase | RegexOptions.Compiled), ["sound", "звук", "аудио", "audio"]),
        (new Regex(@"^(сет|сеть|сети|пинг|ping|пакет|netty|сервер|онлайн|мультиплеер)", RegexOptions.IgnoreCase | RegexOptions.Compiled), ["network", "netty", "сеть", "сетев", "пинг", "ping", "packet", "пакет", "krypton", "server"]),
        (new Regex(@"^(частиц|частицы|particle|эффект)", RegexOptions.IgnoreCase | RegexOptions.Compiled), ["particle", "частиц", "эффект"]),
        (new Regex(@"^(рецепт|рецепты|крафт|верстак|печк|jei|emi)", RegexOptions.IgnoreCase | RegexOptions.Compiled), ["recipe", "рецепт", "крафт", "jei", "emi", "workbench", "furnace", "tmrv"]),
        (new Regex(@"^(шрифт|шрифты|текст|интерфейс|меню|f3|hud|gui|ui)", RegexOptions.IgnoreCase | RegexOptions.Compiled), ["font", "шрифт", "ui", "gui", "f3", "immediatelyfast", "hud", "меню"]),
        (new Regex(@"^(генер|мир|структур|биом|шум|worldgen)", RegexOptions.IgnoreCase | RegexOptions.Compiled), ["worldgen", "генерац", "структур", "биом", "biome", "noise", "шум", "noisium"]),
        (new Regex(@"^(запуск|загруз|старт|boot|load)", RegexOptions.IgnoreCase | RegexOptions.Compiled), ["boot", "load", "запуск", "загруз", "smooth", "modernfix"]),
        (new Regex(@"^(редстоун|механизм|воронк|create|криейт)", RegexOptions.IgnoreCase | RegexOptions.Compiled), ["redstone", "редстоун", "create", "flywheel", "воронк", "radium"]),
        (new Regex(@"^(сундук|таблич|рамк|печ|chest|block\s*entit)", RegexOptions.IgnoreCase | RegexOptions.Compiled), ["chest", "сундук", "таблич", "рамк", "block", "entity", "eber", "enhanced"]),
        (new Regex(@"^(опыт|предмет|дроп|лут|clumps|xp|item)", RegexOptions.IgnoreCase | RegexOptions.Compiled), ["item", "xp", "опыт", "clumps", "предмет", "дроп", "despawn"]),
        (new Regex(@"^(профайл|диагност|монитор|spark|тпс)", RegexOptions.IgnoreCase | RegexOptions.Compiled), ["spark", "observable", "profiler", "профил", "диагност", "лаг"]),
        (new Regex(@"^(бэкап|сохран|zstd|backup)", RegexOptions.IgnoreCase | RegexOptions.Compiled), ["backup", "бэкап", "сохран", "zstd"]),
        (new Regex(@"^(апскейл|fsr|dlss|масштаб|разрешен)", RegexOptions.IgnoreCase | RegexOptions.Compiled), ["fsr", "upscal", "dlss", "апскейл", "масштаб"])
    ];

    static ModAnalyzer()
    {
        foreach (var m in ModDatabase.ModsDatabase)
        {
            ModScoreById[m.Id] = ComputeEffectivenessScore(m);
            var slug = NormalizeSlug(m.Name);
            ModSlugById[m.Id] = slug;
            if (!string.IsNullOrEmpty(slug) && !ModBySlug.ContainsKey(slug))
                ModBySlug[slug] = m;

            var nameLower = m.Name.ToLowerInvariant();
            if (!ModByLowerName.ContainsKey(nameLower))
                ModByLowerName[nameLower] = m;

            if (!string.IsNullOrEmpty(m.CoveredBy))
                ModCoveredByLower[m.Id] = m.CoveredBy.ToLowerInvariant();

            ModDatabase.GroupById.TryGetValue(m.GroupId ?? string.Empty, out var grp);
            var tagsLower = string.Join(" ", m.Tags ?? []).ToLowerInvariant();
            var descLower = m.Description.ToLowerInvariant();
            var groupTitleLower = $"{m.GroupTitle ?? string.Empty} {m.Category}".ToLowerInvariant();
            var groupDescLower = (grp?.Description ?? string.Empty).ToLowerInvariant();
            var altLower = $"{m.BestAlternative ?? string.Empty} {m.Warning ?? string.Empty} {m.Platform}".ToLowerInvariant();
            var allText = $"{nameLower} {tagsLower} {descLower} {altLower} {groupTitleLower} {groupDescLower}";

            var tagsWords = ExtractWords(tagsLower);
            var descWords = ExtractWords(descLower);
            var altWords = ExtractWords(altLower);
            var groupDescWords = ExtractWords(groupDescLower);
            var groupTitleWords = ExtractWords(groupTitleLower);
            var words = ExtractWords(allText);

            var entry = new SearchIndexEntry
            {
                Mod = m,
                NameLower = nameLower,
                Slug = slug,
                TagsLower = tagsLower,
                DescLower = descLower,
                GroupTitleLower = groupTitleLower,
                GroupDescLower = groupDescLower,
                AltLower = altLower,
                TagsWords = tagsWords,
                DescWords = descWords,
                AltWords = altWords,
                GroupDescWords = groupDescWords,
                GroupTitleWords = groupTitleWords,
                WordsSet = new HashSet<string>(words, StringComparer.Ordinal),
                WordsList = words.Distinct(StringComparer.Ordinal).ToArray(),
                AllText = allText
            };
            SearchIndex.Add(entry);
            SearchIndexById[m.Id] = entry;
        }

        foreach (var g in ModDatabase.ModGroups)
        {
            if (!g.Exclusive) continue;
            var addonSet = new HashSet<string>(g.AddonIds, StringComparer.Ordinal);
            var pool = g.MemberIds.Where(id => !addonSet.Contains(id)).ToList();
            foreach (var id in pool)
            {
                ExclusivePoolByModId[id] = (g.Id, pool);
            }
        }

        foreach (var rule in ModDatabase.IncompatibilityRules)
        {
            PrecomputedRuleMeta.Add(new RuleMeta
            {
                Rule = rule,
                SlugSet = new HashSet<string>(rule.ConflictingMods.Select(NormalizeSlug), StringComparer.Ordinal),
                IsOptiFine = rule.Title.Contains("OptiFine", StringComparison.Ordinal),
                IsNvidiumShader = rule.Title.Contains("Nvidium и шейдеров", StringComparison.Ordinal)
            });
        }

        foreach (var mod in ModDatabase.ModsDatabase)
        {
            var slug = ModSlugById.GetValueOrDefault(mod.Id, string.Empty);
            var conflicts = new HashSet<string>(StringComparer.Ordinal);
            foreach (var rm in PrecomputedRuleMeta)
            {
                if (!rm.SlugSet.Contains(slug)) continue;
                if (rm.IsOptiFine)
                {
                    foreach (var m in ModDatabase.ModsDatabase)
                    {
                        var ms = ModSlugById.GetValueOrDefault(m.Id, string.Empty);
                        if (slug == "optifine")
                        {
                            if (ms != "optifine" && rm.SlugSet.Contains(ms)) conflicts.Add(m.Id);
                        }
                        else if (ms == "optifine")
                        {
                            conflicts.Add(m.Id);
                        }
                    }
                    continue;
                }
                if (rm.IsNvidiumShader)
                {
                    foreach (var m in ModDatabase.ModsDatabase)
                    {
                        var ms = ModSlugById.GetValueOrDefault(m.Id, string.Empty);
                        if (mod.Id == "nvidium")
                        {
                            if (m.Id != "nvidium" && rm.SlugSet.Contains(ms)) conflicts.Add(m.Id);
                        }
                        else if (m.Id == "nvidium")
                        {
                            conflicts.Add(m.Id);
                        }
                    }
                    continue;
                }
                foreach (var m in ModDatabase.ModsDatabase)
                {
                    var ms = ModSlugById.GetValueOrDefault(m.Id, string.Empty);
                    if (m.Id != mod.Id && rm.SlugSet.Contains(ms))
                        conflicts.Add(m.Id);
                }
            }

            if (ExclusivePoolByModId.TryGetValue(mod.Id, out var exclusive))
            {
                foreach (var pid in exclusive.PoolIds)
                {
                    if (pid != mod.Id) conflicts.Add(pid);
                }
            }

            PrecomputedRuleConflictSets[mod.Id] = conflicts;
            PrecomputedRuleConflicts[mod.Id] = conflicts.ToList();
        }
    }

    [GeneratedRegex(@"\([^)]*\)")]
    private static partial Regex ParenRegex();

    [GeneratedRegex(@"[^a-z0-9]")]
    private static partial Regex NonAlnumRegex();

    [GeneratedRegex(@"[^a-zа-яё0-9]+", RegexOptions.IgnoreCase)]
    private static partial Regex WordSplitRegex();

    [GeneratedRegex(@"^[а-яё]+$", RegexOptions.IgnoreCase)]
    private static partial Regex CyrillicWordRegex();

    [GeneratedRegex(@"(иями|ями|ами|ого|его|ому|ему|ыми|ими|ость|ение|ание|ция|ции|ов|ев|ей|ам|ям|ах|ях|ом|ем|ой|ий|ый|ая|ое|ые|ие|ую|юю|у|ю|а|я|е|и|ы|о)$", RegexOptions.IgnoreCase)]
    private static partial Regex RuEndingRegex();

    [GeneratedRegex(@"\bForge\b")]
    private static partial Regex WholeForgeRegex();

    public static string NormalizeSlug(string name)
    {
        var noParens = ParenRegex().Replace(name.ToLowerInvariant(), string.Empty);
        return NonAlnumRegex().Replace(noParens, string.Empty);
    }

    private static int ComputeEffectivenessScore(ModItem mod)
    {
        int score = 0;
        if (mod.IsGroupLeader) score += 500;
        if (mod.IsGroupAddon) score += 250;
        if (mod.Installed) score += 120;
        score += mod.Status switch
        {
            "recommended" => 1000,
            "useful" => 600,
            "diagnostic" => 350,
            "redundant" => 100,
            "outdated" => 50,
            "danger" => -800,
            _ => 0
        };
        if (!string.IsNullOrEmpty(mod.CoveredBy)) score -= 220;
        if (IsUnsupportedOnForge(mod)) score -= 600;
        return score;
    }

    public static int GetModEffectivenessScore(ModItem mod)
    {
        return ModScoreById.TryGetValue(mod.Id, out var score) ? score : ComputeEffectivenessScore(mod);
    }

    public static bool IsUnsupportedOnForge(ModItem mod)
    {
        return mod.Platform.Contains("Несовм.", StringComparison.Ordinal) ||
               mod.Platform.Contains("Fabric (Справка", StringComparison.Ordinal);
    }

    private static string[] ExtractWords(string text)
    {
        if (string.IsNullOrEmpty(text)) return [];
        return WordSplitRegex().Split(text.ToLowerInvariant())
            .Where(w => !string.IsNullOrEmpty(w))
            .ToArray();
    }

    public static ModItem? FindModByDisplayName(string name)
    {
        var trimmed = name.Trim();
        if (string.IsNullOrEmpty(trimmed)) return null;

        if (ModByLowerName.TryGetValue(trimmed.ToLowerInvariant(), out var exactLower))
            return exactLower;

        if (ModDatabase.ModById.TryGetValue(trimmed.ToLowerInvariant(), out var byId))
            return byId;

        var target = NormalizeSlug(trimmed);
        if (string.IsNullOrEmpty(target)) return null;

        if (ModBySlug.TryGetValue(target, out var exactSlug))
            return exactSlug;

        foreach (var m in ModDatabase.ModsDatabase)
        {
            var ms = ModSlugById.GetValueOrDefault(m.Id, string.Empty);
            if (ms.StartsWith(target, StringComparison.Ordinal))
                return m;
        }
        return null;
    }

    private static string StemRuWord(string word)
    {
        if (word.Length < 4 || !CyrillicWordRegex().IsMatch(word))
            return word;
        var stripped = RuEndingRegex().Replace(word, string.Empty);
        return stripped.Length >= 3 ? stripped : word[..Math.Max(3, word.Length - 1)];
    }

    private static string[] GetExpandedTerms(string token)
    {
        var terms = new HashSet<string>(StringComparer.Ordinal) { token };
        var stemmed = StemRuWord(token);
        if (stemmed != token) terms.Add(stemmed);
        foreach (var (pattern, syns) in PurposeSynonyms)
        {
            if (pattern.IsMatch(token))
            {
                foreach (var s in syns) terms.Add(s);
            }
        }
        return terms.ToArray();
    }

    private static bool WordPrefixMatches(string[] words, string term)
    {
        if (term.Length <= 2) return words.Any(w => w == term);
        if (term.Length == 3) return words.Any(w => w == term || w.StartsWith(term, StringComparison.Ordinal));
        return words.Any(w => w.StartsWith(term, StringComparison.Ordinal));
    }

    private static bool TextMatchesTerm(string text, string[] words, string term)
    {
        if (string.IsNullOrEmpty(text)) return false;
        if (WordPrefixMatches(words, term)) return true;
        return term.Length >= 5 && text.Contains(term, StringComparison.Ordinal);
    }

    private static double ScoreEntryAgainstTokens(
        SearchIndexEntry entry,
        List<(string Raw, string Stem, string[] Terms)> expandedTokens,
        string fullQueryLower)
    {
        double totalRank = 0;
        bool hasDirectNameMatch = false;

        foreach (var (raw, stem, terms) in expandedTokens)
        {
            double nameScore = 0;
            if (entry.NameLower == raw)
            {
                nameScore = 2600;
                hasDirectNameMatch = true;
            }
            else if (entry.NameLower.StartsWith(raw, StringComparison.Ordinal))
            {
                nameScore = 1500;
                hasDirectNameMatch = true;
            }
            else if (raw.Length >= 3 && entry.NameLower.Contains(raw, StringComparison.Ordinal))
            {
                nameScore = 950;
                hasDirectNameMatch = true;
            }

            double bestTag = 0;
            double bestDesc = 0;
            double bestAlt = 0;
            double bestGroupDesc = 0;
            double bestGroupTitle = 0;

            foreach (var term in terms)
            {
                bool isPrimaryTerm = term == raw || term == stem;
                double mult = isPrimaryTerm ? 1.0 : 0.72;

                if (entry.WordsSet.Contains(term) ||
                    (term.Length >= 3 && entry.WordsList.Any(w => w.StartsWith(term, StringComparison.Ordinal))))
                {
                    if (entry.TagsWords.Length > 0 && WordPrefixMatches(entry.TagsWords, term))
                        bestTag = Math.Max(bestTag, Math.Round(760 * mult));
                    if (!string.IsNullOrEmpty(entry.DescLower) && TextMatchesTerm(entry.DescLower, entry.DescWords, term))
                        bestDesc = Math.Max(bestDesc, Math.Round(620 * mult));
                    if (!string.IsNullOrEmpty(entry.AltLower) && TextMatchesTerm(entry.AltLower, entry.AltWords, term))
                        bestAlt = Math.Max(bestAlt, Math.Round(320 * mult));
                    if (!string.IsNullOrEmpty(entry.GroupDescLower) && TextMatchesTerm(entry.GroupDescLower, entry.GroupDescWords, term))
                        bestGroupDesc = Math.Max(bestGroupDesc, Math.Round(250 * mult));
                    if (!string.IsNullOrEmpty(entry.GroupTitleLower) && TextMatchesTerm(entry.GroupTitleLower, entry.GroupTitleWords, term))
                        bestGroupTitle = Math.Max(bestGroupTitle, Math.Round(170 * mult));
                }
                else if (term.Length >= 5 && entry.AllText.Contains(term, StringComparison.Ordinal))
                {
                    if (entry.TagsLower.Contains(term, StringComparison.Ordinal))
                        bestTag = Math.Max(bestTag, Math.Round(540 * mult));
                    if (entry.DescLower.Contains(term, StringComparison.Ordinal))
                        bestDesc = Math.Max(bestDesc, Math.Round(460 * mult));
                    if (entry.GroupDescLower.Contains(term, StringComparison.Ordinal))
                        bestGroupDesc = Math.Max(bestGroupDesc, Math.Round(190 * mult));
                    else
                        bestGroupTitle = Math.Max(bestGroupTitle, Math.Round(120 * mult));
                }
            }

            double fieldPrimary = Math.Max(nameScore, Math.Max(bestTag, Math.Max(bestDesc, Math.Max(bestAlt, Math.Max(bestGroupDesc, bestGroupTitle)))));
            if (fieldPrimary == 0) return 0;

            totalRank += nameScore + bestTag * 0.85 + bestDesc * 0.75 + bestGroupDesc * 0.45 + bestGroupTitle * 0.35 + bestAlt * 0.3;
        }

        if (entry.NameLower == fullQueryLower) totalRank += 1400;
        else if (entry.NameLower.StartsWith(fullQueryLower, StringComparison.Ordinal)) totalRank += 650;

        if (!hasDirectNameMatch)
        {
            bool isPassiveLib = entry.TagsWords.Any(w => PassiveLibTags.Contains(w)) ||
                                entry.Mod.Category == "Библиотеки и API";
            if (isPassiveLib) totalRank -= 420;
            if (entry.Mod.Status == "danger") totalRank -= 450;
            if (entry.Mod.Status == "outdated") totalRank -= 280;
            if (!string.IsNullOrEmpty(entry.Mod.CoveredBy) || (entry.Mod.SupersededBy?.Count > 0))
                totalRank -= 180;
            if (IsUnsupportedOnForge(entry.Mod)) totalRank -= 320;
        }
        else if (entry.NameLower != fullQueryLower && !entry.NameLower.StartsWith(fullQueryLower, StringComparison.Ordinal))
        {
            if (entry.Mod.Status == "outdated") totalRank -= 220;
            if (entry.Mod.Status == "danger") totalRank -= 350;
            if (!string.IsNullOrEmpty(entry.Mod.CoveredBy) || (entry.Mod.SupersededBy?.Count > 0))
                totalRank -= 160;
        }

        if (entry.Mod.IsGroupLeader) totalRank += 140;
        if (entry.Mod.HealthTier == "green" && entry.Mod.Status == "recommended") totalRank += 120;
        totalRank += ModScoreById.GetValueOrDefault(entry.Mod.Id, 0) * 0.16;

        return Math.Max(1, totalRank);
    }

    public static bool MatchesModPurposeQuery(ModItem mod, string query) => MatchesModPurposeQuery(mod.Id, query);

    public static bool MatchesModPurposeQuery(string modId, string query)
    {
        var q = query.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(q)) return true;
        if (!SearchIndexById.TryGetValue(modId, out var entry)) return false;

        var tokens = q.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var expanded = tokens.Select(t => (t, StemRuWord(t), GetExpandedTerms(t))).ToList();
        return ScoreEntryAgainstTokens(entry, expanded, q) > 0;
    }

    public static List<ModItem> SearchModsByQuery(string query, int limit = 15)
    {
        var q = query.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(q)) return [];

        var tokens = q.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var expanded = tokens.Select(t => (t, StemRuWord(t), GetExpandedTerms(t))).ToList();

        var scored = new List<(ModItem Mod, double Rank)>();
        foreach (var entry in SearchIndex)
        {
            var rank = ScoreEntryAgainstTokens(entry, expanded, q);
            if (rank > 0) scored.Add((entry.Mod, rank));
        }

        var ruCulture = System.Globalization.CultureInfo.GetCultureInfo("ru-RU");
        return scored
            .OrderByDescending(x => x.Rank)
            .ThenBy(x => x.Mod.Name, StringComparer.Create(ruCulture, true))
            .Take(limit)
            .Select(x => x.Mod)
            .ToList();
    }

    public static DualPortPair? GetDualPortPairForMod(string modId)
    {
        foreach (var pair in DualPortPairs)
        {
            if (pair.ForgeModId == modId ||
                pair.FabricModId == modId ||
                (pair.AltForgeIds?.Contains(modId) ?? false) ||
                (pair.AltFabricIds?.Contains(modId) ?? false))
            {
                return pair;
            }
        }
        return null;
    }

    public static List<string> GetDirectDependencies(string modId, IReadOnlySet<string>? contextSet = null)
    {
        var deps = new HashSet<string>(
            ModHardDependencies.TryGetValue(modId, out var hard) ? hard : [],
            StringComparer.Ordinal);

        if (ModAlternativeDependencies.TryGetValue(modId, out var altGroups))
        {
            foreach (var options in altGroups)
            {
                if (options.Length == 0) continue;
                var satisfied = contextSet is not null
                    ? options.FirstOrDefault(opt => contextSet.Contains(opt))
                    : null;
                deps.Add(satisfied ?? options[0]);
            }
        }

        if (ModDatabase.ModById.TryGetValue(modId, out var mod))
        {
            if (mod.Platform.Contains("Sinytra", StringComparison.Ordinal))
            {
                deps.Add("sinytra-connector");
                deps.Add("forgified-fabric-api");
            }

            if (mod.RecipeEcosystem == "jei-plugin")
            {
                if (contextSet is not null && contextSet.Contains("emi") && contextSet.Contains("tmrv-updated"))
                {
                    deps.Add("emi");
                    deps.Add("tmrv-updated");
                }
                else if (contextSet is null || !contextSet.Contains("just-enough-items-jei"))
                {
                    deps.Add("just-enough-items-jei");
                }
            }
        }

        return deps.ToList();
    }

    public static List<string> GetTransitiveDependencies(
        string modId,
        HashSet<string>? visited = null,
        IReadOnlySet<string>? contextSet = null)
    {
        visited ??= new HashSet<string>(StringComparer.Ordinal);
        if (!visited.Add(modId)) return [];

        var direct = GetDirectDependencies(modId, contextSet);
        var result = new HashSet<string>(direct, StringComparer.Ordinal);
        foreach (var d in direct)
        {
            foreach (var td in GetTransitiveDependencies(d, visited, contextSet))
            {
                result.Add(td);
            }
        }
        return result.ToList();
    }

    public static (string GroupId, List<string> PoolIds)? GetExclusivePoolForMod(string modId)
    {
        return ExclusivePoolByModId.TryGetValue(modId, out var val) ? val : null;
    }

    public static IReadOnlyList<string> GetRuleConflictingIds(string modId)
    {
        return PrecomputedRuleConflicts.TryGetValue(modId, out var list) ? list : EmptyConflictList;
    }

    private static bool HasAnyConflictInSet(string modId, IReadOnlySet<string> activeSet)
    {
        if (!PrecomputedRuleConflictSets.TryGetValue(modId, out var set) || set.Count == 0)
            return false;
        foreach (var cid in set)
        {
            if (activeSet.Contains(cid)) return true;
        }
        return false;
    }

    public static AssemblyAnalysisResult AnalyzeSelection(IEnumerable<string> selectedIds)
    {
        var selectedSet = new HashSet<string>(selectedIds, StringComparer.Ordinal);
        var selectedMods = ModDatabase.ModsDatabase.Where(m => selectedSet.Contains(m.Id)).ToList();
        var conflicts = new List<ConflictDetection>();
        var conflictingIdSet = new HashSet<string>(StringComparer.Ordinal);

        foreach (var rm in PrecomputedRuleMeta)
        {
            if (rm.IsOptiFine)
            {
                var opti = selectedMods.FirstOrDefault(m => ModSlugById.GetValueOrDefault(m.Id, "") == "optifine");
                if (opti is null) continue;
                var others = selectedMods.Where(m =>
                {
                    var n = ModSlugById.GetValueOrDefault(m.Id, "");
                    return n != "optifine" && rm.SlugSet.Contains(n);
                }).ToList();
                if (others.Count > 0)
                {
                    var matched = new List<ModItem> { opti };
                    matched.AddRange(others);
                    foreach (var m in matched) conflictingIdSet.Add(m.Id);
                    conflicts.Add(new ConflictDetection
                    {
                        Rule = rm.Rule,
                        MatchedIds = matched.Select(m => m.Id).ToList(),
                        MatchedNames = matched.Select(m => m.Name).ToList()
                    });
                }
                continue;
            }

            if (rm.IsNvidiumShader)
            {
                var nvid = selectedMods.FirstOrDefault(m => m.Id == "nvidium");
                if (nvid is null) continue;
                var others = selectedMods.Where(m =>
                    m.Id != "nvidium" && rm.SlugSet.Contains(ModSlugById.GetValueOrDefault(m.Id, ""))).ToList();
                if (others.Count > 0)
                {
                    var matched = new List<ModItem> { nvid };
                    matched.AddRange(others);
                    foreach (var m in matched) conflictingIdSet.Add(m.Id);
                    conflicts.Add(new ConflictDetection
                    {
                        Rule = rm.Rule,
                        MatchedIds = matched.Select(m => m.Id).ToList(),
                        MatchedNames = matched.Select(m => m.Name).ToList()
                    });
                }
                continue;
            }

            var matchedMods = selectedMods.Where(sm => rm.SlugSet.Contains(ModSlugById.GetValueOrDefault(sm.Id, ""))).ToList();
            if (matchedMods.Count > 1)
            {
                foreach (var m in matchedMods) conflictingIdSet.Add(m.Id);
                conflicts.Add(new ConflictDetection
                {
                    Rule = rm.Rule,
                    MatchedIds = matchedMods.Select(m => m.Id).ToList(),
                    MatchedNames = matchedMods.Select(m => m.Name).ToList()
                });
            }
        }

        var warnings = new List<DependencyWarning>();
        bool hasSinytra = selectedSet.Contains("sinytra-connector");
        bool hasFFAPI = selectedSet.Contains("forgified-fabric-api");
        bool hasEmbeddium = selectedSet.Contains("embeddium");
        bool hasEmberbium = selectedSet.Contains("emberbium");
        bool hasEmi = selectedSet.Contains("emi");
        bool hasJei = selectedSet.Contains("just-enough-items-jei");
        bool hasTmrv = selectedSet.Contains("tmrv-updated");

        foreach (var mod in selectedMods)
        {
            if (mod.Platform.Contains("Sinytra", StringComparison.Ordinal))
            {
                if (!hasSinytra || !hasFFAPI)
                {
                    warnings.Add(new DependencyWarning
                    {
                        ModId = mod.Id,
                        ModName = mod.Name,
                        Severity = "red",
                        Message = $"{mod.Name} (Fabric-мод) не запустится на Forge без Sinytra Connector и Forgified Fabric API.",
                        FixModIds = ["sinytra-connector", "forgified-fabric-api"]
                    });
                }
                if (mod.Id == "nvidium" && !hasEmbeddium && !hasEmberbium)
                {
                    warnings.Add(new DependencyWarning
                    {
                        ModId = mod.Id,
                        ModName = mod.Name,
                        Severity = "red",
                        Message = "Nvidium на Forge требует Embeddium или Emberbium (и алиас embeddium -> sodium в конфиге Sinytra .connector).",
                        FixModIds = ["embeddium"]
                    });
                }
            }

            var hardDeps = ModHardDependencies.TryGetValue(mod.Id, out var h) ? h : [];
            var missingDeps = hardDeps.Where(depId => !selectedSet.Contains(depId)).ToList();
            var missingNonSinytra = missingDeps.Where(depId =>
                !(mod.Platform.Contains("Sinytra", StringComparison.Ordinal) &&
                  (depId == "sinytra-connector" || depId == "forgified-fabric-api" || depId == "embeddium" || depId == "emberbium"))).ToList();

            if (missingNonSinytra.Count > 0)
            {
                var missingNames = missingNonSinytra.Select(id => ModDatabase.ModById.TryGetValue(id, out var dm) ? dm.Name : id);
                bool isCriticalViewerMissing = mod.RecipeEcosystem is "emi-only" or "tmrv-bridge" or "jei-engine";
                warnings.Add(new DependencyWarning
                {
                    ModId = mod.Id,
                    ModName = mod.Name,
                    Severity = isCriticalViewerMissing ? "red" : "yellow",
                    Message = $"{mod.Name} требует: {string.Join(" + ", missingNames)}. Добавьте зависимость, иначе мод не сработает.",
                    FixModIds = missingNonSinytra
                });
            }

            if (ModAlternativeDependencies.TryGetValue(mod.Id, out var altDepGroups))
            {
                foreach (var options in altDepGroups)
                {
                    if (mod.Id == "nvidium" && options.Contains("embeddium")) continue;
                    if (options.Length > 0 && !options.Any(optId => selectedSet.Contains(optId)))
                    {
                        var optNames = options.Select(id => ModDatabase.ModById.TryGetValue(id, out var dm) ? dm.Name : id);
                        warnings.Add(new DependencyWarning
                        {
                            ModId = mod.Id,
                            ModName = mod.Name,
                            Severity = "yellow",
                            Message = $"{mod.Name} требует один из базовых модов: {string.Join(" или ", optNames)}.",
                            FixModIds = [options[0]]
                        });
                    }
                }
            }

            if (mod.RecipeEcosystem == "jei-plugin" && !hasJei && !(hasEmi && hasTmrv))
            {
                warnings.Add(new DependencyWarning
                {
                    ModId = mod.Id,
                    ModName = mod.Name,
                    Severity = "red",
                    Message = hasEmi
                        ? $"{mod.Name} — плагин JEI: для работы внутри чистого EMI нужна прослойка TMRV Updated (или JEI)."
                        : $"{mod.Name} — плагин JEI: установите Just Enough Items (JEI) или связку EMI + TMRV Updated.",
                    FixModIds = hasEmi ? ["tmrv-updated"] : ["just-enough-items-jei"]
                });
            }

            var activeSuperseder = GetModSuperseder(mod, selectedSet, null, false);
            if (activeSuperseder is { IsActiveInSelection: true })
            {
                warnings.Add(new DependencyWarning
                {
                    ModId = mod.Id,
                    ModName = mod.Name,
                    Severity = "yellow",
                    Message = $"{mod.Name} лишний — {activeSuperseder.Reason}."
                });
            }
            else if (!string.IsNullOrEmpty(mod.CoveredBy) &&
                     selectedMods.Any(other => other.Name == mod.CoveredBy || mod.CoveredBy.Contains(other.Name, StringComparison.Ordinal)))
            {
                warnings.Add(new DependencyWarning
                {
                    ModId = mod.Id,
                    ModName = mod.Name,
                    Severity = "yellow",
                    Message = $"{mod.Name} избыточен — его функции уже встроены в {mod.CoveredBy}."
                });
            }

            if (IsUnsupportedOnForge(mod))
            {
                warnings.Add(new DependencyWarning
                {
                    ModId = mod.Id,
                    ModName = mod.Name,
                    Severity = "red",
                    Message = $"{mod.Name} ({mod.Platform}) не работает нативно на Forge 1.20.1 — {mod.BestAlternative ?? "удалите или замените Forge-портом"}."
                });
            }
            else if (mod.Platform.Contains("NeoForge", StringComparison.Ordinal) && !WholeForgeRegex().IsMatch(mod.Platform))
            {
                warnings.Add(new DependencyWarning
                {
                    ModId = mod.Id,
                    ModName = mod.Name,
                    Severity = "yellow",
                    Message = $"{mod.Name} рассчитан на {mod.Platform}, а не на Forge 1.20.1."
                });
            }
        }

        var runtimeOnlySet = new HashSet<string>(StringComparer.Ordinal);
        var hardCrashSet = new HashSet<string>(StringComparer.Ordinal);
        foreach (var c in conflicts)
        {
            bool isEmberlensNvidiumOnly =
                c.Rule.Title.Contains("Nvidium и шейдеров", StringComparison.Ordinal) &&
                c.MatchedIds.Contains("nvidium") &&
                c.MatchedIds.Contains("emberlens") &&
                !c.MatchedIds.Contains("oculus") &&
                !c.MatchedIds.Contains("iris-shaders");
            if (isEmberlensNvidiumOnly)
            {
                foreach (var id in c.MatchedIds) runtimeOnlySet.Add(id);
            }
            else
            {
                foreach (var id in c.MatchedIds) hardCrashSet.Add(id);
            }
        }

        var effectiveRuntimeOnly = new HashSet<string>(
            runtimeOnlySet.Where(id => !hardCrashSet.Contains(id)),
            StringComparer.Ordinal);

        return new AssemblyAnalysisResult
        {
            SelectedMods = selectedMods,
            Conflicts = conflicts,
            ConflictingIdSet = conflictingIdSet,
            RuntimeOnlyConflictIdSet = effectiveRuntimeOnly,
            Warnings = warnings
        };
    }

    public static (CheckerTier Tier, string Label, string Reason) GetCheckerTier(
        ModItem mod,
        IReadOnlySet<string>? conflictingIdSet = null,
        string? warningSeverity = null,
        IReadOnlySet<string>? runtimeOnlyConflictIdSet = null)
    {
        bool isInConflict = conflictingIdSet?.Contains(mod.Id) ?? false;
        bool isRuntimeOnlyConflict = isInConflict && (runtimeOnlyConflictIdSet?.Contains(mod.Id) ?? false);

        if (mod.Status == "danger" ||
            (isInConflict && !isRuntimeOnlyConflict) ||
            warningSeverity == "red" ||
            IsUnsupportedOnForge(mod))
        {
            var reason = mod.Status == "danger"
                ? (mod.Warning ?? "Плацебо или высокий риск поломки/фризов")
                : isInConflict
                    ? "Активный конфликт с другим выбранным модом"
                    : (mod.Warning ?? mod.BestAlternative ?? "Несовместим с Forge 1.20.1 или отсутствуют зависимости");
            return (CheckerTier.Red, "Плохо / Конфликт", reason);
        }

        if (isRuntimeOnlyConflict)
        {
            return (
                CheckerTier.Orange,
                "Конфликт режима",
                mod.Id == "emberlens"
                    ? "Не работает при активном Nvidium (шейдеры отключают Mesh Shaders)"
                    : "При включении шейдеров Emberlens Nvidium отключается или конфликтует"
            );
        }

        if (warningSeverity == "orange")
        {
            return (
                CheckerTier.Orange,
                "Риск / Оговорка",
                mod.Warning ?? mod.BestAlternative ?? "Требует внимания и проверки совместимости"
            );
        }

        if (mod.HealthTier == "green" && warningSeverity != "yellow")
        {
            return (CheckerTier.Green, "Стабильно", "Проверенный стабильный лидер для Forge");
        }

        if (warningSeverity != "yellow" &&
            (OrangeCaveatModIds.Contains(mod.Id) ||
             (mod.Status == "redundant" && mod.Id != "fastchest-revived" && mod.Id != "memory-leak-fix-modern-port")))
        {
            var reason = !string.IsNullOrEmpty(mod.CoveredBy)
                ? $"Уже встроен в {mod.CoveredBy}"
                : (mod.Warning ?? mod.BestAlternative ?? "Экспериментальный, дублирующий или требующий много ресурсов мод");
            return (CheckerTier.Orange, "Сомнительно / Риск", reason);
        }

        bool isNeoForgeOnly = mod.Platform.Contains("NeoForge", StringComparison.Ordinal) &&
                              !WholeForgeRegex().IsMatch(mod.Platform);
        bool needsBridgeOrExperimental =
            mod.Platform.Contains("Sinytra", StringComparison.Ordinal) ||
            mod.Platform.Contains("Fabric", StringComparison.Ordinal) ||
            mod.Platform.Contains("Quick Pack-Fix", StringComparison.Ordinal) ||
            mod.Platform.Contains("устаревший порт", StringComparison.Ordinal) ||
            mod.Platform.Contains("1.20.4", StringComparison.Ordinal) ||
            mod.Platform.Contains("Эксперимент", StringComparison.Ordinal) ||
            mod.Id is "concurrent-chunk-management-engine-forge-c2me" or "c2mer" or "tritium" or "nvidium-forged";

        bool isSuitableNative =
            warningSeverity != "yellow" &&
            string.IsNullOrEmpty(mod.CoveredBy) &&
            !isNeoForgeOnly &&
            !needsBridgeOrExperimental &&
            mod.Status is "useful" or "diagnostic";

        if (isSuitableNative)
        {
            var reason = mod.Status == "diagnostic"
                ? "Инструмент диагностики — работает стабильно, FPS сам не повышает"
                : (mod.BestAlternative ?? mod.Description);
            return (CheckerTier.Lime, "Сносно / Подходит", reason);
        }

        var yellowReason = !string.IsNullOrEmpty(mod.Warning) ? mod.Warning : mod.BestAlternative;
        if (string.IsNullOrEmpty(yellowReason))
        {
            if (mod.Platform.Contains("Sinytra", StringComparison.Ordinal))
                yellowReason = "Работает на Forge через Sinytra Connector (требует настройки)";
            else if (!string.IsNullOrEmpty(mod.CoveredBy))
                yellowReason = $"Уже встроен в {mod.CoveredBy}";
            else if (mod.Status == "outdated")
                yellowReason = "Устарел — есть более новый лидер";
            else
                yellowReason = "Условно полезен с ограничениями по версии или лоадеру";
        }

        return (CheckerTier.Yellow, "Не гарантировано", yellowReason);
    }

    public static CheckerTier GetBaseHealthTier(ModItem mod) => GetCheckerTier(mod).Tier;

    public static string? GetDubiousModReason(
        ModItem mod,
        IReadOnlySet<string>? selectedSet = null,
        AssemblyPreferences? prefs = null)
    {
        if (DubiousExplicitReasons.TryGetValue(mod.Id, out var explicitReason))
            return explicitReason;

        if (mod.Status == "danger")
            return string.IsNullOrEmpty(mod.Warning) ? "Опасный или плацебо-мод (риск краша/статтеров)" : mod.Warning;

        if (IsUnsupportedOnForge(mod))
            return $"Не работает нативно на Forge 1.20.1 ({mod.Platform})";

        if (!string.IsNullOrEmpty(mod.CoveredBy))
            return $"Сомнительно: функции уже встроены в {mod.CoveredBy}";

        if (mod.Status is "outdated" or "redundant")
            return mod.BestAlternative ?? mod.Warning ?? "Устаревший или дублирующий мод";

        var sup = GetModSuperseder(mod, selectedSet, prefs, true);
        if (sup is not null)
            return $"Сомнительно в сборке: {sup.Reason}";

        var baseTier = GetBaseHealthTier(mod);
        if (baseTier is CheckerTier.Orange or CheckerTier.Yellow or CheckerTier.Red)
            return !string.IsNullOrEmpty(mod.Warning)
                ? mod.Warning
                : (!string.IsNullOrEmpty(mod.StatusLabel) ? mod.StatusLabel : "Нестабильный, экспериментальный или требующий оговорок мод");

        return null;
    }

    public static List<DiagnosticIssue> BuildCheckerDiagnostics(IEnumerable<string> selectedIds)
    {
        var idList = selectedIds.ToList();
        var analysis = AnalyzeSelection(idList);
        var selectedSet = new HashSet<string>(idList, StringComparer.Ordinal);
        var issues = new List<DiagnosticIssue>();

        for (int i = 0; i < analysis.Conflicts.Count; i++)
        {
            var c = analysis.Conflicts[i];
            bool isEmberlensNvidiumOnly =
                c.Rule.Title.Contains("Nvidium и шейдеров", StringComparison.Ordinal) &&
                c.MatchedIds.Contains("nvidium") &&
                c.MatchedIds.Contains("emberlens") &&
                !c.MatchedIds.Contains("oculus") &&
                !c.MatchedIds.Contains("iris-shaders");

            if (isEmberlensNvidiumOnly)
            {
                issues.Add(new DiagnosticIssue
                {
                    Id = $"conflict-nvidium-emberlens-{i}",
                    Kind = "runtime",
                    Severity = "orange",
                    Title = "Nvidium и Emberlens (шейдеры) не работают вместе",
                    BetweenIds = c.MatchedIds,
                    BetweenNames = c.MatchedNames,
                    Problem = "При активном Nvidium сам Emberlens не вызывает краш, но не работает — включение любого шейдер-пака конфликтует с Nvidium Mesh Shaders.",
                    BestSolution = "Если играете с шейдерами — уберите Nvidium (Emberlens лучше Oculus в паре с Emberbium). Если играете без шейдеров на NVIDIA — уберите Emberlens.",
                    KeepPreferredId = "emberlens",
                    RemoveIds = ["nvidium"]
                });
                continue;
            }

            var matchedMods = c.MatchedIds
                .Select(id => ModDatabase.ModById.GetValueOrDefault(id))
                .OfType<ModItem>()
                .ToList();
            var sortedWinners = matchedMods.OrderByDescending(GetModEffectivenessScore).ToList();
            var winner = sortedWinners.FirstOrDefault();

            if (c.MatchedIds.Contains("emberlens") &&
                c.MatchedIds.Contains("oculus") &&
                selectedSet.Contains("emberbium") &&
                !selectedSet.Contains("embeddium"))
            {
                winner = ModDatabase.ModById.GetValueOrDefault("emberlens") ?? winner;
            }

            var losers = matchedMods.Where(m => m.Id != winner?.Id).ToList();
            issues.Add(new DiagnosticIssue
            {
                Id = $"conflict-{i}-{string.Join("_", c.MatchedIds)}",
                Kind = "conflict",
                Severity = "red",
                Title = c.Rule.Title,
                BetweenIds = c.MatchedIds,
                BetweenNames = c.MatchedNames,
                Problem = c.Rule.Explanation,
                BestSolution = winner is not null
                    ? $"{c.Rule.Recommendation} Лучший выбор здесь: оставить «{winner.Name}» и убрать {string.Join(", ", losers.Select(l => $"«{l.Name}»"))}."
                    : c.Rule.Recommendation,
                KeepPreferredId = winner?.Id,
                RemoveIds = losers.Select(l => l.Id).ToList()
            });
        }

        for (int i = 0; i < analysis.Warnings.Count; i++)
        {
            var w = analysis.Warnings[i];
            ModDatabase.ModById.TryGetValue(w.ModId, out var mod);
            var fixMods = (w.FixModIds ?? [])
                .Select(id => ModDatabase.ModById.GetValueOrDefault(id))
                .OfType<ModItem>()
                .ToList();

            if (w.FixModIds is { Count: > 0 })
            {
                var betweenIds = new[] { w.ModId }.Concat(w.FixModIds).Distinct(StringComparer.Ordinal).ToList();
                var betweenNames = betweenIds.Select(id => ModDatabase.ModById.TryGetValue(id, out var bm) ? bm.Name : id).ToList();
                issues.Add(new DiagnosticIssue
                {
                    Id = $"warn-dep-{i}-{w.ModId}",
                    Kind = "dependency",
                    Severity = w.Severity,
                    Title = $"Не хватает зависимости для {w.ModName}",
                    BetweenIds = betweenIds,
                    BetweenNames = betweenNames,
                    Problem = w.Message,
                    BestSolution = $"Добавьте {string.Join(" + ", fixMods.Select(m => m.Name))} (либо уберите {w.ModName}, если этот стек не нужен).",
                    AddIds = w.FixModIds,
                    RemoveIds = [w.ModId]
                });
                continue;
            }

            var sup = mod is not null ? GetModSuperseder(mod, selectedSet, null, false) : null;
            bool isRedundant = sup is not null || !string.IsNullOrEmpty(mod?.CoveredBy);
            List<string> otherBetweenNames = sup is not null
                ? new[] { w.ModName }.Concat(sup.ByNames).ToList()
                : !string.IsNullOrEmpty(mod?.CoveredBy)
                    ? [w.ModName, mod.CoveredBy]
                    : [w.ModName, "Forge 1.20.1"];

            issues.Add(new DiagnosticIssue
            {
                Id = $"warn-other-{i}-{w.ModId}",
                Kind = isRedundant ? "redundant" : "platform",
                Severity = w.Severity,
                Title = isRedundant ? $"Дублирование функций: {w.ModName}" : $"Проблема совместимости: {w.ModName}",
                BetweenIds = [w.ModId],
                BetweenNames = otherBetweenNames,
                Problem = w.Message,
                BestSolution = !string.IsNullOrEmpty(mod?.BestAlternative)
                    ? mod.BestAlternative
                    : sup is not null
                        ? $"Удалите «{w.ModName}» — его задачи полностью закрывает {string.Join(" + ", sup.ByNames)}."
                        : $"Удалите «{w.ModName}» или замените на нативный аналог для Forge 1.20.1.",
                RemoveIds = [w.ModId]
            });
        }

        return issues;
    }

    public static AssemblyHealthResult EvaluateAssemblyHealth(IEnumerable<string> selectedIds)
    {
        var idList = selectedIds.ToList();
        var analysis = AnalyzeSelection(idList);
        var selectedSet = new HashSet<string>(idList, StringComparer.Ordinal);
        string[] essentialGroupIds = ["core-renderer", "memory-core", "tick-physics-engine", "block-culling", "networking"];
        var missingCoreSlots = new List<MissingSlotWarning>();

        if (idList.Count > 0)
        {
            foreach (var gid in essentialGroupIds)
            {
                if (!ModDatabase.GroupById.TryGetValue(gid, out var grp)) continue;
                if (!grp.MemberIds.Any(id => selectedSet.Contains(id)))
                {
                    if (ModDatabase.ModById.TryGetValue(grp.LeaderId, out var leader))
                    {
                        missingCoreSlots.Add(new MissingSlotWarning
                        {
                            GroupId = grp.Id,
                            GroupTitle = grp.Title,
                            LeaderId = leader.Id,
                            LeaderName = leader.Name
                        });
                    }
                }
            }
        }

        return new AssemblyHealthResult
        {
            SelectedMods = analysis.SelectedMods,
            Conflicts = analysis.Conflicts,
            ConflictingIdSet = analysis.ConflictingIdSet,
            RuntimeOnlyConflictIdSet = analysis.RuntimeOnlyConflictIdSet,
            Warnings = analysis.Warnings,
            MissingCoreSlots = missingCoreSlots
        };
    }

    public static RecipeBridgeStatus GetRecipeBridgeStatus(
        ModItem mod,
        IReadOnlySet<string> selectedSet,
        AssemblyPreferences? prefs = null)
    {
        bool hasEmi = selectedSet.Contains("emi");
        bool hasJei = selectedSet.Contains("just-enough-items-jei");
        bool hasTmrv = selectedSet.Contains("tmrv-updated");
        var viewerPref = prefs?.RecipeViewer;

        if (mod.Platform.Contains("Sinytra", StringComparison.Ordinal))
        {
            if (prefs is not null && !prefs.AllowSinytra)
                return new RecipeBridgeStatus { Compatible = false, Reason = "Прослойка Sinytra Connector выключена" };
            if (selectedSet.Contains("sinytra-connector") && selectedSet.Contains("forgified-fabric-api"))
                return new RecipeBridgeStatus { Compatible = true, ViaBridge = "Через прослойку Sinytra Connector" };
        }

        bool allowsEmi = viewerPref != "none" && (hasEmi || viewerPref is "emi" or "emi-jei");
        bool allowsJei = viewerPref != "none" && (hasJei || viewerPref is "jei" or "emi-jei");

        if (mod.Id == "emi")
        {
            if (viewerPref is "jei" or "none")
                return new RecipeBridgeStatus { Compatible = false, Reason = viewerPref == "none" ? "Просмотрщик рецептов отключён" : "Выбран режим JEI" };
            return new RecipeBridgeStatus { Compatible = true };
        }

        if (mod.Id == "just-enough-items-jei")
        {
            if (viewerPref is "emi" or "none")
                return new RecipeBridgeStatus { Compatible = false, Reason = viewerPref == "none" ? "Просмотрщик рецептов отключён" : "Выбран режим чистого EMI (без JEI)" };
            return new RecipeBridgeStatus { Compatible = true };
        }

        var eco = mod.RecipeEcosystem;
        if (string.IsNullOrEmpty(eco)) return new RecipeBridgeStatus { Compatible = true };

        if (eco == "emi-only")
        {
            if (viewerPref is "jei" or "none")
                return new RecipeBridgeStatus { Compatible = false, Reason = "Аддон работает только с EMI" };
            if (!allowsEmi && (hasJei || viewerPref is not null))
                return new RecipeBridgeStatus { Compatible = false, Reason = "Требует установленный EMI" };
            return new RecipeBridgeStatus { Compatible = true };
        }

        if (eco == "tmrv-bridge")
        {
            if (viewerPref is "none" or "jei")
                return new RecipeBridgeStatus { Compatible = false, Reason = "Прослойка нужна только для EMI без JEI" };
            if (hasJei || viewerPref == "emi-jei")
                return new RecipeBridgeStatus { Compatible = false, Reason = "Не нужен при наличии JEI (EMI сам подхватит JEI)" };
            if (!allowsEmi)
                return new RecipeBridgeStatus { Compatible = false, Reason = "Требует установленный EMI" };
            return new RecipeBridgeStatus { Compatible = true, ViaBridge = "Прослойка JEI-плагинов для EMI" };
        }

        if (eco == "jei-engine")
        {
            if (viewerPref is "emi" or "none")
                return new RecipeBridgeStatus { Compatible = false, Reason = "Работает только с самим JEI (не работает в чистом EMI)" };
            if (!allowsJei && (hasEmi || viewerPref is not null))
                return new RecipeBridgeStatus { Compatible = false, Reason = "Требует сам JEI (не работает через TMRV)" };
            return new RecipeBridgeStatus { Compatible = true };
        }

        if (eco == "jei-plugin")
        {
            if (viewerPref == "none")
                return new RecipeBridgeStatus { Compatible = false, Reason = "Просмотрщик рецептов отключён" };
            if (allowsJei && viewerPref != "emi")
            {
                return hasEmi
                    ? new RecipeBridgeStatus { Compatible = true, ViaBridge = "Нативный мост EMI + JEI" }
                    : new RecipeBridgeStatus { Compatible = true };
            }
            if (allowsEmi)
            {
                return hasTmrv
                    ? new RecipeBridgeStatus { Compatible = true, ViaBridge = "Через прослойку TMRV Updated (EMI)" }
                    : new RecipeBridgeStatus { Compatible = false, Reason = "В чистом EMI не работает без прослойки TMRV Updated" };
            }
            return new RecipeBridgeStatus { Compatible = true };
        }

        if (eco == "emi-or-jei")
        {
            if (viewerPref == "none" && mod.Id != "fast-recipe-search")
                return new RecipeBridgeStatus { Compatible = false, Reason = "Требует JEI или EMI" };
            return new RecipeBridgeStatus { Compatible = true };
        }

        return new RecipeBridgeStatus { Compatible = true };
    }

    public static SupersederInfo? GetModSuperseder(
        ModItem mod,
        IReadOnlySet<string>? selectedSet = null,
        AssemblyPreferences? prefs = null,
        bool allowGlobalSupersede = false)
    {
        var activeSet = selectedSet ?? new HashSet<string>(StringComparer.Ordinal);

        if (mod.SupersededWhenAll is { Count: > 0 })
        {
            if (mod.SupersededWhenAll.All(id => activeSet.Contains(id)))
            {
                var names = mod.SupersededWhenAll.Select(id => ModDatabase.ModById.TryGetValue(id, out var m) ? m.Name : id).ToList();
                return new SupersederInfo
                {
                    ByNames = names,
                    IsActiveInSelection = true,
                    Reason = $"полностью перекрывается более лучшей связкой: {string.Join(" + ", names)}"
                };
            }
        }

        if (mod.SupersededBy is { Count: > 0 })
        {
            var activeBest = mod.SupersededBy.Where(id => activeSet.Contains(id)).ToList();
            if (activeBest.Count > 0)
            {
                var names = activeBest.Select(id => ModDatabase.ModById.TryGetValue(id, out var m) ? m.Name : id).ToList();
                return new SupersederInfo
                {
                    ByNames = names,
                    IsActiveInSelection = true,
                    Reason = $"полностью перекрывается более лучшим: {string.Join(" / ", names)}"
                };
            }

            if (allowGlobalSupersede)
            {
                var viableBest = mod.SupersededBy.Where(id =>
                {
                    if (!ModDatabase.ModById.TryGetValue(id, out var bm) || IsUnsupportedOnForge(bm)) return false;
                    if (prefs is not null && !GetRecipeBridgeStatus(bm, activeSet, prefs).Compatible) return false;
                    if (HasAnyConflictInSet(bm.Id, activeSet)) return false;
                    return true;
                }).ToList();

                if (viableBest.Count > 0)
                {
                    var names = viableBest.Select(id => ModDatabase.ModById.TryGetValue(id, out var m) ? m.Name : id).ToList();
                    return new SupersederInfo
                    {
                        ByNames = names,
                        IsActiveInSelection = false,
                        Reason = $"есть более совершенный аналог: {string.Join(" / ", names)}"
                    };
                }
            }
        }

        if (!string.IsNullOrEmpty(mod.CoveredBy))
        {
            bool activeCover = false;
            var coverLower = ModCoveredByLower.GetValueOrDefault(mod.Id) ?? mod.CoveredBy.ToLowerInvariant();
            foreach (var id in activeSet)
            {
                if (ModDatabase.ModById.TryGetValue(id, out var other) &&
                    (other.Name == mod.CoveredBy || coverLower.Contains(other.Name.ToLowerInvariant(), StringComparison.Ordinal)))
                {
                    activeCover = true;
                    break;
                }
            }
            if (activeCover || allowGlobalSupersede)
            {
                return new SupersederInfo
                {
                    ByNames = [mod.CoveredBy],
                    IsActiveInSelection = activeCover,
                    Reason = $"уже встроено в {mod.CoveredBy}"
                };
            }
        }

        return null;
    }

    public static RestrictionBadge? GetModRestrictionBadge(
        ModItem mod,
        IReadOnlySet<string> selectedSet,
        AssemblyPreferences? prefs = null)
    {
        if (mod.Status == "danger")
            return new RestrictionBadge { Text = "Опасно / Плацебо", Tone = "red" };

        if (IsUnsupportedOnForge(mod))
        {
            var swapName = FabricToForgeSwap.TryGetValue(mod.Id, out var swapId) &&
                           ModDatabase.ModById.TryGetValue(swapId, out var sm)
                ? sm.Name
                : null;
            return new RestrictionBadge
            {
                Text = swapName is not null ? $"Только Fabric → есть {swapName}" : "Не работает на Forge",
                Tone = "red"
            };
        }

        var sup = GetModSuperseder(mod, selectedSet, prefs, true);
        if (sup is not null)
        {
            return new RestrictionBadge
            {
                Text = !string.IsNullOrEmpty(mod.CoveredBy) ? $"Уже в {mod.CoveredBy}" : $"Перекрыт: {sup.ByNames[0]}",
                Tone = "amber"
            };
        }

        var bridge = GetRecipeBridgeStatus(mod, selectedSet, prefs);
        if (!bridge.Compatible)
        {
            return new RestrictionBadge
            {
                Text = bridge.Reason ?? "Не подходит к просмотрщику рецептов",
                Tone = "red"
            };
        }

        if (!selectedSet.Contains(mod.Id))
        {
            var activeConflicts = GetRuleConflictingIds(mod.Id)
                .Where(id => selectedSet.Contains(id))
                .Select(id => ModDatabase.ModById.TryGetValue(id, out var cm) ? cm.Name : null)
                .Where(n => !string.IsNullOrEmpty(n))
                .Cast<string>()
                .ToList();

            if (activeConflicts.Count > 0)
            {
                return new RestrictionBadge
                {
                    Text = $"Конфликт с {string.Join(", ", activeConflicts.Take(2))}",
                    Tone = "red"
                };
            }

            var missingDeps = GetDirectDependencies(mod.Id, selectedSet)
                .Where(id => !selectedSet.Contains(id))
                .Select(id => ModDatabase.ModById.TryGetValue(id, out var dm) ? dm.Name : null)
                .Where(n => !string.IsNullOrEmpty(n))
                .Cast<string>()
                .ToList();

            if (missingDeps.Count > 0)
            {
                return new RestrictionBadge
                {
                    Text = $"Нужен {string.Join(" + ", missingDeps)}",
                    Tone = "sky"
                };
            }
        }

        if (!string.IsNullOrEmpty(bridge.ViaBridge) && mod.RecipeEcosystem == "jei-plugin")
            return new RestrictionBadge { Text = bridge.ViaBridge, Tone = "sky" };

        if (mod.Id == "nvidium" && prefs is not null && prefs.Gpu != "nvidia")
            return new RestrictionBadge { Text = "Только NVIDIA GTX 16xx/RTX", Tone = "violet" };

        if (mod.Id == "accelerated-rendering" && prefs is not null && (prefs.Goal == "potato" || prefs.Gpu == "integrated"))
            return new RestrictionBadge { Text = "OpenGL 4.6 · жрёт VRAM (опасно для встройки)", Tone = "amber" };

        return null;
    }

    public static bool IsModIncompatibleWithSelection(
        ModItem mod,
        IReadOnlySet<string> selectedSet,
        AssemblyPreferences? prefs = null)
    {
        if (selectedSet.Contains(mod.Id)) return false;
        if (IsUnsupportedOnForge(mod)) return true;
        if (!GetRecipeBridgeStatus(mod, selectedSet, prefs).Compatible) return true;
        if (HasAnyConflictInSet(mod.Id, selectedSet)) return true;

        var deps = GetTransitiveDependencies(mod.Id, null, selectedSet);
        if (deps.Any(depId => !selectedSet.Contains(depId) && HasAnyConflictInSet(depId, selectedSet)))
            return true;

        if (prefs is not null && !prefs.AllowSinytra &&
            (mod.Platform.Contains("Sinytra", StringComparison.Ordinal) || deps.Contains("sinytra-connector")))
        {
            return true;
        }

        return false;
    }

    public static bool ShouldHideMod(
        ModItem mod,
        IReadOnlySet<string> selectedSet,
        AssemblyPreferences? prefs,
        bool hideIncompatible,
        bool hideUselessCovered)
    {
        if (selectedSet.Contains(mod.Id)) return false;
        if (hideUselessCovered)
        {
            if (mod.Status == "danger") return true;
            if (IsUnsupportedOnForge(mod)) return true;
            if (GetModSuperseder(mod, selectedSet, prefs, true) is not null) return true;
        }
        if (hideIncompatible && IsModIncompatibleWithSelection(mod, selectedSet, prefs))
            return true;
        return false;
    }

    public static GroupMembersByRole? GetGroupMembersByRole(string groupId)
    {
        if (!ModDatabase.GroupById.TryGetValue(groupId, out var group)) return null;
        ModDatabase.ModById.TryGetValue(group.LeaderId, out var leader);
        var addons = group.AddonIds
            .Select(id => ModDatabase.ModById.GetValueOrDefault(id))
            .OfType<ModItem>()
            .ToList();
        var excludedSet = new HashSet<string>(group.AddonIds, StringComparer.Ordinal) { group.LeaderId };
        var removedAnalogs = group.MemberIds
            .Where(id => !excludedSet.Contains(id))
            .Select(id => ModDatabase.ModById.GetValueOrDefault(id))
            .OfType<ModItem>()
            .ToList();
        return new GroupMembersByRole
        {
            Group = group,
            Leader = leader,
            Addons = addons,
            RemovedAnalogs = removedAnalogs
        };
    }
}
