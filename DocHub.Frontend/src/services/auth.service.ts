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
    console.log('🔐 [AUTH-SERVICE] Starting login process');
    console.log('📋 [AUTH-SERVICE] Credentials:', { 
      emailOrUsername: credentials.emailOrUsername, 
      password: '***' 
    });
    
    this.setState({ isLoading: true, error: null });
    console.log('🔄 [AUTH-SERVICE] Set loading state to true');

    try {
      console.log('🔄 [AUTH-SERVICE] Calling API service login...');
      const response = await apiService.login(credentials);
      console.log('📊 [AUTH-SERVICE] API response received:', response);
      
      if ((response.Success || response.success) && (response.Data || response.data)) {
        console.log('✅ [AUTH-SERVICE] Login successful, processing response...');
        // Handle nested data structure from backend
        const loginData = (response.Data || response.data) as any; // Type assertion to handle backend response structure
        
        const token = loginData.token || loginData.Token;
        const refreshToken = loginData.refreshToken || loginData.RefreshToken;
        const user = loginData.user || loginData.User;
        
        // Validate required user data
        if (!user || !(user.id || user.Id) || !(user.username || user.Username) || !(user.email || user.Email)) {
          throw new Error('Invalid user data received from server');
        }
        
        // Handle circular references in ModuleAccess and Roles
        let moduleAccess: string[] = [];
        let roles: string[] = [];
        
        try {
          // Check if moduleAccess is a circular reference or valid array (try both cases)
          const moduleAccessData = loginData.moduleAccess || loginData.ModuleAccess;
          if (moduleAccessData && typeof moduleAccessData === 'object') {
            if (Array.isArray(moduleAccessData)) {
              moduleAccess = moduleAccessData;
            } else if (moduleAccessData.$ref) {
              // Handle circular reference - use default values
              console.warn('ModuleAccess contains circular reference, using default values');
              moduleAccess = ['ER', 'Billing']; // Default for admin
            } else {
              console.warn('ModuleAccess is not an array and not a circular reference:', moduleAccessData);
              moduleAccess = ['ER', 'Billing']; // Default for admin
            }
          } else {
            console.warn('ModuleAccess is not an object:', moduleAccessData);
            moduleAccess = ['ER', 'Billing']; // Default for admin
          }
          
          // Check if Roles is a circular reference or valid array (try both cases)
          const rolesData = loginData.roles || loginData.Roles;
          if (rolesData && typeof rolesData === 'object') {
            if (Array.isArray(rolesData)) {
              roles = rolesData;
            } else if (rolesData.$ref) {
              // Handle circular reference - use default values
              console.warn('Roles contains circular reference, using default values');
              roles = ['Admin']; // Default for admin
            } else {
              console.warn('Roles is not an array and not a circular reference:', rolesData);
              roles = ['Admin']; // Default for admin
            }
          } else {
            console.warn('Roles is not an object:', rolesData);
            roles = ['Admin']; // Default for admin
          }
        } catch (error) {
          console.warn('Error processing ModuleAccess/Roles:', error);
          // Use default values for admin
          moduleAccess = ['ER', 'Billing'];
          roles = ['Admin'];
        }
        
        
        // Map backend user to frontend UserRole structure
        const mappedUser: UserRole = {
          id: user.id || user.Id,
          username: user.username || user.Username,
          name: user.name || user.Name || `${user.firstName || user.FirstName || ''} ${user.lastName || user.LastName || ''}`.trim() || user.username || user.Username,
          email: user.email || user.Email,
          role: (user.role || user.Role)?.toLowerCase() as 'admin' | 'er' | 'billing' || 'admin',
          permissions: {
            canAccessER: moduleAccess.includes('ER') || roles.includes('Admin') || moduleAccess.includes('Admin'),
            canAccessBilling: moduleAccess.includes('Billing') || roles.includes('Admin') || moduleAccess.includes('Admin'),
            isAdmin: roles.includes('Admin') || moduleAccess.includes('Admin') || (user.role || user.Role)?.toLowerCase() === 'admin'
          },
          lastLogin: (user.lastLogin || user.LastLogin) ? new Date(user.lastLogin || user.LastLogin) : undefined,
          isActive: user.isActive !== undefined ? user.isActive : (user.IsActive !== undefined ? user.IsActive : true)
        };
        
        
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

        // Update browser URL to reflect successful login
        if (window.location.pathname === '/login' || window.location.pathname === '/') {
          window.history.pushState({}, '', '/dashboard');
        }
      } else {
        const errorMessage = response.Error?.Message || response.error?.message || 'Login failed';
        throw new Error(errorMessage);
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

    // Update browser URL to reflect logout
    if (window.location.pathname !== '/login') {
      window.history.pushState({}, '', '/login');
    }
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