import React from 'react';

interface UnauthorizedPageProps {
  module: string;
}

export function UnauthorizedPage({ module }: UnauthorizedPageProps) {
  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Access Denied</h1>
          <p className="text-muted-foreground">You don't have permission to access this module</p>
        </div>
      </div>
      
      <div className="glass-panel border-glass-border rounded-lg p-12 text-center">
        <div className="space-y-4">
          <div className="mx-auto w-24 h-24 bg-red-500/20 rounded-full flex items-center justify-center">
            <div className="text-4xl">🔒</div>
          </div>
          <div>
            <h2 className="text-xl font-semibold text-red-400">Unauthorized Access</h2>
            <p className="text-muted-foreground mt-2">
              You don't have permission to access <strong>{module}</strong>. 
              Please contact your administrator to request access.
            </p>
          </div>
          <div className="pt-4">
            <div className="inline-flex items-center gap-2 text-sm text-red-400">
              <div className="w-2 h-2 bg-red-400 rounded-full"></div>
              Access Restricted
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}