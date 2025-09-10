# DocHub API Documentation

## Overview
DocHub is a generic, dynamic document generation and email management system that can handle any letter type without hardcoding. The system supports both database and Excel data sources, with real-time email tracking and comprehensive template management.

## Architecture

### Core Components
1. **Dynamic Letter Type Management** - Create and manage any letter type dynamically
2. **Excel Processing Engine** - Upload and process Excel files with automatic field mapping
3. **Document Generation Engine** - Generate documents using Syncfusion with pixel-perfect signature insertion
4. **Email Management System** - Send emails with SendGrid integration and real-time tracking
5. **Webhook System** - Real-time status updates via SendGrid webhooks
6. **File Storage System** - Secure file storage with cleanup and validation

## API Endpoints

### 1. Dynamic Letter Type Management

#### GET /api/DynamicLetterType
Get all letter type definitions
```json
{
  "success": true,
  "data": [
    {
      "id": "guid",
      "typeKey": "promotion_letter",
      "displayName": "Promotion Letter",
      "description": "Letter for employee promotions",
      "fieldConfiguration": "{\"requiredFields\":[\"employeeName\",\"newPosition\"]}",
      "isActive": true,
      "fields": [...],
      "createdAt": "2024-01-01T00:00:00Z",
      "updatedAt": "2024-01-01T00:00:00Z"
    }
  ]
}
```

#### POST /api/DynamicLetterType
Create a new letter type definition
```json
{
  "typeKey": "promotion_letter",
  "displayName": "Promotion Letter",
  "description": "Letter for employee promotions",
  "fieldConfiguration": "{\"requiredFields\":[\"employeeName\",\"newPosition\"]}",
  "isActive": true,
  "moduleId": "guid"
}
```

#### PUT /api/DynamicLetterType/{id}
Update a letter type definition

#### DELETE /api/DynamicLetterType/{id}
Delete a letter type definition

### 2. Dynamic Field Management

#### GET /api/DynamicField?letterTypeDefinitionId={id}
Get fields for a specific letter type
```json
{
  "success": true,
  "data": [
    {
      "id": "guid",
      "letterTypeDefinitionId": "guid",
      "fieldKey": "employeeName",
      "fieldName": "Employee Name",
      "displayName": "Employee Full Name",
      "fieldType": "Text",
      "isRequired": true,
      "validationRules": "{\"minLength\":2,\"maxLength\":100}",
      "defaultValue": "",
      "order": 1
    }
  ]
}
```

#### POST /api/DynamicField
Create a new dynamic field
```json
{
  "letterTypeDefinitionId": "guid",
  "fieldKey": "employeeName",
  "fieldName": "Employee Name",
  "displayName": "Employee Full Name",
  "fieldType": "Text",
  "isRequired": true,
  "validationRules": "{\"minLength\":2,\"maxLength\":100}",
  "defaultValue": "",
  "order": 1
}
```

### 3. Excel Processing

#### POST /api/DynamicExcel/upload
Upload and process Excel file
```json
{
  "letterTypeDefinitionId": "guid",
  "file": "multipart/form-data",
  "fieldMappings": [
    {
      "excelColumn": "Employee Name",
      "fieldKey": "employeeName",
      "fieldType": "Text",
      "isRequired": true
    }
  ],
  "options": {
    "skipEmptyRows": true,
    "validateData": true,
    "generateTemplates": false,
    "maxRows": 1000
  }
}
```

#### POST /api/DynamicExcel/field-mapping-suggestions
Get field mapping suggestions
```json
{
  "letterTypeDefinitionId": "guid",
  "excelHeaders": ["Employee Name", "Department", "Position"]
}
```

#### POST /api/DynamicExcel/validate
Validate Excel data
```json
{
  "letterTypeDefinitionId": "guid",
  "data": [
    {
      "employeeName": "John Doe",
      "department": "IT",
      "position": "Senior Developer"
    }
  ]
}
```

### 4. Document Generation

#### POST /api/DynamicDocumentGeneration/generate-bulk
Generate documents for multiple employees
```json
{
  "letterTypeDefinitionId": "guid",
  "employeeIds": ["guid1", "guid2"],
  "templateId": "guid",
  "signatureId": "guid",
  "includeDocumentAttachments": true,
  "additionalFieldData": {
    "companyName": "DocHub Technologies",
    "currentDate": "2024-01-01"
  }
}
```

#### POST /api/DynamicDocumentGeneration/generate-single
Generate document for single employee
```json
{
  "letterTypeDefinitionId": "guid",
  "employeeId": "guid",
  "templateId": "guid",
  "signatureId": "guid",
  "includeDocumentAttachments": true,
  "additionalFieldData": {...}
}
```

#### POST /api/DynamicDocumentGeneration/preview
Preview document before generation
```json
{
  "letterTypeDefinitionId": "guid",
  "employeeId": "guid",
  "templateId": "guid",
  "signatureId": "guid",
  "additionalFieldData": {...}
}
```

