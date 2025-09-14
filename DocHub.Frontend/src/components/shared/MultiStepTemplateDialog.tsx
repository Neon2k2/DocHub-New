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

interface MultiStepTemplateDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  letterType: string;
  onTemplateSelect: (template: DocumentTemplate) => void;
  onComplete: (template: DocumentTemplate, signature: Signature | null) => void;
  initialTemplate?: DocumentTemplate | null;
  initialSignature?: Signature | null;
}

export function MultiStepTemplateDialog({
  open,
  onOpenChange,
  letterType,
  onTemplateSelect,
  onComplete,
  initialTemplate = null,
  initialSignature = null
}: MultiStepTemplateDialogProps) {
  const [templates, setTemplates] = useState<DocumentTemplate[]>([]);
  const [signatures, setSignatures] = useState<Signature[]>([]);
  const [loading, setLoading] = useState(false);
  const [uploadFile, setUploadFile] = useState<File | null>(null);
  const [uploadName, setUploadName] = useState('');
  const [uploading, setUploading] = useState(false);
  const [signatureUploadFile, setSignatureUploadFile] = useState<File | null>(null);
  const [signatureUploadName, setSignatureUploadName] = useState('');
  const [signatureUploading, setSignatureUploading] = useState(false);
  const [activeTab, setActiveTab] = useState('existing');
  const [signatureTab, setSignatureTab] = useState('existing');
  const fileInputRef = React.useRef<HTMLInputElement>(null);
  const signatureFileInputRef = React.useRef<HTMLInputElement>(null);
  
  // Multi-step state
  const [currentStep, setCurrentStep] = useState(1); // 1: Template, 2: Signature, 3: Preview
  const [selectedTemplate, setSelectedTemplate] = useState<DocumentTemplate | null>(initialTemplate);
  const [selectedSignature, setSelectedSignature] = useState<Signature | null>(initialSignature);

  useEffect(() => {
    if (open) {
      loadTemplates();
      loadSignatures();
      
      // Determine starting step based on what's already selected
      if (initialTemplate && initialSignature) {
        console.log('🔍 [MultiStepTemplateDialog] Both template and signature provided, starting at step 3');
        setCurrentStep(3);
        setSelectedTemplate(initialTemplate);
        setSelectedSignature(initialSignature);
      } else if (initialTemplate) {
        console.log('🔍 [MultiStepTemplateDialog] Template provided, starting at step 2');
        setCurrentStep(2);
        setSelectedTemplate(initialTemplate);
        setSelectedSignature(initialSignature);
      } else {
        console.log('🔍 [MultiStepTemplateDialog] No initial values, starting at step 1');
        setCurrentStep(1);
        setSelectedTemplate(null);
        setSelectedSignature(null);
      }
    }
  }, [open, letterType, initialTemplate, initialSignature]);

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

  const handleSignatureFileUpload = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (file) {
      setSignatureUploadFile(file);
      setSignatureUploadName(file.name.replace(/\.[^/.]+$/, '')); // Remove extension
    }
  };

  const handleSignatureUploadAreaClick = () => {
    signatureFileInputRef.current?.click();
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

  const handleUploadSignature = async () => {
    console.log('🔍 [MultiStepTemplateDialog] Starting signature upload');
    console.log('🔍 [MultiStepTemplateDialog] Upload file:', signatureUploadFile);
    console.log('🔍 [MultiStepTemplateDialog] Upload name:', signatureUploadName);
    
    if (!signatureUploadFile || !signatureUploadName.trim()) {
      console.warn('⚠️ [MultiStepTemplateDialog] Missing file or name for signature upload');
      return;
    }

    setSignatureUploading(true);
    try {
      console.log('🔍 [MultiStepTemplateDialog] Calling documentService.uploadSignature');
      const newSignature = await documentService.uploadSignature(
        signatureUploadFile,
        signatureUploadName.trim()
      );
      console.log('✅ [MultiStepTemplateDialog] Signature uploaded successfully:', newSignature);
      
      setSignatures([newSignature, ...signatures]);
      setSignatureUploadFile(null);
      setSignatureUploadName('');
      setSignatureTab('existing');
      
      // Automatically select the newly uploaded signature
      setTimeout(() => {
        console.log('🔍 [MultiStepTemplateDialog] Auto-selecting uploaded signature');
        handleSignatureSelect(newSignature);
      }, 500); // Small delay to ensure UI updates
    } catch (error) {
      console.error('❌ [MultiStepTemplateDialog] Failed to upload signature:', error);
    } finally {
      setSignatureUploading(false);
    }
  };

  const handlePreviewTemplate = (template: DocumentTemplate) => {
    if (template.fileUrl) {
      window.open(template.fileUrl, '_blank');
    }
  };

  const handleDownloadTemplate = async (template: DocumentTemplate) => {
    try {
      if (template.fileUrl) {
        const link = document.createElement('a');
        link.href = template.fileUrl;
        link.download = template.fileName || `${template.name}.docx`;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
      }
    } catch (error) {
      console.error('Failed to download template:', error);
    }
  };

  const handleTemplateSelect = (template: DocumentTemplate) => {
    console.log('🔍 [MultiStepTemplateDialog] Template selected:', template);
    setSelectedTemplate(template);
    onTemplateSelect(template);
    setCurrentStep(2); // Move to signature selection
  };

  const handleSignatureSelect = (signature: Signature) => {
    console.log('🔍 [MultiStepTemplateDialog] Signature selected:', signature);
    setSelectedSignature(signature);
    setCurrentStep(3); // Move to preview/complete
  };

  const handleComplete = () => {
    console.log('🔍 [MultiStepTemplateDialog] Complete button clicked');
    console.log('🔍 [MultiStepTemplateDialog] Selected template:', selectedTemplate);
    console.log('🔍 [MultiStepTemplateDialog] Selected signature:', selectedSignature);
    
    if (selectedTemplate) {
      onComplete(selectedTemplate, selectedSignature);
      onOpenChange(false);
    } else {
      console.error('❌ [MultiStepTemplateDialog] No template selected');
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
      'confirmation_letter': 'Confirmation Letter',
      'experience_letter': 'Experience Letter'
    };
    return labels[type as keyof typeof labels] || type.replace(/_/g, ' ').replace(/\b\w/g, l => l.toUpperCase());
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

  const renderStepContent = () => {
    switch (currentStep) {
      case 1: // Template Selection
        return (
          <div className="flex flex-col h-full">
            <Tabs value={activeTab} onValueChange={setActiveTab} className="w-full flex flex-col h-full">
              <TabsList className="grid w-full grid-cols-2 flex-shrink-0">
                <TabsTrigger value="existing">Existing Templates</TabsTrigger>
                <TabsTrigger value="upload">Upload New</TabsTrigger>
              </TabsList>

              <TabsContent value="existing" className="flex-1 min-h-0 mt-4">
                {loading ? (
                  <div className="flex items-center justify-center py-8">
                    <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
                  </div>
                ) : templates.length === 0 ? (
                  <div className="text-center py-8 text-muted-foreground">
                    <FileText className="h-12 w-12 mx-auto mb-4 opacity-50" />
                    <p>No templates found for {getLetterTypeLabel(letterType).toLowerCase()}.</p>
                    <p className="text-sm">Upload a new template to get started.</p>
                  </div>
                ) : (
                  <ScrollArea className="h-full pr-4">
                    <div className="grid gap-4">
                      {templates.map((template) => (
                        <Card
                          key={template.id}
                          className={`dialog-content-solid hover:bg-muted/20 transition-colors ${
                            selectedTemplate?.id === template.id ? 'ring-2 ring-primary' : ''
                          }`}
                        >
                          <CardContent className="p-6">
                            <div className="flex items-start justify-between">
                              <div className="flex-1 cursor-pointer" onClick={() => handleTemplateSelect(template)}>
                                <div className="flex items-center gap-3 mb-2">
                                  <FileText className="h-5 w-5 text-primary" />
                                  <CardTitle className="text-lg">{template.name}</CardTitle>
                                  <Badge variant="secondary" className="text-xs">
                                    {template.type}
                                  </Badge>
                                  {selectedTemplate?.id === template.id && (
                                    <Check className="h-4 w-4 text-primary" />
                                  )}
                                </div>
                                
                                <CardDescription className="mb-4">
                                  {template.description || 'No description available'}
                                </CardDescription>

                                <div className="flex items-center gap-4 text-sm text-muted-foreground mb-4">
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

                              {/* Action Buttons */}
                              <div className="flex flex-col gap-2 ml-4">
                                <Button
                                  size="sm"
                                  variant="outline"
                                  className="flex items-center gap-1"
                                  onClick={(e) => {
                                    e.stopPropagation();
                                    handlePreviewTemplate(template);
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
                                    e.stopPropagation();
                                    handleDownloadTemplate(template);
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
                  </ScrollArea>
                )}
              </TabsContent>

              <TabsContent value="upload" className="flex-1 min-h-0 mt-4">
                <ScrollArea className="h-full pr-4">
                  <div className="space-y-4">
                    <div className="text-center py-8">
                      <Upload className="h-12 w-12 mx-auto mb-4 text-muted-foreground" />
                      <h3 className="text-lg font-semibold mb-2">Upload New Template</h3>
                      <p className="text-muted-foreground mb-4">
                        Upload a Word document (.docx) template for {getLetterTypeLabel(letterType).toLowerCase()}s.
                      </p>
                    </div>

                    <div className="space-y-4">
                      <div>
                        <Label htmlFor="template-file">Template File</Label>
                        <div
                          className="border-2 border-dashed border-muted-foreground/25 rounded-lg p-8 text-center cursor-pointer hover:border-muted-foreground/50 transition-colors"
                          onClick={handleUploadAreaClick}
                        >
                          <input
                            ref={fileInputRef}
                            type="file"
                            accept=".docx"
                            onChange={handleFileUpload}
                            className="hidden"
                          />
                          {uploadFile ? (
                            <div className="space-y-2">
                              <FileText className="h-8 w-8 mx-auto text-primary" />
                              <p className="font-medium">{uploadFile.name}</p>
                              <p className="text-sm text-muted-foreground">
                                {(uploadFile.size / 1024).toFixed(1)} KB
                              </p>
                            </div>
                          ) : (
                            <div className="space-y-2">
                              <Upload className="h-8 w-8 mx-auto text-muted-foreground" />
                              <p className="font-medium">Click to select a .docx file</p>
                              <p className="text-sm text-muted-foreground">
                                Only Word documents (.docx) are supported
                              </p>
                            </div>
                          )}
                        </div>
                      </div>

                      <div>
                        <Label htmlFor="template-name">Template Name</Label>
                        <Input
                          id="template-name"
                          placeholder="Enter a name for this template"
                          value={uploadName}
                          onChange={(e) => setUploadName(e.target.value)}
                        />
                      </div>

                      <Button
                        onClick={handleUploadTemplate}
                        disabled={!uploadFile || !uploadName.trim() || uploading}
                        className="w-full"
                      >
                        {uploading ? (
                          <>
                            <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2"></div>
                            Uploading...
                          </>
                        ) : (
                          <>
                            <Upload className="h-4 w-4 mr-2" />
                            Upload Template
                          </>
                        )}
                      </Button>
                    </div>
                  </div>
                </ScrollArea>
              </TabsContent>
            </Tabs>
          </div>
        );

      case 2: // Signature Selection
        return (
          <div className="flex flex-col h-full">
            <div className="text-center py-4 flex-shrink-0">
              <h3 className="text-lg font-semibold mb-2">Select Signature (Optional)</h3>
              <p className="text-muted-foreground">
                Choose a signature to include in the generated letters, or skip this step.
              </p>
            </div>

            <Tabs value={signatureTab} onValueChange={setSignatureTab} className="flex-1 min-h-0 flex flex-col">
              <TabsList className="grid w-full grid-cols-2 flex-shrink-0">
                <TabsTrigger value="existing">Existing Signatures</TabsTrigger>
                <TabsTrigger value="upload">Upload New</TabsTrigger>
              </TabsList>

              <TabsContent value="existing" className="flex-1 min-h-0 mt-4">
                {signatures.length === 0 ? (
                  <div className="text-center py-8 text-muted-foreground">
                    <User className="h-12 w-12 mx-auto mb-4 opacity-50" />
                    <p>No signatures available.</p>
                    <p className="text-sm">You can skip this step and add signatures later.</p>
                  </div>
                ) : (
                  <ScrollArea className="h-full pr-4">
                    <div className="grid gap-4">
                      <Card
                        className={`dialog-content-solid cursor-pointer hover:bg-muted/20 transition-colors ${
                          selectedSignature === null ? 'ring-2 ring-primary' : ''
                        }`}
                        onClick={() => setSelectedSignature(null)}
                      >
                        <CardContent className="p-6">
                          <div className="flex items-center gap-3">
                            <X className="h-5 w-5 text-muted-foreground" />
                            <div>
                              <CardTitle className="text-lg">No Signature</CardTitle>
                              <CardDescription>Generate letters without signature</CardDescription>
                            </div>
                            {selectedSignature === null && (
                              <Check className="h-4 w-4 text-primary ml-auto" />
                            )}
                          </div>
                        </CardContent>
                      </Card>

                      {signatures.map((signature) => (
                        <Card
                          key={signature.id}
                          className={`dialog-content-solid cursor-pointer hover:bg-muted/20 transition-colors ${
                            selectedSignature?.id === signature.id ? 'ring-2 ring-primary' : ''
                          }`}
                          onClick={() => handleSignatureSelect(signature)}
                        >
                          <CardContent className="p-6">
                            <div className="flex items-center gap-3">
                              <User className="h-5 w-5 text-primary" />
                              <div className="flex-1">
                                <CardTitle className="text-lg">{signature.name}</CardTitle>
                                <CardDescription>
                                  {signature.description || 'Digital signature'}
                                </CardDescription>
                              </div>
                              {selectedSignature?.id === signature.id && (
                                <Check className="h-4 w-4 text-primary" />
                              )}
                            </div>
                          </CardContent>
                        </Card>
                      ))}
                    </div>
                  </ScrollArea>
                )}
              </TabsContent>

              <TabsContent value="upload" className="flex-1 min-h-0 mt-4">
                <ScrollArea className="h-full pr-4">
                  <div className="space-y-4">
                      <div>
                        <Label htmlFor="signature-file" className="text-sm font-medium">Signature File</Label>
                        <div
                          className="border-2 border-dashed border-primary/30 rounded-lg p-8 text-center cursor-pointer hover:border-primary/50 transition-colors bg-muted/5"
                          onClick={handleSignatureUploadAreaClick}
                        >
                          <input
                            ref={signatureFileInputRef}
                            type="file"
                            accept=".png,.jpg,.jpeg"
                            onChange={handleSignatureFileUpload}
                            className="hidden"
                          />
                          {signatureUploadFile ? (
                            <div className="space-y-2">
                              <User className="h-8 w-8 mx-auto text-primary" />
                              <p className="font-medium">{signatureUploadFile.name}</p>
                              <p className="text-sm text-muted-foreground">
                                {(signatureUploadFile.size / 1024).toFixed(1)} KB
                              </p>
                            </div>
                          ) : (
                            <div className="space-y-2">
                              <Upload className="h-10 w-10 mx-auto text-primary" />
                              <p className="font-medium text-primary">Click to select an image file</p>
                              <p className="text-sm text-muted-foreground">
                                PNG, JPG, and JPEG files are supported
                              </p>
                            </div>
                          )}
                        </div>
                      </div>

                      <div>
                        <Label htmlFor="signature-name">Signature Name</Label>
                        <Input
                          id="signature-name"
                          placeholder="Enter a name for this signature"
                          value={signatureUploadName}
                          onChange={(e) => setSignatureUploadName(e.target.value)}
                        />
                      </div>

                      <Button
                        onClick={handleUploadSignature}
                        disabled={!signatureUploadFile || !signatureUploadName.trim() || signatureUploading}
                        className="w-full"
                      >
                        {signatureUploading ? (
                          <>
                            <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2"></div>
                            Uploading...
                          </>
                        ) : (
                          <>
                            <Upload className="h-4 w-4 mr-2" />
                            Upload Signature
                          </>
                        )}
                      </Button>
                  </div>
                </ScrollArea>
              </TabsContent>
            </Tabs>
          </div>
        );

      case 3: // Preview/Complete
        return (
          <div className="flex flex-col h-full">
            <div className="text-center py-4 flex-shrink-0">
              <h3 className="text-lg font-semibold mb-2">Ready to Generate</h3>
              <p className="text-muted-foreground">
                Review your selections and generate the letters.
              </p>
            </div>

            <ScrollArea className="flex-1 pr-4">
              <div className="space-y-4">
                <Card>
                  <CardContent className="p-6">
                    <div className="space-y-4">
                      <div>
                        <Label className="text-sm font-medium text-muted-foreground">Selected Template</Label>
                        <div className="flex items-center gap-3 mt-1">
                          <FileText className="h-4 w-4 text-primary" />
                          <span className="font-medium">{selectedTemplate?.name}</span>
                        </div>
                      </div>

                      <div>
                        <Label className="text-sm font-medium text-muted-foreground">Selected Signature</Label>
                        <div className="flex items-center gap-3 mt-1">
                          {selectedSignature ? (
                            <>
                              <User className="h-4 w-4 text-primary" />
                              <span className="font-medium">{selectedSignature.name}</span>
                            </>
                          ) : (
                            <>
                              <X className="h-4 w-4 text-muted-foreground" />
                              <span className="text-muted-foreground">No signature</span>
                            </>
                          )}
                        </div>
                      </div>
                    </div>
                  </CardContent>
                </Card>
              </div>
            </ScrollArea>
          </div>
        );

      default:
        return null;
    }
  };

  const getStepTitle = () => {
    switch (currentStep) {
      case 1: return `Select ${getLetterTypeLabel(letterType)} Template`;
      case 2: return 'Select Signature (Optional)';
      case 3: return 'Ready to Generate';
      default: return 'Template Selection';
    }
  };

  const getStepDescription = () => {
    switch (currentStep) {
      case 1: return `Choose a template for generating ${getLetterTypeLabel(letterType).toLowerCase()}s or upload a new one.`;
      case 2: return 'Choose a signature to include in the generated letters, or skip this step.';
      case 3: return 'Review your selections and generate the letters.';
      default: return '';
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-6xl h-[90vh] dialog-panel flex flex-col">
        <DialogHeader className="flex-shrink-0">
          <DialogTitle className="flex items-center gap-2">
            <FileText className="h-5 w-5" />
            {getStepTitle()}
          </DialogTitle>
          <DialogDescription>
            {getStepDescription()}
          </DialogDescription>
        </DialogHeader>

        <div className="flex-1 min-h-0 overflow-hidden flex flex-col">
          {renderStepContent()}
        </div>

        {/* Step Navigation */}
        <div className="flex-shrink-0 flex items-center justify-between pt-4 border-t">
          <Button
            variant="outline"
            onClick={handleBack}
            disabled={currentStep === 1}
            className="flex items-center gap-2"
          >
            <ArrowLeft className="h-4 w-4" />
            Back
          </Button>

          <div className="flex items-center gap-2">
            {[1, 2, 3].map((step) => (
              <div
                key={step}
                className={`w-2 h-2 rounded-full ${
                  step <= currentStep ? 'bg-primary' : 'bg-muted'
                }`}
              />
            ))}
          </div>

          {currentStep < 3 ? (
            <Button
              onClick={handleNext}
              disabled={currentStep === 1 && !selectedTemplate}
              className="flex items-center gap-2"
            >
              Next
              <ArrowRight className="h-4 w-4" />
            </Button>
          ) : (
            <Button
              onClick={handleComplete}
              disabled={!selectedTemplate}
              className="flex items-center gap-2"
            >
              <Check className="h-4 w-4" />
              Generate Letters
            </Button>
          )}
        </div>
      </DialogContent>
    </Dialog>
  );
}