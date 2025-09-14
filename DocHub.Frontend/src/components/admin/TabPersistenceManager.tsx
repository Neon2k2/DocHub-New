import React, { useState, useEffect } from 'react';
import { Download, Upload, Trash2, RefreshCw, Database, HardDrive, AlertTriangle } from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../ui/card';
import { Button } from '../ui/button';
import { Badge } from '../ui/badge';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle, DialogTrigger } from '../ui/dialog';
import { Textarea } from '../ui/textarea';
import { Alert, AlertDescription } from '../ui/alert';
import { toast } from 'sonner';
import { tabService } from '../../services/tab.service';
import { tabPersistenceService } from '../../services/tab-persistence.service';

export function TabPersistenceManager() {
  const [stats, setStats] = useState<any>(null);
  const [exportData, setExportData] = useState('');
  const [importData, setImportData] = useState('');
  const [showExportDialog, setShowExportDialog] = useState(false);
  const [showImportDialog, setShowImportDialog] = useState(false);
  const [showClearDialog, setShowClearDialog] = useState(false);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    loadStats();
  }, []);

  const loadStats = () => {
    const storageStats = tabPersistenceService.getStorageStats();
    setStats(storageStats);
  };

  const handleExport = async () => {
    try {
      setLoading(true);
      const data = await tabService.exportTabsData();
      setExportData(data);
      setShowExportDialog(true);
      toast.success('Tabs data exported successfully');
    } catch (error) {
      console.error('Export failed:', error);
      toast.error('Failed to export tabs data');
    } finally {
      setLoading(false);
    }
  };

  const handleDownloadExport = () => {
    const blob = new Blob([exportData], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `dochub-tabs-backup-${new Date().toISOString().split('T')[0]}.json`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
    toast.success('Backup file downloaded');
  };

  const handleImport = async () => {
    if (!importData.trim()) {
      toast.error('Please enter import data');
      return;
    }

    try {
      setLoading(true);
      await tabService.importTabsData(importData);
      setImportData('');
      setShowImportDialog(false);
      loadStats();
      toast.success('Tabs data imported successfully');
    } catch (error) {
      console.error('Import failed:', error);
      toast.error('Failed to import tabs data. Please check the format.');
    } finally {
      setLoading(false);
    }
  };

  const handleClearAll = async () => {
    try {
      setLoading(true);
      await tabService.clearAllTabs();
      setShowClearDialog(false);
      loadStats();
      toast.success('All tabs data cleared');
    } catch (error) {
      console.error('Clear failed:', error);
      toast.error('Failed to clear tabs data');
    } finally {
      setLoading(false);
    }
  };

  const handleFileImport = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;

    const reader = new FileReader();
    reader.onload = (e) => {
      const content = e.target?.result as string;
      setImportData(content);
    };
    reader.readAsText(file);
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-xl font-semibold">Tab Persistence Manager</h2>
          <p className="text-muted-foreground">
            Manage backup, restore, and storage of dynamic tabs
          </p>
        </div>
        <Button
          variant="outline"
          size="sm"
          onClick={loadStats}
          className="gap-2"
        >
          <RefreshCw className="h-4 w-4" />
          Refresh
        </Button>
      </div>

      {/* Storage Statistics */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <Card className="glass-panel border-glass-border">
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-muted-foreground">Storage Type</p>
                <p className="text-2xl font-bold">
                  {stats?.hasLocalStorage ? 'LocalStorage' : 'Memory Only'}
                </p>
              </div>
              <HardDrive className={`h-8 w-8 ${stats?.hasLocalStorage ? 'text-green-400' : 'text-orange-400'}`} />
            </div>
          </CardContent>
        </Card>

        <Card className="glass-panel border-glass-border">
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-muted-foreground">Active Tabs</p>
                <p className="text-2xl font-bold">{stats?.tabCount || 0}</p>
              </div>
              <Database className="h-8 w-8 text-neon-blue" />
            </div>
          </CardContent>
        </Card>

        <Card className="glass-panel border-glass-border">
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-muted-foreground">Memory Cache</p>
                <p className="text-2xl font-bold">{stats?.memoryTabCount || 0}</p>
              </div>
              <RefreshCw className="h-8 w-8 text-purple-400" />
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Storage Status Alert */}
      {stats && !stats.hasLocalStorage && (
        <Alert className="border-orange-500/30 bg-orange-500/10">
          <AlertTriangle className="h-4 w-4" />
          <AlertDescription>
            LocalStorage is not available. Tabs will only persist in memory during this session.
          </AlertDescription>
        </Alert>
      )}

      {/* Action Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {/* Export Card */}
        <Card className="glass-panel border-glass-border">
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Download className="h-5 w-5" />
              Export Tabs
            </CardTitle>
            <CardDescription>
              Create a backup of all dynamic tabs data
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <p className="text-sm text-muted-foreground">
              Export includes all tabs, templates, and metadata. Perfect for creating backups or moving data between environments.
            </p>
            <Button 
              onClick={handleExport} 
              disabled={loading}
              className="w-full"
            >
              {loading ? (
                <RefreshCw className="h-4 w-4 mr-2 animate-spin" />
              ) : (
                <Download className="h-4 w-4 mr-2" />
              )}
              Export All Tabs
            </Button>
          </CardContent>
        </Card>

        {/* Import Card */}
        <Card className="glass-panel border-glass-border">
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Upload className="h-5 w-5" />
              Import Tabs
            </CardTitle>
            <CardDescription>
              Restore tabs from a backup file
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <p className="text-sm text-muted-foreground">
              Import will merge with existing tabs. Duplicate IDs will be updated with imported data.
            </p>
            <div className="space-y-2">
              <input
                type="file"
                accept=".json"
                onChange={handleFileImport}
                className="hidden"
                id="import-file"
              />
              <Button 
                variant="outline" 
                onClick={() => document.getElementById('import-file')?.click()}
                className="w-full"
              >
                Choose File
              </Button>
              <Dialog open={showImportDialog} onOpenChange={setShowImportDialog}>
                <DialogTrigger asChild>
                  <Button className="w-full">
                    <Upload className="h-4 w-4 mr-2" />
                    Import from Text
                  </Button>
                </DialogTrigger>
                <DialogContent className="dialog-panel max-w-2xl max-h-[70vh] overflow-y-auto">
                  <DialogHeader>
                    <DialogTitle>Import Tabs Data</DialogTitle>
                    <DialogDescription>
                      Paste the exported JSON data below to restore tabs
                    </DialogDescription>
                  </DialogHeader>
                  <div className="space-y-4">
                    <Textarea
                      value={importData}
                      onChange={(e) => setImportData(e.target.value)}
                      placeholder="Paste exported JSON data here..."
                      rows={10}
                      className="font-mono text-sm"
                    />
                    <div className="flex gap-3">
                      <Button 
                        onClick={handleImport} 
                        disabled={loading || !importData.trim()}
                        className="flex-1"
                      >
                        {loading ? (
                          <RefreshCw className="h-4 w-4 mr-2 animate-spin" />
                        ) : (
                          <Upload className="h-4 w-4 mr-2" />
                        )}
                        Import Data
                      </Button>
                      <Button 
                        variant="outline" 
                        onClick={() => setShowImportDialog(false)}
                        className="flex-1"
                      >
                        Cancel
                      </Button>
                    </div>
                  </div>
                </DialogContent>
              </Dialog>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Danger Zone */}
      <Card className="glass-panel border-red-500/30 bg-red-500/5">
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-red-400">
            <Trash2 className="h-5 w-5" />
            Danger Zone
          </CardTitle>
          <CardDescription>
            Irreversible actions that affect all tab data
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="flex items-center justify-between">
            <div>
              <h4 className="font-medium text-red-400">Clear All Tabs</h4>
              <p className="text-sm text-muted-foreground">
                Permanently delete all dynamic tabs and related data
              </p>
            </div>
            <Dialog open={showClearDialog} onOpenChange={setShowClearDialog}>
              <DialogTrigger asChild>
                <Button variant="destructive">
                  <Trash2 className="h-4 w-4 mr-2" />
                  Clear All
                </Button>
              </DialogTrigger>
              <DialogContent className="dialog-panel max-w-md">
                <DialogHeader>
                  <DialogTitle>Confirm Clear All</DialogTitle>
                  <DialogDescription>
                    This action cannot be undone. All dynamic tabs and their data will be permanently deleted.
                  </DialogDescription>
                </DialogHeader>
                <div className="flex gap-3 pt-4">
                  <Button 
                    variant="destructive" 
                    onClick={handleClearAll}
                    disabled={loading}
                    className="flex-1"
                  >
                    {loading ? (
                      <RefreshCw className="h-4 w-4 mr-2 animate-spin" />
                    ) : (
                      <Trash2 className="h-4 w-4 mr-2" />
                    )}
                    Yes, Clear All
                  </Button>
                  <Button 
                    variant="outline" 
                    onClick={() => setShowClearDialog(false)}
                    className="flex-1"
                  >
                    Cancel
                  </Button>
                </div>
              </DialogContent>
            </Dialog>
          </div>
        </CardContent>
      </Card>

      {/* Export Dialog */}
      <Dialog open={showExportDialog} onOpenChange={setShowExportDialog}>
        <DialogContent className="dialog-panel max-w-4xl max-h-[70vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>Exported Tabs Data</DialogTitle>
            <DialogDescription>
              Copy this data or download as a backup file
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            <Textarea
              value={exportData}
              readOnly
              rows={15}
              className="font-mono text-sm"
            />
            <div className="flex gap-3">
              <Button onClick={handleDownloadExport} className="flex-1">
                <Download className="h-4 w-4 mr-2" />
                Download as File
              </Button>
              <Button 
                variant="outline" 
                onClick={() => {
                  navigator.clipboard.writeText(exportData);
                  toast.success('Copied to clipboard');
                }}
                className="flex-1"
              >
                Copy to Clipboard
              </Button>
              <Button 
                variant="outline" 
                onClick={() => setShowExportDialog(false)}
              >
                Close
              </Button>
            </div>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}