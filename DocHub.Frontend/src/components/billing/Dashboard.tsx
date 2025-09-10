import React, { useState, useEffect } from 'react';
import { Download, Merge, Mail, AlertTriangle, Calendar, Clock, Users, FileText } from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../ui/card';
import { Badge } from '../ui/badge';
import { Progress } from '../ui/progress';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '../ui/table';
import { Loading } from '../ui/loading';
import { apiService, DashboardStats } from '../../services/api.service';
import { notify } from '../../utils/notifications';
import { handleError } from '../../utils/errorHandler';

interface ActivityLog {
  id: string;
  action: string;
  employee: string;
  timestamp: string;
  status: 'success' | 'error' | 'warning';
}

export function BillingDashboard() {
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadDashboardStats();
  }, []);

  const loadDashboardStats = async () => {
    setLoading(true);
    try {
      const response = await apiService.getDashboardStats('billing');
      if (response.success && response.data) {
        setStats(response.data);
      }
    } catch (error) {
      handleError(error, 'Load billing dashboard stats');
    } finally {
      setLoading(false);
    }
  };

  const getStatusBadge = (status: string) => {
    switch (status) {
      case 'success':
        return <Badge className="bg-green-500/20 text-green-400 border-green-500/30">Success</Badge>;
      case 'error':
        return <Badge className="bg-red-500/20 text-red-400 border-red-500/30">Error</Badge>;
      case 'warning':
        return <Badge className="bg-yellow-500/20 text-yellow-400 border-yellow-500/30">Warning</Badge>;
      default:
        return <Badge variant="secondary">{status}</Badge>;
    }
  };

  if (loading) {
    return (
      <div className="flex justify-center items-center h-64">
        <Loading />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Quick Stats */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        <Card className="glass-panel border-glass-border">
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total Projects</CardTitle>
            <Download className="h-4 w-4 text-neon-blue" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold neon-text">{stats?.totalProjects || 0}</div>
            <p className="text-xs text-muted-foreground">Active projects</p>
          </CardContent>
        </Card>

        <Card className="glass-panel border-glass-border">
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total Hours</CardTitle>
            <Merge className="h-4 w-4 text-neon-green" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-green-400">{stats?.totalHoursThisMonth || 0}</div>
            <p className="text-xs text-muted-foreground">Hours this month</p>
          </CardContent>
        </Card>

        <Card className="glass-panel border-glass-border">
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Pending Timesheets</CardTitle>
            <Mail className="h-4 w-4 text-neon-purple" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-purple-400">{stats?.pendingTimesheets || 0}</div>
            <p className="text-xs text-muted-foreground">Awaiting approval</p>
          </CardContent>
        </Card>

        <Card className="glass-panel border-glass-border">
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total Revenue</CardTitle>
            <AlertTriangle className="h-4 w-4 text-red-400" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-red-400">${stats?.totalRevenue?.toLocaleString() || 0}</div>
            <p className="text-xs text-muted-foreground">Monthly revenue</p>
          </CardContent>
        </Card>
      </div>

      {/* Progress Cards */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Card className="glass-panel border-glass-border">
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Clock className="h-5 w-5 text-neon-blue" />
              Current Processing
            </CardTitle>
            <CardDescription>Active batch operations in progress</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="space-y-2">
              <div className="flex justify-between text-sm">
                <span>HCL Timesheet Download</span>
                <span>75%</span>
              </div>
              <Progress value={75} className="h-2" />
            </div>
            <div className="space-y-2">
              <div className="flex justify-between text-sm">
                <span>Email Batch Processing</span>
                <span>45%</span>
              </div>
              <Progress value={45} className="h-2" />
            </div>
            <div className="space-y-2">
              <div className="flex justify-between text-sm">
                <span>Sheet Merger Queue</span>
                <span>90%</span>
              </div>
              <Progress value={90} className="h-2" />
            </div>
          </CardContent>
        </Card>

        <Card className="glass-panel border-glass-border">
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Users className="h-5 w-5 text-neon-green" />
              Employee Summary
            </CardTitle>
            <CardDescription>Quick overview of employee data</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="flex justify-between items-center">
              <span className="text-sm">HCL Employees</span>
              <Badge className="bg-blue-500/20 text-blue-400 border-blue-500/30">847</Badge>
            </div>
            <div className="flex justify-between items-center">
              <span className="text-sm">Infosys Employees</span>
              <Badge className="bg-green-500/20 text-green-400 border-green-500/30">623</Badge>
            </div>
            <div className="flex justify-between items-center">
              <span className="text-sm">Pending Timesheets</span>
              <Badge className="bg-yellow-500/20 text-yellow-400 border-yellow-500/30">12</Badge>
            </div>
            <div className="flex justify-between items-center">
              <span className="text-sm">Failed Processes</span>
              <Badge className="bg-red-500/20 text-red-400 border-red-500/30">3</Badge>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Recent Activity */}
      <Card className="glass-panel border-glass-border">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <FileText className="h-5 w-5 text-neon-purple" />
            Recent Activity
          </CardTitle>
          <CardDescription>Latest 10 actions performed in the system</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="glass-panel rounded-lg border-glass-border overflow-hidden">
            <Table>
              <TableHeader>
                <TableRow className="border-glass-border hover:bg-muted/50">
                  <TableHead>Action</TableHead>
                  <TableHead>Employee</TableHead>
                  <TableHead>Timestamp</TableHead>
                  <TableHead>Status</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {stats?.recentActivities?.map((activity) => (
                  <TableRow 
                    key={activity.id} 
                    className="border-glass-border hover:bg-muted/50 transition-colors"
                  >
                    <TableCell className="font-medium">{activity.type.replace('_', ' ').toUpperCase()}</TableCell>
                    <TableCell>{activity.employeeName}</TableCell>
                    <TableCell>{activity.createdAt.toLocaleString()}</TableCell>
                    <TableCell>{getStatusBadge(activity.status)}</TableCell>
                  </TableRow>
                )) || (
                  <TableRow>
                    <TableCell colSpan={4} className="text-center text-muted-foreground py-8">
                      No recent activity
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}