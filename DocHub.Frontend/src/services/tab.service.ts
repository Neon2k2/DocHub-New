import { apiService, LetterTypeDefinition, DynamicField, FieldType } from './api.service';

export interface DynamicTab {
  id: string;
  name: string;
  description: string;
  letterType: string;
  isActive: boolean;
  createdAt: string; // API returns as string
  updatedAt: string; // API returns as string
  metadata: {
    templateConfig?: any;
    defaultSignature?: string;
    defaultTemplate?: string;
    emailConfig?: any;
    customFields?: any[];
  };
  // New dynamic properties
  letterTypeDefinition?: LetterTypeDefinition;
  fields?: DynamicField[];
}

export interface TabTemplate {
  id: string;
  name: string;
  content: string;
  placeholders: string[];
  createdAt: Date;
}

export interface TabSignature {
  id: string;
  name: string;
  imageUrl: string;
  description?: string;
  createdAt: Date;
}

class TabService {

  private templates: TabTemplate[] = [];

  private signatures: TabSignature[] = [
    {
      id: 'ceo_signature',
      name: 'CEO Signature',
      imageUrl: 'data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMjAwIiBoZWlnaHQ9IjgwIiB2aWV3Qm94PSIwIDAgMjAwIDgwIiBmaWxsPSJub25lIiB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciPgo8cmVjdCB3aWR0aD0iMjAwIiBoZWlnaHQ9IjgwIiBmaWxsPSIjRjFGNUY5Ii8+CjxwYXRoIGQ9Ik0xMDAgNDBMMTA0IDQ0TDEwMCA0OEw5NiA0NFoiIGZpbGw9IiM5Q0E5QjQiLz4KPHN0cm9rZSB3aWR0aD0iMiIgc3Ryb2tlPSIjOUNBOUI0IiBkPSJNODAgNDBIMTIwIi8+Cjx0ZXh0IHg9IjEwMCIgeT0iNjAiIHRleHQtYW5jaG9yPSJtaWRkbGUiIGZvbnQtZmFtaWx5PSJBcmlhbCwgc2Fucy1zZXJpZiIgZm9udC1zaXplPSIxMiIgZmlsbD0iIzlDQTlCNCI+Q0VPIFNpZ25hdHVyZTwvdGV4dD4KPC9zdmc+Cg==',
      description: 'CEO Digital Signature',
      createdAt: new Date('2024-01-01')
    },
    {
      id: 'hr_signature',
      name: 'HR Manager Signature',
      imageUrl: 'data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMjAwIiBoZWlnaHQ9IjgwIiB2aWV3Qm94PSIwIDAgMjAwIDgwIiBmaWxsPSJub25lIiB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciPgo8cmVjdCB3aWR0aD0iMjAwIiBoZWlnaHQ9IjgwIiBmaWxsPSIjRjFGNUY5Ii8+CjxwYXRoIGQ9Ik0xMDAgNDBMMTA0IDQ0TDEwMCA0OEw5NiA0NFoiIGZpbGw9IiM5Q0E5QjQiLz4KPHN0cm9rZSB3aWR0aD0iMiIgc3Ryb2tlPSIjOUNBOUI0IiBkPSJNODAgNDBIMTIwIi8+Cjx0ZXh0IHg9IjEwMCIgeT0iNjAiIHRleHQtYW5jaG9yPSJtaWRkbGUiIGZvbnQtZmFtaWx5PSJBcmlhbCwgc2Fucy1zZXJpZiIgZm9udC1zaXplPSIxMiIgZmlsbD0iIzlDQTlCNCI+SFIgU2lnbmF0dXJlPC90ZXh0Pgo8L3N2Zz4K',
      description: 'HR Manager Digital Signature',
      createdAt: new Date('2024-01-01')
    }
  ];



  // Tab Management - Now using Dynamic Letter Types
  async getTabs(): Promise<DynamicTab[]> {
    try {
      const response = await apiService.getLetterTypeDefinitions();
      if (response.success && response.data) {
        // Convert LetterTypeDefinition to DynamicTab format
        return response.data.map(letterType => ({
          id: letterType.id,
          name: letterType.displayName,
          description: letterType.description,
          letterType: letterType.typeKey,
          isActive: letterType.isActive,
          createdAt: letterType.createdAt,
          updatedAt: letterType.updatedAt,
          metadata: {
            templateConfig: letterType.fieldConfiguration ? JSON.parse(letterType.fieldConfiguration) : {},
            customFields: letterType.fields || []
          },
          letterTypeDefinition: letterType,
          fields: letterType.fields || []
        }));
      }
      return [];
    } catch (error) {
      console.error('Failed to load tabs:', error);
      return [];
    }
  }

