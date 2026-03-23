import { useState, useRef } from 'react';
import { queryApi } from '../api/queryApi';
import type { Citation } from '../types/message';

/**
 * Result interface for the useQuery hook
 */
export interface UseQueryResult {
  sendQuery: (
    question: string,
    documentId: string | null,
    onToken: (token: string) => void,
    onCitation: (citation: Citation) => void,
    onComplete: () => void,
    onError: (error: string) => void
  ) => Promise<void>;
  isStreaming: boolean;
  abort: () => void;
}

/**
 * Custom hook for managing document query operations with streaming
 * 
 * Manages query state including streaming status and request cancellation.
 * Provides a clean interface for components to send queries and receive
 * streaming responses without managing AbortController complexity.
 * 
 * @returns Query operations and state
 * 
 * @example
 * ```tsx
 * const { sendQuery, isStreaming, abort } = useQuery();
 * 
 * const handleQuery = async () => {
 *   await sendQuery(
 *     'What is this document about?',
 *     documentId,
 *     (token) => console.log('Token:', token),
 *     (citation) => console.log('Citation:', citation),
 *     () => console.log('Complete'),
 *     (error) => console.error('Error:', error)
 *   );
 * };
 * 
 * // Cancel the query if needed
 * const handleCancel = () => abort();
 * ```
 */
export function useQuery(): UseQueryResult {
  const [isStreaming, setIsStreaming] = useState(false);
  const abortControllerRef = useRef<AbortController | null>(null);

  const sendQuery = async (
    question: string,
    documentId: string | null,
    onToken: (token: string) => void,
    onCitation: (citation: Citation) => void,
    onComplete: () => void,
    onError: (error: string) => void
  ) => {
    setIsStreaming(true);
    abortControllerRef.current = new AbortController();

    try {
      await queryApi.sendQuery(
        { question, documentId },
        {
          onToken,
          onCitation,
          onComplete: () => {
            setIsStreaming(false);
            onComplete();
          },
          onError: (err) => {
            setIsStreaming(false);
            onError(err);
          },
        },
        abortControllerRef.current.signal
      );
    } catch (err) {
      setIsStreaming(false);
      onError(err instanceof Error ? err.message : 'Query failed');
    }
  };

  const abort = () => {
    abortControllerRef.current?.abort();
    setIsStreaming(false);
  };

  return { sendQuery, isStreaming, abort };
}
