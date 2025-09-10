import React, { useState, useEffect } from 'react';
import { FileText, Users, TrendingUp, Clock, Filter, Upload, Plus, Eye, Download, FileSpreadsheet, Database } from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../ui/card';
import { Badge } from '../ui/badge';
import { Button } from '../ui/button';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../ui/tabs';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from '../ui/dialog';
import { Input } from '../ui/input';
import { Label } from '../ui/label';
import { ScrollArea } from '../ui/scroll-area';
import { Textarea } from '../ui/textarea';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../ui/select';
import { Progress } from '../ui/progress';
import { Separator } from '../ui/separator';
import { EmployeeSelector } from '../er/EmployeeSelector';
import { TemplateSelectionDialog } from '../er/TemplateSelectionDialog';
import { DocumentPreviewDialog } from '../er/DocumentPreviewDialog';
import { EmailComposerDialog } from '../er/EmailComposerDialog';
import { PageHeader } from './PageHeader';
import { StatisticsCards } from './StatisticsCards';
import { TabbedInterface } from './TabbedInterface';
import { Employee, apiService, LetterTypeDefinition, DynamicField, FieldType, DynamicDocumentGenerationRequest, DynamicEmailRequest, EmailPriority } from '../../services/api.service';
import { DynamicTab, TabTemplate, TabSignature, tabService } from '../../services/tab.service';
import { DocumentTemplate, GeneratedDocument, EmailJob } from '../../services/document.service';
import { useDocumentRequests, DocumentRequestWithEmployee } from '../../hooks/useDocumentRequests';
import { excelService, ExcelData } from '../../services/excel.service';
import { ExcelUploadDialog } from './ExcelUploadDialog';
import { toast } from 'sonner';

interface DynamicLetterTabProps {
  tab: DynamicTab;
}

