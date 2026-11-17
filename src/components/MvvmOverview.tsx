import { useMemo, useState } from 'react';
import { CheckCircle2, Cpu, FileCode2, FolderSearch, Layers, Palette, ShieldCheck, Wrench } from 'lucide-react';
import metaJson from '../data/db/meta.json';
import presetsJson from '../data/db/presets.json';
import groupsJson from '../data/db/groups.json';
import rulesJson from '../data/db/rules.json';
import modsRender from '../data/db/mods-render.json';
import modsMem from '../data/db/mods-memory-cpu.json';
import modsWorld from '../data/db/mods-world-tps.json';
import modsNet from '../data/db/mods-net-recipes-misc.json';

interface MvvmLayerCard {
  layer: string;
  namespace: string;
  items: { name: string; role: string }[];
}

const MVVM_LAYERS: MvvmLayerCard[] = [
  {
    layer: 'Models',
    namespace: 'ModpackWorkbench.Models',
    items: [
      { name: 'ModItem / ModGroupInfo / IncompatibilityRule', role: 'Десериализация JSON-базы модов, слотов и правил конфликтов' },
      { name: 'AssemblyPreferences / SmartAssemblyResult', role: 'Параметры GPU, режима игры, стратегии портов и рецептов' },
      { name: 'DiagnosticIssue / ConflictDetection / CheckerTier', role: 'Тиры стабильности (Green/Lime/Yellow/Orange/Red) и решения' },
      { name: 'BuilderThemeConfig / PersistedAppState', role: 'Конфигурация темы, фракталов и сохранение состояния в AppData' },
    ],
  },
  {
    layer: 'Services',
    namespace: 'ModpackWorkbench.Services',
    items: [
      { name: 'ModDatabase', role: 'Загрузка встроенных EmbeddedResource JSON-файлов (db/*.json)' },
      { name: 'ModAnalyzer', role: 'Поиск с русским стеммингом и синонимами, граф зависимостей и диагностика' },
      { name: 'ModBuilder', role: 'Умный авто-конструктор BuildSmartAssembly, SmartFixAssembly и ToggleModSmart' },
      { name: 'ModTreeService', role: 'Иерархия Мод -> Аддоны, экспорт с ссылками CurseForge и парсер списков' },
      { name: 'JarMatcherService', role: 'Нативное сканирование папки mods/ и сопоставление имён .jar файлов' },
      { name: 'PersistenceService & ThemeService', role: 'Сохранение JSON-состояния и динамических кистей Avalonia' },
    ],
  },
  {
    layer: 'ViewModels (CommunityToolkit.Mvvm)',
    namespace: 'ModpackWorkbench.ViewModels',
    items: [
      { name: 'MainWindowViewModel', role: 'Единое синхронизированное состояние сборки для всех 4 вкладок' },
      { name: 'CatalogViewModel', role: 'Фильтры по категориям, статусам, совместимости, сортировка и аддоны' },
      { name: 'AssemblyBuilderViewModel', role: 'Конструктор сборок по слотам, 1-в-слоте, мультиплеер-пакет и авто-сборка' },
      { name: 'PresetsViewModel', role: 'Готовые сборки (Nvidium + Emberbium, Alloyium + Embeddium) и Золотой стандарт' },
      { name: 'CheckerViewModel', role: 'Проверка конфликтов, 1-клик фиксы, IStorageProvider сканер и буфер обмена' },
      { name: 'ModDetailViewModel & ThemeConfiguratorViewModel', role: 'Модальное окно мода с переключателем портов и редактор темы' },
    ],
  },
  {
    layer: 'Views & Controls (Avalonia 11.2)',
    namespace: 'ModpackWorkbench.Views',
    items: [
      { name: 'MainWindow.axaml', role: 'Верхняя панель вкладок, мета-строка базы, боковая панель темы и StorageProvider' },
      { name: 'FractalBackdropControl.cs', role: 'Отрисовка углового свечения и фракталов (Жюлиа, Лоренц, Папоротник, Дракон, Кох)' },
      { name: 'CatalogView / AssemblyBuilderView / PresetsView / CheckerView', role: 'Полный набор экранов «Верстака сборок» на нативном XAML' },
      { name: 'ModDetailWindow.axaml & ThemeConfiguratorView.axaml', role: 'Диалог деталей мода и настройка темы в реальном времени' },
    ],
  },
];

