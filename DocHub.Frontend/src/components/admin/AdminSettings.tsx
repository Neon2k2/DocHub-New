import React, { useState, useEffect } from 'react';
import { Settings, Users, Shield, Database, Activity, Bell, Save } from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../ui/card';
import { Button } from '../ui/button';
import { Switch } from '../ui/switch';
import { Label } from '../ui/label';
import { Input } from '../ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../ui/select';
import { Badge } from '../ui/badge';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '../ui/table';
import { Loading } from '../ui/loading';
import { storageService } from '../../services/storage.service';
import { notify } from '../../utils/notifications';
import { handleError } from '../../utils/errorHandler';
import { apiService, UserRole } from '../../services/api.service';

export function AdminSettings() {
  const [emailNotifications, setEmailNotifications] = useState(true);
  const [autoBackup, setAutoBackup] = useState(true);
  const [debugMode, setDebugMode] = useState(false);
  const [saving, setSaving] = useState(false);
  const [users, setUsers] = useState<UserRole[]>([]);
  const [usersLoading, setUsersLoading] = useState(true);
  const [settings, setSettings] = useState({
    emailNotifications: true,
    autoBackup: true,
    debugMode: false,
    backupFrequency: 'daily',
    emailFrequency: 'immediate',
    systemName: 'DocHub',
    maxFileSize: '10',
    sessionTimeout: '30',
    erPermissions: {
      viewLetters: true,
      generateLetters: true,
      createTemplates: false,
      adminFunctions: false
    },
    billingPermissions: {
      viewTimesheets: true,
      downloadTimesheets: true,
      processBilling: false,
      adminFunctions: false
    }
  });

  // Load settings on component mount
  useEffect(() => {
    loadSettings();
    loadUsers();
  }, []);

  const loadSettings = () => {
    const savedSettings = storageService.getAdminSettings();
    if (Object.keys(savedSettings).length > 0) {
      setSettings(savedSettings);
      setEmailNotifications(savedSettings.emailNotifications);
      setAutoBackup(savedSettings.autoBackup);
      setDebugMode(savedSettings.debugMode);
    }
  };

  const loadUsers = async () => {
    setUsersLoading(true);
    try {
      const response = await apiService.getUsers();
      if (response.success && response.data) {
        setUsers(response.data);
      }
    } catch (error) {
      handleError(error, 'Load users');
    } finally {
      setUsersLoading(false);
    }
  };

  const handleSaveSettings = async () => {
    setSaving(true);
    try {
      const updatedSettings = {
        ...settings,
        emailNotifications,
        autoBackup,
        debugMode
      };
      
      // Simulate API call
      await new Promise(resolve => setTimeout(resolve, 1500));
      
      // Save to storage
      storageService.saveAdminSettings(updatedSettings);
      setSettings(updatedSettings);
      
      notify.settingsSaved();
    } catch (error) {
      handleError(error, 'Save admin settings');
    } finally {
      setSaving(false);
    }
  };

  const handleSettingChange = (key: string, value: any) => {
    setSettings(prev => ({
      ...prev,
      [key]: value
    }));
  };

  const handlePermissionChange = (module: 'er' | 'billing', permission: string, value: boolean) => {
    setSettings(prev => ({
      ...prev,
      [`${module}Permissions`]: {
        ...prev[`${module}Permissions`],
        [permission]: value
      }
    }));
  };



  const getRoleBadge = (role: string) => {
    switch (role) {
      case 'admin':
        return <Badge className="bg-red-500/20 text-red-400 border-red-500/30">Admin</Badge>;
      case 'er':
        return <Badge className="bg-blue-500/20 text-blue-400 border-blue-500/30">ER User</Badge>;
      case 'billing':
        return <Badge className="bg-green-500/20 text-green-400 border-green-500/30">Billing User</Badge>;
      default:
        return <Badge variant="secondary">{role}</Badge>;
    }
  };

  const getStatusBadge = (status: string) => {
    return status === 'active' 
      ? <Badge className="bg-green-500/20 text-green-400 border-green-500/30">Active</Badge>
      : <Badge className="bg-gray-500/20 text-gray-400 border-gray-500/30">Inactive</Badge>;
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Admin Settings</h1>
          <p className="text-muted-foreground">Manage system configuration and user permissions</p>
        </div>
        <Button 
          onClick={handleSaveSettings}
          disabled={saving}
          className="border border-blue-500 bg-blue-50 text-blue-700 hover:bg-blue-100 dark:bg-blue-950 dark:text-blue-300 dark:hover:bg-blue-900 transition-all duration-300"
        >
          {saving ? (
            <Loading size="sm" className="mr-2" />
          ) : (
            <Save className="mr-2 h-4 w-4" />
          )}
          {saving ? 'Saving...' : 'Save Settings'}
        </Button>
      </div>

      {/* System Configuration */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Card className="glass-panel border-glass-border">
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Settings className="h-5 w-5 text-blue-600" />
              System Configuration
            </CardTitle>
            <CardDescription>
              Configure global system settings
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-6">
            <div className="flex items-center justify-between">
              <div className="space-y-0.5">
                <Label>Email Notifications</Label>
                <p className="text-sm text-muted-foreground">
                  Send system notifications via email
                </p>
              </div>
              <Switch
                checked={emailNotifications}
                onCheckedChange={setEmailNotifications}
              />
            </div>

            <div className="flex items-center justify-between">
              <div className="space-y-0.5">
                <Label>Auto Backup</Label>
                <p className="text-sm text-muted-foreground">
                  Automatically backup data daily
                </p>
              </div>
              <Switch
                checked={autoBackup}
                onCheckedChange={setAutoBackup}
              />
            </div>

            <div className="flex items-center justify-between">
              <div className="space-y-0.5">
                <Label>Debug Mode</Label>
                <p className="text-sm text-muted-foreground">
                  Enable detailed logging
                </p>
              </div>
              <Switch
                checked={debugMode}
                onCheckedChange={setDebugMode}
              />
            </div>

            <div className="flex items-center justify-between">
              <div className="space-y-0.5">
                <Label>Backup Frequency</Label>
                <p className="text-sm text-muted-foreground">
                  How often to backup system data
                </p>
              </div>
              <Select 
                value={settings.backupFrequency} 
                onValueChange={(value) => handleSettingChange('backupFrequency', value)}
              >
                <SelectTrigger className="w-32">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="hourly">Hourly</SelectItem>
                  <SelectItem value="daily">Daily</SelectItem>
                  <SelectItem value="weekly">Weekly</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="flex items-center justify-between">
              <div className="space-y-0.5">
                <Label>Email Frequency</Label>
                <p className="text-sm text-muted-foreground">
                  How often to send email notifications
                </p>
              </div>
              <Select 
                value={settings.emailFrequency} 
                onValueChange={(value) => handleSettingChange('emailFrequency', value)}
              >
                <SelectTrigger className="w-32">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="immediate">Immediate</SelectItem>
                  <SelectItem value="hourly">Hourly</SelectItem>
                  <SelectItem value="daily">Daily</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <Label>Session Timeout (minutes)</Label>
              <Input
                type="number"
                defaultValue="30"
                className="glass-panel border-glass-border"
              />
            </div>

            <div className="space-y-2">
              <Label>Max Upload Size (MB)</Label>
              <Select defaultValue="10">
                <SelectTrigger className="glass-panel border-glass-border">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent className="glass-panel border-glass-border">
                  <SelectItem value="5">5 MB</SelectItem>
                  <SelectItem value="10">10 MB</SelectItem>
                  <SelectItem value="25">25 MB</SelectItem>
                  <SelectItem value="50">50 MB</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </CardContent>
        </Card>

        <Card className="glass-panel border-glass-border">
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Activity className="h-5 w-5 text-green-600" />
              System Status
            </CardTitle>
            <CardDescription>
              Monitor system health and performance
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="space-y-2">
              <div className="flex justify-between items-center">
                <span className="text-sm">Database Status</span>
                <Badge className="bg-green-500/20 text-green-400 border-green-500/30">Online</Badge>
              </div>
              <div className="flex justify-between items-center">
                <span className="text-sm">Email Service</span>
                <Badge className="bg-green-500/20 text-green-400 border-green-500/30">Active</Badge>
              </div>
              <div className="flex justify-between items-center">
                <span className="text-sm">File Storage</span>
                <Badge className="bg-yellow-500/20 text-yellow-400 border-yellow-500/30">Warning</Badge>
              </div>
              <div className="flex justify-between items-center">
                <span className="text-sm">API Gateway</span>
                <Badge className="bg-green-500/20 text-green-400 border-green-500/30">Healthy</Badge>
              </div>
            </div>

            <div className="pt-4 space-y-2">
              <div className="flex justify-between items-center">
                <span className="text-sm">CPU Usage</span>
                <span className="text-sm">45%</span>
              </div>
              <div className="w-full bg-muted rounded-full h-2">
                <div className="bg-neon-blue h-2 rounded-full" style={{ width: '45%' }}></div>
              </div>
            </div>

            <div className="space-y-2">
              <div className="flex justify-between items-center">
                <span className="text-sm">Memory Usage</span>
                <span className="text-sm">67%</span>
              </div>
              <div className="w-full bg-muted rounded-full h-2">
                <div className="bg-neon-green h-2 rounded-full" style={{ width: '67%' }}></div>
              </div>
            </div>

            <div className="space-y-2">
              <div className="flex justify-between items-center">
                <span className="text-sm">Storage Usage</span>
                <span className="text-sm">89%</span>
              </div>
              <div className="w-full bg-muted rounded-full h-2">
                <div className="bg-yellow-500 h-2 rounded-full" style={{ width: '89%' }}></div>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* User Management */}
      <Card className="glass-panel border-glass-border">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Users className="h-5 w-5 text-neon-purple" />
            User Management
          </CardTitle>
          <CardDescription>
            Manage user accounts and permissions
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="flex justify-between items-center mb-6">
            <div className="text-sm text-muted-foreground">
              {users.length} total users
            </div>
            <Button className="border border-blue-500 bg-blue-50 text-blue-700 hover:bg-blue-100 dark:bg-blue-950 dark:text-blue-300 dark:hover:bg-blue-900 transition-all duration-300">
              Add New User
            </Button>
          </div>

          {usersLoading ? (
            <div className="flex justify-center items-center h-32">
              <Loading />
            </div>
          ) : (
            <div className="glass-panel rounded-lg border-glass-border overflow-hidden">
              <Table>
                <TableHeader>
                  <TableRow className="border-glass-border hover:bg-muted/50">
                    <TableHead>User</TableHead>
                    <TableHead>Role</TableHead>
                    <TableHead>Last Login</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead>Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {users.map((user) => (
                  <TableRow 
                    key={user.id} 
                    className="border-glass-border hover:bg-muted/50 transition-colors"
                  >
                    <TableCell>
                      <div>
                        <div className="font-medium">{user.name}</div>
                        <div className="text-sm text-muted-foreground">{user.email}</div>
                      </div>
                    </TableCell>
                    <TableCell>{getRoleBadge(user.role)}</TableCell>
                    <TableCell>{user.lastLogin ? user.lastLogin.toLocaleString() : 'Never'}</TableCell>
                    <TableCell>{getStatusBadge(user.isActive ? 'active' : 'inactive')}</TableCell>
                    <TableCell>
                      <div className="flex items-center gap-2">
                        <Button
                          variant="ghost"
                          size="sm"
                          className="neon-glow hover:text-neon-blue"
                        >
                          Edit
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          className="neon-glow hover:text-red-400"
                        >
                          Suspend
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
          )}
        </CardContent>
      </Card>

      {/* Module Permissions */}
      <Card className="glass-panel border-glass-border">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Shield className="h-5 w-5 text-neon-pink" />
            Module Permissions
          </CardTitle>
          <CardDescription>
            Configure access permissions for different modules
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div className="space-y-4">
              <h4 className="font-medium">Employee Relations Module</h4>
              <div className="space-y-2">
                <div className="flex items-center justify-between">
                  <Label className="text-sm">View Letters</Label>
                  <Switch 
                    checked={settings.erPermissions.viewLetters}
                    onCheckedChange={(value) => handlePermissionChange('er', 'viewLetters', value)}
                  />
                </div>
                <div className="flex items-center justify-between">
                  <Label className="text-sm">Generate Letters</Label>
                  <Switch 
                    checked={settings.erPermissions.generateLetters}
                    onCheckedChange={(value) => handlePermissionChange('er', 'generateLetters', value)}
                  />
                </div>
                <div className="flex items-center justify-between">
                  <Label className="text-sm">Create Templates</Label>
                  <Switch 
                    checked={settings.erPermissions.createTemplates}
                    onCheckedChange={(value) => handlePermissionChange('er', 'createTemplates', value)}
                  />
                </div>
                <div className="flex items-center justify-between">
                  <Label className="text-sm">Admin Functions</Label>
                  <Switch 
                    checked={settings.erPermissions.adminFunctions}
                    onCheckedChange={(value) => handlePermissionChange('er', 'adminFunctions', value)}
                  />
                </div>
              </div>
            </div>

            <div className="space-y-4">
              <h4 className="font-medium">Billing Module</h4>
              <div className="space-y-2">
                <div className="flex items-center justify-between">
                  <Label className="text-sm">Download Timesheets</Label>
                  <Switch 
                    checked={settings.billingPermissions.downloadTimesheets}
                    onCheckedChange={(value) => handlePermissionChange('billing', 'downloadTimesheets', value)}
                  />
                </div>
                <div className="flex items-center justify-between">
                  <Label className="text-sm">Merge Data</Label>
                  <Switch 
                    checked={settings.billingPermissions.processBilling}
                    onCheckedChange={(value) => handlePermissionChange('billing', 'processBilling', value)}
                  />
                </div>
                <div className="flex items-center justify-between">
                  <Label className="text-sm">Send Bulk Emails</Label>
                  <Switch 
                    checked={settings.billingPermissions.adminFunctions}
                    onCheckedChange={(value) => handlePermissionChange('billing', 'adminFunctions', value)}
                  />
                </div>
                <div className="flex items-center justify-between">
                  <Label className="text-sm">View Error Logs</Label>
                  <Switch 
                    checked={settings.billingPermissions.viewTimesheets}
                    onCheckedChange={(value) => handlePermissionChange('billing', 'viewTimesheets', value)}
                  />
                </div>
              </div>
            </div>
          </div>


        </CardContent>
      </Card>
    </div>
  );
}