### 5. Email Management

#### POST /api/DynamicEmail/send
Send single email
```json
{
  "letterTypeDefinitionId": "guid",
  "employeeId": "guid",
  "documentId": "guid",
  "signatureId": "guid",
  "emailTemplateId": "guid",
  "customSubject": "Promotion Letter - {{employeeName}}",
  "customBody": "Congratulations on your promotion!",
  "ccRecipients": ["manager@company.com"],
  "bccRecipients": ["hr@company.com"],
  "additionalFieldData": {...},
  "includeDocumentAttachment": true,
  "additionalAttachments": [...],
  "priority": 1,
  "enableTracking": true,
  "sendImmediately": true
}
```

#### POST /api/DynamicEmail/send-bulk
Send bulk emails
```json
{
  "letterTypeDefinitionId": "guid",
  "employeeIds": ["guid1", "guid2"],
  "employeeDocumentIds": {
    "guid1": "doc1",
    "guid2": "doc2"
  },
  "signatureId": "guid",
  "emailTemplateId": "guid",
  "customSubjectTemplate": "Promotion Letter - {{employeeName}}",
  "customBodyTemplate": "Congratulations {{employeeName}} on your promotion to {{newPosition}}!",
  "ccRecipients": ["manager@company.com"],
  "bccRecipients": ["hr@company.com"],
  "employeeFieldData": {
    "guid1": {"newPosition": "Senior Developer"},
    "guid2": {"newPosition": "Team Lead"}
  },
  "includeDocumentAttachments": true,
  "additionalAttachments": [...],
  "priority": 1,
  "enableTracking": true,
  "sendImmediately": true,
  "maxEmailsPerMinute": 100
}
```

#### GET /api/DynamicEmail/status/{emailJobId}
Get email status
```json
{
  "success": true,
  "data": {
    "emailJobId": "guid",
    "status": "delivered",
    "trackingId": "tracking123",
    "sendGridMessageId": "sg123",
    "createdAt": "2024-01-01T00:00:00Z",
    "sentAt": "2024-01-01T00:01:00Z",
    "deliveredAt": "2024-01-01T00:02:00Z",
    "openedAt": "2024-01-01T00:05:00Z",
    "events": [...]
  }
}
```

### 6. Real-time Webhooks

#### POST /api/v1/dynamic-webhooks/sendgrid
SendGrid webhook endpoint for real-time status updates
```json
{
  "email": "employee@company.com",
  "timestamp": 1640995200,
  "event": "delivered",
  "sg_message_id": "sg123",
  "sg_event_id": "event123",
  "sg_template_id": "template123",
  "sg_template_name": "Promotion Letter"
}
```

#### GET /api/v1/dynamic-webhooks/stats/{letterTypeDefinitionId}
Get real-time email statistics
```json
{
  "success": true,
  "data": {
    "letterTypeDefinitionId": "guid",
    "totalEmails": 100,
    "pendingEmails": 5,
    "sentEmails": 80,
    "deliveredEmails": 75,
    "openedEmails": 60,
    "clickedEmails": 10,
    "bouncedEmails": 2,
    "failedEmails": 3,
    "lastUpdated": "2024-01-01T00:00:00Z"
  }
}
```

### 7. Template Management

#### GET /api/Templates
Get all document templates
```json
{
  "success": true,
  "data": [
    {
      "id": "guid",
      "name": "Promotion Letter Template",
      "type": "promotion_letter",
      "fileName": "promotion_template.docx",
      "fileUrl": "https://storage.../templates/promotion_template.docx",
      "fileSize": 1024000,
      "mimeType": "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
      "placeholders": ["{EMPLOYEE_NAME}", "{NEW_POSITION}", "{EFFECTIVE_DATE}"],
      "isActive": true,
      "version": 1,
      "createdBy": "admin",
      "createdAt": "2024-01-01T00:00:00Z",
      "updatedAt": "2024-01-01T00:00:00Z"
    }
  ]
}
```

#### POST /api/Templates/upload
Upload new template
```json
{
  "file": "multipart/form-data",
  "name": "Promotion Letter Template",
  "type": "promotion_letter",
  "description": "Template for promotion letters"
}
```

### 8. Signature Management

#### GET /api/Signatures
Get all signatures
```json
{
  "success": true,
  "data": [
    {
      "id": "guid",
      "name": "CEO Signature",
      "fileName": "ceo_signature.png",
      "fileUrl": "https://storage.../signatures/ceo_signature.png",
      "fileSize": 51200,
      "mimeType": "image/png",
      "createdBy": "admin",
      "createdAt": "2024-01-01T00:00:00Z",
      "updatedAt": "2024-01-01T00:00:00Z"
    }
  ]
}
```

