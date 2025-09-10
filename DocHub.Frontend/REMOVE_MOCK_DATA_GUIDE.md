# Remove Mock Data from Frontend - ER Tab

## 🚨 Critical Mock Data to Remove

### 1. Document Service (`src/services/document.service.ts`)

#### Remove Dummy Document Generation (Lines 148-189)
```typescript
// REMOVE THIS ENTIRE BLOCK
// Dummy document generation
const placeholderData = {
  EMPLOYEE_NAME: employee.name,
  // ... rest of dummy data
};

const content = `
  <div style="font-family: Arial, sans-serif; padding: 20px;">
    <h1>Experience Letter</h1>
    // ... rest of hardcoded HTML
  </div>
`;

return Promise.resolve({
  id: Date.now().toString(),
  // ... rest of dummy response
});
```

**Replace with:**
```typescript
// Real API call
const response = await apiService.request<{ document: GeneratedDocument }>(
  '/er/documents/generate', {
    method: 'POST',
    body: JSON.stringify({
      templateId,
      employeeId: employee.id,
      signatureId,
      additionalData
    })
  }
);
return response.document;
```

#### Remove Dummy Email Templates (Lines 210-226)
```typescript
// REMOVE THIS ENTIRE BLOCK
async getEmailTemplates(): Promise<EmailTemplate[]> {
  return [
    {
      id: '1',
      name: 'Experience Letter Email',
      // ... rest of hardcoded data
    }
  ];
}
```

**Replace with:**
```typescript
async getEmailTemplates(): Promise<EmailTemplate[]> {
  const response = await apiService.request<ApiResponse<EmailTemplate[]>>('/er/email-templates');
  return response.success && response.data ? response.data : [];
}
```

#### Remove Dummy Email Jobs (Lines 256-289)
```typescript
// REMOVE THIS ENTIRE BLOCK
async getEmailJobs(params?: {...}): Promise<EmailJob[]> {
  // Dummy email jobs
  const dummyJobs: EmailJob[] = [
    {
      id: '1',
      employeeId: 'EMP001',
      // ... rest of hardcoded data
    }
  ];
  return Promise.resolve(dummyJobs);
}
```

**Replace with:**
```typescript
async getEmailJobs(params?: {...}): Promise<EmailJob[]> {
  const queryParams = new URLSearchParams();
  // Add params to query string
  const response = await apiService.request<ApiResponse<EmailJob[]>>(`/er/email-jobs?${queryParams}`);
  return response.success && response.data ? response.data : [];
}
```

### 2. Transfer Letter Component (`src/components/er/TransferLetter.tsx`)

#### Remove Mock Bulk Upload (Lines 218-243)
```typescript
// REMOVE THIS ENTIRE BLOCK
// Mock parsing of uploaded file
const mockNewTransfers: TransferData[] = [
  {
    id: Date.now().toString(),
    name: 'Uploaded Employee 1',
    // ... rest of hardcoded data
  }
];
```

**Replace with:**
```typescript
// Real file upload and processing
const formData = new FormData();
formData.append('file', file);
formData.append('type', 'transfer_letter');

const response = await apiService.request<ApiResponse<TransferData[]>>('/er/transfer-letters/bulk-upload', {
  method: 'POST',
  body: formData
});

if (response.success && response.data) {
  const updatedData = [...transferData, ...response.data];
  saveTransferData(updatedData);
  notify.success(`${response.data.length} transfer requests uploaded successfully`);
}
```

### 3. Billing Tab Mock Data

#### Remove Mock Employee Data (`src/components/billing/HCLTimesheet.tsx` Lines 35-50)
```typescript
// REMOVE THIS ENTIRE BLOCK
// Mock employee data
const employees: Employee[] = [
  {
    id: '1',
    name: 'John Doe',
    // ... rest of hardcoded data
  }
];
```

**Replace with:**
```typescript
// Real API call
const [employees, setEmployees] = useState<Employee[]>([]);

useEffect(() => {
  const fetchEmployees = async () => {
    try {
      const response = await apiService.getEmployees();
      if (response.success && response.data) {
        setEmployees(response.data.items);
      }
    } catch (error) {
      console.error('Failed to fetch employees:', error);
    }
  };
  
  fetchEmployees();
}, []);
```

#### Remove Dummy Timesheet Operations (`src/hooks/useTimesheets.ts`)
```typescript
// REMOVE ALL DUMMY IMPLEMENTATIONS
// Replace with real API calls
const submitTimesheet = async (timesheetId: string) => {
  try {
    await apiService.submitTimesheet(timesheetId);
    // Update local state after successful API call
    setTimesheets(prev => prev.map(timesheet => 
      timesheet.id === timesheetId 
        ? { ...timesheet, status: 'submitted' as const, submittedAt: new Date() }
        : timesheet
    ));
  } catch (err: any) {
    throw new Error(err.message || 'Failed to submit timesheet');
  }
};
```

### 4. Angular Components (Legacy - Remove Entirely)

#### Remove Dummy Users (`src/src/app/components/auth/login.component.ts`)
- Remove lines 207-250 (dummyUsers array)
- Remove lines 51-109 (dummy authentication logic)

#### Remove Dummy Auth Service (`src/src/app/services/auth.service.ts`)
- Remove lines 26-80 (dummyUsers array)
- Remove lines 93-120 (dummy authentication logic)

## 🔧 Implementation Steps

1. **Remove Mock Data**: Delete all hardcoded data arrays and dummy implementations
2. **Add Real API Calls**: Replace with actual API service calls
3. **Add Error Handling**: Implement proper error handling for API failures
4. **Add Loading States**: Show loading indicators during API calls
5. **Test Integration**: Verify all components work with real backend data

## 📋 Checklist

- [ ] Document Service - Remove dummy document generation
- [ ] Document Service - Remove dummy email templates
- [ ] Document Service - Remove dummy email jobs
- [ ] Transfer Letter - Remove mock bulk upload
- [ ] HCL Timesheet - Remove mock employee data
- [ ] Timesheet Hooks - Remove dummy CRUD operations
- [ ] Angular Components - Remove dummy users and auth
- [ ] Add real API integration
- [ ] Add proper error handling
- [ ] Test with real backend

## ⚠️ Important Notes

- **Backup First**: Make sure to backup your current code before making changes
- **Test Incrementally**: Remove mock data one component at a time and test
- **API Endpoints**: Ensure all required API endpoints exist in the backend
- **Error Handling**: Add proper error handling for failed API calls
- **Loading States**: Add loading indicators for better UX
