import { toast } from 'sonner';

export interface ApiError {
  message: string;
  code?: string;
  status?: number;
}

export class ErrorHandler {
  static handle(error: unknown, context?: string): void {
    console.error(`Error in ${context || 'unknown context'}:`, error);
    
    let message = 'An unexpected error occurred';
    
    if (error instanceof Error) {
      message = error.message;
    } else if (typeof error === 'string') {
      message = error;
    } else if (error && typeof error === 'object' && 'message' in error) {
      message = (error as ApiError).message;
    }
    
    toast.error(message);
  }
  
  static handleAsync<T>(
    asyncFn: () => Promise<T>,
    context?: string,
    onError?: (error: unknown) => void
  ): Promise<T | null> {
    return asyncFn()
      .then(result => result)
      .catch(error => {
        this.handle(error, context);
        if (onError) onError(error);
        return null;
      });
  }
  
  static withLoading<T>(
    asyncFn: () => Promise<T>,
    setLoading: (loading: boolean) => void,
    context?: string
  ): Promise<T | null> {
    setLoading(true);
    return this.handleAsync(asyncFn, context)
      .finally(() => setLoading(false));
  }
}

export const handleError = ErrorHandler.handle;
export const handleAsync = ErrorHandler.handleAsync;
export const withLoading = ErrorHandler.withLoading;
