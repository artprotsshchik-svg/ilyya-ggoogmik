using ModpackWorkbench.Models;

namespace ModpackWorkbench.Services;

public static class ModBuilder
{
    public static SmartAssemblyResult BuildSmartAssembly(AssemblyPreferences prefs)
    {
        var anchorRaw = !string.IsNullOrEmpty(prefs.FocusModId) && ModDatabase.ModById.TryGetValue(prefs.FocusModId, out var foundAnchor)
            ? foundAnchor
            : null;

        var notes = new List<string>();
        var excluded = new List<ExcludedModNote>();
        ModItem? activeAnchor = anchorRaw;

        if (anchorRaw is not null && ModAnalyzer.IsUnsupportedOnForge(anchorRaw))
        {
            if (ModAnalyzer.FabricToForgeSwap.TryGetValue(anchorRaw.Id, out var swapId) &&
                ModDatabase.ModById.TryGetValue(swapId, out var swapMod))
            {
                notes.Add($"Упор «{anchorRaw.Name}» недоступен на Forge 1.20.1 — автоматически переключён нативный порт {swapMod.Name}.");
                excluded.Add(new ExcludedModNote { Name = anchorRaw.Name, Reason = $"Fabric-оригинал заменён на порт {swapMod.Name}." });
                activeAnchor = swapMod;
            }
            else
            {
                notes.Add($"Упор «{anchorRaw.Name}» несовместим с Forge 1.20.1 — упор пропущен.");
                excluded.Add(new ExcludedModNote { Name = anchorRaw.Name, Reason = anchorRaw.BestAlternative ?? "Не работает на Forge 1.20.1." });
                activeAnchor = null;
            }
        }
        else if (anchorRaw is not null && anchorRaw.Status == "danger")
        {
            notes.Add($"Упор «{anchorRaw.Name}» заблокирован умным движком: {anchorRaw.Warning ?? "плацебо или высокий риск краша"}.");
            excluded.Add(new ExcludedModNote { Name = anchorRaw.Name, Reason = anchorRaw.Warning ?? "Опасный или плацебо-мод." });
            activeAnchor = null;
        }
        else if (anchorRaw is not null && !string.IsNullOrEmpty(anchorRaw.CoveredBy))
        {
            notes.Add($"Упор «{anchorRaw.Name}» уже встроен в {anchorRaw.CoveredBy} — использован основной лидер.");
        }

        var portStrategy = !string.IsNullOrEmpty(prefs.PortStrategy)
            ? prefs.PortStrategy
            : (prefs.AllowSinytra ? "hybrid" : "native-forge");

        var recipeViewer = !string.IsNullOrEmpty(prefs.RecipeViewer) ? prefs.RecipeViewer : "jei";
        if (activeAnchor is not null)
        {
            if (activeAnchor.Id is "tmrv-updated" or "emi" || activeAnchor.RecipeEcosystem == "emi-only")
            {
                if (activeAnchor.Id == "tmrv-updated" || recipeViewer is "none" or "jei")
                    recipeViewer = "emi";
            }
            else if (activeAnchor.Id == "just-enough-items-jei" || activeAnchor.RecipeEcosystem == "jei-engine")
            {
                recipeViewer = recipeViewer == "emi-jei" ? "emi-jei" : "jei";
            }
            else if (activeAnchor.RecipeEcosystem == "jei-plugin" && recipeViewer == "none")
            {
                recipeViewer = "jei";
            }
        }

        var effectivePrefs = prefs.Clone();
        effectivePrefs.PortStrategy = portStrategy;
        effectivePrefs.RecipeViewer = recipeViewer;
        effectivePrefs.AllowSinytra = portStrategy == "fabric-sinytra" || prefs.AllowSinytra;
        effectivePrefs.FocusModId = activeAnchor?.Id;

        bool useEmberlensPipeline = false;
        if (activeAnchor is not null)
        {
            var aid = activeAnchor.Id;
            var anchorPair = ModAnalyzer.GetDualPortPairForMod(aid);
            if (aid is "nvidium" or "nvidium-forged")
            {
                effectivePrefs.Gpu = "nvidia";
                effectivePrefs.AllowSinytra = true;
                effectivePrefs.UseShaders = false;
                if (effectivePrefs.Goal == "potato") effectivePrefs.Goal = "balanced";
                notes.Add($"Упор на {activeAnchor.Name}: активирован Sinytra Connector + Embeddium + Flerovium, а шейдеры и Alloyium отключены для работы Mesh Shaders.");
            }
            else if (aid == "alloyium")
            {
                if (effectivePrefs.Goal == "potato") effectivePrefs.Goal = "balanced";
                notes.Add("Упор на Alloyium: выбран нативный OpenGL Compute конвейер (без Nvidium). Доступно переключение на Nvidium (Fabric + Sinytra) в блоке выбора портов.");
            }
            else if (aid is "emberlens" or "emberbium")
            {
                useEmberlensPipeline = true;
                effectivePrefs.UseShaders = true;
                if (effectivePrefs.Goal == "potato") effectivePrefs.Goal = "balanced";
                notes.Add($"Упор на {activeAnchor.Name}: подключена альтернативная связка Emberbium + Emberlens (вместо Embeddium + Oculus + Nvidium).");
            }
            else if (aid is "oculus" or "faster-iris-shadow-mapper")
            {
                effectivePrefs.UseShaders = true;
                if (effectivePrefs.Goal == "potato") effectivePrefs.Goal = "balanced";
                notes.Add($"Упор на {activeAnchor.Name}: включён шейдерный стек Embeddium + Oculus (Nvidium исключён).");
            }
            else if (aid == "c2me-concurrent-chunk-management-engine" || activeAnchor.Platform.Contains("Sinytra", StringComparison.Ordinal))
            {
                effectivePrefs.AllowSinytra = true;
                var pairSuffix = anchorPair is { SwitchableOnForge: true } ? $" Можно в 1 клик переключить на нативный Forge-порт {anchorPair.ForgeLabel}." : string.Empty;
                notes.Add($"Упор на {activeAnchor.Name} (Fabric + Sinytra): подключены Sinytra Connector + Forgified Fabric API.{pairSuffix}");
            }
            else if (aid is "concurrent-chunk-management-engine-forge-c2me" or "c2mer" or "vmp-forge")
            {
                var pairSuffix = anchorPair is { SwitchableOnForge: true } ? $" При желании можно переключить на {anchorPair.FabricLabel}." : string.Empty;
                notes.Add($"Упор на нативный Forge-порт {activeAnchor.Name}: Fabric-дубликаты исключены.{pairSuffix}");
            }
            else if (aid is "pluto" or "krypton-hybrid")
            {
                notes.Add($"Упор на сетевой стек {activeAnchor.Name}: он заменил стандартный Krypton Reno в сетевом слоте.");
            }
            else if (aid == "accelerated-rendering")
            {
                if (effectivePrefs.Goal == "potato") effectivePrefs.Goal = "balanced";
                notes.Add("Упор на Accelerated Rendering: подключён OpenGL 4.6 GPU-инстансинг сущностей и анимаций GeckoLib.");
            }
            else if (aid is "distant-horizons" or "distant-horizons-memory-fix-dh")
            {
                notes.Add("Упор на Distant Horizons: добавлены DH Memory Fix, Noisiumed и асинхронная подгрузка чанков под сверхдальний LOD.");
            }
            else if (aid is "emi" or "tmrv-updated")
            {
                notes.Add($"Упор на {activeAnchor.Name}: включена экосистема EMI + прослойка TMRV Updated + нативные EMI-плагины.");
            }
            else
            {
                notes.Add($"Упор на мод {activeAnchor.Name}: конфликтные аналоги исключены, а недостающие зависимости подтянуты автоматически.");
            }
        }
        else if (portStrategy == "native-forge")
        {
            notes.Add("Режим портов «Нативный Forge»: для C2ME, VMP, GPU-рендера (Alloyium) и сети выбраны прямые Forge-порты без зависимости от Sinytra Connector.");
        }
        else if (portStrategy == "fabric-sinytra")
        {
            notes.Add("Режим портов «Fabric + Sinytra»: подключены оригинальные Fabric-версии (Nvidium, C2ME, VMP) через Sinytra Connector (LTS) + Forgified Fabric API.");
        }
        else
        {
            notes.Add("Режим «Гибрид (Умный баланс)»: VMP и C2ME берутся нативными Forge-портами без лишних миксинов, а для NVIDIA без шейдеров подключается Nvidium через Sinytra.");
        }

        bool isPotato = effectivePrefs.Goal == "potato" || effectivePrefs.Gpu == "integrated";
        bool isHeavy = effectivePrefs.Goal == "max-safe";

        var ids = new HashSet<string>(StringComparer.Ordinal)
        {
            "embeddium",
            "sodium-chloride",
            "immediatelyfast",
            "entity-culling",
            "more-culling-reforged",
            "enhanced-block-entities-reloaded",
            "culllessleaves-reforged",
            "modernfix",
            "ferritecore",
            "alltheleaks-memory-leak-fix",
            "jasione",
            "put-a-plug-in-it-papi",
            "radium-re-reforged",
            "badoptimizations",
            "noisiumed",
            "ixeris",
            "dynamic-fps",
            "packet-fixer",
            "fastvancements",
            "clumps",
            "let-me-despawn",
            "cloth-config-v12",
            "matlib"
        };

        if (recipeViewer == "jei")
        {
            ids.Add("just-enough-items-jei");
            ids.Add("jei-emi-optimizations");
            ids.Add("fast-recipe-search");
            if (isHeavy)
            {
                ids.Add("jeioptimizer");
                ids.Add("just-enough-freezes");
                ids.Add("just-enough-threads");
            }
            notes.Add("Просмотрщик рецептов: Just Enough Items (JEI) + JEI/EMI Optimizations" + (isHeavy ? " + многопоточная индексация JEI." : "."));
            excluded.Add(new ExcludedModNote { Name = "EMI / TMRV Updated / EMI Loot", Reason = "Отключены, так как выбран классический режим JEI." });
        }
        else if (recipeViewer == "emi")
        {
            ids.Add("emi");
            ids.Add("tmrv-updated");
            ids.Add("jei-emi-optimizations");
            ids.Add("fast-recipe-search");
            if (isHeavy)
            {
                ids.Add("emi-loot");
                ids.Add("emi-ores");
                ids.Add("emiffect");
                ids.Add("emi-professions");
            }
            notes.Add("Просмотрщик рецептов: EMI + прослойка TMRV Updated (запускает плагины JEI внутри EMI без самого JEI).");
            excluded.Add(new ExcludedModNote { Name = "Just Enough Items (JEI) + JEIOptimizer / Threads", Reason = "Заменены на EMI + TMRV Updated." });
            if (isHeavy)
            {
                excluded.Add(new ExcludedModNote { Name = "JER / JEP / JEED", Reason = "Полностью перекрыты более быстрыми нативными EMI-плагинами (EMI Loot + EMI Ores, EMI Professions, EMIffect)." });
            }
        }
        else if (recipeViewer == "emi-jei")
        {
            ids.Add("emi");
            ids.Add("just-enough-items-jei");
            ids.Add("jei-emi-optimizations");
            ids.Add("fast-recipe-search");
            if (isHeavy)
            {
                ids.Add("emi-loot");
                ids.Add("emi-ores");
                ids.Add("emiffect");
                ids.Add("emi-professions");
                ids.Add("jeioptimizer");
                ids.Add("just-enough-freezes");
                ids.Add("just-enough-threads");
            }
            notes.Add("Режим EMI + JEI: работают оба просмотрщика (прослойка TMRV Updated отключена, так как EMI нативно подхватывает установленный JEI).");
            excluded.Add(new ExcludedModNote { Name = "TMRV Updated", Reason = "Конфликтует с JEI — не нужен при совместной установке EMI + JEI." });
        }
        else
        {
            excluded.Add(new ExcludedModNote { Name = "JEI / EMI и их плагины", Reason = "Просмотрщик рецептов отключён в настройках сборки." });
        }

        if (effectivePrefs.Mode == "hybrid-server")
        {
            ids.Add("krypton-hybrid");
            ids.Add("connectivity");
            ids.Add("servercore");
            notes.Add("Выбран Krypton Hybrid (сжатие Zstd и фиксы для гибридных серверов Mohist/Arclight) вместо Krypton Reno.");
            excluded.Add(new ExcludedModNote { Name = "Krypton Reno", Reason = "Заменён на Krypton Hybrid для совместимости с гибрид-ядром." });
        }
        else
        {
            ids.Add("krypton-reno");
            if (effectivePrefs.Mode == "server")
            {
                ids.Add("servercore");
                ids.Add("connectivity");
                ids.Add("fast-ip-ping");
                ids.Add("spark");
                if (!effectivePrefs.AllowSinytra && isHeavy)
                    ids.Add("vmp-forge");
                notes.Add("Для сетевой игры добавлены ServerCore, Connectivity, Fast IP Ping и Spark.");
            }
            else
            {
                ids.Add("smooth-boot-reloaded");
                notes.Add("Для одиночной игры добавлен Smooth Boot (Reloaded) для безопасного распределения потоков CPU без поломки спавн-чанков.");
            }
        }

        if (isHeavy)
        {
            string[] heavyMods =
            [
                "duplicate-nbt-flattener", "capability-fix-revived", "structure-dfu-cache",
                "structure-layout-optimizer", "broken-tag-fixer", "stutterfix-reforged",
                "threadtweak-reforged", "smooth-boot-reloaded", "lightspeedre",
                "language-reload-reforged", "fastsuite", "fastfurnace", "fastworkbench",
                "fast-item-frames", "sodium-chloride", "flerovium", "better-biome-reblend",
                "kerria", "servercore", "immersive-optimization", "alternate-current",
                "entity-collision-optimization", "villager-optimizer", "falling-blocks-fps-fix",
                "restmode", "particle-core", "kotlin-for-forge", "fzzy-config", "fast-sounds",
                "soundgc", "reload-looping-sound-fix", "connectivity", "cupboard",
                "geckolib-animation-optimizer", "async-screenshot"
            ];
            foreach (var hid in heavyMods) ids.Add(hid);
            notes.Add("Для максимальной сборки подключены кеш рецептов FastSuite/Furnace/Workbench, Structure DFU Cache и потоковые фиксы.");
        }
        else
        {
            excluded.Add(new ExcludedModNote { Name = "FastSuite / FastFurnace / FastWorkbench", Reason = "Не требуются в лёгкой сборке (лишняя зависимость Placebo)." });
        }

        bool anchorForcesAlloyium = activeAnchor?.Id is "alloyium" or "revidium-accedium";
        bool anchorForcesNvidium = activeAnchor?.Id is "nvidium" or "nvidium-forged";
        bool useNvidium =
            !useEmberlensPipeline &&
            !anchorForcesAlloyium &&
            (anchorForcesNvidium ||
             (effectivePrefs.AllowSinytra &&
              portStrategy != "native-forge" &&
              effectivePrefs.Gpu == "nvidia" &&
              !effectivePrefs.UseShaders &&
              !isPotato));

        if (useEmberlensPipeline)
        {
            ids.Add("emberbium");
            ids.Add("emberlens");
            ids.Add("flywheel");
            ids.Add("vanillin");
            excluded.Add(new ExcludedModNote
            {
                Name = "Embeddium + Oculus + Alloyium + Nvidium",
                Reason = "Заменены на альтернативный шейдерный стек Emberbium + Emberlens."
            });
        }
        else if (isPotato && !anchorForcesAlloyium)
        {
            ids.Add("amd-fsr-upscaler-render-scale-for-embeddium");
            ids.Add("restmode");
            ids.Add("particle-core");
            ids.Add("kotlin-for-forge");
            ids.Add("fzzy-config");
            ids.Add("fast-sounds");
            ids.Add("dynamicrenderdistance");
            notes.Add("Режим слабой графики: добавлены AMD FSR апскейлер, проверенная динамическая дальность DynamicRenderDistance, RestMode и оптимизатор частиц (без дубля PartiCull).");
            excluded.Add(new ExcludedModNote { Name = "FastBoot / Accelerated Rendering / Alloyium", Reason = "FastBoot уже встроен в ModernFix, а Compute Shaders могут перегружать слабую GPU и активно расходовать VRAM." });
        }
        else if (!useNvidium)
        {
            ids.Add("alloyium");
            if (effectivePrefs.Goal == "max-safe" || activeAnchor?.Id == "accelerated-rendering")
            {
                ids.Add("accelerated-rendering");
                notes.Add("Для дискретной GPU добавлены Alloyium, Flerovium и Accelerated Rendering (OpenGL 4.6 — расходует больше VRAM GPU, при нехватке видеопамяти уберите).");
            }
            else
            {
                notes.Add("Добавлен Alloyium (нативный Compute-аддон к Embeddium для любых видеокарт без Sinytra).");
            }
        }
        else
        {
            ids.Add("flerovium");
            excluded.Add(new ExcludedModNote { Name = "Alloyium", Reason = "Заменён на Nvidium (оба мода перехватывают GPU-рендер чанков Embeddium)." });
        }

        if (!useEmberlensPipeline && effectivePrefs.UseShaders && effectivePrefs.Goal != "potato")
        {
            ids.Add("oculus");
            ids.Add("faster-iris-shadow-mapper");
            ids.Add("gputape");
            notes.Add("Активированы шейдеры: Oculus + Faster Iris Shadow Mapper + GPUTape (стандартный стек на Embeddium).");
            excluded.Add(new ExcludedModNote { Name = "Emberlens", Reason = "Форк Oculus, требующий Emberbium вместо Embeddium (исключён во избежание конфликта)." });
            if (effectivePrefs.AllowSinytra)
            {
                excluded.Add(new ExcludedModNote { Name = "Nvidium", Reason = "Жёсткий конфликт с буферами шейдеров Oculus (исключён автоматически)." });
            }
        }
        else if (effectivePrefs.UseShaders && effectivePrefs.Goal == "potato")
        {
            excluded.Add(new ExcludedModNote { Name = "Oculus и шейдеры", Reason = "Пропущены, так как выбрана цель «Слабый ПК»." });
        }

        if (effectivePrefs.UseCreate)
        {
            ids.Add("flywheel");
            ids.Add("vanillin");
            ids.Add("immersive-optimization");
            ids.Add("alternate-current");
            ids.Add("entity-collision-optimization");
            ids.Add("observable");
            notes.Add("Для Create/индустрии добавлены Flywheel, Vanillin, Alternate Current, Immersive Optimization и Observable.");
        }

        bool useFabricC2ME =
            effectivePrefs.AllowSinytra &&
            activeAnchor?.Id != "concurrent-chunk-management-engine-forge-c2me" &&
            activeAnchor?.Id != "c2mer" &&
            (activeAnchor?.Id == "c2me-concurrent-chunk-management-engine" || portStrategy == "fabric-sinytra");

        bool useNativeC2MEF =
            !useFabricC2ME &&
            activeAnchor?.Id != "c2me-concurrent-chunk-management-engine" &&
            (activeAnchor?.Id == "concurrent-chunk-management-engine-forge-c2me" ||
             (!isPotato && (isHeavy || portStrategy == "native-forge" || effectivePrefs.AllowSinytra)));

        bool useFabricVMP =
            effectivePrefs.AllowSinytra &&
            activeAnchor?.Id != "vmp-forge" &&
            (activeAnchor?.Id == "very-many-players-vmp" ||
             (portStrategy == "fabric-sinytra" && effectivePrefs.Mode != "singleplayer" && isHeavy));

        if (useFabricC2ME)
        {
            ids.Add("c2me-concurrent-chunk-management-engine");
            excluded.Add(new ExcludedModNote { Name = "C2ME-Forge (C2MEF)", Reason = "Выбран оригинальный Fabric C2ME через Sinytra Connector." });
        }
        else if (useNativeC2MEF)
        {
            ids.Add("concurrent-chunk-management-engine-forge-c2me");
            if (effectivePrefs.AllowSinytra)
            {
                excluded.Add(new ExcludedModNote { Name = "C2ME (Fabric)", Reason = "Заменён на нативный C2ME-Forge (C2MEF) во избежание конфликтов шины событий." });
            }
        }

        if (useFabricVMP)
        {
            ids.Add("very-many-players-vmp");
            ids.Remove("vmp-forge");
            excluded.Add(new ExcludedModNote { Name = "Very Many Players (Forge)", Reason = "Выбрана Fabric-версия VMP через Sinytra Connector." });
        }
        else if (effectivePrefs.Mode != "singleplayer" && isHeavy)
        {
            ids.Add("vmp-forge");
            if (effectivePrefs.AllowSinytra)
            {
                excluded.Add(new ExcludedModNote { Name = "VMP (Fabric)", Reason = "Выбран нативный VMP (Forge) без конфликтов миксинов через Sinytra." });
            }
        }

        if (effectivePrefs.AllowSinytra)
        {
            bool needsSinytraBridge =
                useNvidium ||
                useFabricC2ME ||
                useFabricVMP ||
                (activeAnchor?.Platform.Contains("Sinytra", StringComparison.Ordinal) ?? false);

            if (needsSinytraBridge || portStrategy == "fabric-sinytra")
            {
                ids.Add("sinytra-connector");
                ids.Add("forgified-fabric-api");
                ids.Add("connector-extras");
            }

            if (useNvidium)
            {
                ids.Add("nvidium");
                notes.Add("Через Sinytra подключён Nvidium (RTX/GTX 16xx+). В connector.json добавьте алиас \"embeddium\": \"sodium\" (или Nvidium Forged).");
            }
            else if (activeAnchor?.Id != "bobby-reforged")
            {
                ids.Add("distant-horizons");
                ids.Add("distant-horizons-memory-fix-dh");
                if (portStrategy == "native-forge" && effectivePrefs.Gpu == "nvidia" && !effectivePrefs.UseShaders && !isPotato)
                {
                    excluded.Add(new ExcludedModNote { Name = "Nvidium", Reason = "Отключён из-за приоритета «Нативный Forge» (используется Alloyium + Distant Horizons)." });
                }
                else if (effectivePrefs.Gpu != "nvidia")
                {
                    excluded.Add(new ExcludedModNote { Name = "Nvidium", Reason = "Требует видеокарту NVIDIA (GTX 16xx / RTX 20xx+), заменён на Alloyium + Distant Horizons." });
                }
                else if (isPotato)
                {
                    excluded.Add(new ExcludedModNote { Name = "Nvidium", Reason = "Для слабого ПК вместо Mesh Shaders подключён Distant Horizons." });
                }
                notes.Add("Подключён Distant Horizons (безопасный LOD на любой GPU и совместим с шейдерами).");
            }
        }
        else
        {
            excluded.Add(new ExcludedModNote { Name = "Nvidium / C2ME (Fabric) / VMP (Fabric)", Reason = "Прослойка Sinytra Connector выключена — сборка на 100% нативном Forge." });
        }

        if (activeAnchor is not null && string.IsNullOrEmpty(activeAnchor.CoveredBy))
        {
            foreach (var cid in ModAnalyzer.GetRuleConflictingIds(activeAnchor.Id))
            {
                if (ids.Remove(cid))
                {
                    var cname = ModDatabase.ModById.TryGetValue(cid, out var cm) ? cm.Name : cid;
                    if (!excluded.Any(e => e.Name == cname))
                    {
                        excluded.Add(new ExcludedModNote { Name = cname, Reason = $"Заменён из-за упора сборки на {activeAnchor.Name}." });
                    }
                }
            }

            ids.Add(activeAnchor.Id);
            foreach (var depId in ModAnalyzer.GetTransitiveDependencies(activeAnchor.Id))
            {
                foreach (var depConflict in ModAnalyzer.GetRuleConflictingIds(depId))
                {
                    if (ids.Remove(depConflict))
                    {
                        var cname = ModDatabase.ModById.TryGetValue(depConflict, out var cm) ? cm.Name : depConflict;
                        if (!excluded.Any(e => e.Name == cname))
                        {
                            var depName = ModDatabase.ModById.TryGetValue(depId, out var dm) ? dm.Name : depId;
                            excluded.Add(new ExcludedModNote { Name = cname, Reason = $"Конфликтует с зависимостью упора ({depName})." });
                        }
                    }
                }
                ids.Add(depId);
            }

            bool cascadeChanged = true;
            while (cascadeChanged)
            {
                cascadeChanged = false;
                foreach (var id in ids.ToList())
                {
                    if (ModAnalyzer.GetDirectDependencies(id).Any(d => !ids.Contains(d)))
                    {
                        ids.Remove(id);
                        cascadeChanged = true;
                    }
                }
            }
        }

        excluded.Add(new ExcludedModNote { Name = "OptiFine / Xenon / Rubidium", Reason = "Конфликтуют с базовым рендерером." });
        excluded.Add(new ExcludedModNote { Name = "Lithium / Krypton / Indium / DashLoader", Reason = "Fabric-оригиналы заменены на нативные порты Forge (Radium Re-Reforged, Krypton Reno/Hybrid/Pluto, ModernFix)." });

        var cleaned = SmartFixAssembly(ids, effectivePrefs);
        return new SmartAssemblyResult
        {
            ModIds = cleaned,
            Notes = notes,
            Excluded = excluded
        };
    }

