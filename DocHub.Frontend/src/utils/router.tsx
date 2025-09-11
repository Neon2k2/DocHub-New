import React from 'react';
import { ERDashboard } from '../components/er/Dashboard';
import { AdminSettings } from '../components/admin/AdminSettings';
import { TabManagement } from '../components/admin/TabManagement';
import { TabPersistenceManager } from '../components/admin/TabPersistenceManager';
import { UserManagement } from '../components/admin/UserManagement';
import { WelcomeDashboard } from '../components/WelcomeDashboard';
import { PlaceholderPage } from '../components/shared/PlaceholderPage';
import { UnauthorizedPage } from '../components/shared/UnauthorizedPage';
import { DynamicLetterTab } from '../components/shared/DynamicLetterTab';
import { DynamicTab, tabService } from '../services/tab.service';

export type Module = 'er' | 'billing';
export type Page = string;

interface RouteParams {
  activeModule: Module;
  activePage: Page;
  isAdmin: () => boolean;
  canAccessModule: (module: Module) => boolean;
  onNavigate: (module: Module, page: Page) => void;
}

// Create a component for dynamic tab rendering
const DynamicTabRenderer: React.FC<{ tabId: string }> = ({ tabId }) => {
  const [tab, setTab] = React.useState<DynamicTab | null>(null);
  const [loading, setLoading] = React.useState(true);

  React.useEffect(() => {
    const loadTab = async () => {
      try {
        console.log('DynamicTabRenderer: Loading tab with ID:', tabId);
        const tabData = await tabService.getActiveTabById(tabId);
        console.log('DynamicTabRenderer: Tab data received:', tabData);
        setTab(tabData);
      } catch (error) {
        console.error('Failed to load tab:', error);
      } finally {
        setLoading(false);
      }
    };

    loadTab();
  }, [tabId]);

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-96">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-neon-blue"></div>
      </div>
    );
  }

  if (!tab) {
    return <PlaceholderPage title="Tab Not Found" />;
  }

  return <DynamicLetterTab tab={tab} />;
};

export function renderPageContent({ 
  activeModule, 
  activePage, 
  isAdmin, 
  canAccessModule, 
  onNavigate 
}: RouteParams): React.ReactNode {
  // Welcome page
  if (activePage === 'welcome') {
    return <WelcomeDashboard onNavigate={onNavigate} />;
  }

  // Check if user has access to the current module
  if (!canAccessModule(activeModule)) {
    return <UnauthorizedPage module={activeModule === 'er' ? 'Employee Relations' : 'Billing & Timesheet'} />;
  }

  // ER Module Routes
  if (activeModule === 'er') {
    switch (activePage) {
      case 'dashboard':
        return <ERDashboard />;
      case 'dynamic-letters':
        return isAdmin() 
          ? <TabManagement /> 
          : <UnauthorizedPage module="Dynamic Letters" />;
      case 'tab-persistence':
        return isAdmin() 
          ? <TabPersistenceManager /> 
          : <UnauthorizedPage module="Tab Persistence" />;
      case 'user-management':
        return isAdmin() 
          ? <UserManagement /> 
          : <UnauthorizedPage module="User Management" />;
      default:
        // All other pages are dynamic tabs
        return <DynamicTabRenderer tabId={activePage} />;
    }
  }

  // Billing Module Routes - Coming Soon
  if (activeModule === 'billing') {
    return <PlaceholderPage title="Billing Module" description="Billing module is coming soon!" />;
  }

  // Fallback
  return <ERDashboard />;
}