import { apiClient } from './apiClient';
import type { UploadResponse } from '../types/document';

/**
 * Document API module for handling document upload operations
 */
export const documentApi = {
  /**
   * Upload a document (PDF or DOCX) to the backend
   * @param file - File to upload
   * @param onProgress - Optional callback for upload progress (0-100)
   * @returns Upload response with documentId, fileName, and chunkCount
   * @throws Error if file type is invalid or upload fails
   */
  async uploadDocument(
    file: File,
    onProgress?: (progress: number) => void
  ): Promise<UploadResponse> {
    // Validate file type
    const validExtensions = ['.pdf', '.docx'];
    const fileName = file.name.toLowerCase();
    const extension = fileName.substring(fileName.lastIndexOf('.'));

    if (!validExtensions.includes(extension)) {
      throw new Error('Invalid file type. Only PDF and DOCX files are supported.');
    }

    // Upload file using XMLHttpRequest for progress tracking
    return apiClient.upload('/api/documents/upload', file, onProgress);
  },
};
