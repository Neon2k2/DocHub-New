import React, { useState, useEffect } from 'react';
import { Users, FileText, Clock, TrendingUp, User, CheckCircle, XCircle, AlertCircle, Plus } from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../ui/card';
import { Badge } from '../ui/badge';
import { Button } from '../ui/button';
import { EmailStatusTracker } from './EmailStatusTracker';
import { useDashboard } from '../../hooks/useDashboard';
import { useAuth } from '../../contexts/AuthContext';
import { tabService, DynamicTab } from '../../services/tab.service';
import { useDocumentRequests } from '../../hooks/useDocumentRequests';

export function ERDashboard() {
  const { stats, loading, error, refetch } = useDashboard('er');
  const { isAdmin } = useAuth();
  const [dynamicTabs, setDynamicTabs] = useState<DynamicTab[]>([]);
  const [tabsLoading, setTabsLoading] = useState(true);
  const [tabsError, setTabsError] = useState<string | null>(null);

  // Load dynamic tabs
  useEffect(() => {
    const loadTabs = async () => {
      try {
        setTabsLoading(true);
        setTabsError(null);
        const tabs = await tabService.getActiveTabs();
        setDynamicTabs(tabs);
      } catch (err) {
        setTabsError(err instanceof Error ? err.message : 'Failed to load tabs');
        setDynamicTabs([]);
      } finally {
        setTabsLoading(false);
      }
    };

    loadTabs();
  }, []);

  if (loading) {
    return (
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold">Employee Relations Dashboard</h1>
            <p className="text-muted-foreground">Overview of employee management activities</p>
          </div>
        </div>
        
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
          {Array.from({ length: 4 }).map((_, i) => (
            <Card key={i} className="glass-panel border-glass-border">
              <CardContent className="p-6">
                <div className="animate-pulse">
                  <div className="h-4 bg-muted rounded w-3/4 mb-2"></div>
                  <div className="h-8 bg-muted rounded w-1/2"></div>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold">Employee Relations Dashboard</h1>
            <p className="text-muted-foreground">Overview of employee management activities</p>
          </div>
          <Button onClick={refetch} variant="outline">
            Retry
          </Button>
        </div>
        
        <Card className="glass-panel border-glass-border">
          <CardContent className="p-6 text-center">
            <AlertCircle className="h-12 w-12 text-red-400 mx-auto mb-4" />
            <h3 className="text-lg font-semibold mb-2">Failed to load dashboard</h3>
            <p className="text-muted-foreground">{error}</p>
          </CardContent>
        </Card>
      </div>
    );
  }

  // Create dynamic stat cards from tabs
  const statCards = [
    {
      title: 'Total Employees',
      value: stats?.totalEmployees || 0,
      icon: <Users className="h-4 w-4" />,
      description: 'Active workforce',
      trend: '+2.5%',
      color: 'text-blue-400'
    },
    {
      title: 'Active Employees',
      value: stats?.activeEmployees || 0,
      icon: <User className="h-4 w-4" />,
      description: 'Currently employed',
      trend: '+1.2%',
      color: 'text-green-400'
    },
    // Dynamic tabs will be added here
    ...dynamicTabs.map((tab, index) => {
      const { requests } = useDocumentRequests(tab.letterType);
      const pendingCount = requests.filter(r => r.status === 'Pending').length;
      const approvedCount = requests.filter(r => r.status === 'Approved').length;
      
      return {
        title: tab.name,
        value: requests.length,
        icon: <FileText className="h-4 w-4" />,
        description: `${pendingCount} pending, ${approvedCount} approved`,
        trend: pendingCount > 0 ? `${pendingCount} pending` : 'All caught up',
        color: index % 2 === 0 ? 'text-orange-400' : 'text-purple-400',
        tabId: tab.id
      };
    })
  ];

  const monthlyStats = [
    {
      title: 'New Joinings',
      value: stats?.newJoiningsThisMonth || 0,
      icon: <TrendingUp className="h-4 w-4" />,
      color: 'text-green-400'
    },
    {
      title: 'Relieved',
      value: stats?.relievedThisMonth || 0,
      icon: <Clock className="h-4 w-4" />,
      color: 'text-red-400'
    }
  ];

  const getStatusBadge = (status: string) => {
    switch (status) {
      case 'pending':
        return <Badge variant="outline" className="text-orange-400 border-orange-500/30">Pending</Badge>;
      case 'approved':
        return <Badge variant="outline" className="text-green-400 border-green-500/30">Approved</Badge>;
      case 'rejected':
        return <Badge variant="outline" className="text-red-400 border-red-500/30">Rejected</Badge>;
      default:
        return <Badge variant="outline">{status}</Badge>;
    }
  };

  const getActivityIcon = (type: string) => {
    switch (type) {
      case 'document_request':
        return <FileText className="h-4 w-4" />;
      case 'transfer_letter':
        return <FileText className="h-4 w-4" />;
      case 'joining':
        return <User className="h-4 w-4" />;
      case 'relieving':
        return <XCircle className="h-4 w-4" />;
      default:
        return <CheckCircle className="h-4 w-4" />;
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Employee Relations Dashboard</h1>
          <p className="text-muted-foreground">Overview of employee management activities</p>
        </div>
        <Button onClick={refetch} variant="outline">
          Refresh
        </Button>
      </div>

      {/* Main Stats */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        {statCards.map((stat, index) => (
          <Card key={index} className="glass-panel border-glass-border">
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm text-muted-foreground">{stat.title}</p>
                  <p className="text-2xl font-bold">{stat.value}</p>
                </div>
                <div className={`p-2 glass-panel rounded-lg ${stat.color}`}>
                  {stat.icon}
                </div>
              </div>
              <div className="mt-4">
                <p className="text-xs text-muted-foreground">{stat.description}</p>
                <p className="text-xs text-green-400">{stat.trend}</p>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Monthly Statistics */}
        <Card className="glass-panel border-glass-border">
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <TrendingUp className="h-5 w-5 text-neon-blue" />
              This Month
            </CardTitle>
            <CardDescription>
              Employee movement statistics
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            {monthlyStats.map((stat, index) => (
              <div key={index} className="flex items-center justify-between">
                <div className="flex items-center gap-3">
                  <div className={`p-2 glass-panel rounded-lg ${stat.color}`}>
                    {stat.icon}
                  </div>
                  <span className="font-medium">{stat.title}</span>
                </div>
                <span className="text-2xl font-bold">{stat.value}</span>
              </div>
            ))}
          </CardContent>
        </Card>

        {/* Recent Activities */}
        <Card className="lg:col-span-2 glass-panel border-glass-border">
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Clock className="h-5 w-5 text-neon-green" />
              Recent Activities
            </CardTitle>
            <CardDescription>
              Latest employee-related activities
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {stats?.recentActivities?.length ? (
                stats.recentActivities.slice(0, 5).map((activity, index) => (
                  <div key={index} className="flex items-center justify-between p-3 glass-panel rounded-lg border-glass-border">
                    <div className="flex items-center gap-3">
                      <div className="text-neon-blue">
                        {getActivityIcon(activity.type)}
                      </div>
                      <div>
                        <p className="font-medium">{activity.employeeName}</p>
                        <p className="text-sm text-muted-foreground">
                          {activity.type.replace('_', ' ').toUpperCase()} - {activity.employeeId}
                        </p>
                      </div>
                    </div>
                    <div className="text-right">
                      {getStatusBadge(activity.status)}
                      <p className="text-xs text-muted-foreground mt-1">
                        {new Date(activity.createdAt).toLocaleDateString()}
                      </p>
                    </div>
                  </div>
                ))
              ) : (
                <div className="text-center py-8">
                  <Clock className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
                  <p className="text-muted-foreground">No recent activities</p>
                </div>
              )}
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Email Status Overview - Real-time */}
      <Card className="glass-panel border-glass-border">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <div className="w-3 h-3 bg-green-400 rounded-full animate-pulse" />
            Email Status Overview
          </CardTitle>
          <CardDescription>
            Real-time email delivery status for all sent documents
          </CardDescription>
        </CardHeader>
        <CardContent>
          <EmailStatusTracker showOnlyOwnEmails={!isAdmin()} />
        </CardContent>
      </Card>

      {/* Quick Actions */}
      <Card className="glass-panel border-glass-border">
        <CardHeader>
          <CardTitle>Quick Actions</CardTitle>
          <CardDescription>
            Common tasks and shortcuts
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            {dynamicTabs.length > 0 ? (
              dynamicTabs.slice(0, 2).map((tab, index) => {
                return (
                  <Button 
                    key={tab.id}
                    className={`h-16 neon-border-blue bg-blue-50 text-blue-700 hover:bg-blue-100 dark:bg-blue-950 dark:text-blue-300 dark:hover:bg-blue-900 transition-all duration-300`}
                  >
                    <FileText className="mr-2 h-5 w-5" />
                    Generate {tab.name}
                  </Button>
                );
              })
            ) : (
              <div className="col-span-2 text-center py-8">
                <FileText className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
                <p className="text-muted-foreground">No document types available</p>
                <p className="text-sm text-muted-foreground">Create dynamic tabs to see quick actions</p>
              </div>
            )}
            <Button 
              className="h-16 neon-border-purple bg-purple-50 text-purple-700 hover:bg-purple-100 dark:bg-purple-950 dark:text-purple-300 dark:hover:bg-purple-900 transition-all duration-300"
            >
              <Users className="mr-2 h-5 w-5" />
              Employee Management
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}