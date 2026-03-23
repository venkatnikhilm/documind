import { DocumentProvider } from './contexts/DocumentContext';
import { MessageProvider } from './contexts/MessageContext';
import { ToastProvider } from './components/ErrorToast';
import { UploadPanel } from './components/UploadPanel';
import { QueryPanel } from './components/QueryPanel';

function App() {
  return (
    <ToastProvider>
      <DocumentProvider>
        <MessageProvider>
          <div className="min-h-screen bg-background grid-bg">
            {/* Header */}
            <header className="border-b border-neutral-800 px-6 py-4">
              <div className="flex items-center gap-3">
                <div className="w-8 h-8 rounded-lg bg-accent/20 flex items-center justify-center">
                  <svg
                    className="w-5 h-5 text-accent"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
                    />
                  </svg>
                </div>
                <div>
                  <h1 className="text-lg font-medium text-neutral-100 tracking-tight">
                    DocuMind
                  </h1>
                  <p className="text-xs text-neutral-500">
                    Intelligent Document Q&A
                  </p>
                </div>
                <div className="ml-auto flex items-center gap-2">
                  <span className="px-2 py-1 text-[10px] font-medium uppercase tracking-wider bg-accent/10 text-accent rounded">
                    Beta
                  </span>
                </div>
              </div>
            </header>

            {/* Main Content */}
            <main className="h-[calc(100vh-73px)] flex flex-col md:flex-row">
              {/* Upload Panel - Left Side */}
              <aside className="w-full md:w-[340px] md:flex-shrink-0 border-b md:border-b-0 overflow-auto">
                <UploadPanel />
              </aside>

              {/* Query Panel - Right Side */}
              <section className="flex-1 min-w-0 overflow-hidden">
                <QueryPanel />
              </section>
            </main>
          </div>
        </MessageProvider>
      </DocumentProvider>
    </ToastProvider>
  );
}

export default App;
