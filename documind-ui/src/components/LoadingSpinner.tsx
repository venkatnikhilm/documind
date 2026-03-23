export function LoadingSpinner() {
  return (
    <div 
      className="inline-block w-6 h-6 border-2 border-blue-600 border-t-transparent rounded-full animate-spin"
      role="status"
      aria-label="Loading"
    >
      <span className="sr-only">Loading...</span>
    </div>
  );
}
