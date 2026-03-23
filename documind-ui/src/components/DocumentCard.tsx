import type { Document } from '../types/document';

interface DocumentCardProps {
  document: Document;
  onSelect?: (id: string) => void;
  selected?: boolean;
}

export function DocumentCard({ 
  document, 
  onSelect, 
  selected = false 
}: DocumentCardProps) {
  const isPdf = document.fileName.toLowerCase().endsWith('.pdf');
  
  const formatTimestamp = (date: Date) => {
    return new Intl.DateTimeFormat('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    }).format(date);
  };
  
  return (
    <div 
      className={`
        rounded-lg shadow p-4 cursor-pointer transition-all duration-200
        ${selected 
          ? 'bg-blue-50 border-2 border-blue-500 shadow-md' 
          : 'bg-white border border-gray-200 hover:shadow-lg hover:border-blue-300'
        }
      `}
      onClick={() => onSelect?.(document.id)}
      role="button"
      tabIndex={0}
      aria-pressed={selected}
    >
      <div className="flex items-start gap-3">
        <div className="flex-shrink-0">
          {isPdf ? (
            <svg 
              className="h-10 w-10 text-red-500" 
              fill="currentColor" 
              viewBox="0 0 20 20"
              aria-hidden="true"
            >
              <path 
                fillRule="evenodd" 
                d="M4 4a2 2 0 012-2h4.586A2 2 0 0112 2.586L15.414 6A2 2 0 0116 7.414V16a2 2 0 01-2 2H6a2 2 0 01-2-2V4zm2 6a1 1 0 011-1h6a1 1 0 110 2H7a1 1 0 01-1-1zm1 3a1 1 0 100 2h6a1 1 0 100-2H7z" 
                clipRule="evenodd" 
              />
            </svg>
          ) : (
            <svg 
              className="h-10 w-10 text-blue-500" 
              fill="currentColor" 
              viewBox="0 0 20 20"
              aria-hidden="true"
            >
              <path 
                fillRule="evenodd" 
                d="M4 4a2 2 0 012-2h4.586A2 2 0 0112 2.586L15.414 6A2 2 0 0116 7.414V16a2 2 0 01-2 2H6a2 2 0 01-2-2V4z" 
                clipRule="evenodd" 
              />
            </svg>
          )}
        </div>
        <div className="flex-1 min-w-0">
          <h3 className="text-sm font-medium text-gray-900 truncate">
            {document.fileName}
          </h3>
          <p className="text-xs text-gray-500 mt-1">
            {document.chunkCount} chunks
          </p>
          <time className="text-xs text-gray-400 mt-1 block">
            {formatTimestamp(document.uploadedAt)}
          </time>
        </div>
      </div>
    </div>
  );
}
