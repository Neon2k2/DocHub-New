import { useState, useEffect } from 'react';
import { apiService, TabDataRecord } from '../services/api.service';

export interface UseTabDataResult {
  data: TabDataRecord[];
  loading: boolean;
  error: string | null;
  refetch: () => Promise<void>;
  totalCount: number;
  page: number;
  totalPages: number;
}

export function useTabData(tabId: string, page: number = 1, pageSize: number = 10): UseTabDataResult {
  const [data, setData] = useState<TabDataRecord[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [totalCount, setTotalCount] = useState(0);
  const [currentPage, setCurrentPage] = useState(page);
  const [totalPages, setTotalPages] = useState(0);

  const fetchData = async () => {
    try {
      setLoading(true);
      setError(null);
      
      const response = await apiService.getTabData(tabId, {
        page: currentPage,
        pageSize
      });
      
      if (response.success && response.data) {
        setData(response.data.items || []);
        setTotalCount(response.data.pagination?.totalRecords || 0);
        setCurrentPage(response.data.pagination?.currentPage || 1);
        setTotalPages(response.data.pagination?.totalPages || 0);
      } else {
        setError('Failed to fetch tab data');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An error occurred');
      console.error('Error fetching tab data:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (tabId) {
      fetchData();
    }
  }, [tabId, currentPage, pageSize]);

  return {
    data,
    loading,
    error,
    refetch: fetchData,
    totalCount,
    page: currentPage,
    totalPages
  };
}
