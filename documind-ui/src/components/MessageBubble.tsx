import { useState } from 'react';
import type { Message } from '../types/message';

interface MessageBubbleProps {
  message: Message;
  isStreaming?: boolean;
}

export function MessageBubble({ 
  message, 
  isStreaming = false 
}: MessageBubbleProps) {
  const [copied, setCopied] = useState(false);
  
  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(message.content);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch (err) {
      console.error('Failed to copy text:', err);
    }
  };
  
  const isUser = message.role === 'user';
  
  return (
    <div 
      className={`
        p-4 rounded-lg mb-4 animate-fade-in
        ${isUser 
          ? 'bg-blue-100 text-blue-900 ml-auto max-w-[80%]' 
          : 'bg-gray-100 text-gray-900 mr-auto max-w-[80%]'
        }
      `}
    >
      <div className="message-content whitespace-pre-wrap break-words">
        {message.content}
        {isStreaming && (
          <span 
            className="inline-block w-2 h-5 bg-blue-600 animate-pulse ml-1 align-middle"
            aria-label="Streaming"
          />
        )}
      </div>
      
      {!isUser && !isStreaming && (
        <>
          <button
            onClick={handleCopy}
            className="mt-2 text-xs text-gray-500 hover:text-gray-700 transition-colors flex items-center gap-1"
            aria-label="Copy message"
          >
            {copied ? (
              <>
                <svg 
                  className="h-4 w-4" 
                  fill="none" 
                  stroke="currentColor" 
                  viewBox="0 0 24 24"
                  aria-hidden="true"
                >
                  <path 
                    strokeLinecap="round" 
                    strokeLinejoin="round" 
                    strokeWidth={2} 
                    d="M5 13l4 4L19 7" 
                  />
                </svg>
                <span>Copied!</span>
              </>
            ) : (
              <>
                <svg 
                  className="h-4 w-4" 
                  fill="none" 
                  stroke="currentColor" 
                  viewBox="0 0 24 24"
                  aria-hidden="true"
                >
                  <path 
                    strokeLinecap="round" 
                    strokeLinejoin="round" 
                    strokeWidth={2} 
                    d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z" 
                  />
                </svg>
                <span>Copy</span>
              </>
            )}
          </button>
          
          {message.citations && message.citations.length > 0 && (
            <div className="mt-3 pt-3 border-t border-gray-300">
              <p className="text-xs font-medium text-gray-600 mb-2">Sources:</p>
              <ul className="space-y-1">
                {message.citations.map((citation, index) => (
                  <li 
                    key={`${citation.chunkId}-${index}`}
                    className="text-xs text-gray-500"
                  >
                    <span className="font-mono">
                      Chunk {citation.chunkId.substring(0, 8)}...
                    </span>
                    <span className="text-gray-400 ml-2">
                      (score: {citation.score.toFixed(3)})
                    </span>
                  </li>
                ))}
              </ul>
            </div>
          )}
        </>
      )}
    </div>
  );
}
