import { useState } from 'react';
import { documentApi } from '../api/documentApi';
import type { Document } from '../types/document';

/**
 * Result interface for the useUpload hook
 */
export interface UseUploadResult {
  uploadDocument: (file: File) => Promise<Document>;
  isUploading: boolean;
  progress: number;
  error: string | null;
}

/**
 * Custom hook for managing document upload operations
 * 
 * Manages upload state including progress tracking and error handling.
 * Provides a clean interface for components to upload documents without
 * managing the complexity of progress callbacks and error states.
 * 
 * @returns Upload operations and state
 * 
 * @example
 * ```tsx
 * const { uploadDocument, isUploading, progress, error } = useUpload();
 * 
 * const handleUpload = async (file: File) => {
 *   try {
 *     const document = await uploadDocument(file);
 *     console.log('Uploaded:', document);
 *   } catch (err) {
 *     console.error('Upload failed:', error);
 *   }
 * };
 * ```
 */
export function useUpload(): UseUploadResult {
  const [isUploading, setIsUploading] = useState(false);
  const [progress, setProgress] = useState(0);
  const [error, setError] = useState<string | null>(null);

  const uploadDocument = async (file: File): Promise<Document> => {
    setIsUploading(true);
    setProgress(0);
    setError(null);

    try {
      const result = await documentApi.uploadDocument(file, (p) => {
        setProgress(p);
      });

      return {
        id: result.documentId,
        fileName: result.fileName,
        chunkCount: result.chunkCount,
        uploadedAt: new Date(),
      };
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Upload failed';
      setError(message);
      throw err;
    } finally {
      setIsUploading(false);
    }
  };

  return { uploadDocument, isUploading, progress, error };
}
