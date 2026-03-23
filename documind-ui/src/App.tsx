import { DocumentProvider } from './contexts/DocumentContext'
import { UploadPanel } from './components/UploadPanel'
import './App.css'

function App() {
  return (
    <DocumentProvider>
      <div className="min-h-screen bg-gray-50 p-8">
        <div className="max-w-4xl mx-auto">
          <h1 className="text-3xl font-bold text-gray-900 mb-8">DocuMind UI</h1>
          <div className="bg-white rounded-lg shadow-lg p-6">
            <UploadPanel />
          </div>
        </div>
      </div>
    </DocumentProvider>
  )
}

export default App
