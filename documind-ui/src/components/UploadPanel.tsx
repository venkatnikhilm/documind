import { useState } from 'react';
import { useDocuments } from '../contexts/DocumentContext';
import { useUpload } from '../hooks/useUpload';
import { DropZone } from './DropZone';
import { ProgressBar } from './ProgressBar';
import { DocumentCard } from './DocumentCard';
import { ErrorToast } from './ErrorToast';

interface UploadPanelProps {
  className?: string;
}

export function UploadPanel({ className }: UploadPanelProps) {
  const { documents, addDocument } = useDocuments();
  const { uploadDocument, isUploading, progress, error: uploadError } = useUpload();
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [retryFile, setRetryFile] = useState<File | null>(null);

  const handleDrop = async (files: FileList) => {
    if (files.length === 0) return;

    const file = files[0];
    
    // Validate file type
    const validExtensions = ['.pdf', '.docx'];
    const extension = file.name.substring(file.name.lastIndexOf('.')).toLowerCase();
    
    if (!validExtensions.includes(extension)) {
      setErrorMessage('Invalid file type. Only PDF and DOCX files are supported.');
      return;
    }

    // Clear previous errors
    setErrorMessage(null);
    setRetryFile(null);

    try {
      const document = await uploadDocument(file);
      addDocument(document);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Upload failed';
      setErrorMessage(message);
      setRetryFile(file);
    }
  };

  const handleRetry = () => {
    if (retryFile) {
      const fileList = new DataTransfer();
      fileList.items.add(retryFile);
      handleDrop(fileList.files);
    }
  };

  const handleCloseError = () => {
    setErrorMessage(null);
    setRetryFile(null);
  };

  return (
    <div className={className}>
      <div className="flex flex-col gap-4">
        <h2 className="text-xl font-semibold text-gray-900">Upload Documents</h2>
        
        <DropZone onDrop={handleDrop} />
        
        {isUploading && (
          <div className="space-y-2">
            <ProgressBar progress={progress} />
            <p className="text-sm text-gray-600 text-center">
              Uploading... {Math.round(progress)}%
            </p>
          </div>
        )}
        
        {errorMessage && (
          <div className="bg-red-50 border border-red-200 rounded-lg p-4">
            <div className="flex items-start gap-3">
              <svg 
                className="h-5 w-5 text-red-500 flex-shrink-0 mt-0.5" 
                fill="none" 
                stroke="currentColor" 
                viewBox="0 0 24 24"
                aria-hidden="true"
              >
                <path 
                  strokeLinecap="round" 
                  strokeLinejoin="round" 
                  strokeWidth={2} 
                  d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" 
                />
              </svg>
              <div className="flex-1">
                <p className="text-sm text-red-800">{errorMessage}</p>
                {retryFile && (
                  <button
                    onClick={handleRetry}
                    className="mt-2 text-sm font-medium text-red-600 hover:text-red-700 underline"
                  >
                    Retry upload
                  </button>
                )}
              </div>
              <button
                onClick={handleCloseError}
                className="flex-shrink-0 text-red-500 hover:text-red-700"
                aria-label="Close error message"
              >
                <svg 
                  className="h-5 w-5" 
                  fill="none" 
                  stroke="currentColor" 
                  viewBox="0 0 24 24"
                  aria-hidden="true"
                >
                  <path 
                    strokeLinecap="round" 
                    strokeLinejoin="round" 
                    strokeWidth={2} 
                    d="M6 18L18 6M6 6l12 12" 
                  />
                </svg>
              </button>
            </div>
          </div>
        )}
        
        <div className="mt-6">
          <h3 className="text-lg font-medium text-gray-900 mb-3">
            Uploaded Documents
          </h3>
          
          {documents.length === 0 ? (
            <div className="text-center py-8 text-gray-500">
              <svg 
                className="mx-auto h-12 w-12 text-gray-400 mb-3" 
                fill="none" 
                stroke="currentColor" 
                viewBox="0 0 24 24"
                aria-hidden="true"
              >
                <path 
                  strokeLinecap="round" 
                  strokeLinejoin="round" 
                  strokeWidth={2} 
                  d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" 
                />
              </svg>
              <p className="text-sm">No documents uploaded yet</p>
              <p className="text-xs mt-1">Upload a document to get started</p>
            </div>
          ) : (
            <div className="flex flex-col gap-4 max-h-96 overflow-y-auto">
              {documents.map((document) => (
                <DocumentCard 
                  key={document.id} 
                  document={document} 
                />
              ))}
            </div>
          )}
        </div>
      </div>
      
      {uploadError && (
        <ErrorToast 
          message={uploadError} 
          type="error" 
          onClose={handleCloseError} 
        />
      )}
    </div>
  );
}
