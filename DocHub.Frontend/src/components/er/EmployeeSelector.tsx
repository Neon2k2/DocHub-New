import React, { useState, useEffect } from 'react';
import { Search, Filter, Users, Check, X, Mail, FileText, User } from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../ui/card';
import { Input } from '../ui/input';
import { Button } from '../ui/button';
import { Badge } from '../ui/badge';
import { Checkbox } from '../ui/checkbox';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../ui/select';
import { Avatar, AvatarFallback } from '../ui/avatar';
import { ScrollArea } from '../ui/scroll-area';
import { Separator } from '../ui/separator';
import { Employee } from '../../services/api.service';
import { useEmployees } from '../../hooks/useEmployees';

interface EmployeeSelectorProps {
  selectedEmployees: Employee[];
  onSelectionChange: (employees: Employee[]) => void;
  onGenerate?: (employees: Employee[]) => void;
  onSendEmail?: (employees: Employee[]) => void;
  showSelectedCount?: boolean;
}

export function EmployeeSelector({ 
  selectedEmployees, 
  onSelectionChange, 
  onGenerate, 
  onSendEmail,
  showSelectedCount = false
}: EmployeeSelectorProps) {
  const [search, setSearch] = useState('');
  const [departmentFilter, setDepartmentFilter] = useState<string>('all');
  const [statusFilter, setStatusFilter] = useState<string>('all');
  const [showFilters, setShowFilters] = useState(false);

  const { employees, loading, error, pagination } = useEmployees({
    search,
    department: departmentFilter === 'all' ? undefined : departmentFilter,
    status: statusFilter === 'all' ? undefined : statusFilter,
    limit: 50
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
    return name
      .split(' ')
      .map(part => part[0])
      .join('')
      .toUpperCase()
      .slice(0, 2);
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
                    className="neon-border bg-card text-neon-blue hover:bg-neon-blue hover:text-white"
                  >
                    <FileText className="h-4 w-4 mr-2" />
                    Generate ({selectedEmployees.length})
                  </Button>
                )}
                
                {onSendEmail && (
                  <Button
                    onClick={() => onSendEmail(employeesWithEmails)}
                    disabled={employeesWithEmails.length === 0}
                    className="neon-border bg-card text-neon-green hover:bg-neon-green hover:text-white disabled:opacity-50"
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
              <CardTitle className="flex items-center gap-2">
                <Users className="h-5 w-5 text-neon-blue" />
                Select Employees
              </CardTitle>
              <CardDescription>
                Choose employees to generate letters for
              </CardDescription>
            </div>
            <Button
              variant="outline"
              size="sm"
              onClick={() => setShowFilters(!showFilters)}
            >
              <Filter className="h-4 w-4 mr-1" />
              Filters
            </Button>
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

          {/* Bulk Selection Controls */}
          <div className="flex items-center justify-between py-2">
            <div className="flex items-center gap-4">
              <Button
                variant="outline"
                size="sm"
                onClick={handleSelectAll}
                disabled={employees.length === 0}
              >
                {employees.every(emp => isEmployeeSelected(emp.id)) ? (
                  <>
                    <X className="h-4 w-4 mr-1" />
                    Deselect All
                  </>
                ) : (
                  <>
                    <Check className="h-4 w-4 mr-1" />
                    Select All
                  </>
                )}
              </Button>
              
              <span className="text-sm text-muted-foreground">
                {employees.length} employee{employees.length !== 1 ? 's' : ''} found
              </span>
            </div>
          </div>

          <Separator />

          {/* Employee List */}
          <ScrollArea className="h-96">
            {loading ? (
              <div className="space-y-3">
                {Array.from({ length: 5 }).map((_, i) => (
                  <div key={i} className="flex items-center gap-3 p-3 glass-panel rounded-lg animate-pulse">
                    <div className="w-10 h-10 bg-muted rounded-full" />
                    <div className="flex-1 space-y-2">
                      <div className="h-4 bg-muted rounded w-1/4" />
                      <div className="h-3 bg-muted rounded w-1/3" />
                    </div>
                  </div>
                ))}
              </div>
            ) : error ? (
              <div className="text-center py-8 text-red-400">
                {error}
              </div>
            ) : employees.length === 0 ? (
              <div className="text-center py-8 text-muted-foreground">
                <Users className="h-12 w-12 mx-auto mb-4 opacity-50" />
                <p>No employees found matching your criteria</p>
              </div>
            ) : (
              <div className="space-y-2">
                {employees.map(employee => (
                  <div
                    key={employee.id}
                    className={`flex items-center gap-3 p-3 glass-panel rounded-lg border-glass-border cursor-pointer hover:bg-muted/20 transition-colors ${
                      isEmployeeSelected(employee.id) ? 'ring-2 ring-neon-blue' : ''
                    }`}
                    onClick={() => handleSelectEmployee(employee, !isEmployeeSelected(employee.id))}
                  >
                    <Checkbox
                      checked={isEmployeeSelected(employee.id)}
                      onCheckedChange={(checked) => handleSelectEmployee(employee, !!checked)}
                      onClick={(e) => e.stopPropagation()}
                    />
                    
                    <Avatar className="w-10 h-10">
                      <AvatarFallback>
                        {getEmployeeInitials(employee.name)}
                      </AvatarFallback>
                    </Avatar>

                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2">
                        <p className="font-medium truncate">{employee.name}</p>
                        <Badge variant="outline" className="text-xs">
                          {employee.employeeId}
                        </Badge>
                        {!employee.email && (
                          <Badge variant="outline" className="text-orange-400 border-orange-500/30 text-xs">
                            No Email
                          </Badge>
                        )}
                      </div>
                      <div className="flex items-center gap-4 text-sm text-muted-foreground">
                        <span>{employee.designation}</span>
                        <span>•</span>
                        <span>{employee.department}</span>
                        {employee.email && (
                          <>
                            <span>•</span>
                            <span className="flex items-center gap-1">
                              <Mail className="h-3 w-3" />
                              {employee.email}
                            </span>
                          </>
                        )}
                      </div>
                    </div>

                    <Badge 
                      variant="outline"
                      className={employee.status === 'active' 
                        ? 'text-green-400 border-green-500/30' 
                        : 'text-red-400 border-red-500/30'
                      }
                    >
                      {employee.status}
                    </Badge>
                  </div>
                ))}
              </div>
            )}
          </ScrollArea>
        </CardContent>
      </Card>
    </div>
  );
}