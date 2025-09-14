import React, { useState, useEffect } from 'react';
import { FileText, Upload, Eye, Download, Calendar, User, X, ArrowLeft, ArrowRight, Check } from 'lucide-react';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from '../ui/dialog';
import { Button } from '../ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../ui/card';
import { Badge } from '../ui/badge';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../ui/tabs';
import { Input } from '../ui/input';
import { Label } from '../ui/label';
import { ScrollArea } from '../ui/scroll-area';
import { Separator } from '../ui/separator';
import { documentService, DocumentTemplate, Signature } from '../../services/document.service';

interface TemplateSelectionDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  letterType: 'transfer_letter' | 'mutual_cessation' | 'confirmation_letter';
  onTemplateSelect: (template: DocumentTemplate) => void;
  onComplete: (template: DocumentTemplate, signature: Signature | null) => void;
}

export function TemplateSelectionDialog({
  open,
  onOpenChange,
  letterType,
  onTemplateSelect,
  onComplete
}: TemplateSelectionDialogProps) {
  const [templates, setTemplates] = useState<DocumentTemplate[]>([]);
  const [signatures, setSignatures] = useState<Signature[]>([]);
  const [loading, setLoading] = useState(false);
  const [uploadFile, setUploadFile] = useState<File | null>(null);
  const [uploadName, setUploadName] = useState('');
  const [uploading, setUploading] = useState(false);
  const [activeTab, setActiveTab] = useState('existing');
  const fileInputRef = React.useRef<HTMLInputElement>(null);
  
  // Multi-step state
  const [currentStep, setCurrentStep] = useState(1); // 1: Template, 2: Signature, 3: Preview
  const [selectedTemplate, setSelectedTemplate] = useState<DocumentTemplate | null>(null);
  const [selectedSignature, setSelectedSignature] = useState<Signature | null>(null);

  useEffect(() => {
    if (open) {
      loadTemplates();
      loadSignatures();
      // Reset step when dialog opens
      setCurrentStep(1);
      setSelectedTemplate(null);
      setSelectedSignature(null);
    }
  }, [open, letterType]);

  const loadTemplates = async () => {
    setLoading(true);
    try {
      const data = await documentService.getTemplates(letterType);
      setTemplates(data);
    } catch (error) {
      console.error('Failed to load templates:', error);
    } finally {
      setLoading(false);
    }
  };

  const loadSignatures = async () => {
    try {
      const data = await documentService.getSignatures();
      setSignatures(data);
    } catch (error) {
      console.error('Failed to load signatures:', error);
    }
  };

  const handleFileUpload = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (file) {
      setUploadFile(file);
      setUploadName(file.name.replace(/\.[^/.]+$/, '')); // Remove extension
    }
  };

  const handleUploadAreaClick = () => {
    fileInputRef.current?.click();
  };

  const handleUploadTemplate = async () => {
    if (!uploadFile || !uploadName.trim()) return;

    setUploading(true);
    try {
      const newTemplate = await documentService.uploadTemplate(
        uploadFile,
        letterType,
        uploadName.trim()
      );
      setTemplates([newTemplate, ...templates]);
      setUploadFile(null);
      setUploadName('');
      setActiveTab('existing');
      
      // Automatically select and use the newly uploaded template
      setTimeout(() => {
        handleTemplateSelect(newTemplate);
      }, 500); // Small delay to ensure UI updates
    } catch (error) {
      console.error('Failed to upload template:', error);
    } finally {
      setUploading(false);
    }
  };

  const handleTemplateSelect = (template: DocumentTemplate) => {
    setSelectedTemplate(template);
    onTemplateSelect(template);
    setCurrentStep(2); // Move to signature selection
  };

  const handleSignatureSelect = (signature: Signature) => {
    setSelectedSignature(signature);
    setCurrentStep(3); // Move to preview/complete
  };

  const handleComplete = () => {
    if (selectedTemplate) {
      onComplete(selectedTemplate, selectedSignature);
      onOpenChange(false);
    }
  };

  const handleBack = () => {
    if (currentStep > 1) {
      setCurrentStep(currentStep - 1);
    }
  };

  const handleNext = () => {
    if (currentStep < 3) {
      setCurrentStep(currentStep + 1);
    }
  };

  const getLetterTypeLabel = (type: string) => {
    const labels = {
      'transfer_letter': 'Transfer Letter',
      'mutual_cessation': 'Mutual Cessation Letter',
      'confirmation_letter': 'Confirmation Letter'
    };
    return labels[type as keyof typeof labels] || type;
  };

  const formatDate = (date: Date | string | undefined) => {
    if (!date) return 'Unknown date';
    
    try {
      const dateObj = typeof date === 'string' ? new Date(date) : date;
      if (isNaN(dateObj.getTime())) {
        return 'Invalid date';
      }
      
      return new Intl.DateTimeFormat('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric'
      }).format(dateObj);
    } catch (error) {
      console.warn('Error formatting date:', error);
      return 'Invalid date';
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="dialog-panel max-w-4xl h-[80vh] flex flex-col">
        <DialogHeader className="flex-shrink-0">
          <DialogTitle className="flex items-center gap-2">
            <FileText className="h-5 w-5 text-neon-blue" />
            Select {getLetterTypeLabel(letterType)} Template
          </DialogTitle>
          <DialogDescription>
            Choose an existing template or upload a new one to generate letters
          </DialogDescription>
        </DialogHeader>

        <Tabs value={activeTab} onValueChange={setActiveTab} className="flex-1 flex flex-col min-h-0">
          <TabsList className="grid w-full grid-cols-2">
            <TabsTrigger value="existing">Existing Templates</TabsTrigger>
            <TabsTrigger value="upload">Upload New</TabsTrigger>
          </TabsList>

          <TabsContent value="existing" className="flex-1 mt-4 min-h-0">
            <ScrollArea className="h-full">
              {loading ? (
                <div className="space-y-4">
                  {Array.from({ length: 3 }).map((_, i) => (
                    <Card key={i} className="dialog-content-solid animate-pulse">
                      <CardContent className="p-6">
                        <div className="space-y-3">
                          <div className="h-4 bg-muted rounded w-1/3" />
                          <div className="h-3 bg-muted rounded w-1/2" />
                          <div className="h-3 bg-muted rounded w-1/4" />
                        </div>
                      </CardContent>
                    </Card>
                  ))}
                </div>
              ) : templates.length === 0 ? (
                <div className="text-center py-12">
                  <FileText className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
                  <h3 className="text-lg font-semibold mb-2">No Templates Found</h3>
                  <p className="text-muted-foreground mb-4">
                    No templates available for {getLetterTypeLabel(letterType).toLowerCase()}
                  </p>
                  <Button onClick={() => setActiveTab('upload')}>
                    <Upload className="h-4 w-4 mr-2" />
                    Upload Template
                  </Button>
                </div>
              ) : (
                <div className="space-y-4">
                  {templates.map((template) => (
                    <Card
                      key={template.id}
                      className="dialog-content-solid cursor-pointer hover:bg-muted/20 transition-colors"
                      onClick={() => handleTemplateSelect(template)}
                    >
                      <CardContent className="p-6">
                        <div className="flex items-start justify-between">
                          <div className="flex-1">
                            <div className="flex items-center gap-3 mb-2">
                              <h3 className="font-semibold text-lg">{template.name}</h3>
                              <Badge variant="outline">v{template.version}</Badge>
                              {template.isActive && (
                                <Badge className="bg-green-500/20 text-green-400 border-green-500/30">
                                  Active
                                </Badge>
                              )}
                            </div>
                            
                            <p className="text-sm text-muted-foreground mb-3">
                              {template.fileName}
                            </p>

                            <div className="flex items-center gap-6 text-sm text-muted-foreground mb-4">
                              <div className="flex items-center gap-1">
                                <User className="h-3 w-3" />
                                <span>{template.createdBy}</span>
                              </div>
                              <div className="flex items-center gap-1">
                                <Calendar className="h-3 w-3" />
                                <span>{formatDate(template.createdAt)}</span>
                              </div>
                            </div>

                            {/* Placeholders */}
                            <div className="space-y-2">
                              <Label className="text-xs font-medium text-muted-foreground">
                                Available Placeholders:
                              </Label>
                              <div className="flex flex-wrap gap-1">
                                {(template.placeholders || []).slice(0, 8).map((placeholder) => (
                                  <Badge
                                    key={placeholder}
                                    variant="outline"
                                    className="text-xs font-mono"
                                  >
                                    {'{' + placeholder + '}'}
                                  </Badge>
                                ))}
                                {(template.placeholders || []).length > 8 && (
                                  <Badge variant="outline" className="text-xs">
                                    +{(template.placeholders || []).length - 8} more
                                  </Badge>
                                )}
                              </div>
                            </div>
                          </div>

                          <div className="flex flex-col gap-2 ml-4">
                            <Button
                              size="sm"
                              variant="outline"
                              className="flex items-center gap-1"
                              onClick={(e) => {
                                e.stopPropagation(); // Prevent card click
                                // TODO: Implement preview functionality
                              }}
                            >
                              <Eye className="h-3 w-3" />
                              Preview
                            </Button>
                            <Button
                              size="sm"
                              variant="outline"
                              className="flex items-center gap-1"
                              onClick={(e) => {
                                e.stopPropagation(); // Prevent card click
                                // TODO: Implement download functionality
                              }}
                            >
                              <Download className="h-3 w-3" />
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
            <Card className="dialog-content-solid">
              <CardHeader>
                <CardTitle>Upload New Template</CardTitle>
                <CardDescription>
                  Upload a Word document (.docx) with content controls for placeholders
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-6">
                <div className="space-y-2">
                  <Label htmlFor="template-name">Template Name</Label>
                  <Input
                    id="template-name"
                    placeholder="Enter template name"
                    value={uploadName}
                    onChange={(e) => setUploadName(e.target.value)}
                  />
                </div>

                <div className="space-y-2">
                  <Label htmlFor="template-file">Template File</Label>
                  <div 
                    className="relative border-2 border-dashed border-glass-border rounded-lg p-8 text-center cursor-pointer hover:border-blue-400 transition-colors"
                    onClick={handleUploadAreaClick}
                  >
                    {uploadFile ? (
                      <div className="space-y-4">
                        <div className="flex items-center justify-center gap-2">
                          <FileText className="h-8 w-8 text-neon-blue" />
                          <div>
                            <p className="font-medium">{uploadFile.name}</p>
                            <p className="text-sm text-muted-foreground">
                              {(uploadFile.size / 1024 / 1024).toFixed(2)} MB
                            </p>
                          </div>
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={(e) => {
                              e.stopPropagation();
                              setUploadFile(null);
                            }}
                          >
                            <X className="h-4 w-4" />
                          </Button>
                        </div>
                      </div>
                    ) : (
                      <div className="space-y-4">
                        <Upload className="h-12 w-12 text-muted-foreground mx-auto" />
                        <div>
                          <p className="font-medium">Click to upload or drag and drop</p>
                          <p className="text-sm text-muted-foreground">
                            Word documents (.docx) only
                          </p>
                        </div>
                        <input
                          ref={fileInputRef}
                          type="file"
                          accept=".docx"
                          onChange={handleFileUpload}
                          className="hidden"
                        />
                      </div>
                    )}
                  </div>
                </div>

                <div className="dialog-content-solid rounded-lg p-4">
                  <h4 className="font-medium mb-2">Template Requirements:</h4>
                  <ul className="text-sm text-muted-foreground space-y-1">
                    <li>• Use Word Content Controls for placeholders</li>
                    <li>• Supported placeholders: EMPLOYEE_NAME, EMPLOYEE_ID, DESIGNATION, etc.</li>
                    <li>• File should be in .docx format</li>
                    <li>• Maximum file size: 10MB</li>
                  </ul>
                </div>

                <Button
                  onClick={handleUploadTemplate}
                  disabled={!uploadFile || !uploadName.trim() || uploading}
                  className="w-full neon-border bg-card text-neon-blue hover:bg-neon-blue hover:text-white"
                >
                  {uploading ? (
                    <>Processing...</>
                  ) : (
                    <>
                      <Upload className="h-4 w-4 mr-2" />
                      Upload Template
                    </>
                  )}
                </Button>
              </CardContent>
            </Card>
          </TabsContent>
        </Tabs>
      </DialogContent>
    </Dialog>
  );
}