import { useState, useEffect, useCallback } from 'react';
import { apiService, DashboardStats } from '../services/api.service';

interface UseDashboardReturn {
  stats: DashboardStats | null;
  loading: boolean;
  error: string | null;
  refetch: () => void;
}

export const useDashboard = (module: 'er' | 'billing'): UseDashboardReturn => {
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchStats = useCallback(async () => {
    setLoading(true);
    setError(null);
    
    try {
      const response = await apiService.getDashboardStats(module);
      if (response.success && response.data) {
        setStats(response.data);
      } else {
        throw new Error('Failed to fetch dashboard stats');
      }
    } catch (err: any) {
      setError(err.message || 'Failed to fetch dashboard stats');
      setStats(null);
    } finally {
      setLoading(false);
    }
  }, [module]);

  useEffect(() => {
    fetchStats();
  }, [fetchStats]);

  return {
    stats,
    loading,
    error,
    refetch: fetchStats,
  };
};