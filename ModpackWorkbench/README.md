# Верстак сборок (C# + .NET 8 + Avalonia UI 11)

Нативный кроссплатформенный десктопный порт приложения «Верстак сборок» на **C# (.NET 8)** и **Avalonia UI 11 (MVVM)**.

## Архитектура проекта

- `ModpackWorkbench.sln` — решение Visual Studio / Rider / .NET CLI
- `ModpackWorkbench/` — основное настольное приложение Avalonia UI 11 (`CommunityToolkit.Mvvm`)
  - `Models/` — модели данных модов, слотов (`ModGroupInfo`), правил конфликтов (`IncompatibilityRule`), пресетов, диагностики, конфигурации темы (`BuilderThemeConfig`) и состояния (`PersistedAppState`)
  - `Services/`
    - `ModDatabase.cs` — загрузка встроенных JSON-баз (`Assets/Data/db/*.json`)
    - `ModAnalyzer.cs` — анализ конфликтов, зависимостей, дубликатов, мостов EMI/JEI/TMRV, тиров стабильности (`Green`/`Lime`/`Yellow`/`Orange`/`Red`) и поиск на русском языке с учётом синонимов и стемминга
    - `ModBuilder.cs` — умный конструктор сборки (`BuildSmartAssembly`), авто-исправление (`SmartFixAssembly`), переключение модов с каскадом зависимостей (`ToggleModSmart`)
    - `ModTreeService.cs` — иерархия «Мод → Аддоны», экспорт дерева и импорт списков модов
    - `JarMatcherService.cs` — сопоставление имён `.jar` файлов и нативное сканирование папки `mods/` (с проверкой `.connector` и алиаса `embeddium -> sodium`)
    - `PersistenceService.cs` — сохранение состояния в `%AppData%/ModpackWorkbench/workbench-state.json`
    - `ThemeService.cs` — управление светлой/тёмной темой, пресетами оформления и динамическими ресурсами
  - `Controls/FractalBackdropControl.cs` — рендеринг углового свечения и математических фракталов (Жюлиа, Мандельброт, Лоренц, Папоротник, Дракон, Кох, Серпинский, Дерево)
  - `ViewModels/` & `Views/` — экраны:
    - **Каталог** (`CatalogView`)
    - **Клипание / Конструктор** (`AssemblyBuilderView`)
    - **Сборки и Золотой стандарт** (`PresetsView`)
    - **Чекер и сканер `mods/`** (`CheckerView`)
    - **Карточка мода** (`ModDetailWindow`)
    - **Настройки темы** (`ThemeConfiguratorView`)
- `ModpackWorkbench.Tests/` — модульные и headless-UI тесты на xUnit + `Avalonia.Headless.XUnit`

## Запуск и тесты

```bash
dotnet restore ModpackWorkbench.sln
dotnet build ModpackWorkbench.sln
dotnet test ModpackWorkbench.sln
dotnet run --project ModpackWorkbench/ModpackWorkbench.csproj
```
