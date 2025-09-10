// API Service for DocHub Application
// This service handles all API communications

import { UserRole } from '../components/Login';
import { config } from '../config/environment';

// Dynamic System Types
export interface LetterTypeDefinition {
  id: string;
  typeKey: string;
  displayName: string;
  description: string;
  fieldConfiguration?: string;
  isActive: boolean;
  fields: DynamicField[];
  createdAt: string;
  updatedAt: string;
}

export interface DynamicField {
  id: string;
  letterTypeDefinitionId: string;
  fieldKey: string;
  displayName: string;
  fieldType: FieldType;
  isRequired: boolean;
  validationRules?: string;
  defaultValue?: string;
  order: number;
  createdAt: string;
  updatedAt: string;
}

export enum FieldType {
  Text = 'Text',
  Number = 'Number',
  Date = 'Date',
  Email = 'Email',
  PhoneNumber = 'PhoneNumber',
  Currency = 'Currency',
  Percentage = 'Percentage',
  Boolean = 'Boolean',
  Dropdown = 'Dropdown',
  TextArea = 'TextArea',
  Url = 'Url',
  Image = 'Image',
  File = 'File',
  DateTime = 'DateTime',
  Time = 'Time',
  Json = 'Json'
}

export interface DynamicFieldData {
  id: string;
  employeeId: string;
  letterTypeDefinitionId: string;
  fieldKey: string;
  fieldValue: string;
  fieldType: FieldType;
  createdAt: string;
  updatedAt: string;
}

export interface ExcelUploadRequest {
  letterTypeDefinitionId: string;
  file: File;
  fieldMappings?: FieldMapping[];
  options?: ExcelProcessingOptions;
}

export interface FieldMapping {
  excelColumn: string;
  fieldKey: string;
  fieldType: FieldType;
  isRequired: boolean;
}

export interface ExcelProcessingOptions {
  skipEmptyRows: boolean;
  validateData: boolean;
  generateTemplates: boolean;
  maxRows?: number;
}

export interface ExcelUploadResponse {
  success: boolean;
  message: string;
  data?: {
    processedRows: number;
    validRows: number;
    invalidRows: number;
    fieldMappings: FieldMapping[];
    sampleData: Record<string, unknown>[];
    errors: string[];
    warnings: string[];
  };
}

export interface DynamicDocumentGenerationRequest {
  letterTypeDefinitionId: string;
  employeeIds: string[];
  templateId?: string;
  signatureId?: string;
  includeDocumentAttachments: boolean;
  additionalFieldData?: Record<string, any>;
}

export interface DynamicDocumentGenerationResponse {
  success: boolean;
  message: string;
  data?: {
    generatedDocuments: GeneratedDocument[];
    errors: string[];
    warnings: string[];
  };
}

export interface DynamicEmailRequest {
  letterTypeDefinitionId: string;
  employeeId: string;
  documentId?: string;
  signatureId?: string;
  emailTemplateId?: string;
  customSubject?: string;
  customBody?: string;
  ccRecipients?: string[];
  bccRecipients?: string[];
  additionalFieldData?: Record<string, any>;
  includeDocumentAttachment: boolean;
  additionalAttachments?: EmailAttachmentRequest[];
  priority: EmailPriority;
  enableTracking: boolean;
  sendImmediately: boolean;
  scheduledSendTime?: string;
  configuration?: Record<string, any>;
}

export interface EmailAttachmentRequest {
  fileName: string;
  filePath: string;
  contentType: string;
  isInline: boolean;
  contentId?: string;
}

export enum EmailPriority {
  Low = 0,
  Normal = 1,
  High = 2,
  Urgent = 3
}

export interface DynamicEmailResponse {
  success: boolean;
  message: string;
  emailJobId?: string;
  trackingId?: string;
  sendGridMessageId?: string;
  status: string;
  sentAt?: string;
  scheduledFor?: string;
  stats: EmailProcessingStats;
  errors: string[];
  warnings: string[];
}

export interface EmailProcessingStats {
  startTime: string;
  endTime: string;
  duration: string;
  emailsProcessed: number;
  validationErrors: number;
  processingErrors: number;
  rateLimitHits: number;
  memoryUsageMB: number;
}

export interface BulkDynamicEmailRequest {
  letterTypeDefinitionId: string;
  employeeIds: string[];
  employeeDocumentIds?: Record<string, string>;
  signatureId?: string;
  emailTemplateId?: string;
  customSubjectTemplate?: string;
  customBodyTemplate?: string;
  ccRecipients?: string[];
  bccRecipients?: string[];
  employeeFieldData?: Record<string, Record<string, any>>;
  includeDocumentAttachments: boolean;
  additionalAttachments?: EmailAttachmentRequest[];
  priority: EmailPriority;
  enableTracking: boolean;
  sendImmediately: boolean;
  scheduledSendTime?: string;
  maxEmailsPerMinute: number;
  configuration?: Record<string, any>;
}

export interface BulkDynamicEmailResponse {
  success: boolean;
  message: string;
  totalEmails: number;
  successfulEmails: number;
  failedEmails: number;
  emailJobs: EmailJobInfo[];
  stats: EmailProcessingStats;
  errors: string[];
  warnings: string[];
}

export interface EmailJobInfo {
  emailJobId: string;
  employeeId: string;
  employeeName: string;
  employeeEmail: string;
  subject: string;
  trackingId: string;
  status: string;
  success: boolean;
  errorMessage: string;
  createdAt: string;
  sentAt?: string;
}

export interface EmailAnalytics {
  totalEmailsSent: number;
  emailsDelivered: number;
  emailsOpened: number;
  emailsClicked: number;
  emailsBounced: number;
  emailsFailed: number;
  deliveryRate: number;
  openRate: number;
  clickRate: number;
  bounceRate: number;
  failureRate: number;
  averageDeliveryTime: string;
  averageOpenTime: string;
  averageClickTime: string;
  dailyStats: EmailAnalyticsByDay[];
  employeeStats: EmailAnalyticsByEmployee[];
}

export interface EmailAnalyticsByDay {
  date: string;
  emailsSent: number;
  emailsDelivered: number;
  emailsOpened: number;
  emailsClicked: number;
  emailsBounced: number;
  emailsFailed: number;
}

export interface EmailAnalyticsByEmployee {
  employeeId: string;
  employeeName: string;
  employeeEmail: string;
  emailsSent: number;
  emailsDelivered: number;
  emailsOpened: number;
  emailsClicked: number;
  emailsBounced: number;
  emailsFailed: number;
  openRate: number;
  clickRate: number;
}

export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  error?: {
    code: string;
    message: string;
    details?: string;
  };
}

export interface PaginatedResponse<T> {
  success: boolean;
  data: {
    items: T[];
    pagination: {
      currentPage: number;
      totalPages: number;
      totalRecords: number;
      hasNext: boolean;
      hasPrevious: boolean;
    };
  };
}

class ApiService {
  private baseUrl = config.api.baseUrl;
  private authToken: string | null = null;
  private isRefreshing = false;
  private refreshPromise: Promise<ApiResponse<{
    token: string;
    refreshToken: string;
  }>> | null = null;

  constructor() {
    // Initialize auth token from localStorage
    this.authToken = localStorage.getItem('authToken');
  }

  setAuthToken(token: string) {
    this.authToken = token;
    localStorage.setItem('authToken', token);
  }

