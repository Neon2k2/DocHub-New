import React, { useState, useEffect } from 'react';
import { 
  Users, 
  Activity, 
  Clock, 
  MapPin, 
  Monitor, 
  Smartphone, 
  Tablet,
  Trash2,
  AlertTriangle,
  RefreshCw,
  Eye,
  Shield,
  BarChart3
} from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../ui/card';
import { Button } from '../ui/button';
import { Badge } from '../ui/badge';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '../ui/table';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle, DialogTrigger } from '../ui/dialog';
import { Input } from '../ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../ui/select';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../ui/tabs';
import { Alert, AlertDescription } from '../ui/alert';
import { apiService } from '../../services/api.service';
import { cacheService } from '../../services/cache.service';

interface ActiveSession {
  sessionId: string;
  userId: string;
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  department: string;
  ipAddress: string;
  userAgent: string;
  loginAt: string;
  lastActivityAt: string;
  isActive: boolean;
  deviceInfo: string;
  location: string;
}

interface SessionStats {
  totalActiveSessions: number;
  totalSessionsToday: number;
  totalSessionsThisWeek: number;
  totalSessionsThisMonth: number;
  averageSessionDuration: string;
  longestActiveSession: string;
  sessionsByDepartment: Record<string, number>;
  sessionsByHour: Record<string, number>;
  uniqueUsersToday: number;
  uniqueUsersThisWeek: number;
  uniqueUsersThisMonth: number;
}

interface LoginHistory {
  id: string;
  userId: string;
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  department: string;
  ipAddress: string;
  userAgent: string;
  loginAt: string;
  logoutAt?: string;
  isSuccessful: boolean;
  failureReason?: string;
  deviceInfo: string;
  location: string;
  sessionDuration?: string;
}

