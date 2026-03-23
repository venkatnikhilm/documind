import { useState, useCallback, useRef } from 'react';
import { uploadDocument } from '../api/documentApi';
import { useDocuments } from '../contexts/DocumentContext';
import type { UploadProgress } from '../types';

interface UseUploadReturn {
  isUploading: boolean;
  progress: UploadProgress | null;
  error: string | null;
  upload: (file: File) => void;
  cancelUpload: () => void;
  clearError: () => void;
}

const ALLOWED_TYPES = [
  'application/pdf',
  'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
];

const ALLOWED_EXTENSIONS = ['.pdf', '.docx'];

export function useUpload(): UseUploadReturn {
  const [isUploading, setIsUploading] = useState(false);
  const [progress, setProgress] = useState<UploadProgress | null>(null);
  const [error, setError] = useState<string | null>(null);
  const abortRef = useRef<(() => void) | null>(null);
  const { addDocument } = useDocuments();

  const validateFile = (file: File): boolean => {
    const extension = file.name.toLowerCase().slice(file.name.lastIndexOf('.'));
    
    if (!ALLOWED_TYPES.includes(file.type) && !ALLOWED_EXTENSIONS.includes(extension)) {
      setError('Invalid file type. Please upload a PDF or DOCX file.');
      return false;
    }
    return true;
  };

  const upload = useCallback((file: File) => {
    if (!validateFile(file)) return;

    setIsUploading(true);
    setProgress({ loaded: 0, total: file.size, percentage: 0 });
    setError(null);

    const abort = uploadDocument(file, {
      onProgress: (prog) => {
        setProgress(prog);
      },
      onSuccess: (response) => {
        setIsUploading(false);
        setProgress(null);
        addDocument({
          id: response.documentId,
          fileName: response.fileName,
          chunkCount: response.chunkCount,
          uploadedAt: new Date(),
        });
      },
      onError: (err) => {
        setIsUploading(false);
        setProgress(null);
        setError(err);
      },
    });

    abortRef.current = abort;
  }, [addDocument]);

  const cancelUpload = useCallback(() => {
    if (abortRef.current) {
      abortRef.current();
      abortRef.current = null;
    }
  }, []);

  const clearError = useCallback(() => {
    setError(null);
  }, []);

  return {
    isUploading,
    progress,
    error,
    upload,
    cancelUpload,
    clearError,
  };
}
