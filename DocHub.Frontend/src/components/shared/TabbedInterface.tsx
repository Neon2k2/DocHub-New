import React from 'react';
import { FileText, Clock, Filter } from 'lucide-react';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../ui/tabs';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../ui/card';
import { Button } from '../ui/button';

interface TabbedInterfaceProps {
  children: React.ReactNode;
  tabName: string;
  requests?: Array<{
    id: string;
    employeeName: string;
    employeeId: string;
    createdAt: Date;
    requestedBy: string;
    status: string;
  }>;
  loading?: boolean;
}

export function TabbedInterface({ 
  children, 
  tabName, 
  requests = [], 
  loading = false 
}: TabbedInterfaceProps) {
  const getStatusBadge = (status: string) => {
    switch (status) {
      case 'pending':
        return <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-orange-100 text-orange-800 border border-orange-200">Pending</span>;
      case 'approved':
        return <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800 border border-green-200">Approved</span>;
      case 'rejected':
        return <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-red-100 text-red-800 border border-red-200">Rejected</span>;
      case 'draft':
        return <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-800 border border-blue-200">Draft</span>;
      default:
        return <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-gray-100 text-gray-800 border border-gray-200">{status}</span>;
    }
  };

  const formatDate = (date: Date) => {
    return new Intl.DateTimeFormat('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    }).format(date);
  };

  return (
    <Tabs defaultValue="generate" className="space-y-6">
      <TabsList className="grid w-full grid-cols-3">
        <TabsTrigger value="generate">Generate Letters</TabsTrigger>
        <TabsTrigger value="requests">View Requests</TabsTrigger>
        <TabsTrigger value="history">History</TabsTrigger>
      </TabsList>

      <TabsContent value="generate" className="space-y-6">
        {children}
      </TabsContent>

      <TabsContent value="requests" className="space-y-6">
        <Card className="glass-panel border-glass-border">
          <CardHeader>
            <div className="flex items-center justify-between">
              <div>
                <CardTitle>{tabName} Requests</CardTitle>
                <CardDescription>
                  Manage and review {tabName.toLowerCase()} requests
                </CardDescription>
              </div>
              <Button variant="outline" size="sm">
                <Filter className="h-4 w-4 mr-2" />
                Filter
              </Button>
            </div>
          </CardHeader>
          <CardContent>
            {loading ? (
              <div className="space-y-4">
                {Array.from({ length: 3 }).map((_, i) => (
                  <div key={i} className="flex items-center justify-between p-4 glass-panel rounded-lg animate-pulse">
                    <div className="space-y-2">
                      <div className="h-4 bg-muted rounded w-32" />
                      <div className="h-3 bg-muted rounded w-24" />
                    </div>
                    <div className="h-6 bg-muted rounded w-20" />
                  </div>
                ))}
              </div>
            ) : requests && requests.length > 0 ? (
              <div className="space-y-4">
                {requests.map((request) => (
                  <div
                    key={request.id}
                    className="flex items-center justify-between p-4 glass-panel rounded-lg border-glass-border hover:bg-muted/10 transition-colors"
                  >
                    <div className="flex items-center gap-4">
                      <div className="w-12 h-12 bg-neon-blue/20 rounded-lg flex items-center justify-center">
                        <FileText className="h-6 w-6 text-neon-blue" />
                      </div>
                      <div>
                        <h3 className="font-semibold">{request.employeeName}</h3>
                        <div className="flex items-center gap-4 text-sm text-muted-foreground">
                          <span>ID: {request.employeeId}</span>
                          <span>•</span>
                          <span>Requested: {formatDate(request.createdAt)}</span>
                          <span>•</span>
                          <span>By: {request.requestedBy}</span>
                        </div>
                      </div>
                    </div>

                    <div className="flex items-center gap-3">
                      {getStatusBadge(request.status)}
                      <Button size="sm" variant="outline">
                        View Details
                      </Button>
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              <div className="text-center py-12">
                <FileText className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
                <h3 className="text-lg font-semibold mb-2">No Requests Found</h3>
                <p className="text-muted-foreground">
                  No {tabName.toLowerCase()} requests have been submitted yet.
                </p>
              </div>
            )}
          </CardContent>
        </Card>
      </TabsContent>

      <TabsContent value="history" className="space-y-6">
        <Card className="glass-panel border-glass-border">
          <CardHeader>
            <CardTitle>Generated Documents History</CardTitle>
            <CardDescription>
              View previously generated and sent documents
            </CardDescription>
          </CardHeader>
          <CardContent>
            {loading ? (
              <div className="space-y-4">
                {Array.from({ length: 3 }).map((_, i) => (
                  <div key={i} className="flex items-center justify-between p-4 glass-panel rounded-lg animate-pulse">
                    <div className="space-y-2">
                      <div className="h-4 bg-muted rounded w-32" />
                      <div className="h-3 bg-muted rounded w-24" />
                    </div>
                    <div className="h-6 bg-muted rounded w-20" />
                  </div>
                ))}
              </div>
            ) : requests && requests.length > 0 ? (
              <div className="space-y-4">
                {requests
                  .filter(request => request.status === 'approved')
                  .map((request) => (
                    <div
                      key={request.id}
                      className="flex items-center justify-between p-4 glass-panel rounded-lg border-glass-border hover:bg-muted/10 transition-colors"
                    >
                      <div className="flex items-center gap-4">
                        <div className="w-12 h-12 bg-green-500/20 rounded-lg flex items-center justify-center">
                          <Clock className="h-6 w-6 text-green-500" />
                        </div>
                        <div>
                          <h3 className="font-semibold">{request.employeeName}</h3>
                          <div className="flex items-center gap-4 text-sm text-muted-foreground">
                            <span>ID: {request.employeeId}</span>
                            <span>•</span>
                            <span>Generated: {formatDate(request.createdAt)}</span>
                            <span>•</span>
                            <span>By: {request.requestedBy}</span>
                          </div>
                        </div>
                      </div>

                      <div className="flex items-center gap-3">
                        {getStatusBadge(request.status)}
                        <Button size="sm" variant="outline">
                          View Document
                        </Button>
                      </div>
                    </div>
                  ))}
                {requests.filter(request => request.status === 'approved').length === 0 && (
                  <div className="text-center py-12">
                    <Clock className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
                    <h3 className="text-lg font-semibold mb-2">No History Found</h3>
                    <p className="text-muted-foreground">
                      No approved {tabName.toLowerCase()} documents found in history.
                    </p>
                  </div>
                )}
              </div>
            ) : (
              <div className="text-center py-12">
                <Clock className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
                <h3 className="text-lg font-semibold mb-2">No History Found</h3>
                <p className="text-muted-foreground">
                  No {tabName.toLowerCase()} documents have been generated yet.
                </p>
              </div>
            )}
          </CardContent>
        </Card>
      </TabsContent>
    </Tabs>
  );
}
