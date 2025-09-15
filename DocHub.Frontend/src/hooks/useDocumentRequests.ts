import { useState, useEffect } from 'react';
import { apiService, DocumentRequest } from '../services/api.service';

export interface DocumentRequestWithEmployee extends DocumentRequest {
  employeeName: string;
}

export interface UseDocumentRequestsResult {
  requests: DocumentRequestWithEmployee[];
  loading: boolean;
  error: string | null;
  refetch: () => Promise<void>;
  getRequestsByType: (documentType: string) => DocumentRequestWithEmployee[];
  getRequestsByStatus: (status: string) => DocumentRequestWithEmployee[];
  getRequestsByEmployee: (employeeId: string) => DocumentRequestWithEmployee[];
}

export function useDocumentRequests(documentType?: string): UseDocumentRequestsResult {
  const [requests, setRequests] = useState<DocumentRequestWithEmployee[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchRequests = async () => {
    try {
      setLoading(true);
      setError(null);
      
      const response = await apiService.getDocumentRequests({
        documentType,
        page: 1,
        limit: 1000 // Get all requests for now
      });
      
      if (response.success && response.data) {
        // Map the API response to include employeeName
        const mappedRequests: DocumentRequestWithEmployee[] = (response.data.items || []).map(request => ({
          ...request,
          employeeName: request.employeeName || 'Unknown Employee'
        }));
        setRequests(mappedRequests);
      } else {
        setError(response.error?.message || 'Failed to fetch document requests');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An error occurred');
      console.error('Error fetching document requests:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchRequests();
  }, [documentType]);

  const getRequestsByType = (type: string) => {
    return requests.filter(req => req.documentType === type);
  };

  const getRequestsByStatus = (status: string) => {
    return requests.filter(req => req.status === status);
  };

  const getRequestsByEmployee = (employeeId: string) => {
    return requests.filter(req => req.employeeId === employeeId);
  };

  return {
    requests,
    loading,
    error,
    refetch: fetchRequests,
    getRequestsByType,
    getRequestsByStatus,
    getRequestsByEmployee
  };
}
