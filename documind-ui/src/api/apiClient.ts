import type { APIError } from '../types/error';

/**
 * Base API client for making HTTP requests to the DocuMind backend.
 * Uses relative paths that are proxied by Vite dev server to localhost:5161.
 */
export class APIClient {
  private defaultHeaders: HeadersInit;

  constructor() {
    this.defaultHeaders = {
      'Content-Type': 'application/json',
    };
  }

  /**
   * Make a generic HTTP request using fetch API
   * @param endpoint - Relative API endpoint (e.g., /api/documents/upload)
   * @param options - Fetch request options
   * @returns Parsed JSON response
   * @throws Error with user-friendly message on failure
   */
  async request<T>(
    endpoint: string,
    options: RequestInit = {}
  ): Promise<T> {
    const config: RequestInit = {
      ...options,
      headers: {
        ...this.defaultHeaders,
        ...options.headers,
      },
    };

    try {
      const response = await fetch(endpoint, config);

      if (!response.ok) {
        // Try to parse error response
        let errorData: APIError;
        try {
          errorData = await response.json();
        } catch {
          // If JSON parsing fails, create a generic error
          errorData = {
            error: 'RequestFailed',
            message: this.getStatusMessage(response.status),
          };
        }

        // Map status codes to user-friendly messages
        const message = this.mapErrorMessage(response.status, errorData);
        throw new Error(message);
      }

      return response.json();
    } catch (err) {
      // Handle network errors
      if (err instanceof TypeError) {
        throw new Error('Unable to connect to the server. Please check your network connection.');
      }
      // Re-throw other errors
      throw err;
    }
  }

  /**
   * Upload a file with progress tracking using XMLHttpRequest
   * @param endpoint - Relative API endpoint (e.g., /api/documents/upload)
   * @param file - File to upload
   * @param onProgress - Optional callback for upload progress (0-100)
   * @returns Parsed JSON response
   */
  async upload(
    endpoint: string,
    file: File,
    onProgress?: (progress: number) => void
  ): Promise<any> {
    const formData = new FormData();
    formData.append('file', file);

    return new Promise((resolve, reject) => {
      const xhr = new XMLHttpRequest();

      // Track upload progress
      xhr.upload.addEventListener('progress', (e) => {
        if (e.lengthComputable && onProgress) {
          const progress = (e.loaded / e.total) * 100;
          onProgress(progress);
        }
      });

      // Handle successful completion
      xhr.addEventListener('load', () => {
        if (xhr.status >= 200 && xhr.status < 300) {
          try {
            const response = JSON.parse(xhr.responseText);
            resolve(response);
          } catch {
            reject(new Error('Invalid response from server'));
          }
        } else {
          // Try to parse error response
          let errorMessage: string;
          try {
            const errorData: APIError = JSON.parse(xhr.responseText);
            errorMessage = this.mapErrorMessage(xhr.status, errorData);
          } catch {
            errorMessage = this.getStatusMessage(xhr.status);
          }
          reject(new Error(errorMessage));
        }
      });

      // Handle network errors
      xhr.addEventListener('error', () => {
        reject(new Error('Network error. Please check your connection and try again.'));
      });

      // Handle aborted requests
      xhr.addEventListener('abort', () => {
        reject(new Error('Upload cancelled'));
      });

      xhr.open('POST', endpoint);
      xhr.send(formData);
    });
  }

  /**
   * Map HTTP status codes to user-friendly error messages
   */
  private mapErrorMessage(status: number, errorData: APIError): string {
    switch (status) {
      case 400:
        return errorData.message || 'Invalid request. Please check your input and try again.';
      case 404:
        return errorData.message || 'Resource not found. It may have been deleted.';
      case 503:
        return errorData.message || 'Service temporarily unavailable. Please try again in a few moments.';
      default:
        return errorData.message || this.getStatusMessage(status);
    }
  }

  /**
   * Get generic status message for HTTP status codes
   */
  private getStatusMessage(status: number): string {
    if (status >= 500) {
      return 'Server error. Please try again later.';
    } else if (status >= 400) {
      return 'Request failed. Please try again.';
    }
    return 'An unexpected error occurred.';
  }
}

/**
 * Singleton API client instance
 */
export const apiClient = new APIClient();
