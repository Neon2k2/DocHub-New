import React, { useState, useEffect } from 'react';
import { Users, Shield, UserPlus, Eye, Activity, Clock, Settings, Lock, Trash2 } from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../ui/card';
import { Button } from '../ui/button';
import { Badge } from '../ui/badge';
import { Avatar, AvatarFallback } from '../ui/avatar';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle, DialogTrigger } from '../ui/dialog';
import { Input } from '../ui/input';
import { Label } from '../ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../ui/select';
import { Switch } from '../ui/switch';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../ui/tabs';
import { toast } from 'sonner';
import { notify } from '../../utils/notifications';
import { apiService, CreateUserRequest, UpdateUserRequest } from '../../services/api.service';
import { Loading } from '../ui/loading';

interface User {
  id: string;
  username: string;
  name: string;
  email: string;
  role: 'admin' | 'er' | 'billing';
  isActive: boolean;
  lastLogin: Date | null;
  createdAt: Date;
  permissions: {
    isAdmin: boolean;
    canAccessER: boolean;
    canAccessBilling: boolean;
  };
}

interface UserSession {
  id: string;
  userId: string;
  userName: string;
  userRole: string;
  loginTime: Date;
  lastActivity: Date;
  ipAddress: string;
  userAgent: string;
  isActive: boolean;
}

