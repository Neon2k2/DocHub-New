// Authentication Service for DocHub
import { apiService } from './api.service';
import { UserRole } from '../components/Login';

export interface AuthState {
  user: UserRole | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
}

class AuthService {
  private listeners: ((state: AuthState) => void)[] = [];
  private state: AuthState = {
    user: null,
    isAuthenticated: false,
    isLoading: false,
    error: null
  };

  constructor() {
    this.initializeAuth();
  }

  private initializeAuth() {
    const token = localStorage.getItem('authToken');
    const userStr = localStorage.getItem('currentUser');
    
    if (token && userStr) {
      try {
        const user = JSON.parse(userStr);
        this.setState({
          user,
          isAuthenticated: true,
          isLoading: false,
          error: null
        });
        apiService.setAuthToken(token);
      } catch (error) {
        this.clearAuth();
      }
    }
  }

  private setState(newState: Partial<AuthState>) {
    this.state = { ...this.state, ...newState };
    this.listeners.forEach(listener => listener(this.state));
  }

  public getState(): AuthState {
    return this.state;
  }

  public subscribe(listener: (state: AuthState) => void): () => void {
    this.listeners.push(listener);
    return () => {
      this.listeners = this.listeners.filter(l => l !== listener);
    };
  }

  async login(credentials: { username: string; password: string }): Promise<void> {
    this.setState({ isLoading: true, error: null });

    try {
      const response = await apiService.login(credentials);
      
      if (response.success && response.data) {
        const { token, refreshToken, user } = response.data;
        
        // Store auth data
        localStorage.setItem('authToken', token);
        localStorage.setItem('refreshToken', refreshToken);
        localStorage.setItem('currentUser', JSON.stringify(user));
        
        // Set API token
        apiService.setAuthToken(token);
        
        this.setState({
          user,
          isAuthenticated: true,
          isLoading: false,
          error: null
        });
      } else {
        throw new Error(response.error?.message || 'Login failed');
      }
    } catch (error: any) {
      this.setState({
        isLoading: false,
        error: error.message || 'Login failed'
      });
      throw error;
    }
  }

  async logout(): Promise<void> {
    try {
      await apiService.logout();
    } catch (error) {
      console.warn('Logout API call failed:', error);
    } finally {
      this.clearAuth();
    }
  }

  private clearAuth() {
    localStorage.removeItem('authToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('currentUser');
    apiService.clearAuthToken();
    
    this.setState({
      user: null,
      isAuthenticated: false,
      isLoading: false,
      error: null
    });
  }

  hasPermission(permission: keyof UserRole['permissions']): boolean {
    return this.state.user?.permissions[permission] || false;
  }

  canAccessModule(module: 'er' | 'billing'): boolean {
    if (!this.state.user) return false;
    
    return module === 'er' 
      ? this.state.user.permissions.canAccessER 
      : this.state.user.permissions.canAccessBilling;
  }

  getCurrentUser(): UserRole | null {
    return this.state.user;
  }

  isAdmin(): boolean {
    return this.state.user?.permissions.isAdmin || false;
  }
}

// Export singleton instance
export const authService = new AuthService();
export default authService;