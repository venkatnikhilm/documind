import { DocumentProvider } from './contexts/DocumentContext'
import { MessageProvider } from './contexts/MessageContext'
import { Header } from './components/Header'
import { MainLayout } from './components/MainLayout'
import { UploadPanel } from './components/UploadPanel'
import { QueryPanel } from './components/QueryPanel'
import { ErrorToast } from './components/ErrorToast'
import { useState } from 'react'
import './App.css'

function App() {
  const [toastMessage, setToastMessage] = useState<string | null>(null)
  const [toastType] = useState<'success' | 'error' | 'warning'>('error')

  return (
    <DocumentProvider>
      <MessageProvider>
        <div className="flex flex-col h-screen bg-gray-50">
          <Header />
          <MainLayout>
            <UploadPanel className="w-full md:w-1/3 md:min-w-[320px]" />
            <QueryPanel className="flex-1" />
          </MainLayout>
          {toastMessage && (
            <ErrorToast
              message={toastMessage}
              type={toastType}
              onClose={() => setToastMessage(null)}
            />
          )}
        </div>
      </MessageProvider>
    </DocumentProvider>
  )
}

export default App
