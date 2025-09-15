import React, { useState, useEffect, useRef } from 'react';
import { Mail, Paperclip, Send, Eye, Upload, X, Loader2, CheckCircle, AlertCircle, User, FileText, Download, Eye as EyeIcon } from 'lucide-react';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from '../ui/dialog';
import { Button } from '../ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../ui/card';
import { Input } from '../ui/input';
import { Textarea } from '../ui/textarea';
import { Badge } from '../ui/badge';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../ui/tabs';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../ui/select';
import { ScrollArea } from '../ui/scroll-area';
import { Separator } from '../ui/separator';
import { Label } from '../ui/label';
import { Progress } from '../ui/progress';
import { documentService, EmailTemplate, EmailJob, EmailAttachment, Signature, DocumentTemplate } from '../../services/document.service';
import { Employee, apiService, EmailAttachmentRequest } from '../../services/api.service';
import { toast } from 'sonner';

interface EnhancedEmailDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  employees: Employee[];
  tabId: string;
  onEmailsSent: (jobs: EmailJob[]) => void;
}

interface EmployeeEmailData {
  employee: Employee;
  subject: string;
  content: string;
  attachments: EmailAttachment[];
  selectedTemplate: DocumentTemplate | null;
  selectedSignature: Signature | null;
  generatedPdfUrl?: string;
  isGeneratingPdf?: boolean;
}

interface EmailPreviewData {
  employee: Employee;
  pdfUrl: string;
  subject: string;
  content: string;
  attachments: EmailAttachment[];
}