  async getActiveTabs(): Promise<DynamicTab[]> {
    try {
      const tabs = await this.getTabs();
      return tabs.filter(tab => tab.isActive);
    } catch (error) {
      console.error('Failed to load active tabs:', error);
      return [];
    }
  }

  async getActiveTabById(id: string): Promise<DynamicTab | null> {
    try {
      const response = await apiService.getLetterTypeDefinition(id);
      if (response.success && response.data && response.data.isActive) {
        const letterType = response.data;
        return {
          id: letterType.id,
          name: letterType.displayName,
          description: letterType.description,
          letterType: letterType.typeKey,
          isActive: letterType.isActive,
          createdAt: letterType.createdAt,
          updatedAt: letterType.updatedAt,
          metadata: {
            templateConfig: letterType.fieldConfiguration ? JSON.parse(letterType.fieldConfiguration) : {},
            customFields: letterType.fields || []
          },
          letterTypeDefinition: letterType,
          fields: letterType.fields || []
        };
      }
      return null;
    } catch (error) {
      console.error('Failed to get tab by ID:', error);
      return null;
    }
  }

  async createTab(tabData: Omit<DynamicTab, 'id' | 'createdAt' | 'updatedAt'>): Promise<DynamicTab> {
    try {
      // Transform the data to match LetterTypeDefinition format
      const createRequest = {
        typeKey: tabData.letterType,
        displayName: tabData.name,
        description: tabData.description,
        isActive: tabData.isActive,
        fieldConfiguration: tabData.metadata?.templateConfig ? JSON.stringify(tabData.metadata.templateConfig) : undefined
      };

      const response = await apiService.createLetterTypeDefinition(createRequest);
      if (response.success && response.data) {
        console.log('Created new letter type:', response.data.displayName);
        // Convert back to DynamicTab format
        const letterType = response.data;
        return {
          id: letterType.id,
          name: letterType.displayName,
          description: letterType.description,
          letterType: letterType.typeKey,
          isActive: letterType.isActive,
          createdAt: letterType.createdAt,
          updatedAt: letterType.updatedAt,
          metadata: {
            templateConfig: letterType.fieldConfiguration ? JSON.parse(letterType.fieldConfiguration) : {},
            customFields: letterType.fields || []
          },
          letterTypeDefinition: letterType,
          fields: letterType.fields || []
        };
      }
      throw new Error(response.error?.message || 'Failed to create letter type');
    } catch (error) {
      console.error('Failed to create tab:', error);
      throw error;
    }
  }

  async updateTab(id: string, updates: Partial<DynamicTab>): Promise<DynamicTab | null> {
    try {
      // Transform the data to match LetterTypeDefinition format
      const updateRequest: any = {};
      
      if (updates.name) updateRequest.displayName = updates.name;
      if (updates.description) updateRequest.description = updates.description;
      if (updates.letterType) updateRequest.typeKey = updates.letterType;
      if (updates.isActive !== undefined) updateRequest.isActive = updates.isActive;
      
      if (updates.metadata?.templateConfig) {
        updateRequest.fieldConfiguration = JSON.stringify(updates.metadata.templateConfig);
      }

      const response = await apiService.updateLetterTypeDefinition(id, updateRequest);
      if (response.success && response.data) {
        console.log('Updated letter type:', response.data.displayName);
        // Convert back to DynamicTab format
        const letterType = response.data;
        return {
          id: letterType.id,
          name: letterType.displayName,
          description: letterType.description,
          letterType: letterType.typeKey,
          isActive: letterType.isActive,
          createdAt: letterType.createdAt,
          updatedAt: letterType.updatedAt,
          metadata: {
            templateConfig: letterType.fieldConfiguration ? JSON.parse(letterType.fieldConfiguration) : {},
            customFields: letterType.fields || []
          },
          letterTypeDefinition: letterType,
          fields: letterType.fields || []
        };
      }
      return null;
    } catch (error) {
      console.error('Failed to update tab:', error);
      return null;
    }
  }

  async deleteTab(id: string): Promise<boolean> {
    try {
      const response = await apiService.deleteLetterTypeDefinition(id);
      if (response.success) {
        console.log('Deleted letter type with ID:', id);
        return true;
      }
      return false;
    } catch (error) {
      console.error('Failed to delete tab:', error);
      return false;
    }
  }

  // Additional utility methods for tab management

  /**
   * Permanently remove a tab from storage
   */
  async permanentlyDeleteTab(id: string): Promise<boolean> {
    try {
      const response = await apiService.deleteDynamicTab(id);
      if (response.success) {
        console.log('Permanently deleted tab:', id);
        return true;
      }
      return false;
    } catch (error) {
      console.error('Failed to permanently delete tab:', error);
      return false;
    }
  }

