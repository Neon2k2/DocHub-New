import React, { useState } from 'react';
import { Download, Calendar, Users, CheckCircle, Clock, AlertCircle } from 'lucide-react';
import { Button } from '../ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '../ui/table';
import { Badge } from '../ui/badge';
import { Input } from '../ui/input';
import { Label } from '../ui/label';
import { Checkbox } from '../ui/checkbox';
import { Progress } from '../ui/progress';

interface Employee {
  id: string;
  name: string;
  employeeId: string;
  department: string;
  lastDownload: string;
  status: 'success' | 'pending' | 'failed';
}

interface DownloadProgress {
  employee: string;
  progress: number;
  status: 'downloading' | 'completed' | 'failed';
}

export function HCLTimesheet() {
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [selectedEmployees, setSelectedEmployees] = useState<string[]>([]);
  const [isDownloading, setIsDownloading] = useState(false);
  const [downloadProgress, setDownloadProgress] = useState<DownloadProgress[]>([]);

  // Real employee data from API
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchEmployees = async () => {
      try {
        setLoading(true);
        const response = await apiService.getEmployees();
        if (response.success && response.data) {
          // Transform the API response to match our Employee interface
          const employeeData = response.data.items?.map((emp: any) => ({
            id: emp.id,
            name: emp.name,
            employeeId: emp.employeeId,
            department: emp.department,
            lastDownload: emp.lastDownload || 'Never',
            status: emp.status || 'pending'
          })) || [];
          setEmployees(employeeData);
        }
      } catch (error) {
        console.error('Failed to fetch employees:', error);
        notify.error('Failed to load employees');
      } finally {
        setLoading(false);
      }
    };
    
    fetchEmployees();
  }, []);

  const handleEmployeeSelect = (employeeId: string, checked: boolean) => {
    if (checked) {
      setSelectedEmployees([...selectedEmployees, employeeId]);
    } else {
      setSelectedEmployees(selectedEmployees.filter(id => id !== employeeId));
    }
  };

  const handleSelectAll = (checked: boolean) => {
    if (checked) {
      setSelectedEmployees(employees.map(emp => emp.id));
    } else {
      setSelectedEmployees([]);
    }
  };

  const handleDownload = async () => {
    if (selectedEmployees.length === 0) return;
    
    setIsDownloading(true);
    const selectedEmployeeData = employees.filter(emp => selectedEmployees.includes(emp.id));
    
    // Initialize progress
    const initialProgress = selectedEmployeeData.map(emp => ({
      employee: emp.name,
      progress: 0,
      status: 'downloading' as const
    }));
    setDownloadProgress(initialProgress);

    // Simulate download progress
    for (let i = 0; i < selectedEmployeeData.length; i++) {
      const employee = selectedEmployeeData[i];
      
      // Simulate download progress for each employee
      for (let progress = 0; progress <= 100; progress += 20) {
        await new Promise(resolve => setTimeout(resolve, 200));
        setDownloadProgress(prev => 
          prev.map((item, index) => 
            index === i 
              ? { ...item, progress, status: progress === 100 ? 'completed' : 'downloading' }
              : item
          )
        );
      }
    }
    
    setIsDownloading(false);
  };

  const getStatusBadge = (status: string) => {
    switch (status) {
      case 'success':
        return <Badge className="bg-green-500/20 text-green-400 border-green-500/30">Success</Badge>;
      case 'pending':
        return <Badge className="bg-yellow-500/20 text-yellow-400 border-yellow-500/30">Pending</Badge>;
      case 'failed':
        return <Badge className="bg-red-500/20 text-red-400 border-red-500/30">Failed</Badge>;
      default:
        return <Badge variant="secondary">{status}</Badge>;
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">HCL Timesheet Download</h1>
          <p className="text-muted-foreground">Download timesheets for HCL employees</p>
        </div>
      </div>

      {/* Download Configuration */}
      <Card className="glass-panel border-glass-border">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Calendar className="h-5 w-5 text-neon-blue" />
            Download Configuration
          </CardTitle>
          <CardDescription>
            Set date range and select employees for timesheet download
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-6">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="start-date">Start Date</Label>
              <Input
                id="start-date"
                type="date"
                value={startDate}
                onChange={(e) => setStartDate(e.target.value)}
                className="glass-panel border-glass-border"
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="end-date">End Date</Label>
              <Input
                id="end-date"
                type="date"
                value={endDate}
                onChange={(e) => setEndDate(e.target.value)}
                className="glass-panel border-glass-border"
              />
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Employee Selection */}
      <Card className="glass-panel border-glass-border">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Users className="h-5 w-5 text-neon-green" />
            Employee Selection
          </CardTitle>
          <CardDescription>
            Select employees for timesheet download
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="glass-panel rounded-lg border-glass-border overflow-hidden">
            <Table>
              <TableHeader>
                <TableRow className="border-glass-border hover:bg-muted/50">
                  <TableHead className="w-12">
                    <Checkbox
                      checked={selectedEmployees.length === employees.length}
                      onCheckedChange={handleSelectAll}
                    />
                  </TableHead>
                  <TableHead>Name</TableHead>
                  <TableHead>Employee ID</TableHead>
                  <TableHead>Department</TableHead>
                  <TableHead>Last Download</TableHead>
                  <TableHead>Status</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {employees.map((employee) => (
                  <TableRow 
                    key={employee.id} 
                    className="border-glass-border hover:bg-muted/50 transition-colors"
                  >
                    <TableCell>
                      <Checkbox
                        checked={selectedEmployees.includes(employee.id)}
                        onCheckedChange={(checked) => handleEmployeeSelect(employee.id, checked as boolean)}
                      />
                    </TableCell>
                    <TableCell className="font-medium">{employee.name}</TableCell>
                    <TableCell>{employee.employeeId}</TableCell>
                    <TableCell>{employee.department}</TableCell>
                    <TableCell>{employee.lastDownload}</TableCell>
                    <TableCell>{getStatusBadge(employee.status)}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>

          <div className="mt-6 flex justify-between items-center">
            <div className="text-sm text-muted-foreground">
              {selectedEmployees.length} of {employees.length} employees selected
            </div>
            <Button
              onClick={handleDownload}
              disabled={selectedEmployees.length === 0 || isDownloading}
              className="neon-border bg-card text-neon-blue hover:bg-neon-blue hover:text-white transition-all duration-300"
            >
              <Download className="mr-2 h-4 w-4" />
              {isDownloading ? 'Downloading...' : 'Download Timesheets'}
            </Button>
          </div>
        </CardContent>
      </Card>

      {/* Download Progress */}
      {downloadProgress.length > 0 && (
        <Card className="glass-panel border-glass-border">
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Clock className="h-5 w-5 text-neon-purple" />
              Download Progress
            </CardTitle>
            <CardDescription>
              Real-time progress of timesheet downloads
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            {downloadProgress.map((item, index) => (
              <div key={index} className="space-y-2">
                <div className="flex justify-between items-center">
                  <span className="text-sm font-medium">{item.employee}</span>
                  <div className="flex items-center gap-2">
                    {item.status === 'completed' && (
                      <CheckCircle className="h-4 w-4 text-green-400" />
                    )}
                    {item.status === 'downloading' && (
                      <Clock className="h-4 w-4 text-neon-blue animate-pulse" />
                    )}
                    {item.status === 'failed' && (
                      <AlertCircle className="h-4 w-4 text-red-400" />
                    )}
                    <span className="text-sm">{item.progress}%</span>
                  </div>
                </div>
                <Progress value={item.progress} className="h-2" />
              </div>
            ))}
          </CardContent>
        </Card>
      )}

      {/* Recent Downloads */}
      <Card className="glass-panel border-glass-border">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Download className="h-5 w-5 text-neon-pink" />
            Recent Downloads
          </CardTitle>
          <CardDescription>
            Recently downloaded timesheet files
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            {[
              { file: 'HCL_Timesheets_2024-08-28.xlsx', date: '2024-08-28 10:30 AM', size: '2.4 MB' },
              { file: 'HCL_Timesheets_2024-08-27.xlsx', date: '2024-08-27 03:15 PM', size: '2.1 MB' },
              { file: 'HCL_Timesheets_2024-08-26.xlsx', date: '2024-08-26 11:45 AM', size: '2.3 MB' }
            ].map((download, index) => (
              <div key={index} className="flex items-center justify-between p-3 glass-panel rounded-lg border-glass-border">
                <div className="flex items-center gap-3">
                  <div className="w-10 h-10 bg-green-500/20 rounded-lg flex items-center justify-center">
                    <Download className="h-5 w-5 text-green-400" />
                  </div>
                  <div>
                    <p className="font-medium">{download.file}</p>
                    <p className="text-sm text-muted-foreground">{download.date} • {download.size}</p>
                  </div>
                </div>
                <Button variant="ghost" size="sm" className="neon-glow hover:text-neon-blue">
                  <Download className="h-4 w-4" />
                </Button>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>
    </div>
  );
}