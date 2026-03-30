import { useCallback, useRef, useState } from 'react';
import { sendQuery } from '../api/queryApi';
import { useMessages } from '../contexts/MessageContext';
import { useDocuments } from '../contexts/DocumentContext';
import type { Citation, StatusEvent, PipelineStage, StageState } from '../types';

const initialStages: PipelineStage[] = [
  { node: 'router', label: 'Router', state: 'pending', detail: null },
  { node: 'retriever', label: 'Retriever', state: 'pending', detail: null },
  { node: 'grader', label: 'Grader', state: 'pending', detail: null },
  { node: 'generator', label: 'Generator', state: 'pending', detail: null },
];

interface UseQueryReturn {
  submitQuery: (question: string) => void;
  cancelQuery: () => void;
  pipelineStages: PipelineStage[];
  retryCount: number;
  isQueryActive: boolean;
}

export function useQuery(): UseQueryReturn {
  const abortControllerRef = useRef<AbortController | null>(null);
  const lastGraderLowRelevanceRef = useRef(false);
  const [pipelineStages, setPipelineStages] = useState<PipelineStage[]>(initialStages);
  const [retryCount, setRetryCount] = useState(0);
  const [isQueryActive, setIsQueryActive] = useState(false);
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

  const completionMessages = [
    'Query classified',
    'Retrieved top 5 chunks',
    'Relevance check passed',
    'Answer verified',
  ];

  const submitQuery = useCallback(
    (question: string) => {
      if (!question.trim()) return;

      // Reset pipeline state for new query
      setIsQueryActive(true);
      setPipelineStages(initialStages);
      setRetryCount(0);
      lastGraderLowRelevanceRef.current = false;

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
            setIsStreaming(false);
            // Keep the pipeline panel visible briefly after completion
            setTimeout(() => setIsQueryActive(false), 3000);
          },
          onError: (error) => {
            addErrorMessage(error);
            setPipelineStages((prev) =>
              prev.map((stage) =>
                stage.state === 'active' || stage.state === 'pending'
                  ? { ...stage, state: 'error' as StageState }
                  : stage
              )
            );
            setIsQueryActive(false);
          },
          onStatus: (event: StatusEvent) => {
            // Detect retry: retriever event after grader "Low relevance" event
            if (event.node === 'retriever' && lastGraderLowRelevanceRef.current) {
              lastGraderLowRelevanceRef.current = false;
              setRetryCount((prev) => prev + 1);
              setPipelineStages((prev) =>
                prev.map((stage) => {
                  if (stage.node === 'retriever' || stage.node === 'grader') {
                    return { ...stage, state: 'pending' as StageState, detail: null };
                  }
                  return stage;
                })
              );
              // Then set retriever to active
              setPipelineStages((prev) =>
                prev.map((stage) =>
                  stage.node === 'retriever'
                    ? { ...stage, state: 'active' as StageState, detail: event.detail }
                    : stage
                )
              );
              return;
            }

            // Track grader low relevance for retry detection
            if (event.node === 'grader' && event.message.startsWith('Low relevance')) {
              lastGraderLowRelevanceRef.current = true;
            }

            // Determine if this is a completion message
            const isCompletion = completionMessages.includes(event.message);

            setPipelineStages((prev) =>
              prev.map((stage) => {
                if (stage.node !== event.node) return stage;
                if (isCompletion) {
                  return { ...stage, state: 'completed' as StageState, detail: event.detail };
                }
                return { ...stage, state: 'active' as StageState, detail: event.detail };
              })
            );
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
    pipelineStages,
    retryCount,
    isQueryActive,
  };
}
