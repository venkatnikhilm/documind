export interface Document {
  id: string;
  fileName: string;
  chunkCount: number;
  uploadedAt: Date;
}

export interface Citation {
  chunkId: string;
  documentId: string;
  score: number;
}

export interface Message {
  id: string;
  role: 'user' | 'assistant';
  content: string;
  citations: Citation[];
  timestamp: Date;
}

export interface QueryRequest {
  question: string;
  documentId?: string;
}

export interface UploadResponse {
  documentId: string;
  chunkCount: number;
  fileName: string;
}

export interface UploadProgress {
  loaded: number;
  total: number;
  percentage: number;
}
