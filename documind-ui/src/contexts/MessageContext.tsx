import { createContext, useContext, useState } from 'react';
import type { PropsWithChildren } from 'react';
import type { Message, Citation } from '../types/message';

interface MessageContextValue {
  messages: Message[];
  addMessage: (message: Omit<Message, 'id' | 'timestamp'>) => string;
  updateMessage: (id: string, content: string) => void;
  addCitation: (messageId: string, citation: Citation) => void;
  clearMessages: () => void;
}

const MessageContext = createContext<MessageContextValue | null>(null);

export function MessageProvider({ children }: PropsWithChildren) {
  const [messages, setMessages] = useState<Message[]>([]);

  const addMessage = (message: Omit<Message, 'id' | 'timestamp'>) => {
    const id = crypto.randomUUID();
    const newMessage: Message = {
      ...message,
      id,
      timestamp: new Date(),
    };
    setMessages((prev) => [...prev, newMessage]);
    return id;
  };

  const updateMessage = (id: string, content: string) => {
    setMessages((prev) =>
      prev.map((msg) => (msg.id === id ? { ...msg, content } : msg))
    );
  };

  const addCitation = (messageId: string, citation: Citation) => {
    setMessages((prev) =>
      prev.map((msg) =>
        msg.id === messageId
          ? { ...msg, citations: [...(msg.citations || []), citation] }
          : msg
      )
    );
  };

  const clearMessages = () => {
    setMessages([]);
  };

  return (
    <MessageContext.Provider
      value={{
        messages,
        addMessage,
        updateMessage,
        addCitation,
        clearMessages,
      }}
    >
      {children}
    </MessageContext.Provider>
  );
}

export function useMessages() {
  const context = useContext(MessageContext);
  if (!context) {
    throw new Error('useMessages must be used within MessageProvider');
  }
  return context;
}
