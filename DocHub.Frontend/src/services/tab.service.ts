import { apiService, LetterTypeDefinition, DynamicField, FieldType } from './api.service';
import { cacheService } from './cache.service';

export interface DynamicTab {
  id: string;
  name: string;
  description: string;
  letterType: string;
  isActive: boolean;
  department: string; // ER or Billing
  createdAt: string; // API returns as string
  updatedAt: string; // API returns as string
  fieldConfiguration?: string; // JSON string containing field definitions
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
  private activeRequest: Promise<DynamicTab[]> | null = null;

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
    const cacheKey = 'tabs_all';
    
    // Check cache first
    const cachedTabs = cacheService.get<DynamicTab[]>(cacheKey);
    if (cachedTabs) {
      console.log('📦 [TAB-SERVICE] Returning cached tabs:', cachedTabs.length);
      return cachedTabs;
    }

    // If there's already a request in progress, return it
    if (this.activeRequest) {
      return this.activeRequest;
    }

    // Start new request
    this.activeRequest = this.fetchTabsFromAPI();
    
    try {
      const result = await this.activeRequest;
      // Cache for 2 minutes
      cacheService.set(cacheKey, result, 2 * 60 * 1000);
      return result;
    } finally {
      this.activeRequest = null;
    }
  }

  private clearCache(): void {
    cacheService.invalidatePattern('tabs_');
  }

  private async fetchTabsFromAPI(): Promise<DynamicTab[]> {
    try {
      const response = await apiService.getLetterTypeDefinitions();
      
      if (response.success || response.Success) {
        const responseData: any = response.data || response.Data || [];
        
        // Handle different data structures - could be array or object with array property
        let letterTypes: any = responseData;
        
        // If Data is an object, check if it has an array property
        if (!Array.isArray(responseData) && typeof responseData === 'object') {
          // Check common array property names
          if (Array.isArray(responseData.items)) {
            letterTypes = responseData.items;
          } else if (Array.isArray(responseData.data)) {
            letterTypes = responseData.data;
          } else if (Array.isArray(responseData.results)) {
            letterTypes = responseData.results;
          } else if (Array.isArray(responseData.$values)) {
            // Handle .NET serialization format
            letterTypes = responseData.$values;
          } else {
            // Data structure not recognized, return empty array
            return [];
          }
        }
        
        if (Array.isArray(letterTypes)) {
          // Convert LetterTypeDefinition to DynamicTab format
          return letterTypes.map((letterType: any) => {
            return {
          id: letterType.Id || letterType.id,
          name: letterType.DisplayName || letterType.displayName,
          description: letterType.Description || letterType.description,
          letterType: letterType.TypeKey || letterType.typeKey,
          isActive: letterType.IsActive === 1 || letterType.IsActive === true || letterType.isActive === true,
          fieldConfiguration: letterType.fieldConfiguration,
          createdAt: letterType.CreatedAt || letterType.createdAt,
          updatedAt: letterType.UpdatedAt || letterType.updatedAt,
          metadata: {
            templateConfig: letterType.fieldConfiguration ? 
              JSON.parse(letterType.fieldConfiguration) : {},
            customFields: letterType.Fields || letterType.fields || []
          },
          letterTypeDefinition: letterType,
          fields: letterType.Fields || letterType.fields || []
        };
          });
        } else {
          // Data is not an array, return empty array
          return [];
        }
      } else {
        // API call was not successful, return empty array
        return [];
      }
    } catch (error) {
      console.error('Failed to load tabs:', error);
      return [];
    }
  }

  async getActiveTabs(): Promise<DynamicTab[]> {
    const cacheKey = 'tabs_active';
    
    // Check cache first
    const cachedTabs = cacheService.get<DynamicTab[]>(cacheKey);
    if (cachedTabs) {
      console.log('📦 [TAB-SERVICE] Returning cached active tabs:', cachedTabs.length);
      return cachedTabs;
    }

    try {
      const tabs = await this.getTabs();
      const activeTabs = tabs.filter(tab => tab.isActive);
      // Cache for 1 minute
      cacheService.set(cacheKey, activeTabs, 60 * 1000);
      return activeTabs;
    } catch (error) {
      console.error('Failed to load active tabs:', error);
      return [];
    }
  }

  async getActiveTabById(id: string): Promise<DynamicTab | null> {
    const cacheKey = `tab_${id}`;
    
    // Check cache first
    const cachedTab = cacheService.get<DynamicTab>(cacheKey);
    if (cachedTab) {
      console.log('📦 [TAB-SERVICE] Returning cached tab:', cachedTab.name);
      return cachedTab;
    }

    try {
      console.log('🔍 [TAB-SERVICE] Getting tab by ID:', id);
      
      // First try to get from the list of all tabs (which includes department filtering)
      const allTabs = await this.getAllTabs();
      console.log('📊 [TAB-SERVICE] All tabs loaded:', allTabs.length);
      
      const tab = allTabs.find(t => t.id === id);
      if (tab && tab.isActive) {
        console.log('✅ [TAB-SERVICE] Found active tab in list:', tab.name);
        // Cache for 5 minutes
        cacheService.set(cacheKey, tab, 5 * 60 * 1000);
        return tab;
      }
      
      // If not found in the filtered list, try direct API call as fallback
      console.log('🔄 [TAB-SERVICE] Tab not found in filtered list, trying direct API call...');
      const response = await apiService.getLetterTypeDefinition(id);
      
      if (response.success && response.data) {
        const letterType: any = response.data;
        const isActive = letterType.IsActive === 1 || letterType.IsActive === true || letterType.isActive === true;
        
        if (isActive) {
          const mappedTab = {
            id: letterType.Id || letterType.id,
            name: letterType.DisplayName || letterType.displayName,
            description: letterType.Description || letterType.description,
            letterType: letterType.TypeKey || letterType.typeKey,
            isActive: isActive,
            department: letterType.Department || 'ER',
            fieldConfiguration: letterType.fieldConfiguration,
            createdAt: letterType.CreatedAt || letterType.createdAt,
            updatedAt: letterType.UpdatedAt || letterType.updatedAt,
            metadata: {
              templateConfig: (letterType.FieldConfiguration || letterType.fieldConfiguration) ? 
                JSON.parse(letterType.FieldConfiguration || letterType.fieldConfiguration) : {},
              customFields: letterType.Fields || letterType.fields || []
            },
            letterTypeDefinition: letterType,
            fields: letterType.Fields || letterType.fields || []
          };
          
          console.log('✅ [TAB-SERVICE] Found active tab via direct API call:', mappedTab.name);
          // Cache for 5 minutes
          cacheService.set(cacheKey, mappedTab, 5 * 60 * 1000);
          return mappedTab;
        } else {
          console.log('❌ [TAB-SERVICE] Tab found but not active');
        }
      } else {
        console.log('❌ [TAB-SERVICE] API call failed or no data returned');
      }
      return null;
    } catch (error) {
      console.error('❌ [TAB-SERVICE] Failed to get tab by ID:', error);
      return null;
    }
  }

  async createTab(tabData: Omit<DynamicTab, 'id' | 'createdAt' | 'updatedAt'>): Promise<DynamicTab> {
    try {
      // Check if a tab with the same name already exists
      const existingTabs = await this.getTabs();
      const duplicateTab = existingTabs.find(tab => 
        tab.name.toLowerCase() === tabData.name.toLowerCase()
      );
      
      if (duplicateTab) {
        throw new Error(`A tab with the name "${tabData.name}" already exists`);
      }

      // Generate a unique key to avoid duplicates
      const uniqueKey = `${tabData.letterType}_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
      
      // Transform the data to match LetterTypeDefinition format (camelCase for backend)
      const createRequest = {
        typeKey: uniqueKey,
        displayName: tabData.name,
        description: tabData.description,
        dataSourceType: 'Excel', // Default data source type
        fieldConfiguration: tabData.metadata?.templateConfig ? JSON.stringify(tabData.metadata.templateConfig) : undefined,
        isActive: tabData.isActive,
        module: 'ER' // Default to ER module
      };

      const response = await apiService.createLetterTypeDefinition(createRequest);
      if (response.success && response.data) {
        // Clear cache to force refresh
        this.clearCache();
        
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
        // Clear cache to force refresh
        this.clearCache();
        
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
        // Clear cache to force refresh
        this.clearCache();
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
      console.log('🔍 [TAB-SERVICE] Calling getLetterTypeDefinitions API...');
      const response = await apiService.getLetterTypeDefinitions();
      console.log('📊 [TAB-SERVICE] API response:', response);
      if (response.success && response.data && Array.isArray(response.data)) {
        console.log('📋 [TAB-SERVICE] Raw letter types:', response.data);
        // Convert LetterTypeDefinition to DynamicTab format
        const mappedTabs = response.data.map(letterType => ({
          id: letterType.id,
          name: letterType.displayName,
          description: letterType.description,
          letterType: letterType.typeKey,
          isActive: letterType.isActive,
          department: letterType.department || 'ER', // Add department field
          createdAt: letterType.createdAt,
          updatedAt: letterType.updatedAt,
          fieldConfiguration: letterType.fieldConfiguration, // Add fieldConfiguration directly to tab
          metadata: {
            templateConfig: letterType.fieldConfiguration ? JSON.parse(letterType.fieldConfiguration) : {},
            customFields: letterType.fields || []
          },
          letterTypeDefinition: letterType,
          fields: letterType.fields || []
        }));
        console.log('✅ [TAB-SERVICE] Mapped tabs:', mappedTabs);
        return mappedTabs;
      }
      console.warn('No data received from getLetterTypeDefinitions API or data is not an array:', response);
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

  /**
   * Get tabs filtered by department
   */
  async getTabsByDepartment(department: string): Promise<DynamicTab[]> {
    const cacheKey = `tabs_department_${department}`;
    
    // Check cache first
    const cachedTabs = cacheService.get<DynamicTab[]>(cacheKey);
    if (cachedTabs) {
      console.log('📦 [TAB-SERVICE] Returning cached tabs for department:', department, cachedTabs.length);
      return cachedTabs;
    }

    try {
      console.log('🔍 [TAB-SERVICE] Getting tabs for department:', department);
      const tabs = await this.getAllTabs();
      console.log('📊 [TAB-SERVICE] All tabs received:', tabs);
      const filteredTabs = tabs.filter(tab => tab.department === department);
      console.log('✅ [TAB-SERVICE] Filtered tabs for department:', filteredTabs);
      // Cache for 2 minutes
      cacheService.set(cacheKey, filteredTabs, 2 * 60 * 1000);
      return filteredTabs;
    } catch (error) {
      console.error('Failed to get tabs by department:', error);
      return [];
    }
  }

  /**
   * Get active tabs filtered by department
   */
  async getActiveTabsByDepartment(department: string): Promise<DynamicTab[]> {
    try {
      const tabs = await this.getActiveTabs();
      return tabs.filter(tab => tab.department === department);
    } catch (error) {
      console.error('Failed to get active tabs by department:', error);
      return [];
    }
  }
}

export const tabService = new TabService();