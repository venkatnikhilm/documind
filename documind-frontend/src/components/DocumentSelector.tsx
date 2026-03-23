import { useDocuments } from '../contexts/DocumentContext';

export function DocumentSelector() {
  const { documents, selectedDocId, selectDocument } = useDocuments();
  const hasDocuments = documents.length > 0;

  return (
    <div className="relative">
      <select
        value={selectedDocId || ''}
        onChange={(e) => selectDocument(e.target.value || null)}
        disabled={!hasDocuments}
        className={`
          w-full appearance-none bg-surface border border-neutral-700 rounded-lg
          px-4 py-2.5 pr-10 text-sm font-mono
          focus:outline-none focus:border-accent/50 focus:ring-1 focus:ring-accent/20
          transition-colors
          ${
            hasDocuments
              ? 'text-neutral-200 cursor-pointer hover:border-neutral-600'
              : 'text-neutral-600 cursor-not-allowed'
          }
        `}
      >
        {hasDocuments ? (
          <>
            <option value="">All Documents</option>
            {documents.map((doc) => (
              <option key={doc.id} value={doc.id}>
                {doc.fileName}
              </option>
            ))}
          </>
        ) : (
          <option value="">Upload a document to start</option>
        )}
      </select>
      
      {/* Custom dropdown arrow */}
      <div className="absolute inset-y-0 right-0 flex items-center pr-3 pointer-events-none">
        <svg
          className={`w-4 h-4 ${hasDocuments ? 'text-neutral-400' : 'text-neutral-600'}`}
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
        </svg>
      </div>
    </div>
  );
}
