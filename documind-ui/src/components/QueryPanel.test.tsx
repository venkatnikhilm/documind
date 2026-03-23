import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryPanel } from './QueryPanel';
import { DocumentProvider } from '../contexts/DocumentContext';
import { MessageProvider } from '../contexts/MessageContext';

// Test wrapper with required providers
function TestWrapper({ children }: { children: React.ReactNode }) {
  return (
    <DocumentProvider>
      <MessageProvider>
        {children}
      </MessageProvider>
    </DocumentProvider>
  );
}

describe('QueryPanel', () => {
  it('renders without crashing', () => {
    render(
      <TestWrapper>
        <QueryPanel />
      </TestWrapper>
    );
    
    expect(screen.getByLabelText('Query Document')).toBeInTheDocument();
  });

  it('displays prompt message when no documents are uploaded', () => {
    render(
      <TestWrapper>
        <QueryPanel />
      </TestWrapper>
    );
    
    expect(screen.getByText(/please upload a document to start asking questions/i)).toBeInTheDocument();
  });

  it('displays empty chat history message', () => {
    render(
      <TestWrapper>
        <QueryPanel />
      </TestWrapper>
    );
    
    expect(screen.getByText(/no messages yet/i)).toBeInTheDocument();
  });

  it('renders document selector with "All Documents" option', () => {
    render(
      <TestWrapper>
        <QueryPanel />
      </TestWrapper>
    );
    
    const selector = screen.getByLabelText('Query Document') as HTMLSelectElement;
    expect(selector).toBeInTheDocument();
    expect(screen.getByText('All Documents')).toBeInTheDocument();
  });
});
