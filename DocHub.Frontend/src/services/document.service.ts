// Document Generation and Template Management Service
import { apiService, Employee, ApiResponse, DocumentTemplate, Signature } from './api.service';

// Re-export types from API service for consistency
export type { DocumentTemplate, Signature };

export interface GeneratedDocument {
  id: string;
  templateId: string;
  employeeId: string;
  signatureId?: string;
  content: string; // Generated HTML/Rich Text content
  placeholderData: Record<string, any>;
  generatedBy: string;
  generatedAt: Date;
  downloadUrl?: string;
}

export interface EmailTemplate {
  id: string;
  name: string;
  subject: string;
  content: string; // HTML content
  placeholders: string[];
  createdBy: string;
  createdAt: Date;
}

export interface EmailAttachment {
  id: string;
  fileName: string;
  fileUrl: string;
  fileSize: number;
  mimeType: string;
}

export interface EmailJob {
  id: string;
  employeeId: string;
  employeeName: string;
  employeeEmail: string;
  documentId: string;
  emailTemplateId?: string;
  subject: string;
  content: string;
  attachments: EmailAttachment[];
  status: 'pending' | 'sending' | 'sent' | 'failed' | 'delivered' | 'opened';
  sentBy: string;
  createdAt: Date;
  sentAt?: Date;
  deliveredAt?: Date;
  openedAt?: Date;
  error?: string;
  trackingId?: string;
}

class DocumentService {
  // Template Management
  async getTemplates(type?: string): Promise<DocumentTemplate[]> {
    const params = new URLSearchParams();
    if (type) params.set('type', type);
    
    const response = await apiService.getTemplates();
    if (response.success && response.data) {
      return type 
        ? response.data.filter(t => t.type === type)
        : response.data;
    }
    return [];
  }

  async uploadTemplate(file: File, type: string, name: string): Promise<DocumentTemplate> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('type', type);
    formData.append('name', name);

    const response = await apiService.uploadTemplate(formData);
    if (response.success && response.data) {
      return response.data;
    }
    throw new Error('Failed to upload template');
  }

  // Signature Management
  async getSignatures(): Promise<Signature[]> {
    const response = await apiService.getSignatures();
    if (response.success && response.data) {
      return response.data;
    }
    return [];
  }

  async uploadSignature(file: File, name: string): Promise<Signature> {
    const formData = new FormData();
    formData.append('signature', file);
    formData.append('name', name);

    const response = await apiService.uploadSignature(formData);
    if (response.success && response.data) {
      return response.data;
    }
    throw new Error('Failed to upload signature');
  }

  // Document Generation
  async generateDocument(
    templateId: string,
    employee: Employee,
    signatureId?: string,
    additionalData?: Record<string, any>
  ): Promise<GeneratedDocument> {
    try {
      const response = await apiService.request<ApiResponse<GeneratedDocument>>(
        '/documents/generate', {
          method: 'POST',
          body: JSON.stringify({
            templateId,
            employeeId: employee.id,
            signatureId,
            placeholderData: {
              EMPLOYEE_NAME: employee.name,
              EMPLOYEE_ID: employee.employeeId,
              DESIGNATION: employee.designation,
              DEPARTMENT: employee.department,
              JOIN_DATE: employee.joiningDate,
              RELIEVING_DATE: employee.relievingDate || 'Current',
              COMPANY_NAME: 'DocHub Technologies',
              ...additionalData
            }
          })
        }
      );

      if (response.success && response.data) {
        return response.data;
      } else {
        throw new Error(response.error?.message || 'Failed to generate document');
      }
    } catch (error) {
      console.error('Error generating document:', error);
      throw new Error('Failed to generate document. Please try again.');
    }
  }

  async downloadDocument(documentId: string, format: 'pdf' | 'docx' = 'pdf'): Promise<Blob> {
    try {
      const response = await fetch(`${apiService['baseUrl']}/documents/${documentId}/download?format=${format}`, {
        method: 'GET',
        headers: {
          'Authorization': `Bearer ${apiService['authToken']}`,
          'Content-Type': 'application/json'
        }
      });

      if (!response.ok) {
        throw new Error(`Failed to download document: ${response.statusText}`);
      }

      return await response.blob();
    } catch (error) {
      console.error('Error downloading document:', error);
      throw new Error('Failed to download document. Please try again.');
    }
  }

  // Email Management
  async getEmailTemplates(): Promise<EmailTemplate[]> {
    try {
      const response = await apiService.request<ApiResponse<EmailTemplate[]>>('/email-templates');
      return response.success && response.data ? response.data : [];
    } catch (error) {
      console.error('Error fetching email templates:', error);
      return [];
    }
  }

  async sendEmails(emailJobs: Omit<EmailJob, 'id' | 'status' | 'createdAt' | 'sentBy'>[]): Promise<EmailJob[]> {
    const response = await apiService.sendEmail({
      to: emailJobs.map(job => job.employeeEmail),
      subject: emailJobs[0]?.subject || 'Document Email',
      body: emailJobs[0]?.content || 'Please find attached your document.',
      attachments: emailJobs.flatMap(job => job.attachments.map(att => att.fileUrl))
    });
    
    if (response.success) {
      return emailJobs.map(job => ({
        ...job,
        id: Date.now().toString() + Math.random(),
        status: 'pending' as const,
        sentBy: 'Current User',
        createdAt: new Date()
      }));
    }
    throw new Error('Failed to send emails');
  }

  async getEmailJobs(params?: {
    employeeId?: string;
    status?: string;
    sentBy?: string;
    startDate?: string;
    endDate?: string;
  }): Promise<EmailJob[]> {
    try {
      const queryParams = new URLSearchParams();
      if (params?.employeeId) queryParams.append('employeeId', params.employeeId);
      if (params?.status) queryParams.append('status', params.status);
      if (params?.sentBy) queryParams.append('sentBy', params.sentBy);
      if (params?.startDate) queryParams.append('startDate', params.startDate);
      if (params?.endDate) queryParams.append('endDate', params.endDate);

      const response = await apiService.request<ApiResponse<{ items: EmailJob[]; totalCount: number; page: number; limit: number; totalPages: number }>>(`/emails/jobs?${queryParams}`);
      return response.success && response.data ? response.data.items : [];
    } catch (error) {
      console.error('Error fetching email jobs:', error);
      return [];
    }
  }

  // Real-time status updates using SignalR
  subscribeToEmailUpdates(callback: (job: EmailJob) => void): () => void {
    // SignalR implementation placeholder
    console.log('Email updates subscription - SignalR implementation needed');
    return () => {
      console.log('Email updates unsubscribed');
    };
  }
}

export const documentService = new DocumentService();
export default documentService;