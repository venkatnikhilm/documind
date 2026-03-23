import { useState } from 'react';

interface DropZoneProps {
  onDrop: (files: FileList) => void;
  accept?: string[];
}

export function DropZone({ 
  onDrop, 
  accept = ['.pdf', '.docx'] 
}: DropZoneProps) {
  const [isDragging, setIsDragging] = useState(false);
  
  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(true);
  };
  
  const handleDragLeave = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(false);
  };
  
  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(false);
    
    if (e.dataTransfer.files && e.dataTransfer.files.length > 0) {
      onDrop(e.dataTransfer.files);
    }
  };
  
  const handleClick = () => {
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = accept.join(',');
    input.onchange = (e) => {
      const files = (e.target as HTMLInputElement).files;
      if (files && files.length > 0) {
        onDrop(files);
      }
    };
    input.click();
  };
  
  return (
    <div
      className={`
        border-2 border-dashed rounded-lg p-8 text-center cursor-pointer
        transition-all duration-200
        ${isDragging 
          ? 'border-blue-500 bg-blue-50 scale-105' 
          : 'border-gray-300 bg-white hover:border-blue-400 hover:bg-blue-50'
        }
      `}
      onDragOver={handleDragOver}
      onDragLeave={handleDragLeave}
      onDrop={handleDrop}
      onClick={handleClick}
      role="button"
      tabIndex={0}
      aria-label="Upload document"
    >
      <svg 
        className="mx-auto h-12 w-12 text-gray-400" 
        stroke="currentColor" 
        fill="none" 
        viewBox="0 0 48 48" 
        aria-hidden="true"
      >
        <path
          d="M28 8H12a4 4 0 00-4 4v20m32-12v8m0 0v8a4 4 0 01-4 4H12a4 4 0 01-4-4v-4m32-4l-3.172-3.172a4 4 0 00-5.656 0L28 28M8 32l9.172-9.172a4 4 0 015.656 0L28 28m0 0l4 4m4-24h8m-4-4v8m-12 4h.02"
          strokeWidth={2}
          strokeLinecap="round"
          strokeLinejoin="round"
        />
      </svg>
      <p className="mt-4 text-base text-gray-600">
        Drag and drop PDF or DOCX files here
      </p>
      <p className="mt-1 text-sm text-gray-500">
        or click to browse
      </p>
    </div>
  );
}
