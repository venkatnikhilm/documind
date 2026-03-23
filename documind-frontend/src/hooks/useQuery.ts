import { useCallback, useRef } from 'react';
import { sendQuery } from '../api/queryApi';
import { useMessages } from '../contexts/MessageContext';
import { useDocuments } from '../contexts/DocumentContext';
import type { Citation } from '../types';

interface UseQueryReturn {
  submitQuery: (question: string) => void;
  cancelQuery: () => void;
}

export function useQuery(): UseQueryReturn {
  const abortControllerRef = useRef<AbortController | null>(null);
  const {
    addUserMessage,
    startAssistantMessage,
    appendToMessage,
    addCitationsToMessage,
    finalizeMessage,
    setIsStreaming,
    addErrorMessage,
  } = useMessages();
  const { selectedDocId } = useDocuments();

  const submitQuery = useCallback(
    (question: string) => {
      if (!question.trim()) return;

      // Add user message
      addUserMessage(question);

      // Start streaming
      setIsStreaming(true);
      const messageId = startAssistantMessage();

      // Create abort controller
      abortControllerRef.current = new AbortController();

      const citations: Citation[] = [];

      sendQuery(
        {
          question,
          documentId: selectedDocId || undefined,
        },
        {
          onToken: (content) => {
            appendToMessage(messageId, content);
          },
          onCitation: (citation) => {
            citations.push(citation);
            addCitationsToMessage(messageId, [citation]);
          },
          onComplete: () => {
            finalizeMessage(messageId);
          },
          onError: (error) => {
            // If we already have some content, keep it and add error indicator
            addErrorMessage(error);
          },
        },
        abortControllerRef.current
      );
    },
    [
      addUserMessage,
      startAssistantMessage,
      appendToMessage,
      addCitationsToMessage,
      finalizeMessage,
      setIsStreaming,
      addErrorMessage,
      selectedDocId,
    ]
  );

  const cancelQuery = useCallback(() => {
    if (abortControllerRef.current) {
      abortControllerRef.current.abort();
      abortControllerRef.current = null;
    }
  }, []);

  return {
    submitQuery,
    cancelQuery,
  };
}
