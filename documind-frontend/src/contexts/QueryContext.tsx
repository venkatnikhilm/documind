import { createContext, useContext, type ReactNode } from 'react';
import { useQuery } from '../hooks/useQuery';
import type { PipelineStage } from '../types';

interface QueryContextValue {
  submitQuery: (question: string) => void;
  cancelQuery: () => void;
  pipelineStages: PipelineStage[];
  retryCount: number;
  isQueryActive: boolean;
}

const QueryContext = createContext<QueryContextValue | null>(null);

export function QueryProvider({ children }: { children: ReactNode }) {
  const query = useQuery();
  return <QueryContext.Provider value={query}>{children}</QueryContext.Provider>;
}

export function useQueryContext(): QueryContextValue {
  const ctx = useContext(QueryContext);
  if (!ctx) {
    throw new Error('useQueryContext must be used within a QueryProvider');
  }
  return ctx;
}
