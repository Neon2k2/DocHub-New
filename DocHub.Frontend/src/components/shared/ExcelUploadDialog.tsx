import React, { useState, useRef } from 'react';
import { Upload, FileSpreadsheet, X, CheckCircle, AlertCircle } from 'lucide-react';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from '../ui/dialog';
import { Button } from '../ui/button';
import { Label } from '../ui/label';
import { Textarea } from '../ui/textarea';
import { Progress } from '../ui/progress';
import { excelService, ExcelData } from '../../services/excel.service';
import { notify } from '../../utils/notifications';

interface ExcelUploadDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  tabId: string;
  tabName: string;
  onUploadSuccess: (data: ExcelData) => void;
}

export function ExcelUploadDialog({ 
  open, 
  onOpenChange, 
  tabId, 
  tabName, 
  onUploadSuccess 
}: ExcelUploadDialogProps) {
  const [file, setFile] = useState<File | null>(null);
  const [description, setDescription] = useState('');
  const [uploading, setUploading] = useState(false);
  const [uploadProgress, setUploadProgress] = useState(0);
  const [previewData, setPreviewData] = useState<ExcelData | null>(null);
  const [error, setError] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const handleFileSelect = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFile = event.target.files?.[0];
    if (!selectedFile) return;

    setError(null);
    setPreviewData(null);

    // Validate file
    const validation = excelService.validateExcelFile(selectedFile);
    if (!validation.valid) {
      setError(validation.error || 'Invalid file');
      return;
    }

    setFile(selectedFile);

    try {
      // Parse file for preview
      const data = await excelService.parseExcelFile(selectedFile);
      setPreviewData(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to parse file');
    }
  };

  const handleUpload = async () => {
    if (!file || !previewData) return;

    setUploading(true);
    setUploadProgress(0);
    setError(null);

    try {
      // Simulate progress
      const progressInterval = setInterval(() => {
        setUploadProgress(prev => {
          if (prev >= 90) {
            clearInterval(progressInterval);
            return prev;
          }
          return prev + 10;
        });
      }, 200);

      // Create FormData for file upload
      const formData = new FormData();
      formData.append('file', file);
      formData.append('description', description || '');
      formData.append('metadata', JSON.stringify({
        headers: previewData.headers,
        rowCount: previewData.data.length,
        fileName: previewData.fileName
      }));

      // Upload to backend
      console.log('🔄 [EXCEL-DIALOG] Starting upload to backend...');
      console.log('📤 [EXCEL-DIALOG] FormData contents:', {
        hasFile: formData.has('file'),
        file: formData.get('file'),
        description: formData.get('description'),
        metadata: formData.get('metadata')
      });
      
      const response = await excelService.uploadExcelFile(formData, tabId);
      
      clearInterval(progressInterval);
      setUploadProgress(100);

      console.log('📊 [EXCEL-DIALOG] Upload response received:', response);
      console.log('🔍 [EXCEL-DIALOG] Response analysis:', {
        success: response.success,
        hasData: !!response.data,
        error: response.error
      });

      if (response.success) {
        console.log('✅ [EXCEL-DIALOG] Upload successful!');
        notify.success(`Excel file uploaded successfully! ${previewData.data.length} rows processed.`);
        onUploadSuccess(previewData);
        handleClose();
      } else {
        console.error('❌ [EXCEL-DIALOG] Upload failed:', response.error);
        setError(response.error || 'Upload failed');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Upload failed');
    } finally {
      setUploading(false);
    }
  };

  const handleClose = () => {
    setFile(null);
    setDescription('');
    setPreviewData(null);
    setError(null);
    setUploadProgress(0);
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
    onOpenChange(false);
  };

  const handleDrop = (event: React.DragEvent<HTMLDivElement>) => {
    event.preventDefault();
    const droppedFile = event.dataTransfer.files[0];
    if (droppedFile) {
      const validation = excelService.validateExcelFile(droppedFile);
      if (validation.valid) {
        setFile(droppedFile);
        excelService.parseExcelFile(droppedFile)
          .then(data => setPreviewData(data))
          .catch(err => setError(err instanceof Error ? err.message : 'Failed to parse file'));
      } else {
        setError(validation.error || 'Invalid file');
      }
    }
  };

  const handleDragOver = (event: React.DragEvent<HTMLDivElement>) => {
    event.preventDefault();
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent 
        className="dialog-panel max-h-[70vh] flex flex-col"
        style={{
          width: 'calc(100vw - 3rem) !important',
          maxWidth: 'calc(100vw - 3rem) !important',
          minWidth: 'calc(100vw - 3rem) !important'
        }}
      >
        <DialogHeader className="flex-shrink-0">
          <DialogTitle className="flex items-center gap-2">
            <FileSpreadsheet className="h-5 w-5 text-green-500" />
            Upload Excel Data for {tabName}
          </DialogTitle>
          <DialogDescription>
            Upload an Excel file (.xlsx, .xls) or CSV file to populate this tab with data
          </DialogDescription>
        </DialogHeader>

        <div className="flex-1 space-y-6 overflow-y-auto min-h-0">
          {/* File Upload Area */}
          <div className="space-y-4">
            <Label>Select Excel File</Label>
            <div
              className={`relative border-2 border-dashed rounded-lg transition-colors ${
                file ? 'border-green-500 bg-green-50' : 'border-gray-300 hover:border-gray-400'
              }`}
              onDrop={handleDrop}
              onDragOver={handleDragOver}
            >
              {file ? (
                <div className="p-6 text-center">
                  <div className="space-y-3">
                  <CheckCircle className="h-12 w-12 text-green-500 mx-auto" />
                    <div>
                  <p className="text-lg font-medium text-green-700">{file.name}</p>
                  <p className="text-sm text-gray-500">
                    {(file.size / 1024 / 1024).toFixed(2)} MB
                  </p>
                    </div>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => {
                      setFile(null);
                      setPreviewData(null);
                      setError(null);
                      if (fileInputRef.current) {
                        fileInputRef.current.value = '';
                      }
                    }}
                      className="mt-2"
                  >
                    <X className="h-4 w-4 mr-2" />
                    Remove
                  </Button>
                  </div>
                </div>
              ) : (
                <div className="p-8 text-center">
                <div className="space-y-4">
                  <Upload className="h-12 w-12 text-gray-400 mx-auto" />
                  <div>
                    <p className="text-lg font-medium">Drop your Excel file here</p>
                    <p className="text-sm text-gray-500">or click to browse</p>
                  </div>
                  <Button
                    variant="outline"
                    onClick={() => fileInputRef.current?.click()}
                  >
                    <Upload className="h-4 w-4 mr-2" />
                    Choose File
                  </Button>
                  </div>
                </div>
              )}
            </div>

            <input
              ref={fileInputRef}
              type="file"
              accept=".xlsx,.xls,.csv"
              onChange={handleFileSelect}
              className="hidden"
            />
          </div>

          {/* Description */}
          <div className="space-y-2">
            <Label htmlFor="description">Description (Optional)</Label>
            <Textarea
              id="description"
              placeholder="Describe this data set..."
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              rows={3}
            />
          </div>

          {/* Error Display */}
          {error && (
            <div className="flex items-center gap-2 p-3 bg-red-50 border border-red-200 rounded-lg">
              <AlertCircle className="h-5 w-5 text-red-500" />
              <p className="text-sm text-red-700">{error}</p>
            </div>
          )}

          {/* Preview Data */}
          {previewData && (
            <div className="space-y-3">
              <div className="flex items-center justify-between">
                <h3 className="text-lg font-medium">Data Preview</h3>
                <div className="text-sm text-gray-500">
                  {previewData.data.length} rows, {previewData.headers.length} columns
                </div>
              </div>
              
              <div className="border rounded-lg overflow-hidden max-h-48 overflow-y-auto">
                <table className="w-full text-sm">
                  <thead className="bg-gray-50 sticky top-0">
                    <tr>
                      {previewData.headers.map((header, index) => (
                        <th key={index} className="px-3 py-2 text-left font-medium border-b">
                          {header}
                        </th>
                      ))}
                    </tr>
                  </thead>
                  <tbody>
                    {previewData.data.slice(0, 5).map((row, rowIndex) => (
                      <tr key={rowIndex} className="hover:bg-gray-50">
                        {previewData.headers.map((header, colIndex) => (
                          <td key={colIndex} className="px-3 py-2 border-b">
                            {row[header] || ''}
                          </td>
                        ))}
                      </tr>
                    ))}
                  </tbody>
                </table>
                {previewData.data.length > 5 && (
                  <div className="px-3 py-2 bg-gray-50 text-sm text-gray-500 text-center">
                    Showing first 5 rows of {previewData.data.length} total rows
                  </div>
                )}
              </div>
            </div>
          )}

          {/* Upload Progress */}
          {uploading && (
            <div className="space-y-2">
              <div className="flex items-center justify-between text-sm">
                <span>Uploading...</span>
                <span>{uploadProgress}%</span>
              </div>
              <Progress value={uploadProgress} className="w-full" />
            </div>
          )}
        </div>

        {/* Actions */}
        <div className="flex-shrink-0 flex justify-end gap-3 pt-4 border-t">
          <Button 
            variant="outline" 
            onClick={handleClose} 
            disabled={uploading}
            className="text-gray-900 border-gray-300 hover:bg-gray-100"
          >
            Cancel
          </Button>
          <Button
            onClick={handleUpload}
            disabled={!file || !previewData || uploading}
            className="bg-green-600 hover:bg-green-700 text-white"
          >
            {uploading ? 'Uploading...' : 'Upload Excel Data'}
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  );
}
