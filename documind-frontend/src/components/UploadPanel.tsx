import { DropZone } from './DropZone';
import { DocumentCard } from './DocumentCard';
import { useDocuments } from '../contexts/DocumentContext';

export function UploadPanel() {
  const { documents } = useDocuments();

  return (
    <div className="h-full flex flex-col p-4 border-r border-neutral-800">
      {/* Header */}
      <div className="mb-4">
        <h2 className="text-sm font-medium text-neutral-200 uppercase tracking-wider">
          Documents
        </h2>
        <p className="text-xs text-neutral-500 mt-1">
          Upload files to query
        </p>
      </div>

      {/* Drop Zone */}
      <DropZone />

      {/* Document List */}
      {documents.length > 0 && (
        <div className="mt-4 flex-1 overflow-auto">
          <div className="flex items-center justify-between mb-2">
            <span className="text-xs text-neutral-500 uppercase tracking-wider">
              Uploaded
            </span>
            <span className="text-xs text-neutral-600">
              {documents.length} file{documents.length !== 1 ? 's' : ''}
            </span>
          </div>
          <div className="space-y-2">
            {documents.map((doc) => (
              <DocumentCard key={doc.id} document={doc} />
            ))}
          </div>
        </div>
      )}

      {/* Empty State */}
      {documents.length === 0 && (
        <div className="flex-1 flex items-center justify-center">
          <p className="text-xs text-neutral-600 text-center">
            No documents uploaded yet
          </p>
        </div>
      )}
    </div>
  );
}
