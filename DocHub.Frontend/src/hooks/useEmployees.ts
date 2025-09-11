import { useState, useEffect, useCallback } from 'react';
import { apiService, Employee, CreateEmployeeRequest } from '../services/api.service';

interface UseEmployeesParams {
  page?: number;
  limit?: number;
  search?: string;
  department?: string;
  status?: string;
  tabId?: string;
}

interface UseEmployeesReturn {
  employees: Employee[];
  loading: boolean;
  error: string | null;
  pagination: {
    currentPage: number;
    totalPages: number;
    totalRecords: number;
    hasNext: boolean;
    hasPrevious: boolean;
  } | null;
  refetch: () => void;
  createEmployee: (employee: CreateEmployeeRequest) => Promise<void>;
  updateEmployee: (id: string, employee: Partial<Employee>) => Promise<void>;
  deleteEmployee: (id: string) => Promise<void>;
}

export const useEmployees = (params: UseEmployeesParams = {}): UseEmployeesReturn => {
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [pagination, setPagination] = useState<UseEmployeesReturn['pagination']>(null);

  const fetchEmployees = useCallback(async () => {
    setLoading(true);
    setError(null);
    
    try {
      const response = await apiService.getEmployees(params);
      if (response.success && response.data) {
        setEmployees(response.data.items);
        setPagination(response.data.pagination);
      } else {
        throw new Error('Failed to fetch employees');
      }
    } catch (err: any) {
      setError(err.message || 'Failed to fetch employees');
      setEmployees([]);
    } finally {
      setLoading(false);
    }
  }, [params.page, params.limit, params.search, params.department, params.status, params.tabId]);

  useEffect(() => {
    fetchEmployees();
  }, [fetchEmployees]);

  const createEmployee = async (employee: CreateEmployeeRequest) => {
    try {
      const response = await apiService.createEmployee(employee);
      if (response.success && response.data) {
        setEmployees(prev => [response.data!, ...prev]);
      } else {
        throw new Error('Failed to create employee');
      }
    } catch (err: any) {
      throw new Error(err.message || 'Failed to create employee');
    }
  };

  const updateEmployee = async (id: string, employee: Partial<Employee>) => {
    try {
      const response = await apiService.updateEmployee(id, employee);
      if (response.success && response.data) {
        setEmployees(prev => prev.map(emp => 
          emp.id === id ? response.data! : emp
        ));
      } else {
        throw new Error('Failed to update employee');
      }
    } catch (err: any) {
      throw new Error(err.message || 'Failed to update employee');
    }
  };

  const deleteEmployee = async (id: string) => {
    try {
      const response = await apiService.deleteEmployee(id);
      if (response.success) {
        setEmployees(prev => prev.filter(emp => emp.id !== id));
      } else {
        throw new Error('Failed to delete employee');
      }
    } catch (err: any) {
      throw new Error(err.message || 'Failed to delete employee');
    }
  };

  return {
    employees,
    loading,
    error,
    pagination,
    refetch: fetchEmployees,
    createEmployee,
    updateEmployee,
    deleteEmployee,
  };
};