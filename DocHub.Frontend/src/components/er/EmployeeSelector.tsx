import React, { useState, useEffect } from 'react';
import { Search, Filter, Users, Check, X, Mail, FileText, User, Edit3, Eye, Save } from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../ui/card';
import { Input } from '../ui/input';
import { Button } from '../ui/button';
import { Badge } from '../ui/badge';
import { Checkbox } from '../ui/checkbox';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../ui/select';
import { Avatar, AvatarFallback } from '../ui/avatar';
import { ScrollArea } from '../ui/scroll-area';
import { Separator } from '../ui/separator';
import { Employee, apiService } from '../../services/api.service';
import { useEmployees } from '../../hooks/useEmployees';
import { ExcelData } from '../../services/excel.service';

interface EmployeeSelectorProps {
  selectedEmployees: Employee[];
  onSelectionChange: (employees: Employee[]) => void;
  onGenerate?: (employees: Employee[]) => void;
  onSendEmail?: (employees: Employee[]) => void;
  showSelectedCount?: boolean;
  tabId?: string;
  excelData?: ExcelData | null;
}

export function EmployeeSelector({ 
  selectedEmployees, 
  onSelectionChange, 
  onGenerate, 
  onSendEmail,
  showSelectedCount = false,
  tabId,
  excelData
}: EmployeeSelectorProps) {
  const [search, setSearch] = useState('');
  const [departmentFilter, setDepartmentFilter] = useState<string>('all');
  const [statusFilter, setStatusFilter] = useState<string>('all');
  const [showFilters, setShowFilters] = useState(false);
  const [isEditMode, setIsEditMode] = useState(false);
  const [editingCell, setEditingCell] = useState<{rowId: string, field: string} | null>(null);
  const [editingValue, setEditingValue] = useState('');
  const [isSaving, setIsSaving] = useState(false);
  const [saveMessage, setSaveMessage] = useState<string | null>(null);

  // Convert Excel data to Employee format
  const convertExcelDataToEmployees = (excelData: ExcelData): Employee[] => {
    if (!excelData || !excelData.data || !Array.isArray(excelData.data)) {
      console.log('No Excel data available for conversion');
      return [];
    }

    console.log('Converting Excel data to employees:', {
      headers: excelData.headers,
      dataLength: excelData.data.length,
      firstRow: excelData.data[0]
    });

    return excelData.data.map((row, index) => {
      // Try to find the correct column names by checking all possible variations
      const findColumnValue = (possibleNames: string[]) => {
        for (const name of possibleNames) {
          if (row[name] !== undefined && row[name] !== null && row[name] !== '') {
            return row[name];
          }
        }
        return '';
      };

      const employee = {
        id: findColumnValue(['EMP ID', 'Employee ID', 'ID', 'EmpId', 'EmployeeId', 'EmployeeId']) || `excel-${index}`,
        name: findColumnValue(['EMP NAME', 'Employee Name', 'Name', 'EmpName', 'EmployeeName', 'Full Name', 'FullName', 'EmployeeName']) || `Employee ${index + 1}`,
        employeeId: findColumnValue(['EMP ID', 'Employee ID', 'ID', 'EmpId', 'EmployeeId', 'EmployeeId']) || `EMP${index + 1}`,
        email: findColumnValue(['Email', 'EMAIL', 'Email Address', 'EmailAddress', 'E-mail', 'E-Mail', 'Email']),
        designation: findColumnValue(['Designation', 'Position', 'Job Title', 'JobTitle', 'Title', 'Role', 'Position']),
        department: findColumnValue(['Department', 'DEPT', 'Division', 'Dept', 'DeptName', 'Department Name', 'Department']),
        status: 'active', // Default status for Excel data
        // Add any other custom fields from Excel
        ...Object.keys(row).reduce((acc, key) => {
          // Include all fields that aren't already mapped
          const mappedFields = ['EMP ID', 'Employee ID', 'ID', 'EmpId', 'EmployeeId', 'EmployeeId', 
                               'EMP NAME', 'Employee Name', 'Name', 'EmpName', 'EmployeeName', 'Full Name', 'FullName', 'EmployeeName',
                               'Email', 'EMAIL', 'Email Address', 'EmailAddress', 'E-mail', 'E-Mail', 'Email',
                               'Designation', 'Position', 'Job Title', 'JobTitle', 'Title', 'Role', 'Position',
                               'Department', 'DEPT', 'Division', 'Dept', 'DeptName', 'Department Name', 'Department'];
          if (!mappedFields.includes(key)) {
            acc[key] = row[key];
          }
          return acc;
        }, {} as any)
      };
      
      console.log(`Employee ${index + 1}:`, employee);
      return employee;
    });
  };

  // Use Excel data if available, otherwise use database data
  const excelEmployees = excelData ? convertExcelDataToEmployees(excelData) : [];
  const shouldUseExcelData = excelData && excelData.data && excelData.data.length > 0;
  
  const { employees: dbEmployees, loading, error, pagination } = useEmployees({
    search,
    department: departmentFilter === 'all' ? undefined : departmentFilter,
    status: statusFilter === 'all' ? undefined : statusFilter,
    limit: 50,
    tabId
  });

  // Filter Excel employees based on search and filters
  const filteredExcelEmployees = excelEmployees.filter(emp => {
    const matchesSearch = !search || 
      emp.name.toLowerCase().includes(search.toLowerCase()) ||
      emp.employeeId.toLowerCase().includes(search.toLowerCase()) ||
      emp.department.toLowerCase().includes(search.toLowerCase()) ||
      emp.designation.toLowerCase().includes(search.toLowerCase());
    
    const matchesDepartment = departmentFilter === 'all' || emp.department === departmentFilter;
    const matchesStatus = statusFilter === 'all' || emp.status === statusFilter;
    
    return matchesSearch && matchesDepartment && matchesStatus;
  });

  const employees = shouldUseExcelData ? filteredExcelEmployees : dbEmployees;
  
  console.log('EmployeeSelector data source:', {
    shouldUseExcelData,
    excelDataExists: !!excelData,
    excelDataLength: excelData?.data?.length || 0,
    excelHeaders: excelData?.headers || [],
    dbEmployeesLength: dbEmployees.length,
    finalEmployeesLength: employees.length,
    firstEmployee: employees[0]
  });

  const departments = Array.from(new Set(employees.map(emp => emp.department))).sort();

  const handleSelectEmployee = (employee: Employee, checked: boolean) => {
    if (checked) {
      onSelectionChange([...selectedEmployees, employee]);
    } else {
      onSelectionChange(selectedEmployees.filter(emp => emp.id !== employee.id));
    }
  };

  const handleSelectAll = () => {
    const visibleEmployeeIds = employees.map(emp => emp.id);
    const alreadySelected = selectedEmployees.filter(emp => visibleEmployeeIds.includes(emp.id));
    
    if (alreadySelected.length === employees.length) {
      // Deselect all visible employees
      onSelectionChange(selectedEmployees.filter(emp => !visibleEmployeeIds.includes(emp.id)));
    } else {
      // Select all visible employees
      const newSelections = employees.filter(emp => !selectedEmployees.some(sel => sel.id === emp.id));
      onSelectionChange([...selectedEmployees, ...newSelections]);
    }
  };

  const handleClearSelection = () => {
    onSelectionChange([]);
  };

  const employeesWithEmails = selectedEmployees.filter(emp => emp.email);
  const employeesWithoutEmails = selectedEmployees.filter(emp => !emp.email);

  const isEmployeeSelected = (employeeId: string) => {
    return selectedEmployees.some(emp => emp.id === employeeId);
  };

  const getEmployeeInitials = (name: string) => {
    if (!name || typeof name !== 'string') {
      return '??';
    }
    return name
      .split(' ')
      .map(part => part[0])
      .join('')
      .toUpperCase()
      .slice(0, 2);
  };

  const handleCellClick = (rowId: string, field: string, currentValue: string) => {
    if (!isEditMode) return;
    
    setEditingCell({ rowId, field });
    setEditingValue(currentValue || '');
  };

  const handleCellKeyDown = (e: React.KeyboardEvent, rowId: string, field: string) => {
    if (e.key === 'Enter') {
      e.preventDefault();
      handleSaveCell(rowId, field);
    } else if (e.key === 'Escape') {
      setEditingCell(null);
      setEditingValue('');
    }
  };

  const handleSaveCell = async (rowId: string, field: string) => {
    if (!tabId || editingValue === undefined) return;
    
    setIsSaving(true);
    try {
      console.log('Saving cell:', { rowId, field, value: editingValue, tabId });
      
      // Call API to update employee data
      const response = await apiService.updateEmployeeData(tabId, rowId, field, editingValue);
      
      if (response.success || response.Success) {
        console.log('Cell saved successfully');
        setEditingCell(null);
        setEditingValue('');
        setSaveMessage('Cell saved successfully!');
        setTimeout(() => setSaveMessage(null), 3000);
        
        // TODO: Update local state or refresh data
        // For now, the change will be reflected on next page refresh
      } else {
        console.error('Failed to save cell:', response.error || response.Error);
        setSaveMessage('Failed to save cell. Please try again.');
        setTimeout(() => setSaveMessage(null), 3000);
      }
    } catch (error) {
      console.error('Error saving cell:', error);
      setSaveMessage('Error saving cell. Please try again.');
      setTimeout(() => setSaveMessage(null), 3000);
    } finally {
      setIsSaving(false);
    }
  };

  const getCellValue = (employee: Employee, field: string) => {
    switch (field) {
      case 'name':
        return employee.name;
      case 'employeeId':
        return employee.employeeId;
      case 'designation':
        return employee.designation;
      case 'department':
        return employee.department;
      case 'email':
        return employee.email;
      case 'status':
        return employee.status;
      default:
        return (employee as any)[field] || '';
    }
  };

  return (
    <div className="space-y-6">
      {/* Selection Summary & Actions */}
      {selectedEmployees.length > 0 && (
        <Card className="glass-panel border-glass-border">
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-4">
                <div className="flex items-center gap-2">
                  <Users className="h-5 w-5 text-neon-blue" />
                  <span className="font-semibold">
                    {selectedEmployees.length} Employee{selectedEmployees.length !== 1 ? 's' : ''} Selected
                  </span>
                </div>
                
                {employeesWithoutEmails.length > 0 && (
                  <Badge variant="outline" className="text-orange-400 border-orange-500/30">
                    {employeesWithoutEmails.length} without email
                  </Badge>
                )}
              </div>

              <div className="flex items-center gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={handleClearSelection}
                >
                  <X className="h-4 w-4 mr-1" />
                  Clear
                </Button>
                
                {onGenerate && (
                  <Button
                    onClick={() => onGenerate(selectedEmployees)}
                    className="border border-blue-500 bg-blue-50 text-blue-700 hover:bg-blue-100 dark:bg-blue-950 dark:text-blue-300 dark:hover:bg-blue-900"
                  >
                    <FileText className="h-4 w-4 mr-2" />
                    Generate ({selectedEmployees.length})
                  </Button>
                )}
                
                {onSendEmail && (
                  <Button
                    onClick={() => onSendEmail(employeesWithEmails)}
                    disabled={employeesWithEmails.length === 0}
                    className="border border-green-500 bg-green-50 text-green-700 hover:bg-green-100 dark:bg-green-950 dark:text-green-300 dark:hover:bg-green-900 disabled:opacity-50"
                  >
                    <Mail className="h-4 w-4 mr-2" />
                    Send Email ({employeesWithEmails.length})
                  </Button>
                )}
              </div>
            </div>

            {/* Selected employees preview */}
            <div className="mt-4 flex flex-wrap gap-2">
              {selectedEmployees.slice(0, 10).map(employee => (
                <div
                  key={employee.id}
                  className="flex items-center gap-2 glass-panel rounded-full px-3 py-1 border-glass-border"
                >
                  <Avatar className="w-6 h-6">
                    <AvatarFallback className="text-xs">
                      {getEmployeeInitials(employee.name)}
                    </AvatarFallback>
                  </Avatar>
                  <span className="text-sm">{employee.name}</span>
                  <Button
                    variant="ghost"
                    size="sm"
                    className="h-4 w-4 p-0 hover:bg-red-500/20"
                    onClick={() => handleSelectEmployee(employee, false)}
                  >
                    <X className="h-3 w-3" />
                  </Button>
                </div>
              ))}
              {selectedEmployees.length > 10 && (
                <Badge variant="outline">+{selectedEmployees.length - 10} more</Badge>
              )}
            </div>
          </CardContent>
        </Card>
      )}

      {/* Search and Filters */}
      <Card className="glass-panel border-glass-border">
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
               <CardTitle className="flex items-center gap-2 text-gray-900 dark:text-white">
                 <Users className="h-5 w-5 text-neon-blue" />
                 Select Employees
               </CardTitle>
               <CardDescription className="text-gray-600 dark:text-gray-400">
                 Choose employees to generate letters for
               </CardDescription>
            </div>
            <div className="flex items-center gap-2">
              <Button
                variant={isEditMode ? "default" : "outline"}
                size="sm"
                onClick={() => setIsEditMode(!isEditMode)}
                className={isEditMode 
                  ? "bg-blue-600 hover:bg-blue-700 text-white border-blue-600 shadow-md" 
                  : "border-2 border-blue-500 text-blue-600 hover:bg-blue-50 hover:text-blue-700 dark:border-blue-400 dark:text-blue-400 dark:hover:bg-blue-950 dark:hover:text-blue-300"
                }
              >
                {isEditMode ? (
                  <>
                    <Eye className="h-4 w-4 mr-1" />
                    View Mode
                  </>
                ) : (
                  <>
                    <Edit3 className="h-4 w-4 mr-1" />
                    Edit Mode
                  </>
                )}
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={() => setShowFilters(!showFilters)}
                className="border-2 border-gray-300 text-gray-700 hover:bg-gray-100 hover:text-gray-900 dark:border-gray-600 dark:text-gray-300 dark:hover:bg-gray-700 dark:hover:text-white"
              >
                <Filter className="h-4 w-4 mr-1" />
                Filters
              </Button>
            </div>
          </div>
        </CardHeader>
        
        <CardContent className="space-y-4">
          {/* Search */}
          <div className="relative">
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
            <Input
              placeholder="Search employees by name, ID, or department..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="pl-10"
            />
          </div>

          {/* Filters */}
          {showFilters && (
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4 p-4 glass-panel rounded-lg border-glass-border">
              <Select value={departmentFilter} onValueChange={setDepartmentFilter}>
                <SelectTrigger>
                  <SelectValue placeholder="All Departments" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All Departments</SelectItem>
                  {departments.map(dept => (
                    <SelectItem key={dept} value={dept}>{dept}</SelectItem>
                  ))}
                </SelectContent>
              </Select>

              <Select value={statusFilter} onValueChange={setStatusFilter}>
                <SelectTrigger>
                  <SelectValue placeholder="All Status" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All Status</SelectItem>
                  <SelectItem value="active">Active</SelectItem>
                  <SelectItem value="inactive">Inactive</SelectItem>
                </SelectContent>
              </Select>

              <Button
                variant="outline"
                onClick={() => {
                  setDepartmentFilter('');
                  setStatusFilter('');
                  setSearch('');
                }}
              >
                Clear Filters
              </Button>
            </div>
          )}

          {/* Employee Count */}
          <div className="flex items-center justify-between py-2">
             <span className="text-sm text-gray-600 dark:text-gray-400">
               {employees.length} employee{employees.length !== 1 ? 's' : ''} found
             </span>
            <div className="flex items-center gap-4">
              {isSaving && (
                <div className="flex items-center gap-2 text-sm text-blue-400">
                  <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-blue-400"></div>
                  <span>Saving...</span>
                </div>
              )}
              {saveMessage && (
                <div className={`text-sm px-3 py-1 rounded ${
                  saveMessage.includes('successfully') 
                    ? 'bg-green-100 text-green-700' 
                    : 'bg-red-100 text-red-700'
                }`}>
                  {saveMessage}
                </div>
              )}
            </div>
          </div>

          <Separator />

          {/* Employee Table */}
          <div className={`border rounded-lg overflow-hidden ${isEditMode ? 'ring-2 ring-blue-500/50' : ''}`}>
            {isEditMode && (
              <div className="bg-blue-50 border-b border-blue-200 px-4 py-2 text-sm text-blue-700 flex items-center gap-2">
                <Edit3 className="h-4 w-4" />
                <span>Edit Mode: Click on any cell to edit. Press Enter to save, Escape to cancel.</span>
              </div>
            )}
            <div className="overflow-x-auto max-h-96 overflow-y-auto">
              {loading ? (
                <div className="p-8 text-center">
                  <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-neon-blue mx-auto mb-4"></div>
                   <p className="text-gray-600 dark:text-gray-400">Loading employees...</p>
                </div>
              ) : error ? (
                <div className="text-center py-8 text-red-400">
                  {error}
                </div>
              ) : employees.length === 0 ? (
                 <div className="text-center py-8 text-gray-600 dark:text-gray-400">
                   <Users className="h-12 w-12 mx-auto mb-4 opacity-50" />
                   <p>No employees found matching your criteria</p>
                 </div>
              ) : (
                <table className="w-full text-sm">
                  <thead className="bg-muted sticky top-0">
                    <tr>
                       <th className="px-3 py-2 text-left font-medium border-b text-gray-900 dark:text-white">
                         <Checkbox
                           checked={employees.length > 0 && employees.every(emp => isEmployeeSelected(emp.id))}
                           onCheckedChange={handleSelectAll}
                         />
                       </th>
                       <th className="px-3 py-2 text-left font-medium border-b text-gray-900 dark:text-white">Employee</th>
                       <th className="px-3 py-2 text-left font-medium border-b text-gray-900 dark:text-white">ID</th>
                       <th className="px-3 py-2 text-left font-medium border-b text-gray-900 dark:text-white">Designation</th>
                       <th className="px-3 py-2 text-left font-medium border-b text-gray-900 dark:text-white">Department</th>
                       <th className="px-3 py-2 text-left font-medium border-b text-gray-900 dark:text-white">Email</th>
                      {shouldUseExcelData && excelData?.headers && excelData.headers
                        .filter(header => {
                          const mappedFields = ['EMP ID', 'Employee ID', 'ID', 'EmpId', 'EmployeeId', 
                                               'EMP NAME', 'Employee Name', 'Name', 'EmpName', 'EmployeeName', 'Full Name', 'FullName',
                                               'Email', 'EMAIL', 'Email Address', 'EmailAddress', 'E-mail', 'E-Mail',
                                               'Designation', 'Position', 'Job Title', 'JobTitle', 'Title', 'Role',
                                               'Department', 'DEPT', 'Division', 'Dept', 'DeptName', 'Department Name'];
                          return !mappedFields.includes(header);
                        })
                        .map(header => (
                           <th key={header} className="px-3 py-2 text-left font-medium border-b text-gray-900 dark:text-white">
                             {header}
                           </th>
                        ))
                      }
                      <th className="px-3 py-2 text-left font-medium border-b text-gray-900 dark:text-white">Status</th>
                    </tr>
                  </thead>
                  <tbody>
                    {employees.map(employee => (
                      <tr 
                        key={employee.id}
                        className={`hover:bg-muted/50 cursor-pointer transition-colors ${
                          isEmployeeSelected(employee.id) ? 'bg-primary/10' : ''
                        }`}
                        onClick={() => handleSelectEmployee(employee, !isEmployeeSelected(employee.id))}
                      >
                        <td className="px-3 py-2 border-b">
                          <Checkbox
                            checked={isEmployeeSelected(employee.id)}
                            onCheckedChange={(checked) => handleSelectEmployee(employee, !!checked)}
                            onClick={(e) => e.stopPropagation()}
                          />
                        </td>
                        <td className="px-3 py-2 border-b">
                          <div className="flex items-center gap-2">
                            <Avatar className="w-8 h-8">
                              <AvatarFallback className="text-xs">
                                {getEmployeeInitials(employee.name)}
                              </AvatarFallback>
                            </Avatar>
                            {editingCell?.rowId === employee.id && editingCell?.field === 'name' ? (
                              <Input
                                value={editingValue}
                                onChange={(e) => setEditingValue(e.target.value)}
                                onKeyDown={(e) => handleCellKeyDown(e, employee.id, 'name')}
                                onBlur={() => handleSaveCell(employee.id, 'name')}
                                className="h-8 text-sm"
                                autoFocus
                              />
                            ) : (
                               <span 
                                 className={`font-medium text-gray-900 dark:text-white ${isEditMode ? 'cursor-pointer hover:bg-gray-100 dark:hover:bg-gray-700 px-2 py-1 rounded' : ''}`}
                                 onClick={() => handleCellClick(employee.id, 'name', employee.name)}
                               >
                                 {employee.name}
                               </span>
                            )}
                          </div>
                        </td>
                        <td className="px-3 py-2 border-b">
                          {editingCell?.rowId === employee.id && editingCell?.field === 'employeeId' ? (
                            <Input
                              value={editingValue}
                              onChange={(e) => setEditingValue(e.target.value)}
                              onKeyDown={(e) => handleCellKeyDown(e, employee.id, 'employeeId')}
                              onBlur={() => handleSaveCell(employee.id, 'employeeId')}
                              className="h-8 text-sm"
                              autoFocus
                            />
                          ) : (
                            <Badge 
                              variant="outline" 
                              className={`text-xs ${isEditMode ? 'cursor-pointer hover:bg-muted' : ''}`}
                              onClick={() => handleCellClick(employee.id, 'employeeId', employee.employeeId)}
                            >
                              {employee.employeeId}
                            </Badge>
                          )}
                        </td>
                        <td className="px-3 py-2 border-b">
                          {editingCell?.rowId === employee.id && editingCell?.field === 'designation' ? (
                            <Input
                              value={editingValue}
                              onChange={(e) => setEditingValue(e.target.value)}
                              onKeyDown={(e) => handleCellKeyDown(e, employee.id, 'designation')}
                              onBlur={() => handleSaveCell(employee.id, 'designation')}
                              className="h-8 text-sm"
                              autoFocus
                            />
                          ) : (
                             <span 
                               className={`text-gray-900 dark:text-white ${isEditMode ? 'cursor-pointer hover:bg-gray-100 dark:hover:bg-gray-700 px-2 py-1 rounded' : ''}`}
                               onClick={() => handleCellClick(employee.id, 'designation', employee.designation)}
                             >
                               {employee.designation}
                             </span>
                          )}
                        </td>
                        <td className="px-3 py-2 border-b">
                          {editingCell?.rowId === employee.id && editingCell?.field === 'department' ? (
                            <Input
                              value={editingValue}
                              onChange={(e) => setEditingValue(e.target.value)}
                              onKeyDown={(e) => handleCellKeyDown(e, employee.id, 'department')}
                              onBlur={() => handleSaveCell(employee.id, 'department')}
                              className="h-8 text-sm"
                              autoFocus
                            />
                          ) : (
                             <span 
                               className={`text-gray-900 dark:text-white ${isEditMode ? 'cursor-pointer hover:bg-gray-100 dark:hover:bg-gray-700 px-2 py-1 rounded' : ''}`}
                               onClick={() => handleCellClick(employee.id, 'department', employee.department)}
                             >
                               {employee.department}
                             </span>
                          )}
                        </td>
                        <td className="px-3 py-2 border-b">
                          {editingCell?.rowId === employee.id && editingCell?.field === 'email' ? (
                            <Input
                              value={editingValue}
                              onChange={(e) => setEditingValue(e.target.value)}
                              onKeyDown={(e) => handleCellKeyDown(e, employee.id, 'email')}
                              onBlur={() => handleSaveCell(employee.id, 'email')}
                              className="h-8 text-sm"
                              autoFocus
                            />
                          ) : employee.email ? (
                            <div className="flex items-center gap-1">
                              <Mail className="h-3 w-3 text-gray-500 dark:text-gray-400" />
                               <span 
                                 className={`text-xs text-gray-900 dark:text-white ${isEditMode ? 'cursor-pointer hover:bg-gray-100 dark:hover:bg-gray-700 px-2 py-1 rounded' : ''}`}
                                 onClick={() => handleCellClick(employee.id, 'email', employee.email)}
                               >
                                 {employee.email}
                               </span>
                            </div>
                          ) : (
                            <Badge 
                              variant="outline" 
                              className={`text-orange-400 border-orange-500/30 text-xs ${isEditMode ? 'cursor-pointer hover:bg-muted' : ''}`}
                              onClick={() => handleCellClick(employee.id, 'email', '')}
                            >
                              No Email
                            </Badge>
                          )}
                        </td>
                        {shouldUseExcelData && excelData?.headers && excelData.headers
                          .filter(header => {
                            const mappedFields = ['EMP ID', 'Employee ID', 'ID', 'EmpId', 'EmployeeId', 
                                                 'EMP NAME', 'Employee Name', 'Name', 'EmpName', 'EmployeeName', 'Full Name', 'FullName',
                                                 'Email', 'EMAIL', 'Email Address', 'EmailAddress', 'E-mail', 'E-Mail',
                                                 'Designation', 'Position', 'Job Title', 'JobTitle', 'Title', 'Role',
                                                 'Department', 'DEPT', 'Division', 'Dept', 'DeptName', 'Department Name'];
                            return !mappedFields.includes(header);
                          })
                          .map(header => (
                             <td key={header} className="px-3 py-2 border-b text-gray-900 dark:text-white">
                               {employee[header] || ''}
                             </td>
                          ))
                        }
                        <td className="px-3 py-2 border-b">
                          {editingCell?.rowId === employee.id && editingCell?.field === 'status' ? (
                            <Select
                              value={editingValue}
                              onValueChange={(value) => setEditingValue(value)}
                              onOpenChange={(open) => {
                                if (!open) {
                                  handleSaveCell(employee.id, 'status');
                                }
                              }}
                            >
                              <SelectTrigger className="h-8 text-sm">
                                <SelectValue />
                              </SelectTrigger>
                              <SelectContent>
                                <SelectItem value="active">Active</SelectItem>
                                <SelectItem value="inactive">Inactive</SelectItem>
                              </SelectContent>
                            </Select>
                          ) : (
                            <Badge 
                              variant="outline"
                              className={`${employee.status === 'active' 
                                ? 'text-green-400 border-green-500/30' 
                                : 'text-red-400 border-red-500/30'
                              } ${isEditMode ? 'cursor-pointer hover:bg-muted' : ''}`}
                              onClick={() => handleCellClick(employee.id, 'status', employee.status)}
                            >
                              {employee.status}
                            </Badge>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}