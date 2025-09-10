/**
 * Tab Persistence Service
 * Handles persistence of dynamic tabs using localStorage with fallback to memory
 */

import { DynamicTab } from './tab.service';

interface TabPersistenceData {
  tabs: DynamicTab[];
  lastUpdated: Date;
  version: string;
}

class TabPersistenceService {
  private readonly STORAGE_KEY = 'dochub_dynamic_tabs';
  private readonly VERSION = '1.0.0';
  private memoryCache: Map<string, DynamicTab> = new Map();

  /**
   * Save tabs to localStorage with error handling
   */
  async saveTabs(tabs: DynamicTab[]): Promise<void> {
    try {
      const persistenceData: TabPersistenceData = {
        tabs,
        lastUpdated: new Date(),
        version: this.VERSION
      };

      // Save to localStorage
      if (typeof localStorage !== 'undefined') {
        localStorage.setItem(this.STORAGE_KEY, JSON.stringify(persistenceData));
      }

      // Update memory cache
      this.memoryCache.clear();
      tabs.forEach(tab => {
        this.memoryCache.set(tab.id, tab);
      });

      console.log(`Persisted ${tabs.length} dynamic tabs to storage`);
    } catch (error) {
      console.error('Failed to save tabs to storage:', error);
      // Still update memory cache as fallback
      this.memoryCache.clear();
      tabs.forEach(tab => {
        this.memoryCache.set(tab.id, tab);
      });
    }
  }

  /**
   * Load tabs from localStorage with fallback to default data
   */
  async loadTabs(): Promise<DynamicTab[]> {
    try {
      // Try localStorage first
      if (typeof localStorage !== 'undefined') {
        const stored = localStorage.getItem(this.STORAGE_KEY);
        if (stored) {
          const persistenceData: TabPersistenceData = JSON.parse(stored);
          
          // Check version compatibility
          if (persistenceData.version === this.VERSION && persistenceData.tabs) {
            // Convert date strings back to Date objects
            const tabs = persistenceData.tabs.map(tab => ({
              ...tab,
              createdAt: new Date(tab.createdAt),
              updatedAt: new Date(tab.updatedAt)
            }));

            // Update memory cache
            this.memoryCache.clear();
            tabs.forEach(tab => {
              this.memoryCache.set(tab.id, tab);
            });

            console.log(`Loaded ${tabs.length} dynamic tabs from storage`);
            return tabs;
          }
        }
      }

      // Fallback to memory cache
      if (this.memoryCache.size > 0) {
        console.log(`Loaded ${this.memoryCache.size} tabs from memory cache`);
        return Array.from(this.memoryCache.values());
      }

      // Return default tabs if nothing is found
      console.log('No persisted tabs found, returning default data');
      return this.getDefaultTabs();
    } catch (error) {
      console.error('Failed to load tabs from storage:', error);
      
      // Fallback to memory cache or defaults
      if (this.memoryCache.size > 0) {
        return Array.from(this.memoryCache.values());
      }
      
      return this.getDefaultTabs();
    }
  }

  /**
   * Save a single tab (used for create/update operations)
   */
  async saveTab(tab: DynamicTab): Promise<void> {
    try {
      // Update memory cache first
      this.memoryCache.set(tab.id, tab);

      // Load all tabs, update the specific one, and save back
      const allTabs = await this.loadTabs();
      const tabIndex = allTabs.findIndex(t => t.id === tab.id);
      
      if (tabIndex >= 0) {
        allTabs[tabIndex] = tab;
      } else {
        allTabs.push(tab);
      }

      await this.saveTabs(allTabs);
    } catch (error) {
      console.error('Failed to save individual tab:', error);
      // At least keep it in memory cache
      this.memoryCache.set(tab.id, tab);
    }
  }

  /**
   * Delete a tab from persistence
   */
  async deleteTab(tabId: string): Promise<void> {
    try {
      // Remove from memory cache
      this.memoryCache.delete(tabId);

      // Load all tabs, remove the specific one, and save back
      const allTabs = await this.loadTabs();
      const filteredTabs = allTabs.filter(t => t.id !== tabId);

      await this.saveTabs(filteredTabs);
    } catch (error) {
      console.error('Failed to delete tab from storage:', error);
      // At least remove from memory cache
      this.memoryCache.delete(tabId);
    }
  }

  /**
   * Get a specific tab by ID
   */
  async getTabById(tabId: string): Promise<DynamicTab | null> {
    try {
      // Check memory cache first
      if (this.memoryCache.has(tabId)) {
        return this.memoryCache.get(tabId) || null;
      }

      // Load all tabs and find the specific one
      const allTabs = await this.loadTabs();
      return allTabs.find(tab => tab.id === tabId) || null;
    } catch (error) {
      console.error('Failed to get tab by ID:', error);
      return null;
    }
  }

  /**
   * Clear all persisted data
   */
  async clearAll(): Promise<void> {
    try {
      if (typeof localStorage !== 'undefined') {
        localStorage.removeItem(this.STORAGE_KEY);
      }
      this.memoryCache.clear();
      console.log('Cleared all persisted tab data');
    } catch (error) {
      console.error('Failed to clear persisted data:', error);
    }
  }

  /**
   * Get default tabs data (fallback) - now returns empty array for fully dynamic system
   */
  private getDefaultTabs(): DynamicTab[] {
    return [];
  }

  /**
   * Export all tabs data (for backup purposes)
   */
  async exportTabs(): Promise<string> {
    try {
      const tabs = await this.loadTabs();
      const exportData = {
        tabs,
        exportedAt: new Date(),
        version: this.VERSION
      };
      return JSON.stringify(exportData, null, 2);
    } catch (error) {
      console.error('Failed to export tabs:', error);
      throw new Error('Failed to export tabs data');
    }
  }

  /**
   * Import tabs data (for restore purposes)
   */
  async importTabs(jsonData: string): Promise<void> {
    try {
      const importData = JSON.parse(jsonData);
      
      if (!importData.tabs || !Array.isArray(importData.tabs)) {
        throw new Error('Invalid import data format');
      }

      // Convert date strings back to Date objects
      const tabs = importData.tabs.map((tab: any) => ({
        ...tab,
        createdAt: new Date(tab.createdAt),
        updatedAt: new Date(tab.updatedAt)
      }));

      await this.saveTabs(tabs);
      console.log(`Imported ${tabs.length} tabs successfully`);
    } catch (error) {
      console.error('Failed to import tabs:', error);
      throw new Error('Failed to import tabs data');
    }
  }

  /**
   * Get storage statistics
   */
  getStorageStats(): { hasLocalStorage: boolean; tabCount: number; memoryTabCount: number } {
    return {
      hasLocalStorage: typeof localStorage !== 'undefined',
      tabCount: this.memoryCache.size,
      memoryTabCount: this.memoryCache.size
    };
  }
}

export const tabPersistenceService = new TabPersistenceService();