export function SessionManagement() {
  const [activeSessions, setActiveSessions] = useState<ActiveSession[]>([]);
  const [sessionStats, setSessionStats] = useState<SessionStats | null>(null);
  const [loginHistory, setLoginHistory] = useState<LoginHistory[]>([]);
  const [loading, setLoading] = useState(false);
  const [selectedUserId, setSelectedUserId] = useState<string>('');
  const [showTerminateDialog, setShowTerminateDialog] = useState(false);
  const [selectedSessionId, setSelectedSessionId] = useState<string>('');
  const [filter, setFilter] = useState({
    department: 'all',
    status: 'all'
  });

  useEffect(() => {
    loadSessionData();
  }, []);

  const loadSessionData = async () => {
    setLoading(true);
    try {
      const [sessionsResponse, statsResponse] = await Promise.all([
        apiService.getActiveSessions(),
        apiService.getSessionStats()
      ]);

      if (sessionsResponse.success) {
        setActiveSessions(sessionsResponse.data || []);
      }
      if (statsResponse.success) {
        setSessionStats(statsResponse.data || null);
      }
    } catch (error) {
      console.error('Error loading session data:', error);
    } finally {
      setLoading(false);
    }
  };

  const loadLoginHistory = async (userId: string) => {
    try {
      const response = await apiService.getUserLoginHistory(userId);
      if (response.success) {
        setLoginHistory(response.data || []);
      }
    } catch (error) {
      console.error('Error loading login history:', error);
    }
  };

  const handleTerminateSession = async (sessionId: string) => {
    try {
      const response = await apiService.terminateSession(sessionId);
      if (response.success) {
        await loadSessionData();
        setShowTerminateDialog(false);
      }
    } catch (error) {
      console.error('Error terminating session:', error);
    }
  };

  const handleTerminateUserSessions = async (userId: string) => {
    try {
      const response = await apiService.terminateUserSessions(userId);
      if (response.success) {
        await loadSessionData();
      }
    } catch (error) {
      console.error('Error terminating user sessions:', error);
    }
  };

  const getDeviceIcon = (userAgent: string) => {
    if (userAgent.includes('Mobile')) return <Smartphone className="h-4 w-4" />;
    if (userAgent.includes('Tablet')) return <Tablet className="h-4 w-4" />;
    return <Monitor className="h-4 w-4" />;
  };

  const getStatusBadge = (isActive: boolean) => {
    return (
      <Badge className={isActive ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'}>
        {isActive ? 'Active' : 'Inactive'}
      </Badge>
    );
  };

  const formatDuration = (startTime: string, endTime?: string) => {
    const start = new Date(startTime);
    const end = endTime ? new Date(endTime) : new Date();
    const diff = end.getTime() - start.getTime();
    const hours = Math.floor(diff / (1000 * 60 * 60));
    const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));
    return `${hours}h ${minutes}m`;
  };

  const filteredSessions = activeSessions.filter(session => {
    if (filter.department !== 'all' && session.department !== filter.department) return false;
    if (filter.status !== 'all' && session.isActive !== (filter.status === 'active')) return false;
    return true;
  });

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-xl font-semibold">Session Management</h2>
          <p className="text-muted-foreground">Monitor and manage user sessions</p>
        </div>
        <Button 
          onClick={loadSessionData} 
          disabled={loading} 
          className="bg-blue-600 hover:bg-blue-700 text-white border-2 border-blue-500 hover:border-blue-400 transition-all duration-300 shadow-lg gap-2"
          style={{ 
            backgroundColor: '#2563eb', 
            color: 'white', 
            borderColor: '#3b82f6' 
          }}
        >
          <RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />
          Refresh
        </Button>
      </div>

      {/* Statistics Cards */}
      {sessionStats && (
        <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
          <Card className="glass-panel border-glass-border">
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm text-muted-foreground">Active Sessions</p>
                  <p className="text-2xl font-bold">{sessionStats.totalActiveSessions}</p>
                </div>
                <Activity className="h-8 w-8 text-green-400" />
              </div>
            </CardContent>
          </Card>

          <Card className="glass-panel border-glass-border">
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm text-muted-foreground">Today's Sessions</p>
                  <p className="text-2xl font-bold">{sessionStats.totalSessionsToday}</p>
                </div>
                <Clock className="h-8 w-8 text-blue-400" />
              </div>
            </CardContent>
          </Card>

          <Card className="glass-panel border-glass-border">
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm text-muted-foreground">Unique Users Today</p>
                  <p className="text-2xl font-bold">{sessionStats.uniqueUsersToday}</p>
                </div>
                <Users className="h-8 w-8 text-purple-400" />
              </div>
            </CardContent>
          </Card>

          <Card className="glass-panel border-glass-border">
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm text-muted-foreground">Avg Session Duration</p>
                  <p className="text-2xl font-bold">{sessionStats.averageSessionDuration}</p>
                </div>
                <BarChart3 className="h-8 w-8 text-orange-400" />
              </div>
            </CardContent>
          </Card>
        </div>
      )}

      <Tabs defaultValue="active-sessions" className="space-y-4">
        <TabsList>
          <TabsTrigger value="active-sessions">Active Sessions</TabsTrigger>
          <TabsTrigger value="login-history">Login History</TabsTrigger>
          <TabsTrigger value="statistics">Statistics</TabsTrigger>
        </TabsList>

        <TabsContent value="active-sessions" className="space-y-4">
          {/* Filters */}
          <Card className="glass-panel border-glass-border">
            <CardContent className="p-4">
              <div className="flex gap-4">
                <Select value={filter.department} onValueChange={(value) => setFilter(prev => ({ ...prev, department: value }))}>
                  <SelectTrigger className="w-48">
                    <SelectValue placeholder="All Departments" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">All Departments</SelectItem>
                    <SelectItem value="ER">ER</SelectItem>
                    <SelectItem value="Billing">Billing</SelectItem>
                  </SelectContent>
                </Select>

                <Select value={filter.status} onValueChange={(value) => setFilter(prev => ({ ...prev, status: value }))}>
                  <SelectTrigger className="w-48">
                    <SelectValue placeholder="All Status" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">All Status</SelectItem>
                    <SelectItem value="active">Active</SelectItem>
                    <SelectItem value="inactive">Inactive</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </CardContent>
          </Card>

          {/* Active Sessions Table */}
          <Card className="glass-panel border-glass-border">
            <CardHeader>
              <CardTitle>Active Sessions ({filteredSessions.length})</CardTitle>
              <CardDescription>Currently active user sessions</CardDescription>
            </CardHeader>
            <CardContent>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>User</TableHead>
                    <TableHead>Department</TableHead>
                    <TableHead>Device</TableHead>
                    <TableHead>IP Address</TableHead>
                    <TableHead>Login Time</TableHead>
                    <TableHead>Last Activity</TableHead>
                    <TableHead>Duration</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead>Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {filteredSessions.map((session) => (
                    <TableRow key={session.sessionId}>
                      <TableCell>
                        <div>
                          <div className="font-medium">{session.firstName} {session.lastName}</div>
                          <div className="text-sm text-muted-foreground">{session.email}</div>
                        </div>
                      </TableCell>
                      <TableCell>
                        <Badge variant="outline">{session.department}</Badge>
                      </TableCell>
                      <TableCell>
                        <div className="flex items-center gap-2">
                          {getDeviceIcon(session.userAgent)}
                          <span className="text-sm">{session.deviceInfo}</span>
                        </div>
                      </TableCell>
                      <TableCell className="font-mono text-sm">{session.ipAddress}</TableCell>
                      <TableCell className="text-sm">
                        {new Date(session.loginAt).toLocaleString()}
                      </TableCell>
                      <TableCell className="text-sm">
                        {new Date(session.lastActivityAt).toLocaleString()}
                      </TableCell>
                      <TableCell className="text-sm">
                        {formatDuration(session.loginAt)}
                      </TableCell>
                      <TableCell>
                        {getStatusBadge(session.isActive)}
                      </TableCell>
                      <TableCell>
                        <div className="flex gap-2">
                          <Button
                            size="sm"
                            variant="outline"
                            onClick={() => {
                              setSelectedUserId(session.userId);
                              loadLoginHistory(session.userId);
                            }}
                          >
                            <Eye className="h-4 w-4" />
                          </Button>
                          <Button
                            size="sm"
                            variant="destructive"
                            onClick={() => {
                              setSelectedSessionId(session.sessionId);
                              setShowTerminateDialog(true);
                            }}
                          >
                            <Trash2 className="h-4 w-4" />
                          </Button>
                        </div>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="login-history" className="space-y-4">
          <Card className="glass-panel border-glass-border">
            <CardHeader>
              <CardTitle>Login History</CardTitle>
              <CardDescription>Recent login attempts and sessions</CardDescription>
            </CardHeader>
            <CardContent>
              {loginHistory.length === 0 ? (
                <div className="text-center py-8">
                  <Shield className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
                  <p className="text-muted-foreground">Select a user to view their login history</p>
                </div>
              ) : (
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>User</TableHead>
                      <TableHead>IP Address</TableHead>
                      <TableHead>Device</TableHead>
                      <TableHead>Login Time</TableHead>
                      <TableHead>Logout Time</TableHead>
                      <TableHead>Duration</TableHead>
                      <TableHead>Status</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {loginHistory.map((history) => (
                      <TableRow key={history.id}>
                        <TableCell>
                          <div>
                            <div className="font-medium">{history.firstName} {history.lastName}</div>
                            <div className="text-sm text-muted-foreground">{history.email}</div>
                          </div>
                        </TableCell>
                        <TableCell className="font-mono text-sm">{history.ipAddress}</TableCell>
                        <TableCell>
                          <div className="flex items-center gap-2">
                            {getDeviceIcon(history.userAgent)}
                            <span className="text-sm">{history.deviceInfo}</span>
                          </div>
                        </TableCell>
                        <TableCell className="text-sm">
                          {new Date(history.loginAt).toLocaleString()}
                        </TableCell>
                        <TableCell className="text-sm">
                          {history.logoutAt ? new Date(history.logoutAt).toLocaleString() : '-'}
                        </TableCell>
                        <TableCell className="text-sm">
                          {history.sessionDuration || formatDuration(history.loginAt, history.logoutAt)}
                        </TableCell>
                        <TableCell>
                          <Badge className={history.isSuccessful ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'}>
                            {history.isSuccessful ? 'Success' : 'Failed'}
                          </Badge>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="statistics" className="space-y-4">
          {sessionStats && (
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <Card className="glass-panel border-glass-border">
                <CardHeader>
                  <CardTitle>Sessions by Department</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="space-y-2">
                    {Object.entries(sessionStats.sessionsByDepartment).map(([dept, count]) => (
                      <div key={dept} className="flex justify-between">
                        <span>{dept}</span>
                        <Badge variant="outline">{count}</Badge>
                      </div>
                    ))}
                  </div>
                </CardContent>
              </Card>

              <Card className="glass-panel border-glass-border">
                <CardHeader>
                  <CardTitle>Session Summary</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="space-y-2">
                    <div className="flex justify-between">
                      <span>This Week</span>
                      <span className="font-medium">{sessionStats.totalSessionsThisWeek}</span>
                    </div>
                    <div className="flex justify-between">
                      <span>This Month</span>
                      <span className="font-medium">{sessionStats.totalSessionsThisMonth}</span>
                    </div>
                    <div className="flex justify-between">
                      <span>Unique Users (Week)</span>
                      <span className="font-medium">{sessionStats.uniqueUsersThisWeek}</span>
                    </div>
                    <div className="flex justify-between">
                      <span>Unique Users (Month)</span>
                      <span className="font-medium">{sessionStats.uniqueUsersThisMonth}</span>
                    </div>
                  </div>
                </CardContent>
              </Card>
            </div>
          )}
        </TabsContent>
      </Tabs>

      {/* Terminate Session Dialog */}
      <Dialog open={showTerminateDialog} onOpenChange={setShowTerminateDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Terminate Session</DialogTitle>
            <DialogDescription>
              Are you sure you want to terminate this session? The user will be logged out immediately.
            </DialogDescription>
          </DialogHeader>
          <div className="flex gap-3">
            <Button
              variant="destructive"
              onClick={() => handleTerminateSession(selectedSessionId)}
            >
              Terminate Session
            </Button>
            <Button variant="outline" onClick={() => setShowTerminateDialog(false)}>
              Cancel
            </Button>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}