import React, { useState } from 'react';
import { LogIn, Loader2 } from 'lucide-react';
import { Button } from './ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from './ui/card';
import { Input } from './ui/input';
import { Label } from './ui/label';
import { useAuth } from '../contexts/AuthContext';

export interface UserRole {
  id: string;
  username: string;
  name: string;
  email: string;
  role: 'admin' | 'er' | 'billing';
  permissions: {
    canAccessER: boolean;
    canAccessBilling: boolean;
    isAdmin: boolean;
  };
  lastLogin?: Date;
  isActive: boolean;
}

export function Login() {
  const [emailOrUsername, setEmailOrUsername] = useState('');
  const [password, setPassword] = useState('');
  const [localError, setLocalError] = useState('');
  const { login, isLoading, error } = useAuth();

  const handleLogin = async () => {
    if (!emailOrUsername || !password) {
      setLocalError('Please enter both email/username and password');
      return;
    }

    setLocalError('');
    
    try {
      await login({ emailOrUsername, password });
    } catch (err: any) {
      setLocalError(err.message || 'Login failed');
    }
  };

  const displayError = localError || error;

  return (
    <div className="min-h-screen bg-background flex items-center justify-center p-4">
      <div className="w-full max-w-md space-y-8">
        {/* Header */}
        <div className="text-center space-y-4">
          <div className="flex items-center justify-center space-x-2">
            <div className="w-12 h-12 gradient-bg rounded-lg flex items-center justify-center">
              <span className="text-white text-xl font-bold">D</span>
            </div>
            <h1 className="text-3xl font-bold text-blue-600">DocHub</h1>
          </div>
          <p className="text-muted-foreground">
            Welcome to DocHub - Your Document Management System
          </p>
        </div>

        {/* Login Form */}
        <Card className="glass-panel border-glass-border">
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <LogIn className="h-5 w-5 text-blue-600" />
              Login
            </CardTitle>
            <CardDescription>
              Enter your credentials to access the system
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="emailOrUsername">Email or Username</Label>
              <Input
                id="emailOrUsername"
                placeholder="Enter your email or username"
                value={emailOrUsername}
                onChange={(e) => setEmailOrUsername(e.target.value)}
                className="glass-panel border-glass-border"
                disabled={isLoading}
              />
            </div>
            
            <div className="space-y-2">
              <Label htmlFor="password">Password</Label>
              <Input
                id="password"
                type="password"
                placeholder="Enter your password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                className="glass-panel border-glass-border"
                onKeyPress={(e) => e.key === 'Enter' && !isLoading && handleLogin()}
                disabled={isLoading}
              />
            </div>

            {displayError && (
              <div className="text-sm text-red-400 bg-red-500/10 border border-red-500/20 rounded-md p-3">
                {displayError}
              </div>
            )}

            <Button 
              onClick={handleLogin}
              disabled={isLoading}
              className="w-full neon-border-blue bg-blue-600 hover:bg-blue-700 text-black dark:text-white transition-all duration-300 disabled:opacity-50"
            >
              {isLoading ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  Logging in...
                </>
              ) : (
                <>
                  <LogIn className="mr-2 h-4 w-4" />
                  Login
                </>
              )}
            </Button>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}