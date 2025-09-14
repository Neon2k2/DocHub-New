import React, { useState, useEffect } from 'react';
import { FileText, Download, User, X, Eye, Loader2 } from 'lucide-react';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from '../ui/dialog';
import { Button } from '../ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../ui/card';
import { Badge } from '../ui/badge';
import { ScrollArea } from '../ui/scroll-area';
import { Separator } from '../ui/separator';
import { DocumentTemplate, Signature } from '../../services/document.service';
import { Employee, apiService } from '../../services/api.service';

interface SimplePreviewDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  template: DocumentTemplate;
  signature: Signature | null;
  employees: Employee[];
  tabId: string;
  onGenerate: () => void;
  onDownload: () => void;
}

export function SimplePreviewDialog({
  open,
  onOpenChange,
  template,
  signature,
  employees,
  tabId,
  onGenerate,
  onDownload
}: SimplePreviewDialogProps) {
  console.log('🔍 [SimplePreviewDialog] Dialog opened');
  console.log('🔍 [SimplePreviewDialog] Template:', template);
  console.log('🔍 [SimplePreviewDialog] Signature:', signature);
  console.log('🔍 [SimplePreviewDialog] Employees:', employees);
  console.log('🔍 [SimplePreviewDialog] TabId:', tabId);

  const [selectedEmployee, setSelectedEmployee] = useState<Employee | null>(employees[0] || null);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [generatingPreview, setGeneratingPreview] = useState(false);

  useEffect(() => {
    if (employees.length > 0 && !selectedEmployee) {
      setSelectedEmployee(employees[0]);
    }
  }, [employees, selectedEmployee]);

  // Generate PDF preview when employee changes
  useEffect(() => {
    if (selectedEmployee && template && tabId) {
      generatePreview();
    }
  }, [selectedEmployee, template, signature, tabId]);

  const generatePreview = async () => {
    if (!selectedEmployee || !template || !tabId) return;

    console.log('🔍 [SimplePreviewDialog] Generating PDF preview for:', selectedEmployee.name);
    console.log('🔍 [SimplePreviewDialog] Selected employee details:', {
      id: selectedEmployee.id,
      employeeId: selectedEmployee.employeeId,
      name: selectedEmployee.name,
      client: selectedEmployee.client,
      designation: selectedEmployee.designation
    });
    setGeneratingPreview(true);
    
    try {
      const requestData = {
        employeeId: selectedEmployee.employeeId, // Use the actual employee ID from Excel data
        templateId: template.id,
        signaturePath: signature?.fileId || null,
        employeeData: selectedEmployee.data || {} // Include the employee data from Excel
      };

      console.log('🔍 [SimplePreviewDialog] Preview request data:', requestData);
      console.log('🔍 [SimplePreviewDialog] Selected employee details:', selectedEmployee);
      console.log('🔍 [SimplePreviewDialog] Employee data keys:', Object.keys(selectedEmployee.data || {}));
      console.log('🔍 [SimplePreviewDialog] Employee data values:', selectedEmployee.data);
      console.log('🔍 [SimplePreviewDialog] Template details:', template);
      console.log('🔍 [SimplePreviewDialog] Signature details:', signature);
      console.log('🔍 [SimplePreviewDialog] Making request to:', `/Tab/${tabId}/generate-preview`);

      // Call the generate-preview endpoint
      const response = await apiService.requestBinary(`/Tab/${tabId}/generate-preview`, {
        method: 'POST',
        body: JSON.stringify(requestData),
        headers: {
          'Content-Type': 'application/json'
        }
      });

      console.log('✅ [SimplePreviewDialog] Preview response received:', response);

      // Create blob URL for the PDF
      const blob = new Blob([response], { type: 'application/pdf' });
      const url = URL.createObjectURL(blob);
      
      // Clean up previous URL
      if (previewUrl) {
        URL.revokeObjectURL(previewUrl);
      }
      
      setPreviewUrl(url);
      console.log('✅ [SimplePreviewDialog] PDF preview URL created:', url);
    } catch (error) {
      console.error('❌ [SimplePreviewDialog] Error generating preview:', error);
      
      // Try to get more details about the error
      if (error instanceof Error) {
        console.error('❌ [SimplePreviewDialog] Error message:', error.message);
        console.error('❌ [SimplePreviewDialog] Error stack:', error.stack);
      }
      
      // Show user-friendly error message
      console.error('❌ [SimplePreviewDialog] Failed to generate PDF preview. Check console for details.');
    } finally {
      setGeneratingPreview(false);
    }
  };

  const handleEmployeeSelect = (employee: Employee) => {
    console.log('🔍 [SimplePreviewDialog] Employee selected:', employee);
    setSelectedEmployee(employee);
  };

  // Clean up blob URL on unmount
  useEffect(() => {
    return () => {
      if (previewUrl) {
        URL.revokeObjectURL(previewUrl);
      }
    };
  }, [previewUrl]);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-7xl h-[90vh] dialog-panel flex flex-col">
        <DialogHeader className="flex-shrink-0">
          <DialogTitle className="flex items-center gap-2">
            <FileText className="h-5 w-5" />
            Letter Preview - {template.name}
          </DialogTitle>
          <DialogDescription>
            Preview and generate letters for selected employees
          </DialogDescription>
        </DialogHeader>

        <div className="flex flex-1 gap-6 min-h-0 overflow-hidden">
          {/* Left Sidebar */}
          <div className="w-80 space-y-4 overflow-y-auto min-h-0 pr-2">
            {/* Template Info */}
            <Card className="dialog-content-solid">
              <CardHeader>
                <CardTitle className="text-sm flex items-center gap-2">
                  <FileText className="h-4 w-4" />
                  Selected Template
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-2">
                <div className="text-sm font-medium">{template.name}</div>
                <div className="text-xs text-muted-foreground">{template.type}</div>
                <Badge variant="secondary" className="text-xs">
                  {template.fileName}
                </Badge>
              </CardContent>
            </Card>

            {/* Signature Info */}
            <Card className="dialog-content-solid">
              <CardHeader>
                <CardTitle className="text-sm flex items-center gap-2">
                  <User className="h-4 w-4" />
                  Selected Signature
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-2">
                {signature ? (
                  <>
                    <div className="text-sm font-medium">{signature.name}</div>
                    <div className="text-xs text-muted-foreground">{signature.fileName}</div>
                  </>
                ) : (
                  <div className="text-sm text-muted-foreground">No signature selected</div>
                )}
              </CardContent>
            </Card>

            {/* Employee List */}
            <Card className="dialog-content-solid">
              <CardHeader>
                <CardTitle className="text-sm flex items-center gap-2">
                  <User className="h-4 w-4" />
                  Selected Employees ({employees.length})
                </CardTitle>
              </CardHeader>
              <CardContent className="p-0">
                <ScrollArea className="h-64">
                  <div className="space-y-1 p-4">
                    {employees.map((employee, index) => (
                      <div
                        key={employee.id}
                        className={`p-2 rounded cursor-pointer transition-colors ${
                          selectedEmployee?.id === employee.id
                            ? 'bg-primary/10 border border-primary/20'
                            : 'hover:bg-muted/50'
                        }`}
                        onClick={() => handleEmployeeSelect(employee)}
                      >
                        <div className="text-sm font-medium">{employee.name}</div>
                        <div className="text-xs text-muted-foreground">
                          {employee.employeeId} • {employee.client}
                        </div>
                      </div>
                    ))}
                  </div>
                </ScrollArea>
              </CardContent>
            </Card>

            {/* Action Buttons */}
            <div className="space-y-2">
              <Button
                onClick={onGenerate}
                className="w-full"
                disabled={employees.length === 0}
              >
                <FileText className="h-4 w-4 mr-2" />
                Generate Letters ({employees.length})
              </Button>
              
              <Button
                onClick={onDownload}
                variant="outline"
                className="w-full"
                disabled={employees.length === 0}
              >
                <Download className="h-4 w-4 mr-2" />
                Download All
              </Button>
            </div>
          </div>

          {/* Right Side - Preview */}
          <div className="flex-1 min-h-0 overflow-hidden">
            <Card className="h-full dialog-content-solid">
              <CardHeader>
                <CardTitle className="text-sm flex items-center gap-2">
                  <Eye className="h-4 w-4" />
                  Letter Preview
                </CardTitle>
                <CardDescription>
                  {selectedEmployee ? `Preview for ${selectedEmployee.name}` : 'Select an employee to preview'}
                </CardDescription>
              </CardHeader>
              <CardContent className="h-full overflow-hidden">
                {selectedEmployee ? (
                  <div className="h-full flex flex-col">
                    {generatingPreview ? (
                      <div className="flex-1 flex items-center justify-center">
                        <div className="text-center">
                          <Loader2 className="h-8 w-8 animate-spin mx-auto mb-4" />
                          <p className="text-muted-foreground">Generating PDF preview...</p>
                        </div>
                      </div>
                    ) : previewUrl ? (
                      <div className="flex-1 overflow-hidden">
                        <iframe
                          src={`${previewUrl}#toolbar=0&navpanes=0&scrollbar=0&statusbar=0&messages=0&scrollbar=0&view=FitH`}
                          className="w-full h-full border-0"
                          title={`Letter preview for ${selectedEmployee.name}`}
                        />
                      </div>
                    ) : (
                      <div className="flex-1 flex items-center justify-center text-muted-foreground">
                        <div className="text-center">
                          <FileText className="h-12 w-12 mx-auto mb-4 opacity-50" />
                          <p>No preview available</p>
                          <p className="text-sm">Click on an employee to generate preview</p>
                        </div>
                      </div>
                    )}
                  </div>
                ) : (
                  <div className="h-full flex items-center justify-center text-muted-foreground">
                    <div className="text-center">
                      <User className="h-12 w-12 mx-auto mb-4 opacity-50" />
                      <p>Select an employee to preview their letter</p>
                    </div>
                  </div>
                )}
              </CardContent>
            </Card>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}