  /**
   * Get all tabs including inactive ones
   */
  async getAllTabs(): Promise<DynamicTab[]> {
    try {
      const response = await apiService.getLetterTypeDefinitions();
      if (response.success && response.data) {
        // Convert LetterTypeDefinition to DynamicTab format
        return response.data.map(letterType => ({
          id: letterType.id,
          name: letterType.displayName,
          description: letterType.description,
          letterType: letterType.typeKey,
          isActive: letterType.isActive,
          createdAt: letterType.createdAt,
          updatedAt: letterType.updatedAt,
          metadata: {
            templateConfig: letterType.fieldConfiguration ? JSON.parse(letterType.fieldConfiguration) : {},
            customFields: letterType.fields || []
          },
          letterTypeDefinition: letterType,
          fields: letterType.fields || []
        }));
      }
      return [];
    } catch (error) {
      console.error('Failed to load all tabs:', error);
      return [];
    }
  }

  /**
   * Reactivate a deactivated tab
   */
  async reactivateTab(id: string): Promise<DynamicTab | null> {
    return this.updateTab(id, { isActive: true });
  }

  /**
   * Export tabs data for backup
   */
  async exportTabsData(): Promise<string> {
    try {
      const tabs = await this.getAllTabs();
      return JSON.stringify(tabs, null, 2);
    } catch (error) {
      console.error('Failed to export tabs data:', error);
      return '[]';
    }
  }

  /**
   * Import tabs data from backup
   */
  async importTabsData(jsonData: string): Promise<void> {
    try {
      const tabs = JSON.parse(jsonData);
      for (const tab of tabs) {
        await this.createTab(tab);
      }
    } catch (error) {
      console.error('Failed to import tabs data:', error);
      throw error;
    }
  }

  /**
   * Clear all tab data (use with caution)
   */
  async clearAllTabs(): Promise<void> {
    try {
      const tabs = await this.getAllTabs();
      for (const tab of tabs) {
        await this.deleteTab(tab.id);
      }
    } catch (error) {
      console.error('Failed to clear all tabs:', error);
      throw error;
    }
  }

  // Template Management
  async getTemplatesForTab(tabId: string): Promise<TabTemplate[]> {
    return new Promise(resolve => {
      setTimeout(() => {
        // In a real app, filter by tab association
        resolve([...this.templates]);
      }, 300);
    });
  }

  async createTemplate(templateData: Omit<TabTemplate, 'id' | 'createdAt'>): Promise<TabTemplate> {
    return new Promise(resolve => {
      setTimeout(() => {
        const newTemplate: TabTemplate = {
          ...templateData,
          id: `template_${Date.now()}`,
          createdAt: new Date()
        };
        this.templates.push(newTemplate);
        resolve(newTemplate);
      }, 500);
    });
  }

  async uploadTemplate(file: File, name: string): Promise<TabTemplate> {
    return new Promise((resolve, reject) => {
      setTimeout(() => {
        if (file.type !== 'text/plain' && file.type !== 'application/msword' && file.type !== 'application/vnd.openxmlformats-officedocument.wordprocessingml.document') {
          reject(new Error('Invalid file type. Please upload a text or Word document.'));
          return;
        }
        
        const reader = new FileReader();
        reader.onload = (e) => {
          const content = e.target?.result as string;
          const placeholders = this.extractPlaceholders(content);
          
          const newTemplate: TabTemplate = {
            id: `template_${Date.now()}`,
            name: name || file.name,
            content,
            placeholders,
            createdAt: new Date()
          };
          
          this.templates.push(newTemplate);
          resolve(newTemplate);
        };
        reader.readAsText(file);
      }, 500);
    });
  }

  private extractPlaceholders(content: string): string[] {
    const matches = content.match(/\{\{([^}]+)\}\}/g);
    if (!matches) return [];
    
    return [...new Set(matches.map(match => match.replace(/\{\{|\}\}/g, '')))];
  }

  // Signature Management
  async getSignatures(): Promise<TabSignature[]> {
    return new Promise(resolve => {
      setTimeout(() => resolve([...this.signatures]), 300);
    });
  }

  async uploadSignature(file: File, name: string, description?: string): Promise<TabSignature> {
    return new Promise((resolve, reject) => {
      setTimeout(() => {
        if (!file.type.startsWith('image/')) {
          reject(new Error('Invalid file type. Please upload an image file.'));
          return;
        }
        
        // In a real app, upload to storage and get URL
        const imageUrl = URL.createObjectURL(file);
        
        const newSignature: TabSignature = {
          id: `signature_${Date.now()}`,
          name: name || file.name,
          imageUrl,
          description,
          createdAt: new Date()
        };
        
        this.signatures.push(newSignature);
        resolve(newSignature);
      }, 500);
    });
  }
}

export const tabService = new TabService();