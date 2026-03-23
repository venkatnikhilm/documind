export interface Document {
  id: string;
  fileName: string;
  chunkCount: number;
  uploadedAt: Date;
}

export interface UploadResponse {
  documentId: string;
  chunkCount: number;
  fileName: string;
}
