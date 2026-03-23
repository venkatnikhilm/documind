import { createContext, useContext, useState, type ReactNode } from 'react';
import type { Document } from '../types';

interface DocumentContextType {
  documents: Document[];
  selectedDocId: string | null;
  addDocument: (doc: Document) => void;
  selectDocument: (id: string | null) => void;
}

const DocumentContext = createContext<DocumentContextType | undefined>(undefined);

export function DocumentProvider({ children }: { children: ReactNode }) {
  const [documents, setDocuments] = useState<Document[]>([]);
  const [selectedDocId, setSelectedDocId] = useState<string | null>(null);

  const addDocument = (doc: Document) => {
    setDocuments((prev) => [...prev, doc]);
  };

  const selectDocument = (id: string | null) => {
    setSelectedDocId(id);
  };

  return (
    <DocumentContext.Provider
      value={{
        documents,
        selectedDocId,
        addDocument,
        selectDocument,
      }}
    >
      {children}
    </DocumentContext.Provider>
  );
}

export function useDocuments() {
  const context = useContext(DocumentContext);
  if (context === undefined) {
    throw new Error('useDocuments must be used within a DocumentProvider');
  }
  return context;
}
