import { createContext, useContext, useState, useCallback, type ReactNode } from 'react';
import type { Message, Citation } from '../types';

interface MessageContextType {
  messages: Message[];
  isStreaming: boolean;
  currentStreamingId: string | null;
  addUserMessage: (content: string) => string;
  startAssistantMessage: () => string;
  appendToMessage: (id: string, content: string) => void;
  addCitationsToMessage: (id: string, citations: Citation[]) => void;
  finalizeMessage: (id: string) => void;
  setIsStreaming: (value: boolean) => void;
  addErrorMessage: (error: string) => void;
}

const MessageContext = createContext<MessageContextType | undefined>(undefined);

function generateId(): string {
  return Math.random().toString(36).substring(2, 15);
}

export function MessageProvider({ children }: { children: ReactNode }) {
  const [messages, setMessages] = useState<Message[]>([]);
  const [isStreaming, setIsStreaming] = useState(false);
  const [currentStreamingId, setCurrentStreamingId] = useState<string | null>(null);

  const addUserMessage = useCallback((content: string): string => {
    const id = generateId();
    const message: Message = {
      id,
      role: 'user',
      content,
      citations: [],
      timestamp: new Date(),
    };
    setMessages((prev) => [...prev, message]);
    return id;
  }, []);

  const startAssistantMessage = useCallback((): string => {
    const id = generateId();
    const message: Message = {
      id,
      role: 'assistant',
      content: '',
      citations: [],
      timestamp: new Date(),
    };
    setMessages((prev) => [...prev, message]);
    setCurrentStreamingId(id);
    return id;
  }, []);

  const appendToMessage = useCallback((id: string, content: string) => {
    setMessages((prev) =>
      prev.map((msg) =>
        msg.id === id ? { ...msg, content: msg.content + content } : msg
      )
    );
  }, []);

  const addCitationsToMessage = useCallback((id: string, citations: Citation[]) => {
    setMessages((prev) =>
      prev.map((msg) =>
        msg.id === id
          ? { ...msg, citations: [...msg.citations, ...citations] }
          : msg
      )
    );
  }, []);

  const finalizeMessage = useCallback((id: string) => {
    setCurrentStreamingId(null);
    setIsStreaming(false);
    setMessages((prev) =>
      prev.map((msg) =>
        msg.id === id ? { ...msg, timestamp: new Date() } : msg
      )
    );
  }, []);

  const addErrorMessage = useCallback((error: string) => {
    const id = generateId();
    const message: Message = {
      id,
      role: 'assistant',
      content: `Error: ${error}`,
      citations: [],
      timestamp: new Date(),
    };
    setMessages((prev) => [...prev, message]);
    setIsStreaming(false);
    setCurrentStreamingId(null);
  }, []);

  return (
    <MessageContext.Provider
      value={{
        messages,
        isStreaming,
        currentStreamingId,
        addUserMessage,
        startAssistantMessage,
        appendToMessage,
        addCitationsToMessage,
        finalizeMessage,
        setIsStreaming,
        addErrorMessage,
      }}
    >
      {children}
    </MessageContext.Provider>
  );
}

export function useMessages() {
  const context = useContext(MessageContext);
  if (context === undefined) {
    throw new Error('useMessages must be used within a MessageProvider');
  }
  return context;
}
