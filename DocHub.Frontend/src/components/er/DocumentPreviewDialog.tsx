import React, { useState, useEffect } from 'react';
import { FileText, Download, Eye, Image as ImageIcon, Signature, X, Loader2, Edit3, Save, FileDown, Send } from 'lucide-react';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from '../ui/dialog';
import { Button } from '../ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../ui/card';
import { Badge } from '../ui/badge';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../ui/select';
import { ScrollArea } from '../ui/scroll-area';
import { Separator } from '../ui/separator';
import { Label } from '../ui/label';
import { Textarea } from '../ui/textarea';
import { Input } from '../ui/input';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../ui/tabs';
import { Switch } from '../ui/switch';
import { documentService, DocumentTemplate, Signature as SignatureType, GeneratedDocument } from '../../services/document.service';
import { Employee } from '../../services/api.service';
import { notify } from '../../utils/notifications';
import { handleError } from '../../utils/errorHandler';
import { Loading } from '../ui/loading';

interface DocumentPreviewDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  template: DocumentTemplate;
  employees: Employee[];
  onDocumentsGenerated: (documents: GeneratedDocument[]) => void;
}

export function DocumentPreviewDialog({
  open,
  onOpenChange,
  template,
  employees,
  onDocumentsGenerated
}: DocumentPreviewDialogProps) {
  console.log('🔍 [DocumentPreviewDialog] Dialog opened');
  console.log('🔍 [DocumentPreviewDialog] Template:', template);
  console.log('🔍 [DocumentPreviewDialog] Employees:', employees);
  const [signatures, setSignatures] = useState<SignatureType[]>([]);
  const [selectedSignatureId, setSelectedSignatureId] = useState<string>('');
  const [generatedDocuments, setGeneratedDocuments] = useState<GeneratedDocument[]>([]);
  const [currentPreviewIndex, setCurrentPreviewIndex] = useState(0);
  const [generating, setGenerating] = useState(false);
  const [loading, setLoading] = useState(false);
  const [editing, setEditing] = useState(false);
  const [editedContent, setEditedContent] = useState<{ [key: string]: string }>({});
  const [saving, setSaving] = useState(false);
  const [activeTab, setActiveTab] = useState('preview');

  useEffect(() => {
    if (open) {
      loadSignatures();
      generatePreviews();
    }
  }, [open, template, employees]);

  const loadSignatures = async () => {
    setLoading(true);
    try {
      const data = await documentService.getSignatures();
      setSignatures(data);
      if (data.length > 0) {
        setSelectedSignatureId(data[0].id);
      }
    } catch (error) {
      console.error('Failed to load signatures:', error);
    } finally {
      setLoading(false);
    }
  };

  const generatePreviews = async () => {
    setGenerating(true);
    try {
      const docs = await Promise.all(
        employees.map(employee =>
          documentService.generateDocument(
            template.id,
            employee,
            selectedSignatureId || undefined
          )
        )
      );
      setGeneratedDocuments(docs);
      
      // Initialize edited content with current content
      const initialEditedContent: { [key: string]: string } = {};
      docs.forEach((doc, index) => {
        initialEditedContent[index.toString()] = doc.content;
      });
      setEditedContent(initialEditedContent);
    } catch (error) {
      handleError(error, 'Generate document previews');
    } finally {
      setGenerating(false);
    }
  };

  const handleEditContent = (index: number, content: string) => {
    setEditedContent(prev => ({
      ...prev,
      [index.toString()]: content
    }));
  };

  const handleSaveChanges = async () => {
    setSaving(true);
    try {
      // Update the generated documents with edited content
      const updatedDocuments = generatedDocuments.map((doc, index) => ({
        ...doc,
        content: editedContent[index.toString()] || doc.content
      }));
      
      setGeneratedDocuments(updatedDocuments);
      setEditing(false);
      notify.success('Document changes saved successfully');
    } catch (error) {
      handleError(error, 'Save document changes');
    } finally {
      setSaving(false);
    }
  };

  const handleGenerateFinal = async () => {
    setGenerating(true);
    try {
      // Use edited content if available
      const finalDocuments = generatedDocuments.map((doc, index) => ({
        ...doc,
        content: editedContent[index.toString()] || doc.content
      }));
      
      onDocumentsGenerated(finalDocuments);
      notify.success(`${finalDocuments.length} documents generated successfully`);
      onOpenChange(false);
    } catch (error) {
      handleError(error, 'Generate final documents');
    } finally {
      setGenerating(false);
    }
  };

  const handleDownloadDocument = async (document: GeneratedDocument) => {
    try {
      const blob = new Blob([document.content], { type: 'text/plain' });
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `${document.employeeName}_${template.name}.txt`;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      window.URL.revokeObjectURL(url);
      
      notify.success('Document downloaded successfully');
    } catch (error) {
      handleError(error, 'Download document');
    }
  };

  const handleSendEmail = async (document: GeneratedDocument) => {
    try {
      // Simulate email sending
      await new Promise(resolve => setTimeout(resolve, 2000));
      notify.emailSent(1);
    } catch (error) {
      handleError(error, 'Send email');
    }
  };

  const handleSignatureChange = async (signatureId: string) => {
    setSelectedSignatureId(signatureId === "none" ? "" : signatureId);
    // Regenerate previews with new signature
    await generatePreviews();
  };

  const handleDownload = async (documentId: string, employee: Employee) => {
    try {
      const blob = await documentService.downloadDocument(documentId, 'pdf');
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `${employee.name}_${template.name}.pdf`;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      URL.revokeObjectURL(url);
    } catch (error) {
      console.error('Failed to download document:', error);
    }
  };

  const handleDownloadAll = async () => {
    for (const [index, doc] of generatedDocuments.entries()) {
      await handleDownload(doc.id, employees[index]);
      // Add small delay between downloads
      await new Promise(resolve => setTimeout(resolve, 500));
    }
  };

  const handleGenerate = () => {
    onDocumentsGenerated(generatedDocuments);
    onOpenChange(false);
  };

  const currentEmployee = employees[currentPreviewIndex];
  const currentDocument = generatedDocuments[currentPreviewIndex];
  const selectedSignature = signatures.find(sig => sig.id === selectedSignatureId);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-7xl h-[90vh] dialog-panel flex flex-col">
        <DialogHeader className="flex-shrink-0">
          <DialogTitle className="flex items-center gap-2">
            <FileText className="h-5 w-5 text-neon-blue" />
            Document Preview - {template.name}
          </DialogTitle>
          <DialogDescription>
            Preview and customize documents before generation
          </DialogDescription>
        </DialogHeader>

        <div className="flex flex-1 gap-6 min-h-0 overflow-hidden">
          {/* Left Panel - Controls */}
          <div className="w-80 space-y-4 overflow-y-auto min-h-0 pr-2">
            {/* Signature Selection */}
            <Card className="dialog-content-solid">
              <CardHeader>
                <CardTitle className="text-sm flex items-center gap-2">
                  <Signature className="h-4 w-4" />
                  Signature
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                {loading ? (
                  <div className="animate-pulse">
                    <div className="h-10 bg-muted rounded" />
                  </div>
                ) : (
                  <Select value={selectedSignatureId} onValueChange={handleSignatureChange}>
                    <SelectTrigger>
                      <SelectValue placeholder="Select signature" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="none">No Signature</SelectItem>
                      {signatures.map(signature => (
                        <SelectItem key={signature.id} value={signature.id}>
                          {signature.name}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}

                {selectedSignature && (
                  <div className="dialog-content-solid rounded-lg p-3">
                    <div className="flex items-center gap-3">
                      <div className="w-12 h-12 bg-muted rounded flex items-center justify-center">
                        <ImageIcon className="h-6 w-6 text-muted-foreground" />
                      </div>
                      <div>
                        <p className="font-medium text-sm">{selectedSignature.name}</p>
                        <p className="text-xs text-muted-foreground">
                          Created by {selectedSignature.createdBy}
                        </p>
                      </div>
                    </div>
                  </div>
                )}
              </CardContent>
            </Card>

            {/* Employee List */}
            <Card className="dialog-content-solid">
              <CardHeader>
                <CardTitle className="text-sm">Employees ({employees.length})</CardTitle>
              </CardHeader>
              <CardContent>
                <ScrollArea className="h-64">
                  <div className="space-y-2">
                    {employees.map((employee, index) => (
                      <div
                        key={employee.id}
                        className={`p-3 dialog-content-solid rounded-lg cursor-pointer hover:bg-muted/20 transition-colors ${
                          index === currentPreviewIndex ? 'ring-2 ring-neon-blue' : ''
                        }`}
                        onClick={() => setCurrentPreviewIndex(index)}
                      >
                        <div className="flex items-center justify-between">
                          <div>
                            <p className="font-medium text-sm">{employee.name}</p>
                            <p className="text-xs text-muted-foreground">
                              {employee.employeeId}
                            </p>
                          </div>
                          <Badge variant="outline" className="text-xs">
                            {employee.department}
                          </Badge>
                        </div>
                      </div>
                    ))}
                  </div>
                </ScrollArea>
              </CardContent>
            </Card>

            {/* Editing Controls */}
            <Card className="dialog-content-solid">
              <CardHeader>
                <CardTitle className="text-sm flex items-center gap-2">
                  <Edit3 className="h-4 w-4" />
                  Document Editing
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-3">
                <div className="flex items-center justify-between">
                  <Label className="text-sm">Edit Mode</Label>
                  <Switch
                    checked={editing}
                    onCheckedChange={setEditing}
                  />
                </div>
                
                {editing && (
                  <div className="space-y-2">
                    <Button
                      onClick={handleSaveChanges}
                      disabled={saving}
                      size="sm"
                      className="w-full"
                    >
                      {saving ? (
                        <Loading size="sm" className="mr-2" />
                      ) : (
                        <Save className="h-4 w-4 mr-2" />
                      )}
                      {saving ? 'Saving...' : 'Save Changes'}
                    </Button>
                  </div>
                )}
              </CardContent>
            </Card>

            {/* Actions */}
            <div className="space-y-3">
              <Button
                onClick={() => generatedDocuments.forEach(handleDownloadDocument)}
                disabled={generating || generatedDocuments.length === 0}
                className="w-full neon-border bg-card text-neon-green hover:bg-neon-green hover:text-white"
              >
                <Download className="h-4 w-4 mr-2" />
                Download All ({generatedDocuments.length})
              </Button>

              <Button
                onClick={handleGenerateFinal}
                disabled={generating || generatedDocuments.length === 0}
                className="w-full neon-border bg-card text-neon-blue hover:bg-neon-blue hover:text-white"
              >
                {generating ? (
                  <>
                    <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                    Generating...
                  </>
                ) : (
                  <>
                    <FileText className="h-4 w-4 mr-2" />
                    Generate Final Documents
                  </>
                )}
              </Button>
            </div>
          </div>

          {/* Right Panel - Preview */}
          <div className="flex-1 flex flex-col min-h-0">
            <Card className="dialog-content-solid flex-1 flex flex-col min-h-0">
              <CardHeader>
                <div className="flex items-center justify-between">
                  <div>
                    <CardTitle className="text-sm">
                      Document Preview
                      {currentEmployee && (
                        <span className="text-muted-foreground ml-2">
                          - {currentEmployee.name}
                        </span>
                      )}
                    </CardTitle>
                    <CardDescription className="flex items-center gap-4 mt-1">
                      <span>Template: {template.name}</span>
                      {selectedSignature && (
                        <span>Signature: {selectedSignature.name}</span>
                      )}
                    </CardDescription>
                  </div>

                  {currentDocument && currentEmployee && (
                    <div className="flex items-center gap-2">
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={() => handleDownloadDocument(currentDocument)}
                      >
                        <Download className="h-4 w-4 mr-1" />
                        Download
                      </Button>
                      
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={() => handleSendEmail(currentDocument)}
                      >
                        <Send className="h-4 w-4 mr-1" />
                        Send Email
                      </Button>

                      <div className="flex items-center gap-1">
                        <Button
                          size="sm"
                          variant="outline"
                          disabled={currentPreviewIndex === 0}
                          onClick={() => setCurrentPreviewIndex(prev => prev - 1)}
                        >
                          ←
                        </Button>
                        <span className="text-xs px-2">
                          {currentPreviewIndex + 1} / {employees.length}
                        </span>
                        <Button
                          size="sm"
                          variant="outline"
                          disabled={currentPreviewIndex === employees.length - 1}
                          onClick={() => setCurrentPreviewIndex(prev => prev + 1)}
                        >
                          →
                        </Button>
                      </div>
                    </div>
                  )}
                </div>
              </CardHeader>

              <Separator />

              <CardContent className="flex-1 p-0 min-h-0">
                <ScrollArea className="h-full">
                  {generating ? (
                    <div className="flex items-center justify-center h-full">
                      <div className="text-center space-y-4">
                        <Loader2 className="h-12 w-12 animate-spin text-neon-blue mx-auto" />
                        <div>
                          <p className="font-medium">Generating Documents...</p>
                          <p className="text-sm text-muted-foreground">
                            Please wait while we process {employees.length} document{employees.length !== 1 ? 's' : ''}
                          </p>
                        </div>
                      </div>
                    </div>
                  ) : currentDocument ? (
                    <div className="p-6">
                      {/* Document Metadata */}
                      <div className="mb-6 p-4 dialog-content-solid rounded-lg">
                        <div className="grid grid-cols-2 gap-4 text-sm">
                          <div>
                            <Label className="text-xs text-muted-foreground">Employee</Label>
                            <p className="font-medium">{currentEmployee?.name}</p>
                          </div>
                          <div>
                            <Label className="text-xs text-muted-foreground">Employee ID</Label>
                            <p className="font-medium">{currentEmployee?.employeeId}</p>
                          </div>
                          <div>
                            <Label className="text-xs text-muted-foreground">Department</Label>
                            <p className="font-medium">{currentEmployee?.department}</p>
                          </div>
                          <div>
                            <Label className="text-xs text-muted-foreground">Designation</Label>
                            <p className="font-medium">{currentEmployee?.designation}</p>
                          </div>
                        </div>
                      </div>

                      {/* Document Content */}
                      {editing ? (
                        <div className="space-y-4">
                          <div className="flex items-center justify-between">
                            <Label className="text-sm font-medium">Edit Document Content</Label>
                            <Badge variant="outline" className="text-xs">
                              Edit Mode
                            </Badge>
                          </div>
                          <Textarea
                            value={editedContent[currentPreviewIndex.toString()] || currentDocument.content}
                            onChange={(e) => handleEditContent(currentPreviewIndex, e.target.value)}
                            className="min-h-[600px] font-mono text-sm"
                            placeholder="Enter document content..."
                          />
                        </div>
                      ) : (
                        <div 
                          className="bg-white text-black p-8 rounded-lg shadow-lg min-h-[600px]"
                          dangerouslySetInnerHTML={{ __html: currentDocument.content }}
                        />
                      )}
                    </div>
                  ) : (
                    <div className="flex items-center justify-center h-full">
                      <div className="text-center space-y-4">
                        <Eye className="h-12 w-12 text-muted-foreground mx-auto" />
                        <div>
                          <p className="font-medium">No Preview Available</p>
                          <p className="text-sm text-muted-foreground">
                            Select a template and employees to preview documents
                          </p>
                        </div>
                      </div>
                    </div>
                  )}
                </ScrollArea>
              </CardContent>
            </Card>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}