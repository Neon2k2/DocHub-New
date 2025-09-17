import React from 'react';
import { ERDashboard } from '../components/er/Dashboard';
import { AdminSettings } from '../components/admin/AdminSettings';
import { TabManagement } from '../components/admin/TabManagement';
import UserManagement from '../components/admin/UserManagement';
import { SessionManagement } from '../components/admin/SessionManagement';
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
        console.log('🔍 [DYNAMIC-TAB-RENDERER] Loading tab with ID:', tabId);
        const tabData = await tabService.getActiveTabById(tabId);
        console.log('📊 [DYNAMIC-TAB-RENDERER] Tab data received:', tabData);
        
        if (tabData) {
          console.log('✅ [DYNAMIC-TAB-RENDERER] Tab found successfully:', {
            id: tabData.id,
            name: tabData.name,
            isActive: tabData.isActive,
            department: tabData.department
          });
        } else {
          console.log('❌ [DYNAMIC-TAB-RENDERER] No tab data returned');
        }
        
        setTab(tabData);
      } catch (error) {
        console.error('❌ [DYNAMIC-TAB-RENDERER] Failed to load tab:', error);
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
      case 'user-management':
        return isAdmin() 
          ? <UserManagement /> 
          : <UnauthorizedPage module="User Management" />;
      case 'session-management':
        return isAdmin() 
          ? <SessionManagement /> 
          : <UnauthorizedPage module="Session Management" />;
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