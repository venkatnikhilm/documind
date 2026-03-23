import type { QueryRequest } from '../types/query';
import type { Citation } from '../types/message';

/**
 * Callbacks for handling SSE streaming events
 */
export interface StreamCallbacks {
  onToken: (token: string) => void;
  onCitation: (citation: Citation) => void;
  onComplete: () => void;
  onError: (error: string) => void;
}

/**
 * Query API module for handling document query operations with SSE streaming
 */
export const queryApi = {
  /**
   * Send a query to the backend and process the streaming response
   * @param request - Query request with question and optional documentId
   * @param callbacks - Callbacks for handling streaming events
   * @param signal - Optional AbortSignal for request cancellation
   */
  async sendQuery(
    request: QueryRequest,
    callbacks: StreamCallbacks,
    signal?: AbortSignal
  ): Promise<void> {
    try {
      // Make POST request to query endpoint
      const response = await fetch('/api/documents/query', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(request),
        signal,
      });

      // Handle non-OK responses
      if (!response.ok) {
        let errorMessage: string;
        try {
          const error = await response.json();
          errorMessage = error.message || 'Query failed';
        } catch {
          errorMessage = `Request failed with status ${response.status}`;
        }
        callbacks.onError(errorMessage);
        return;
      }

      // Ensure response body exists
      if (!response.body) {
        callbacks.onError('No response body');
        return;
      }

      // Process SSE stream using ReadableStream
      const reader = response.body.getReader();
      const decoder = new TextDecoder();
      let buffer = '';
      let currentEventType = '';

      while (true) {
        const { done, value } = await reader.read();

        if (done) break;

        // Decode chunk and add to buffer
        buffer += decoder.decode(value, { stream: true });

        // Split buffer by newlines
        const lines = buffer.split('\n');
        
        // Keep the last incomplete line in the buffer
        buffer = lines.pop() || '';

        // Process each complete line
        for (const line of lines) {
          // Parse event type
          if (line.startsWith('event:')) {
            currentEventType = line.substring(6).trim();
            continue;
          }

          // Parse data payload
          if (line.startsWith('data:')) {
            const data = line.substring(5).trim();

            // Skip empty data lines
            if (!data) continue;

            try {
              const parsed = JSON.parse(data);

              // Route to appropriate callback based on event type or data structure
              if (currentEventType === 'token' || parsed.content !== undefined) {
                callbacks.onToken(parsed.content);
              } else if (currentEventType === 'citation' || parsed.chunkId !== undefined) {
                callbacks.onCitation({
                  chunkId: parsed.chunkId,
                  documentId: parsed.documentId,
                  score: parsed.score,
                });
              } else if (currentEventType === 'complete' || parsed.totalTokens !== undefined || Object.keys(parsed).length === 0) {
                callbacks.onComplete();
              } else if (currentEventType === 'error' || parsed.message !== undefined) {
                callbacks.onError(parsed.message);
              }

              // Reset event type after processing
              currentEventType = '';
            } catch (e) {
              // Ignore parse errors for empty or malformed data lines
              console.warn('Failed to parse SSE data:', data, e);
            }
          }
        }
      }
    } catch (err) {
      // Handle abort errors gracefully
      if (err instanceof Error && err.name === 'AbortError') {
        // Query was aborted, don't call error callback
        return;
      }

      // Handle network and other errors
      const errorMessage = err instanceof Error ? err.message : 'Unknown error occurred';
      callbacks.onError(errorMessage);
    }
  },
};