  clearAuthToken() {
    this.authToken = null;
    localStorage.removeItem('authToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('currentUser');
  }

  private getHeaders(): HeadersInit {
    const headers: HeadersInit = {
      'Content-Type': 'application/json',
    };

    if (this.authToken) {
      headers.Authorization = `Bearer ${this.authToken}`;
    }

    return headers;
  }

  private async handleResponse<T>(response: Response, originalRequest?: () => Promise<T>): Promise<T> {
    if (!response.ok) {
      if (response.status === 401) {
        // Try to refresh token
        const refreshToken = localStorage.getItem('refreshToken');
        if (refreshToken && originalRequest) {
          try {
            // Use existing refresh promise if already refreshing
            if (this.isRefreshing && this.refreshPromise) {
              await this.refreshPromise;
            } else {
              await this.refreshToken(refreshToken);
            }
            // Retry the original request
            return await originalRequest();
          } catch (refreshError) {
            console.warn('Token refresh failed:', refreshError);
            this.clearAuthToken();
            window.location.href = '/login';
            throw new Error('Authentication failed - please login again');
          }
        } else {
          this.clearAuthToken();
          window.location.href = '/login';
          throw new Error('Authentication required');
        }
      }
      
      const errorData = await response.json().catch(() => ({}));
      throw new Error(errorData.message || `HTTP Error: ${response.status}`);
    }

    return response.json();
  }

  async request<T>(
    endpoint: string,
    options: RequestInit = {}
  ): Promise<T> {
    const url = `${this.baseUrl}${endpoint}`;
    
    // For FormData, don't set Content-Type - let browser set it with boundary
    const isFormData = options.body instanceof FormData;
    const baseHeaders = isFormData ? {} : this.getHeaders();
    
    const config: RequestInit = {
      ...options,
      headers: {
        ...baseHeaders,
        ...options.headers,
      },
    };


    const makeRequest = async (): Promise<T> => {
      const response = await fetch(url, config);
      return this.handleResponse<T>(response, makeRequest);
    };

    try {
      return await makeRequest();
    } catch (error) {
      console.error(`API Request failed: ${endpoint}`, error);
      throw error;
    }
  }

  // Authentication APIs
  async login(credentials: { username: string; password: string }): Promise<ApiResponse<{
    token: string;
    refreshToken: string;
    user: UserRole;
  }>> {
    return this.request<ApiResponse<{
      token: string;
      refreshToken: string;  
      user: UserRole;
    }>>('/auth/login', {
      method: 'POST',
      body: JSON.stringify(credentials),
    });
  }

  async refreshToken(refreshToken: string): Promise<ApiResponse<{
    token: string;
    refreshToken: string;
  }>> {
    // Prevent multiple simultaneous refresh attempts
    if (this.isRefreshing && this.refreshPromise) {
      return this.refreshPromise;
    }

    this.isRefreshing = true;
    this.refreshPromise = this.performTokenRefresh(refreshToken);

    try {
      const result = await this.refreshPromise;
      return result;
    } finally {
      this.isRefreshing = false;
      this.refreshPromise = null;
    }
  }

  private async performTokenRefresh(refreshToken: string): Promise<ApiResponse<{
    token: string;
    refreshToken: string;
  }>> {
    const response = await this.request<ApiResponse<{
      token: string;
      refreshToken: string;
    }>>('/auth/refresh', {
      method: 'POST',
      body: JSON.stringify({ refreshToken }),
    });

    if (response.success && response.data) {
      const { token, refreshToken: newRefreshToken } = response.data;
      
      // Update stored tokens
      localStorage.setItem('authToken', token);
      localStorage.setItem('refreshToken', newRefreshToken);
      this.setAuthToken(token);
    }

    return response;
  }

  async logout(): Promise<ApiResponse<void>> {
    const refreshToken = localStorage.getItem('refreshToken');
    
    if (refreshToken) {
      await this.request<ApiResponse<void>>('/auth/logout', {
        method: 'POST',
        body: JSON.stringify({ refreshToken }),
      });
    }

    this.clearAuthToken();
    return Promise.resolve({ success: true });
  }

  // Employee APIs
  async getEmployees(params?: {
    page?: number;
    limit?: number;
    search?: string;
    department?: string;
    status?: string;
  }): Promise<PaginatedResponse<Employee>> {
    const queryParams = new URLSearchParams();
    if (params?.page) queryParams.set('page', params.page.toString());
    if (params?.limit) queryParams.set('limit', params.limit.toString());
    if (params?.search) queryParams.set('search', params.search);
    if (params?.department) queryParams.set('department', params.department);
    if (params?.status) queryParams.set('status', params.status);

    return this.request<PaginatedResponse<Employee>>(`/er/employees?${queryParams}`);
  }

  async getEmployee(id: string): Promise<ApiResponse<Employee>> {
    return this.request<ApiResponse<Employee>>(`/er/employees/${id}`);
  }

  async createEmployee(employee: CreateEmployeeRequest): Promise<ApiResponse<Employee>> {
    return this.request<ApiResponse<Employee>>('/er/employees', {
      method: 'POST',
      body: JSON.stringify(employee),
    });
  }

  async updateEmployee(id: string, employee: Partial<Employee>): Promise<ApiResponse<Employee>> {
    return this.request<ApiResponse<Employee>>(`/er/employees/${id}`, {
      method: 'PUT',
      body: JSON.stringify(employee),
    });
  }

  async deleteEmployee(id: string): Promise<ApiResponse<void>> {
    return this.request<ApiResponse<void>>(`/er/employees/${id}`, {
      method: 'DELETE',
    });
  }



  async getDashboardStats(module: 'er' | 'billing'): Promise<ApiResponse<DashboardStats>> {
    return this.request<ApiResponse<DashboardStats>>(`/dashboard/${module}/stats`);
  }

  // User Management APIs
  async getUsers(): Promise<ApiResponse<UserRole[]>> {
    return this.request<ApiResponse<UserRole[]>>('/admin/users');
  }

  async createUser(user: CreateUserRequest): Promise<ApiResponse<UserRole>> {
    return this.request<ApiResponse<UserRole>>('/admin/users', {
      method: 'POST',
      body: JSON.stringify(user),
    });
  }

  async updateUser(id: string, user: UpdateUserRequest): Promise<ApiResponse<UserRole>> {
    return this.request<ApiResponse<UserRole>>(`/admin/users/${id}`, {
      method: 'PUT',
      body: JSON.stringify(user),
    });
  }

  async deleteUser(id: string): Promise<ApiResponse<void>> {
    return this.request<ApiResponse<void>>(`/admin/users/${id}`, {
      method: 'DELETE',
    });
  }

  async resetUserPassword(id: string): Promise<ApiResponse<void>> {
    return this.request<ApiResponse<void>>(`/admin/users/${id}/reset-password`, {
      method: 'POST',
    });
  }

  // Document Template APIs
  async getTemplates(): Promise<ApiResponse<DocumentTemplate[]>> {
    return this.request<ApiResponse<DocumentTemplate[]>>('/er/templates');
  }

  async uploadTemplate(template: FormData): Promise<ApiResponse<DocumentTemplate>> {
    return this.request<ApiResponse<DocumentTemplate>>('/er/templates/upload', {
      method: 'POST',
      body: template,
      headers: {
        ...(this.authToken ? { Authorization: `Bearer ${this.authToken}` } : {})
      }
    });
  }

  // Signature APIs
  async getSignatures(): Promise<ApiResponse<Signature[]>> {
    return this.request<ApiResponse<Signature[]>>('/er/signatures');
  }

  async uploadSignature(signature: FormData): Promise<ApiResponse<Signature>> {
    return this.request<ApiResponse<Signature>>('/er/signatures/upload', {
      method: 'POST',
      body: signature,
      headers: {
        ...(this.authToken ? { Authorization: `Bearer ${this.authToken}` } : {})
      }
    });
  }

  async deleteSignature(id: string): Promise<ApiResponse<void>> {
    return this.request<ApiResponse<void>>(`/er/signatures/${id}`, {
      method: 'DELETE',
    });
  }

  // Email APIs
  async sendEmail(email: SendEmailRequest): Promise<ApiResponse<void>> {
    return this.request<ApiResponse<void>>('/er/emails/send', {
      method: 'POST',
      body: JSON.stringify(email),
    });
  }

  async getEmailStatus(emailId: string): Promise<ApiResponse<EmailStatus>> {
    return this.request<ApiResponse<EmailStatus>>(`/er/emails/${emailId}/status`);
  }

  // Dynamic Tab APIs
  async getDynamicTabs(): Promise<ApiResponse<DynamicTab[]>> {
    return this.request<ApiResponse<DynamicTab[]>>('/er/dynamic-tabs');
  }

  async createDynamicTab(tab: CreateDynamicTabRequest): Promise<ApiResponse<DynamicTab>> {
    return this.request<ApiResponse<DynamicTab>>('/er/dynamic-tabs', {
      method: 'POST',
      body: JSON.stringify(tab),
    });
  }

  async updateDynamicTab(id: string, tab: UpdateDynamicTabRequest): Promise<ApiResponse<DynamicTab>> {
    return this.request<ApiResponse<DynamicTab>>(`/er/dynamic-tabs/${id}`, {
      method: 'PUT',
      body: JSON.stringify(tab),
    });
  }

  async deleteDynamicTab(id: string): Promise<ApiResponse<void>> {
    return this.request<ApiResponse<void>>(`/er/dynamic-tabs/${id}`, {
      method: 'DELETE',
    });
  }

  // Excel Data APIs
  async uploadExcelFile(formData: FormData): Promise<ApiResponse<any>> {
    // Debug logging
    console.log('Uploading Excel file with FormData:', {
      hasFile: formData.has('file'),
      tabId: formData.get('tabId'),
      description: formData.get('description'),
      metadata: formData.get('metadata')
    });
    
    return this.request<ApiResponse<any>>('/er/excel/upload', {
      method: 'POST',
      body: formData,
      headers: {
        ...(this.authToken ? { Authorization: `Bearer ${this.authToken}` } : {})
      }
    });
  }

  async getExcelDataForTab(tabId: string): Promise<ApiResponse<any>> {
    return this.request<ApiResponse<any>>(`/er/excel/tab/${tabId}`);
  }

  async deleteExcelDataForTab(tabId: string): Promise<ApiResponse<void>> {
    return this.request<ApiResponse<void>>(`/er/excel/tab/${tabId}`, {
      method: 'DELETE',
    });
  }

  async getDynamicTabById(id: string): Promise<ApiResponse<DynamicTab>> {
    return this.request<ApiResponse<DynamicTab>>(`/er/dynamic-tabs/${id}`);
  }

  // Dynamic Tab Operations
  async generateDocumentsForTab(tabId: string, request: GenerateDocumentsForTabRequest): Promise<ApiResponse<any>> {
    return this.request<ApiResponse<any>>(`/er/dynamic-tabs/${tabId}/generate`, {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async clearSelectionForTab(tabId: string): Promise<ApiResponse<any>> {
    return this.request<ApiResponse<any>>(`/er/dynamic-tabs/${tabId}/clear-selection`, {
      method: 'POST',
    });
  }

  async previewDocumentsForTab(tabId: string, request: PreviewDocumentsForTabRequest): Promise<ApiResponse<any>> {
    return this.request<ApiResponse<any>>(`/er/dynamic-tabs/${tabId}/preview`, {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  // Bulk Email Operations
  async sendBulkEmails(request: SendBulkEmailRequest): Promise<ApiResponse<any>> {
    return this.request<ApiResponse<any>>('/er/emails/send-bulk', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  // Document Request APIs
  async getDocumentRequests(params: {
    page?: number;
    limit?: number;
    documentType?: string;
    status?: string;
    employeeId?: string;
    approverId?: string;
  } = {}): Promise<PaginatedResponse<DocumentRequest>> {
    const queryParams = new URLSearchParams();
    
    if (params.page) queryParams.append('page', params.page.toString());
    if (params.limit) queryParams.append('limit', params.limit.toString());
    if (params.documentType) queryParams.append('documentType', params.documentType);
    if (params.status) queryParams.append('status', params.status);
    if (params.employeeId) queryParams.append('employeeId', params.employeeId);
    if (params.approverId) queryParams.append('approverId', params.approverId);

    return this.request<PaginatedResponse<DocumentRequest>>(`/documentrequests?${queryParams}`);
  }

  async getDocumentRequest(id: string): Promise<ApiResponse<DocumentRequest>> {
    return this.request<ApiResponse<DocumentRequest>>(`/documentrequests/${id}`);
  }

  async createDocumentRequest(request: CreateDocumentRequestRequest): Promise<ApiResponse<DocumentRequest>> {
    return this.request<ApiResponse<DocumentRequest>>('/documentrequests', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async approveDocumentRequest(id: string, request: ApproveDocumentRequestRequest): Promise<ApiResponse<DocumentRequest>> {
    return this.request<ApiResponse<DocumentRequest>>(`/documentrequests/${id}/approve`, {
      method: 'PUT',
      body: JSON.stringify(request),
    });
  }

  async generateDocumentRequest(id: string): Promise<ApiResponse<DocumentRequest>> {
    return this.request<ApiResponse<DocumentRequest>>(`/documentrequests/${id}/generate`, {
      method: 'POST',
    });
  }

  async getDocumentRequestStats(): Promise<ApiResponse<DocumentRequestStats>> {
    return this.request<ApiResponse<DocumentRequestStats>>('/documentrequests/stats');
  }

  // Email Template APIs
  async getEmailTemplates(params?: {
    page?: number;
    limit?: number;
    type?: string;
    category?: string;
    isActive?: boolean;
    searchTerm?: string;
  }): Promise<ApiResponse<PaginatedResponse<EmailTemplate>>> {
    const queryParams = new URLSearchParams();
    if (params?.page) queryParams.set('page', params.page.toString());
    if (params?.limit) queryParams.set('limit', params.limit.toString());
    if (params?.type) queryParams.set('type', params.type);
    if (params?.category) queryParams.set('category', params.category);
    if (params?.isActive !== undefined) queryParams.set('isActive', params.isActive.toString());
    if (params?.searchTerm) queryParams.set('searchTerm', params.searchTerm);

    return this.request<ApiResponse<PaginatedResponse<EmailTemplate>>>(`/email-templates?${queryParams}`);
  }

  async getEmailTemplate(id: string): Promise<ApiResponse<EmailTemplate>> {
    return this.request<ApiResponse<EmailTemplate>>(`/email-templates/${id}`);
  }

  async createEmailTemplate(template: CreateEmailTemplateRequest): Promise<ApiResponse<EmailTemplate>> {
    return this.request<ApiResponse<EmailTemplate>>('/email-templates', {
      method: 'POST',
      body: JSON.stringify(template),
    });
  }

  async processEmailTemplate(id: string, data: Record<string, any>): Promise<ApiResponse<ProcessedEmailTemplate>> {
    return this.request<ApiResponse<ProcessedEmailTemplate>>(`/email-templates/${id}/process`, {
      method: 'POST',
      body: JSON.stringify(data),
    });
  }

  async cloneEmailTemplate(id: string, request: CloneEmailTemplateRequest): Promise<ApiResponse<EmailTemplate>> {
    return this.request<ApiResponse<EmailTemplate>>(`/email-templates/${id}/clone`, {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async validateEmailTemplate(id: string): Promise<ApiResponse<EmailTemplateValidation>> {
    return this.request<ApiResponse<EmailTemplateValidation>>(`/email-templates/${id}/validate`, {
      method: 'POST',
    });
  }

  async createDefaultEmailTemplates(): Promise<ApiResponse<string>> {
    return this.request<ApiResponse<string>>('/email-templates/create-defaults', {
      method: 'POST',
    });
  }

  // File Management APIs
  async getFileStorageStatistics(): Promise<ApiResponse<FileStorageStatistics>> {
    return this.request<ApiResponse<FileStorageStatistics>>('/filemanagement/statistics');
  }

  async getFileCleanupReport(): Promise<ApiResponse<FileCleanupReport>> {
    return this.request<ApiResponse<FileCleanupReport>>('/filemanagement/cleanup-report');
  }

  async cleanupExpiredFiles(): Promise<ApiResponse<string>> {
    return this.request<ApiResponse<string>>('/filemanagement/cleanup/expired', {
      method: 'POST',
    });
  }

  async cleanupOrphanedFiles(): Promise<ApiResponse<string>> {
    return this.request<ApiResponse<string>>('/filemanagement/cleanup/orphaned', {
      method: 'POST',
    });
  }

  async cleanupTempFiles(): Promise<ApiResponse<string>> {
    return this.request<ApiResponse<string>>('/filemanagement/cleanup/temp', {
      method: 'POST',
    });
  }

  async validateFile(file: File): Promise<ApiResponse<FileValidationResult>> {
    const formData = new FormData();
    formData.append('file', file);
    
    return this.request<ApiResponse<FileValidationResult>>('/filemanagement/validate', {
      method: 'POST',
      body: formData,
      headers: {
        ...(this.authToken ? { Authorization: `Bearer ${this.authToken}` } : {})
      }
    });
  }

  // Notification APIs
  async getNotifications(params?: {
    page?: number;
    limit?: number;
    includeRead?: boolean;
    type?: string;
    priority?: string;
  }): Promise<ApiResponse<PaginatedResponse<Notification>>> {
    const queryParams = new URLSearchParams();
    if (params?.page) queryParams.set('page', params.page.toString());
    if (params?.limit) queryParams.set('limit', params.limit.toString());
    if (params?.includeRead !== undefined) queryParams.set('includeRead', params.includeRead.toString());
    if (params?.type) queryParams.set('type', params.type);
    if (params?.priority) queryParams.set('priority', params.priority);

    return this.request<ApiResponse<PaginatedResponse<Notification>>>(`/notifications?${queryParams}`);
  }

  async getNotificationStats(): Promise<ApiResponse<NotificationStats>> {
    return this.request<ApiResponse<NotificationStats>>('/notifications/stats');
  }

  async getUnreadNotificationCount(): Promise<ApiResponse<number>> {
    return this.request<ApiResponse<number>>('/notifications/unread-count');
  }

  async createNotification(notification: CreateNotificationRequest): Promise<ApiResponse<Notification>> {
    return this.request<ApiResponse<Notification>>('/notifications', {
      method: 'POST',
      body: JSON.stringify(notification),
    });
  }

  async markNotificationAsRead(notificationId: string): Promise<ApiResponse<boolean>> {
    return this.request<ApiResponse<boolean>>(`/notifications/${notificationId}/read`, {
      method: 'PUT',
    });
  }

  async markAllNotificationsAsRead(): Promise<ApiResponse<boolean>> {
    return this.request<ApiResponse<boolean>>('/notifications/mark-all-read', {
      method: 'PUT',
    });
  }

  async deleteNotification(notificationId: string): Promise<ApiResponse<boolean>> {
    return this.request<ApiResponse<boolean>>(`/notifications/${notificationId}`, {
      method: 'DELETE',
    });
  }

  // Notification Template APIs
  async getNotificationTemplates(params?: {
    type?: string;
    category?: string;
    activeOnly?: boolean;
  }): Promise<ApiResponse<NotificationTemplate[]>> {
    const queryParams = new URLSearchParams();
    if (params?.type) queryParams.set('type', params.type);
    if (params?.category) queryParams.set('category', params.category);
    if (params?.activeOnly !== undefined) queryParams.set('activeOnly', params.activeOnly.toString());

    return this.request<ApiResponse<NotificationTemplate[]>>(`/notificationtemplates?${queryParams}`);
  }

  async createNotificationTemplate(template: CreateNotificationTemplateRequest): Promise<ApiResponse<NotificationTemplate>> {
    return this.request<ApiResponse<NotificationTemplate>>('/notificationtemplates', {
      method: 'POST',
      body: JSON.stringify(template),
    });
  }

  // Report APIs
  async getReports(params?: {
    page?: number;
    limit?: number;
    type?: string;
    category?: string;
    isActive?: boolean;
    isSystemReport?: boolean;
    createdBy?: string;
  }): Promise<ApiResponse<PaginatedResponse<Report>>> {
    const queryParams = new URLSearchParams();
    if (params?.page) queryParams.set('page', params.page.toString());
    if (params?.limit) queryParams.set('limit', params.limit.toString());
    if (params?.type) queryParams.set('type', params.type);
    if (params?.category) queryParams.set('category', params.category);
    if (params?.isActive !== undefined) queryParams.set('isActive', params.isActive.toString());
    if (params?.isSystemReport !== undefined) queryParams.set('isSystemReport', params.isSystemReport.toString());
    if (params?.createdBy) queryParams.set('createdBy', params.createdBy);

    return this.request<ApiResponse<PaginatedResponse<Report>>>(`/reports?${queryParams}`);
  }

  async getReportStats(): Promise<ApiResponse<ReportStats>> {
    return this.request<ApiResponse<ReportStats>>('/reports/stats');
  }

  async createReport(report: CreateReportRequest): Promise<ApiResponse<Report>> {
    return this.request<ApiResponse<Report>>('/reports', {
      method: 'POST',
      body: JSON.stringify(report),
    });
  }

  async getReport(id: string): Promise<ApiResponse<Report>> {
    return this.request<ApiResponse<Report>>(`/reports/${id}`);
  }

  async executeReport(id: string, request: ExecuteReportRequest): Promise<ApiResponse<ReportExecution>> {
    return this.request<ApiResponse<ReportExecution>>(`/reports/${id}/execute`, {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async generateDocumentReport(params?: {
    startDate?: string;
    endDate?: string;
    documentType?: string;
  }): Promise<ApiResponse<ReportExecution>> {
    const queryParams = new URLSearchParams();
    if (params?.startDate) queryParams.set('startDate', params.startDate);
    if (params?.endDate) queryParams.set('endDate', params.endDate);
    if (params?.documentType) queryParams.set('documentType', params.documentType);

    return this.request<ApiResponse<ReportExecution>>(`/reports/quick/document?${queryParams}`, {
      method: 'POST',
    });
  }

  async generateUserActivityReport(params?: {
    startDate?: string;
    endDate?: string;
    userId?: string;
  }): Promise<ApiResponse<ReportExecution>> {
    const queryParams = new URLSearchParams();
    if (params?.startDate) queryParams.set('startDate', params.startDate);
    if (params?.endDate) queryParams.set('endDate', params.endDate);
    if (params?.userId) queryParams.set('userId', params.userId);

    return this.request<ApiResponse<ReportExecution>>(`/reports/quick/user-activity?${queryParams}`, {
      method: 'POST',
    });
  }

  // Audit Log APIs
  async getAuditLogs(params?: {
    userId?: string;
    action?: string;
    entityType?: string;
    entityId?: string;
    startDate?: string;
    endDate?: string;
    ipAddress?: string;
    page?: number;
    limit?: number;
  }): Promise<ApiResponse<PaginatedResponse<AuditLog>>> {
    const queryParams = new URLSearchParams();
    if (params?.userId) queryParams.set('userId', params.userId);
    if (params?.action) queryParams.set('action', params.action);
    if (params?.entityType) queryParams.set('entityType', params.entityType);
    if (params?.entityId) queryParams.set('entityId', params.entityId);
    if (params?.startDate) queryParams.set('startDate', params.startDate);
    if (params?.endDate) queryParams.set('endDate', params.endDate);
    if (params?.ipAddress) queryParams.set('ipAddress', params.ipAddress);
    if (params?.page) queryParams.set('page', params.page.toString());
    if (params?.limit) queryParams.set('limit', params.limit.toString());

    return this.request<ApiResponse<PaginatedResponse<AuditLog>>>(`/audit-logs?${queryParams}`);
  }

  async getAuditLog(id: string): Promise<ApiResponse<AuditLog>> {
    return this.request<ApiResponse<AuditLog>>(`/audit-logs/${id}`);
  }

  async getAuditLogStats(params?: {
    startDate?: string;
    endDate?: string;
  }): Promise<ApiResponse<AuditLogStats>> {
    const queryParams = new URLSearchParams();
    if (params?.startDate) queryParams.set('startDate', params.startDate);
    if (params?.endDate) queryParams.set('endDate', params.endDate);

    return this.request<ApiResponse<AuditLogStats>>(`/audit-logs/stats?${queryParams}`);
  }

  async getMyAuditLogs(params?: {
    page?: number;
    limit?: number;
  }): Promise<ApiResponse<PaginatedResponse<AuditLog>>> {
    const queryParams = new URLSearchParams();
    if (params?.page) queryParams.set('page', params.page.toString());
    if (params?.limit) queryParams.set('limit', params.limit.toString());

    return this.request<ApiResponse<PaginatedResponse<AuditLog>>>(`/audit-logs/my-logs?${queryParams}`);
  }

  async getRecentAuditLogs(count?: number): Promise<ApiResponse<PaginatedResponse<AuditLog>>> {
    const queryParams = new URLSearchParams();
    if (count) queryParams.set('count', count.toString());

    return this.request<ApiResponse<PaginatedResponse<AuditLog>>>(`/audit-logs/recent?${queryParams}`);
  }

  // Data Export APIs
  async getDataExports(params?: {
    page?: number;
    limit?: number;
    type?: string;
    format?: string;
    status?: string;
    requestedBy?: string;
    startDate?: string;
    endDate?: string;
  }): Promise<ApiResponse<PaginatedResponse<DataExport>>> {
    const queryParams = new URLSearchParams();
    if (params?.page) queryParams.set('page', params.page.toString());
    if (params?.limit) queryParams.set('limit', params.limit.toString());
    if (params?.type) queryParams.set('type', params.type);
    if (params?.format) queryParams.set('format', params.format);
    if (params?.status) queryParams.set('status', params.status);
    if (params?.requestedBy) queryParams.set('requestedBy', params.requestedBy);
    if (params?.startDate) queryParams.set('startDate', params.startDate);
    if (params?.endDate) queryParams.set('endDate', params.endDate);

    return this.request<ApiResponse<PaginatedResponse<DataExport>>>(`/dataexports?${queryParams}`);
  }

  async getDataExportStats(): Promise<ApiResponse<DataExportStats>> {
    return this.request<ApiResponse<DataExportStats>>('/dataexports/stats');
  }

  async createDataExport(exportRequest: CreateDataExportRequest): Promise<ApiResponse<DataExport>> {
    return this.request<ApiResponse<DataExport>>('/dataexports', {
      method: 'POST',
      body: JSON.stringify(exportRequest),
    });
  }

  async getDataExport(id: string): Promise<ApiResponse<DataExport>> {
    return this.request<ApiResponse<DataExport>>(`/dataexports/${id}`);
  }

  async executeDataExport(id: string): Promise<ApiResponse<DataExport>> {
    return this.request<ApiResponse<DataExport>>(`/dataexports/${id}/execute`, {
      method: 'POST',
    });
  }

  async downloadDataExport(id: string): Promise<Blob> {
    const response = await fetch(`${this.baseUrl}/dataexports/${id}/download`, {
      headers: {
        ...(this.authToken ? { Authorization: `Bearer ${this.authToken}` } : {})
      }
    });
    
    if (!response.ok) {
      throw new Error('Failed to download export file');
    }
    
    return response.blob();
  }

  async quickExportUsers(params?: {
    format?: string;
    filters?: string;
    columns?: string;
    sendEmail?: boolean;
    emailRecipients?: string;
  }): Promise<ApiResponse<DataExport>> {
    const queryParams = new URLSearchParams();
    if (params?.format) queryParams.set('format', params.format);
    if (params?.filters) queryParams.set('filters', params.filters);
    if (params?.columns) queryParams.set('columns', params.columns);
    if (params?.sendEmail !== undefined) queryParams.set('sendEmail', params.sendEmail.toString());
    if (params?.emailRecipients) queryParams.set('emailRecipients', params.emailRecipients);

    return this.request<ApiResponse<DataExport>>(`/dataexports/quick/users?${queryParams}`, {
      method: 'POST',
    });
  }

  async quickExportEmployees(params?: {
    format?: string;
    filters?: string;
    columns?: string;
    sendEmail?: boolean;
    emailRecipients?: string;
  }): Promise<ApiResponse<DataExport>> {
    const queryParams = new URLSearchParams();
    if (params?.format) queryParams.set('format', params.format);
    if (params?.filters) queryParams.set('filters', params.filters);
    if (params?.columns) queryParams.set('columns', params.columns);
    if (params?.sendEmail !== undefined) queryParams.set('sendEmail', params.sendEmail.toString());
    if (params?.emailRecipients) queryParams.set('emailRecipients', params.emailRecipients);

    return this.request<ApiResponse<DataExport>>(`/dataexports/quick/employees?${queryParams}`, {
      method: 'POST',
    });
  }

  async quickExportDocuments(params?: {
    format?: string;
    filters?: string;
    columns?: string;
    sendEmail?: boolean;
    emailRecipients?: string;
  }): Promise<ApiResponse<DataExport>> {
    const queryParams = new URLSearchParams();
    if (params?.format) queryParams.set('format', params.format);
    if (params?.filters) queryParams.set('filters', params.filters);
    if (params?.columns) queryParams.set('columns', params.columns);
    if (params?.sendEmail !== undefined) queryParams.set('sendEmail', params.sendEmail.toString());
    if (params?.emailRecipients) queryParams.set('emailRecipients', params.emailRecipients);

    return this.request<ApiResponse<DataExport>>(`/dataexports/quick/documents?${queryParams}`, {
      method: 'POST',
    });
  }

  async cancelDataExport(id: string): Promise<ApiResponse<DataExport>> {
    return this.request<ApiResponse<DataExport>>(`/dataexports/${id}/cancel`, {
      method: 'POST',
    });
  }

  // Document APIs
  async generateDocument(request: GenerateDocumentRequest): Promise<ApiResponse<GeneratedDocument>> {
    return this.request<ApiResponse<GeneratedDocument>>('/documents/generate', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  async uploadDocumentTemplate(template: FormData): Promise<ApiResponse<DocumentTemplate>> {
    return this.request<ApiResponse<DocumentTemplate>>('/documents/templates/upload', {
      method: 'POST',
      body: template,
      headers: {
        ...(this.authToken ? { Authorization: `Bearer ${this.authToken}` } : {})
      }
    });
  }

  async getDocumentTemplates(params?: {
    type?: string;
    isActive?: boolean;
  }): Promise<ApiResponse<DocumentTemplate[]>> {
    const queryParams = new URLSearchParams();
    if (params?.type) queryParams.set('type', params.type);
    if (params?.isActive !== undefined) queryParams.set('isActive', params.isActive.toString());

    return this.request<ApiResponse<DocumentTemplate[]>>(`/documents/templates?${queryParams}`);
  }

  async getGeneratedDocuments(params?: {
    page?: number;
    limit?: number;
    documentType?: string;
    generatedBy?: string;
    startDate?: string;
    endDate?: string;
  }): Promise<ApiResponse<PaginatedResponse<GeneratedDocument>>> {
    const queryParams = new URLSearchParams();
    if (params?.page) queryParams.set('page', params.page.toString());
    if (params?.limit) queryParams.set('limit', params.limit.toString());
    if (params?.documentType) queryParams.set('documentType', params.documentType);
    if (params?.generatedBy) queryParams.set('generatedBy', params.generatedBy);
    if (params?.startDate) queryParams.set('startDate', params.startDate);
    if (params?.endDate) queryParams.set('endDate', params.endDate);

    return this.request<ApiResponse<PaginatedResponse<GeneratedDocument>>>(`/documents/generated?${queryParams}`);
  }

  // Dynamic System API Methods
  // Letter Type Management
  async getLetterTypeDefinitions(): Promise<ApiResponse<LetterTypeDefinition[]>> {
    return this.request<ApiResponse<LetterTypeDefinition[]>>('/api/DynamicLetterType');
  }

  async getLetterTypeDefinition(id: string): Promise<ApiResponse<LetterTypeDefinition>> {
    return this.request<ApiResponse<LetterTypeDefinition>>(`/api/DynamicLetterType/${id}`);
  }

  async createLetterTypeDefinition(data: Partial<LetterTypeDefinition>): Promise<ApiResponse<LetterTypeDefinition>> {
    return this.request<ApiResponse<LetterTypeDefinition>>('/api/DynamicLetterType', {
      method: 'POST',
      body: JSON.stringify(data)
    });
  }

  async updateLetterTypeDefinition(id: string, data: Partial<LetterTypeDefinition>): Promise<ApiResponse<LetterTypeDefinition>> {
    return this.request<ApiResponse<LetterTypeDefinition>>(`/api/DynamicLetterType/${id}`, {
      method: 'PUT',
      body: JSON.stringify(data)
    });
  }

  async deleteLetterTypeDefinition(id: string): Promise<ApiResponse<void>> {
    return this.request<ApiResponse<void>>(`/api/DynamicLetterType/${id}`, {
      method: 'DELETE'
    });
  }

  // Dynamic Fields Management
  async getDynamicFields(letterTypeDefinitionId: string): Promise<ApiResponse<DynamicField[]>> {
    return this.request<ApiResponse<DynamicField[]>>(`/api/DynamicField?letterTypeDefinitionId=${letterTypeDefinitionId}`);
  }

  async createDynamicField(data: Partial<DynamicField>): Promise<ApiResponse<DynamicField>> {
    return this.request<ApiResponse<DynamicField>>('/api/DynamicField', {
      method: 'POST',
      body: JSON.stringify(data)
    });
  }

  async updateDynamicField(id: string, data: Partial<DynamicField>): Promise<ApiResponse<DynamicField>> {
    return this.request<ApiResponse<DynamicField>>(`/api/DynamicField/${id}`, {
      method: 'PUT',
      body: JSON.stringify(data)
    });
  }

  async deleteDynamicField(id: string): Promise<ApiResponse<void>> {
    return this.request<ApiResponse<void>>(`/api/DynamicField/${id}`, {
      method: 'DELETE'
    });
  }

  // Excel Upload and Processing
  async uploadDynamicExcelFile(request: ExcelUploadRequest): Promise<ApiResponse<ExcelUploadResponse>> {
    const formData = new FormData();
    formData.append('file', request.file);
    formData.append('letterTypeDefinitionId', request.letterTypeDefinitionId);
    
    if (request.fieldMappings) {
      formData.append('fieldMappings', JSON.stringify(request.fieldMappings));
    }
    
    if (request.options) {
      formData.append('options', JSON.stringify(request.options));
    }

    return this.request<ApiResponse<ExcelUploadResponse>>('/api/DynamicExcel/upload', {
      method: 'POST',
      body: formData
    });
  }

  async getFieldMappingSuggestions(letterTypeDefinitionId: string, excelHeaders: string[]): Promise<ApiResponse<FieldMapping[]>> {
    return this.request<ApiResponse<FieldMapping[]>>('/api/DynamicExcel/field-mapping-suggestions', {
      method: 'POST',
      body: JSON.stringify({ letterTypeDefinitionId, excelHeaders })
    });
  }

  async validateExcelData(letterTypeDefinitionId: string, data: Record<string, unknown>[]): Promise<ApiResponse<{ isValid: boolean; errors: string[]; warnings: string[] }>> {
    return this.request<ApiResponse<{ isValid: boolean; errors: string[]; warnings: string[] }>>('/api/DynamicExcel/validate', {
      method: 'POST',
      body: JSON.stringify({ letterTypeDefinitionId, data })
    });
  }

  // Dynamic Document Generation
  async generateDynamicDocuments(request: DynamicDocumentGenerationRequest): Promise<ApiResponse<DynamicDocumentGenerationResponse>> {
    return this.request<ApiResponse<DynamicDocumentGenerationResponse>>('/api/DynamicDocumentGeneration/generate-bulk', {
      method: 'POST',
      body: JSON.stringify(request)
    });
  }

  async generateSingleDocument(request: Omit<DynamicDocumentGenerationRequest, 'employeeIds'> & { employeeId: string }): Promise<ApiResponse<DynamicDocumentGenerationResponse>> {
    return this.request<ApiResponse<DynamicDocumentGenerationResponse>>('/api/DynamicDocumentGeneration/generate-single', {
      method: 'POST',
      body: JSON.stringify(request)
    });
  }

  async previewDocument(request: Omit<DynamicDocumentGenerationRequest, 'employeeIds'> & { employeeId: string }): Promise<ApiResponse<{ previewUrl: string; documentId: string }>> {
    return this.request<ApiResponse<{ previewUrl: string; documentId: string }>>('/api/DynamicDocumentGeneration/preview', {
      method: 'POST',
      body: JSON.stringify(request)
    });
  }

  async validateDocumentGeneration(letterTypeDefinitionId: string, employeeIds: string[]): Promise<ApiResponse<{ isValid: boolean; errors: string[]; warnings: string[] }>> {
    return this.request<ApiResponse<{ isValid: boolean; errors: string[]; warnings: string[] }>>('/api/DynamicDocumentGeneration/validate', {
      method: 'POST',
      body: JSON.stringify({ letterTypeDefinitionId, employeeIds })
    });
  }

  // Dynamic Email System
  async sendDynamicEmail(request: DynamicEmailRequest): Promise<ApiResponse<DynamicEmailResponse>> {
    return this.request<ApiResponse<DynamicEmailResponse>>('/api/DynamicEmail/send', {
      method: 'POST',
      body: JSON.stringify(request)
    });
  }

  async sendBulkDynamicEmails(request: BulkDynamicEmailRequest): Promise<ApiResponse<BulkDynamicEmailResponse>> {
    return this.request<ApiResponse<BulkDynamicEmailResponse>>('/api/DynamicEmail/send-bulk', {
      method: 'POST',
      body: JSON.stringify(request)
    });
  }

  async sendEmailsForLetterType(
    letterTypeDefinitionId: string,
    options?: {
      signatureId?: string;
      emailTemplateId?: string;
      includeDocumentAttachments?: boolean;
      enableTracking?: boolean;
    }
  ): Promise<ApiResponse<BulkDynamicEmailResponse>> {
    const params = new URLSearchParams();
    if (options?.signatureId) params.append('signatureId', options.signatureId);
    if (options?.emailTemplateId) params.append('emailTemplateId', options.emailTemplateId);
    if (options?.includeDocumentAttachments !== undefined) params.append('includeDocumentAttachments', options.includeDocumentAttachments.toString());
    if (options?.enableTracking !== undefined) params.append('enableTracking', options.enableTracking.toString());

    return this.request<ApiResponse<BulkDynamicEmailResponse>>(`/api/DynamicEmail/send-for-letter-type/${letterTypeDefinitionId}?${params.toString()}`, {
      method: 'POST'
    });
  }

  async generateEmailPreview(request: DynamicEmailRequest): Promise<ApiResponse<{ subject: string; body: string; htmlBody: string; attachments: EmailAttachment[] }>> {
    return this.request<ApiResponse<{ subject: string; body: string; htmlBody: string; attachments: EmailAttachment[] }>>('/api/DynamicEmail/preview', {
      method: 'POST',
      body: JSON.stringify(request)
    });
  }

  async validateEmailSending(letterTypeDefinitionId: string, employeeId: string, additionalFieldData?: Record<string, any>): Promise<ApiResponse<{ isValid: boolean; missingRequiredFields: string[]; invalidFieldValues: string[]; warnings: string[] }>> {
    return this.request<ApiResponse<{ isValid: boolean; missingRequiredFields: string[]; invalidFieldValues: string[]; warnings: string[] }>>(`/api/DynamicEmail/validate/${letterTypeDefinitionId}/${employeeId}`, {
      method: 'POST',
      body: JSON.stringify(additionalFieldData || {})
    });
  }

  async getDynamicEmailTemplates(letterTypeDefinitionId: string): Promise<ApiResponse<{ id: string; name: string; description: string; subject: string; body: string; htmlBody: string; isDefault: boolean; createdAt: string; updatedAt: string }[]>> {
    return this.request<ApiResponse<{ id: string; name: string; description: string; subject: string; body: string; htmlBody: string; isDefault: boolean; createdAt: string; updatedAt: string }[]>>(`/api/DynamicEmail/templates/${letterTypeDefinitionId}`);
  }

  async getEmailHistory(letterTypeDefinitionId: string, pageNumber: number = 1, pageSize: number = 10): Promise<ApiResponse<{ items: EmailJob[]; totalCount: number; pageNumber: number; pageSize: number }>> {
    return this.request<ApiResponse<{ items: EmailJob[]; totalCount: number; pageNumber: number; pageSize: number }>>(`/api/DynamicEmail/history/${letterTypeDefinitionId}?pageNumber=${pageNumber}&pageSize=${pageSize}`);
  }

  async getDynamicEmailStatus(emailJobId: string): Promise<ApiResponse<{ emailJobId: string; status: string; trackingId: string; sendGridMessageId: string; createdAt: string; sentAt?: string; deliveredAt?: string; openedAt?: string; clickedAt?: string; bouncedAt?: string; errorMessage?: string; retryCount: number; lastRetryAt?: string; events: EmailEvent[] }>> {
    return this.request<ApiResponse<{ emailJobId: string; status: string; trackingId: string; sendGridMessageId: string; createdAt: string; sentAt?: string; deliveredAt?: string; openedAt?: string; clickedAt?: string; bouncedAt?: string; errorMessage?: string; retryCount: number; lastRetryAt?: string; events: EmailEvent[] }>>(`/api/DynamicEmail/status/${emailJobId}`);
  }

  async cancelScheduledEmail(emailJobId: string): Promise<ApiResponse<{ success: boolean; message: string }>> {
    return this.request<ApiResponse<{ success: boolean; message: string }>>(`/api/DynamicEmail/cancel/${emailJobId}`, {
      method: 'POST'
    });
  }

  async resendEmail(emailJobId: string): Promise<ApiResponse<DynamicEmailResponse>> {
    return this.request<ApiResponse<DynamicEmailResponse>>(`/api/DynamicEmail/resend/${emailJobId}`, {
      method: 'POST'
    });
  }

  async getEmailAnalytics(letterTypeDefinitionId: string, startDate: string, endDate: string): Promise<ApiResponse<EmailAnalytics>> {
    return this.request<ApiResponse<EmailAnalytics>>(`/api/DynamicEmail/analytics/${letterTypeDefinitionId}?startDate=${startDate}&endDate=${endDate}`);
  }

  async getEmailStats(letterTypeDefinitionId: string): Promise<ApiResponse<{ totalEmails: number; deliveryRate: number; openRate: number; clickRate: number; bounceRate: number; failureRate: number; averageDeliveryTime: number; averageOpenTime: number; averageClickTime: number }>> {
    return this.request<ApiResponse<{ totalEmails: number; deliveryRate: number; openRate: number; clickRate: number; bounceRate: number; failureRate: number; averageDeliveryTime: number; averageOpenTime: number; averageClickTime: number }>>(`/api/DynamicEmail/stats/${letterTypeDefinitionId}`);
  }

  // Real-time Email Tracking
  async getRealTimeEmailStats(letterTypeDefinitionId: string): Promise<ApiResponse<{ letterTypeDefinitionId: string; totalEmails: number; pendingEmails: number; sentEmails: number; deliveredEmails: number; openedEmails: number; clickedEmails: number; bouncedEmails: number; failedEmails: number; lastUpdated: string }>> {
    return this.request<ApiResponse<{ letterTypeDefinitionId: string; totalEmails: number; pendingEmails: number; sentEmails: number; deliveredEmails: number; openedEmails: number; clickedEmails: number; bouncedEmails: number; failedEmails: number; lastUpdated: string }>>(`/api/v1/dynamic-webhooks/stats/${letterTypeDefinitionId}`);
  }

  async getWebhookEvents(emailJobId: string): Promise<ApiResponse<{ id: string; eventType: string; timestamp: string; userAgent?: string; ipAddress?: string; url?: string; reason?: string; data?: string }[]>> {
    return this.request<ApiResponse<{ id: string; eventType: string; timestamp: string; userAgent?: string; ipAddress?: string; url?: string; reason?: string; data?: string }[]>>(`/api/v1/dynamic-webhooks/events/${emailJobId}`);
  }
}

// Types and Interfaces
export interface Employee {
  id: string;
  employeeId: string;
  name: string;
  email: string;
  phone: string;
  department: string;
  designation: string;
  joiningDate: string;
  relievingDate?: string;
  status: 'active' | 'inactive';
  manager: string;
  location: string;
}

export interface CreateEmployeeRequest {
  employeeId: string;
  name: string;
  email: string;
  phone: string;
  department: string;
  designation: string;
  joiningDate: string;
  manager: string;
  location: string;
  salary?: number;
}



export interface DashboardStats {
  totalEmployees?: number;
  activeEmployees?: number;
  newJoiningsThisMonth?: number;
  relievedThisMonth?: number;
  totalProjects?: number;
  activeProjects?: number;
  totalHoursThisMonth?: number;
  billableHours?: number;
  pendingTimesheets?: number;
  approvedTimesheets?: number;
  totalRevenue?: number;
  recentActivities: Activity[];
}

export interface Activity {
  id: string;
  type: string; // Generic type - can be any document type or activity
  employeeName: string;
  employeeId: string;
  status: 'pending' | 'approved' | 'rejected' | 'submitted';
  createdAt: Date;
}

// Additional interfaces for new API methods
export interface CreateUserRequest {
  username: string;
  name: string;
  email: string;
  role: 'admin' | 'er' | 'billing';
  password: string;
  permissions: {
    canAccessER: boolean;
    canAccessBilling: boolean;
    isAdmin: boolean;
  };
}

export interface UpdateUserRequest {
  name?: string;
  email?: string;
  role?: 'admin' | 'er' | 'billing';
  permissions?: {
    canAccessER: boolean;
    canAccessBilling: boolean;
    isAdmin: boolean;
  };
  isActive?: boolean;
}

export interface DocumentTemplate {
  id: string;
  name: string;
  type: string;
  fileName: string;
  fileUrl: string;
  fileSize: number;
  mimeType: string;
  placeholders: string[];
  isActive: boolean;
  version: number;
  createdBy: string;
  createdAt: Date;
  updatedAt: Date;
}

export interface Signature {
  id: string;
  name: string;
  fileName: string;
  fileUrl: string;
  fileSize: number;
  mimeType: string;
  createdBy: string;
  createdAt: Date;
  updatedAt: Date;
}

export interface SendEmailRequest {
  to: string[];
  cc?: string[];
  bcc?: string[];
  subject: string;
  body: string;
  attachments?: string[];
  templateId?: string;
  employeeId?: string;
}

export interface EmailStatus {
  id: string;
  status: 'pending' | 'sent' | 'delivered' | 'failed';
  sentAt?: Date;
  deliveredAt?: Date;
  errorMessage?: string;
  recipient: string;
}

// Dynamic Tab interfaces
export interface DynamicTab {
  id: string;
  name: string;
  description: string;
  letterType: string;
  isActive: boolean;
  templates: TabTemplate[];
  signatures: TabSignature[];
  createdAt: string; // API returns as string
  updatedAt: string; // API returns as string
  createdBy: string;
}

export interface TabTemplate {
  id: string;
  name: string;
  type: string;
  fileName: string;
  fileUrl: string;
  version: number;
  uploadedAt: Date;
  uploadedBy: string;
}

export interface TabSignature {
  id: string;
  name: string;
  fileName: string;
  fileUrl: string;
  uploadedAt: Date;
  uploadedBy: string;
}

export interface CreateDynamicTabRequest {
  name: string;
  description: string;
  letterType: string;
  isActive: boolean;
  metadata?: string;
  templateIds?: string[];
  signatureIds?: string[];
  defaultTemplateId?: string;
  defaultSignatureId?: string;
}

export interface UpdateDynamicTabRequest {
  name?: string;
  description?: string;
  letterType?: string;
  isActive?: boolean;
  metadata?: string;
  templateIds?: string[];
  signatureIds?: string[];
  defaultTemplateId?: string;
  defaultSignatureId?: string;
}

// Document Request interfaces
export interface DocumentRequest {
  id: string;
  employeeId: string;
  documentTemplateId: string;
  documentType: string;
  requestedBy: string;
  status: 'Pending' | 'Approved' | 'Rejected' | 'InProgress';
  metadata: string;
  approvedAt?: string;
  approvedBy?: string;
  rejectionReason?: string;
  effectiveDate?: string;
  generatedDocumentId?: string;
  createdAt: string;
  updatedAt: string;
  // Navigation properties
  employee?: Employee;
  documentTemplate?: DocumentTemplate;
  approver?: UserRole;
  generatedDocument?: GeneratedDocument;
}

export interface CreateDocumentRequestRequest {
  employeeId: string;
  documentTemplateId: string;
  documentType: string;
  requestedBy: string;
  metadata?: string;
  comments?: string;
  effectiveDate?: string;
}

export interface ApproveDocumentRequestRequest {
  comments?: string;
}

export interface DocumentRequestStats {
  totalRequests: number;
  pendingRequests: number;
  approvedRequests: number;
  rejectedRequests: number;
  inProgressRequests: number;
  requestsThisMonth: number;
  requestsLastMonth: number;
  averageProcessingTime: number;
  requestsByType: { [key: string]: number };
  requestsByStatus: { [key: string]: number };
}

// Email Template Types
export interface EmailTemplate {
  id: string;
  name: string;
  subject: string;
  body: string;
  htmlBody: string;
  type: string;
  category: string;
  placeholders: string[];
  isActive: boolean;
  isDefault: boolean;
  description?: string;
  createdBy: string;
  version: number;
  createdAt: string;
  updatedAt: string;
}

export interface CreateEmailTemplateRequest {
  name: string;
  subject: string;
  body: string;
  htmlBody: string;
  type: string;
  category: string;
  placeholders: string[];
  isActive: boolean;
  description?: string;
}

export interface ProcessedEmailTemplate {
  subject: string;
  body: string;
  htmlBody: string;
}

export interface CloneEmailTemplateRequest {
  newName: string;
}

export interface EmailTemplateValidation {
  isValid: boolean;
  errors: string[];
  warnings: string[];
  undeclaredPlaceholders: string[];
}

// File Management Types
export interface FileStorageStatistics {
  totalFiles: number;
  totalStorageUsed: number;
  expiredFiles: number;
  tempFiles: number;
  storageByCategory: Record<string, number>;
}

export interface FileCleanupReport {
  expiredFiles: number;
  orphanedFiles: number;
  tempFiles: number;
  totalSizeToClean: number;
  lastCleanupDate?: string;
}

export interface FileValidationResult {
  isValid: boolean;
  isMalwareFree: boolean;
  metadata: Record<string, any>;
  validationDate: string;
}

// Notification Types
export interface Notification {
  id: string;
  userId: string;
  title: string;
  message: string;
  type: string;
  priority: string;
  isRead: boolean;
  actionUrl?: string;
  entityType?: string;
  entityId?: string;
  metadata?: Record<string, any>;
  expiresAt?: string;
  createdAt: string;
  readAt?: string;
}

export interface CreateNotificationRequest {
  userId: string;
  title: string;
  message: string;
  type: string;
  priority: string;
  actionUrl?: string;
  entityType?: string;
  entityId?: string;
  metadata?: Record<string, any>;
  expiresAt?: string;
}

export interface NotificationStats {
  total: number;
  unread: number;
  byType: Record<string, number>;
  byPriority: Record<string, number>;
}

// Notification Template Types
export interface NotificationTemplate {
  id: string;
  name: string;
  title: string;
  message: string;
  type: string;
  priority: string;
  category: string;
  placeholders: string[];
  isActive: boolean;
  isSystemTemplate: boolean;
  description?: string;
  createdBy: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateNotificationTemplateRequest {
  name: string;
  title: string;
  message: string;
  type: string;
  priority: string;
  category: string;
  placeholders: string[];
  isActive: boolean;
  isSystemTemplate: boolean;
  description?: string;
}

// Report Types
export interface Report {
  id: string;
  name: string;
  description?: string;
  type: string;
  category: string;
  query: string;
  parameters: Record<string, any>;
  template?: string;
  isSystemReport: boolean;
  isActive: boolean;
  schedule?: string;
  emailRecipients?: string[];
  createdBy: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateReportRequest {
  name: string;
  description?: string;
  type: string;
  category: string;
  query: string;
  parameters: Record<string, any>;
  template?: string;
  isSystemReport: boolean;
  isActive: boolean;
  schedule?: string;
  emailRecipients?: string[];
}

export interface ExecuteReportRequest {
  parameters?: Record<string, any>;
  emailRecipients?: string[];
  sendEmail?: boolean;
}

export interface ReportExecution {
  id: string;
  reportId: string;
  status: string;
  startedAt: string;
  completedAt?: string;
  result?: Record<string, unknown>;
  error?: string;
  executedBy: string;
}

export interface ReportStats {
  totalReports: number;
  activeReports: number;
  systemReports: number;
  byType: Record<string, number>;
  byCategory: Record<string, number>;
  recentExecutions: number;
}

// Audit Log Types
export interface AuditLog {
  id: string;
  userId: string;
  userName: string;
  action: string;
  entityType: string;
  entityId: string;
  oldValues?: Record<string, any>;
  newValues?: Record<string, any>;
  ipAddress: string;
  userAgent: string;
  timestamp: string;
}

export interface AuditLogStats {
  totalLogs: number;
  byAction: Record<string, number>;
  byEntityType: Record<string, number>;
  byUser: Record<string, number>;
  recentActivity: number;
}

// Data Export Types
export interface DataExport {
  id: string;
  name: string;
  description?: string;
  type: string;
  format: string;
  status: string;
  entityType: string;
  query: string;
  filters?: Record<string, any>;
  columns?: string[];
  emailRecipients?: string[];
  sendEmail: boolean;
  template?: string;
  isScheduled: boolean;
  schedule?: string;
  fileName?: string;
  filePath?: string;
  fileSize?: number;
  requestedBy: string;
  requestedAt: string;
  completedAt?: string;
  expiresAt?: string;
}

export interface CreateDataExportRequest {
  name: string;
  description?: string;
  type: string;
  format: string;
  entityType: string;
  query: string;
  filters?: Record<string, any>;
  columns?: string[];
  emailRecipients?: string[];
  sendEmail: boolean;
  template?: string;
  isScheduled: boolean;
  schedule?: string;
}

export interface DataExportStats {
  totalExports: number;
  byType: Record<string, number>;
  byFormat: Record<string, number>;
  byStatus: Record<string, number>;
  totalSize: number;
  recentExports: number;
}

// Document Types
export interface GeneratedDocument {
  id: string;
  templateId: string;
  templateName: string;
  documentType: string;
  fileName: string;
  filePath: string;
  fileSize: number;
  generatedBy: string;
  generatedAt: string;
  metadata?: Record<string, any>;
}

export interface GenerateDocumentRequest {
  templateId: string;
  employeeId: string;
  data: Record<string, any>;
  outputFormat?: string;
}

// Dynamic Tab Operation Types
export interface GenerateDocumentsForTabRequest {
  employeeIds: string[];
  templateId: string;
  signatureId?: string;
  placeholderData?: Record<string, any>;
}

export interface PreviewDocumentsForTabRequest {
  employeeIds: string[];
  templateId: string;
  signatureId?: string;
  placeholderData?: Record<string, any>;
}

// Bulk Email Types
export interface SendBulkEmailRequest {
  employees: BulkEmailEmployee[];
  subject: string;
  body: string;
  attachments?: string[];
  emailTemplateId?: string;
}

export interface BulkEmailEmployee {
  employeeId: string;
  employeeName: string;
  employeeEmail: string;
  documentId?: string;
}

// Export singleton instance
export const apiService = new ApiService();
export default apiService;