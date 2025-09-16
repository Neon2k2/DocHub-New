import React, { useState, useEffect } from 'react';
import { Bell, CheckCircle, AlertCircle, Clock, Mail, X, Eye, ExternalLink } from 'lucide-react';
import { Button } from './ui/button';
import { Badge } from './ui/badge';
import { DropdownMenu, DropdownMenuContent, DropdownMenuTrigger } from './ui/dropdown-menu';
import { ScrollArea } from './ui/scroll-area';
import { Card, CardContent } from './ui/card';
import { signalRService, EmailStatusUpdate } from '../services/signalr.service';
import { useAuth } from '../contexts/AuthContext';
import { toast } from 'sonner';
import { Skeleton, SkeletonNotification } from './ui/skeleton';

interface NotificationItem {
  id: string;
  title: string;
  message: string;
  type: 'email_sent' | 'email_delivered' | 'email_opened' | 'email_clicked' | 'email_bounced' | 'email_dropped' | 'error';
  timestamp: Date;
  isRead: boolean;
  emailJobId?: string;
  letterTypeDefinitionId?: string;
  employeeName?: string;
}

interface NotificationDropdownProps {
  onNavigateToTab?: (tabId: string, emailJobId?: string) => void;
}

export function NotificationDropdown({ onNavigateToTab }: NotificationDropdownProps) {
  const [notifications, setNotifications] = useState<NotificationItem[]>([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [isOpen, setIsOpen] = useState(false);
  const [loading, setLoading] = useState(false);
  const { user } = useAuth();

  useEffect(() => {
    if (!user?.id) return;

    // Initialize SignalR connection for email status updates
    const initializeSignalR = async () => {
      try {
        console.log('🔔 [NOTIFICATION_DROPDOWN] Initializing SignalR connection');
        await signalRService.start();
        console.log('✅ [NOTIFICATION_DROPDOWN] SignalR connection started successfully');
        
        // Listen for email status updates
        signalRService.onEmailStatusUpdated((update: EmailStatusUpdate) => {
          console.log('📧 [NOTIFICATION_DROPDOWN] Received email status update:', update);
          addEmailStatusNotification(update);
        });
        console.log('👂 [NOTIFICATION_DROPDOWN] SignalR listener registered for email status updates');
        
        // Manually join user group as backup
        if (user?.id) {
          await signalRService.joinUserGroup(user.id);
        }
      } catch (error) {
        console.error('❌ [NOTIFICATION_DROPDOWN] Failed to initialize SignalR for notifications:', error);
      }
    };

    initializeSignalR();

    // Cleanup on unmount
    return () => {
      signalRService.offEmailStatusUpdated();
    };
  }, [user?.id]);

  const addEmailStatusNotification = (update: EmailStatusUpdate) => {
    console.log('📝 [NOTIFICATION_DROPDOWN] Adding email status notification:', update);
    
    // Show notifications for all status updates except pending
    if (update.status === 'pending') {
      console.log('📝 [NOTIFICATION_DROPDOWN] Skipping notification for status:', update.status);
      return;
    }
    
    const notification: NotificationItem = {
      id: `email-${update.emailJobId}-${update.status}-${Date.now()}`,
      title: getNotificationTitle(update.status),
      message: getNotificationMessage(update.status, update.employeeName || 'Unknown'),
      type: getNotificationType(update.status),
      timestamp: new Date(update.timestamp),
      isRead: false,
      emailJobId: update.emailJobId,
      letterTypeDefinitionId: update.letterTypeDefinitionId,
      employeeName: update.employeeName
    };

    console.log('📋 [NOTIFICATION_DROPDOWN] Created notification item:', notification);
    
    setNotifications(prev => {
      // Remove any existing notification for the same email job and status
      const filtered = prev.filter(notif => 
        !(notif.emailJobId === update.emailJobId && notif.type === notification.type)
      );
      const newNotifications = [notification, ...filtered].slice(0, 10); // Keep last 10 notifications
      console.log('📊 [NOTIFICATION_DROPDOWN] Updated notifications count:', newNotifications.length);
      return newNotifications;
    });
    
    setUnreadCount(prev => {
      const newCount = prev + 1;
      console.log('🔢 [NOTIFICATION_DROPDOWN] Updated unread count:', newCount);
      return newCount;
    });

    // Show toast notification based on status
    const toastMessage = getNotificationMessage(update.status, update.employeeName || 'employee');
    if (update.status === 'sent') {
      toast.success(`📧 ${toastMessage}`);
    } else if (update.status === 'delivered') {
      toast.success(`✅ ${toastMessage}`);
    } else if (update.status === 'bounced' || update.status === 'dropped') {
      toast.error(`❌ ${toastMessage}`);
    } else {
      toast.info(`ℹ️ ${toastMessage}`);
    }
  };

  const getNotificationTitle = (status: string): string => {
    switch (status) {
      case 'sent': return 'Email Sent';
      case 'delivered': return 'Email Delivered';
      case 'opened': return 'Email Opened';
      case 'clicked': return 'Email Clicked';
      case 'bounced': return 'Email Bounced';
      case 'dropped': return 'Email Dropped';
      case 'failed': return 'Email Failed';
      default: return 'Email Update';
    }
  };

  const getNotificationMessage = (status: string, employeeName: string): string => {
    switch (status) {
      case 'sent': return `Email sent to ${employeeName}`;
      case 'delivered': return `Email delivered to ${employeeName}`;
      case 'opened': return `${employeeName} opened the email`;
      case 'clicked': return `${employeeName} clicked a link in the email`;
      case 'bounced': return `Email bounced for ${employeeName}`;
      case 'dropped': return `Email dropped for ${employeeName}`;
      case 'failed': return `Email failed to send to ${employeeName}`;
      default: return `Email status updated for ${employeeName}`;
    }
  };

  const getNotificationType = (status: string): NotificationItem['type'] => {
    switch (status) {
      case 'sent': return 'email_sent';
      case 'delivered': return 'email_delivered';
      case 'opened': return 'email_opened';
      case 'clicked': return 'email_clicked';
      case 'bounced': return 'email_bounced';
      case 'dropped': return 'email_dropped';
      case 'failed': return 'error';
      default: return 'email_sent';
    }
  };

  const getNotificationIcon = (type: NotificationItem['type']) => {
    switch (type) {
      case 'email_sent': return <Mail className="h-4 w-4 text-blue-500" />;
      case 'email_delivered': return <CheckCircle className="h-4 w-4 text-green-500" />;
      case 'email_opened': return <Eye className="h-4 w-4 text-purple-500" />;
      case 'email_clicked': return <CheckCircle className="h-4 w-4 text-indigo-500" />;
      case 'email_bounced': return <AlertCircle className="h-4 w-4 text-red-500" />;
      case 'email_dropped': return <AlertCircle className="h-4 w-4 text-orange-500" />;
      case 'error': return <AlertCircle className="h-4 w-4 text-red-500" />;
      default: return <Mail className="h-4 w-4 text-gray-500" />;
    }
  };

  const markAsRead = (notificationId: string) => {
    setNotifications(prev => 
      prev.map(notif => 
        notif.id === notificationId 
          ? { ...notif, isRead: true }
          : notif
      )
    );
    setUnreadCount(prev => Math.max(0, prev - 1));
  };

  const handleNotificationClick = (notification: NotificationItem) => {
    console.log('🔔 [NOTIFICATION_DROPDOWN] Notification clicked:', notification);
    console.log('🔔 [NOTIFICATION_DROPDOWN] LetterTypeDefinitionId:', notification.letterTypeDefinitionId);
    console.log('🔔 [NOTIFICATION_DROPDOWN] EmailJobId:', notification.emailJobId);
    console.log('🔔 [NOTIFICATION_DROPDOWN] onNavigateToTab function:', onNavigateToTab);
    
    // Mark as read
    markAsRead(notification.id);
    
    // Navigate to the tab and open history
    if (notification.letterTypeDefinitionId && onNavigateToTab) {
      console.log('🔔 [NOTIFICATION_DROPDOWN] Calling onNavigateToTab with:', notification.letterTypeDefinitionId, notification.emailJobId);
      onNavigateToTab(notification.letterTypeDefinitionId, notification.emailJobId);
      setIsOpen(false); // Close the dropdown
      console.log('🔔 [NOTIFICATION_DROPDOWN] Navigation called, dropdown closed');
    } else {
      console.log('🔔 [NOTIFICATION_DROPDOWN] Cannot navigate - missing letterTypeDefinitionId or onNavigateToTab function');
    }
  };

  const markAllAsRead = () => {
    setNotifications(prev => 
      prev.map(notif => ({ ...notif, isRead: true }))
    );
    setUnreadCount(0);
  };

  const clearAll = () => {
    setNotifications([]);
    setUnreadCount(0);
  };

  const formatTimeAgo = (timestamp: Date): string => {
    const now = new Date();
    const diffInSeconds = Math.floor((now.getTime() - timestamp.getTime()) / 1000);
    
    if (diffInSeconds < 60) return 'Just now';
    if (diffInSeconds < 3600) return `${Math.floor(diffInSeconds / 60)}m ago`;
    if (diffInSeconds < 86400) return `${Math.floor(diffInSeconds / 3600)}h ago`;
    return `${Math.floor(diffInSeconds / 86400)}d ago`;
  };

  return (
    <DropdownMenu open={isOpen} onOpenChange={setIsOpen}>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="sm" className="hover:text-blue-600 relative">
          <Bell className={`h-4 w-4 ${unreadCount > 0 ? 'animate-pulse' : ''}`} />
          {unreadCount > 0 && (
            <Badge className="absolute -top-1 -right-1 h-5 w-5 rounded-full p-0 bg-pink-500 border-0 text-xs flex items-center justify-center animate-bounce">
              {unreadCount > 99 ? '99+' : unreadCount}
            </Badge>
          )}
        </Button>
      </DropdownMenuTrigger>
      
      <DropdownMenuContent align="end" className="w-80 p-0">
        <Card className="border-0 shadow-lg">
          <div className="flex items-center justify-between p-4 border-b">
            <h3 className="font-semibold">Notifications</h3>
            <div className="flex items-center gap-2">
              {unreadCount > 0 && (
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={markAllAsRead}
                  className="text-xs h-6 px-2"
                >
                  Mark all read
                </Button>
              )}
              {notifications.length > 0 && (
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={clearAll}
                  className="text-xs h-6 px-2 text-red-500 hover:text-red-700"
                >
                  Clear all
                </Button>
              )}
            </div>
          </div>
          
          <CardContent className="p-0">
            <ScrollArea className="h-96">
              {loading ? (
                <div className="space-y-1 p-2">
                  {Array.from({ length: 3 }).map((_, i) => (
                    <SkeletonNotification key={i} />
                  ))}
                </div>
              ) : notifications.length === 0 ? (
                <div className="text-center py-12">
                  <Bell className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
                  <h3 className="text-lg font-semibold mb-2">No Notifications</h3>
                  <p className="text-muted-foreground text-sm">
                    Email status updates will appear here.
                  </p>
                </div>
              ) : (
                <div className="space-y-1">
                  {notifications.map((notification) => (
                    <div
                      key={notification.id}
                      className={`p-3 hover:bg-muted/50 cursor-pointer transition-colors group ${
                        !notification.isRead ? 'bg-blue-50/50 border-l-2 border-l-blue-500' : ''
                      }`}
                      onClick={() => handleNotificationClick(notification)}
                    >
                      <div className="flex items-start gap-3">
                        <div className="flex-shrink-0 mt-0.5">
                          {getNotificationIcon(notification.type)}
                        </div>
                        
                        <div className="flex-1 min-w-0">
                          <div className="flex items-center justify-between">
                            <p className="font-medium text-sm">{notification.title}</p>
                            <div className="flex items-center gap-2">
                              <span className="text-xs text-muted-foreground">
                                {formatTimeAgo(notification.timestamp)}
                              </span>
                              {notification.letterTypeDefinitionId && (
                                <ExternalLink className="h-3 w-3 text-muted-foreground group-hover:text-blue-500 transition-colors" />
                              )}
                            </div>
                          </div>
                          <p className="text-sm text-muted-foreground mt-1">
                            {notification.message}
                          </p>
                          {notification.letterTypeDefinitionId && (
                            <p className="text-xs text-blue-600 mt-1 group-hover:text-blue-700">
                              Click to view in history
                            </p>
                          )}
                        </div>
                        
                        {!notification.isRead && (
                          <div className="w-2 h-2 bg-blue-500 rounded-full flex-shrink-0 mt-2" />
                        )}
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </ScrollArea>
          </CardContent>
        </Card>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
