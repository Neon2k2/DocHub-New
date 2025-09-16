import React, { useState } from 'react';
import { Moon, Sun, Bell, User, LogOut, Settings, Shield, Briefcase, Menu } from 'lucide-react';
import { Button } from './ui/button';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuSeparator, DropdownMenuTrigger } from './ui/dropdown-menu';
import { Badge } from './ui/badge';
import { UserRole } from './Login';
import { useAuth } from '../contexts/AuthContext';
import { useTheme } from '../contexts/ThemeContext';
import { useResponsive } from '../utils/responsive';
import { NotificationDropdown } from './NotificationDropdown';

interface HeaderProps {
  activeModule: 'er' | 'billing';
  onModuleChange: (module: 'er' | 'billing') => void;
  currentUser: UserRole;
  onNavigateToWelcome: () => void;
}

export function Header({ activeModule, onModuleChange, currentUser, onNavigateToWelcome }: HeaderProps) {
  const { logout } = useAuth();
  const { isDarkMode, toggleTheme } = useTheme();
  const { isMobile } = useResponsive();
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const getRoleIcon = () => {
    switch (currentUser.role) {
      case 'admin':
        return <Shield className="h-4 w-4" />;
      case 'er':
        return <User className="h-4 w-4" />;
      case 'billing':
        return <Briefcase className="h-4 w-4" />;
      default:
        return <User className="h-4 w-4" />;
    }
  };
  return (
    <header className="fixed top-0 left-0 right-0 z-50 h-16 glass-panel border-b">
      <div className="flex items-center justify-between h-full px-4 md:px-6">
        {/* Logo */}
        <div className="flex items-center space-x-2 md:space-x-4">
          <button 
            onClick={onNavigateToWelcome}
            className="flex items-center space-x-2 hover:opacity-80 transition-opacity"
          >
            <div className="w-8 h-8 gradient-bg rounded-lg flex items-center justify-center">
              <span className="text-white font-bold">D</span>
            </div>
            <h1 className="text-lg md:text-xl font-bold text-blue-600">DocHub</h1>
          </button>
        </div>

        {/* Module Switcher - Only show if user has access to multiple modules */}
        {currentUser.permissions.canAccessER && currentUser.permissions.canAccessBilling && !isMobile && (
          <div className="flex items-center space-x-1 glass-panel rounded-lg p-1">
            <Button
              variant={activeModule === 'er' ? 'default' : 'ghost'}
              size="sm"
              onClick={() => onModuleChange('er')}
              className={`relative transition-all duration-300 ${
                activeModule === 'er' 
                  ? 'neon-border-blue bg-blue-50 text-blue-700 dark:bg-blue-950 dark:text-blue-300 hover:bg-blue-100 dark:hover:bg-blue-900' 
                  : 'hover:bg-muted'
              }`}
            >
              Employee Relations
            </Button>
            <Button
              variant={activeModule === 'billing' ? 'default' : 'ghost'}
              size="sm"
              onClick={() => onModuleChange('billing')}
              className={`relative transition-all duration-300 ${
                activeModule === 'billing' 
                  ? 'neon-border-blue bg-blue-50 text-blue-700 dark:bg-blue-950 dark:text-blue-300 hover:bg-blue-100 dark:hover:bg-blue-900' 
                  : 'hover:bg-muted'
              }`}
            >
              Billing & Timesheet
            </Button>
          </div>
        )}
        
        {/* Single Module Label - Show when user only has access to one module */}
        {!(currentUser.permissions.canAccessER && currentUser.permissions.canAccessBilling) && !isMobile && (
          <div className="flex items-center space-x-2 glass-panel rounded-lg px-4 py-2">
            <div className="flex items-center gap-2">
              {getRoleIcon()}
              <span className="font-medium">
                {currentUser.permissions.canAccessER ? 'Employee Relations' : 'Billing & Timesheet'}
              </span>
            </div>
          </div>
        )}

        {/* Right Section */}
        <div className="flex items-center space-x-2 md:space-x-4">
          {/* Theme Toggle */}
          <Button
            variant="ghost"
            size="sm"
            onClick={toggleTheme}
            className="hover:text-blue-600"
          >
            {isDarkMode ? <Sun className="h-4 w-4" /> : <Moon className="h-4 w-4" />}
          </Button>

          {/* Notifications */}
          <NotificationDropdown />

          {/* Profile Dropdown */}
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="sm" className="hover:text-blue-600">
                {getRoleIcon()}
                <span className="ml-2 hidden md:inline">{currentUser.name}</span>
                {currentUser.permissions.isAdmin && (
                  <Badge className="ml-2 bg-red-500/20 text-red-400 border-red-500/30 text-xs">
                    Admin
                  </Badge>
                )}
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="glass-panel border-glass-border">
              <div className="px-2 py-1.5 text-sm text-muted-foreground">
                Signed in as <span className="font-medium text-foreground">{currentUser.username}</span>
              </div>
              <DropdownMenuSeparator />
              <DropdownMenuItem className="hover:bg-muted">
                <User className="mr-2 h-4 w-4" />
                Profile
              </DropdownMenuItem>
              <DropdownMenuItem className="hover:bg-muted">
                <Settings className="mr-2 h-4 w-4" />
                Settings
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuItem 
                className="hover:bg-destructive hover:text-destructive-foreground"
                onClick={logout}
              >
                <LogOut className="mr-2 h-4 w-4" />
                Logout
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </div>
    </header>
  );
}