import React, { useState, useEffect } from 'react';
import { Plus, Edit2, Trash2, FileText, Settings, Eye, Copy, Download, MoreVertical, Database, Table, Upload, ArrowLeft, ArrowRight, X, Check } from 'lucide-react';
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
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../ui/select';
import { Separator } from '../ui/separator';
import { toast } from 'sonner';
import { DynamicTab, tabService } from '../../services/tab.service';
import { apiService } from '../../services/api.service';
import { notify } from '../../utils/notifications';
import { handleError } from '../../utils/errorHandler';
import { useAuth } from '../../contexts/AuthContext';

// Field configuration types
interface FieldConfig {
  id: string;
  fieldKey: string;
  fieldName: string;
  displayName: string;
  fieldType: string;
  isRequired: boolean;
  validationRules?: string;
  defaultValue?: string;
  order: number;
}

export function TabManagement() {
  const { isAuthenticated } = useAuth();
  const [tabs, setTabs] = useState<DynamicTab[]>([]);
  const [loading, setLoading] = useState(true);
  const [showCreateDialog, setShowCreateDialog] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  
  // Multi-step dialog state
  const [currentStep, setCurrentStep] = useState(1);
  const [formData, setFormData] = useState({
    typeKey: '',
    displayName: '',
    description: '',
    isActive: true,
    module: 'ER'
  });

  // Data source type selection
  const [dataSource, setDataSource] = useState<'Database' | 'Excel'>('Database');
  
  // Field configuration
  const [fields, setFields] = useState<FieldConfig[]>([]);
  const [newField, setNewField] = useState<Partial<FieldConfig>>({
    fieldKey: '',
    fieldName: '',
    displayName: '',
    fieldType: 'Text', // Always Text type for simplicity
    isRequired: false,
    order: 0
  });


  useEffect(() => {
    loadTabs();
  }, []);

  const loadTabs = async () => {
    try {
      setLoading(true);
      const tabsData = await tabService.getTabs();
      setTabs(tabsData);
      
      // Show a friendly message if no tabs exist yet
      if (tabsData.length === 0) {
        toast.info('No tabs found. Create your first tab to get started!');
      }
    } catch (error) {
      console.error('Failed to load tabs:', error);
      toast.error('Failed to load tabs. Please try again.');
      setTabs([]); // Ensure we set empty array on error
    } finally {
      setLoading(false);
    }
  };

  // Field management functions
  const addField = () => {
    if (!newField.fieldKey || !newField.fieldName || !newField.displayName) {
      toast.error('Please fill in all required field information');
      return;
    }

    const field: FieldConfig = {
      id: `field_${Date.now()}`,
      fieldKey: newField.fieldKey,
      fieldName: newField.fieldName,
      displayName: newField.displayName,
      fieldType: newField.fieldType || 'Text',
      isRequired: newField.isRequired || false,
      validationRules: newField.validationRules,
      defaultValue: newField.defaultValue,
      order: fields.length
    };

    setFields([...fields, field]);
    setNewField({
      fieldKey: '',
      fieldName: '',
      displayName: '',
      fieldType: 'Text',
      isRequired: false,
      order: fields.length + 1
    });
  };

  const removeField = (fieldId: string) => {
    setFields(fields.filter(f => f.id !== fieldId));
  };

  const updateFieldOrder = (fieldId: string, newOrder: number) => {
    setFields(fields.map(f => 
      f.id === fieldId ? { ...f, order: newOrder } : f
    ));
  };

  const handleCreateTab = async () => {
    console.log('🚀 [TAB-CREATE] Starting tab creation process');
    console.log('🔍 [TAB-CREATE] Authentication status:', isAuthenticated);
    console.log('📋 [TAB-CREATE] Form data:', formData);
    console.log('📊 [TAB-CREATE] Fields:', fields);
    
    if (isSubmitting) {
      console.log('⏳ [TAB-CREATE] Already submitting, skipping');
      return;
    }
    
    if (!isAuthenticated) {
      console.log('❌ [TAB-CREATE] User not authenticated');
      toast.error('Please login first to create tabs');
      return;
    }
    
    if (!formData.typeKey || !formData.displayName) {
      console.log('❌ [TAB-CREATE] Missing required fields');
      toast.error('Please fill in all required fields');
      return;
    }
    
    try {
      console.log('🔄 [TAB-CREATE] Setting submitting state to true');
      setIsSubmitting(true);
      
      // Create field configuration JSON
      console.log('🔧 [TAB-CREATE] Creating field configuration...');
      const fieldConfiguration = {
        fields: fields.map(field => ({
          fieldKey: field.fieldKey,
          fieldName: field.fieldName,
          displayName: field.displayName,
          fieldType: 'Text', // Always Text type for simplicity
          isRequired: field.isRequired,
          validationRules: field.validationRules,
          defaultValue: field.defaultValue,
          order: field.order
        }))
      };
      console.log('✅ [TAB-CREATE] Field configuration created:', fieldConfiguration);

      // Create the letter type definition
      console.log('📝 [TAB-CREATE] Creating letter type data...');
      const letterTypeData = {
        typeKey: formData.typeKey,
        displayName: formData.displayName,
        description: formData.description,
        dataSourceType: dataSource,
        fieldConfiguration: JSON.stringify(fieldConfiguration),
        isActive: formData.isActive
      };
      console.log('✅ [TAB-CREATE] Letter type data created:', letterTypeData);

      // Call the actual API to create letter type
      console.log('🔄 [TAB-CREATE] Calling API service...');
      const response = await apiService.createLetterTypeDefinition(letterTypeData);
      console.log('📊 [TAB-CREATE] API response received:', response);
      
      if (response.success && response.data) {
        console.log('✅ [TAB-CREATE] API call successful, creating tab UI representation...');
        // Create a tab representation for the UI
        const newTab: DynamicTab = {
          id: response.data.id,
          name: response.data.displayName,
          description: response.data.description || '',
          letterType: response.data.typeKey,
          isActive: response.data.isActive,
          createdAt: new Date(response.data.createdAt),
          updatedAt: new Date(response.data.updatedAt),
          metadata: JSON.stringify({
            dataSourceType: dataSource,
            fieldConfiguration: fieldConfiguration
          })
        };
        console.log('✅ [TAB-CREATE] Tab UI representation created:', newTab);
        
        console.log('🔄 [TAB-CREATE] Updating tabs state...');
        setTabs([...tabs, newTab]);
        setShowCreateDialog(false);
        resetForm();
        console.log('🎉 [TAB-CREATE] Tab created successfully!');
        toast.success('Tab created successfully');
      } else {
        console.log('❌ [TAB-CREATE] API call failed:', response.error);
        throw new Error(response.error?.message || 'Failed to create letter type');
      }
    } catch (error: any) {
      console.error('❌ [TAB-CREATE] Error in tab creation:', error);
      if (error.message?.includes('Authentication required') || error.message?.includes('401')) {
        console.log('🔐 [TAB-CREATE] Authentication error detected');
        toast.error('Please login first to create tabs');
      } else {
        console.log('💥 [TAB-CREATE] Other error:', error.message);
        toast.error(error.message || 'Failed to create tab');
      }
    } finally {
      console.log('🔄 [TAB-CREATE] Setting submitting state to false');
      setIsSubmitting(false);
    }
  };

  const nextStep = () => {
    if (currentStep === 1) {
      if (!formData.typeKey || !formData.displayName) {
        toast.error('Please fill in all required fields');
        return;
      }
    }
    setCurrentStep(currentStep + 1);
  };

  const prevStep = () => {
    setCurrentStep(currentStep - 1);
  };



  const handleDeleteTab = async (tabId: string) => {
    const tab = tabs.find(t => t.id === tabId);
    if (!tab) return;

    notify.confirmAction(
      `Are you sure you want to delete tab "${tab.name}"?`,
      async () => {
        try {
          const response = await apiService.deleteLetterTypeDefinition(tabId);
          
          if (response.success) {
            setTabs(tabs.filter(tab => tab.id !== tabId));
            notify.success('Tab deleted successfully');
          } else {
            throw new Error(response.error?.message || 'Failed to delete letter type');
          }
        } catch (error) {
          handleError(error, 'Delete tab');
        }
      }
    );
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


  const resetForm = () => {
    setFormData({
      typeKey: '',
      displayName: '',
      description: '',
      isActive: true,
      module: 'ER'
    });
    setDataSource('Database');
    setFields([]);
    setNewField({
      fieldKey: '',
      fieldName: '',
      displayName: '',
      fieldType: 'Text',
      isRequired: false,
      order: 0
    });
    setCurrentStep(1);
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
            <DialogContent className="dialog-panel max-w-4xl max-h-[70vh] overflow-hidden">
              <DialogHeader>
                <DialogTitle className="flex items-center justify-between">
                  <span>Create New Tab</span>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => setShowCreateDialog(false)}
                  >
                    <X className="h-4 w-4" />
                  </Button>
                </DialogTitle>
                <DialogDescription>
                  Create a new letter type tab with custom fields
                </DialogDescription>
              </DialogHeader>
              
              {/* Step Indicator */}
              <div className="flex items-center justify-center space-x-4 py-4">
                {[1, 2, 3].map((step) => (
                  <div key={step} className="flex items-center">
                    <div className={`w-8 h-8 rounded-full flex items-center justify-center text-sm font-medium ${
                      currentStep >= step 
                        ? 'bg-neon-blue text-white' 
                        : 'bg-muted text-muted-foreground'
                    }`}>
                      {currentStep > step ? <Check className="h-4 w-4" /> : step}
                    </div>
                    {step < 3 && (
                      <div className={`w-16 h-0.5 mx-2 ${
                        currentStep > step ? 'bg-neon-blue' : 'bg-muted'
                      }`} />
                    )}
                  </div>
                ))}
              </div>

              <div className="flex-1 overflow-y-auto">
                {/* Step 1: Basic Information */}
                {currentStep === 1 && (
                  <div className="space-y-6">
                    <div className="space-y-4">
                      <h3 className="text-lg font-semibold">Basic Information</h3>
                      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                        <div className="space-y-2">
                          <Label htmlFor="typeKey">Type Key *</Label>
                          <Input
                            id="typeKey"
                            value={formData.typeKey}
                            onChange={(e) => setFormData({ ...formData, typeKey: e.target.value })}
                            placeholder="e.g., promotion_letter"
                            required
                          />
                          <p className="text-xs text-muted-foreground">
                            Unique identifier for this letter type
                          </p>
                        </div>
                        
                        <div className="space-y-2">
                          <Label htmlFor="displayName">Display Name *</Label>
                          <Input
                            id="displayName"
                            value={formData.displayName}
                            onChange={(e) => setFormData({ ...formData, displayName: e.target.value })}
                            placeholder="e.g., Promotion Letter"
                            required
                          />
                        </div>
                      </div>
                      
                      <div className="space-y-2">
                        <Label htmlFor="description">Description</Label>
                        <Textarea
                          id="description"
                          value={formData.description}
                          onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                          placeholder="Brief description of this letter type"
                          rows={3}
                        />
                      </div>
                      
                      <div className="flex items-center space-x-2">
                        <Switch
                          id="isActive"
                          checked={formData.isActive}
                          onCheckedChange={(checked) => setFormData({ ...formData, isActive: checked })}
                        />
                        <Label htmlFor="isActive">Active</Label>
                      </div>
                    </div>
                  </div>
                )}

                {/* Step 2: Data Source Selection */}
                {currentStep === 2 && (
                  <div className="space-y-6">
                    <h3 className="text-lg font-semibold">Data Source Configuration</h3>
                    <div className="space-y-4">
                      <Label>Choose Data Source Type</Label>
                      <p className="text-sm text-muted-foreground">
                        Select how this tab will receive its data
                      </p>
                      
                      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                        <Card 
                          className={`cursor-pointer transition-all ${
                            dataSource === 'Database' 
                              ? 'ring-2 ring-neon-blue bg-neon-blue/10' 
                              : 'hover:bg-muted/50'
                          }`}
                          onClick={() => setDataSource('Database')}
                        >
                          <CardContent className="p-6">
                            <div className="flex items-center space-x-3">
                              <Database className="h-8 w-8 text-blue-500" />
                              <div>
                                <h4 className="font-semibold">Database Connection</h4>
                                <p className="text-sm text-muted-foreground">
                                  Connect to external database
                                </p>
                              </div>
                            </div>
                          </CardContent>
                        </Card>
                        
                        <Card 
                          className={`cursor-pointer transition-all ${
                            dataSource === 'Excel' 
                              ? 'ring-2 ring-neon-blue bg-neon-blue/10' 
                              : 'hover:bg-muted/50'
                          }`}
                          onClick={() => setDataSource('Excel')}
                        >
                          <CardContent className="p-6">
                            <div className="flex items-center space-x-3">
                              <Upload className="h-8 w-8 text-green-500" />
                              <div>
                                <h4 className="font-semibold">Excel Upload</h4>
                                <p className="text-sm text-muted-foreground">
                                  Upload and work with Excel files
                                </p>
                              </div>
                            </div>
                          </CardContent>
                        </Card>
                      </div>
                    </div>
                  </div>
                )}

                {/* Step 3: Field Configuration */}
                {currentStep === 3 && (
                  <div className="space-y-6">
                    <h3 className="text-lg font-semibold">Field Configuration</h3>
                    <p className="text-sm text-muted-foreground">
                      Define the fields that will be available in this letter type
                    </p>
                    
                    {/* Add New Field Form */}
                    <Card>
                      <CardHeader>
                        <CardTitle className="text-base">Add New Field</CardTitle>
                      </CardHeader>
                      <CardContent className="space-y-4">
                        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                          <div className="space-y-2">
                            <Label htmlFor="fieldKey">Field Key *</Label>
                            <Input
                              id="fieldKey"
                              value={newField.fieldKey || ''}
                              onChange={(e) => setNewField({ ...newField, fieldKey: e.target.value })}
                              placeholder="e.g., employee_name"
                            />
                          </div>
                          
                          <div className="space-y-2">
                            <Label htmlFor="fieldName">Field Name *</Label>
                            <Input
                              id="fieldName"
                              value={newField.fieldName || ''}
                              onChange={(e) => setNewField({ ...newField, fieldName: e.target.value })}
                              placeholder="e.g., EmployeeName"
                            />
                          </div>
                          
                          <div className="space-y-2">
                            <Label htmlFor="displayName">Display Name *</Label>
                            <Input
                              id="displayName"
                              value={newField.displayName || ''}
                              onChange={(e) => setNewField({ ...newField, displayName: e.target.value })}
                              placeholder="e.g., Employee Name"
                            />
                          </div>
                        </div>
                        
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                          {/* Field type is always Text - no selection needed */}
                          
                          <div className="space-y-2">
                            <Label htmlFor="defaultValue">Default Value</Label>
                            <Input
                              id="defaultValue"
                              value={newField.defaultValue || ''}
                              onChange={(e) => setNewField({ ...newField, defaultValue: e.target.value })}
                              placeholder="Optional default value"
                            />
                          </div>
                        </div>
                        
                        <div className="flex items-center space-x-4">
                          <div className="flex items-center space-x-2">
                            <Switch
                              id="isRequired"
                              checked={newField.isRequired || false}
                              onCheckedChange={(checked) => setNewField({ ...newField, isRequired: checked })}
                            />
                            <Label htmlFor="isRequired">Required Field</Label>
                          </div>
                          
                          <Button onClick={addField} className="neon-glow">
                            <Plus className="h-4 w-4 mr-2" />
                            Add Field
                          </Button>
                        </div>
                      </CardContent>
                    </Card>
                    
                    {/* Fields List */}
                    {fields.length > 0 && (
                      <Card>
                        <CardHeader>
                          <CardTitle className="text-base">Configured Fields ({fields.length})</CardTitle>
                        </CardHeader>
                        <CardContent>
                          <div className="space-y-2">
                            {fields.map((field, index) => (
                              <div key={field.id} className="flex items-center justify-between p-3 bg-muted/50 rounded-lg">
                                <div className="flex items-center space-x-4">
                                  <div className="w-6 h-6 bg-neon-blue/20 rounded-full flex items-center justify-center text-xs font-medium">
                                    {index + 1}
                                  </div>
                                  <div>
                                    <div className="font-medium">{field.displayName}</div>
                                    <div className="text-sm text-muted-foreground">
                                      {field.fieldKey}
                                      {field.isRequired && ' • Required'}
                                    </div>
                                  </div>
                                </div>
                                <Button
                                  variant="ghost"
                                  size="sm"
                                  onClick={() => removeField(field.id)}
                                  className="text-red-400 hover:text-red-300"
                                >
                                  <X className="h-4 w-4" />
                                </Button>
                              </div>
                            ))}
                          </div>
                        </CardContent>
                      </Card>
                    )}
                  </div>
                )}
              </div>
              
              {/* Navigation Buttons */}
              <div className="flex justify-between pt-6 border-t">
                <div>
                  {currentStep > 1 && (
                    <Button variant="outline" onClick={prevStep}>
                      <ArrowLeft className="h-4 w-4 mr-2" />
                      Previous
                    </Button>
                  )}
                </div>
                
                <div className="flex space-x-2">
                  <Button 
                    variant="outline" 
                    onClick={() => setShowCreateDialog(false)}
                  >
                    Cancel
                  </Button>
                  
                  {currentStep < 3 ? (
                    <Button onClick={nextStep}>
                      Next
                      <ArrowRight className="h-4 w-4 ml-2" />
                    </Button>
                  ) : (
                    <Button 
                      onClick={handleCreateTab}
                      disabled={isSubmitting}
                      className="neon-glow"
                    >
                      {isSubmitting ? (
                        <>
                          <Loading className="h-4 w-4 mr-2" />
                          Creating...
                        </>
                      ) : (
                        'Create Tab'
                      )}
                    </Button>
                  )}
                </div>
              </div>
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


    </div>
  );
}