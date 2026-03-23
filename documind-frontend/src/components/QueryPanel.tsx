import { DocumentSelector } from './DocumentSelector';
import { ChatHistory } from './ChatHistory';
import { QueryInput } from './QueryInput';

export function QueryPanel() {
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

      {/* Chat History */}
      <ChatHistory />

      {/* Query Input */}
      <QueryInput />
    </div>
  );
}
