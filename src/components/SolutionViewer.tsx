import { useMemo, useState } from 'react';
import { Check, Copy, FileCode2, FolderOpen, Search } from 'lucide-react';

export interface SolutionFileEntry {
  path: string;
  folder: string;
  name: string;
  language: string;
  lines: number;
  content: string;
}

interface SolutionViewerProps {
  files: SolutionFileEntry[];
}

export function SolutionViewer({ files }: SolutionViewerProps) {
  const [search, setSearch] = useState('');
  const [folderFilter, setFolderFilter] = useState<string>('Все');
  const [selectedPath, setSelectedPath] = useState<string>(
    files.find(f => f.path.endsWith('MainWindow.axaml'))?.path ?? files[0]?.path ?? ''
  );
  const [copied, setCopied] = useState(false);

  const folders = useMemo(() => {
    const set = new Set<string>();
    for (const f of files) set.add(f.folder);
    return ['Все', ...Array.from(set)];
  }, [files]);

  const filteredFiles = useMemo(() => {
    const q = search.trim().toLowerCase();
    return files.filter(f => {
      if (folderFilter !== 'Все' && f.folder !== folderFilter) return false;
      if (!q) return true;
      return f.path.toLowerCase().includes(q) || f.content.toLowerCase().includes(q);
    });
  }, [files, folderFilter, search]);

  const activeFile = useMemo(
    () => files.find(f => f.path === selectedPath) ?? filteredFiles[0] ?? files[0],
    [files, filteredFiles, selectedPath]
  );

  const handleCopy = async () => {
    if (!activeFile || typeof navigator === 'undefined' || !navigator.clipboard) return;
    try {
      await navigator.clipboard.writeText(activeFile.content);
      setCopied(true);
      setTimeout(() => setCopied(false), 1800);
    } catch {
      // Ignore clipboard failures
    }
  };

  return (
    <div className="grid grid-cols-1 lg:grid-cols-12 gap-3">
      {/* Left Sidebar: File Tree */}
      <aside className="lg:col-span-4 xl:col-span-3 ui-block overflow-hidden flex flex-col max-h-[680px]">
        <div className="p-3 border-b border-[var(--border-soft)] bg-[var(--surface-sub)] space-y-2">
          <div className="relative">
            <Search className="w-3.5 h-3.5 text-[var(--ink-3)] absolute left-2.5 top-1/2 -translate-y-1/2" />
            <input
              type="text"
              value={search}
              onChange={e => setSearch(e.target.value)}
              placeholder="Поиск по файлам C# / AXAML..."
              aria-label="Поиск по файлам решения"
              className="w-full pl-8 pr-2.5 py-1.5 ui-control bg-[var(--surface-solid)] border border-[var(--border-line)] text-xs text-[var(--ink-1)] placeholder:text-[var(--ink-3)] focus:border-[var(--accent-text)]"
            />
          </div>
          <div className="flex flex-wrap gap-1">
            {folders.map(folder => (
              <button
                key={folder}
                type="button"
                onClick={() => setFolderFilter(folder)}
                className={`px-2 py-0.5 ui-control text-[11px] border transition-colors ${
                  folderFilter === folder
                    ? 'bg-[var(--accent)] border-[var(--accent)] text-white font-semibold'
                    : 'bg-[var(--surface-control)] border-[var(--border-line)] text-[var(--ink-2)] hover:text-[var(--ink-1)]'
                }`}
              >
                {folder}
              </button>
            ))}
          </div>
        </div>

        <div className="overflow-y-auto divide-y divide-[var(--border-soft)]">
          {filteredFiles.length === 0 ? (
            <div className="p-4 text-xs text-[var(--ink-3)] text-center">Файлы не найдены</div>
          ) : (
            filteredFiles.map(file => {
              const active = activeFile?.path === file.path;
              return (
                <button
                  key={file.path}
                  type="button"
                  onClick={() => setSelectedPath(file.path)}
                  className={`w-full px-3 py-2 text-left flex items-center justify-between gap-2 transition-colors ${
                    active
                      ? 'bg-[var(--accent)]/15 text-[var(--ink-1)] border-l-2 border-l-[var(--accent-text)]'
                      : 'hover:bg-[var(--surface-hover)] text-[var(--ink-2)]'
                  }`}
                >
                  <div className="min-w-0 flex items-center gap-2">
                    <FileCode2 className="w-3.5 h-3.5 text-[var(--accent-text)] shrink-0" />
                    <div className="min-w-0">
                      <div className="text-xs font-mono font-semibold truncate text-[var(--ink-1)]">
                        {file.name}
                      </div>
                      <div className="text-[10px] font-mono text-[var(--ink-3)] truncate">
                        {file.folder}
                      </div>
                    </div>
                  </div>
                  <span className="text-[10px] font-mono text-[var(--ink-3)] shrink-0 tabular-nums">
                    {file.lines}L
                  </span>
                </button>
              );
            })
          )}
        </div>
      </aside>

      {/* Right Code Viewer */}
      <section className="lg:col-span-8 xl:col-span-9 ui-block overflow-hidden flex flex-col max-h-[680px]">
        {activeFile ? (
          <>
            <div className="px-4 py-2.5 bg-[var(--surface-sub)] border-b border-[var(--border-soft)] flex flex-wrap items-center justify-between gap-2">
              <div className="flex items-center gap-2 min-w-0">
                <FolderOpen className="w-4 h-4 text-[var(--accent-text)] shrink-0" />
                <span className="text-xs font-mono font-semibold text-[var(--ink-1)] truncate">
                  {activeFile.path}
                </span>
                <span className="text-[11px] font-mono text-[var(--ink-3)] shrink-0">
                  {activeFile.language} · {activeFile.lines} строк
                </span>
              </div>
              <button
                type="button"
                onClick={handleCopy}
                className="inline-flex items-center gap-1.5 px-2.5 py-1 ui-control bg-[var(--surface-control)] hover:bg-[var(--surface-hover)] border border-[var(--border-line)] text-xs text-[var(--ink-1)] transition-colors"
              >
                {copied ? <Check className="w-3.5 h-3.5 text-emerald-400" /> : <Copy className="w-3.5 h-3.5" />}
                <span>{copied ? 'Скопировано' : 'Копировать код'}</span>
              </button>
            </div>
            <div className="overflow-auto flex-1 bg-[#0B0A0F]/80 p-3">
              <pre className="text-[11.5px] leading-5 font-mono text-[#E4E4EA] whitespace-pre overflow-x-auto">
                <code>{activeFile.content}</code>
              </pre>
            </div>
          </>
        ) : (
          <div className="p-8 text-center text-xs text-[var(--ink-3)]">Выберите файл из списка</div>
        )}
      </section>
    </div>
  );
}
