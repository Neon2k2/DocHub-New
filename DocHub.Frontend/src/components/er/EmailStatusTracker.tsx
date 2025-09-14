import React, { useState, useEffect, useRef } from 'react';
import { Mail, CheckCircle, AlertCircle, Clock, User, Eye, Loader2, RefreshCw, Bell, BellOff } from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../ui/card';
import { Badge } from '../ui/badge';
import { Button } from '../ui/button';
import { ScrollArea } from '../ui/scroll-area';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../ui/tabs';
import { Input } from '../ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../ui/select';
import { Switch } from '../ui/switch';
import { Label } from '../ui/label';
import { documentService, EmailJob } from '../../services/document.service';
import { signalRService, EmailStatusUpdate } from '../../services/signalr.service';
import { useAuth } from '../../contexts/AuthContext';
import { notify } from '../../utils/notifications';
import { Loading } from '../ui/loading';

interface EmailStatusTrackerProps {
  showOnlyOwnEmails?: boolean;
}

export function EmailStatusTracker({ showOnlyOwnEmails = false }: EmailStatusTrackerProps) {
  const [emailJobs, setEmailJobs] = useState<EmailJob[]>([]);
  const [loading, setLoading] = useState(false);
  const [filter, setFilter] = useState<{
    status: string;
    employee: string;
    sentBy: string;
  }>({
    status: 'all',
    employee: '',
    sentBy: ''
  });
  const [realTimeUpdates, setRealTimeUpdates] = useState<EmailStatusUpdate[]>([]);
  const [isRealTimeEnabled, setIsRealTimeEnabled] = useState(true);
  const [connectionStatus, setConnectionStatus] = useState<'connected' | 'disconnected' | 'reconnecting'>('disconnected');
  const [lastUpdate, setLastUpdate] = useState<Date | null>(null);
  const { user, isAdmin } = useAuth();
  const initializingRef = useRef(false);
  const cleanupRef = useRef(false);

  useEffect(() => {
    loadEmailJobs();
    initializeSignalR();

    return () => {
      cleanupRef.current = true;
      cleanupSignalR();
    };
  }, []);

  const initializeSignalR = async () => {
    if (!isRealTimeEnabled || initializingRef.current || cleanupRef.current) return;
    
    try {
      initializingRef.current = true;
      setConnectionStatus('reconnecting');
      
      await signalRService.start();
      
      // Only proceed if we haven't started cleanup
      if (cleanupRef.current) {
        await signalRService.stop();
        return;
      }
      
      setConnectionStatus('connected');
      
      // Listen for email status updates
      signalRService.onEmailStatusUpdated(handleEmailStatusUpdate);
      
      // Listen for connection state changes
      const checkConnection = () => {
        if (cleanupRef.current) return;
        
        const state = signalRService.getConnectionState();
        if (state === 'Connected') {
          setConnectionStatus('connected');
        } else if (state === 'Reconnecting') {
          setConnectionStatus('reconnecting');
        } else {
          setConnectionStatus('disconnected');
        }
      };
      
      // Check connection state periodically
      const interval = setInterval(checkConnection, 2000);
      
      return () => clearInterval(interval);
    } catch (error) {
      console.error('Failed to initialize SignalR:', error);
      setConnectionStatus('disconnected');
    } finally {
      initializingRef.current = false;
    }
  };

  const handleEmailStatusUpdate = (update: EmailStatusUpdate) => {
    // Add to real-time updates
    setRealTimeUpdates(prev => [update, ...prev].slice(0, 10));
    
    // Update the main email jobs list
    setEmailJobs(prev => prev.map(job => 
      job.id === update.emailJobId 
        ? { ...job, status: update.status }
        : job
    ));
    
    // Show notification
    if (update.status === 'sent' || update.status === 'delivered') {
      notify.success(`Email ${update.status} to ${update.employeeName}`);
    } else if (update.status === 'failed' || update.status === 'bounced') {
      notify.error(`Email ${update.status} to ${update.employeeName}`);
    }
    
    setLastUpdate(new Date());
  };

  const cleanupSignalR = async () => {
    try {
      cleanupRef.current = true;
      signalRService.offEmailStatusUpdated(handleEmailStatusUpdate);
      await signalRService.stop();
      setConnectionStatus('disconnected');
    } catch (error) {
      console.warn('Error during SignalR cleanup:', error);
    }
  };

  const toggleRealTime = async () => {
    if (isRealTimeEnabled) {
      await cleanupSignalR();
      setConnectionStatus('disconnected');
    } else {
      await initializeSignalR();
      setConnectionStatus('connected');
    }
    setIsRealTimeEnabled(!isRealTimeEnabled);
  };

  const retryConnection = async () => {
    setConnectionStatus('reconnecting');
    cleanupRef.current = false; // Reset cleanup flag for retry
    initializingRef.current = false; // Reset initializing flag
    await cleanupSignalR();
    await initializeSignalR();
  };

  const loadEmailJobs = async () => {
    setLoading(true);
    try {
      const params: any = {};
      if (filter.status && filter.status !== 'all') params.status = filter.status;
      if (filter.employee) params.employeeId = filter.employee;
      
      // Backend now handles user filtering automatically based on admin permissions
      // Admin users see all emails, regular users see only their own
      // No need for frontend filtering - backend does it securely
      
      const jobs = await documentService.getEmailJobs(params);
      setEmailJobs(jobs);
    } catch (error) {
      console.error('Failed to load email jobs:', error);
    } finally {
      setLoading(false);
    }
  };

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'sent':
      case 'delivered':
      case 'opened':
        return <CheckCircle className="h-4 w-4 text-green-400" />;
      case 'failed':
        return <AlertCircle className="h-4 w-4 text-red-400" />;
      case 'sending':
        return <Loader2 className="h-4 w-4 animate-spin text-blue-400" />;
      default:
        return <Clock className="h-4 w-4 text-orange-400" />;
    }
  };

  const getStatusBadge = (status: string) => {
    const colors = {
      pending: 'text-orange-400 border-orange-500/30',
      sending: 'text-blue-400 border-blue-500/30',
      sent: 'text-green-400 border-green-500/30',
      delivered: 'text-green-400 border-green-500/30',
      opened: 'text-purple-400 border-purple-500/30',
      failed: 'text-red-400 border-red-500/30'
    };

    return (
      <Badge variant="outline" className={colors[status as keyof typeof colors] || 'text-muted-foreground'}>
        {status.toUpperCase()}
      </Badge>
    );
  };

  const formatDateTime = (date: Date | string) => {
    try {
      const dateObj = typeof date === 'string' ? new Date(date) : date;
      
      // Check if the date is valid
      if (isNaN(dateObj.getTime())) {
        return 'Invalid Date';
      }
      
      return new Intl.DateTimeFormat('en-US', {
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
      }).format(dateObj);
    } catch (error) {
      console.error('Error formatting date:', error, 'Input:', date);
      return 'Invalid Date';
    }
  };

  const getStatusCounts = () => {
    const jobs = Array.isArray(emailJobs) ? emailJobs : [];
    const counts = jobs.reduce((acc, job) => {
      acc[job.status] = (acc[job.status] || 0) + 1;
      return acc;
    }, {} as Record<string, number>);

    return {
      total: jobs.length,
      pending: counts.pending || 0,
      sending: counts.sending || 0,
      sent: (counts.sent || 0) + (counts.delivered || 0) + (counts.opened || 0),
      failed: counts.failed || 0
    };
  };

  const filteredJobs = (Array.isArray(emailJobs) ? emailJobs : []).filter(job => {
    if (filter.status && filter.status !== 'all' && job.status !== filter.status) return false;
    if (filter.employee && !job.employeeName.toLowerCase().includes(filter.employee.toLowerCase()) && 
        !job.employeeId.toLowerCase().includes(filter.employee.toLowerCase())) return false;
    if (filter.sentBy && !job.sentBy.toLowerCase().includes(filter.sentBy.toLowerCase())) return false;
    return true;
  });

  const statusCounts = getStatusCounts();

  return (
    <div className="space-y-6">
      {/* Header with Stats */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <Card className="glass-panel border-glass-border">
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-muted-foreground">Total Emails</p>
                <p className="text-xl font-bold">{statusCounts.total}</p>
              </div>
              <Mail className="h-6 w-6 text-neon-blue" />
            </div>
          </CardContent>
        </Card>

        <Card className="glass-panel border-glass-border">
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-muted-foreground">In Progress</p>
                <p className="text-xl font-bold">{statusCounts.pending + statusCounts.sending}</p>
              </div>
              <Loader2 className="h-6 w-6 text-orange-400" />
            </div>
          </CardContent>
        </Card>

        <Card className="glass-panel border-glass-border">
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-muted-foreground">Successful</p>
                <p className="text-xl font-bold">{statusCounts.sent}</p>
              </div>
              <CheckCircle className="h-6 w-6 text-green-400" />
            </div>
          </CardContent>
        </Card>

        <Card className="glass-panel border-glass-border">
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-muted-foreground">Failed</p>
                <p className="text-xl font-bold">{statusCounts.failed}</p>
              </div>
              <AlertCircle className="h-6 w-6 text-red-400" />
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Real-time Controls */}
      <Card className="glass-panel border-glass-border">
        <CardContent className="p-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-4">
              <div className="flex items-center gap-2">
                <Switch
                  checked={isRealTimeEnabled}
                  onCheckedChange={toggleRealTime}
                />
                <Label className="text-sm">Real-time Updates</Label>
              </div>
              
              <div className="flex items-center gap-2">
                <div className={`w-2 h-2 rounded-full ${
                  connectionStatus === 'connected' ? 'bg-green-400' :
                  connectionStatus === 'reconnecting' ? 'bg-yellow-400' :
                  'bg-red-400'
                }`} />
                <span className="text-sm text-muted-foreground capitalize">
                  {connectionStatus}
                </span>
              </div>
              
              {lastUpdate && (
                <span className="text-xs text-muted-foreground">
                  Last update: {lastUpdate.toLocaleTimeString()}
                </span>
              )}
            </div>
            
            <div className="flex items-center gap-2">
              {connectionStatus === 'disconnected' && (
                <Button
                  variant="outline"
                  size="sm"
                  onClick={retryConnection}
                >
                  <RefreshCw className="h-4 w-4 mr-2" />
                  Retry Connection
                </Button>
              )}
              
              <Button
                variant="outline"
                size="sm"
                onClick={loadEmailJobs}
                disabled={loading}
              >
                {loading ? (
                  <Loader2 className="h-4 w-4 animate-spin mr-2" />
                ) : (
                  <RefreshCw className="h-4 w-4 mr-2" />
                )}
                Manual Refresh
              </Button>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Main Content */}
      <Tabs defaultValue="all" className="space-y-4">
        <div className="flex items-center justify-between">
          <TabsList>
            <TabsTrigger value="all">All Emails</TabsTrigger>
            <TabsTrigger value="live">Live Updates</TabsTrigger>
          </TabsList>
        </div>

        <TabsContent value="all" className="space-y-4">
          {/* Filters */}
          <Card className="glass-panel border-glass-border">
            <CardContent className="p-4">
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                <Select
                  value={filter.status}
                  onValueChange={(value) => setFilter(prev => ({ ...prev, status: value }))}
                >
                  <SelectTrigger>
                    <SelectValue placeholder="All Statuses" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">All Statuses</SelectItem>
                    <SelectItem value="pending">Pending</SelectItem>
                    <SelectItem value="sending">Sending</SelectItem>
                    <SelectItem value="sent">Sent</SelectItem>
                    <SelectItem value="delivered">Delivered</SelectItem>
                    <SelectItem value="opened">Opened</SelectItem>
                    <SelectItem value="failed">Failed</SelectItem>
                  </SelectContent>
                </Select>

                <Input
                  placeholder="Search employee..."
                  value={filter.employee}
                  onChange={(e) => setFilter(prev => ({ ...prev, employee: e.target.value }))}
                />

                {isAdmin() && (
                  <Input
                    placeholder="Search sender..."
                    value={filter.sentBy}
                    onChange={(e) => setFilter(prev => ({ ...prev, sentBy: e.target.value }))}
                  />
                )}
              </div>
            </CardContent>
          </Card>

          {/* Email Jobs List */}
          <Card className="glass-panel border-glass-border">
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Mail className="h-5 w-5 text-neon-blue" />
                Email Status ({filteredJobs.length})
              </CardTitle>
              <CardDescription>
                {isAdmin() 
                  ? 'All email communications from all users and their delivery status (Admin View)'
                  : 'Your sent emails and their delivery status'
                }
              </CardDescription>
            </CardHeader>
            <CardContent>
              <ScrollArea className="h-96">
                {loading ? (
                  <div className="space-y-3">
                    {Array.from({ length: 5 }).map((_, i) => (
                      <div key={i} className="flex items-center gap-3 p-3 glass-panel rounded-lg animate-pulse">
                        <div className="w-8 h-8 bg-muted rounded-full" />
                        <div className="flex-1 space-y-2">
                          <div className="h-4 bg-muted rounded w-1/3" />
                          <div className="h-3 bg-muted rounded w-1/2" />
                        </div>
                        <div className="h-6 bg-muted rounded w-20" />
                      </div>
                    ))}
                  </div>
                ) : filteredJobs.length === 0 ? (
                  <div className="text-center py-12">
                    <Mail className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
                    <h3 className="text-lg font-semibold mb-2">No Emails Found</h3>
                    <p className="text-muted-foreground">
                      No emails match your current filters.
                    </p>
                  </div>
                ) : (
                  <div className="space-y-3">
                    {filteredJobs.map((job) => (
                      <div
                        key={job.id}
                        className="flex items-center justify-between p-4 glass-panel rounded-lg border-glass-border hover:bg-muted/10 transition-colors"
                      >
                        <div className="flex items-center gap-4">
                          <div className={`w-10 h-10 rounded-full flex items-center justify-center ${
                            job.status === 'sent' || job.status === 'delivered' ? 'bg-green-500/20' :
                            job.status === 'failed' ? 'bg-red-500/20' :
                            job.status === 'sending' ? 'bg-blue-500/20' : 'bg-orange-500/20'
                          }`}>
                            {getStatusIcon(job.status)}
                          </div>

                          <div className="flex-1 min-w-0">
                            <div className="flex items-center gap-2 mb-1">
                              <p className="font-medium">{job.employeeName}</p>
                              <Badge variant="outline" className="text-xs">
                                {job.employeeId}
                              </Badge>
                            </div>
                            <p className="text-sm text-muted-foreground truncate">
                              {job.employeeEmail}
                            </p>
                            <div className="flex items-center gap-4 text-xs text-muted-foreground mt-1">
                              <span>Subject: {job.subject}</span>
                              <span>•</span>
                              <span>Sent by: {job.sentBy}</span>
                              <span>•</span>
                              <span>{formatDateTime(job.createdAt)}</span>
                            </div>
                          </div>
                        </div>

                        <div className="flex items-center gap-3">
                          {getStatusBadge(job.status)}
                          
                          {job.trackingId && (
                            <Button size="sm" variant="ghost">
                              <Eye className="h-4 w-4" />
                            </Button>
                          )}
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </ScrollArea>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="live" className="space-y-4">
          <Card className="glass-panel border-glass-border">
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <div className="w-3 h-3 bg-green-400 rounded-full animate-pulse" />
                Live Email Updates
              </CardTitle>
              <CardDescription>
                Real-time status updates for outgoing emails
              </CardDescription>
            </CardHeader>
            <CardContent>
              <ScrollArea className="h-96">
                {realTimeUpdates.length === 0 ? (
                  <div className="text-center py-12">
                    <Clock className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
                    <h3 className="text-lg font-semibold mb-2">No Recent Updates</h3>
                    <p className="text-muted-foreground">
                      Live email status updates will appear here.
                    </p>
                  </div>
                ) : (
                  <div className="space-y-3">
                    {realTimeUpdates.map((update, index) => (
                      <div
                        key={`${update.emailJobId}-${index}`}
                        className="flex items-center gap-4 p-3 glass-panel rounded-lg border-glass-border animate-pulse"
                      >
                        <div className={`w-8 h-8 rounded-full flex items-center justify-center ${
                          update.status === 'sent' || update.status === 'delivered' ? 'bg-green-500/20' :
                          update.status === 'failed' || update.status === 'bounced' ? 'bg-red-500/20' :
                          update.status === 'sending' ? 'bg-blue-500/20' : 'bg-orange-500/20'
                        }`}>
                          {getStatusIcon(update.status)}
                        </div>

                        <div className="flex-1 min-w-0">
                          <p className="font-medium">{update.employeeName}</p>
                          <div className="flex items-center gap-2 text-sm text-muted-foreground">
                            <span>Status updated from {update.oldStatus} to:</span>
                            {getStatusBadge(update.status)}
                            <span>•</span>
                            <span>{formatDateTime(update.timestamp)}</span>
                          </div>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </ScrollArea>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}