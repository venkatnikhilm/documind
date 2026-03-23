import type { Document } from '../types';
import { useDocuments } from '../contexts/DocumentContext';

interface DocumentCardProps {
  document: Document;
}

function formatTime(date: Date): string {
  return date.toLocaleTimeString('en-US', {
    hour: 'numeric',
    minute: '2-digit',
    hour12: true,
  });
}

function getFileIcon(fileName: string): JSX.Element {
  const isPdf = fileName.toLowerCase().endsWith('.pdf');
  
  if (isPdf) {
    return (
      <svg className="w-8 h-8 text-red-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z" />
        <text x="7" y="17" fontSize="6" fill="currentColor" fontFamily="monospace">PDF</text>
      </svg>
    );
  }
  
  return (
    <svg className="w-8 h-8 text-blue-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z" />
      <text x="5" y="17" fontSize="5" fill="currentColor" fontFamily="monospace">DOC</text>
    </svg>
  );
}

export function DocumentCard({ document }: DocumentCardProps) {
  const { selectedDocId, selectDocument } = useDocuments();
  const isSelected = selectedDocId === document.id;

  return (
    <button
      onClick={() => selectDocument(isSelected ? null : document.id)}
      className={`
        w-full flex items-center gap-3 p-3 rounded-lg transition-all duration-150
        text-left
        ${
          isSelected
            ? 'bg-accent/10 border border-accent/30'
            : 'bg-surface hover:bg-neutral-800 border border-transparent'
        }
      `}
    >
      {getFileIcon(document.fileName)}
      
      <div className="flex-1 min-w-0">
        <p className="text-sm text-neutral-200 truncate font-medium">
          {document.fileName}
        </p>
        <div className="flex items-center gap-2 mt-0.5">
          <span className="text-xs text-neutral-500">
            {document.chunkCount} chunk{document.chunkCount !== 1 ? 's' : ''}
          </span>
          <span className="text-neutral-600">·</span>
          <span className="text-xs text-neutral-500">
            {formatTime(document.uploadedAt)}
          </span>
        </div>
      </div>

      {isSelected && (
        <div className="w-2 h-2 rounded-full bg-accent flex-shrink-0" />
      )}
    </button>
  );
}
