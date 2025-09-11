import React, { useState, useEffect } from 'react';
import { Plus, Edit2, Trash2, FileText, Settings, Eye, Copy, Download, MoreVertical, Database, Table, Upload } from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../ui/card';
import { Button } from '../ui/button';
import { Badge } from '../ui/badge';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle, DialogTrigger } from '../ui/dialog';
import { Input } from '../ui/input';
import { Label } from '../ui/label';
import { Textarea } from '../ui/textarea';
import { Switch } from '../ui/switch';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '../ui/dropdown-menu';
import { Loading } from '../ui/loading';
import { toast } from 'sonner';
import { DynamicTab, tabService } from '../../services/tab.service';
import { notify } from '../../utils/notifications';
import { handleError } from '../../utils/errorHandler';

export function TabManagement() {
  const [tabs, setTabs] = useState<DynamicTab[]>([]);
  const [loading, setLoading] = useState(true);
  const [showCreateDialog, setShowCreateDialog] = useState(false);
  const [editingTab, setEditingTab] = useState<DynamicTab | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [formData, setFormData] = useState({
    name: '',
    description: '',
    letterType: '',
    isActive: true
  });

  // Data source type selection
  const [dataSource, setDataSource] = useState<'database' | 'excel'>('database');

  // Data table state
  const [showDataDialog, setShowDataDialog] = useState(false);
  const [selectedTab, setSelectedTab] = useState<DynamicTab | null>(null);
  const [tableData, setTableData] = useState<any[]>([]);
  const [tableHeaders, setTableHeaders] = useState<string[]>([]);
  const [dataLoading, setDataLoading] = useState(false);

  useEffect(() => {
    loadTabs();
  }, []);

  const loadTabs = async () => {
    try {
      setLoading(true);
      const data = await tabService.getActiveTabs();
      setTabs(data);
    } catch (error) {
      toast.error('Failed to load tabs');
    } finally {
      setLoading(false);
    }
  };

  const handleCreateTab = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (isSubmitting) return; // Prevent multiple submissions
    
    if (!formData.name || !formData.description || !formData.letterType) {
      toast.error('Please fill in all required fields');
      return;
    }
    
    try {
      setIsSubmitting(true);
      
      // Create metadata object with data source type configuration
      const metadata = {
        dataSourceType: dataSource, // Store the type of data source this tab will use
        templateConfig: {
          requiredFields: ['employeeName', 'employeeId']
        },
        emailConfig: {
          defaultSubject: `${formData.name} - {{employeeName}}`,
          allowAttachments: true
        }
      };

      const newTab = await tabService.createTab({
        name: formData.name,
        description: formData.description,
        letterType: formData.letterType.toLowerCase().replace(/\s+/g, '_'),
        isActive: formData.isActive,
        metadata
      });
      
      setTabs([...tabs, newTab]);
      setShowCreateDialog(false);
      resetForm();
      toast.success('Tab created successfully');
    } catch (error: any) {
      console.error('Failed to create tab:', error);
      toast.error(error.message || 'Failed to create tab');
    } finally {
      setIsSubmitting(false);
    }
  };



  const handleDeleteTab = async (tabId: string) => {
    const tab = tabs.find(t => t.id === tabId);
    if (!tab) return;

    notify.confirmAction(
      `Are you sure you want to delete tab "${tab.name}"?`,
      async () => {
        try {
          await tabService.deleteTab(tabId);
          setTabs(tabs.filter(tab => tab.id !== tabId));
          notify.success('Tab deleted successfully');
        } catch (error) {
          handleError(error, 'Delete tab');
        }
      }
    );
  };

  const handleEditTab = (tab: DynamicTab) => {
    setEditingTab(tab);
    setFormData({
      name: tab.name,
      description: tab.description,
      letterType: tab.letterType,
      isActive: tab.isActive
    });
    setShowCreateDialog(true);
  };

  const handleDuplicateTab = async (tab: DynamicTab) => {
    try {
      const duplicatedTab: DynamicTab = {
        ...tab,
        id: `tab_${Date.now()}`,
        name: `${tab.name} (Copy)`,
        createdAt: new Date()
      };
      
      await tabService.createTab(duplicatedTab);
      setTabs([...tabs, duplicatedTab]);
      notify.success(`Tab "${duplicatedTab.name}" created successfully`);
    } catch (error) {
      handleError(error, 'Duplicate tab');
    }
  };

  const handleExportTab = async (tab: DynamicTab) => {
    try {
      const exportData = {
        name: tab.name,
        description: tab.description,
        letterType: tab.letterType,
        isActive: tab.isActive,
        templates: tab.templates || [],
        signatures: tab.signatures || [],
        exportedAt: new Date().toISOString(),
        version: '1.0'
      };

      const blob = new Blob([JSON.stringify(exportData, null, 2)], { type: 'application/json' });
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `${tab.name.replace(/\s+/g, '_')}_export.json`;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      window.URL.revokeObjectURL(url);
      
      notify.success(`Tab "${tab.name}" exported successfully`);
    } catch (error) {
      handleError(error, 'Export tab');
    }
  };

  const handleImportTab = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;

    try {
      const text = await file.text();
      const importData = JSON.parse(text);
      
      // Validate import data
      if (!importData.name || !importData.letterType) {
        throw new Error('Invalid tab export file');
      }

      const importedTab: DynamicTab = {
        id: `tab_${Date.now()}`,
        name: `${importData.name} (Imported)`,
        description: importData.description || '',
        letterType: importData.letterType,
        isActive: importData.isActive !== false,
        templates: importData.templates || [],
        signatures: importData.signatures || [],
        createdAt: new Date()
      };

      await tabService.createTab(importedTab);
      setTabs([...tabs, importedTab]);
      notify.success(`Tab "${importedTab.name}" imported successfully`);
    } catch (error) {
      handleError(error, 'Import tab');
    }
  };

  const handleUpdateTab = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!editingTab) return;
    
    if (!formData.name || !formData.description || !formData.letterType) {
      notify.error('Please fill in all required fields');
      return;
    }
    
    try {
      const updatedTab: DynamicTab = {
        ...editingTab,
        name: formData.name,
        description: formData.description,
        letterType: formData.letterType,
        isActive: formData.isActive
      };
      
      await tabService.updateTab(editingTab.id, updatedTab);
      setTabs(tabs.map(tab => tab.id === editingTab.id ? updatedTab : tab));
      setShowCreateDialog(false);
      setEditingTab(null);
      setFormData({ name: '', description: '', letterType: '', isActive: true });
      notify.success('Tab updated successfully');
    } catch (error) {
      handleError(error, 'Update tab');
    }
  };

  const resetForm = () => {
    setFormData({
      name: '',
      description: '',
      letterType: '',
      isActive: true
    });
    setDataSource('database');
  };


  const handleViewData = async (tab: DynamicTab) => {
    setSelectedTab(tab);
    setShowDataDialog(true);
    setDataLoading(true);

    try {
      // Parse metadata to get data source type
      const metadata = typeof tab.metadata === 'string' 
        ? JSON.parse(tab.metadata) 
        : tab.metadata;

      if (metadata.dataSourceType === 'excel') {
        setTableHeaders(['Data Source Type']);
        setTableData([{ 
          'Data Source Type': 'Excel Upload - Upload functionality available in the tab interface' 
        }]);
      } else if (metadata.dataSourceType === 'database') {
        setTableHeaders(['Data Source Type']);
        setTableData([{ 
          'Data Source Type': 'Database Connection - Database connection available in the tab interface' 
        }]);
      } else {
        setTableHeaders(['Status']);
        setTableData([{ 'Status': 'No data source type configured' }]);
      }
    } catch (error) {
      console.error('Error parsing tab metadata:', error);
      setTableHeaders(['Error']);
      setTableData([{ 'Error': 'Failed to parse tab configuration' }]);
    } finally {
      setDataLoading(false);
    }
  };

  const startEditing = (tab: DynamicTab) => {
    setEditingTab(tab);
    setFormData({
      name: tab.name,
      description: tab.description,
      letterType: tab.letterType,
      isActive: tab.isActive
    });
  };

  const formatDate = (date: Date | string) => {
    const dateObj = typeof date === 'string' ? new Date(date) : date;
    
    // Check if the date is valid
    if (isNaN(dateObj.getTime())) {
      return 'Invalid Date';
    }
    
    return new Intl.DateTimeFormat('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    }).format(dateObj);
  };

  if (loading) {
    return (
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h1>Tab Management</h1>
            <p className="text-muted-foreground">Loading...</p>
          </div>
        </div>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {Array.from({ length: 6 }).map((_, i) => (
            <Card key={i} className="glass-panel border-glass-border animate-pulse">
              <CardContent className="p-6">
                <div className="space-y-4">
                  <div className="h-4 bg-muted rounded w-3/4" />
                  <div className="h-3 bg-muted rounded w-full" />
                  <div className="h-3 bg-muted rounded w-1/2" />
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1>Tab Management</h1>
          <p className="text-muted-foreground">
            Create and manage dynamic letter type tabs
          </p>
        </div>
        
        <div className="flex gap-2">
          <input
            type="file"
            accept=".json"
            onChange={handleImportTab}
            className="hidden"
            id="import-tab"
          />
          <label htmlFor="import-tab">
            <Button variant="outline" className="cursor-pointer">
              <Download className="h-4 w-4 mr-2" />
              Import
            </Button>
          </label>
          
          <Dialog open={showCreateDialog} onOpenChange={(open) => {
            setShowCreateDialog(open);
            if (open) {
              resetForm();
            }
          }}>
            <DialogTrigger asChild>
              <Button className="neon-glow">
                <Plus className="h-4 w-4 mr-2" />
                Create Tab
              </Button>
            </DialogTrigger>
          <DialogContent className="dialog-panel max-w-md max-h-[80vh] overflow-y-auto">
            <DialogHeader>
              <DialogTitle>{editingTab ? 'Edit Tab' : 'Create New Tab'}</DialogTitle>
              <DialogDescription>
                {editingTab ? 'Update the tab configuration' : 'Add a new letter type tab to the system'}
              </DialogDescription>
            </DialogHeader>
            
            <form onSubmit={editingTab ? handleUpdateTab : handleCreateTab} className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="name">Tab Name</Label>
                <Input
                  id="name"
                  value={formData.name}
                  onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                  placeholder="e.g., Promotion Letter"
                  required
                />
              </div>
              
              <div className="space-y-2">
                <Label htmlFor="description">Description</Label>
                <Textarea
                  id="description"
                  value={formData.description}
                  onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                  placeholder="Brief description of this letter type"
                  rows={3}
                  required
                />
              </div>
              
              <div className="space-y-2">
                <Label htmlFor="letterType">Letter Type ID</Label>
                <Input
                  id="letterType"
                  value={formData.letterType}
                  onChange={(e) => setFormData({ ...formData, letterType: e.target.value })}
                  placeholder="e.g., promotion_letter"
                  required
                />
                <p className="text-xs text-muted-foreground">
                  Used internally for identification (will be auto-formatted)
                </p>
              </div>
              
              <div className="flex items-center space-x-2">
                <Switch
                  id="isActive"
                  checked={formData.isActive}
                  onCheckedChange={(checked) => setFormData({ ...formData, isActive: checked })}
                />
                <Label htmlFor="isActive">Active</Label>
              </div>

              {/* Data Source Type Selection */}
              <div className="space-y-4 border-t pt-4">
                <div className="space-y-2">
                  <Label>Data Source Type</Label>
                  <p className="text-sm text-muted-foreground">
                    Choose how this tab will receive its data
                  </p>
                  <div className="flex space-x-4">
                    <div className="flex items-center space-x-2">
                      <input
                        type="radio"
                        id="database-source"
                        name="dataSource"
                        value="database"
                        checked={dataSource === 'database'}
                        onChange={(e) => setDataSource(e.target.value as 'database' | 'excel')}
                        className="w-4 h-4"
                      />
                      <Label htmlFor="database-source" className="flex items-center gap-2">
                        <Database className="h-4 w-4" />
                        Database Connection
                      </Label>
                    </div>
                    <div className="flex items-center space-x-2">
                      <input
                        type="radio"
                        id="excel-source"
                        name="dataSource"
                        value="excel"
                        checked={dataSource === 'excel'}
                        onChange={(e) => setDataSource(e.target.value as 'database' | 'excel')}
                        className="w-4 h-4"
                      />
                      <Label htmlFor="excel-source" className="flex items-center gap-2">
                        <Upload className="h-4 w-4" />
                        Excel Upload
                      </Label>
                    </div>
                  </div>
                </div>

                <div className="p-4 bg-muted/50 rounded-lg">
                  {dataSource === 'database' ? (
                    <div className="space-y-2">
                      <div className="flex items-center gap-2 text-sm">
                        <Database className="h-4 w-4 text-blue-500" />
                        <span className="font-medium">Database Connection Tab</span>
                      </div>
                      <p className="text-sm text-muted-foreground">
                        This tab will allow users to connect to an external database and configure queries to fetch data.
                      </p>
                    </div>
                  ) : (
                    <div className="space-y-2">
                      <div className="flex items-center gap-2 text-sm">
                        <Upload className="h-4 w-4 text-green-500" />
                        <span className="font-medium">Excel Upload Tab</span>
                      </div>
                      <p className="text-sm text-muted-foreground">
                        This tab will allow users to upload Excel files and work with the data directly.
                      </p>
                    </div>
                  )}
                </div>
              </div>
              
              <div className="flex gap-3 pt-4">
                <Button type="submit" className="flex-1" disabled={isSubmitting}>
                  {isSubmitting ? (
                    <>
                      <Loading className="h-4 w-4 mr-2" />
                      {editingTab ? 'Updating...' : 'Creating...'}
                    </>
                  ) : (
                    editingTab ? 'Update Tab' : 'Create Tab'
                  )}
                </Button>
                <Button 
                  type="button" 
                  variant="outline" 
                  onClick={() => setShowCreateDialog(false)}
                  className="flex-1"
                >
                  Cancel
                </Button>
              </div>
            </form>
          </DialogContent>
        </Dialog>
        </div>
      </div>

      {/* Statistics */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
        <Card className="glass-panel border-glass-border">
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-muted-foreground">Total Tabs</p>
                <p className="text-2xl font-bold">{tabs.length}</p>
              </div>
              <FileText className="h-8 w-8 text-neon-blue" />
            </div>
          </CardContent>
        </Card>

        <Card className="glass-panel border-glass-border">
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-muted-foreground">Active</p>
                <p className="text-2xl font-bold">{tabs.filter(t => t.isActive).length}</p>
              </div>
              <Eye className="h-8 w-8 text-green-400" />
            </div>
          </CardContent>
        </Card>

        <Card className="glass-panel border-glass-border">
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-muted-foreground">Inactive</p>
                <p className="text-2xl font-bold">{tabs.filter(t => !t.isActive).length}</p>
              </div>
              <Settings className="h-8 w-8 text-orange-400" />
            </div>
          </CardContent>
        </Card>

        <Card className="glass-panel border-glass-border">
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-muted-foreground">This Month</p>
                <p className="text-2xl font-bold">
                  {tabs.filter(t => {
                    const monthAgo = new Date();
                    monthAgo.setMonth(monthAgo.getMonth() - 1);
                    return t.createdAt > monthAgo;
                  }).length}
                </p>
              </div>
              <Plus className="h-8 w-8 text-purple-400" />
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Tabs Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {tabs.map((tab) => (
          <Card key={tab.id} className="glass-panel border-glass-border hover:bg-muted/10 transition-colors">
            <CardHeader className="pb-4">
              <div className="flex items-start justify-between">
                <div className="flex items-center gap-3">
                  <div className="w-10 h-10 bg-neon-blue/20 rounded-lg flex items-center justify-center">
                    <FileText className="h-5 w-5 text-neon-blue" />
                  </div>
                  <div>
                    <CardTitle className="text-base">{tab.name}</CardTitle>
                    <div className="flex items-center gap-2 mt-1">
                      <Badge 
                        variant={tab.isActive ? 'default' : 'secondary'}
                        className={tab.isActive ? 'bg-green-500/20 text-green-400 border-green-500/30' : ''}
                      >
                        {tab.isActive ? 'Active' : 'Inactive'}
                      </Badge>
                    </div>
                  </div>
                </div>
              </div>
            </CardHeader>
            
            <CardContent className="pt-0">
              <CardDescription className="mb-4 line-clamp-2">
                {tab.description}
              </CardDescription>
              
              <div className="space-y-2 text-sm text-muted-foreground mb-4">
                <div className="flex justify-between">
                  <span>Type:</span>
                  <span className="font-mono text-xs bg-muted px-2 py-1 rounded">
                    {tab.letterType}
                  </span>
                </div>
                <div className="flex justify-between">
                  <span>Created:</span>
                  <span>{formatDate(tab.createdAt)}</span>
                </div>
                <div className="flex justify-between">
                  <span>Updated:</span>
                  <span>{formatDate(tab.updatedAt)}</span>
                </div>
              </div>
              
              <div className="flex gap-2">
                <Button 
                  size="sm" 
                  variant="outline" 
                  onClick={() => handleViewData(tab)}
                  className="flex-1"
                >
                  <Table className="h-3 w-3 mr-2" />
                  View Data
                </Button>
                
                <Button 
                  size="sm" 
                  variant="outline" 
                  onClick={() => handleEditTab(tab)}
                  className="flex-1"
                >
                  <Edit2 className="h-3 w-3 mr-2" />
                  Edit
                </Button>
                
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button size="sm" variant="outline">
                      <MoreVertical className="h-3 w-3" />
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="end">
                    <DropdownMenuItem onClick={() => handleDuplicateTab(tab)}>
                      <Copy className="h-4 w-4 mr-2" />
                      Duplicate
                    </DropdownMenuItem>
                    <DropdownMenuItem onClick={() => handleExportTab(tab)}>
                      <Download className="h-4 w-4 mr-2" />
                      Export
                    </DropdownMenuItem>
                    <DropdownMenuItem 
                      onClick={() => handleDeleteTab(tab.id)}
                      className="text-red-400"
                    >
                      <Trash2 className="h-4 w-4 mr-2" />
                      Delete
                    </DropdownMenuItem>
                  </DropdownMenuContent>
                </DropdownMenu>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>

      {tabs.length === 0 && (
        <Card className="glass-panel border-glass-border">
          <CardContent className="p-12 text-center">
            <FileText className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
            <h3 className="text-lg font-semibold mb-2">No Tabs Found</h3>
            <p className="text-muted-foreground mb-4">
              Create your first dynamic tab to get started.
            </p>
            <Button onClick={() => {
              resetForm();
              setShowCreateDialog(true);
            }}>
              <Plus className="h-4 w-4 mr-2" />
              Create First Tab
            </Button>
          </CardContent>
        </Card>
      )}

      {/* Data Table Dialog */}
      <Dialog open={showDataDialog} onOpenChange={setShowDataDialog}>
        <DialogContent className="max-w-6xl max-h-[80vh] overflow-hidden">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <Table className="h-5 w-5" />
              {selectedTab?.name} - Data View
            </DialogTitle>
            <DialogDescription>
              {selectedTab?.description}
            </DialogDescription>
          </DialogHeader>
          
          <div className="flex-1 overflow-auto">
            {dataLoading ? (
              <div className="flex items-center justify-center h-32">
                <Loading />
              </div>
            ) : (
              <div className="space-y-4">
                {tableHeaders.length > 0 ? (
                  <div className="border rounded-lg overflow-hidden">
                    <div className="overflow-x-auto">
                      <table className="w-full">
                        <thead className="bg-muted">
                          <tr>
                            {tableHeaders.map((header, index) => (
                              <th key={index} className="px-4 py-3 text-left font-medium text-sm">
                                {header}
                              </th>
                            ))}
                          </tr>
                        </thead>
                        <tbody>
                          {tableData.map((row, rowIndex) => (
                            <tr key={rowIndex} className="border-b hover:bg-muted/50">
                              {tableHeaders.map((header, colIndex) => (
                                <td key={colIndex} className="px-4 py-3 text-sm">
                                  {row[header] || ''}
                                </td>
                              ))}
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                    <div className="px-4 py-2 bg-muted/50 text-sm text-muted-foreground">
                      Showing {tableData.length} rows
                    </div>
                  </div>
                ) : (
                  <div className="text-center py-8 text-muted-foreground">
                    No data available
                  </div>
                )}
              </div>
            )}
          </div>
          
          <div className="flex justify-end gap-2 pt-4 border-t">
            <Button variant="outline" onClick={() => setShowDataDialog(false)}>
              Close
            </Button>
          </div>
        </DialogContent>
      </Dialog>

    </div>
  );
}