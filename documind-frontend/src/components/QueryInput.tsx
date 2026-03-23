import { useState, type KeyboardEvent, type ChangeEvent } from 'react';
import { useQuery } from '../hooks/useQuery';
import { useMessages } from '../contexts/MessageContext';
import { useDocuments } from '../contexts/DocumentContext';

export function QueryInput() {
  const [input, setInput] = useState('');
  const { submitQuery } = useQuery();
  const { isStreaming } = useMessages();
  const { documents } = useDocuments();

  const canSubmit = input.trim().length > 0 && !isStreaming && documents.length > 0;

  const handleSubmit = () => {
    if (!canSubmit) return;
    submitQuery(input.trim());
    setInput('');
  };

  const handleKeyDown = (e: KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSubmit();
    }
  };

  const handleChange = (e: ChangeEvent<HTMLTextAreaElement>) => {
    setInput(e.target.value);
  };

  return (
    <div className="p-4 border-t border-neutral-800">
      <div className="flex gap-3">
        <div className="flex-1 relative">
          <textarea
            value={input}
            onChange={handleChange}
            onKeyDown={handleKeyDown}
            disabled={isStreaming || documents.length === 0}
            placeholder={
              documents.length === 0
                ? 'Upload a document first...'
                : 'Ask a question about your documents...'
            }
            rows={1}
            className={`
              w-full bg-surface border border-neutral-700 rounded-lg
              px-4 py-3 text-sm font-mono text-neutral-200
              placeholder:text-neutral-600
              focus:outline-none focus:border-accent/50 focus:ring-1 focus:ring-accent/20
              resize-none min-h-[48px] max-h-[120px]
              transition-colors
              ${(isStreaming || documents.length === 0) ? 'opacity-50 cursor-not-allowed' : ''}
            `}
            style={{
              height: 'auto',
              minHeight: '48px',
            }}
            onInput={(e) => {
              const target = e.target as HTMLTextAreaElement;
              target.style.height = 'auto';
              target.style.height = `${Math.min(target.scrollHeight, 120)}px`;
            }}
          />
          <p className="absolute -bottom-5 left-0 text-[10px] text-neutral-600">
            Press Enter to send, Shift+Enter for new line
          </p>
        </div>
        
        <button
          onClick={handleSubmit}
          disabled={!canSubmit}
          className={`
            flex-shrink-0 w-12 h-12 rounded-lg flex items-center justify-center
            transition-all duration-150
            ${
              canSubmit
                ? 'bg-accent text-background hover:bg-accent/90 active:scale-95'
                : 'bg-neutral-800 text-neutral-600 cursor-not-allowed'
            }
          `}
        >
          {isStreaming ? (
            <svg className="w-5 h-5 animate-spin" fill="none" viewBox="0 0 24 24">
              <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
              <path
                className="opacity-75"
                fill="currentColor"
                d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
              />
            </svg>
          ) : (
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M12 19l9 2-9-18-9 18 9-2zm0 0v-8"
              />
            </svg>
          )}
        </button>
      </div>
    </div>
  );
}
