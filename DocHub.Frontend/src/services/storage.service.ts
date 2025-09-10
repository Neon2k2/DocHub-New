export class StorageService {
  private static instance: StorageService;
  
  static getInstance(): StorageService {
    if (!StorageService.instance) {
      StorageService.instance = new StorageService();
    }
    return StorageService.instance;
  }

  // Generic storage methods
  setItem<T>(key: string, value: T): void {
    try {
      const serializedValue = JSON.stringify(value);
      localStorage.setItem(key, serializedValue);
    } catch (error) {
      console.error('Failed to save to localStorage:', error);
    }
  }

  getItem<T>(key: string, defaultValue?: T): T | null {
    try {
      const item = localStorage.getItem(key);
      if (item === null) return defaultValue || null;
      return JSON.parse(item) as T;
    } catch (error) {
      console.error('Failed to read from localStorage:', error);
      return defaultValue || null;
    }
  }

  removeItem(key: string): void {
    try {
      localStorage.removeItem(key);
    } catch (error) {
      console.error('Failed to remove from localStorage:', error);
    }
  }

  clear(): void {
    try {
      localStorage.clear();
    } catch (error) {
      console.error('Failed to clear localStorage:', error);
    }
  }

  // Application-specific storage methods
  saveTransferData(data: any[]): void {
    this.setItem('transfer_requests', data);
  }

  getTransferData(): any[] {
    return this.getItem('transfer_requests', []);
  }

  saveAdminSettings(settings: any): void {
    this.setItem('admin_settings', settings);
  }

  getAdminSettings(): any {
    return this.getItem('admin_settings', {});
  }

  saveUserPreferences(preferences: any): void {
    this.setItem('user_preferences', preferences);
  }

  getUserPreferences(): any {
    return this.getItem('user_preferences', {});
  }

  saveFormData(formId: string, data: any): void {
    this.setItem(`form_${formId}`, data);
  }

  getFormData(formId: string): any {
    return this.getItem(`form_${formId}`, {});
  }

  clearFormData(formId: string): void {
    this.removeItem(`form_${formId}`);
  }

  // Session storage for temporary data
  setSessionItem<T>(key: string, value: T): void {
    try {
      const serializedValue = JSON.stringify(value);
      sessionStorage.setItem(key, serializedValue);
    } catch (error) {
      console.error('Failed to save to sessionStorage:', error);
    }
  }

  getSessionItem<T>(key: string, defaultValue?: T): T | null {
    try {
      const item = sessionStorage.getItem(key);
      if (item === null) return defaultValue || null;
      return JSON.parse(item) as T;
    } catch (error) {
      console.error('Failed to read from sessionStorage:', error);
      return defaultValue || null;
    }
  }

  removeSessionItem(key: string): void {
    try {
      sessionStorage.removeItem(key);
    } catch (error) {
      console.error('Failed to remove from sessionStorage:', error);
    }
  }
}

export const storageService = StorageService.getInstance();
