import React, { useState } from 'react';
import { AuthProvider, useAuth } from './contexts/AuthContext';
import { ThemeProvider } from './contexts/ThemeContext';
import { Header } from './components/Header';
import { Sidebar } from './components/Sidebar';
import { Login } from './components/Login';
import { Toaster } from './components/ui/sonner';
import { ErrorBoundary } from './components/ErrorBoundary';
import { renderPageContent, Module, Page } from './utils/router';
import { useResponsive } from './utils/responsive';

function AppContent() {
  const { user, isAuthenticated, canAccessModule, isAdmin, isLoading } = useAuth();
  const [activeModule, setActiveModule] = useState<Module>('er');
  const [activePage, setActivePage] = useState<Page>('welcome');
  const { isMobile, isTablet } = useResponsive();

  // Reset to dashboard when switching modules
  const handleModuleChange = (module: Module) => {
    if (!user) return;
    
    // Check if user has permission to access the module
    if (!canAccessModule(module)) return;
    
    setActiveModule(module);
    setActivePage('dashboard');
  };

  const handleNavigate = (module: Module, page: Page) => {
    setActiveModule(module);
    setActivePage(page);
  };

  const handleNavigateToWelcome = () => {
    setActivePage('welcome');
  };

  const renderContent = () => {
    if (!user) return null;

    return renderPageContent({
      activeModule,
      activePage,
      isAdmin,
      canAccessModule,
      onNavigate: handleNavigate,
    });
  };

  // Show loading state while auth is initializing
  if (isLoading) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary mx-auto mb-4"></div>
          <p className="text-muted-foreground">Loading...</p>
        </div>
      </div>
    );
  }

  // Show login page if not authenticated
  if (!isAuthenticated) {
    return (
      <>
        <Login />
        <Toaster />
      </>
    );
  }

  return (
    <div className="min-h-screen bg-background">
      <Header
        activeModule={activeModule}
        onModuleChange={handleModuleChange}
        currentUser={user}
        onNavigateToWelcome={handleNavigateToWelcome}
      />
      
      <div className="flex">
        {!isMobile && (
          <Sidebar
            activeModule={activeModule}
            activePage={activePage}
            onPageChange={setActivePage}
            currentUser={user}
          />
        )}
        
        <main className={`flex-1 mt-16 p-4 sm:p-6 ${!isMobile ? 'ml-64' : ''}`}>
          <div className="max-w-7xl mx-auto">
            {renderContent()}
          </div>
        </main>
      </div>
      
      <Toaster />
    </div>
  );
}

export default function App() {
  return (
    <ErrorBoundary>
      <AuthProvider>
        <ThemeProvider>
          <AppContent />
        </ThemeProvider>
      </AuthProvider>
    </ErrorBoundary>
  );
}

