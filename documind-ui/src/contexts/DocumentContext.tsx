import { createContext, useContext, useState } from 'react';
import type { PropsWithChildren } from 'react';
import type { Document } from '../types/document';

interface DocumentContextValue {
  documents: Document[];
  addDocument: (doc: Document) => void;
  removeDocument: (id: string) => void;
  getDocument: (id: string) => Document | undefined;
}

const DocumentContext = createContext<DocumentContextValue | null>(null);

export function DocumentProvider({ children }: PropsWithChildren) {
  const [documents, setDocuments] = useState<Document[]>([]);

  const addDocument = (doc: Document) => {
    setDocuments((prev) => [...prev, doc]);
  };

  const removeDocument = (id: string) => {
    setDocuments((prev) => prev.filter((d) => d.id !== id));
  };

  const getDocument = (id: string) => {
    return documents.find((d) => d.id === id);
  };

  return (
    <DocumentContext.Provider
      value={{
        documents,
        addDocument,
        removeDocument,
        getDocument,
      }}
    >
      {children}
    </DocumentContext.Provider>
  );
}

export function useDocuments() {
  const context = useContext(DocumentContext);
  if (!context) {
    throw new Error('useDocuments must be used within DocumentProvider');
  }
  return context;
}
