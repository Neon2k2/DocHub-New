import React, { useState, useEffect, useMemo } from 'react';
import { Upload, FileText, Mail, Database, Edit3, Eye, MousePointer, CheckCircle, AlertCircle, Clock, X } from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../ui/card';
import { Button } from '../ui/button';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from '../ui/dialog';
import { Input } from '../ui/input';
import { Label } from '../ui/label';
import { Textarea } from '../ui/textarea';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../ui/select';
import { EmployeeSelector } from '../er/EmployeeSelector';
import { MultiStepTemplateDialog } from './MultiStepTemplateDialog';
import { SimplePreviewDialog } from './SimplePreviewDialog';
import { EmailComposerDialog } from '../er/EmailComposerDialog';
import { EnhancedEmailDialog } from './EnhancedEmailDialog';
import { Employee, apiService, DynamicField, FieldType, DynamicDocumentGenerationRequest, DynamicEmailRequest, EmailPriority, TabStatistics, TabDataRecord } from '../../services/api.service';
import { DynamicTab } from '../../services/tab.service';
import { DocumentTemplate, GeneratedDocument, EmailJob, Signature } from '../../services/document.service';
import { useTabData } from '../../hooks/useTabData';
import { excelService, ExcelData } from '../../services/excel.service';
import { ExcelUploadDialog } from './ExcelUploadDialog';
import { EmailHistoryDialog } from './EmailHistoryDialog';
import { signalRService, EmailStatusUpdate } from '../../services/signalr.service';
import { useAuth } from '../../contexts/AuthContext';
import { toast } from 'sonner';

interface DynamicLetterTabProps {
  tab: DynamicTab;
}