export function EnhancedEmailDialog({
  open,
  onOpenChange,
  employees,
  tabId,
  onEmailsSent
}: EnhancedEmailDialogProps) {
  const [emailTemplates, setEmailTemplates] = useState<DocumentTemplate[]>([]);
  const [signatures, setSignatures] = useState<Signature[]>([]);
  const [emailData, setEmailData] = useState<EmployeeEmailData[]>([]);
  const [currentEmployeeIndex, setCurrentEmployeeIndex] = useState(0);
  const [sending, setSending] = useState(false);
  const [sendingProgress, setSendingProgress] = useState(0);
  const [sentJobs, setSentJobs] = useState<EmailJob[]>([]);
  const [activeTab, setActiveTab] = useState('compose');
  const [uploadingAttachment, setUploadingAttachment] = useState(false);
  const [previewData, setPreviewData] = useState<EmailPreviewData[]>([]);
  const [isGeneratingPreview, setIsGeneratingPreview] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (open) {
      loadTemplatesAndSignatures();
      initializeEmailData();
    }
  }, [open, employees]);

  const loadTemplatesAndSignatures = async () => {
    try {
      const [templates, sigs] = await Promise.all([
        documentService.getTemplates(),
        documentService.getSignatures()
      ]);
      setEmailTemplates(templates);
      setSignatures(sigs);
    } catch (error) {
      console.error('Failed to load templates and signatures:', error);
      toast.error('Failed to load templates and signatures');
    }
  };

  const initializeEmailData = () => {
    const data = employees.map(employee => ({
      employee,
      subject: `Document Request - ${employee.name}`,
      content: `Dear ${employee.name},\n\nPlease find attached your requested document.\n\nBest regards,\nHR Department`,
      attachments: [],
      selectedTemplate: null,
      selectedSignature: null,
      generatedPdfUrl: undefined,
      isGeneratingPdf: false
    }));
    setEmailData(data);
  };

  const handleTemplateSelect = (templateId: string, employeeIndex: number) => {
    const template = emailTemplates.find(t => t.id === templateId);
    if (template) {
      const updatedData = [...emailData];
      updatedData[employeeIndex].selectedTemplate = template;
      updatedData[employeeIndex].subject = replacePlaceholders(template.name, updatedData[employeeIndex].employee);
      updatedData[employeeIndex].content = replacePlaceholders(template.description || '', updatedData[employeeIndex].employee);
      setEmailData(updatedData);
    }
  };

  const handleSignatureSelect = (signatureId: string, employeeIndex: number) => {
    const signature = signatures.find(s => s.id === signatureId);
    if (signature) {
      const updatedData = [...emailData];
      updatedData[employeeIndex].selectedSignature = signature;
      setEmailData(updatedData);
    }
  };

  const replacePlaceholders = (text: string, employee: Employee) => {
    return text
      .replace(/\{EMPLOYEE_NAME\}/g, employee.name)
      .replace(/\{EMPLOYEE_ID\}/g, employee.employeeId)
      .replace(/\{COMPANY_NAME\}/g, 'DocHub Technologies')
      .replace(/\{DEPARTMENT\}/g, employee.department)
      .replace(/\{DESIGNATION\}/g, employee.designation);
  };

  const updateEmployeeEmail = (index: number, field: keyof EmployeeEmailData, value: any) => {
    const updatedData = [...emailData];
    updatedData[index] = { ...updatedData[index], [field]: value };
    setEmailData(updatedData);
  };

  const handleFileUpload = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const files = event.target.files;
    if (!files || files.length === 0) return;

    setUploadingAttachment(true);
    try {
      for (const file of Array.from(files)) {
        const attachment: EmailAttachment = {
          id: Date.now().toString() + Math.random(),
          fileName: file.name,
          fileUrl: URL.createObjectURL(file),
          fileSize: file.size,
          mimeType: file.type
        };

        const updatedData = [...emailData];
        updatedData[currentEmployeeIndex].attachments.push(attachment);
        setEmailData(updatedData);
      }
    } catch (error) {
      console.error('Failed to upload attachment:', error);
      toast.error('Failed to upload attachment');
    } finally {
      setUploadingAttachment(false);
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
    }
  };

  const removeAttachment = (employeeIndex: number, attachmentId: string) => {
    const updatedData = [...emailData];
    updatedData[employeeIndex].attachments = updatedData[employeeIndex].attachments.filter(
      att => att.id !== attachmentId
    );
    setEmailData(updatedData);
  };

  const generatePdfPreview = async () => {
    setIsGeneratingPreview(true);
    try {
      const previewData: EmailPreviewData[] = [];
      
      for (let i = 0; i < emailData.length; i++) {
        const data = emailData[i];
        if (!data.selectedTemplate || !data.selectedSignature) {
          toast.error(`Please select template and signature for ${data.employee.name}`);
          continue;
        }

        // Mark as generating
        const updatedData = [...emailData];
        updatedData[i].isGeneratingPdf = true;
        setEmailData(updatedData);

        try {
          // Generate PDF using the existing generate-preview endpoint
          const response = await apiService.requestBinary(`/Tab/${tabId}/generate-preview`, {
            method: 'POST',
            headers: {
              'Content-Type': 'application/json',
            },
            body: JSON.stringify({
              employeeId: data.employee.employeeId,
              templateId: data.selectedTemplate.id,
              signaturePath: data.selectedSignature.id,
              employeeData: data.employee.data || {}
            })
          });

          // Create blob URL for preview
          const pdfUrl = URL.createObjectURL(response);
          
          previewData.push({
            employee: data.employee,
            pdfUrl,
            subject: data.subject,
            content: data.content,
            attachments: data.attachments
          });

          // Update email data with PDF URL
          updatedData[i].generatedPdfUrl = pdfUrl;
          updatedData[i].isGeneratingPdf = false;
          setEmailData(updatedData);

        } catch (error) {
          console.error(`Failed to generate PDF for ${data.employee.name}:`, error);
          updatedData[i].isGeneratingPdf = false;
          setEmailData(updatedData);
          toast.error(`Failed to generate PDF for ${data.employee.name}`);
        }
      }

      setPreviewData(previewData);
      setActiveTab('preview');
      toast.success('PDF previews generated successfully');
    } catch (error) {
      console.error('Failed to generate PDF previews:', error);
      toast.error('Failed to generate PDF previews');
    } finally {
      setIsGeneratingPreview(false);
    }
  };

  const handleSendAll = async () => {
    setSending(true);
    setSendingProgress(0);
    setActiveTab('sending');

    try {
      const emailJobs: EmailJob[] = [];
      
      for (let i = 0; i < emailData.length; i++) {
        const data = emailData[i];
        
        if (!data.employee.email) {
          console.warn(`No email address for employee: ${data.employee.name}`);
          continue;
        }

        if (!data.selectedTemplate || !data.selectedSignature) {
          console.warn(`Missing template or signature for employee: ${data.employee.name}`);
          continue;
        }

        // Create email job with PDF attachment
        const attachments = [...data.attachments];
        if (data.generatedPdfUrl) {
          // Convert PDF URL to attachment
          const pdfResponse = await fetch(data.generatedPdfUrl);
          const pdfBlob = await pdfResponse.blob();
          const pdfAttachment: EmailAttachment = {
            id: `pdf_${data.employee.id}`,
            fileName: `${data.employee.name}_Document.pdf`,
            fileUrl: data.generatedPdfUrl,
            fileSize: pdfBlob.size,
            mimeType: 'application/pdf'
          };
          attachments.push(pdfAttachment);
        }

        const emailJob: EmailJob = {
          id: `job_${Date.now()}_${i}`,
          employeeId: data.employee.id,
          employeeName: data.employee.name,
          employeeEmail: data.employee.email,
          documentId: `DOC_${Date.now()}_${data.employee.id}`,
          emailTemplateId: data.selectedTemplate.id,
          subject: data.subject,
          content: data.content,
          attachments,
          status: 'pending',
          sentBy: 'Current User',
          createdAt: new Date()
        };

        emailJobs.push(emailJob);
      }

      // Send individual emails using the real API
      const updatedJobs: EmailJob[] = [];
      let successfulCount = 0;

      for (let i = 0; i < emailJobs.length; i++) {
        const job = emailJobs[i];
        const data = emailData[i];
        
        try {
          // Convert extra attachments to base64
          const extraAttachments = data.attachments.map(att => {
            // Convert file URL to base64
            return new Promise<EmailAttachmentRequest>((resolve) => {
              fetch(att.fileUrl)
                .then(response => response.blob())
                .then(blob => {
                  const reader = new FileReader();
                  reader.onload = () => {
                    resolve({
                      fileName: att.fileName,
                      mimeType: att.mimeType,
                      content: (reader.result as string).split(',')[1] // Remove data:type;base64, prefix
                    });
                  };
                  reader.readAsDataURL(blob);
                });
            });
          });

          const resolvedAttachments = await Promise.all(extraAttachments);

          // Send email with PDF
          const response = await apiService.sendEmailWithPdf(tabId, {
            employeeId: data.employee.employeeId,
            templateId: data.selectedTemplate!.id,
            signaturePath: data.selectedSignature?.id,
            subject: data.subject,
            content: data.content,
            employeeData: data.employee.data || {},
            extraAttachments: resolvedAttachments
          });

          if (response.success && response.data) {
            const updatedJob = {
              ...job,
              id: response.data.id,
              status: response.data.status as any,
              sentAt: response.data.sentAt || new Date()
            };
            
            updatedJobs.push(updatedJob);
            successfulCount++;
          } else {
            throw new Error(response.error?.message || 'Failed to send email');
          }
          
          // Update progress
          setSendingProgress(Math.round(((i + 1) / emailJobs.length) * 100));
          
        } catch (error) {
          console.error(`Failed to send email to ${job.employeeEmail}:`, error);
          const failedJob = {
            ...job,
            status: 'failed' as const,
            sentAt: new Date()
          };
          updatedJobs.push(failedJob);
        }
      }

      setSentJobs(updatedJobs);
      onEmailsSent(updatedJobs);
      toast.success(`Successfully sent ${successfulCount} out of ${emailJobs.length} emails`);
    } catch (error) {
      console.error('Failed to send emails:', error);
      toast.error('Failed to send emails');
    } finally {
      setSending(false);
    }
  };

  const currentEmployeeData = emailData[currentEmployeeIndex];
  const formatFileSize = (bytes: number) => {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-7xl h-[95vh] dialog-panel flex flex-col">
        <DialogHeader className="flex-shrink-0">
          <DialogTitle className="flex items-center gap-2">
            <Mail className="h-5 w-5 text-neon-green" />
            Send Documents via Email with PDF Generation
          </DialogTitle>
          <DialogDescription>
            Select templates and signatures, generate PDFs, and send emails to {employees.length} employee{employees.length !== 1 ? 's' : ''}
          </DialogDescription>
        </DialogHeader>

        <Tabs value={activeTab} onValueChange={setActiveTab} className="flex-1 flex flex-col min-h-0">
          <TabsList className="grid w-full grid-cols-4">
            <TabsTrigger value="compose">Compose</TabsTrigger>
            <TabsTrigger value="preview">PDF Preview</TabsTrigger>
            <TabsTrigger value="sending" disabled={!sending && sentJobs.length === 0}>
              {sending ? 'Sending...' : 'Status'}
            </TabsTrigger>
          </TabsList>

          <TabsContent value="compose" className="flex-1 mt-4 min-h-0">
            <div className="flex gap-6 h-full min-h-0">
              {/* Left Panel - Employee List */}
              <div className="w-80 space-y-4 overflow-y-auto min-h-0 pr-2">
                <Card className="dialog-content-solid">
                  <CardHeader>
                    <CardTitle className="text-sm">Recipients ({employees.length})</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <ScrollArea className="h-64">
                      <div className="space-y-2">
                        {employees.map((employee, index) => (
                          <div
                            key={employee.id}
                            className={`p-3 dialog-content-solid rounded-lg cursor-pointer hover:bg-muted/20 transition-colors ${
                              index === currentEmployeeIndex ? 'ring-2 ring-neon-green' : ''
                            }`}
                            onClick={() => setCurrentEmployeeIndex(index)}
                          >
                            <div className="flex items-start justify-between">
                              <div className="flex-1 min-w-0">
                                <p className="font-medium text-sm truncate">{employee.name}</p>
                                <p className="text-xs text-muted-foreground">
                                  {employee.email || 'No email'}
                                </p>
                                <div className="flex gap-1 mt-1">
                                  {emailData[index]?.selectedTemplate && (
                                    <Badge variant="outline" className="text-xs">
                                      Template
                                    </Badge>
                                  )}
                                  {emailData[index]?.selectedSignature && (
                                    <Badge variant="outline" className="text-xs">
                                      Signature
                                    </Badge>
                                  )}
                                  {emailData[index]?.attachments.length > 0 && (
                                    <Badge variant="outline" className="text-xs">
                                      {emailData[index].attachments.length} file{emailData[index].attachments.length !== 1 ? 's' : ''}
                                    </Badge>
                                  )}
                                </div>
                              </div>
                              {!employee.email && (
                                <AlertCircle className="h-4 w-4 text-orange-400" />
                              )}
                            </div>
                          </div>
                        ))}
                      </div>
                    </ScrollArea>
                  </CardContent>
                </Card>
              </div>

              {/* Right Panel - Email Composition */}
              <div className="flex-1 space-y-4 min-h-0 overflow-y-auto">
                {currentEmployeeData && (
                  <div className="space-y-4">
                    {/* Template Selection */}
                    <Card className="dialog-content-solid">
                      <CardHeader>
                        <CardTitle className="text-sm">Template & Signature Selection</CardTitle>
                      </CardHeader>
                      <CardContent className="space-y-4">
                        <div className="grid grid-cols-2 gap-4">
                          <div className="space-y-2">
                            <Label>Document Template</Label>
                            <Select 
                              value={currentEmployeeData.selectedTemplate?.id || ''} 
                              onValueChange={(value) => handleTemplateSelect(value, currentEmployeeIndex)}
                            >
                              <SelectTrigger>
                                <SelectValue placeholder="Select template" />
                              </SelectTrigger>
                              <SelectContent>
                                {emailTemplates.map(template => (
                                  <SelectItem key={template.id} value={template.id}>
                                    {template.name}
                                  </SelectItem>
                                ))}
                              </SelectContent>
                            </Select>
                          </div>
                          <div className="space-y-2">
                            <Label>Signature</Label>
                            <Select 
                              value={currentEmployeeData.selectedSignature?.id || ''} 
                              onValueChange={(value) => handleSignatureSelect(value, currentEmployeeIndex)}
                            >
                              <SelectTrigger>
                                <SelectValue placeholder="Select signature" />
                              </SelectTrigger>
                              <SelectContent>
                                {signatures.map(signature => (
                                  <SelectItem key={signature.id} value={signature.id}>
                                    {signature.name}
                                  </SelectItem>
                                ))}
                              </SelectContent>
                            </Select>
                          </div>
                        </div>
                      </CardContent>
                    </Card>

                    {/* Email Content */}
                    <Card className="dialog-content-solid">
                      <CardHeader>
                        <CardTitle className="text-sm flex items-center gap-2">
                          <User className="h-4 w-4" />
                          Email for {currentEmployeeData.employee.name}
                        </CardTitle>
                        <CardDescription>
                          {currentEmployeeData.employee.email || 'No email address available'}
                        </CardDescription>
                      </CardHeader>
                      <CardContent className="space-y-4">
                        {/* Attachments */}
                        <div className="space-y-1">
                          <Label className="text-sm">Extra Attachments</Label>
                          <div className="space-y-1">
                            {currentEmployeeData.attachments.length > 0 && (
                              <div className="space-y-1 max-h-20 overflow-y-auto">
                                {currentEmployeeData.attachments.map((attachment) => (
                                  <div key={attachment.id} className="flex items-center justify-between px-2 py-1 bg-muted/20 rounded text-xs">
                                    <div className="flex items-center gap-1 min-w-0 flex-1">
                                      <FileText className="h-3 w-3 text-muted-foreground flex-shrink-0" />
                                      <span className="truncate">{attachment.fileName}</span>
                                      <span className="text-muted-foreground flex-shrink-0">({formatFileSize(attachment.fileSize)})</span>
                                    </div>
                                    <Button
                                      variant="ghost"
                                      size="sm"
                                      className="h-5 w-5 p-0 flex-shrink-0"
                                      onClick={() => removeAttachment(currentEmployeeIndex, attachment.id)}
                                    >
                                      <X className="h-3 w-3" />
                                    </Button>
                                  </div>
                                ))}
                              </div>
                            )}
                            <Button
                              variant="outline"
                              size="sm"
                              onClick={() => fileInputRef.current?.click()}
                              disabled={uploadingAttachment}
                              className="w-full h-7 text-xs"
                            >
                              {uploadingAttachment ? (
                                <>
                                  <Loader2 className="mr-1 h-3 w-3 animate-spin" />
                                  Uploading...
                                </>
                              ) : (
                                <>
                                  <Upload className="mr-1 h-3 w-3" />
                                  Add Files
                                </>
                              )}
                            </Button>
                            <input
                              ref={fileInputRef}
                              type="file"
                              multiple
                              onChange={handleFileUpload}
                              className="hidden"
                              accept=".pdf,.doc,.docx,.txt,.jpg,.jpeg,.png,.gif,.zip"
                            />
                          </div>
                        </div>

                        {/* Subject */}
                        <div className="space-y-2">
                          <Label htmlFor="subject">Subject</Label>
                          <Input
                            id="subject"
                            value={currentEmployeeData.subject}
                            onChange={(e) => updateEmployeeEmail(currentEmployeeIndex, 'subject', e.target.value)}
                            placeholder="Email subject"
                          />
                        </div>

                        {/* Content */}
                        <div className="space-y-2">
                          <Label htmlFor="content">Message</Label>
                          <Textarea
                            id="content"
                            rows={8}
                            value={currentEmployeeData.content}
                            onChange={(e) => updateEmployeeEmail(currentEmployeeIndex, 'content', e.target.value)}
                            placeholder="Email content"
                            className="resize-none"
                          />
                        </div>
                      </CardContent>
                    </Card>
                  </div>
                )}
              </div>
            </div>
          </TabsContent>

          <TabsContent value="preview" className="flex-1 mt-4 min-h-0">
            <Card className="dialog-content-solid h-full flex flex-col">
              <CardHeader className="flex-shrink-0">
                <div className="flex items-center justify-between">
                  <CardTitle className="text-sm">PDF Preview</CardTitle>
                  <div className="flex items-center gap-2">
                    <Button
                      size="sm"
                      variant="outline"
                      disabled={currentEmployeeIndex === 0}
                      onClick={() => setCurrentEmployeeIndex(prev => prev - 1)}
                    >
                      ← Previous
                    </Button>
                    <span className="text-xs px-2">
                      {currentEmployeeIndex + 1} / {employees.length}
                    </span>
                    <Button
                      size="sm"
                      variant="outline"
                      disabled={currentEmployeeIndex === employees.length - 1}
                      onClick={() => setCurrentEmployeeIndex(prev => prev + 1)}
                    >
                      Next →
                    </Button>
                  </div>
                </div>
              </CardHeader>
              <CardContent className="flex-1 min-h-0 overflow-y-auto">
                {currentEmployeeData && (
                  <div className="space-y-4">
                    {currentEmployeeData.isGeneratingPdf ? (
                      <div className="flex items-center justify-center h-64">
                        <div className="text-center">
                          <Loader2 className="h-8 w-8 animate-spin mx-auto mb-2" />
                          <p>Generating PDF for {currentEmployeeData.employee.name}...</p>
                        </div>
                      </div>
                    ) : currentEmployeeData.generatedPdfUrl ? (
                      <div className="space-y-4">
                        <div className="grid grid-cols-2 gap-4 text-sm">
                          <div>
                            <Label className="text-xs text-muted-foreground">To:</Label>
                            <p>{currentEmployeeData.employee.email || 'No email'}</p>
                          </div>
                          <div>
                            <Label className="text-xs text-muted-foreground">Subject:</Label>
                            <p>{currentEmployeeData.subject}</p>
                          </div>
                        </div>
                        
                        <Separator />
                        
                        <div className="bg-white text-black p-4 rounded border">
                          <div className="mb-4">
                            <h4 className="font-medium mb-2">Email Content:</h4>
                            <pre className="whitespace-pre-wrap font-sans text-sm">
                              {currentEmployeeData.content}
                            </pre>
                          </div>
                          
                          <div className="border-t pt-4">
                            <h4 className="font-medium mb-2">Generated Document:</h4>
                            <div className="flex items-center gap-2">
                              <FileText className="h-4 w-4" />
                              <span className="text-sm">{currentEmployeeData.employee.name}_Document.pdf</span>
                              <Button
                                size="sm"
                                variant="outline"
                                onClick={() => window.open(currentEmployeeData.generatedPdfUrl, '_blank')}
                              >
                                <EyeIcon className="h-3 w-3 mr-1" />
                                Preview
                              </Button>
                              <Button
                                size="sm"
                                variant="outline"
                                onClick={() => {
                                  const link = document.createElement('a');
                                  link.href = currentEmployeeData.generatedPdfUrl!;
                                  link.download = `${currentEmployeeData.employee.name}_Document.pdf`;
                                  link.click();
                                }}
                              >
                                <Download className="h-3 w-3 mr-1" />
                                Download
                              </Button>
                            </div>
                          </div>

                          {currentEmployeeData.attachments.length > 0 && (
                            <div className="mt-4 pt-4 border-t">
                              <h4 className="font-medium mb-2">
                                Extra Attachments ({currentEmployeeData.attachments.length}):
                              </h4>
                              <ul className="text-sm space-y-1">
                                {currentEmployeeData.attachments.map(att => (
                                  <li key={att.id} className="flex items-center gap-2">
                                    <Paperclip className="h-3 w-3" />
                                    {att.fileName} ({formatFileSize(att.fileSize)})
                                  </li>
                                ))}
                              </ul>
                            </div>
                          )}
                        </div>
                      </div>
                    ) : (
                      <div className="flex items-center justify-center h-64">
                        <div className="text-center">
                          <FileText className="h-12 w-12 text-muted-foreground mx-auto mb-2" />
                          <p className="text-muted-foreground">No PDF generated yet</p>
                          <p className="text-sm text-muted-foreground">Click "Generate PDF Preview" to create the document</p>
                        </div>
                      </div>
                    )}
                  </div>
                )}
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="sending" className="flex-1 mt-4 min-h-0">
            <Card className="dialog-content-solid h-full flex flex-col">
              <CardHeader className="flex-shrink-0">
                <CardTitle className="text-sm">Email Status</CardTitle>
                <CardDescription>
                  {sending ? 'Sending emails...' : `Sent ${sentJobs.length} emails`}
                </CardDescription>
              </CardHeader>
              <CardContent className="flex-1 min-h-0 overflow-y-auto">
                {sending && (
                  <div className="space-y-4 mb-6">
                    <Progress value={sendingProgress} className="w-full" />
                    <p className="text-center text-sm text-muted-foreground">
                      {sendingProgress}% complete
                    </p>
                  </div>
                )}

                <ScrollArea className="h-96">
                  <div className="space-y-3">
                    {sentJobs.map((job, index) => (
                      <div
                        key={job.id}
                        className="flex items-center justify-between p-3 dialog-content-solid rounded-lg"
                      >
                        <div className="flex items-center gap-3">
                          <div className={`w-8 h-8 rounded-full flex items-center justify-center ${
                            job.status === 'sent' ? 'bg-green-500/20' : 
                            job.status === 'failed' ? 'bg-red-500/20' :
                            job.status === 'sending' ? 'bg-blue-500/20' : 'bg-orange-500/20'
                          }`}>
                            {job.status === 'sent' ? (
                              <CheckCircle className="h-4 w-4 text-green-400" />
                            ) : job.status === 'failed' ? (
                              <AlertCircle className="h-4 w-4 text-red-400" />
                            ) : job.status === 'sending' ? (
                              <Loader2 className="h-4 w-4 animate-spin text-blue-400" />
                            ) : (
                              <Mail className="h-4 w-4 text-orange-400" />
                            )}
                          </div>
                          <div>
                            <p className="font-medium">{job.employeeName}</p>
                            <p className="text-sm text-muted-foreground">
                              {job.employeeEmail}
                            </p>
                          </div>
                        </div>

                        <Badge
                          variant="outline"
                          className={
                            job.status === 'sent' ? 'text-green-400 border-green-500/30' :
                            job.status === 'failed' ? 'text-red-400 border-red-500/30' :
                            job.status === 'sending' ? 'text-blue-400 border-blue-500/30' :
                            'text-orange-400 border-orange-500/30'
                          }
                        >
                          {job.status}
                        </Badge>
                      </div>
                    ))}
                  </div>
                </ScrollArea>
              </CardContent>
            </Card>
          </TabsContent>
        </Tabs>

        {/* Footer Actions */}
        <div className="flex items-center justify-between pt-4 border-t">
          <div className="text-sm text-muted-foreground">
            {employees.filter(emp => emp.email).length} of {employees.length} employees have email addresses
          </div>
          
          <div className="flex items-center gap-2">
            <Button variant="outline" onClick={() => onOpenChange(false)}>
              Cancel
            </Button>
            
            {activeTab === 'compose' && (
              <Button
                onClick={generatePdfPreview}
                disabled={isGeneratingPreview || emailData.some(data => !data.selectedTemplate || !data.selectedSignature)}
                className="neon-border-blue bg-blue-50 text-blue-700 hover:bg-blue-100 dark:bg-blue-950 dark:text-blue-300 dark:hover:bg-blue-900"
              >
                {isGeneratingPreview ? (
                  <>
                    <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                    Generating PDFs...
                  </>
                ) : (
                  <>
                    <Eye className="h-4 w-4 mr-2" />
                    Generate PDF Preview
                  </>
                )}
              </Button>
            )}
            
            {(activeTab === 'preview' || activeTab === 'compose') && (
              <Button
                onClick={handleSendAll}
                disabled={sending || employees.filter(emp => emp.email).length === 0 || !emailData.some(data => data.generatedPdfUrl)}
                className="neon-border-green bg-green-50 text-green-700 hover:bg-green-100 dark:bg-green-950 dark:text-green-300 dark:hover:bg-green-900"
              >
                {sending ? (
                  <>
                    <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                    Sending...
                  </>
                ) : (
                  <>
                    <Send className="h-4 w-4 mr-2" />
                    Send All ({employees.filter(emp => emp.email).length})
                  </>
                )}
              </Button>
            )}
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}
