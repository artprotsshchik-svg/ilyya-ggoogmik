import { useState } from 'react';
import { CheckCircle2, Code2, Download, Layers, Terminal } from 'lucide-react';
import solutionData from './data/avaloniaSolution.json';
import archiveFiles from './data/avaloniaArchive.json';
import { SolutionViewer, type SolutionFileEntry } from './components/SolutionViewer';
import { MvvmOverview } from './components/MvvmOverview';
import { createZipBlob, type ZipInputEntry } from './utils/zipBuilder';

type PortalTab = 'code' | 'architecture';

export default function App() {
  const [activeTab, setActiveTab] = useState<PortalTab>('code');
  const files = solutionData.files as SolutionFileEntry[];

  const handleDownloadZip = () => {
    const blob = createZipBlob(archiveFiles as ZipInputEntry[]);
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = 'ModpackWorkbench-Avalonia.zip';
    document.body.appendChild(link);
    link.click();
    link.remove();
    setTimeout(() => URL.revokeObjectURL(url), 10000);
  };

  return (
    <div className="min-h-screen flex flex-col bg-[var(--surface-page)] text-[var(--ink-1)]">
      {/* Top Header */}
      <header className="sticky top-0 z-20 backdrop-blur-md bg-[var(--surface-header)] border-b border-[var(--border-line)]">
        <div className="max-w-[1360px] mx-auto px-4 sm:px-6 py-2.5 sm:py-0 sm:h-14 flex items-center justify-between gap-3">
          <div className="flex items-center gap-2.5 sm:gap-3 min-w-0">
            <div className="w-8 h-8 rounded-md bg-[var(--accent)]/20 border border-[var(--accent-text)]/40 flex items-center justify-center shrink-0">
              <Code2 className="w-4 h-4 text-[var(--accent-text)]" />
            </div>
            <div className="min-w-0">
              <h1 className="text-sm sm:text-base font-bold tracking-tight truncate text-[var(--ink-1)]">
                Верстак сборок · C# + Avalonia UI
              </h1>
              <p className="text-[11px] font-mono text-[var(--ink-3)] truncate">
                {solutionData.solutionName} · {solutionData.framework}
              </p>
            </div>
          </div>

          <button
            type="button"
            onClick={handleDownloadZip}
            className="inline-flex items-center gap-2 px-3 sm:px-3.5 py-2 ui-control bg-[var(--accent)] hover:bg-[var(--accent-hover)] text-white text-xs sm:text-sm font-semibold transition-colors shrink-0 cursor-pointer"
          >
            <Download className="w-4 h-4" />
            <span className="hidden sm:inline">Скачать C# + Avalonia (.zip, {solutionData.zipSizeKb} КБ)</span>
            <span className="sm:hidden">ZIP ({solutionData.zipSizeKb} КБ)</span>
          </button>
        </div>
      </header>

      {/* Sub-navigation & Verified Build Strip */}
      <div className="bg-[var(--surface-sub)] border-b border-[var(--border-soft)]">
        <div className="max-w-[1360px] mx-auto px-4 sm:px-6 py-2.5 flex flex-wrap items-center justify-between gap-3">
          <nav className="flex items-center gap-2" aria-label="Разделы портала">
            <button
              type="button"
              onClick={() => setActiveTab('code')}
              className={`inline-flex items-center gap-1.5 px-3 py-1.5 ui-control text-xs font-semibold border transition-colors ${
                activeTab === 'code'
                  ? 'bg-[var(--accent)] border-[var(--accent)] text-white'
                  : 'bg-[var(--surface-control)] border-[var(--border-line)] text-[var(--ink-2)] hover:text-[var(--ink-1)]'
              }`}
            >
              <Code2 className="w-3.5 h-3.5" />
              <span>Код решения (.NET 8 / Avalonia)</span>
              <span className="font-mono opacity-80">({solutionData.fileCount})</span>
            </button>

            <button
              type="button"
              onClick={() => setActiveTab('architecture')}
              className={`inline-flex items-center gap-1.5 px-3 py-1.5 ui-control text-xs font-semibold border transition-colors ${
                activeTab === 'architecture'
                  ? 'bg-[var(--accent)] border-[var(--accent)] text-white'
                  : 'bg-[var(--surface-control)] border-[var(--border-line)] text-[var(--ink-2)] hover:text-[var(--ink-1)]'
              }`}
            >
              <Layers className="w-3.5 h-3.5" />
              <span>Архитектура MVVM и экраны</span>
            </button>
          </nav>

          <div className="flex flex-wrap items-center gap-3 text-xs font-mono text-[var(--ink-2)]">
            <span className="inline-flex items-center gap-1.5 text-emerald-400">
              <CheckCircle2 className="w-3.5 h-3.5" />
              <span>dotnet build: {solutionData.buildStats.errors} ошибок, {solutionData.buildStats.warnings} предупреждений</span>
            </span>
            <span className="text-[var(--ink-3)]">·</span>
            <span className="text-sky-300">
              xUnit + Headless UI: {solutionData.buildStats.testsPassed}/{solutionData.buildStats.testsTotal} пройдено
            </span>
            <span className="hidden sm:inline text-[var(--ink-3)]">·</span>
            <span className="hidden sm:inline text-[var(--ink-3)]">
              {solutionData.totalLines.toLocaleString('ru-RU')} строк
            </span>
          </div>
        </div>
      </div>

      {/* Main Content */}
      <main className="flex-1 max-w-[1360px] w-full mx-auto px-4 sm:px-6 py-4 space-y-4">
        {activeTab === 'code' ? <SolutionViewer files={files} /> : <MvvmOverview />}

        {/* Quick Run Commands */}
        <section className="ui-block p-4 space-y-2">
          <div className="flex items-center gap-2 text-xs font-semibold text-[var(--ink-1)]">
            <Terminal className="w-4 h-4 text-[var(--accent-text)]" />
            <span>Локальный запуск десктопного приложения (.NET 10 / 11 / 8 SDK · или Run-Windows.bat)</span>
          </div>
          <div className="grid sm:grid-cols-3 gap-2 text-xs font-mono">
            <div className="p-2.5 rounded bg-[#0B0A0F]/70 border border-[var(--border-soft)] text-[var(--ink-2)] overflow-x-auto">
              dotnet restore ModpackWorkbench.sln
            </div>
            <div className="p-2.5 rounded bg-[#0B0A0F]/70 border border-[var(--border-soft)] text-[var(--ink-2)] overflow-x-auto">
              dotnet test ModpackWorkbench.sln
            </div>
            <div className="p-2.5 rounded bg-[#0B0A0F]/70 border border-[var(--border-soft)] text-emerald-300 overflow-x-auto">
              dotnet run --project ModpackWorkbench
            </div>
          </div>
        </section>
      </main>
    </div>
  );
}