export function DynamicLetterTab({ tab }: DynamicLetterTabProps) {
  const [selectedEmployees, setSelectedEmployees] = useState<Employee[]>([]);
  const [templates, setTemplates] = useState<TabTemplate[]>([]);
  const [signatures, setSignatures] = useState<TabSignature[]>([]);
  const [loading, setLoading] = useState(true);
  const [dynamicFields, setDynamicFields] = useState<DynamicField[]>([]);
  const [fieldData, setFieldData] = useState<Record<string, any>>({});
  
  // Fetch document requests for this tab type
  const { requests, loading: requestsLoading } = useDocumentRequests(tab.letterType);
  
  // Map DocumentRequest to the format expected by TabbedInterface
  const mappedRequests = requests.map(request => ({
    id: request.id,
    employeeName: request.employeeName,
    employeeId: request.employeeId,
    createdAt: new Date(request.createdAt),
    requestedBy: request.requestedBy,
    status: request.status
  }));
  
  // Dialog states - matching Experience tab pattern
  const [showTemplateDialog, setShowTemplateDialog] = useState(false);
  const [showPreviewDialog, setShowPreviewDialog] = useState(false);
  const [showEmailDialog, setShowEmailDialog] = useState(false);
  const [selectedTemplate, setSelectedTemplate] = useState<DocumentTemplate | null>(null);
  const [generatedDocuments, setGeneratedDocuments] = useState<GeneratedDocument[]>([]);
  const [emailJobs, setEmailJobs] = useState<EmailJob[]>([]);
  
  // Upload form state
  const [templateUploadForm, setTemplateUploadForm] = useState({ name: '', file: null as File | null });
  const [uploadingTemplate, setUploadingTemplate] = useState(false);

  // Excel upload state
  const [showExcelUpload, setShowExcelUpload] = useState(false);
  const [excelData, setExcelData] = useState<ExcelData | null>(null);
  const [dataSourceType, setDataSourceType] = useState<'database' | 'excel' | null>(null);

  useEffect(() => {
    loadData();
  }, [tab.id]);

  const loadData = async () => {
    try {
      setLoading(true);
      
      // Load dynamic fields for this letter type
      if (tab.letterTypeDefinition?.id) {
        const fieldsResponse = await apiService.getDynamicFields(tab.letterTypeDefinition.id);
        if (fieldsResponse.success && fieldsResponse.data) {
          setDynamicFields(fieldsResponse.data);
        }
      }
      
      // Parse tab metadata to determine data source type
      try {
        if (tab.metadata) {
          const metadata = typeof tab.metadata === 'string' 
            ? JSON.parse(tab.metadata) 
            : tab.metadata;
          setDataSourceType(metadata?.dataSourceType || null);
        } else {
          setDataSourceType(null);
        }
      } catch (error) {
        console.error('Error parsing tab metadata:', error);
        setDataSourceType(null);
      }
      
      const [templatesData, signaturesData] = await Promise.all([
        tabService.getTemplatesForTab(tab.id),
        tabService.getSignatures()
      ]);
      setTemplates(templatesData);
      setSignatures(signaturesData);

      // Load Excel data if this is an Excel-type tab
      if (dataSourceType === 'excel') {
        const existingData = await excelService.getExcelDataForTab(tab.id);
        if (existingData) {
          setExcelData(existingData);
        }
      }
    } catch (error) {
      console.error('Failed to load tab data:', error);
      toast.error('Failed to load tab data');
    } finally {
      setLoading(false);
    }
  };

  const handleGenerate = async (employees: Employee[]) => {
    if (employees.length === 0) {
      toast.error('Please select at least one employee');
      return;
    }

    if (!tab.letterTypeDefinition?.id) {
      toast.error('Letter type definition not found');
      return;
    }

    setSelectedEmployees(employees);
    
    // Use dynamic document generation
    try {
      const request: DynamicDocumentGenerationRequest = {
        letterTypeDefinitionId: tab.letterTypeDefinition.id,
        employeeIds: employees.map(emp => emp.id),
        includeDocumentAttachments: true,
        additionalFieldData: fieldData
      };

      const response = await apiService.generateDynamicDocuments(request);
      
      if (response.success && response.data) {
        const documents: GeneratedDocument[] = response.data.generatedDocuments.map((doc: any) => ({
          id: doc.documentId,
          templateId: doc.templateId || '',
          employeeId: doc.employeeId,
          content: doc.content || '',
          placeholderData: doc.placeholderData || {},
          generatedBy: 'Current User',
          generatedAt: new Date(doc.generatedAt),
          downloadUrl: doc.downloadUrl || ''
        }));
        
        setGeneratedDocuments(documents);
        setShowPreviewDialog(true);
        toast.success(`Generated ${documents.length} documents successfully`);
        
        if (response.data.errors && response.data.errors.length > 0) {
          toast.warning(`Some documents failed to generate: ${response.data.errors.join(', ')}`);
        }
      } else {
        throw new Error(response.error?.message || 'Failed to generate documents');
      }
    } catch (error) {
      console.error('Error generating documents:', error);
      toast.error('Failed to generate documents. Please try again.');
    }
  };

  const handleSendEmail = async (employees: Employee[]) => {
    if (employees.length === 0) {
      toast.error('Please select at least one employee');
      return;
    }

    if (!tab.letterTypeDefinition?.id) {
      toast.error('Letter type definition not found');
      return;
    }

    setSelectedEmployees(employees);
    
    // Use dynamic email sending
    try {
      if (employees.length === 1) {
        // Single email
        const request: DynamicEmailRequest = {
          letterTypeDefinitionId: tab.letterTypeDefinition.id,
          employeeId: employees[0].id,
          includeDocumentAttachment: true,
          enableTracking: true,
          priority: EmailPriority.Normal,
          sendImmediately: true,
          additionalFieldData: fieldData
        };

        const response = await apiService.sendDynamicEmail(request);
        
        if (response.success) {
          toast.success('Email sent successfully');
        } else {
          throw new Error(response.error?.message || 'Failed to send email');
        }
      } else {
        // Bulk email
        const request = {
          letterTypeDefinitionId: tab.letterTypeDefinition.id,
          employeeIds: employees.map(emp => emp.id),
          includeDocumentAttachments: true,
          enableTracking: true,
          priority: EmailPriority.Normal,
          sendImmediately: true,
          maxEmailsPerMinute: 100
        };

        const response = await apiService.sendBulkDynamicEmails(request);
        
        if (response.success && response.data) {
          toast.success(`Sent ${response.data.successfulEmails} of ${response.data.totalEmails} emails successfully`);
          if (response.data.failedEmails > 0) {
            toast.warning(`${response.data.failedEmails} emails failed to send`);
          }
        } else {
          throw new Error(response.error?.message || 'Failed to send bulk emails');
        }
      }
    } catch (error) {
      console.error('Error sending emails:', error);
      toast.error('Failed to send emails. Please try again.');
    }
  };

  const handleExcelUploadSuccess = (data: ExcelData) => {
    setExcelData(data);
    toast.success(`Excel file uploaded successfully! ${data.data.length} rows loaded.`);
  };

  const handleTemplateSelect = async (template: DocumentTemplate) => {
    setSelectedTemplate(template);
    setShowTemplateDialog(false);
    
    try {
      // Use the new bulk generation API
      const response = await apiService.generateDocumentsForTab(tab.id, {
        employeeIds: selectedEmployees.map(emp => emp.id),
        templateId: template.id,
        signatureId: undefined, // Will be handled in preview dialog
        placeholderData: {
          COMPANY_NAME: 'DocHub Technologies',
          CURRENT_DATE: new Date().toLocaleDateString()
        }
      });

      if (response.success && response.data) {
        const documents: GeneratedDocument[] = response.data.generatedDocuments.map((doc: any) => ({
          id: doc.documentId,
          templateId: template.id,
          employeeId: doc.employeeId,
          content: '', // Will be populated by preview
          placeholderData: {},
          generatedBy: 'Current User',
          generatedAt: new Date(doc.generatedAt),
          downloadUrl: ''
        }));
        
        setGeneratedDocuments(documents);
        setShowPreviewDialog(true);
        toast.success(`Generated ${documents.length} documents successfully`);
        
        if (response.data.errors && response.data.errors.length > 0) {
          toast.warning(`Some documents failed to generate: ${response.data.errors.join(', ')}`);
        }
      } else {
        throw new Error(response.error?.message || 'Failed to generate documents');
      }
    } catch (error) {
      console.error('Error generating documents:', error);
      toast.error('Failed to generate documents. Please try again.');
    }
  };

  const handleDocumentsGenerated = (documents: GeneratedDocument[]) => {
    setGeneratedDocuments(documents);
    // Could show success message or redirect to documents list
  };

  const handleEmailsSent = (jobs: EmailJob[]) => {
    setEmailJobs([...emailJobs, ...jobs]);
      setShowEmailDialog(false);
    // Could show success message or redirect to email status
  };

  const handleTemplateUpload = async () => {
    if (!templateUploadForm.name || !templateUploadForm.file) {
      toast.error('Please provide both template name and file');
      return;
    }

    setUploadingTemplate(true);
    try {
      // Create a new template object
      const newTemplate: TabTemplate = {
        id: `template_${Date.now()}`,
        name: templateUploadForm.name,
        content: '', // Will be populated by the service
        placeholders: ['{EMPLOYEE_NAME}', '{EMPLOYEE_ID}', '{DESIGNATION}', '{DEPARTMENT}', '{JOIN_DATE}', '{RELIEVING_DATE}', '{SALARY}', '{COMPANY_NAME}'],
        createdAt: new Date()
      };

      // Add to templates list
      setTemplates(prev => [...prev, newTemplate]);
      
      // Reset form
      setTemplateUploadForm({ name: '', file: null });
      
      toast.success('Template uploaded successfully');
    } catch (error) {
      console.error('Failed to upload template:', error);
      toast.error('Failed to upload template');
    } finally {
      setUploadingTemplate(false);
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-96">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-neon-blue"></div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <PageHeader 
        title={tab.name}
        description={tab.description}
        isActive={tab.isActive}
      />

      {/* Statistics */}
      <StatisticsCards 
        totalRequests={requests.length}
        pending={requests.filter(r => r.status === 'Pending').length}
        approved={requests.filter(r => r.status === 'Approved').length}
        thisMonth={requests.filter(r => {
          const requestDate = new Date(r.createdAt);
          const now = new Date();
          return requestDate.getMonth() === now.getMonth() && requestDate.getFullYear() === now.getFullYear();
        }).length}
        templates={templates.length}
        signatures={signatures.length}
      />

      {/* Main Content */}
      <TabbedInterface 
        tabName={tab.name}
        requests={mappedRequests}
        loading={requestsLoading}
        children={
          <div className="space-y-6">
            {/* Data Source Actions */}
            {dataSourceType === 'excel' && (
              <Card className="glass-panel border-glass-border">
                <CardContent className="p-4">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-3">
                      <FileSpreadsheet className="h-5 w-5 text-green-500" />
                      <div>
                        <h3 className="font-semibold">Excel Data Source</h3>
                        <p className="text-sm text-muted-foreground">
                          {excelData 
                            ? `Loaded ${excelData.data.length} rows from ${excelData.fileName}`
                            : 'No Excel data uploaded yet'
                          }
                        </p>
                      </div>
                    </div>
                    <Button
                      onClick={() => setShowExcelUpload(true)}
                      className="bg-green-600 hover:bg-green-700 text-white"
                    >
                      <Upload className="h-4 w-4 mr-2 text-white" />
                      {excelData ? 'Upload New File' : 'Upload Excel File'}
                    </Button>
                  </div>
                </CardContent>
              </Card>
            )}

            {dataSourceType === 'database' && (
              <Card className="glass-panel border-glass-border">
                <CardContent className="p-4">
                  <div className="flex items-center gap-3">
                    <Database className="h-5 w-5 text-blue-500" />
                    <div>
                      <h3 className="font-semibold">Database Connection</h3>
                      <p className="text-sm text-muted-foreground">
                        This tab is configured to use database data
                      </p>
                    </div>
                  </div>
                </CardContent>
              </Card>
            )}

            {/* Dynamic Field Inputs */}
            {dynamicFields.length > 0 && (
              <Card className="glass-panel border-glass-border">
                <CardHeader>
                  <CardTitle className="flex items-center gap-2">
                    <FileText className="h-5 w-5 text-purple-500" />
                    Dynamic Field Data
                  </CardTitle>
                  <CardDescription>
                    Configure additional field data for this letter type
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    {dynamicFields.map((field) => (
                      <div key={field.id} className="space-y-2">
                        <Label htmlFor={field.fieldKey} className="text-sm font-medium">
                          {field.displayName}
                          {field.isRequired && <span className="text-red-500 ml-1">*</span>}
                        </Label>
                        {field.fieldType === FieldType.Text && (
                          <Input
                            id={field.fieldKey}
                            value={fieldData[field.fieldKey] || ''}
                            onChange={(e) => setFieldData(prev => ({ ...prev, [field.fieldKey]: e.target.value }))}
                            placeholder={field.defaultValue || `Enter ${field.displayName.toLowerCase()}`}
                            className="glass-input"
                          />
                        )}
                        {field.fieldType === FieldType.TextArea && (
                          <Textarea
                            id={field.fieldKey}
                            value={fieldData[field.fieldKey] || ''}
                            onChange={(e) => setFieldData(prev => ({ ...prev, [field.fieldKey]: e.target.value }))}
                            placeholder={field.defaultValue || `Enter ${field.displayName.toLowerCase()}`}
                            className="glass-input"
                            rows={3}
                          />
                        )}
                        {field.fieldType === FieldType.Number && (
                          <Input
                            id={field.fieldKey}
                            type="number"
                            value={fieldData[field.fieldKey] || ''}
                            onChange={(e) => setFieldData(prev => ({ ...prev, [field.fieldKey]: e.target.value }))}
                            placeholder={field.defaultValue || `Enter ${field.displayName.toLowerCase()}`}
                            className="glass-input"
                          />
                        )}
                        {field.fieldType === FieldType.Date && (
                          <Input
                            id={field.fieldKey}
                            type="date"
                            value={fieldData[field.fieldKey] || ''}
                            onChange={(e) => setFieldData(prev => ({ ...prev, [field.fieldKey]: e.target.value }))}
                            className="glass-input"
                          />
                        )}
                        {field.fieldType === FieldType.Email && (
                          <Input
                            id={field.fieldKey}
                            type="email"
                            value={fieldData[field.fieldKey] || ''}
                            onChange={(e) => setFieldData(prev => ({ ...prev, [field.fieldKey]: e.target.value }))}
                            placeholder={field.defaultValue || `Enter ${field.displayName.toLowerCase()}`}
                            className="glass-input"
                          />
                        )}
                        {field.fieldType === FieldType.Currency && (
                          <Input
                            id={field.fieldKey}
                            type="number"
                            step="0.01"
                            value={fieldData[field.fieldKey] || ''}
                            onChange={(e) => setFieldData(prev => ({ ...prev, [field.fieldKey]: e.target.value }))}
                            placeholder={field.defaultValue || `Enter ${field.displayName.toLowerCase()}`}
                            className="glass-input"
                          />
                        )}
                        {field.fieldType === FieldType.Boolean && (
                          <Select
                            value={fieldData[field.fieldKey] || ''}
                            onValueChange={(value) => setFieldData(prev => ({ ...prev, [field.fieldKey]: value }))}
                          >
                            <SelectTrigger className="glass-input">
                              <SelectValue placeholder={`Select ${field.displayName.toLowerCase()}`} />
                            </SelectTrigger>
                            <SelectContent>
                              <SelectItem value="true">Yes</SelectItem>
                              <SelectItem value="false">No</SelectItem>
                            </SelectContent>
                          </Select>
                        )}
                        {field.fieldType === FieldType.Dropdown && (
                          <Select
                            value={fieldData[field.fieldKey] || ''}
                            onValueChange={(value) => setFieldData(prev => ({ ...prev, [field.fieldKey]: value }))}
                          >
                            <SelectTrigger className="glass-input">
                              <SelectValue placeholder={`Select ${field.displayName.toLowerCase()}`} />
                            </SelectTrigger>
                            <SelectContent>
                              {/* This would be populated from validation rules or a separate options field */}
                              <SelectItem value="option1">Option 1</SelectItem>
                              <SelectItem value="option2">Option 2</SelectItem>
                              <SelectItem value="option3">Option 3</SelectItem>
                            </SelectContent>
                          </Select>
                        )}
                      </div>
                    ))}
                  </div>
                </CardContent>
              </Card>
            )}

            {/* Excel Data Table */}
            {dataSourceType === 'excel' && excelData && (
              <Card className="glass-panel border-glass-border">
                <CardHeader>
                  <CardTitle className="flex items-center gap-2">
                    <FileSpreadsheet className="h-5 w-5 text-green-500" />
                    Excel Data ({excelData.data.length} rows)
                  </CardTitle>
                  <CardDescription>
                    Data from {excelData.fileName} uploaded on {new Date(excelData.uploadedAt).toLocaleDateString()}
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  <div className="border rounded-lg overflow-hidden">
                    <div className="overflow-x-auto max-h-96 overflow-y-auto">
                      <table className="w-full text-sm">
                        <thead className="bg-muted sticky top-0">
                          <tr>
                            {excelData.headers.map((header, index) => (
                              <th key={index} className="px-4 py-3 text-left font-medium">
                                {header}
                              </th>
                            ))}
                          </tr>
                        </thead>
                        <tbody>
                          {excelData.data.map((row, rowIndex) => (
                            <tr key={rowIndex} className="border-b hover:bg-muted/50">
                              {excelData.headers.map((header, colIndex) => (
                                <td key={colIndex} className="px-4 py-3">
                                  {row[header] || ''}
                                </td>
                              ))}
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  </div>
                </CardContent>
              </Card>
            )}

            <EmployeeSelector
              selectedEmployees={selectedEmployees}
              onSelectionChange={setSelectedEmployees}
              onGenerate={handleGenerate}
              onSendEmail={handleSendEmail}
            />
          </div>
        }
      />

            {/* Dialogs */}
      <Dialog open={showTemplateDialog} onOpenChange={setShowTemplateDialog}>
        <DialogContent className="dialog-panel max-w-4xl h-[80vh] flex flex-col">
          <DialogHeader className="flex-shrink-0">
            <DialogTitle className="flex items-center gap-2">
              <FileText className="h-5 w-5 text-neon-blue" />
              Select {tab.name} Template
            </DialogTitle>
            <DialogDescription>
              Choose an existing template or upload a new one to generate letters
            </DialogDescription>
          </DialogHeader>

          <Tabs defaultValue="existing" className="flex-1 flex flex-col min-h-0">
            <TabsList className="grid w-full grid-cols-2">
              <TabsTrigger value="existing">Existing Templates</TabsTrigger>
              <TabsTrigger value="upload">Upload New</TabsTrigger>
            </TabsList>

            <TabsContent value="existing" className="flex-1 mt-4 min-h-0">
              <ScrollArea className="h-full">
                {templates.length === 0 ? (
                  <div className="text-center py-12">
                    <FileText className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
                    <h3 className="text-lg font-semibold mb-2">No Templates Available</h3>
                    <p className="text-muted-foreground">
                      No templates available for {tab.name.toLowerCase()}. Please create a template first.
                    </p>
        </div>
                ) : (
                  <div className="space-y-4">
                    {templates.map((template) => (
                      <Card 
                        key={template.id}
                        className="cursor-pointer transition-colors hover:bg-muted/50"
                        onClick={() => {
                          // Convert TabTemplate to DocumentTemplate format
                          const documentTemplate: DocumentTemplate = {
                            id: template.id,
                            name: template.name,
                            type: tab.letterType as any,
                            fileName: `${template.name.toLowerCase().replace(/\s+/g, '_')}.docx`,
                            fileUrl: '', // Will be populated by the service
                            fileSize: 0, // Will be populated by the service
                            mimeType: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
                            placeholders: template.placeholders,
                            version: 1,
                            isActive: true,
                            createdAt: new Date(),
                            updatedAt: new Date(),
                            createdBy: 'System'
                          };
                          handleTemplateSelect(documentTemplate);
                        }}
                      >
                        <CardContent className="p-6">
                          <div className="flex items-start justify-between">
                            <div className="flex-1">
                              <div className="flex items-center gap-2 mb-2">
                                <h3 className="font-semibold text-lg">{template.name}</h3>
                                <Badge variant="outline" className="text-xs">v1</Badge>
                                <Badge variant="default" className="text-xs">Active</Badge>
                              </div>
                              <p className="text-sm text-muted-foreground mb-2">
                                {template.name.toLowerCase().replace(/\s+/g, '_')}_template.docx
                              </p>
                              <p className="text-sm text-muted-foreground mb-3">
                                System • {new Date().toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })}
                              </p>
                              <div className="flex flex-wrap gap-1">
                                {template.placeholders.slice(0, 8).map(placeholder => (
                                  <Badge key={placeholder} variant="outline" className="text-xs">
                                    {placeholder}
                                  </Badge>
                                ))}
                                {template.placeholders.length > 8 && (
                                  <Badge variant="outline" className="text-xs">
                                    +{template.placeholders.length - 8} more
                                  </Badge>
                                )}
                              </div>
                            </div>
                            <div className="flex items-center gap-2 ml-4">
                              <Button variant="ghost" size="sm" className="h-8">
                                <Eye className="h-4 w-4 mr-1" />
                                Preview
              </Button>
                              <Button variant="ghost" size="sm" className="h-8">
                                <Download className="h-4 w-4 mr-1" />
                                Download
              </Button>
              </div>
              </div>
            </CardContent>
          </Card>
                    ))}
                  </div>
                )}
              </ScrollArea>
            </TabsContent>

            <TabsContent value="upload" className="flex-1 mt-4 min-h-0 overflow-y-auto">
              <div className="space-y-6">
                <div className="text-center py-8">
                  <Upload className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
                  <h3 className="text-lg font-semibold mb-2">Upload New Template</h3>
                  <p className="text-muted-foreground mb-6">
                    Upload a new {tab.name.toLowerCase()} template to get started
                  </p>
                </div>
                
                <div className="space-y-4">
                  <div className="space-y-2">
                    <Label htmlFor="template-name">Template Name</Label>
                    <Input 
                      id="template-name" 
                      placeholder="Enter template name"
                      value={templateUploadForm.name}
                      onChange={(e) => setTemplateUploadForm(prev => ({ ...prev, name: e.target.value }))}
                    />
                  </div>
                  
                  <div className="space-y-2">
                    <Label htmlFor="template-file">Template File</Label>
                    <div className="flex items-center gap-2">
                      <Input 
                        id="template-file"
                        type="file"
                        accept=".docx,.doc"
                        onChange={(e) => setTemplateUploadForm(prev => ({ ...prev, file: e.target.files?.[0] || null }))}
                        className="flex-1"
                      />
              <Button 
                variant="outline" 
                        onClick={handleTemplateUpload}
                        disabled={!templateUploadForm.name || !templateUploadForm.file || uploadingTemplate}
              >
                        {uploadingTemplate ? 'Uploading...' : 'Upload'}
              </Button>
                    </div>
                  </div>
        </div>
      </div>
            </TabsContent>
          </Tabs>
        </DialogContent>
      </Dialog>

      {selectedTemplate && (
        <DocumentPreviewDialog
          open={showPreviewDialog}
          onOpenChange={setShowPreviewDialog}
          template={selectedTemplate}
          employees={selectedEmployees}
          onDocumentsGenerated={handleDocumentsGenerated}
        />
      )}

      <EmailComposerDialog
        open={showEmailDialog}
        onOpenChange={setShowEmailDialog}
        employees={selectedEmployees}
        onEmailsSent={handleEmailsSent}
      />

      {/* Excel Upload Dialog */}
      <ExcelUploadDialog
        open={showExcelUpload}
        onOpenChange={setShowExcelUpload}
        tabId={tab.id}
        tabName={tab.name}
        onUploadSuccess={handleExcelUploadSuccess}
      />
    </div>
  );
}