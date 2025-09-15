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
      
      console.log('🔍 [USE-TAB-DATA] Fetching data for tab:', tabId, 'page:', page, 'pageSize:', pageSize);
      
      const response = await apiService.getTabData(tabId, {
        page: page,
        pageSize
      });
      
      console.log('📊 [USE-TAB-DATA] API response:', response);
      
      if (response.success && response.data) {
        console.log('✅ [USE-TAB-DATA] Data received:', response.data.items?.length || 0, 'items');
        setData(response.data.items || []);
        setTotalCount(response.data.pagination?.totalRecords || 0);
        setCurrentPage(response.data.pagination?.currentPage || 1);
        setTotalPages(response.data.pagination?.totalPages || 0);
      } else {
        console.log('❌ [USE-TAB-DATA] No data in response:', response);
        setError('Failed to fetch tab data');
      }
    } catch (err) {
      console.error('❌ [USE-TAB-DATA] Error fetching tab data:', err);
      setError(err instanceof Error ? err.message : 'An error occurred');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (tabId) {
      fetchData();
    }
  }, [tabId, page, pageSize]);

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