    public static List<string> SmartFixAssembly(IEnumerable<string> selectedIds, AssemblyPreferences? prefs = null)
    {
        var next = new HashSet<string>(selectedIds, StringComparer.Ordinal);
        var focusId = string.IsNullOrEmpty(prefs?.FocusModId) ? null : prefs.FocusModId;

        foreach (var id in next.ToList())
        {
            if (!ModDatabase.ModById.TryGetValue(id, out var mod))
            {
                next.Remove(id);
                continue;
            }
            if (ModAnalyzer.IsUnsupportedOnForge(mod))
            {
                next.Remove(id);
                if (ModAnalyzer.FabricToForgeSwap.TryGetValue(id, out var swap))
                    next.Add(swap);
                continue;
            }
            if (mod.Status == "danger" || !string.IsNullOrEmpty(mod.CoveredBy))
            {
                next.Remove(id);
            }
        }

        foreach (var g in ModDatabase.ModGroups)
        {
            if (!g.Exclusive) continue;
            var addonSet = new HashSet<string>(g.AddonIds, StringComparer.Ordinal);
            var pool = g.MemberIds.Where(id => !addonSet.Contains(id));
            var chosen = pool.Where(id => next.Contains(id)).ToList();
            if (chosen.Count > 1)
            {
                chosen.Sort((a, b) =>
                {
                    if (focusId is not null && a == focusId) return -1;
                    if (focusId is not null && b == focusId) return 1;
                    if (a == g.LeaderId) return -1;
                    if (b == g.LeaderId) return 1;
                    var ma = ModDatabase.ModById[a];
                    var mb = ModDatabase.ModById[b];
                    return ModAnalyzer.GetModEffectivenessScore(mb).CompareTo(ModAnalyzer.GetModEffectivenessScore(ma));
                });
                foreach (var loser in chosen.Skip(1)) next.Remove(loser);
            }
        }

        if (next.Contains("nvidium") && next.Contains("oculus"))
        {
            if (focusId is "nvidium" or "nvidium-forged") next.Remove("oculus");
            else if (focusId == "oculus") next.Remove("nvidium");
            else next.Remove(prefs?.UseShaders == true ? "nvidium" : "oculus");
        }
        if (next.Contains("nvidium") && next.Contains("emberlens"))
        {
            if (focusId is "nvidium" or "nvidium-forged") next.Remove("emberlens");
            else if (focusId is "emberlens" or "emberbium") next.Remove("nvidium");
            else next.Remove(prefs?.UseShaders == true ? "nvidium" : "emberlens");
        }
        if (next.Contains("nvidium") && next.Contains("alloyium"))
        {
            if (focusId == "alloyium") next.Remove("nvidium");
            else if (focusId is "nvidium" or "nvidium-forged") next.Remove("alloyium");
            else
            {
                bool keepNvidium =
                    prefs?.Gpu == "nvidia" &&
                    prefs.UseShaders != true &&
                    prefs.AllowSinytra &&
                    prefs.PortStrategy != "native-forge";
                next.Remove(keepNvidium ? "alloyium" : "nvidium");
            }
        }
        if (next.Contains("nvidium") && next.Contains("revidium-accedium"))
            next.Remove(focusId == "revidium-accedium" ? "nvidium" : "revidium-accedium");

        if (next.Contains("oculus") && next.Contains("emberlens"))
        {
            if (focusId is "emberlens" or "emberbium" || (next.Contains("emberbium") && !next.Contains("embeddium")))
                next.Remove("oculus");
            else
                next.Remove("emberlens");
        }

        if (next.Contains("c2me-concurrent-chunk-management-engine") && next.Contains("concurrent-chunk-management-engine-forge-c2me"))
        {
            if (focusId == "concurrent-chunk-management-engine-forge-c2me")
                next.Remove("c2me-concurrent-chunk-management-engine");
            else if (focusId == "c2me-concurrent-chunk-management-engine")
                next.Remove("concurrent-chunk-management-engine-forge-c2me");
            else if (prefs?.PortStrategy == "fabric-sinytra")
                next.Remove("concurrent-chunk-management-engine-forge-c2me");
            else
                next.Remove("c2me-concurrent-chunk-management-engine");
        }

        if (next.Contains("c2mer") &&
            (next.Contains("c2me-concurrent-chunk-management-engine") || next.Contains("concurrent-chunk-management-engine-forge-c2me")))
        {
            if (focusId == "c2mer")
            {
                next.Remove("c2me-concurrent-chunk-management-engine");
                next.Remove("concurrent-chunk-management-engine-forge-c2me");
            }
            else
            {
                next.Remove("c2mer");
            }
        }

        if (next.Contains("vmp-forge") && next.Contains("very-many-players-vmp"))
        {
            if (focusId == "very-many-players-vmp") next.Remove("vmp-forge");
            else if (focusId == "vmp-forge") next.Remove("very-many-players-vmp");
            else if (prefs?.PortStrategy == "fabric-sinytra") next.Remove("vmp-forge");
            else next.Remove("very-many-players-vmp");
        }

        if (next.Contains("noisiumed") && next.Contains("noisium"))
            next.Remove(focusId == "noisium" ? "noisiumed" : "noisium");

        if (next.Contains("modernfix") && next.Contains("dashloader"))
            next.Remove("dashloader");

        if (next.Contains("jasione") && next.Contains("redirected")) next.Remove("redirected");
        if (next.Contains("jasione") && next.Contains("redirector-modern")) next.Remove("redirector-modern");

        if (next.Contains("krypton-hybrid") && next.Contains("aegisnetworking-krypton-hybrid"))
            next.Remove(focusId == "aegisnetworking-krypton-hybrid" ? "krypton-hybrid" : "aegisnetworking-krypton-hybrid");

        if (next.Contains("tmrv-updated") && next.Contains("just-enough-items-jei"))
        {
            if (focusId == "tmrv-updated") next.Remove("just-enough-items-jei");
            else if (focusId == "just-enough-items-jei") next.Remove("tmrv-updated");
            else if (prefs?.RecipeViewer == "emi") next.Remove("just-enough-items-jei");
            else next.Remove("tmrv-updated");
        }

        if (next.Contains("accelerated-rendering") && next.Contains("accelerated-rendering-turned"))
            next.Remove(focusId == "accelerated-rendering-turned" ? "accelerated-rendering" : "accelerated-rendering-turned");

        foreach (var id in next.ToList())
        {
            if (focusId is not null && id == focusId) continue;
            if (!ModDatabase.ModById.TryGetValue(id, out var mod)) continue;
            var sup = ModAnalyzer.GetModSuperseder(mod, next, prefs, false);
            if (sup is { IsActiveInSelection: true })
                next.Remove(id);
        }

        bool pruneOrphans = true;
        while (pruneOrphans)
        {
            pruneOrphans = false;
            foreach (var id in next.ToList())
            {
                var deps = ModAnalyzer.GetDirectDependencies(id, next);
                if (deps.Any(d => !next.Contains(d) && ModAnalyzer.GetRuleConflictingIds(d).Any(c => next.Contains(c))))
                {
                    next.Remove(id);
                    pruneOrphans = true;
                }
            }
        }

        foreach (var id in next.ToList())
        {
            foreach (var dep in ModAnalyzer.GetTransitiveDependencies(id, null, next))
            {
                if (!ModAnalyzer.GetRuleConflictingIds(dep).Any(c => next.Contains(c)))
                {
                    next.Add(dep);
                }
            }
        }

        return next.ToList();
    }

