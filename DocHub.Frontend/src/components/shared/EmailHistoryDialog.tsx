import React, { useState, useEffect } from 'react';
import { Mail, CheckCircle, AlertCircle, Clock, User, Eye, Loader2, RefreshCw, X, Calendar, Filter } from 'lucide-react';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '../ui/dialog';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../ui/card';
import { Badge } from '../ui/badge';
import { Button } from '../ui/button';
import { ScrollArea } from '../ui/scroll-area';
import { Input } from '../ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../ui/select';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../ui/tabs';
import { EmailJob } from '../../services/document.service';
import { apiService } from '../../services/api.service';
import { signalRService, EmailStatusUpdate } from '../../services/signalr.service';
import { useAuth } from '../../contexts/AuthContext';
import { Loading } from '../ui/loading';
import { cacheService } from '../../services/cache.service';

interface EmailHistoryDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  tabId: string;
  tabName: string;
  highlightedEmailJobId?: string | null;
}

export function EmailHistoryDialog({ open, onOpenChange, tabId, tabName, highlightedEmailJobId }: EmailHistoryDialogProps) {
  const [emailJobs, setEmailJobs] = useState<EmailJob[]>([]);
  const [loading, setLoading] = useState(false);
  
  console.log('📧 [EMAIL-HISTORY] EmailHistoryDialog rendered with highlightedEmailJobId:', highlightedEmailJobId);
  const [filter, setFilter] = useState({
    status: 'all',
    employee: '',
    dateRange: 'all'
  });
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [realTimeUpdates, setRealTimeUpdates] = useState<EmailStatusUpdate[]>([]);
  const [isRealTimeEnabled, setIsRealTimeEnabled] = useState(true);
  const [signalRCallback, setSignalRCallback] = useState<((update: EmailStatusUpdate) => void) | null>(null);

  const pageSize = 20;

  useEffect(() => {
    if (open) {
      // Clear cache and reload fresh data when dialog opens
      cacheService.invalidatePattern(`email_history_${tabId}_`);
      loadEmailHistory();
      initializeSignalR();
    }

    return () => {
      // Clean up SignalR listeners when component unmounts or dependencies change
      if (signalRCallback) {
        signalRService.offEmailStatusUpdated(signalRCallback);
        setSignalRCallback(null);
      }
    };
  }, [open, currentPage, filter, signalRCallback]);

  const initializeSignalR = async () => {
    try {
      await signalRService.start();
      
      const callback = (update: EmailStatusUpdate) => {
        console.log('📧 [EMAIL-HISTORY] Received email status update:', update);
        setRealTimeUpdates(prev => [update, ...prev].slice(0, 10));
        
        // Update the main email jobs list
        setEmailJobs(prev => prev.map(job => 
          job.id === update.emailJobId 
            ? { ...job, status: update.status as any }
            : job
        ));
        
        // Invalidate cache when status updates
        cacheService.invalidatePattern(`email_history_${tabId}_`);
      };
      
      signalRService.onEmailStatusUpdated(callback);
      setSignalRCallback(callback);
      
      // Manually join user group as backup
      const { user } = useAuth();
      if (user?.id) {
        await signalRService.joinUserGroup(user.id);
      }
    } catch (error) {
      console.error('Failed to initialize SignalR for email history:', error);
    }
  };

  const loadEmailHistory = async (forceRefresh = false) => {
    console.log('📧 [EMAIL_HISTORY] Loading email history for tab:', tabId, 'page:', currentPage, 'forceRefresh:', forceRefresh);
    
    // Create cache key based on tab and page
    const cacheKey = `email_history_${tabId}_${currentPage}_${pageSize}`;
    const allDataCacheKey = `email_history_all_${tabId}`;
    
    // Check cache first (unless forcing refresh)
    if (!forceRefresh) {
      const cachedData = cacheService.get<{emailJobs: EmailJob[], totalPages: number}>(cacheKey);
      if (cachedData) {
        console.log('📦 [EMAIL_HISTORY] Returning cached email history:', cachedData.emailJobs.length);
        setEmailJobs(cachedData.emailJobs);
        setTotalPages(cachedData.totalPages);
        return;
      }
      
      // Check if we have all data cached and can slice it
      const allCachedData = cacheService.get<{emailJobs: EmailJob[], totalPages: number}>(allDataCacheKey);
      if (allCachedData && currentPage === 1) {
        console.log('📦 [EMAIL_HISTORY] Using all cached data for page 1:', allCachedData.emailJobs.length);
        setEmailJobs(allCachedData.emailJobs);
        setTotalPages(allCachedData.totalPages);
        return;
      }
    }
    
    setLoading(true);
    try {
      const response = await apiService.getTabEmailHistory(tabId, currentPage, pageSize);
      console.log('📊 [EMAIL_HISTORY] API response:', response);
      
      if (response.success && response.data) {
        console.log('✅ [EMAIL_HISTORY] Successfully loaded', response.data.length, 'email jobs');
        setEmailJobs(response.data);
        // Calculate total pages (this is a simplified calculation)
        const totalPages = Math.ceil(response.data.length / pageSize);
        setTotalPages(totalPages);
        console.log('📄 [EMAIL_HISTORY] Set total pages to:', totalPages);
        
        // Cache for 5 minutes (increased from 1 minute)
        cacheService.set(cacheKey, { emailJobs: response.data, totalPages }, 5 * 60 * 1000);
        
        // If this is page 1, also cache as "all data" for faster subsequent loads
        if (currentPage === 1) {
          cacheService.set(allDataCacheKey, { emailJobs: response.data, totalPages }, 5 * 60 * 1000);
        }
      } else {
        console.log('⚠️ [EMAIL_HISTORY] No data received from API');
      }
    } catch (error) {
      console.error('❌ [EMAIL_HISTORY] Error loading email history:', error);
    } finally {
      setLoading(false);
      console.log('🏁 [EMAIL_HISTORY] Loading completed');
    }
  };

  const handleRefresh = () => {
    console.log('🔄 [EMAIL_HISTORY] Manual refresh triggered');
    cacheService.invalidatePattern(`email_history_${tabId}_`);
    loadEmailHistory(true);
  };

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'sent': return <CheckCircle className="h-4 w-4 text-green-500" />;
      case 'delivered': return <CheckCircle className="h-4 w-4 text-green-600" />;
      case 'opened': return <Eye className="h-4 w-4 text-blue-500" />;
      case 'clicked': return <CheckCircle className="h-4 w-4 text-purple-500" />;
      case 'bounced': return <AlertCircle className="h-4 w-4 text-red-500" />;
      case 'dropped': return <AlertCircle className="h-4 w-4 text-orange-500" />;
      case 'failed': return <AlertCircle className="h-4 w-4 text-red-600" />;
      case 'pending': return <Clock className="h-4 w-4 text-yellow-500" />;
      case 'sending': return <Loader2 className="h-4 w-4 animate-spin text-blue-500" />;
      default: return <Mail className="h-4 w-4 text-gray-500" />;
    }
  };

  const getStatusBadge = (status: string) => {
    const variants: Record<string, string> = {
      sent: 'bg-green-100 text-green-800',
      delivered: 'bg-green-100 text-green-800',
      opened: 'bg-blue-100 text-blue-800',
      clicked: 'bg-purple-100 text-purple-800',
      bounced: 'bg-red-100 text-red-800',
      dropped: 'bg-orange-100 text-orange-800',
      failed: 'bg-red-100 text-red-800',
      pending: 'bg-yellow-100 text-yellow-800',
      sending: 'bg-blue-100 text-blue-800'
    };

    return (
      <Badge className={`${variants[status] || 'bg-gray-100 text-gray-800'} text-xs`}>
        {status.charAt(0).toUpperCase() + status.slice(1)}
      </Badge>
    );
  };

  const formatDateTime = (date: Date | string) => {
    const d = new Date(date);
    return d.toLocaleString();
  };

  const filteredEmails = emailJobs.filter(job => {
    if (filter.status !== 'all' && job.status !== filter.status) return false;
    if (filter.employee && !job.employeeName.toLowerCase().includes(filter.employee.toLowerCase())) return false;
    return true;
  });

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-6xl max-h-[80vh] overflow-hidden">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Mail className="h-5 w-5" />
            Email History - {tabName}
          </DialogTitle>
        </DialogHeader>

        <div className="flex-1 overflow-hidden">
          <Tabs defaultValue="history" className="h-full flex flex-col">
            <div className="flex items-center justify-between mb-4">
              <TabsList>
                <TabsTrigger value="history">Email History</TabsTrigger>
                <TabsTrigger value="live">Live Updates</TabsTrigger>
              </TabsList>
              
              <div className="flex items-center gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={handleRefresh}
                  disabled={loading}
                >
                  <RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />
                  Refresh
                </Button>
              </div>
            </div>

            <TabsContent value="history" className="flex-1 overflow-hidden">
              {/* Filters */}
              <Card className="mb-4">
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
                        <SelectItem value="clicked">Clicked</SelectItem>
                        <SelectItem value="bounced">Bounced</SelectItem>
                        <SelectItem value="dropped">Dropped</SelectItem>
                        <SelectItem value="failed">Failed</SelectItem>
                      </SelectContent>
                    </Select>

                    <Input
                      placeholder="Search employee..."
                      value={filter.employee}
                      onChange={(e) => setFilter(prev => ({ ...prev, employee: e.target.value }))}
                    />

                    <Select
                      value={filter.dateRange}
                      onValueChange={(value) => setFilter(prev => ({ ...prev, dateRange: value }))}
                    >
                      <SelectTrigger>
                        <SelectValue placeholder="All Time" />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="all">All Time</SelectItem>
                        <SelectItem value="today">Today</SelectItem>
                        <SelectItem value="week">This Week</SelectItem>
                        <SelectItem value="month">This Month</SelectItem>
                      </SelectContent>
                    </Select>
                  </div>
                </CardContent>
              </Card>

              {/* Email List */}
              <Card className="flex-1 overflow-hidden">
                <CardContent className="p-0 h-full">
                  <ScrollArea className="h-full">
                    {loading ? (
                      <div className="flex items-center justify-center h-32">
                        <Loading />
                      </div>
                    ) : filteredEmails.length === 0 ? (
                      <div className="text-center py-12">
                        <Mail className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
                        <h3 className="text-lg font-semibold mb-2">No Email History</h3>
                        <p className="text-muted-foreground">
                          No emails have been sent from this tab yet.
                        </p>
                      </div>
                    ) : (
                      <div className="space-y-2 p-4">
                        {filteredEmails.map((job) => (
                          <div
                            key={job.id}
                            className={`flex items-center gap-4 p-4 glass-panel rounded-lg border-glass-border hover:bg-muted/50 transition-colors ${
                              highlightedEmailJobId === job.id 
                                ? 'ring-2 ring-blue-500 bg-blue-50/50 dark:bg-blue-950/50' 
                                : ''
                            }`}
                          >
                            <div className="flex-shrink-0">
                              {getStatusIcon(job.status)}
                            </div>

                            <div className="flex-1 min-w-0">
                              <div className="flex items-center justify-between mb-2">
                                <div className="flex items-center gap-2">
                                  <h4 className="font-medium truncate">{job.employeeName}</h4>
                                  <span className="text-sm text-muted-foreground">
                                    {job.employeeEmail}
                                  </span>
                                </div>
                                <div className="flex items-center gap-2">
                                  {getStatusBadge(job.status)}
                                  <span className="text-xs text-muted-foreground">
                                    {formatDateTime(job.createdAt)}
                                  </span>
                                </div>
                              </div>

                              <div className="text-sm text-muted-foreground">
                                <p className="truncate">{job.subject}</p>
                                {job.errorMessage && (
                                  <p className="text-red-500 text-xs mt-1">
                                    Error: {job.errorMessage}
                                  </p>
                                )}
                              </div>

                              <div className="flex items-center gap-4 mt-2 text-xs text-muted-foreground">
                                {job.sentAt && (
                                  <span>Sent: {formatDateTime(job.sentAt)}</span>
                                )}
                                {job.deliveredAt && (
                                  <span>Delivered: {formatDateTime(job.deliveredAt)}</span>
                                )}
                                {job.openedAt && (
                                  <span>Opened: {formatDateTime(job.openedAt)}</span>
                                )}
                                {job.clickedAt && (
                                  <span>Clicked: {formatDateTime(job.clickedAt)}</span>
                                )}
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

            <TabsContent value="live" className="flex-1 overflow-hidden">
              <Card className="h-full">
                <CardHeader>
                  <CardTitle className="flex items-center gap-2">
                    <div className="w-3 h-3 bg-green-400 rounded-full animate-pulse" />
                    Live Email Updates
                  </CardTitle>
                  <CardDescription>
                    Real-time email status updates for this tab
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
                            <div className="w-8 h-8 rounded-full flex items-center justify-center bg-green-500/20">
                              {getStatusIcon(update.status)}
                            </div>

                            <div className="flex-1 min-w-0">
                              <p className="font-medium">{update.employeeName || 'Unknown'}</p>
                              <div className="flex items-center gap-2 text-sm text-muted-foreground">
                                <span>Status updated to:</span>
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
      </DialogContent>
    </Dialog>
  );
}