export function UserManagement() {
  const [users, setUsers] = useState<User[]>([]);
  const [sessions, setSessions] = useState<UserSession[]>([]);
  const [loading, setLoading] = useState(true);
  const [sessionsLoading, setSessionsLoading] = useState(true);

  const [showCreateUserDialog, setShowCreateUserDialog] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [formData, setFormData] = useState({
    username: '',
    name: '',
    email: '',
    role: 'er' as 'admin' | 'er' | 'billing',
    isActive: true
  });

  useEffect(() => {
    loadUsers();
    loadSessions();
  }, []);

  const loadUsers = async () => {
    setLoading(true);
    try {
      const response = await apiService.getUsers();
      if (response.success && response.data) {
        setUsers(response.data);
      }
    } catch (error) {
      notify.error('Failed to load users');
    } finally {
      setLoading(false);
    }
  };

  const loadSessions = async () => {
    setSessionsLoading(true);
    try {
      // Note: This would need to be implemented in the API service
      // For now, we'll keep the sessions empty or implement a mock
      setSessions([]);
    } catch (error) {
      notify.error('Failed to load sessions');
    } finally {
      setSessionsLoading(false);
    }
  };



  const handleToggleUserStatus = async (userId: string) => {
    const user = users.find(u => u.id === userId);
    if (!user) return;

    try {
      const updateData: UpdateUserRequest = {
        isActive: !user.isActive
      };
      
      const response = await apiService.updateUser(userId, updateData);
      if (response.success) {
        setUsers(users.map(u => 
          u.id === userId ? { ...u, isActive: !u.isActive } : u
        ));
        notify.success('User status updated');
      }
    } catch (error) {
      notify.error('Failed to update user status');
    }
  };

  const handleTerminateSession = async (sessionId: string) => {
    try {
      // Note: This would need to be implemented in the API service
      setSessions(sessions.map(session => 
        session.id === sessionId ? { ...session, isActive: false } : session
      ));
      notify.success('Session terminated');
    } catch (error) {
      notify.error('Failed to terminate session');
    }
  };

  const handleEditUser = (user: User) => {
    setFormData({
      username: user.username,
      name: user.name,
      email: user.email,
      role: user.role,
      isActive: user.isActive
    });
    setShowCreateUserDialog(true);
  };

  const handleDeleteUser = (userId: string) => {
    const user = users.find(u => u.id === userId);
    if (!user) return;

    notify.confirmAction(
      `Are you sure you want to delete user "${user.name}"?`,
      async () => {
        try {
          const response = await apiService.deleteUser(userId);
          if (response.success) {
            setUsers(users.filter(u => u.id !== userId));
            notify.success('User deleted successfully');
          }
        } catch (error) {
          notify.error('Failed to delete user');
        }
      }
    );
  };

  const handleResetPassword = async (userId: string) => {
    const user = users.find(u => u.id === userId);
    if (!user) return;

    setSubmitting(true);
    try {
      const response = await apiService.resetUserPassword(userId);
      if (response.success) {
        notify.success(`Password reset email sent to ${user.email}`);
      }
    } catch (error) {
      notify.error('Failed to reset password');
    } finally {
      setSubmitting(false);
    }
  };

  const handleUpdateUser = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    
    try {
      const existingUser = users.find(u => u.username === formData.username);
      
      if (existingUser) {
        // Update existing user
        const updateData: UpdateUserRequest = {
          name: formData.name,
          email: formData.email,
          role: formData.role,
          isActive: formData.isActive,
          permissions: {
            isAdmin: formData.role === 'admin',
            canAccessER: formData.role === 'admin' || formData.role === 'er',
            canAccessBilling: formData.role === 'admin' || formData.role === 'billing'
          }
        };
        
        const response = await apiService.updateUser(existingUser.id, updateData);
        if (response.success && response.data) {
          setUsers(users.map(user => 
            user.id === existingUser.id ? response.data! : user
          ));
          notify.success('User updated successfully');
        }
      } else {
        // Create new user
        const createData: CreateUserRequest = {
          username: formData.username,
          name: formData.name,
          email: formData.email,
          role: formData.role,
          password: 'TempPassword123!', // This should be generated or set by admin
          permissions: {
            isAdmin: formData.role === 'admin',
            canAccessER: formData.role === 'admin' || formData.role === 'er',
            canAccessBilling: formData.role === 'admin' || formData.role === 'billing'
          }
        };
        
        const response = await apiService.createUser(createData);
        if (response.success && response.data) {
          setUsers([...users, response.data]);
          notify.success('User created successfully');
        }
      }
      
      setShowCreateUserDialog(false);
      setFormData({
        username: '',
        name: '',
        email: '',
        role: 'er',
        isActive: true
      });
    } catch (error) {
      notify.error('Failed to save user');
    } finally {
      setSubmitting(false);
    }
  };

  const getRoleIcon = (role: string) => {
    switch (role) {
      case 'admin':
        return <Shield className="h-4 w-4" />;
      case 'er':
        return <Users className="h-4 w-4" />;
      case 'billing':
        return <Activity className="h-4 w-4" />;
      default:
        return <Users className="h-4 w-4" />;
    }
  };

  const getRoleBadge = (role: string) => {
    switch (role) {
      case 'admin':
        return <Badge className="bg-red-500/20 text-red-400 border-red-500/30">Admin</Badge>;
      case 'er':
        return <Badge className="bg-blue-500/20 text-blue-400 border-blue-500/30">ER</Badge>;
      case 'billing':
        return <Badge className="bg-green-500/20 text-green-400 border-green-500/30">Billing</Badge>;
      default:
        return <Badge variant="outline">{role}</Badge>;
    }
  };

  const formatDate = (date: Date | null) => {
    if (!date) return 'Never';
    return new Intl.DateTimeFormat('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    }).format(date);
  };

  const getTimeAgo = (date: Date) => {
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / (1000 * 60));
    const diffHours = Math.floor(diffMs / (1000 * 60 * 60));
    const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));

    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    return `${diffDays}d ago`;
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1>User Management</h1>
          <p className="text-muted-foreground">
            Manage users, roles, permissions, and active sessions
          </p>
        </div>
        
        <Dialog open={showCreateUserDialog} onOpenChange={setShowCreateUserDialog}>
          <DialogTrigger asChild>
            <Button className="neon-glow">
              <UserPlus className="h-4 w-4 mr-2" />
              Create User
            </Button>
          </DialogTrigger>
          <DialogContent className="dialog-panel max-w-md max-h-[70vh] overflow-y-auto">
            <DialogHeader>
              <DialogTitle>Create New User</DialogTitle>
              <DialogDescription>
                Add a new user to the DocHub system
              </DialogDescription>
            </DialogHeader>
            
            <form onSubmit={handleUpdateUser} className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="username">Username</Label>
                <Input
                  id="username"
                  value={formData.username}
                  onChange={(e) => setFormData({ ...formData, username: e.target.value })}
                  placeholder="e.g., john_doe"
                  required
                />
              </div>
              
              <div className="space-y-2">
                <Label htmlFor="name">Full Name</Label>
                <Input
                  id="name"
                  value={formData.name}
                  onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                  placeholder="e.g., John Doe"
                  required
                />
              </div>
              
              <div className="space-y-2">
                <Label htmlFor="email">Email</Label>
                <Input
                  id="email"
                  type="email"
                  value={formData.email}
                  onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                  placeholder="e.g., john@dochub.com"
                  required
                />
              </div>
              
              <div className="space-y-2">
                <Label htmlFor="role">Role</Label>
                <Select value={formData.role} onValueChange={(value: 'admin' | 'er' | 'billing') => setFormData({ ...formData, role: value })}>
                  <SelectTrigger>
                    <SelectValue placeholder="Select role" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="er">ER User</SelectItem>
                    <SelectItem value="billing">Billing User</SelectItem>
                    <SelectItem value="admin">Administrator</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              
              <div className="flex items-center space-x-2">
                <Switch
                  id="isActive"
                  checked={formData.isActive}
                  onCheckedChange={(checked) => setFormData({ ...formData, isActive: checked })}
                />
                <Label htmlFor="isActive">Active User</Label>
              </div>
              
              <div className="flex gap-3 pt-4">
                <Button type="submit" disabled={submitting} className="flex-1">
                  {submitting ? 'Creating...' : 'Create User'}
                </Button>
                <Button 
                  type="button" 
                  variant="outline" 
                  onClick={() => setShowCreateUserDialog(false)}
                  className="flex-1"
                >
                  Cancel
                </Button>
              </div>
            </form>
          </DialogContent>
        </Dialog>
      </div>

      {/* Statistics */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
        <Card className="glass-panel border-glass-border">
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-muted-foreground">Total Users</p>
                <p className="text-2xl font-bold">{users.length}</p>
              </div>
              <Users className="h-8 w-8 text-neon-blue" />
            </div>
          </CardContent>
        </Card>

        <Card className="glass-panel border-glass-border">
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-muted-foreground">Active Users</p>
                <p className="text-2xl font-bold">{users.filter(u => u.isActive).length}</p>
              </div>
              <Eye className="h-8 w-8 text-green-400" />
            </div>
          </CardContent>
        </Card>

        <Card className="glass-panel border-glass-border">
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-muted-foreground">Active Sessions</p>
                <p className="text-2xl font-bold">{sessions.filter(s => s.isActive).length}</p>
              </div>
              <Activity className="h-8 w-8 text-orange-400" />
            </div>
          </CardContent>
        </Card>

        <Card className="glass-panel border-glass-border">
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-muted-foreground">Admins</p>
                <p className="text-2xl font-bold">{users.filter(u => u.role === 'admin').length}</p>
              </div>
              <Shield className="h-8 w-8 text-purple-400" />
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Main Content */}
      <Tabs defaultValue="users" className="space-y-6">
        <TabsList className="grid w-full grid-cols-2">
          <TabsTrigger value="users">Users & RBAC</TabsTrigger>
          <TabsTrigger value="sessions">Active Sessions</TabsTrigger>
        </TabsList>

        <TabsContent value="users" className="space-y-6">
          <Card className="glass-panel border-glass-border">
            <CardHeader>
              <CardTitle>Users & Role-Based Access Control</CardTitle>
              <CardDescription>
                Manage user accounts, roles, and permissions
              </CardDescription>
            </CardHeader>
            <CardContent>
              {loading ? (
                <div className="flex justify-center items-center h-32">
                  <Loading />
                </div>
              ) : (
                <div className="space-y-4">
                  {users.map((user) => (
                  <Card key={user.id} className="hover:bg-muted/10 transition-colors">
                    <CardContent className="p-4">
                      <div className="flex items-center justify-between">
                        <div className="flex items-center gap-4">
                          <Avatar>
                            <AvatarFallback className="bg-neon-blue/20 text-neon-blue">
                              {user.name.split(' ').map(n => n[0]).join('')}
                            </AvatarFallback>
                          </Avatar>
                          
                          <div>
                            <div className="flex items-center gap-2">
                              <h3 className="font-semibold">{user.name}</h3>
                              {getRoleBadge(user.role)}
                              {!user.isActive && (
                                <Badge variant="outline" className="text-red-400 border-red-500/30">
                                  Inactive
                                </Badge>
                              )}
                            </div>
                            <div className="flex items-center gap-4 text-sm text-muted-foreground mt-1">
                              <span>@{user.username}</span>
                              <span>•</span>
                              <span>{user.email}</span>
                              <span>•</span>
                              <span>Last login: {formatDate(user.lastLogin)}</span>
                            </div>
                            
                            <div className="flex items-center gap-2 mt-2">
                              <div className="text-xs text-muted-foreground">Permissions:</div>
                              {user.permissions.isAdmin && (
                                <Badge variant="outline" className="text-xs bg-red-500/10 border-red-500/30">
                                  <Shield className="h-3 w-3 mr-1" />
                                  Admin
                                </Badge>
                              )}
                              {user.permissions.canAccessER && (
                                <Badge variant="outline" className="text-xs bg-blue-500/10 border-blue-500/30">
                                  <Users className="h-3 w-3 mr-1" />
                                  ER
                                </Badge>
                              )}
                              {user.permissions.canAccessBilling && (
                                <Badge variant="outline" className="text-xs bg-green-500/10 border-green-500/30">
                                  <Activity className="h-3 w-3 mr-1" />
                                  Billing
                                </Badge>
                              )}
                            </div>
                          </div>
                        </div>
                        
                        <div className="flex items-center gap-2">
                          <Button
                            size="sm"
                            variant="outline"
                            onClick={() => handleToggleUserStatus(user.id)}
                          >
                            {user.isActive ? (
                              <>
                                <Lock className="h-3 w-3 mr-2" />
                                Deactivate
                              </>
                            ) : (
                              <>
                                <Eye className="h-3 w-3 mr-2" />
                                Activate
                              </>
                            )}
                          </Button>
                          
                          <Button 
                            size="sm" 
                            variant="outline"
                            onClick={() => handleEditUser(user)}
                          >
                            <Settings className="h-3 w-3 mr-2" />
                            Edit
                          </Button>
                          
                          <Button 
                            size="sm" 
                            variant="outline"
                            onClick={() => handleResetPassword(user.id)}
                            disabled={submitting}
                          >
                            <Lock className="h-3 w-3 mr-2" />
                            Reset Password
                          </Button>
                          
                          <Button 
                            size="sm" 
                            variant="outline"
                            onClick={() => handleDeleteUser(user.id)}
                            className="text-red-400 hover:text-red-300"
                          >
                            <Trash2 className="h-3 w-3 mr-2" />
                            Delete
                          </Button>
                        </div>
                      </div>
                    </CardContent>
                  </Card>
                  ))}
                </div>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="sessions" className="space-y-6">
          <Card className="glass-panel border-glass-border">
            <CardHeader>
              <CardTitle>Active User Sessions</CardTitle>
              <CardDescription>
                Monitor and manage active user sessions in real-time
              </CardDescription>
            </CardHeader>
            <CardContent>
              {sessionsLoading ? (
                <div className="flex justify-center items-center h-32">
                  <Loading />
                </div>
              ) : (
                <div className="space-y-4">
                  {sessions.filter(s => s.isActive).map((session) => (
                  <Card key={session.id} className="hover:bg-muted/10 transition-colors">
                    <CardContent className="p-4">
                      <div className="flex items-center justify-between">
                        <div className="flex items-center gap-4">
                          <div className="w-3 h-3 bg-green-400 rounded-full animate-pulse"></div>
                          
                          <div>
                            <div className="flex items-center gap-2">
                              <h3 className="font-semibold">{session.userName}</h3>
                              {getRoleBadge(session.userRole)}
                            </div>
                            <div className="flex items-center gap-4 text-sm text-muted-foreground mt-1">
                              <span>IP: {session.ipAddress}</span>
                              <span>•</span>
                              <span>{session.userAgent}</span>
                            </div>
                            <div className="flex items-center gap-4 text-sm text-muted-foreground mt-1">
                              <span>Login: {formatDate(session.loginTime)}</span>
                              <span>•</span>
                              <span>Last activity: {getTimeAgo(session.lastActivity)}</span>
                            </div>
                          </div>
                        </div>
                        
                        <div className="flex items-center gap-2">
                          <div className="text-right text-sm">
                            <div className="text-muted-foreground">Session Duration</div>
                            <div className="font-medium">
                              {Math.floor((new Date().getTime() - session.loginTime.getTime()) / (1000 * 60 * 60))}h{' '}
                              {Math.floor(((new Date().getTime() - session.loginTime.getTime()) % (1000 * 60 * 60)) / (1000 * 60))}m
                            </div>
                          </div>
                          
                          <Button
                            size="sm"
                            variant="outline"
                            onClick={() => handleTerminateSession(session.id)}
                            className="text-destructive hover:text-destructive"
                          >
                            <Lock className="h-3 w-3 mr-2" />
                            Terminate
                          </Button>
                        </div>
                      </div>
                    </CardContent>
                  </Card>
                  ))}
                  
                  {sessions.filter(s => s.isActive).length === 0 && (
                    <div className="text-center py-12">
                      <Activity className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
                      <h3 className="text-lg font-semibold mb-2">No Active Sessions</h3>
                      <p className="text-muted-foreground">
                        No users are currently logged into the system.
                      </p>
                    </div>
                  )}
                </div>
              )}
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}