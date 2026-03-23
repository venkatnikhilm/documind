import type { UploadResponse, UploadProgress } from '../types';

interface UploadCallbacks {
  onProgress: (progress: UploadProgress) => void;
  onSuccess: (response: UploadResponse) => void;
  onError: (error: string) => void;
}

export function uploadDocument(file: File, callbacks: UploadCallbacks): () => void {
  const xhr = new XMLHttpRequest();
  const formData = new FormData();
  formData.append('file', file);

  xhr.upload.addEventListener('progress', (event) => {
    if (event.lengthComputable) {
      callbacks.onProgress({
        loaded: event.loaded,
        total: event.total,
        percentage: Math.round((event.loaded / event.total) * 100),
      });
    }
  });

  xhr.addEventListener('load', () => {
    if (xhr.status >= 200 && xhr.status < 300) {
      try {
        const response: UploadResponse = JSON.parse(xhr.responseText);
        callbacks.onSuccess(response);
      } catch {
        callbacks.onError('Failed to parse server response');
      }
    } else {
      callbacks.onError(`Upload failed: ${xhr.statusText || 'Unknown error'}`);
    }
  });

  xhr.addEventListener('error', () => {
    callbacks.onError('Network error occurred during upload');
  });

  xhr.addEventListener('abort', () => {
    callbacks.onError('Upload was cancelled');
  });

  xhr.open('POST', '/api/documents/upload');
  xhr.send(formData);

  // Return abort function
  return () => xhr.abort();
}
