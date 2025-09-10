import React from 'react';
import { User, Shield, Briefcase, CheckCircle, XCircle } from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from './ui/card';
import { Badge } from './ui/badge';
import { UserRole } from './Login';

interface UserInfoCardProps {
  currentUser: UserRole;
}

export function UserInfoCard({ currentUser }: UserInfoCardProps) {
  const getRoleIcon = () => {
    switch (currentUser.role) {
      case 'admin':
        return <Shield className="h-5 w-5 text-red-400" />;
      case 'er':
        return <User className="h-5 w-5 text-blue-400" />;
      case 'billing':
        return <Briefcase className="h-5 w-5 text-green-400" />;
      default:
        return <User className="h-5 w-5" />;
    }
  };

  const getRoleBadge = () => {
    switch (currentUser.role) {
      case 'admin':
        return <Badge className="bg-red-500/20 text-red-400 border-red-500/30">Administrator</Badge>;
      case 'er':
        return <Badge className="bg-blue-500/20 text-blue-400 border-blue-500/30">HR Manager</Badge>;
      case 'billing':
        return <Badge className="bg-green-500/20 text-green-400 border-green-500/30">Finance Manager</Badge>;
      default:
        return <Badge variant="secondary">{currentUser.role}</Badge>;
    }
  };

  return (
    <Card className="glass-panel border-glass-border">
      <CardHeader className="pb-4">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="w-12 h-12 bg-muted rounded-full flex items-center justify-center">
              {getRoleIcon()}
            </div>
            <div>
              <CardTitle className="text-lg">{currentUser.name}</CardTitle>
              <CardDescription>@{currentUser.username}</CardDescription>
            </div>
          </div>
          {getRoleBadge()}
        </div>
      </CardHeader>
      <CardContent>
        <div className="space-y-3">
          <div className="text-sm font-medium text-muted-foreground mb-2">
            Module Access Permissions
          </div>
          
          <div className="space-y-2">
            <div className="flex items-center justify-between">
              <span className="text-sm">Employee Relations</span>
              {currentUser.permissions.canAccessER ? (
                <div className="flex items-center gap-1 text-green-400">
                  <CheckCircle className="h-4 w-4" />
                  <span className="text-xs">Granted</span>
                </div>
              ) : (
                <div className="flex items-center gap-1 text-red-400">
                  <XCircle className="h-4 w-4" />
                  <span className="text-xs">Denied</span>
                </div>
              )}
            </div>
            
            <div className="flex items-center justify-between">
              <span className="text-sm">Billing & Timesheet</span>
              {currentUser.permissions.canAccessBilling ? (
                <div className="flex items-center gap-1 text-green-400">
                  <CheckCircle className="h-4 w-4" />
                  <span className="text-xs">Granted</span>
                </div>
              ) : (
                <div className="flex items-center gap-1 text-red-400">
                  <XCircle className="h-4 w-4" />
                  <span className="text-xs">Denied</span>
                </div>
              )}
            </div>
            
            <div className="flex items-center justify-between">
              <span className="text-sm">Admin Functions</span>
              {currentUser.permissions.isAdmin ? (
                <div className="flex items-center gap-1 text-green-400">
                  <CheckCircle className="h-4 w-4" />
                  <span className="text-xs">Granted</span>
                </div>
              ) : (
                <div className="flex items-center gap-1 text-red-400">
                  <XCircle className="h-4 w-4" />
                  <span className="text-xs">Denied</span>
                </div>
              )}
            </div>
          </div>

          {currentUser.permissions.isAdmin && (
            <div className="mt-4 p-2 bg-red-500/10 border border-red-500/20 rounded-md">
              <div className="flex items-center gap-2 text-red-400">
                <Shield className="h-4 w-4" />
                <span className="text-xs font-medium">Admin Privileges Active</span>
              </div>
              <div className="text-xs text-muted-foreground mt-1">
                Full system access and user management
              </div>
            </div>
          )}
        </div>
      </CardContent>
    </Card>
  );
}