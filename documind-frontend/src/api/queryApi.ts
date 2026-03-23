import type { QueryRequest, Citation } from '../types';

interface QueryCallbacks {
  onToken: (content: string) => void;
  onCitation: (citation: Citation) => void;
  onComplete: () => void;
  onError: (error: string) => void;
}

export async function sendQuery(
  request: QueryRequest,
  callbacks: QueryCallbacks,
  abortController: AbortController
): Promise<void> {
  try {
    const body: Record<string, string> = { question: request.question };
    if (request.documentId) {
      body.documentId = request.documentId;
    }

    const response = await fetch('/api/documents/query', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
      signal: abortController.signal,
    });

    if (!response.ok) {
      throw new Error(`Query failed: ${response.statusText}`);
    }

    if (!response.body) {
      throw new Error('No response body');
    }

    const reader = response.body.getReader();
    const decoder = new TextDecoder();
    let buffer = '';
    let currentEvent = '';

    while (true) {
      const { done, value } = await reader.read();
      if (done) break;

      buffer += decoder.decode(value, { stream: true });
      const lines = buffer.split('\n');
      buffer = lines.pop() || '';

      for (const line of lines) {
        if (line.startsWith('event:')) {
          currentEvent = line.substring(6).trim();
        } else if (line.startsWith('data:')) {
          const data = line.substring(5).trim();
          if (!data) continue;

          try {
            const parsed = JSON.parse(data);

            if (currentEvent === 'token') {
              callbacks.onToken(parsed.content);
            } else if (currentEvent === 'citation') {
              callbacks.onCitation({
                chunkId: parsed.chunkId,
                documentId: parsed.documentId,
                score: parsed.score,
              });
            } else if (currentEvent === 'complete') {
              callbacks.onComplete();
            } else if (currentEvent === 'error') {
              callbacks.onError(parsed.message || 'Unknown error');
            }
          } catch {
            // Ignore parse errors
          }
          currentEvent = '';
        }
      }
    }
  } catch (error) {
    if ((error as Error).name === 'AbortError') {
      return;
    }
    callbacks.onError((error as Error).message || 'An error occurred');
  }
}