export function MvvmOverview() {
  const [selectedPresetIdx, setSelectedPresetIdx] = useState(0);

  const totalMods = useMemo(
    () => modsRender.length + modsMem.length + modsWorld.length + modsNet.length,
    []
  );

  const activePreset = presetsJson[selectedPresetIdx] ?? presetsJson[0];

  return (
    <div className="space-y-4">
      {/* Database parity stats */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
        <div className="ui-block p-3.5">
          <div className="text-[11px] text-[var(--ink-3)] uppercase tracking-wider">Модов в базе</div>
          <div className="text-xl font-bold font-mono text-[var(--ink-1)] mt-1">{totalMods}</div>
          <div className="text-[11px] text-emerald-400 mt-0.5">Обновлено {metaJson.updatedAt}</div>
        </div>
        <div className="ui-block p-3.5">
          <div className="text-[11px] text-[var(--ink-3)] uppercase tracking-wider">Слотов оптимизации</div>
          <div className="text-xl font-bold font-mono text-[var(--ink-1)] mt-1">{groupsJson.length}</div>
          <div className="text-[11px] text-[var(--ink-2)] mt-0.5">Лидеры, аддоны и аналоги</div>
        </div>
        <div className="ui-block p-3.5">
          <div className="text-[11px] text-[var(--ink-3)] uppercase tracking-wider">Правил конфликтов</div>
          <div className="text-xl font-bold font-mono text-[var(--ink-1)] mt-1">{rulesJson.length}</div>
          <div className="text-[11px] text-[var(--ink-2)] mt-0.5">OptiFine, Sodium, Nvidium и др.</div>
        </div>
        <div className="ui-block p-3.5">
          <div className="text-[11px] text-[var(--ink-3)] uppercase tracking-wider">Платформа порта</div>
          <div className="text-xl font-bold font-mono text-sky-400 mt-1">.NET 8</div>
          <div className="text-[11px] text-[var(--ink-2)] mt-0.5">Avalonia UI 11.2 · MVVM</div>
        </div>
      </div>

      {/* Desktop capabilities replaced from Electron */}
      <div className="ui-block p-4 space-y-3">
        <div className="flex items-center gap-2 text-sm font-semibold text-[var(--ink-1)]">
          <Cpu className="w-4 h-4 text-[var(--accent-text)]" />
          <span>Замена Electron/React-стека на нативный C# + Avalonia UI</span>
        </div>
        <div className="grid sm:grid-cols-2 lg:grid-cols-4 gap-2.5 text-xs">
          <div className="p-2.5 rounded bg-[var(--surface-sub)] border border-[var(--border-soft)] space-y-1">
            <div className="flex items-center gap-1.5 font-semibold text-[var(--ink-1)]">
              <FolderSearch className="w-3.5 h-3.5 text-emerald-400" />
              <span>Сканер папки mods/</span>
            </div>
            <p className="text-[var(--ink-2)]">
              Electron IPC <code className="text-sky-300">dialog.showOpenDialog</code> заменён на Avalonia <code className="text-sky-300">IStorageProvider</code> и <code className="text-sky-300">JarMatcherService</code>.
            </p>
          </div>
          <div className="p-2.5 rounded bg-[var(--surface-sub)] border border-[var(--border-soft)] space-y-1">
            <div className="flex items-center gap-1.5 font-semibold text-[var(--ink-1)]">
              <Wrench className="w-3.5 h-3.5 text-sky-400" />
              <span>Конструктор и Чекер</span>
            </div>
            <p className="text-[var(--ink-2)]">
              Логика <code className="text-sky-300">ModBuilder</code>, <code className="text-sky-300">ModAnalyzer</code> и <code className="text-sky-300">ModTreeService</code> портирована 1-в-1 без упрощений.
            </p>
          </div>
          <div className="p-2.5 rounded bg-[var(--surface-sub)] border border-[var(--border-soft)] space-y-1">
            <div className="flex items-center gap-1.5 font-semibold text-[var(--ink-1)]">
              <Palette className="w-3.5 h-3.5 text-violet-400" />
              <span>Фракталы и темы</span>
            </div>
            <p className="text-[var(--ink-2)]">
              Canvas-фон перенесён в нативный контрол <code className="text-sky-300">FractalBackdropControl</code> на <code className="text-sky-300">DrawingContext</code>.
            </p>
          </div>
          <div className="p-2.5 rounded bg-[var(--surface-sub)] border border-[var(--border-soft)] space-y-1">
            <div className="flex items-center gap-1.5 font-semibold text-[var(--ink-1)]">
              <ShieldCheck className="w-3.5 h-3.5 text-amber-400" />
              <span>xUnit + Headless UI</span>
            </div>
            <p className="text-[var(--ink-2)]">
              Все тесты логики и проверка рендера <code className="text-sky-300">MainWindow</code> перенесены на xUnit и <code className="text-sky-300">Avalonia.Headless.XUnit</code>.
            </p>
          </div>
        </div>
      </div>

      {/* MVVM Architecture Cards */}
      <div className="grid md:grid-cols-2 gap-3">
        {MVVM_LAYERS.map(card => (
          <div key={card.layer} className="ui-block overflow-hidden">
            <div className="px-4 py-2.5 bg-[var(--surface-sub)] border-b border-[var(--border-soft)] flex items-center justify-between">
              <div className="flex items-center gap-2">
                <Layers className="w-4 h-4 text-[var(--accent-text)]" />
                <span className="text-sm font-semibold text-[var(--ink-1)]">{card.layer}</span>
              </div>
              <span className="text-[11px] font-mono text-[var(--ink-3)]">{card.namespace}</span>
            </div>
            <div className="divide-y divide-[var(--border-soft)]">
              {card.items.map(item => (
                <div key={item.name} className="px-4 py-2.5 space-y-0.5">
                  <div className="flex items-center gap-1.5 text-xs font-semibold font-mono text-[var(--ink-1)]">
                    <FileCode2 className="w-3.5 h-3.5 text-emerald-400 shrink-0" />
                    <span>{item.name}</span>
                  </div>
                  <p className="text-xs text-[var(--ink-2)]">{item.role}</p>
                </div>
              ))}
            </div>
          </div>
        ))}
      </div>

      {/* Presets parity preview */}
      {activePreset && (
        <div className="ui-block p-4 space-y-3">
          <div className="flex flex-wrap items-center justify-between gap-2">
            <div className="text-sm font-semibold text-[var(--ink-1)]">
              Встроенные пресеты в Resources (Assets/Data/db/presets.json)
            </div>
            <div className="flex items-center gap-1.5">
              {presetsJson.map((p, idx) => (
                <button
                  key={p.id}
                  type="button"
                  onClick={() => setSelectedPresetIdx(idx)}
                  className={`px-2.5 py-1 ui-control text-xs border transition-colors ${
                    selectedPresetIdx === idx
                      ? 'bg-[var(--accent)] border-[var(--accent)] text-white font-semibold'
                      : 'bg-[var(--surface-sub)] border-[var(--border-line)] text-[var(--ink-2)] hover:text-[var(--ink-1)]'
                  }`}
                >
                  {p.name.split('(')[0].trim()}
                </button>
              ))}
            </div>
          </div>
          <div className="p-3 rounded bg-[var(--surface-sub)] border border-[var(--border-soft)] space-y-1.5">
            <div className="flex items-center gap-2 text-xs font-semibold text-emerald-300">
              <CheckCircle2 className="w-3.5 h-3.5" />
              <span>{activePreset.name}</span>
            </div>
            <p className="text-xs text-[var(--ink-2)]">{activePreset.description}</p>
            <div className="text-[11px] font-mono text-[var(--ink-3)]">
              Модов в пресете: {activePreset.modIds.length} · Риск: {activePreset.crashRisk}
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
