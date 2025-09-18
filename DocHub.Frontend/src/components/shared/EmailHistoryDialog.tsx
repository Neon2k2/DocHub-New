import React, { useState, useEffect, useMemo, useCallback } from 'react';
import { Mail, CheckCircle, AlertCircle, Clock, User, Eye, Loader2, RefreshCw, X, Calendar, Filter, Download, Search, ChevronLeft, ChevronRight, ChevronsLeft, ChevronsRight } from 'lucide-react';
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

// Virtual scrolling hook
const useVirtualScroll = (items: any[], itemHeight: number = 80, containerHeight: number = 400) => {
  const [scrollTop, setScrollTop] = useState(0);
  
  const visibleStart = Math.floor(scrollTop / itemHeight);
  const visibleEnd = Math.min(visibleStart + Math.ceil(containerHeight / itemHeight) + 1, items.length);
  
  const visibleItems = items.slice(visibleStart, visibleEnd);
  const totalHeight = items.length * itemHeight;
  const offsetY = visibleStart * itemHeight;
  
  return {
    visibleItems,
    totalHeight,
    offsetY,
    setScrollTop
  };
};

export function EmailHistoryDialog({ open, onOpenChange, tabId, tabName, highlightedEmailJobId }: EmailHistoryDialogProps) {
  const [emailJobs, setEmailJobs] = useState<EmailJob[]>([]);
  const [loading, setLoading] = useState(false);
  const [totalCount, setTotalCount] = useState(0);
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [totalPages, setTotalPages] = useState(1);
  
  console.log('📧 [EMAIL-HISTORY] EmailHistoryDialog rendered with highlightedEmailJobId:', highlightedEmailJobId);
  const [filter, setFilter] = useState({
    status: 'all',
    employee: '',
    dateRange: 'all',
    searchTerm: ''
  });
  
  const [realTimeUpdates, setRealTimeUpdates] = useState<EmailStatusUpdate[]>([]);
  const [isRealTimeEnabled, setIsRealTimeEnabled] = useState(true);
  const [signalRCallback, setSignalRCallback] = useState<((update: EmailStatusUpdate) => void) | null>(null);
  const [sortBy, setSortBy] = useState('createdAt');
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('desc');
  const [isExporting, setIsExporting] = useState(false);

  // Virtual scrolling
  const { visibleItems, totalHeight, offsetY, setScrollTop } = useVirtualScroll(emailJobs, 80, 400);

  // Memoized filtered and sorted emails
  const processedEmails = useMemo(() => {
    let filtered = emailJobs.filter(job => {
      if (filter.status !== 'all' && job.status !== filter.status) return false;
      if (filter.employee && !job.employeeName.toLowerCase().includes(filter.employee.toLowerCase())) return false;
      if (filter.searchTerm && !job.subject.toLowerCase().includes(filter.searchTerm.toLowerCase()) && 
          !job.employeeName.toLowerCase().includes(filter.searchTerm.toLowerCase())) return false;
      return true;
    });

    // Sort
    filtered.sort((a, b) => {
      let aVal = a[sortBy as keyof EmailJob];
      let bVal = b[sortBy as keyof EmailJob];
      
      if (sortBy === 'createdAt' || sortBy === 'sentAt' || sortBy === 'deliveredAt') {
        aVal = new Date(aVal as string).getTime();
        bVal = new Date(bVal as string).getTime();
      }
      
      if (sortDirection === 'asc') {
        return aVal > bVal ? 1 : -1;
      } else {
        return aVal < bVal ? 1 : -1;
      }
    });

    return filtered;
  }, [emailJobs, filter, sortBy, sortDirection]);

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
      if (user?.id) {
        await signalRService.joinUserGroup(user.id);
      }
    } catch (error) {
      console.error('Failed to initialize SignalR for email history:', error);
    }
  };

  const loadEmailHistory = async (forceRefresh = false) => {
    console.log('📧 [EMAIL_HISTORY] Loading email history for tab:', tabId, 'page:', currentPage, 'forceRefresh:', forceRefresh);
    
    // Create cache key based on tab, page, and filters
    const cacheKey = `email_history_${tabId}_${currentPage}_${pageSize}_${filter.status}_${filter.employee}_${filter.searchTerm}`;
    
    // Check cache first (unless forcing refresh)
    if (!forceRefresh) {
      const cachedData = cacheService.get<{emailJobs: EmailJob[], totalCount: number, totalPages: number}>(cacheKey);
      if (cachedData) {
        console.log('📦 [EMAIL_HISTORY] Returning cached email history:', cachedData.emailJobs.length);
        setEmailJobs(cachedData.emailJobs);
        setTotalCount(cachedData.totalCount);
        setTotalPages(cachedData.totalPages);
        return;
      }
    }
    
    setLoading(true);
    try {
      const response = await apiService.getEmailHistory(tabId, {
        page: currentPage,
        pageSize: pageSize,
        status: filter.status !== 'all' ? filter.status : undefined,
        searchTerm: filter.searchTerm || filter.employee,
        sortBy: sortBy,
        sortDirection: sortDirection
      });
      
      console.log('📊 [EMAIL_HISTORY] API response:', response);
      
      if (response.success && response.data) {
        console.log('✅ [EMAIL_HISTORY] Successfully loaded', response.data.items.length, 'email jobs');
        setEmailJobs(response.data.items);
        setTotalCount(response.data.totalCount);
        setTotalPages(response.data.totalPages);
        
        // Cache for 5 minutes fresh, 15 minutes stale window
        cacheService.set(cacheKey, { 
          emailJobs: response.data.items, 
          totalCount: response.data.totalCount,
          totalPages: response.data.totalPages
        }, 5 * 60 * 1000, 15 * 60 * 1000);
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

  const handleRefresh = useCallback(() => {
    console.log('🔄 [EMAIL_HISTORY] Manual refresh triggered');
    cacheService.invalidatePattern(`email_history_${tabId}_`);
    loadEmailHistory(true);
  }, [tabId, currentPage, pageSize, filter, sortBy, sortDirection]);

  const handlePageChange = useCallback((page: number) => {
    setCurrentPage(page);
  }, []);

  const handlePageSizeChange = useCallback((size: number) => {
    setPageSize(size);
    setCurrentPage(1);
  }, []);

  const handleSort = useCallback((field: string) => {
    if (sortBy === field) {
      setSortDirection(prev => prev === 'asc' ? 'desc' : 'asc');
    } else {
      setSortBy(field);
      setSortDirection('desc');
    }
  }, [sortBy]);

  const handleExport = async () => {
    setIsExporting(true);
    try {
      const response = await apiService.request(`/api/email-history/${tabId}/export`, {
        method: 'GET',
        headers: {
          'Accept': 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
        }
      });
      
      if (response.ok) {
        const blob = await response.blob();
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `email-history-${tabName}-${new Date().toISOString().split('T')[0]}.xlsx`;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        window.URL.revokeObjectURL(url);
      }
    } catch (error) {
      console.error('Export failed:', error);
    } finally {
      setIsExporting(false);
    }
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

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-7xl max-h-[90vh] overflow-hidden">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Mail className="h-5 w-5" />
            Email History - {tabName}
          </DialogTitle>
          <p className="text-sm text-muted-foreground">
            View and manage email history for this tab. Track email status, recipients, and delivery information.
          </p>
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
                  onClick={handleExport}
                  disabled={isExporting || emailJobs.length === 0}
                >
                  <Download className="h-4 w-4 mr-2" />
                  {isExporting ? 'Exporting...' : 'Export'}
                </Button>
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
              {/* Advanced Filters */}
              <Card className="mb-4">
                <CardContent className="p-4">
                  <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
                    <div className="flex items-center gap-2">
                      <Search className="h-4 w-4 text-gray-500" />
                      <Input
                        placeholder="Search emails..."
                        value={filter.searchTerm}
                        onChange={(e) => setFilter(prev => ({ ...prev, searchTerm: e.target.value }))}
                        className="flex-1"
                      />
                    </div>
                    
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

              {/* Email List with Virtual Scrolling */}
              <Card className="flex-1 overflow-hidden">
                <CardContent className="p-0 h-full">
                  {loading ? (
                    <div className="flex items-center justify-center h-32">
                      <Loading />
                    </div>
                  ) : processedEmails.length === 0 ? (
                    <div className="text-center py-12">
                      <Mail className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
                      <h3 className="text-lg font-semibold mb-2">No Email History</h3>
                      <p className="text-muted-foreground">
                        No emails have been sent from this tab yet.
                      </p>
                    </div>
                  ) : (
                    <div className="h-96 overflow-auto" onScroll={(e) => setScrollTop(e.currentTarget.scrollTop)}>
                      <div style={{ height: totalHeight, position: 'relative' }}>
                        <div style={{ transform: `translateY(${offsetY}px)` }}>
                          {visibleItems.map((job) => (
                            <div
                              key={job.id}
                              className={`flex items-center gap-4 p-4 glass-panel rounded-lg border-glass-border hover:bg-muted/50 transition-colors ${
                                highlightedEmailJobId === job.id 
                                  ? 'ring-2 ring-blue-500 bg-blue-50/50 dark:bg-blue-950/50' 
                                  : ''
                              }`}
                              style={{ height: 80 }}
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
                      </div>
                    </div>
                  )}
                </CardContent>
              </Card>

              {/* Pagination */}
              {totalPages > 1 && (
                <Card className="mt-4">
                  <CardContent className="p-4">
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-2">
                        <span className="text-sm text-muted-foreground">
                          Showing {((currentPage - 1) * pageSize) + 1} to {Math.min(currentPage * pageSize, totalCount)} of {totalCount} emails
                        </span>
                        <Select value={pageSize.toString()} onValueChange={(value) => handlePageSizeChange(parseInt(value))}>
                          <SelectTrigger className="w-20">
                            <SelectValue />
                          </SelectTrigger>
                          <SelectContent>
                            <SelectItem value="10">10</SelectItem>
                            <SelectItem value="20">20</SelectItem>
                            <SelectItem value="50">50</SelectItem>
                            <SelectItem value="100">100</SelectItem>
                          </SelectContent>
                        </Select>
                      </div>
                      
                      <div className="flex items-center gap-2">
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => handlePageChange(1)}
                          disabled={currentPage === 1}
                        >
                          <ChevronsLeft className="h-4 w-4" />
                        </Button>
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => handlePageChange(currentPage - 1)}
                          disabled={currentPage === 1}
                        >
                          <ChevronLeft className="h-4 w-4" />
                        </Button>
                        
                        <span className="text-sm">
                          Page {currentPage} of {totalPages}
                        </span>
                        
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => handlePageChange(currentPage + 1)}
                          disabled={currentPage === totalPages}
                        >
                          <ChevronRight className="h-4 w-4" />
                        </Button>
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => handlePageChange(totalPages)}
                          disabled={currentPage === totalPages}
                        >
                          <ChevronsRight className="h-4 w-4" />
                        </Button>
                      </div>
                    </div>
                  </CardContent>
                </Card>
              )}
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
                            key={`${update?.emailJobId || index}-${index}`}
                            className="flex items-center gap-4 p-3 glass-panel rounded-lg border-glass-border animate-pulse"
                          >
                            <div className="w-8 h-8 rounded-full flex items-center justify-center bg-green-500/20">
                              {getStatusIcon(update?.status)}
                            </div>

                            <div className="flex-1 min-w-0">
                              <p className="font-medium">{update?.employeeName || 'Unknown'}</p>
                              <div className="flex items-center gap-2 text-sm text-muted-foreground">
                                <span>Status updated to:</span>
                                {getStatusBadge(update?.status)}
                                <span>•</span>
                                <span>{formatDateTime(update?.timestamp)}</span>
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
