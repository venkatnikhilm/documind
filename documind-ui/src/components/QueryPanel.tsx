import { useState, useRef, useEffect } from 'react';
import { useDocuments } from '../contexts/DocumentContext';
import { useMessages } from '../contexts/MessageContext';
import { useQuery } from '../hooks/useQuery';
import { MessageBubble } from './MessageBubble';

interface QueryPanelProps {
  className?: string;
}

export function QueryPanel({ className }: QueryPanelProps) {
  const { documents } = useDocuments();
  const { messages, addMessage, updateMessage, addCitation } = useMessages();
  const { sendQuery, isStreaming } = useQuery();
  
  const [question, setQuestion] = useState('');
  const [selectedDocId, setSelectedDocId] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  
  const chatContainerRef = useRef<HTMLDivElement>(null);
  
  // Auto-scroll to bottom when new content arrives
  useEffect(() => {
    if (chatContainerRef.current) {
      chatContainerRef.current.scrollTop = chatContainerRef.current.scrollHeight;
    }
  }, [messages]);
  
  const handleSubmit = async () => {
    if (!question.trim() || isStreaming || documents.length === 0) return;
    
    // Clear error message
    setErrorMessage(null);
    
    // Add user message immediately
    addMessage({
      role: 'user',
      content: question.trim(),
    });
    
    // Create placeholder assistant message
    const assistantMessageId = addMessage({
      role: 'assistant',
      content: '',
    });
    
    // Clear input
    const currentQuestion = question.trim();
    setQuestion('');
    
    // Track current content for token accumulation
    let currentContent = '';
    
    // Send query with streaming callbacks
    await sendQuery(
      currentQuestion,
      selectedDocId,
      // onToken: append token to assistant message content
      (token: string) => {
        currentContent += token;
        updateMessage(assistantMessageId, currentContent);
      },
      // onCitation: add citation to assistant message
      (citation) => {
        addCitation(assistantMessageId, citation);
      },
      // onComplete: re-enable input field (handled by isStreaming state)
      () => {
        // Streaming complete
      },
      // onError: display error toast, show partial response if any
      (error: string) => {
        setErrorMessage(error);
      }
    );
  };
  
  const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSubmit();
    }
  };
  
  const isInputDisabled = isStreaming || documents.length === 0;
  
  return (
    <div className={className}>
      <div className="flex flex-col h-full">
        {/* Document Selector */}
        <div className="p-4 border-b border-gray-200">
          <label htmlFor="document-selector" className="block text-sm font-medium text-gray-700 mb-2">
            Query Document
          </label>
          <select
            id="document-selector"
            value={selectedDocId || ''}
            onChange={(e) => setSelectedDocId(e.target.value || null)}
            disabled={documents.length === 0}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:bg-gray-100 disabled:cursor-not-allowed"
          >
            <option value="">All Documents</option>
            {documents.map((doc) => (
              <option key={doc.id} value={doc.id}>
                {doc.fileName}
              </option>
            ))}
          </select>
        </div>
        
        {/* Chat History */}
        <div 
          ref={chatContainerRef}
          className="flex-1 overflow-y-auto p-4 space-y-4"
        >
          {messages.length === 0 ? (
            <div className="flex items-center justify-center h-full text-gray-500">
              <div className="text-center">
                <svg 
                  className="mx-auto h-12 w-12 text-gray-400 mb-3" 
                  fill="none" 
                  stroke="currentColor" 
                  viewBox="0 0 24 24"
                  aria-hidden="true"
                >
                  <path 
                    strokeLinecap="round" 
                    strokeLinejoin="round" 
                    strokeWidth={2} 
                    d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" 
                  />
                </svg>
                <p className="text-sm">No messages yet</p>
                <p className="text-xs mt-1">Ask a question about your documents</p>
              </div>
            </div>
          ) : (
            messages.map((message, index) => (
              <MessageBubble
                key={message.id}
                message={message}
                isStreaming={
                  isStreaming &&
                  index === messages.length - 1 &&
                  message.role === 'assistant'
                }
              />
            ))
          )}
        </div>
        
        {/* Error Message */}
        {errorMessage && (
          <div className="mx-4 mb-2 bg-red-50 border border-red-200 rounded-lg p-3">
            <div className="flex items-start gap-2">
              <svg 
                className="h-5 w-5 text-red-500 flex-shrink-0 mt-0.5" 
                fill="none" 
                stroke="currentColor" 
                viewBox="0 0 24 24"
                aria-hidden="true"
              >
                <path 
                  strokeLinecap="round" 
                  strokeLinejoin="round" 
                  strokeWidth={2} 
                  d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" 
                />
              </svg>
              <p className="text-sm text-red-800 flex-1">{errorMessage}</p>
              <button
                onClick={() => setErrorMessage(null)}
                className="flex-shrink-0 text-red-500 hover:text-red-700"
                aria-label="Close error message"
              >
                <svg 
                  className="h-5 w-5" 
                  fill="none" 
                  stroke="currentColor" 
                  viewBox="0 0 24 24"
                  aria-hidden="true"
                >
                  <path 
                    strokeLinecap="round" 
                    strokeLinejoin="round" 
                    strokeWidth={2} 
                    d="M6 18L18 6M6 6l12 12" 
                  />
                </svg>
              </button>
            </div>
          </div>
        )}
        
        {/* Query Input */}
        <div className="p-4 border-t border-gray-200">
          {documents.length === 0 ? (
            <div className="text-center py-4 text-gray-500">
              <p className="text-sm">Please upload a document to start asking questions</p>
            </div>
          ) : (
            <div className="flex gap-2">
              <textarea
                value={question}
                onChange={(e) => setQuestion(e.target.value)}
                onKeyDown={handleKeyDown}
                disabled={isInputDisabled}
                placeholder="Ask a question about your documents..."
                className="flex-1 px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:bg-gray-100 disabled:cursor-not-allowed resize-none"
                rows={2}
              />
              <button
                onClick={handleSubmit}
                disabled={isInputDisabled || !question.trim()}
                className="px-6 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 active:bg-blue-800 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed transition-colors duration-150 self-end"
                aria-label="Send query"
              >
                <svg 
                  className="h-5 w-5" 
                  fill="none" 
                  stroke="currentColor" 
                  viewBox="0 0 24 24"
                  aria-hidden="true"
                >
                  <path 
                    strokeLinecap="round" 
                    strokeLinejoin="round" 
                    strokeWidth={2} 
                    d="M12 19l9 2-9-18-9 18 9-2zm0 0v-8" 
                  />
                </svg>
              </button>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
