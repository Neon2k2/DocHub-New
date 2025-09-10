import * as XLSX from 'xlsx';
import { apiService } from './api.service';

export interface ExcelData {
  headers: string[];
  data: any[];
  fileName: string;
  fileSize: number;
  uploadedAt: Date;
}

export interface ExcelUploadResult {
  success: boolean;
  data?: ExcelData;
  error?: string;
}

class ExcelService {
  /**
   * Parse Excel file and extract data
   */
  async parseExcelFile(file: File): Promise<ExcelData> {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      
      reader.onload = (e) => {
        try {
          const data = e.target?.result;
          const workbook = XLSX.read(data, { type: 'array' });
          const sheetName = workbook.SheetNames[0];
          const worksheet = workbook.Sheets[sheetName];
          const jsonData = XLSX.utils.sheet_to_json(worksheet, { header: 1 });
          
          if (jsonData.length === 0) {
            throw new Error('Excel file is empty');
          }

          // First row should be headers
          const headers = jsonData[0] as string[];
          const rows = jsonData.slice(1) as any[][];

          // Convert rows to objects
          const dataObjects = rows.map(row => {
            const obj: any = {};
            headers.forEach((header, index) => {
              obj[header] = row[index] || '';
            });
            return obj;
          });

          const excelData: ExcelData = {
            headers,
            data: dataObjects,
            fileName: file.name,
            fileSize: file.size,
            uploadedAt: new Date()
          };

          resolve(excelData);
        } catch (error) {
          reject(new Error(`Failed to parse Excel file: ${error instanceof Error ? error.message : 'Unknown error'}`));
        }
      };

      reader.onerror = () => {
        reject(new Error('Failed to read file'));
      };

      reader.readAsArrayBuffer(file);
    });
  }

  /**
   * Upload Excel file to backend
   */
  async uploadExcelFile(formData: FormData): Promise<ExcelUploadResult> {
    try {
      // Upload to backend
      const response = await apiService.uploadExcelFile(formData);
      
      if (response.success) {
        return {
          success: true,
          data: response.data
        };
      } else {
        return {
          success: false,
          error: response.error?.message || 'Upload failed'
        };
      }
    } catch (error) {
      return {
        success: false,
        error: error instanceof Error ? error.message : 'Unknown error occurred'
      };
    }
  }

  /**
   * Get Excel data for a specific tab
   */
  async getExcelDataForTab(tabId: string): Promise<ExcelData | null> {
    try {
      const response = await apiService.getExcelDataForTab(tabId);
      if (response.success && response.data) {
        return response.data;
      }
      return null;
    } catch (error) {
      console.error('Failed to get Excel data for tab:', error);
      return null;
    }
  }

  /**
   * Delete Excel data for a specific tab
   */
  async deleteExcelDataForTab(tabId: string): Promise<boolean> {
    try {
      const response = await apiService.deleteExcelDataForTab(tabId);
      return response.success;
    } catch (error) {
      console.error('Failed to delete Excel data for tab:', error);
      return false;
    }
  }

  /**
   * Validate Excel file
   */
  validateExcelFile(file: File): { valid: boolean; error?: string } {
    const allowedTypes = [
      'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet', // .xlsx
      'application/vnd.ms-excel', // .xls
      'text/csv' // .csv
    ];

    const allowedExtensions = ['.xlsx', '.xls', '.csv'];
    const fileExtension = file.name.toLowerCase().substring(file.name.lastIndexOf('.'));

    if (!allowedTypes.includes(file.type) && !allowedExtensions.includes(fileExtension)) {
      return {
        valid: false,
        error: 'Invalid file type. Please upload an Excel file (.xlsx, .xls) or CSV file.'
      };
    }

    if (file.size > 10 * 1024 * 1024) { // 10MB limit
      return {
        valid: false,
        error: 'File size too large. Please upload a file smaller than 10MB.'
      };
    }

    return { valid: true };
  }

  /**
   * Export data to Excel
   */
  exportToExcel(data: any[], headers: string[], fileName: string = 'export.xlsx'): void {
    const worksheet = XLSX.utils.json_to_sheet(data);
    const workbook = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(workbook, worksheet, 'Sheet1');
    XLSX.writeFile(workbook, fileName);
  }
}

export const excelService = new ExcelService();