    public static SmartToggleResult ToggleModSmart(
        IEnumerable<string> currentIds,
        string modId,
        bool strict,
        AssemblyPreferences? prefs = null)
    {
        var currentList = currentIds.ToList();
        if (!ModDatabase.ModById.TryGetValue(modId, out var mod))
        {
            return new SmartToggleResult { NextIds = currentList };
        }

        var currentSet = new HashSet<string>(currentList, StringComparer.Ordinal);
        if (currentSet.Contains(modId))
        {
            var next = new HashSet<string>(currentList, StringComparer.Ordinal);
            next.Remove(modId);
            if (!strict)
            {
                return new SmartToggleResult { NextIds = next.ToList() };
            }

            var removedDependents = new List<string>();
            bool changed = true;
            while (changed)
            {
                changed = false;
                foreach (var id in next.ToList())
                {
                    ModDatabase.ModById.TryGetValue(id, out var depMod);
                    var deps = ModAnalyzer.GetDirectDependencies(id, next);
                    bool pluginLostAllHosts =
                        depMod?.RecipeEcosystem == "jei-plugin" &&
                        !next.Contains("just-enough-items-jei") &&
                        !(next.Contains("emi") && next.Contains("tmrv-updated"));

                    if (deps.Any(d => !next.Contains(d)) || pluginLostAllHosts)
                    {
                        next.Remove(id);
                        removedDependents.Add(depMod?.Name ?? id);
                        changed = true;
                    }
                }
            }

            return new SmartToggleResult
            {
                NextIds = next.ToList(),
                Notice = removedDependents.Count > 0
                    ? $"Убран {mod.Name} и зависимые от него моды: {string.Join(", ", removedDependents)}."
                    : null,
                NoticeTone = "info"
            };
        }

        if (!strict)
        {
            currentList.Add(modId);
            return new SmartToggleResult { NextIds = currentList };
        }

        if (mod.Status == "danger")
        {
            return new SmartToggleResult
            {
                NextIds = currentList,
                Notice = $"{mod.Name} заблокирован умной защитой: {mod.Warning ?? "плацебо или высокий риск краша"}. Отключите режим 1-слота для ручного теста.",
                NoticeTone = "warn"
            };
        }

        if (ModAnalyzer.IsUnsupportedOnForge(mod))
        {
            if (ModAnalyzer.FabricToForgeSwap.TryGetValue(mod.Id, out var swapId) &&
                ModDatabase.ModById.TryGetValue(swapId, out var swapMod))
            {
                var swapped = ToggleModSmart(currentList, swapId, strict, prefs);
                return new SmartToggleResult
                {
                    NextIds = swapped.NextIds,
                    Notice = $"{mod.Name} не работает на Forge 1.20.1 — автоматически подключён Forge-порт {swapMod.Name}.",
                    NoticeTone = "info"
                };
            }

            return new SmartToggleResult
            {
                NextIds = currentList,
                Notice = $"{mod.Name} не работает на Forge 1.20.1 ({mod.BestAlternative ?? "несовместим даже через Sinytra"}).",
                NoticeTone = "warn"
            };
        }

        if (!string.IsNullOrEmpty(mod.CoveredBy))
        {
            return new SmartToggleResult
            {
                NextIds = currentList,
                Notice = $"{mod.Name} не нужен — его функции уже встроены в {mod.CoveredBy}.",
                NoticeTone = "warn"
            };
        }

        var addNext = new HashSet<string>(currentList, StringComparer.Ordinal);
        var removedNames = new List<string>();
        var addedDeps = new List<string>();

        foreach (var cid in ModAnalyzer.GetRuleConflictingIds(modId))
        {
            if (addNext.Remove(cid))
            {
                removedNames.Add(ModDatabase.ModById.TryGetValue(cid, out var cm) ? cm.Name : cid);
            }
        }

        addNext.Add(modId);

        foreach (var depId in ModAnalyzer.GetTransitiveDependencies(modId, null, addNext))
        {
            if (!addNext.Contains(depId))
            {
                foreach (var depConflict in ModAnalyzer.GetRuleConflictingIds(depId))
                {
                    if (addNext.Remove(depConflict))
                    {
                        removedNames.Add(ModDatabase.ModById.TryGetValue(depConflict, out var cm) ? cm.Name : depConflict);
                    }
                }
                addNext.Add(depId);
                addedDeps.Add(ModDatabase.ModById.TryGetValue(depId, out var dm) ? dm.Name : depId);
            }
        }

        bool pruneChanged = true;
        while (pruneChanged)
        {
            pruneChanged = false;
            foreach (var id in addNext.ToList())
            {
                if (ModAnalyzer.GetDirectDependencies(id, addNext).Any(d => !addNext.Contains(d)))
                {
                    addNext.Remove(id);
                    removedNames.Add(ModDatabase.ModById.TryGetValue(id, out var rm) ? rm.Name : id);
                    pruneChanged = true;
                }
            }
        }

        var parts = new List<string>();
        if (addedDeps.Count > 0) parts.Add($"добраны зависимости: {string.Join(", ", addedDeps)}");
        if (removedNames.Count > 0) parts.Add($"заменены конфликтные: {string.Join(", ", removedNames)}");

        return new SmartToggleResult
        {
            NextIds = addNext.ToList(),
            Notice = parts.Count > 0 ? $"{mod.Name} выбран — {string.Join("; ", parts)}." : null,
            NoticeTone = "info"
        };
    }

    public static (List<string> NextIds, List<string> CascadedNames) RemoveModsCascade(
        IEnumerable<string> currentIds,
        IEnumerable<string> removeIds)
    {
        var next = new HashSet<string>(currentIds, StringComparer.Ordinal);
        foreach (var rid in removeIds) next.Remove(rid);

        var cascadedNames = new List<string>();
        bool changed = true;
        while (changed)
        {
            changed = false;
            foreach (var id in next.ToList())
            {
                if (ModAnalyzer.GetDirectDependencies(id, next).Any(d => !next.Contains(d)))
                {
                    next.Remove(id);
                    cascadedNames.Add(ModDatabase.ModById.TryGetValue(id, out var rm) ? rm.Name : id);
                    changed = true;
                }
            }
        }
        return (next.ToList(), cascadedNames);
    }
}
