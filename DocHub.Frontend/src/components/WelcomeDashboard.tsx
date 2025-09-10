import React from 'react';
import { Shield, User, Briefcase, Clock, Activity, TrendingUp } from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from './ui/card';
import { UserInfoCard } from './UserInfoCard';
import { UserRole } from './Login';
import { useAuth } from '../contexts/AuthContext';

interface WelcomeDashboardProps {
  onNavigate: (module: 'er' | 'billing', page: string) => void;
}

export function WelcomeDashboard({ onNavigate }: WelcomeDashboardProps) {
  const { user: currentUser } = useAuth();
  
  if (!currentUser) return null;
  const getWelcomeMessage = () => {
    const hour = new Date().getHours();
    const greeting = hour < 12 ? 'Good morning' : hour < 18 ? 'Good afternoon' : 'Good evening';
    
    return `${greeting}, ${currentUser.name}!`;
  };

  const getQuickActions = () => {
    const actions = [];
    
    if (currentUser.permissions.canAccessER) {
      actions.push({
        title: 'Employee Relations',
        description: 'Generate and manage employee letters',
        icon: <User className="h-6 w-6 text-blue-400" />,
        onClick: () => onNavigate('er', 'dashboard'),
        color: 'bg-blue-500/20 border-blue-500/30 hover:bg-blue-500/30'
      });
    }
    
    if (currentUser.permissions.canAccessBilling) {
      actions.push({
        title: 'Billing & Timesheet',
        description: 'Process timesheets and manage billing',
        icon: <Briefcase className="h-6 w-6 text-green-400" />,
        onClick: () => onNavigate('billing', 'dashboard'),
        color: 'bg-green-500/20 border-green-500/30 hover:bg-green-500/30'
      });
    }
    
    return actions;
  };

  const getRoleSpecificStats = () => {
    if (currentUser.permissions.isAdmin) {
      return [
        { label: 'Total Users', value: '24', icon: <User className="h-4 w-4" /> },
        { label: 'System Uptime', value: '99.9%', icon: <Activity className="h-4 w-4" /> },
        { label: 'Storage Used', value: '67%', icon: <TrendingUp className="h-4 w-4" /> },
        { label: 'Active Sessions', value: '12', icon: <Clock className="h-4 w-4" /> }
      ];
    } else if (currentUser.permissions.canAccessER) {
      return [
        { label: 'Letters Generated', value: '156', icon: <User className="h-4 w-4" /> },
        { label: 'Pending Requests', value: '8', icon: <Clock className="h-4 w-4" /> },
        { label: 'This Month', value: '42', icon: <TrendingUp className="h-4 w-4" /> },
        { label: 'Success Rate', value: '98%', icon: <Activity className="h-4 w-4" /> }
      ];
    } else {
      return [
        { label: 'Timesheets Processed', value: '234', icon: <Briefcase className="h-4 w-4" /> },
        { label: 'Pending Downloads', value: '5', icon: <Clock className="h-4 w-4" /> },
        { label: 'This Month', value: '67', icon: <TrendingUp className="h-4 w-4" /> },
        { label: 'Success Rate', value: '96%', icon: <Activity className="h-4 w-4" /> }
      ];
    }
  };

  return (
    <div className="space-y-6">
      {/* Welcome Header */}
      <div>
        <h1 className="text-3xl font-bold">{getWelcomeMessage()}</h1>
        <p className="text-muted-foreground mt-2">
          Welcome to DocHub. {currentUser.permissions.isAdmin 
            ? 'You have full administrative access to the system.' 
            : 'Here\'s what you can do with your current permissions.'
          }
        </p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* User Info Card */}
        <div className="lg:col-span-1">
          <UserInfoCard currentUser={currentUser} />
        </div>

        {/* Quick Stats */}
        <div className="lg:col-span-2">
          <Card className="glass-panel border-glass-border">
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Activity className="h-5 w-5 text-neon-blue" />
                Quick Overview
              </CardTitle>
              <CardDescription>
                {currentUser.permissions.isAdmin 
                  ? 'System-wide statistics and metrics'
                  : 'Your activity and performance metrics'
                }
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                {getRoleSpecificStats().map((stat, index) => (
                  <div key={index} className="text-center p-4 glass-panel rounded-lg border-glass-border">
                    <div className="flex justify-center mb-2 text-neon-blue">
                      {stat.icon}
                    </div>
                    <div className="text-2xl font-bold">{stat.value}</div>
                    <div className="text-sm text-muted-foreground">{stat.label}</div>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        </div>
      </div>

      {/* Quick Actions */}
      <Card className="glass-panel border-glass-border">
        <CardHeader>
          <CardTitle>Quick Actions</CardTitle>
          <CardDescription>
            Access the modules you have permissions for
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {getQuickActions().map((action, index) => (
              <div
                key={index}
                className={`p-6 rounded-lg border cursor-pointer transition-all duration-300 ${action.color}`}
                onClick={action.onClick}
              >
                <div className="flex items-start gap-4">
                  <div className="p-2 glass-panel rounded-lg border-glass-border">
                    {action.icon}
                  </div>
                  <div>
                    <h3 className="font-semibold mb-1">{action.title}</h3>
                    <p className="text-sm text-muted-foreground">{action.description}</p>
                  </div>
                </div>
              </div>
            ))}
            
            {/* Show restricted access message if user doesn't have full access */}
            {(!currentUser.permissions.canAccessER || !currentUser.permissions.canAccessBilling) && (
              <div className="p-6 rounded-lg border border-muted bg-muted/10 opacity-50">
                <div className="flex items-start gap-4">
                  <div className="p-2 glass-panel rounded-lg border-glass-border">
                    <Shield className="h-6 w-6 text-muted-foreground" />
                  </div>
                  <div>
                    <h3 className="font-semibold mb-1 text-muted-foreground">
                      {currentUser.permissions.canAccessER ? 'Billing & Timesheet' : 'Employee Relations'}
                    </h3>
                    <p className="text-sm text-muted-foreground">
                      Access restricted - Contact administrator for permissions
                    </p>
                  </div>
                </div>
              </div>
            )}
          </div>
        </CardContent>
      </Card>

      {/* Admin-specific sections */}
      {currentUser.permissions.isAdmin && (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          <Card className="glass-panel border-glass-border">
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Shield className="h-5 w-5 text-red-400" />
                System Health
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <div className="flex justify-between items-center">
                <span className="text-sm">Database Status</span>
                <span className="text-green-400 text-sm">Healthy</span>
              </div>
              <div className="flex justify-between items-center">
                <span className="text-sm">API Response Time</span>
                <span className="text-neon-blue text-sm">125ms</span>
              </div>
              <div className="flex justify-between items-center">
                <span className="text-sm">Active Users</span>
                <span className="text-neon-green text-sm">12</span>
              </div>
              <div className="flex justify-between items-center">
                <span className="text-sm">Error Rate</span>
                <span className="text-yellow-400 text-sm">0.1%</span>
              </div>
            </CardContent>
          </Card>

          <Card className="glass-panel border-glass-border">
            <CardHeader>
              <CardTitle>Recent Activity</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <div className="text-sm">
                <span className="text-blue-400">HR Manager</span> generated document
                <div className="text-muted-foreground text-xs">2 minutes ago</div>
              </div>
              <div className="text-sm">
                <span className="text-green-400">Finance Manager</span> processed timesheet batch
                <div className="text-muted-foreground text-xs">15 minutes ago</div>
              </div>
              <div className="text-sm">
                <span className="text-neon-blue">System</span> completed automated backup
                <div className="text-muted-foreground text-xs">1 hour ago</div>
              </div>
            </CardContent>
          </Card>
        </div>
      )}
    </div>
  );
}