// import React, { useState } from 'react';
// import { Upload, Merge, Download, FileSpreadsheet, Filter, Eye, Trash2 } from 'lucide-react';
// import { Button } from '../ui/button';
// import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../ui/card';
// import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '../ui/table';
// import { Input } from '../ui/input';
// import { Label } from '../ui/label';
// import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../ui/select';
// import { Badge } from '../ui/badge';
// import { Progress } from '../ui/progress';

// interface UploadedFile {
//   id: string;
//   name: string;
//   size: string;
//   uploadTime: string;
//   status: 'uploaded' | 'processing' | 'merged' | 'error';
//   records: number;
// }

// interface MergedData {
//   id: string;
//   employeeId: string;
//   employeeName: string;
//   department: string;
//   hours: number;
//   date: string;
//   project: string;
// }

// export function HCLSheetMerger() {
//   const [uploadedFiles, setUploadedFiles] = useState<UploadedFile[]>([
//     {
//       id: '1',
//       name: 'HCL_August_Week1.xlsx',
//       size: '2.4 MB',
//       uploadTime: '2024-08-29 10:30 AM',
//       status: 'merged',
//       records: 156
//     },
//     {
//       id: '2',
//       name: 'HCL_August_Week2.xlsx',
//       size: '2.1 MB',
//       uploadTime: '2024-08-29 10:32 AM',
//       status: 'merged',
//       records: 142
//     },
//     {
//       id: '3',
//       name: 'HCL_August_Week3.xlsx',
//       size: '2.3 MB',
//       uploadTime: '2024-08-29 10:35 AM',
//       status: 'uploaded',
//       records: 149
//     }
//   ]);

//   const [mergedData, setMergedData] = useState<MergedData[]>([
//     {
//       id: '1',
//       employeeId: 'HCL001',
//       employeeName: 'John Doe',
//       department: 'Engineering',
//       hours: 40,
//       date: '2024-08-26',
//       project: 'Project Alpha'
//     },
//     {
//       id: '2',
//       employeeId: 'HCL002',
//       employeeName: 'Jane Smith',
//       department: 'Marketing',
//       hours: 35,
//       date: '2024-08-26',
//       project: 'Project Beta'
//     },
//     {
//       id: '3',
//       employeeId: 'HCL003',
//       employeeName: 'Mike Johnson',
//       department: 'Sales',
//       hours: 42,
//       date: '2024-08-26',
//       project: 'Project Gamma'
//     }
//   ]);

//   const [isMerging, setIsMerging] = useState(false);
//   const [mergeProgress, setMergeProgress] = useState(0);
//   const [filterDepartment, setFilterDepartment] = useState('all');
//   const [filterDate, setFilterDate] = useState('');

//   const handleFileUpload = (event: React.ChangeEvent<HTMLInputElement>) => {
//     const files = event.target.files;
//     if (files) {
//       Array.from(files).forEach((file) => {
//         const newFile: UploadedFile = {
//           id: Date.now().toString() + Math.random().toString(),
//           name: file.name,
//           size: `${(file.size / (1024 * 1024)).toFixed(1)} MB`,
//           uploadTime: new Date().toLocaleString(),
//           status: 'uploaded',
//           records: Math.floor(Math.random() * 200) + 100
//         };
//         setUploadedFiles(prev => [...prev, newFile]);
//       });
//     }
//   };

//   const handleMergeFiles = async () => {
//     const filesToMerge = uploadedFiles.filter(file => file.status === 'uploaded');
//     if (filesToMerge.length === 0) return;

//     setIsMerging(true);
//     setMergeProgress(0);

//     // Simulate merging progress
//     for (let i = 0; i <= 100; i += 10) {
//       await new Promise(resolve => setTimeout(resolve, 300));
//       setMergeProgress(i);
//     }

//     // Update file statuses
//     setUploadedFiles(prev =>
//       prev.map(file =>
//         file.status === 'uploaded' ? { ...file, status: 'merged' } : file
//       )
//     );

//     setIsMerging(false);
//     setMergeProgress(0);
//   };

//   const getStatusBadge = (status: string) => {
//     switch (status) {
//       case 'uploaded':
//         return <Badge className="bg-blue-500/20 text-blue-400 border-blue-500/30">Uploaded</Badge>;
//       case 'processing':
//         return <Badge className="bg-yellow-500/20 text-yellow-400 border-yellow-500/30">Processing</Badge>;
//       case 'merged':
//         return <Badge className="bg-green-500/20 text-green-400 border-green-500/30">Merged</Badge>;
//       case 'error':
//         return <Badge className="bg-red-500/20 text-red-400 border-red-500/30">Error</Badge>;
//       default:
//         return <Badge variant="secondary">{status}</Badge>;
//     }
//   };

