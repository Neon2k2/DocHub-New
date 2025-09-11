import React, { createContext, useContext, useEffect, useState, ReactNode, useMemo, useCallback } from 'react';
import { authService, AuthState } from '../services/auth.service';
import { UserRole } from '../components/Login';

interface AuthContextType extends AuthState {
  login: (credentials: { emailOrUsername: string; password: string }) => Promise<void>;
  logout: () => Promise<void>;
  hasPermission: (permission: keyof UserRole['permissions']) => boolean;
  canAccessModule: (module: 'er' | 'billing') => boolean;
  isAdmin: () => boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    // During hot reloading, the context might be temporarily unavailable
    // Return a default context to prevent crashes
    console.warn('useAuth called outside of AuthProvider. This might be due to hot reloading.');
    return {
      user: null,
      isAuthenticated: false,
      isLoading: true,
      error: null,
      login: async () => {
        console.warn('Login called outside of AuthProvider');
      },
      logout: async () => {
        console.warn('Logout called outside of AuthProvider');
      },
      hasPermission: () => false,
      canAccessModule: () => false,
      isAdmin: () => false,
    };
  }
  return context;
};

interface AuthProviderProps {
  children: ReactNode;
}

export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
  console.log('AuthProvider rendering');
  
  const [authState, setAuthState] = useState<AuthState>(() => {
    try {
      console.log('Initializing auth state');
      // Initialize with current auth state immediately
      const currentState = authService.getState();
      console.log('Current auth state:', currentState);
      return {
        ...currentState,
        isLoading: false // Auth service initializes synchronously
      };
    } catch (error) {
      console.error('Error initializing auth state:', error);
      return {
        user: null,
        isAuthenticated: false,
        isLoading: false,
        error: 'Failed to initialize authentication'
      };
    }
  });

  useEffect(() => {
    try {
      // Subscribe to auth state changes
      const unsubscribe = authService.subscribe(setAuthState);
      return unsubscribe;
    } catch (error) {
      console.error('Error subscribing to auth state changes:', error);
    }
  }, []);

  const login = useCallback(async (credentials: { emailOrUsername: string; password: string }) => {
    await authService.login(credentials);
  }, []);

  const logout = useCallback(async () => {
    await authService.logout();
  }, []);

  const hasPermission = useCallback((permission: keyof UserRole['permissions']) => {
    return authService.hasPermission(permission);
  }, []);

  const canAccessModule = useCallback((module: 'er' | 'billing') => {
    return authService.canAccessModule(module);
  }, []);

  const isAdmin = useCallback(() => {
    return authService.isAdmin();
  }, []);

  const value: AuthContextType = useMemo(() => ({
    ...authState,
    login,
    logout,
    hasPermission,
    canAccessModule,
    isAdmin,
  }), [authState, login, logout, hasPermission, canAccessModule, isAdmin]);

  return (
    <AuthContext.Provider value={value}>
      {children}
    </AuthContext.Provider>
  );
};