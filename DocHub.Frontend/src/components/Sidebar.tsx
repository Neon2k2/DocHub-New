import React, { useState, useEffect } from 'react';
import { 
  LayoutDashboard, 
  FileText, 
  ArrowRightLeft, 
  HandHeart, 
  CheckCircle, 
  Plus,
  Download,
  Merge,
  Mail,
  AlertTriangle,
  Calendar,
  Users,
  Settings,
  Eye,
  Layers
} from 'lucide-react';
import { Button } from './ui/button';
import { cn } from './ui/utils';
import { UserRole } from './Login';
import { DynamicTab, tabService } from '../services/tab.service';
import { Badge } from './ui/badge';

interface SidebarProps {
  activeModule: 'er' | 'billing';
  activePage: string;
  onPageChange: (page: string) => void;
  currentUser: UserRole;
}

export function Sidebar({ activeModule, activePage, onPageChange, currentUser }: SidebarProps) {
  const [dynamicTabs, setDynamicTabs] = useState<DynamicTab[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const loadDynamicTabs = async () => {
      try {
        const tabs = await tabService.getTabs();
        setDynamicTabs(tabs.filter(tab => tab.isActive));
      } catch (error) {
        console.error('Failed to load dynamic tabs:', error);
      } finally {
        setLoading(false);
      }
    };

    loadDynamicTabs();
    
    // Refresh tabs when module changes or periodically
    const interval = setInterval(loadDynamicTabs, 10000); // Refresh every 10 seconds (less frequent)
    
    return () => clearInterval(interval);
  }, [activeModule]);

  // Base static pages
  const staticERPages = [
    { id: 'dashboard', label: 'Dashboard', icon: LayoutDashboard, isStatic: true, category: 'core' },
  ];


  // Build complete pages array with dynamic tabs integrated
  let erPages = [...staticERPages];

  // Add dynamic tabs to ER module (integrated into letters category)
  if (activeModule === 'er') {
    dynamicTabs.forEach(tab => {
      erPages.push({
        id: tab.id,
        label: tab.name,
        icon: FileText,
        isStatic: false,
        category: 'letters'
      });
    });
  }

  // Add admin-only pages
  if (currentUser.permissions.isAdmin) {
    erPages.push({ id: 'dynamic-letters', label: 'Tab Management', icon: Layers, isStatic: true, category: 'admin' });
    erPages.push({ id: 'tab-persistence', label: 'Tab Persistence', icon: HandHeart, isStatic: true, category: 'admin' });
    erPages.push({ id: 'user-management', label: 'User Management', icon: Users, isStatic: true, category: 'admin' });
  }

  const pages = erPages; // Only ER module for now

  // Group pages by category for better organization
  const pagesByCategory = pages.reduce((acc, page) => {
    const category = page.category || 'other';
    if (!acc[category]) acc[category] = [];
    acc[category].push(page);
    return acc;
  }, {} as Record<string, typeof pages>);

  return (
    <aside className="fixed left-0 top-16 bottom-0 w-64 glass-panel border-r z-40 overflow-y-auto">
      <div className="p-6">
        <div className="space-y-2">
          <div className="mb-6">
            <div className="mb-4">
              <h3 className="text-sm font-medium text-muted-foreground">
                Employee Relations
              </h3>
            </div>
            
            <div className="mb-4 p-3 glass-panel rounded-lg border-glass-border">
              <div className="text-xs text-muted-foreground">
                Logged in as <span className="text-neon-blue font-medium">{currentUser.name}</span>
              </div>
              <div className="text-xs text-muted-foreground mt-1">
                Role: <span className="capitalize text-foreground font-medium">{currentUser.role}</span>
                {currentUser.permissions.isAdmin && (
                  <span className="ml-1 text-red-400">• Admin</span>
                )}
              </div>
            </div>
          </div>
          
          {/* Organized Navigation by Category */}
          <div className="space-y-6">
            {/* Core Pages (Dashboard) */}
            {pagesByCategory['core'] && (
              <div className="space-y-1">
                {pagesByCategory['core'].map((page) => {
                  const Icon = page.icon;
                  const isActive = activePage === page.id;
                  
                  return (
                    <Button
                      key={page.id}
                      variant={isActive ? 'default' : 'ghost'}
                      className={cn(
                        "w-full justify-start transition-all duration-300",
                        isActive && "neon-border bg-card text-neon-blue neon-glow",
                        !isActive && "hover:bg-muted hover:text-neon-blue"
                      )}
                      onClick={() => onPageChange(page.id)}
                    >
                      <Icon className="mr-3 h-4 w-4" />
                      {page.label}
                    </Button>
                  );
                })}
              </div>
            )}

            {/* Letters Section (Static + Dynamic) */}
            {pagesByCategory['letters'] && (
              <div>
                <div className="flex items-center gap-2 mb-3">
                  <h4 className="text-xs font-medium text-muted-foreground">Letter Types</h4>
                  <Badge variant="outline" className="text-xs h-5">
                    {pagesByCategory['letters'].length}
                  </Badge>
                </div>
                <div className="space-y-1">
                  {pagesByCategory['letters'].map((page) => {
                    const Icon = page.icon;
                    const isActive = activePage === page.id;
                    
                    return (
                      <Button
                        key={page.id}
                        variant={isActive ? 'default' : 'ghost'}
                        className={cn(
                          "w-full justify-start transition-all duration-300 group",
                          isActive && "neon-border bg-card text-neon-blue neon-glow",
                          !isActive && "hover:bg-muted hover:text-neon-blue",
                          !page.isStatic && "border-l-2 border-transparent hover:border-neon-blue/50"
                        )}
                        onClick={() => onPageChange(page.id)}
                      >
                        <Icon className={cn(
                          "mr-3 h-4 w-4",
                          !page.isStatic && "text-neon-blue/70 group-hover:text-neon-blue"
                        )} />
                        <span className="truncate">{page.label}</span>
                      </Button>
                    );
                  })}
                </div>
              </div>
            )}

            {/* Timesheet Section */}
            {pagesByCategory['timesheet'] && (
              <div>
                <div className="flex items-center gap-2 mb-3">
                  <h4 className="text-xs font-medium text-muted-foreground">Time Management</h4>
                  <Badge variant="outline" className="text-xs h-5">
                    {pagesByCategory['timesheet'].length}
                  </Badge>
                </div>
                <div className="space-y-1">
                  {pagesByCategory['timesheet'].map((page) => {
                    const Icon = page.icon;
                    const isActive = activePage === page.id;
                    
                    return (
                      <Button
                        key={page.id}
                        variant={isActive ? 'default' : 'ghost'}
                        className={cn(
                          "w-full justify-start transition-all duration-300",
                          isActive && "neon-border bg-card text-neon-blue neon-glow",
                          !isActive && "hover:bg-muted hover:text-neon-blue"
                        )}
                        onClick={() => onPageChange(page.id)}
                      >
                        <Icon className="mr-3 h-4 w-4" />
                        {page.label}
                      </Button>
                    );
                  })}
                </div>
              </div>
            )}

            {/* Mail Section */}
            {pagesByCategory['mail'] && (
              <div>
                <div className="flex items-center gap-2 mb-3">
                  <h4 className="text-xs font-medium text-muted-foreground">Bulk Mail</h4>
                  <Badge variant="outline" className="text-xs h-5">
                    {pagesByCategory['mail'].length}
                  </Badge>
                </div>
                <div className="space-y-1">
                  {pagesByCategory['mail'].map((page) => {
                    const Icon = page.icon;
                    const isActive = activePage === page.id;
                    
                    return (
                      <Button
                        key={page.id}
                        variant={isActive ? 'default' : 'ghost'}
                        className={cn(
                          "w-full justify-start transition-all duration-300",
                          isActive && "neon-border bg-card text-neon-blue neon-glow",
                          !isActive && "hover:bg-muted hover:text-neon-blue"
                        )}
                        onClick={() => onPageChange(page.id)}
                      >
                        <Icon className="mr-3 h-4 w-4" />
                        {page.label}
                      </Button>
                    );
                  })}
                </div>
              </div>
            )}

            {/* Logs Section */}
            {pagesByCategory['logs'] && (
              <div>
                <div className="flex items-center gap-2 mb-3">
                  <h4 className="text-xs font-medium text-muted-foreground">System Logs</h4>
                  <Badge variant="outline" className="text-xs h-5">
                    {pagesByCategory['logs'].length}
                  </Badge>
                </div>
                <div className="space-y-1">
                  {pagesByCategory['logs'].map((page) => {
                    const Icon = page.icon;
                    const isActive = activePage === page.id;
                    
                    return (
                      <Button
                        key={page.id}
                        variant={isActive ? 'default' : 'ghost'}
                        className={cn(
                          "w-full justify-start transition-all duration-300",
                          isActive && "neon-border bg-card text-neon-blue neon-glow",
                          !isActive && "hover:bg-muted hover:text-neon-blue"
                        )}
                        onClick={() => onPageChange(page.id)}
                      >
                        <Icon className="mr-3 h-4 w-4" />
                        {page.label}
                      </Button>
                    );
                  })}
                </div>
              </div>
            )}

            {/* Admin Section */}
            {pagesByCategory['admin'] && (
              <div>
                <div className="flex items-center gap-2 mb-3">
                  <h4 className="text-xs font-medium text-muted-foreground">Administration</h4>
                  <Badge variant="outline" className="text-xs h-5 bg-red-500/10 text-red-400 border-red-500/30">
                    Admin Only
                  </Badge>
                </div>
                <div className="space-y-1">
                  {pagesByCategory['admin'].map((page) => {
                    const Icon = page.icon;
                    const isActive = activePage === page.id;
                    
                    return (
                      <Button
                        key={page.id}
                        variant={isActive ? 'default' : 'ghost'}
                        className={cn(
                          "w-full justify-start transition-all duration-300",
                          isActive && "neon-border bg-card text-neon-blue neon-glow",
                          !isActive && "hover:bg-muted hover:text-neon-blue"
                        )}
                        onClick={() => onPageChange(page.id)}
                      >
                        <Icon className="mr-3 h-4 w-4" />
                        {page.label}
                      </Button>
                    );
                  })}
                </div>
              </div>
            )}
          </div>

          {/* Loading Indicator for Dynamic Tabs */}
          {loading && activeModule === 'er' && (
            <div className="mt-4 p-3 glass-panel rounded-lg border-glass-border bg-orange-500/5">
              <div className="flex items-center gap-2">
                <div className="animate-spin rounded-full h-3 w-3 border-b border-orange-400"></div>
                <div className="text-xs text-orange-400">Loading dynamic tabs...</div>
              </div>
            </div>
          )}
        </div>
      </div>
    </aside>
  );
}