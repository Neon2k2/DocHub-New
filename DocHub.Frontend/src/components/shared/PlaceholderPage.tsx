import React from 'react';

interface PlaceholderPageProps {
  title: string;
}

export function PlaceholderPage({ title }: PlaceholderPageProps) {
  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">{title}</h1>
          <p className="text-muted-foreground">This page is under development</p>
        </div>
      </div>
      
      <div className="glass-panel border-glass-border rounded-lg p-12 text-center">
        <div className="space-y-4">
          <div className="mx-auto w-24 h-24 bg-neon-blue/20 rounded-full flex items-center justify-center">
            <div className="text-4xl">🚧</div>
          </div>
          <div>
            <h2 className="text-xl font-semibold">{title}</h2>
            <p className="text-muted-foreground mt-2">
              This feature is coming soon! Our development team is working hard to bring you this functionality.
            </p>
          </div>
          <div className="pt-4">
            <div className="inline-flex items-center gap-2 text-sm text-neon-blue">
              <div className="w-2 h-2 bg-neon-blue rounded-full animate-pulse"></div>
              Under Development
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}