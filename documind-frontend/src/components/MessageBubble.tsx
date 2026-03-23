import { useState } from 'react';
import type { Message } from '../types';
import { useMessages } from '../contexts/MessageContext';

interface MessageBubbleProps {
  message: Message;
}

export function MessageBubble({ message }: MessageBubbleProps) {
  const [copied, setCopied] = useState(false);
  const { isStreaming, currentStreamingId } = useMessages();
  const isCurrentlyStreaming = isStreaming && currentStreamingId === message.id;
  const isUser = message.role === 'user';
  const isError = message.content.startsWith('Error:');

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(message.content);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch (err) {
      console.error('Failed to copy:', err);
    }
  };

  return (
    <div
      className={`flex ${isUser ? 'justify-end' : 'justify-start'}`}
    >
      <div
        className={`
          max-w-[85%] rounded-lg px-4 py-3
          ${
            isUser
              ? 'bg-accent text-background'
              : isError
              ? 'bg-red-500/10 border border-red-500/20 text-red-400'
              : 'bg-surface border border-neutral-800 text-neutral-200'
          }
        `}
      >
        {/* Message content */}
        <p className="text-sm whitespace-pre-wrap break-words">
          {message.content}
          {isCurrentlyStreaming && (
            <span className="inline-block w-2 h-4 ml-0.5 bg-accent cursor-blink" />
          )}
        </p>

        {/* Citations */}
        {!isUser && message.citations.length > 0 && !isCurrentlyStreaming && (
          <div className="flex flex-wrap gap-1.5 mt-3 pt-3 border-t border-neutral-700/50">
            {message.citations.map((citation, idx) => (
              <span
                key={`${citation.chunkId}-${idx}`}
                className="inline-flex items-center gap-1 px-2 py-0.5 bg-neutral-800 rounded text-xs text-neutral-400"
              >
                <span className="font-medium">Chunk {citation.chunkId.slice(0, 6)}</span>
                <span className="text-neutral-600">·</span>
                <span>score: {citation.score.toFixed(2)}</span>
              </span>
            ))}
          </div>
        )}

        {/* Copy button for assistant messages */}
        {!isUser && !isCurrentlyStreaming && !isError && (
          <div className="flex justify-end mt-2">
            <button
              onClick={handleCopy}
              className="p-1.5 rounded hover:bg-neutral-700/50 transition-colors group"
              title="Copy to clipboard"
            >
              {copied ? (
                <svg
                  className="w-4 h-4 text-accent"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                </svg>
              ) : (
                <svg
                  className="w-4 h-4 text-neutral-500 group-hover:text-neutral-300"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z"
                  />
                </svg>
              )}
            </button>
          </div>
        )}
      </div>
    </div>
  );
}