export function DynamicLetterTab({ tab }: DynamicLetterTabProps) {
  const { user } = useAuth();
  const [selectedEmployees, setSelectedEmployees] = useState<Employee[]>([]);
  const [loading, setLoading] = useState(true);
  const [dynamicFields, setDynamicFields] = useState<DynamicField[]>([]);
  const [fieldData, setFieldData] = useState<Record<string, any>>({});
  
  // Excel upload state
  const [showExcelUpload, setShowExcelUpload] = useState(false);
  const [excelData, setExcelData] = useState<ExcelData | null>(null);
  const [dataSourceType, setDataSourceType] = useState<'database' | 'excel' | null>(null);
  
  // Document generation state
  const [showTemplateDialog, setShowTemplateDialog] = useState(false);
  const [showPreviewDialog, setShowPreviewDialog] = useState(false);
  const [generatedDocuments, setGeneratedDocuments] = useState<GeneratedDocument[]>([]);
  const [selectedTemplate, setSelectedTemplate] = useState<DocumentTemplate | null>(null);
  const [selectedSignature, setSelectedSignature] = useState<Signature | null>(null);
  
  // Email state
  const [showEmailDialog, setShowEmailDialog] = useState(false);
  const [showEnhancedEmailDialog, setShowEnhancedEmailDialog] = useState(false);
  const [showEmailHistoryDialog, setShowEmailHistoryDialog] = useState(false);
  const [emailJobs, setEmailJobs] = useState<EmailJob[]>([]);
  
  // Tab state
  const [activeTab, setActiveTab] = useState<'employees' | 'history'>('employees');
  
  // Generating state
  const [showGeneratingDialog, setShowGeneratingDialog] = useState(false);
  const [generatingProgress, setGeneratingProgress] = useState(0);
  
  // Edit mode state
  const [isEditMode, setIsEditMode] = useState(false);
  
  // Table editing state
  const [editingCell, setEditingCell] = useState<{rowIndex: number, fieldKey: string} | null>(null);
  const [editingValue, setEditingValue] = useState<string>('');
  const [isUpdating, setIsUpdating] = useState(false);
  const [emailError, setEmailError] = useState<string>('');
  
  // Statistics state
  const [statistics, setStatistics] = useState<TabStatistics>({
    totalRequests: 0,
    pendingRequests: 0,
    templatesCount: 0,
    signaturesCount: 0
  });
  
  // Search and filter state
  const [searchQuery, setSearchQuery] = useState<string>('');
  const [departmentFilter, setDepartmentFilter] = useState<string>('all');
  const [statusFilter, setStatusFilter] = useState<string>('all');
  const [showFilters, setShowFilters] = useState<boolean>(false);
  
  // Email template state
  const [emailTemplate, setEmailTemplate] = useState<string>('');
  const [emailTemplateSubject, setEmailTemplateSubject] = useState<string>('');
  const [showEmailTemplateDialog, setShowEmailTemplateDialog] = useState<boolean>(false);
  
  // Fetch tab data for this tab
  const { data: tabData, loading: dataLoading, totalCount } = useTabData(tab.id);
  
  // Email validation function
  const validateEmail = (email: string): boolean => {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
  };

  // Function to update employee data
  const updateEmployeeData = async (employeeId: string, fieldKey: string, newValue: string) => {
    try {
      setIsUpdating(true);
      
      // Find the field configuration to get the correct field name
      const field = dynamicFields.find(f => f.fieldKey === fieldKey);
      const fieldName = field?.fieldName || fieldKey;
      
      console.log('Updating employee data:', {
        employeeId,
        fieldKey,
        fieldName,
        newValue,
        tabId: tab.id
      });
      
      // Use the tab-specific employee data update endpoint
      const response = await apiService.request<{success: boolean; message?: string}>(`/Tab/employee-data/${tab.id}`, {
        method: 'PUT',
        body: JSON.stringify({
          EmployeeId: employeeId,
          Field: fieldName, // Use fieldName instead of fieldKey
          Value: newValue
        })
      });
      
      if (response.success) {
        toast.success('Employee data updated successfully');
        // Refresh the data without reloading the page
        await loadData();
      } else {
        console.error('Update failed:', response);
        toast.error(`Failed to update employee data: ${response.message || 'Unknown error'}`);
      }
    } catch (error) {
      console.error('Error updating employee data:', error);
      
      // Try to extract more detailed error information
      if (error.response) {
        console.error('Response status:', error.response.status);
        console.error('Response data:', error.response.data);
        console.error('Response headers:', error.response.headers);
      }
      
      // Check if it's a 400 error and try to get the specific error message
      if (error.message && error.message.includes('400')) {
        try {
          // Find the field configuration to get the correct field name (re-declare for scope)
          const field = dynamicFields.find(f => f.fieldKey === fieldKey);
          const fieldNameForError = field?.fieldName || fieldKey;
          
          const errorResponse = await fetch(`http://localhost:5120/api/Tab/employee-data/${tab.id}`, {
            method: 'PUT',
            headers: {
              'Content-Type': 'application/json',
              'Authorization': `Bearer ${localStorage.getItem('token')}`
            },
            body: JSON.stringify({
              EmployeeId: employeeId,
              Field: fieldNameForError,
              Value: newValue
            })
          });
          
          const errorText = await errorResponse.text();
          console.error('Detailed 400 error response:', errorText);
          toast.error(`400 Error: ${errorText}`);
        } catch (fetchError) {
          console.error('Could not fetch detailed error:', fetchError);
          toast.error(`Error updating employee data: ${error.message || 'Unknown error'}`);
        }
      } else {
        toast.error(`Error updating employee data: ${error.message || 'Unknown error'}`);
      }
    } finally {
      setIsUpdating(false);
    }
  };
  
  // Handle cell double-click to start editing
  const handleCellDoubleClick = (rowIndex: number, fieldKey: string, currentValue: string) => {
    if (!isEditMode) return;
    
    setEditingCell({ rowIndex, fieldKey });
    setEditingValue(currentValue);
    setEmailError(''); // Clear any previous email errors
  };
  
  // Handle cell edit completion
  const handleCellEditComplete = async (employeeId: string, fieldKey: string) => {
    if (editingValue !== '') {
      // Validate email field specifically
      if (fieldKey === 'EMAIL' && !validateEmail(editingValue)) {
        setEmailError('Please enter a valid email address');
        toast.error('Please enter a valid email address');
        return; // Don't save if email is invalid
      }
      
      // Clear email error if validation passes
      setEmailError('');
      
      // Find the actual EMP ID from the employee data
      const employee = mappedEmployees.find(emp => emp.id === employeeId);
      const actualEmpId = employee?.employeeId || employeeId;
      
      console.log('Using actual EMP ID:', actualEmpId, 'for employee:', employeeId);
      
      await updateEmployeeData(actualEmpId, fieldKey, editingValue);
    }
    setEditingCell(null);
    setEditingValue('');
    setEmailError(''); // Clear error when closing edit mode
  };
  
  // Handle Enter key press
  const handleKeyPress = (e: React.KeyboardEvent, employeeId: string, fieldKey: string) => {
    if (e.key === 'Enter') {
      e.preventDefault(); // Prevent form submission
      handleCellEditComplete(employeeId, fieldKey);
    } else if (e.key === 'Escape') {
      e.preventDefault(); // Prevent any default behavior
      setEditingCell(null);
      setEditingValue('');
    }
    // Don't prevent default for other keys (like typing)
  };
  
  // Map data to Employee format for display - prioritize Excel data if available
  const mappedEmployees = useMemo(() => {
    console.log('🔍 [DYNAMIC-TAB] Mapping employees - Excel data:', excelData?.length || 0, 'Tab data:', tabData?.length || 0);
    
    // If we have Excel data, use it; otherwise use tabData
    if (excelData && excelData.length > 0) {
      console.log('🔍 [DYNAMIC-TAB] Mapping Excel data to employees:', excelData.length, 'rows');
      const mapped = excelData.map((data, index) => ({
        id: `excel_${index}`,
        name: data['EMP NAME'] || `Employee ${index + 1}`,
        firstName: data['EMP NAME'] || `Employee ${index + 1}`,
        lastName: '',
        email: data['EMAIL'] || '',
        phone: '',
        employeeId: data['EMP ID'] || `excel_${index}`,
        department: data['DESIGNATION'] || '',
        position: data['DESIGNATION'] || '',
        designation: data['DESIGNATION'] || '',
        hireDate: data['DOJ'] || '',
        joiningDate: data['DOJ'] || '',
        salary: data['CTC'] || 0,
        status: 'active',
        manager: '',
        location: '',
        isActive: true,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
        // Additional fields from Excel
        client: data['CLIENT'] || '',
        lastWorkingDay: data['LWD'] || '',
        ctc: data['CTC'] || 0,
        // Include the raw Excel data for placeholder replacement
        data: data
      } as Employee & { data: any }));
      console.log('🔍 [DYNAMIC-TAB] Mapped employees:', mapped.length, 'employees');
      console.log('🔍 [DYNAMIC-TAB] First employee sample:', mapped[0]);
      return mapped;
    }
    
    // Fallback to tabData
    console.log('🔍 [DYNAMIC-TAB] Using tabData fallback, records:', tabData.length);
    return tabData.map((record, index) => {
      try {
        console.log('🔍 [DYNAMIC-TAB] Processing record:', index, record);
        const data = JSON.parse(record.data);
        return {
          id: record.id,
          name: data['EMP NAME'] || data['firstName'] || `Employee ${index + 1}`,
          firstName: data['EMP NAME'] || data['firstName'] || `Employee ${index + 1}`,
          lastName: '',
          email: data['EMAIL'] || data['email'] || '',
          phone: '',
          employeeId: data['EMP ID'] || data['employeeId'] || record.id,
          department: data['DESIGNATION'] || data['department'] || '',
          position: data['DESIGNATION'] || data['position'] || '',
          designation: data['DESIGNATION'] || data['designation'] || '',
          hireDate: data['DOJ'] || data['hireDate'] || '',
          joiningDate: data['DOJ'] || data['joiningDate'] || '',
          salary: data['CTC'] || data['salary'] || 0,
          status: 'active',
          manager: '',
          location: '',
          isActive: true,
          createdAt: record.createdAt,
          updatedAt: record.updatedAt,
          // Additional fields from Excel
          client: data['CLIENT'] || data['client'] || '',
          lastWorkingDay: data['LWD'] || data['lastWorkingDay'] || '',
          ctc: data['CTC'] || data['ctc'] || 0,
          // Include the raw data for placeholder replacement
          data: data
        } as Employee & { data: any };
      } catch (error) {
        console.error('Error parsing employee data:', error);
        return {
          id: record.id,
          name: `Employee ${index + 1}`,
          firstName: `Employee ${index + 1}`,
          lastName: '',
          email: '',
          phone: '',
          employeeId: record.id,
          department: '',
          position: '',
          designation: '',
          hireDate: '',
          joiningDate: '',
          salary: 0,
          status: 'active',
          manager: '',
          location: '',
          isActive: true,
          createdAt: record.createdAt,
          updatedAt: record.updatedAt,
          client: '',
          lastWorkingDay: '',
          ctc: 0,
          data: {}
        } as Employee & { data: any };
      }
    });
  }, [excelData, tabData]);

  // Filter employees based on search and filter criteria
  const filteredEmployees = useMemo(() => {
    return mappedEmployees.filter(emp => {
      // Search filter
      const matchesSearch = !searchQuery || 
        emp.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
        emp.employeeId.toLowerCase().includes(searchQuery.toLowerCase()) ||
        emp.department.toLowerCase().includes(searchQuery.toLowerCase()) ||
        emp.designation.toLowerCase().includes(searchQuery.toLowerCase()) ||
        emp.email.toLowerCase().includes(searchQuery.toLowerCase());

      // Department filter
      const matchesDepartment = departmentFilter === 'all' || emp.department === departmentFilter;

      // Status filter
      const matchesStatus = statusFilter === 'all' || emp.status === statusFilter;

      return matchesSearch && matchesDepartment && matchesStatus;
    });
  }, [mappedEmployees, searchQuery, departmentFilter, statusFilter]);

  // Get unique departments for filter dropdown
  const departments = useMemo(() => {
    const deptSet = new Set(mappedEmployees.map(emp => emp.department).filter(Boolean));
    return Array.from(deptSet).sort();
  }, [mappedEmployees]);

  // Load statistics
  const loadStatistics = async () => {
    try {
      const response = await apiService.getTabStatistics(tab.id);
      if (response.success && response.data) {
        setStatistics(response.data);
      }
    } catch (error) {
      console.error('Error loading statistics:', error);
    }
  };

  const loadEmailTemplate = async () => {
    try {
      const response = await apiService.getEmailTemplate(tab.id);
      if (response.success && response.data) {
        setEmailTemplate(response.data.content);
        setEmailTemplateSubject(response.data.subject);
      }
    } catch (error) {
      console.error('Error loading email template:', error);
    }
  };

  const saveEmailTemplate = async () => {
    try {
      const response = await apiService.saveEmailTemplate(tab.id, {
        subject: emailTemplateSubject,
        content: emailTemplate
      });
      if (response.success) {
        toast.success('Email template saved successfully');
        setShowEmailTemplateDialog(false);
      } else {
        toast.error('Failed to save email template');
      }
    } catch (error) {
      console.error('Error saving email template:', error);
      toast.error('Failed to save email template');
    }
  };

  useEffect(() => {
    console.log('DynamicLetterTab received tab:', tab);
    loadData();
    loadStatistics();
    loadEmailTemplate();
    
    let callbackRef: ((update: EmailStatusUpdate) => void) | null = null;
    initializeSignalR(user).then(callback => {
      callbackRef = callback;
    });
    
    return () => {
      if (callbackRef) {
        signalRService.offEmailStatusUpdated(callbackRef);
      }
    };
  }, [tab.id, user]);

  const initializeSignalR = async (user: any) => {
    try {
      await signalRService.start();
      
      const handleEmailStatusUpdate = (update: EmailStatusUpdate) => {
        console.log('📧 [DYNAMIC-TAB] Received email status update:', update);
        
        // Update the email jobs list if we're on the history tab
        if (activeTab === 'history') {
          setEmailJobs(prev => prev.map(job => 
            job.id === update.emailJobId 
              ? { ...job, status: update.status as any }
              : job
          ));
        }
      };
      
      signalRService.onEmailStatusUpdated(handleEmailStatusUpdate);
      
      // Manually join user group as backup
      if (user?.id) {
        await signalRService.joinUserGroup(user.id);
      }
      
      // Store the callback reference for cleanup
      return handleEmailStatusUpdate;
    } catch (error) {
      console.error('Failed to initialize SignalR for email status updates:', error);
      return null;
    }
  };

  const loadData = async () => {
    try {
      setLoading(true);
      
      // Load dynamic fields from the tab's FieldConfiguration
      if (tab.fieldConfiguration) {
        try {
          console.log('Tab field configuration:', tab.fieldConfiguration);
          const fieldConfig = typeof tab.fieldConfiguration === 'string' 
            ? JSON.parse(tab.fieldConfiguration) 
            : tab.fieldConfiguration;
          
          if (fieldConfig.fields && Array.isArray(fieldConfig.fields)) {
            const fields: DynamicField[] = fieldConfig.fields.map((field: any, index: number) => ({
              id: field.id || `field_${index}`,
              fieldKey: field.fieldKey || field.fieldName,
              fieldName: field.fieldName || field.fieldKey,
              displayName: field.displayName || field.fieldName,
              fieldType: field.fieldType as FieldType || FieldType.Text,
              isRequired: field.isRequired || false,
              defaultValue: field.defaultValue || '',
              validationRules: field.validationRules || '',
              orderIndex: field.order || index
            }));
            
            setDynamicFields(fields);
            console.log('Loaded dynamic fields:', fields);
          }
        } catch (error) {
          console.error('Error parsing field configuration:', error);
        }
      } else {
        console.log('No field configuration found for tab:', tab);
      }

      // Load Excel data if available
      try {
        console.log('Loading Excel data for tab ID:', tab.id);
        const excelData = await excelService.getExcelDataForTab(tab.id);
        console.log('Loaded Excel data for tab:', excelData);
        
        if (excelData && excelData.data && excelData.data.length > 0) {
          setExcelData(excelData.data);
          setDataSourceType('excel');
        } else {
          setDataSourceType('database');
        }
      } catch (error) {
        console.error('Error loading Excel data:', error);
        setDataSourceType('database');
      }
    } catch (error) {
      console.error('Error loading tab data:', error);
    } finally {
      setLoading(false);
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
    setShowEnhancedEmailDialog(true);
  };

  const handleViewRequests = async () => {
    try {
      const response = await apiService.getGeneratedDocuments(tab.id);
      if (response.success && response.data) {
        setGeneratedDocuments(response.data);
        setShowPreviewDialog(true);
      } else {
        toast.error('No generated documents found');
      }
    } catch (error) {
      console.error('Error loading generated documents:', error);
      toast.error('Failed to load generated documents');
    }
  };

  const handleHistory = async () => {
    try {
      setActiveTab('history');
      await loadEmailHistory();
    } catch (error) {
      console.error('Error loading email history:', error);
      toast.error('Failed to load email history');
    }
  };

  const loadEmailHistory = async () => {
    try {
      setLoading(true);
      console.log('📧 [DYNAMIC-TAB] Loading email history for tab:', tab.id);
      const response = await apiService.getTabEmailHistory(tab.id);
      console.log('📊 [DYNAMIC-TAB] Email history response:', response);
      
      if (response.success && response.data) {
        console.log('✅ [DYNAMIC-TAB] Successfully loaded', response.data.length, 'email jobs');
        setEmailJobs(response.data);
      } else {
        console.log('⚠️ [DYNAMIC-TAB] No email history data received');
        setEmailJobs([]);
      }
    } catch (error) {
      console.error('❌ [DYNAMIC-TAB] Error loading email history:', error);
      toast.error('Failed to load email history');
      setEmailJobs([]);
    } finally {
      setLoading(false);
    }
  };

  const handleExcelUploadSuccess = async (data: ExcelData) => {
    setExcelData(data);
    setDataSourceType('excel');
    toast.success(`Excel file uploaded successfully! ${data.data?.length || 0} rows loaded.`);
    
    // Refresh the data to show the uploaded employees
    await loadData();
  };

  const handleTemplateSelect = async (template: DocumentTemplate) => {
    console.log('🔍 [DynamicLetterTab] Template selected in dialog:', template);
    setSelectedTemplate(template);
    // Don't close dialog yet - let user proceed to signature selection
  };

  const handleTemplateAndSignatureComplete = async (template: DocumentTemplate, signature: Signature | null) => {
    console.log('🔍 [DynamicLetterTab] Template and signature complete');
    console.log('🔍 [DynamicLetterTab] Template:', template);
    console.log('🔍 [DynamicLetterTab] Signature:', signature);
    console.log('🔍 [DynamicLetterTab] Selected employees count:', selectedEmployees.length);
    console.log('🔍 [DynamicLetterTab] Selected employees:', selectedEmployees);
    
    setSelectedTemplate(template);
    setSelectedSignature(signature);
    setShowTemplateDialog(false);
    
    // Check if employees are selected
    if (selectedEmployees.length === 0) {
      console.warn('⚠️ [DynamicLetterTab] No employees selected for letter generation');
      toast.warning('Please select employees before generating letters');
      return;
    }
    
    // Show preview dialog instead of immediately generating
    console.log('🔍 [DynamicLetterTab] Opening preview dialog');
    setShowPreviewDialog(true);
  };

  const handleGenerateLetters = async () => {
    console.log('🔍 [DynamicLetterTab] Generate Letters button clicked');
    console.log('🔍 [DynamicLetterTab] Current template:', selectedTemplate);
    console.log('🔍 [DynamicLetterTab] Current signature:', selectedSignature);
    console.log('🔍 [DynamicLetterTab] Selected employees:', selectedEmployees);
    
    if (selectedEmployees.length === 0) {
      console.warn('⚠️ [DynamicLetterTab] No employees selected');
      toast.warning('Please select employees before generating letters');
      return;
    }
    
    // Always open template selection dialog to allow user to confirm or change template/signature
    console.log('🔍 [DynamicLetterTab] Opening template selection dialog');
    setShowTemplateDialog(true);
  };

  const handleActualLetterGeneration = async () => {
    console.log('🔍 [DynamicLetterTab] Starting actual letter generation');
    console.log('🔍 [DynamicLetterTab] Template:', selectedTemplate);
    console.log('🔍 [DynamicLetterTab] Signature:', selectedSignature);
    console.log('🔍 [DynamicLetterTab] Selected employees:', selectedEmployees);
    
    if (!selectedTemplate || !selectedSignature) {
      console.error('❌ [DynamicLetterTab] Missing template or signature for generation');
      toast.error('Template and signature are required for letter generation');
      return;
    }
    
    try {
      // Show generating dialog
      setShowGeneratingDialog(true);
      setGeneratingProgress(0);
      
      console.log('🔍 [DynamicLetterTab] Calling API to generate letters');
      console.log('🔍 [DynamicLetterTab] API endpoint:', `/Tab/${tab.id}/generate-letters`);
      console.log('🔍 [DynamicLetterTab] Request body:', {
        employeeIds: selectedEmployees.map(emp => emp.employeeId),
        templateId: selectedTemplate.id,
        signaturePath: selectedSignature?.id
      });
      
      // Simulate progress updates
      const progressInterval = setInterval(() => {
        setGeneratingProgress(prev => {
          if (prev >= 90) {
            clearInterval(progressInterval);
            return prev;
          }
          return prev + 10;
        });
      }, 200);

      // Call the new letter generation API using binary request
      const blob = await apiService.requestBinary(`/Tab/${tab.id}/generate-letters`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          employeeIds: selectedEmployees.map(emp => emp.employeeId),
          templateId: selectedTemplate.id,
          signaturePath: selectedSignature?.id
        })
      });

      console.log('✅ [DynamicLetterTab] Letters generated successfully');
      console.log('🔍 [DynamicLetterTab] Blob size:', blob.size);
      console.log('🔍 [DynamicLetterTab] Blob type:', blob.type);

      // Complete progress
      clearInterval(progressInterval);
      setGeneratingProgress(100);
      
      // Create download link for the ZIP file
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `generated-letters-${new Date().toISOString().split('T')[0]}.zip`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      window.URL.revokeObjectURL(url);
      
      // Close generating dialog after a short delay
      setTimeout(() => {
        setShowGeneratingDialog(false);
        setGeneratingProgress(0);
      }, 500);
      
      toast.success(`Generated letters for ${selectedEmployees.length} employees successfully`);
    } catch (error) {
      console.error('❌ [DynamicLetterTab] Error generating letters:', error);
      toast.error('Failed to generate letters. Please try again.');
      setShowGeneratingDialog(false);
      setGeneratingProgress(0);
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
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-900 dark:text-white">{tab.name}</h1>
          <p className="text-gray-600 dark:text-gray-400 mt-1">{tab.description}</p>
        </div>
        <div className="flex items-center gap-2">
          <span className={`px-3 py-1 rounded-full text-sm font-medium ${
            tab.isActive 
              ? 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200' 
              : 'bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-200'
          }`}>
            {tab.isActive ? 'Active' : 'Inactive'}
          </span>
        </div>
      </div>

      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <Card className="glass-panel border-glass-border">
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-400">Total Requests</p>
                <p className="text-2xl font-bold text-gray-900 dark:text-white">{statistics.totalRequests}</p>
              </div>
              <FileText className="h-8 w-8 text-blue-500" />
            </div>
          </CardContent>
        </Card>

        <Card className="glass-panel border-glass-border">
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-400">Pending</p>
                <p className="text-2xl font-bold text-gray-900 dark:text-white">{statistics.pendingRequests}</p>
              </div>
              <div className="h-8 w-8 rounded-full bg-orange-500 flex items-center justify-center">
                <div className="h-4 w-4 border-2 border-white border-t-transparent rounded-full animate-spin"></div>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card className="glass-panel border-glass-border">
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-400">Templates</p>
                <p className="text-2xl font-bold text-gray-900 dark:text-white">{statistics.templatesCount}</p>
              </div>
              <div className="h-8 w-8 rounded-full bg-green-500 flex items-center justify-center">
                <FileText className="h-4 w-4 text-white" />
              </div>
            </div>
          </CardContent>
        </Card>

        <Card className="glass-panel border-glass-border">
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-400">Signatures</p>
                <p className="text-2xl font-bold text-gray-900 dark:text-white">{statistics.signaturesCount}</p>
              </div>
              <div className="h-8 w-8 rounded-full bg-purple-500 flex items-center justify-center">
                <div className="h-4 w-4 text-white">✍</div>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Action Tabs */}
      <div className="flex border-b border-gray-300 dark:border-gray-700">
        <button
          onClick={() => setActiveTab('employees')}
          className={`flex items-center gap-2 px-4 py-3 text-sm font-medium border-b-2 transition-colors ${
            activeTab === 'employees'
              ? 'text-gray-900 dark:text-white border-blue-500 bg-gray-50 dark:bg-gray-800'
              : 'text-gray-600 hover:text-gray-900 dark:text-gray-400 dark:hover:text-white border-transparent hover:border-gray-400 dark:hover:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-800'
          }`}
        >
          <Database className="h-4 w-4" />
          <span className="font-medium">Employees</span>
        </button>
        
        <button
          onClick={handleHistory}
          className={`flex items-center gap-2 px-4 py-3 text-sm font-medium border-b-2 transition-colors ${
            activeTab === 'history'
              ? 'text-gray-900 dark:text-white border-blue-500 bg-gray-50 dark:bg-gray-800'
              : 'text-gray-600 hover:text-gray-900 dark:text-gray-400 dark:hover:text-white border-transparent hover:border-gray-400 dark:hover:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-800'
          }`}
        >
          <FileText className="h-4 w-4" />
          <span className="font-medium">History</span>
        </button>
      </div>

      {/* Content Section */}
      {activeTab === 'employees' && (
        <>
        <Card className="glass-panel border-glass-border">
          <CardContent className="p-6">
          <div className="flex items-center justify-between mb-4">
            <div className="flex items-center gap-3">
              <Database className="h-6 w-6 text-blue-500" />
              <div>
                <h3 className="text-lg font-semibold text-gray-900 dark:text-white">Employee Data</h3>
                <p className="text-sm text-gray-600 dark:text-gray-400">
                  {dataSourceType === 'excel' && excelData && excelData.data
                    ? `Loaded ${excelData.data.length} rows from ${excelData.fileName}`
                    : 'Upload Excel file to populate employee data for this tab'
                  }
                </p>
              </div>
            </div>
            {selectedEmployees.length > 0 ? (
              <div className="flex gap-3">
                <Button
                  onClick={handleGenerateLetters}
                  className="bg-green-600 hover:bg-green-700 text-white"
                  style={{ color: 'white', backgroundColor: '#059669' }}
                >
                  <FileText className="h-4 w-4 mr-2" style={{ color: 'white' }} />
                  <span style={{ color: 'white', fontWeight: '500' }}>Generate Letters ({selectedEmployees.length})</span>
                </Button>
                <Button
                  onClick={() => handleSendEmail(selectedEmployees)}
                  className="bg-purple-600 hover:bg-purple-700 text-white"
                  style={{ color: 'white', backgroundColor: '#7c3aed' }}
                >
                  <Mail className="h-4 w-4 mr-2" style={{ color: 'white' }} />
                  <span style={{ color: 'white', fontWeight: '500' }}>Send Email ({selectedEmployees.length})</span>
                </Button>
                <Button
                  onClick={() => setShowExcelUpload(true)}
                  variant="outline"
                  className="border-gray-300 text-gray-700 hover:bg-gray-100 hover:text-gray-900 dark:border-gray-600 dark:text-gray-300 dark:hover:bg-gray-700 dark:hover:text-white"
                  style={{ 
                    color: '#374151',
                    borderColor: '#d1d5db',
                    backgroundColor: 'transparent'
                  }}
                >
                  <Upload className="h-4 w-4 mr-2" style={{ color: '#374151' }} />
                  <span style={{ color: '#374151', fontWeight: '500' }}>Upload New File</span>
                </Button>
              </div>
            ) : (
              <Button
                onClick={() => setShowExcelUpload(true)}
                className="bg-blue-600 hover:bg-blue-700 text-white border-blue-600"
                style={{ color: 'white', backgroundColor: '#2563eb' }}
              >
                <Upload className="h-4 w-4 mr-2" style={{ color: 'white' }} />
                <span style={{ color: 'white', fontWeight: '500' }}>{excelData ? 'Upload New File' : 'Upload Excel File'}</span>
              </Button>
            )}
          </div>

        </CardContent>
      </Card>

      {/* Select Employees Section */}
      <Card className="glass-panel border-glass-border">
        <CardContent className="p-6">
          <div className="mb-4">
            <div className="flex items-center justify-between mb-2">
              <div>
                <h3 className="text-lg font-semibold text-gray-900 dark:text-white">Select Employees</h3>
                <p className="text-sm text-gray-600 dark:text-gray-400">Choose employees to generate letters for</p>
              </div>
              <div className="flex items-center gap-3">
                <Button
                  onClick={() => setShowEmailTemplateDialog(true)}
                  variant="outline"
                  size="sm"
                  className="text-gray-600 hover:text-gray-900 dark:text-gray-400 dark:hover:text-white"
                >
                  <Mail className="h-4 w-4 mr-2" />
                  Set Email Template
                </Button>
                <span className="text-sm font-medium text-gray-700 dark:text-gray-300" style={{ color: '#374151' }}>
                  {isEditMode ? 'View Mode' : 'Edit Mode'}
                </span>
                <button
                  onClick={() => setIsEditMode(!isEditMode)}
                  className={`relative inline-flex h-5 w-9 items-center rounded-full transition-all duration-200 ease-in-out focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 ${
                    isEditMode ? 'bg-blue-500' : 'bg-gray-300'
                  }`}
                  style={{
                    backgroundColor: isEditMode ? '#3b82f6' : '#d1d5db'
                  }}
                >
                  <span
                    className={`inline-block h-3.5 w-3.5 transform rounded-full bg-white shadow-sm transition-all duration-200 ease-in-out ${
                      isEditMode ? 'translate-x-4' : 'translate-x-0.5'
                    }`}
                    style={{
                      transform: isEditMode ? 'translateX(1rem)' : 'translateX(0.125rem)',
                      boxShadow: '0 1px 2px 0 rgba(0, 0, 0, 0.1)'
                    }}
                  />
                </button>
              </div>
            </div>
            
            {/* Search and Filters */}
            <div className="flex gap-4 mb-4">
              <div className="flex-1">
                  <Input
                    placeholder="Search employees by name, ID, or department..."
                    value={searchQuery}
                    onChange={(e) => setSearchQuery(e.target.value)}
                    className="bg-white border-gray-300 text-gray-900 placeholder-gray-500 dark:bg-gray-800 dark:border-gray-600 dark:placeholder-gray-400"
                    style={{ 
                      color: '#111827',
                      '--tw-text-opacity': '1'
                    } as React.CSSProperties}
                  />
              </div>
              <Button
                onClick={() => setShowFilters(!showFilters)}
                variant="outline"
                className="border-gray-300 text-gray-700 hover:bg-gray-100 dark:border-gray-600 dark:text-gray-300 dark:hover:bg-gray-700"
              >
                Filters
              </Button>
            </div>

            {/* Filter Dropdowns */}
            {showFilters && (
              <div className="flex gap-4 mb-4 p-4 bg-gray-50 dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700">
                <div className="flex-1">
                  <Label htmlFor="department-filter" className="text-sm font-medium text-gray-700 dark:text-gray-300">
                    Department
                  </Label>
                  <Select value={departmentFilter} onValueChange={setDepartmentFilter}>
                    <SelectTrigger className="mt-1">
                      <SelectValue placeholder="All Departments" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="all">All Departments</SelectItem>
                      {departments.map((dept) => (
                        <SelectItem key={dept} value={dept}>
                          {dept}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
                <div className="flex-1">
                  <Label htmlFor="status-filter" className="text-sm font-medium text-gray-700 dark:text-gray-300">
                    Status
                  </Label>
                  <Select value={statusFilter} onValueChange={setStatusFilter}>
                    <SelectTrigger className="mt-1">
                      <SelectValue placeholder="All Status" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="all">All Status</SelectItem>
                      <SelectItem value="active">Active</SelectItem>
                      <SelectItem value="inactive">Inactive</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
                <div className="flex items-end">
                  <Button
                    onClick={() => {
                      setSearchQuery('');
                      setDepartmentFilter('all');
                      setStatusFilter('all');
                    }}
                    variant="outline"
                    size="sm"
                    className="text-gray-600 hover:text-gray-900 dark:text-gray-400 dark:hover:text-white"
                  >
                    Clear Filters
                  </Button>
                </div>
              </div>
            )}
          </div>

          {/* Results Counter */}
          <div className="mb-3 text-sm text-gray-600 dark:text-gray-400">
            Showing {filteredEmployees.length} of {mappedEmployees.length} employees
            {(searchQuery || departmentFilter !== 'all' || statusFilter !== 'all') && (
              <span className="ml-2 text-blue-600 dark:text-blue-400">
                (filtered)
              </span>
            )}
          </div>

          {/* Dynamic Table */}
          {dynamicFields.length > 0 ? (
            <div className="overflow-x-auto">
              <table className={`w-full border-collapse ${isEditMode ? 'neon-border-blue rounded-lg' : ''}`}>
                  <thead>
                    <tr className="border-b border-gray-700">
                      <th className="text-left p-3 text-sm font-medium text-gray-300">Select</th>
                      {dynamicFields
                        .sort((a, b) => (a.orderIndex || 0) - (b.orderIndex || 0))
                        .map((field) => (
                          <th key={field.id} className="text-left p-3 text-sm font-medium text-gray-300">
                            {field.displayName}
                          </th>
                        ))}
                    </tr>
                  </thead>
                <tbody>
                  {filteredEmployees.length > 0 ? (
                    filteredEmployees.map((employee, index) => (
                      <tr key={employee.id} className="border-b border-gray-800 hover:bg-gray-800/50">
                        <td className="p-3">
                          <input
                            type="checkbox"
                            className="rounded border-gray-600 bg-gray-700 text-blue-600 focus:ring-blue-500"
                            onChange={(e) => {
                              console.log('🔍 [DYNAMIC-TAB] Checkbox changed:', e.target.checked, 'for employee:', employee.name);
                              if (e.target.checked) {
                                setSelectedEmployees(prev => {
                                  const newSelection = [...prev, employee];
                                  console.log('🔍 [DYNAMIC-TAB] Added employee, new selection:', newSelection.length);
                                  return newSelection;
                                });
                              } else {
                                setSelectedEmployees(prev => {
                                  const newSelection = prev.filter(emp => emp.id !== employee.id);
                                  console.log('🔍 [DYNAMIC-TAB] Removed employee, new selection:', newSelection.length);
                                  return newSelection;
                                });
                              }
                            }}
                            checked={selectedEmployees.some(emp => emp.id === employee.id)}
                          />
                        </td>
                        {/* Dynamic Columns */}
                        {dynamicFields
                          .sort((a, b) => (a.orderIndex || 0) - (b.orderIndex || 0))
                          .map((field) => {
                            // Map fieldKey to Employee property or Excel data
                            let fieldValue = '-';
                            if (employee.data && employee.data[field.fieldKey]) {
                              // Use Excel data if available
                              fieldValue = employee.data[field.fieldKey];
                            } else {
                              // Map to Employee properties
                              switch (field.fieldKey) {
                                case 'EMP ID':
                                  fieldValue = employee.employeeId || '-';
                                  break;
                                case 'EMP NAME':
                                  fieldValue = employee.name || '-';
                                  break;
                                case 'CLIENT':
                                  fieldValue = employee.client || '-';
                                  break;
                                case 'DOJ':
                                  fieldValue = employee.hireDate || employee.joiningDate || '-';
                                  break;
                                case 'LWD':
                                  fieldValue = employee.lastWorkingDay || '-';
                                  break;
                                case 'DESIGNATION':
                                  fieldValue = employee.designation || employee.department || employee.position || '-';
                                  break;
                                case 'CTC':
                                  fieldValue = employee.ctc || employee.salary || '-';
                                  break;
                                case 'EMAIL':
                                  fieldValue = employee.email || '-';
                                  break;
                                default:
                                  // Try to find the field in employee data
                                  const normalizedKey = field.fieldKey.toLowerCase().replace(/\s+/g, '');
                                  fieldValue = employee[normalizedKey as keyof Employee] as string || '-';
                              }
                            }
                            
                            // Format the value based on field type
                            if (field.fieldType === 'Date' && fieldValue && fieldValue !== '-') {
                              try {
                                fieldValue = new Date(fieldValue).toLocaleDateString();
                              } catch (e) {
                                // Keep original value if date parsing fails
                              }
                            }
                            const isEditing = editingCell?.rowIndex === index && editingCell?.fieldKey === field.fieldKey;
                            
                            return (
                              <td 
                                key={field.id}
                                className={`p-3 text-sm text-gray-300 ${isEditMode ? 'cursor-pointer hover:bg-gray-800/30 group' : ''} ${isEditing ? 'bg-blue-900/20' : ''}`}
                                onDoubleClick={() => handleCellDoubleClick(index, field.fieldKey, fieldValue)}
                              >
                                {isEditing ? (
                                  <div>
                                    <input
                                      type={field.fieldType === 'Email' ? 'email' : 'text'}
                                      value={editingValue}
                                      onChange={(e) => setEditingValue(e.target.value)}
                                      onKeyDown={(e) => handleKeyPress(e, employee.id, field.fieldKey)}
                                      onBlur={() => handleCellEditComplete(employee.id, field.fieldKey)}
                                      className={`w-full bg-white dark:bg-gray-700 text-gray-900 dark:text-white border rounded px-2 py-1 text-sm focus:outline-none focus:ring-2 ${
                                        emailError && field.fieldType === 'Email' ? 'border-red-500 focus:ring-red-500' : 'border-gray-300 dark:border-gray-600 focus:ring-blue-500'
                                      }`}
                                      style={{ 
                                        color: '#111827',
                                        backgroundColor: '#ffffff'
                                      }}
                                      autoFocus
                                      disabled={isUpdating}
                                    />
                                    {emailError && field.fieldType === 'Email' && (
                                      <div className="text-red-500 text-xs mt-1">
                                        {emailError}
                                      </div>
                                    )}
                                  </div>
                                ) : (
                                  <span className={isEditMode ? 'group-hover:text-blue-400 transition-colors duration-200' : ''}>
                                    {fieldValue}
                                  </span>
                                )}
                              </td>
                            );
                          })}
                      </tr>
                    ))
                  ) : (
                    <tr>
                      <td colSpan={dynamicFields.length + 1} className="p-8 text-center text-gray-400">
                        <div className="flex flex-col items-center gap-2">
                          <div className="h-16 w-16 rounded-full bg-gray-800 flex items-center justify-center mb-4">
                            <div className="h-8 w-8 text-gray-600">👥</div>
                          </div>
                          <p className="text-lg font-medium mb-2">{filteredEmployees.length} employees found</p>
                          <p className="text-sm">
                            {searchQuery || departmentFilter !== 'all' || statusFilter !== 'all' 
                              ? 'No employees found matching your criteria' 
                              : 'No employees available'
                            }
                          </p>
                        </div>
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
          ) : (
            <div className="flex flex-col items-center justify-center py-12 text-gray-400">
              <div className="h-16 w-16 rounded-full bg-gray-800 flex items-center justify-center mb-4">
                <div className="h-8 w-8 text-gray-600">👥</div>
              </div>
              <p className="text-lg font-medium mb-2">{filteredEmployees.length} employees found</p>
              <p className="text-sm">
                {searchQuery || departmentFilter !== 'all' || statusFilter !== 'all' 
                  ? 'No employees found matching your criteria' 
                  : 'No employees available'
                }
              </p>
            </div>
          )}

        </CardContent>
      </Card>
      </>
      )}

      {/* History Section */}
      {activeTab === 'history' && (
        <Card className="glass-panel border-glass-border">
          <CardContent className="p-6">
            <div className="flex items-center justify-between mb-4">
              <div className="flex items-center gap-3">
                <FileText className="h-6 w-6 text-blue-500" />
                <div>
                  <h3 className="text-lg font-semibold text-gray-900 dark:text-white">Email History</h3>
                  <p className="text-sm text-gray-600 dark:text-gray-400">View all email communications for this tab</p>
                </div>
              </div>
            </div>

            {loading ? (
              <div className="flex items-center justify-center py-12">
                <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-500"></div>
                <span className="ml-2 text-gray-600 dark:text-gray-400">Loading email history...</span>
              </div>
            ) : emailJobs.length > 0 ? (
              <div className="space-y-4">
                {emailJobs.map((job) => (
                  <div key={job.id} className="flex items-center gap-4 p-4 glass-panel rounded-lg border-glass-border hover:bg-muted/50 transition-colors">
                    <div className="flex-shrink-0">
                      {job.status === 'sent' && <CheckCircle className="h-5 w-5 text-green-500" />}
                      {job.status === 'delivered' && <CheckCircle className="h-5 w-5 text-blue-500" />}
                      {job.status === 'opened' && <Eye className="h-5 w-5 text-purple-500" />}
                      {job.status === 'clicked' && <MousePointer className="h-5 w-5 text-indigo-500" />}
                      {job.status === 'bounced' && <AlertCircle className="h-5 w-5 text-red-500" />}
                      {job.status === 'dropped' && <X className="h-5 w-5 text-red-500" />}
                      {job.status === 'pending' && <Clock className="h-5 w-5 text-yellow-500" />}
                      {job.status === 'failed' && <X className="h-5 w-5 text-red-500" />}
                    </div>

                    <div className="flex-1 min-w-0">
                      <div className="flex items-center justify-between mb-2">
                        <div className="flex items-center gap-2">
                          <h4 className="font-medium truncate">{job.employeeName || 'Unknown Employee'}</h4>
                          <span className="text-sm text-muted-foreground">
                            {job.employeeEmail || job.recipientEmail || 'No email'}
                          </span>
                        </div>
                        <div className="flex items-center gap-2">
                          <span className={`px-2 py-1 text-xs rounded-full ${
                            job.status === 'sent' ? 'bg-green-100 text-green-800' :
                            job.status === 'delivered' ? 'bg-blue-100 text-blue-800' :
                            job.status === 'opened' ? 'bg-purple-100 text-purple-800' :
                            job.status === 'clicked' ? 'bg-indigo-100 text-indigo-800' :
                            job.status === 'bounced' ? 'bg-red-100 text-red-800' :
                            job.status === 'dropped' ? 'bg-red-100 text-red-800' :
                            job.status === 'pending' ? 'bg-yellow-100 text-yellow-800' :
                            'bg-gray-100 text-gray-800'
                          }`}>
                            {job.status}
                          </span>
                          <span className="text-xs text-muted-foreground">
                            {new Date(job.createdAt).toLocaleString()}
                          </span>
                        </div>
                      </div>

                      <div className="text-sm text-muted-foreground">
                        <p className="truncate">{job.subject}</p>
                        {job.errorMessage && (
                          <p className="text-red-500 text-xs mt-1">
                            Error: {job.errorMessage}
                          </p>
                        )}
                      </div>

                      <div className="flex items-center gap-4 mt-2 text-xs text-muted-foreground">
                        {job.sentAt && (
                          <span>Sent: {new Date(job.sentAt).toLocaleString()}</span>
                        )}
                        {job.deliveredAt && (
                          <span>Delivered: {new Date(job.deliveredAt).toLocaleString()}</span>
                        )}
                        {job.openedAt && (
                          <span>Opened: {new Date(job.openedAt).toLocaleString()}</span>
                        )}
                        {job.clickedAt && (
                          <span>Clicked: {new Date(job.clickedAt).toLocaleString()}</span>
                        )}
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              <div className="flex flex-col items-center justify-center py-12 text-gray-400">
                <div className="h-16 w-16 rounded-full bg-gray-800 flex items-center justify-center mb-4">
                  <Mail className="h-8 w-8 text-gray-600" />
                </div>
                <p className="text-lg font-medium mb-2">No email history found</p>
                <p className="text-sm">No emails have been sent for this tab yet</p>
              </div>
            )}
          </CardContent>
        </Card>
      )}

      {/* Dialogs */}
      {showExcelUpload && (
        <ExcelUploadDialog
          open={showExcelUpload}
          onOpenChange={setShowExcelUpload}
          tabId={tab.id}
          tabName={tab.name}
          onUploadSuccess={handleExcelUploadSuccess}
        />
      )}

      {showTemplateDialog && (
        <MultiStepTemplateDialog
          open={showTemplateDialog}
          onOpenChange={setShowTemplateDialog}
          letterType={tab.letterType || 'confirmation_letter'}
          onTemplateSelect={handleTemplateSelect}
          onComplete={handleTemplateAndSignatureComplete}
          initialTemplate={selectedTemplate}
          initialSignature={selectedSignature}
        />
      )}

      {showPreviewDialog && selectedTemplate && (
        <SimplePreviewDialog
          open={showPreviewDialog}
          onOpenChange={setShowPreviewDialog}
          template={selectedTemplate}
          signature={selectedSignature}
          employees={selectedEmployees}
          tabId={tab.id}
          onGenerate={handleActualLetterGeneration}
          onDownload={handleActualLetterGeneration}
        />
      )}

      {showEmailDialog && (
        <EmailComposerDialog
          open={showEmailDialog}
          onOpenChange={setShowEmailDialog}
          employees={selectedEmployees || []}
          onEmailsSent={() => {}}
        />
      )}

      {showEnhancedEmailDialog && (
        <EnhancedEmailDialog
          open={showEnhancedEmailDialog}
          onOpenChange={setShowEnhancedEmailDialog}
          employees={selectedEmployees || []}
          tabId={tab.id}
          emailTemplate={emailTemplate}
          emailTemplateSubject={emailTemplateSubject}
          onEmailsSent={(jobs) => {
            setEmailJobs(jobs);
            toast.success(`Successfully sent ${jobs.length} emails`);
          }}
        />
      )}

      {/* Generating Dialog */}
      {showGeneratingDialog && (
        <Dialog open={showGeneratingDialog} onOpenChange={() => {}}>
          <DialogContent className="sm:max-w-md">
            <DialogHeader>
              <DialogTitle className="flex items-center gap-2">
                <FileText className="h-5 w-5 text-blue-500" />
                Generating Documents
              </DialogTitle>
              <DialogDescription>
                Please wait while we generate your documents...
              </DialogDescription>
            </DialogHeader>
            <div className="space-y-4">
              <div className="text-center">
                <div className="text-2xl font-bold text-blue-500 mb-2">
                  {generatingProgress}%
                </div>
                <div className="w-full bg-gray-200 rounded-full h-2">
                  <div 
                    className="bg-blue-500 h-2 rounded-full transition-all duration-300"
                    style={{ width: `${generatingProgress}%` }}
                  ></div>
                </div>
              </div>
              <div className="text-center text-sm text-gray-500">
                Generating documents for {selectedEmployees.length} employee{selectedEmployees.length !== 1 ? 's' : ''}...
              </div>
            </div>
          </DialogContent>
        </Dialog>
      )}

      {/* Email Template Dialog */}
      <Dialog open={showEmailTemplateDialog} onOpenChange={setShowEmailTemplateDialog}>
        <DialogContent className="sm:max-w-2xl">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <Mail className="h-5 w-5 text-blue-500" />
              Set Email Template for {tab.name}
            </DialogTitle>
            <DialogDescription>
              Set the default email content that will be prefilled for all employees in this tab. You can use placeholders like {`{employeeName}`}, {`{employeeEmail}`}, etc.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            <div>
              <Label htmlFor="email-template-subject" className="text-sm font-medium">
                Email Subject Template
              </Label>
              <Input
                id="email-template-subject"
                value={emailTemplateSubject}
                onChange={(e) => setEmailTemplateSubject(e.target.value)}
                placeholder="Document Request - {employeeName}"
                className="mt-2"
              />
            </div>
            <div>
              <Label htmlFor="email-template" className="text-sm font-medium">
                Email Template Content
              </Label>
              <Textarea
                id="email-template"
                rows={12}
                value={emailTemplate}
                onChange={(e) => setEmailTemplate(e.target.value)}
                placeholder={`Dear {employeeName},

Please find attached your requested document.

Best regards,
HR Department`}
                className="mt-2 resize-none"
              />
              <p className="text-xs text-gray-500 mt-1">
                Available placeholders: {`{employeeName}`}, {`{employeeEmail}`}, {`{employeeId}`}, {`{department}`}
              </p>
            </div>
            <div className="flex justify-end gap-2">
              <Button
                variant="outline"
                onClick={() => setShowEmailTemplateDialog(false)}
              >
                Cancel
              </Button>
              <Button
                onClick={saveEmailTemplate}
              >
                Save Template
              </Button>
            </div>
          </div>
        </DialogContent>
      </Dialog>

      {/* Email History Dialog */}
      <EmailHistoryDialog
        open={showEmailHistoryDialog}
        onOpenChange={setShowEmailHistoryDialog}
        tabId={tab.id}
        tabName={tab.name}
      />
    </div>
  );
}