//   const filteredData = mergedData.filter(record => {
//     const matchesDepartment = filterDepartment === 'all' || record.department === filterDepartment;
//     const matchesDate = !filterDate || record.date.includes(filterDate);
//     return matchesDepartment && matchesDate;
//   });

//   const departments = [...new Set(mergedData.map(record => record.department))];

//   return (
//     <div className="space-y-6">
//       <div className="flex items-center justify-between">
//         <div>
//           <h1 className="text-2xl font-bold">HCL Sheet Merger</h1>
//           <p className="text-muted-foreground">Merge multiple timesheet files into a consolidated dataset</p>
//         </div>
//       </div>

//       {/* File Upload */}
//       <Card className="glass-panel border-glass-border">
//         <CardHeader>
//           <CardTitle className="flex items-center gap-2">
//             <Upload className="h-5 w-5 text-blue-600" />
//             Upload Files
//           </CardTitle>
//           <CardDescription>
//             Upload multiple Excel files to merge timesheet data
//           </CardDescription>
//         </CardHeader>
//         <CardContent>
//           <div className="border-2 border-dashed border-glass-border rounded-lg p-8 text-center glass-panel">
//             <input
//               type="file"
//               accept=".xlsx,.xls,.csv"
//               multiple
//               onChange={handleFileUpload}
//               className="hidden"
//               id="file-upload"
//             />
//             <label htmlFor="file-upload" className="cursor-pointer">
//               <div className="space-y-4">
//                 <div className="mx-auto w-16 h-16 bg-blue-100 dark:bg-blue-900/20 rounded-full flex items-center justify-center">
//                   <Upload className="h-8 w-8 text-blue-600" />
//                 </div>
//                 <div>
//                   <p className="text-lg font-medium">Drop Excel files here or click to browse</p>
//                   <p className="text-sm text-muted-foreground">Support for multiple file selection</p>
//                 </div>
//               </div>
//             </label>
//           </div>
//         </CardContent>
//       </Card>

//       {/* Uploaded Files */}
//       <Card className="glass-panel border-glass-border">
//         <CardHeader>
//           <CardTitle className="flex items-center gap-2">
//             <FileSpreadsheet className="h-5 w-5 text-green-600" />
//             Uploaded Files
//           </CardTitle>
//           <CardDescription>
//             Manage uploaded timesheet files before merging
//           </CardDescription>
//         </CardHeader>
//         <CardContent>
//           <div className="glass-panel rounded-lg border-glass-border overflow-hidden">
//             <Table>
//               <TableHeader>
//                 <TableRow className="border-glass-border hover:bg-muted/50">
//                   <TableHead>File Name</TableHead>
//                   <TableHead>Size</TableHead>
//                   <TableHead>Records</TableHead>
//                   <TableHead>Upload Time</TableHead>
//                   <TableHead>Status</TableHead>
//                   <TableHead>Actions</TableHead>
//                 </TableRow>
//               </TableHeader>
//               <TableBody>
//                 {uploadedFiles.map((file) => (
//                   <TableRow 
//                     key={file.id} 
//                     className="border-glass-border hover:bg-muted/50 transition-colors"
//                   >
//                     <TableCell className="font-medium">{file.name}</TableCell>
//                     <TableCell>{file.size}</TableCell>
//                     <TableCell>{file.records}</TableCell>
//                     <TableCell>{file.uploadTime}</TableCell>
//                     <TableCell>{getStatusBadge(file.status)}</TableCell>
//                     <TableCell>
//                       <div className="flex items-center gap-2">
//                         <Button
//                           variant="ghost"
//                           size="sm"
//                           className="hover:text-blue-600"
//                         >
//                           <Eye className="h-4 w-4" />
//                         </Button>
//                         <Button
//                           variant="ghost"
//                           size="sm"
//                           className="hover:text-red-400"
//                         >
//                           <Trash2 className="h-4 w-4" />
//                         </Button>
//                       </div>
//                     </TableCell>
//                   </TableRow>
//                 ))}
//               </TableBody>
//             </Table>
//           </div>

