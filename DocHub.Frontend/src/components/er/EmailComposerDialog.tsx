import React, { useState, useEffect, useRef } from 'react';
import { Mail, Paperclip, Send, Eye, Upload, X, Loader2, CheckCircle, AlertCircle, User, FileText } from 'lucide-react';
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
import { documentService, EmailTemplate, EmailJob, EmailAttachment, Signature } from '../../services/document.service';
import { Employee, apiService } from '../../services/api.service';

interface EmailComposerDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  employees: Employee[];
  onEmailsSent: (jobs: EmailJob[]) => void;
}

interface EmployeeEmailData {
  employee: Employee;
  subject: string;
  content: string;
  attachments: EmailAttachment[];
}

export function EmailComposerDialog({
  open,
  onOpenChange,
  employees,
  onEmailsSent
}: EmailComposerDialogProps) {
  const [emailTemplates, setEmailTemplates] = useState<EmailTemplate[]>([]);
  const [signatures, setSignatures] = useState<Signature[]>([]);
  const [selectedTemplateId, setSelectedTemplateId] = useState<string>('');
  const [selectedSignatureId, setSelectedSignatureId] = useState<string>('');
  const [emailData, setEmailData] = useState<EmployeeEmailData[]>([]);
  const [currentEmployeeIndex, setCurrentEmployeeIndex] = useState(0);
  const [sending, setSending] = useState(false);
  const [sendingProgress, setSendingProgress] = useState(0);
  const [sentJobs, setSentJobs] = useState<EmailJob[]>([]);
  const [activeTab, setActiveTab] = useState('compose');
  const [uploadingAttachment, setUploadingAttachment] = useState(false);
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
        documentService.getEmailTemplates(),
        documentService.getSignatures()
      ]);
      setEmailTemplates(templates);
      setSignatures(sigs);
      
      if (templates.length > 0) {
        setSelectedTemplateId(templates[0].id);
      }
      if (sigs.length > 0) {
        setSelectedSignatureId(sigs[0].id);
      }
    } catch (error) {
      console.error('Failed to load templates and signatures:', error);
    }
  };

  const initializeEmailData = () => {
    const data = employees.map(employee => ({
      employee,
      subject: `Document Request - ${employee.name}`,
      content: `Dear ${employee.name},\n\nPlease find attached your requested document.\n\nBest regards,\nHR Department`,
      attachments: []
    }));
    setEmailData(data);
  };

  const handleTemplateSelect = (templateId: string) => {
    setSelectedTemplateId(templateId);
    const template = emailTemplates.find(t => t.id === templateId);
    
    if (template) {
      const updatedData = emailData.map(data => ({
        ...data,
        subject: replacePlaceholders(template.subject, data.employee),
        content: replacePlaceholders(template.content, data.employee)
      }));
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

        // Add to current employee's attachments
        const updatedData = [...emailData];
        updatedData[currentEmployeeIndex].attachments.push(attachment);
        setEmailData(updatedData);
      }
    } catch (error) {
      console.error('Failed to upload attachment:', error);
    } finally {
      setUploadingAttachment(false);
      // Reset file input value so the same file can be selected again
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
    }
  };

  const triggerFileUpload = () => {
    if (fileInputRef.current) {
      fileInputRef.current.click();
    }
  };

  const removeAttachment = (employeeIndex: number, attachmentId: string) => {
    const updatedData = [...emailData];
    updatedData[employeeIndex].attachments = updatedData[employeeIndex].attachments.filter(
      att => att.id !== attachmentId
    );
    setEmailData(updatedData);
  };

  const handleSendAll = async () => {
    setSending(true);
    setSendingProgress(0);
    setActiveTab('sending');

    try {
      // Use the new bulk email API
      const response = await apiService.sendBulkEmails({
        employees: emailData.map(data => ({
          employeeId: data.employee.id,
          employeeName: data.employee.name,
          employeeEmail: data.employee.email,
          documentId: 'DOC_' + Date.now() + '_' + data.employee.id
        })),
        subject: emailData[0]?.subject || 'Document Email',
        body: emailData[0]?.content || 'Please find attached your document.',
        attachments: emailData.flatMap(data => data.attachments.map(att => att.fileUrl)),
        emailTemplateId: selectedTemplateId || undefined
      });

      if (response.success && response.data) {
        // Create mock email jobs for display
        const jobs: EmailJob[] = emailData.map((data, index) => ({
          id: `job_${Date.now()}_${index}`,
          employeeId: data.employee.id,
          employeeName: data.employee.name,
          employeeEmail: data.employee.email,
          documentId: 'DOC_' + Date.now() + '_' + data.employee.id,
          emailTemplateId: selectedTemplateId || undefined,
          subject: data.subject,
          content: data.content,
          attachments: data.attachments,
          status: response.data.successfulJobs > index ? 'sent' : 'failed',
          sentBy: 'Current User',
          createdAt: new Date(),
          sentAt: new Date()
        }));

        setSentJobs(jobs);

        // Simulate sending progress
        for (let i = 0; i <= 100; i += 10) {
          setSendingProgress(i);
          await new Promise(resolve => setTimeout(resolve, 200));
        }

        onEmailsSent(jobs);
      } else {
        throw new Error(response.error?.message || 'Failed to send bulk emails');
      }
    } catch (error) {
      console.error('Failed to send emails:', error);
    } finally {
      setSending(false);
    }
  };

  const currentEmployeeData = emailData[currentEmployeeIndex];
  const selectedTemplate = emailTemplates.find(t => t.id === selectedTemplateId);
  const selectedSignature = signatures.find(s => s.id === selectedSignatureId);

  const formatFileSize = (bytes: number) => {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-6xl h-[90vh] dialog-panel flex flex-col">
        <DialogHeader className="flex-shrink-0">
          <DialogTitle className="flex items-center gap-2">
            <Mail className="h-5 w-5 text-neon-green" />
            Send Documents via Email
          </DialogTitle>
          <DialogDescription>
            Compose and send emails with document attachments to {employees.length} employee{employees.length !== 1 ? 's' : ''}
          </DialogDescription>
        </DialogHeader>

        <Tabs value={activeTab} onValueChange={setActiveTab} className="flex-1 flex flex-col min-h-0">
          <TabsList className="grid w-full grid-cols-3">
            <TabsTrigger value="compose">Compose</TabsTrigger>
            <TabsTrigger value="preview">Preview</TabsTrigger>
            <TabsTrigger value="sending" disabled={!sending && sentJobs.length === 0}>
              {sending ? 'Sending...' : 'Status'}
            </TabsTrigger>
          </TabsList>

          <TabsContent value="compose" className="flex-1 mt-4 min-h-0">
            <div className="flex gap-6 h-full min-h-0">
              {/* Left Panel - Template & Settings */}
              <div className="w-80 space-y-4 overflow-y-auto min-h-0 pr-2">
                {/* Template Selection */}
                <Card className="dialog-content-solid">
                  <CardHeader>
                    <CardTitle className="text-sm">Email Template</CardTitle>
                  </CardHeader>
                  <CardContent className="space-y-3">
                    <Select value={selectedTemplateId} onValueChange={handleTemplateSelect}>
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

                    {selectedTemplate && (
                      <div className="text-xs text-muted-foreground">
                        <p>Subject: {selectedTemplate.subject}</p>
                        <p className="mt-1">
                          Placeholders: {selectedTemplate.placeholders.join(', ')}
                        </p>
                      </div>
                    )}
                  </CardContent>
                </Card>

                {/* Employee List */}
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
                                {emailData[index]?.attachments.length > 0 && (
                                  <Badge variant="outline" className="text-xs mt-1">
                                    {emailData[index].attachments.length} attachment{emailData[index].attachments.length !== 1 ? 's' : ''}
                                  </Badge>
                                )}
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
                        <Label className="text-sm">Attachments</Label>
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
                                    onClick={() => {
                                      const updatedData = [...emailData];
                                      updatedData[currentEmployeeIndex].attachments = 
                                        updatedData[currentEmployeeIndex].attachments.filter(a => a.id !== attachment.id);
                                      setEmailData(updatedData);
                                    }}
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
                            onClick={triggerFileUpload}
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
                          rows={10}
                          value={currentEmployeeData.content}
                          onChange={(e) => updateEmployeeEmail(currentEmployeeIndex, 'content', e.target.value)}
                          placeholder="Email content"
                          className="resize-none"
                        />
                      </div>


                    </CardContent>
                  </Card>
                )}
              </div>
            </div>
          </TabsContent>

          <TabsContent value="preview" className="flex-1 mt-4 min-h-0">
            <Card className="dialog-content-solid h-full flex flex-col">
              <CardHeader className="flex-shrink-0">
                <div className="flex items-center justify-between">
                  <CardTitle className="text-sm">Email Preview</CardTitle>
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
                      <pre className="whitespace-pre-wrap font-sans">
                        {currentEmployeeData.content}
                      </pre>
                      
                      {currentEmployeeData.attachments.length > 0 && (
                        <div className="mt-4 pt-4 border-t">
                          <p className="text-sm font-medium mb-2">
                            Attachments ({currentEmployeeData.attachments.length}):
                          </p>
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
                onClick={() => setActiveTab('preview')}
                className="neon-border bg-card text-neon-blue hover:bg-neon-blue hover:text-white"
              >
                <Eye className="h-4 w-4 mr-2" />
                Preview All
              </Button>
            )}
            
            {(activeTab === 'preview' || activeTab === 'compose') && (
              <Button
                onClick={handleSendAll}
                disabled={sending || employees.filter(emp => emp.email).length === 0}
                className="neon-border bg-card text-neon-green hover:bg-neon-green hover:text-white"
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