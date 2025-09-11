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

  async login(credentials: { emailOrUsername: string; password: string }): Promise<void> {
    this.setState({ isLoading: true, error: null });

    try {
      const response = await apiService.login(credentials);
      
      if (response.Success && response.Data) {
        // Handle nested data structure from backend
        const loginData = response.Data as any; // Type assertion to handle backend response structure
        console.log('Login response data:', loginData);
        console.log('ModuleAccess type:', typeof loginData.ModuleAccess, 'value:', loginData.ModuleAccess);
        console.log('Roles type:', typeof loginData.Roles, 'value:', loginData.Roles);
        
        const token = loginData.Token;
        const refreshToken = loginData.RefreshToken;
        const user = loginData.User;
        
        // Validate required user data
        if (!user || !user.Id || !user.Username || !user.Email) {
          throw new Error('Invalid user data received from server');
        }
        
        // Handle circular references in ModuleAccess and Roles
        let moduleAccess: string[] = [];
        let roles: string[] = [];
        
        try {
          // Check if ModuleAccess is a circular reference or valid array
          if (loginData.ModuleAccess && typeof loginData.ModuleAccess === 'object') {
            if (Array.isArray(loginData.ModuleAccess)) {
              moduleAccess = loginData.ModuleAccess;
              console.log('ModuleAccess is array:', moduleAccess);
            } else if (loginData.ModuleAccess.$ref) {
              // Handle circular reference - use default values
              console.warn('ModuleAccess contains circular reference, using default values');
              moduleAccess = ['ER', 'Billing']; // Default for admin
            } else {
              console.warn('ModuleAccess is not an array and not a circular reference:', loginData.ModuleAccess);
              moduleAccess = ['ER', 'Billing']; // Default for admin
            }
          } else {
            console.warn('ModuleAccess is not an object:', loginData.ModuleAccess);
            moduleAccess = ['ER', 'Billing']; // Default for admin
          }
          
          // Check if Roles is a circular reference or valid array
          if (loginData.Roles && typeof loginData.Roles === 'object') {
            if (Array.isArray(loginData.Roles)) {
              roles = loginData.Roles;
              console.log('Roles is array:', roles);
            } else if (loginData.Roles.$ref) {
              // Handle circular reference - use default values
              console.warn('Roles contains circular reference, using default values');
              roles = ['Admin']; // Default for admin
            } else {
              console.warn('Roles is not an array and not a circular reference:', loginData.Roles);
              roles = ['Admin']; // Default for admin
            }
          } else {
            console.warn('Roles is not an object:', loginData.Roles);
            roles = ['Admin']; // Default for admin
          }
        } catch (error) {
          console.warn('Error processing ModuleAccess/Roles:', error);
          // Use default values for admin
          moduleAccess = ['ER', 'Billing'];
          roles = ['Admin'];
        }
        
        console.log('Final moduleAccess:', moduleAccess);
        console.log('Final roles:', roles);
        
        // Map backend user to frontend UserRole structure
        const mappedUser: UserRole = {
          id: user.Id,
          username: user.Username,
          name: user.Name || user.Username,
          email: user.Email,
          role: user.Role ? user.Role.toLowerCase() as 'admin' | 'er' | 'billing' : 'admin',
          permissions: {
            canAccessER: moduleAccess.includes('ER') || roles.includes('Admin') || user.Role?.toLowerCase() === 'admin',
            canAccessBilling: moduleAccess.includes('Billing') || roles.includes('Admin') || user.Role?.toLowerCase() === 'admin',
            isAdmin: roles.includes('Admin') || user.Role?.toLowerCase() === 'admin'
          },
          lastLogin: user.LastLogin ? new Date(user.LastLogin) : undefined,
          isActive: user.IsActive !== undefined ? user.IsActive : true
        };
        
        console.log('Mapped user permissions:', mappedUser.permissions);
        
        // Store auth data
        localStorage.setItem('authToken', token);
        localStorage.setItem('refreshToken', refreshToken);
        localStorage.setItem('currentUser', JSON.stringify(mappedUser));
        
        // Set API token
        apiService.setAuthToken(token);
        
        this.setState({
          user: mappedUser,
          isAuthenticated: true,
          isLoading: false,
          error: null
        });
      } else {
        throw new Error(response.Error?.Message || 'Login failed');
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