//           <div className="mt-6 flex justify-between items-center">
//             <div className="text-sm text-muted-foreground">
//               {uploadedFiles.filter(f => f.status === 'uploaded').length} files ready to merge
//             </div>
//             <Button
//               onClick={handleMergeFiles}
//               disabled={uploadedFiles.filter(f => f.status === 'uploaded').length === 0 || isMerging}
//               className="border border-blue-500 bg-blue-50 text-blue-700 hover:bg-blue-100 dark:bg-blue-950 dark:text-blue-300 dark:hover:bg-blue-900 transition-all duration-300"
//             >
//               <Merge className="mr-2 h-4 w-4" />
//               {isMerging ? 'Merging...' : 'Merge Files'}
//             </Button>
//           </div>

//           {isMerging && (
//             <div className="mt-4 space-y-2">
//               <div className="flex justify-between items-center">
//                 <span className="text-sm">Merging files...</span>
//                 <span className="text-sm">{mergeProgress}%</span>
//               </div>
//               <Progress value={mergeProgress} className="h-2" />
//             </div>
//           )}
//         </CardContent>
//       </Card>

//       {/* Merged Data Preview */}
//       <Card className="glass-panel border-glass-border">
//         <CardHeader>
//           <CardTitle className="flex items-center gap-2">
//             <FileSpreadsheet className="h-5 w-5 text-purple-600" />
//             Merged Data Preview
//           </CardTitle>
//           <CardDescription>
//             Preview and filter the consolidated timesheet data
//           </CardDescription>
//         </CardHeader>
//         <CardContent>
//           {/* Filters */}
//           <div className="flex flex-col md:flex-row gap-4 mb-6">
//             <div className="space-y-2">
//               <Label>Department</Label>
//               <Select value={filterDepartment} onValueChange={setFilterDepartment}>
//                 <SelectTrigger className="w-full md:w-[200px] glass-panel border-glass-border">
//                   <SelectValue placeholder="Filter by department" />
//                 </SelectTrigger>
//                 <SelectContent className="glass-panel border-glass-border">
//                   <SelectItem value="all">All Departments</SelectItem>
//                   {departments.map((dept) => (
//                     <SelectItem key={dept} value={dept}>{dept}</SelectItem>
//                   ))}
//                 </SelectContent>
//               </Select>
//             </div>
            
//             <div className="space-y-2">
//               <Label>Date</Label>
//               <Input
//                 type="date"
//                 value={filterDate}
//                 onChange={(e) => setFilterDate(e.target.value)}
//                 className="w-full md:w-[200px] glass-panel border-glass-border"
//               />
//             </div>
//           </div>

//           {/* Data Table */}
//           <div className="glass-panel rounded-lg border-glass-border overflow-hidden">
//             <Table>
//               <TableHeader>
//                 <TableRow className="border-glass-border hover:bg-muted/50">
//                   <TableHead>Employee ID</TableHead>
//                   <TableHead>Name</TableHead>
//                   <TableHead>Department</TableHead>
//                   <TableHead>Hours</TableHead>
//                   <TableHead>Date</TableHead>
//                   <TableHead>Project</TableHead>
//                 </TableRow>
//               </TableHeader>
//               <TableBody>
//                 {filteredData.map((record) => (
//                   <TableRow 
//                     key={record.id} 
//                     className="border-glass-border hover:bg-muted/50 transition-colors"
//                   >
//                     <TableCell className="font-medium">{record.employeeId}</TableCell>
//                     <TableCell>{record.employeeName}</TableCell>
//                     <TableCell>{record.department}</TableCell>
//                     <TableCell>{record.hours}h</TableCell>
//                     <TableCell>{record.date}</TableCell>
//                     <TableCell>{record.project}</TableCell>
//                   </TableRow>
//                 ))}
//               </TableBody>
//             </Table>
//           </div>

//           <div className="mt-6 flex justify-between items-center">
//             <div className="text-sm text-muted-foreground">
//               Showing {filteredData.length} of {mergedData.length} records
//             </div>
//             <Button className="border border-blue-500 bg-blue-50 text-blue-700 hover:bg-blue-100 dark:bg-blue-950 dark:text-blue-300 dark:hover:bg-blue-900 transition-all duration-300">
//               <Download className="mr-2 h-4 w-4" />
//               Export Merged Data
//             </Button>
//           </div>
//         </CardContent>
//       </Card>
//     </div>
//   );
// }