#### POST /api/Signatures/upload
Upload new signature
```json
{
  "file": "multipart/form-data",
  "name": "CEO Signature",
  "description": "CEO digital signature"
}
```

#### POST /api/Signatures/{id}/process
Process signature (remove watermark, optimize)
```json
{
  "removeWatermark": true,
  "optimizeForWeb": true,
  "resizeTo": {
    "width": 200,
    "height": 80
  }
}
```

### 9. File Storage

#### GET /api/FileStorage/statistics
Get file storage statistics
```json
{
  "success": true,
  "data": {
    "totalFiles": 1000,
    "totalStorageUsed": 1073741824,
    "expiredFiles": 50,
    "tempFiles": 25,
    "storageByCategory": {
      "templates": 500000000,
      "signatures": 100000000,
      "generated": 400000000,
      "temp": 73741824
    }
  }
}
```

#### POST /api/FileStorage/cleanup/expired
Cleanup expired files

#### POST /api/FileStorage/cleanup/orphaned
Cleanup orphaned files

#### POST /api/FileStorage/cleanup/temp
Cleanup temporary files

### 10. Employee Management

#### GET /api/Employees
Get employees with pagination and filtering
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "guid",
        "employeeId": "EMP001",
        "name": "John Doe",
        "email": "john.doe@company.com",
        "phone": "+1234567890",
        "department": "IT",
        "designation": "Senior Developer",
        "joiningDate": "2020-01-01",
        "relievingDate": null,
        "status": "active",
        "manager": "Jane Smith",
        "location": "New York"
      }
    ],
    "pagination": {
      "currentPage": 1,
      "totalPages": 10,
      "totalRecords": 100,
      "hasNext": true,
      "hasPrevious": false
    }
  }
}
```

## Data Models

### LetterTypeDefinition
```csharp
public class LetterTypeDefinition
{
    public Guid Id { get; set; }
    public string TypeKey { get; set; }
    public string DisplayName { get; set; }
    public string Description { get; set; }
    public string? FieldConfiguration { get; set; }
    public bool IsActive { get; set; }
    public Guid? ModuleId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public virtual ICollection<DynamicField> Fields { get; set; }
    public virtual Module? Module { get; set; }
}
```

### DynamicField
```csharp
public class DynamicField
{
    public Guid Id { get; set; }
    public Guid LetterTypeDefinitionId { get; set; }
    public string FieldKey { get; set; }
    public string FieldName { get; set; }
    public string DisplayName { get; set; }
    public string FieldType { get; set; }
    public bool IsRequired { get; set; }
    public string? ValidationRules { get; set; }
    public string? DefaultValue { get; set; }
    public int Order { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### GeneratedDocument
```csharp
public class GeneratedDocument
{
    public Guid Id { get; set; }
    public Guid LetterTypeDefinitionId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid? TemplateId { get; set; }
    public Guid? SignatureId { get; set; }
    public string FileName { get; set; }
    public string FilePath { get; set; }
    public string? DownloadUrl { get; set; }
    public long FileSize { get; set; }
    public string GeneratedBy { get; set; }
    public DateTime GeneratedAt { get; set; }
    public string? Metadata { get; set; }
    public Guid? ModuleId { get; set; }
}
```

### EmailJob
```csharp
public class EmailJob
{
    public Guid Id { get; set; }
    public string EmployeeId { get; set; }
    public Guid? DocumentId { get; set; }
    public Guid? EmailTemplateId { get; set; }
    public string Subject { get; set; }
    public string Content { get; set; }
    public string Attachments { get; set; }
    public string Status { get; set; }
    public string? TrackingId { get; set; }
    public string? SendGridMessageId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? OpenedAt { get; set; }
    public DateTime? ClickedAt { get; set; }
    public DateTime? BouncedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public DateTime? LastRetryAt { get; set; }
}
```

## Authentication

All API endpoints require JWT authentication. Include the token in the Authorization header:

```
Authorization: Bearer <your-jwt-token>
```

## Error Handling

All API responses follow this format:

```json
{
  "success": true/false,
  "data": {...},
  "error": {
    "code": "ERROR_CODE",
    "message": "Human readable error message",
    "details": "Additional error details"
  }
}
```

## Rate Limiting

- Document generation: 10 requests per minute per user
- Email sending: 100 emails per minute per user
- File uploads: 5 files per minute per user

## Webhooks

The system supports real-time webhooks for:
- Email delivery status updates
- Document generation completion
- File processing completion
- Error notifications

Webhook payloads are signed using HMAC-SHA256 for security.

## Security

- All file uploads are scanned for malware
- File access is restricted by user permissions
- Sensitive data is encrypted at rest
- API endpoints are rate limited
- CORS is configured for specific origins

## Monitoring

The system provides comprehensive monitoring for:
- API response times
- Error rates
- File storage usage
- Email delivery rates
- User activity

## Support

For API support and questions, contact the development team or refer to the internal documentation.