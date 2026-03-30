import { useEffect, useRef } from 'react';
import { DocumentSelector } from './DocumentSelector';
import { ChatHistory } from './ChatHistory';
import { QueryInput } from './QueryInput';
import { PipelineStatusPanel } from './PipelineStatusPanel';
import { QueryProvider, useQueryContext } from '../contexts/QueryContext';

function QueryPanelInner() {
  const { pipelineStages, retryCount, isQueryActive } = useQueryContext();
  const panelRef = useRef<HTMLDivElement>(null);

  // Auto-scroll pipeline panel into view when query becomes active
  useEffect(() => {
    if (isQueryActive && panelRef.current) {
      panelRef.current.scrollIntoView({ behavior: 'smooth' });
    }
  }, [isQueryActive]);

  return (
    <div className="h-full flex flex-col">
      {/* Header with Document Selector */}
      <div className="p-4 border-b border-neutral-800">
        <div className="flex items-center justify-between mb-3">
          <h2 className="text-sm font-medium text-neutral-200 uppercase tracking-wider">
            Query
          </h2>
        </div>
        <DocumentSelector />
      </div>

      {/* Pipeline Status Panel */}
      <div ref={panelRef}>
        <PipelineStatusPanel
          stages={pipelineStages}
          retryCount={retryCount}
          isVisible={isQueryActive}
        />
      </div>

      {/* Chat History */}
      <ChatHistory />

      {/* Query Input */}
      <QueryInput />
    </div>
  );
}

export function QueryPanel() {
  return (
    <QueryProvider>
      <QueryPanelInner />
    </QueryProvider>
  );
}
