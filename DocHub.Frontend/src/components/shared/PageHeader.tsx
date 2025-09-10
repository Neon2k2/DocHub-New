import React from 'react';
import { Badge } from '../ui/badge';
import { TestDialog } from '../TestDialog';

interface PageHeaderProps {
  title: string;
  description: string;
  isActive?: boolean;
  showTestDialog?: boolean;
}

export function PageHeader({ 
  title, 
  description, 
  isActive = true, 
  showTestDialog = false 
}: PageHeaderProps) {
  return (
    <div className="flex items-center justify-between">
      <div>
        <h1 className="text-2xl font-bold">{title}</h1>
        <p className="text-muted-foreground">{description}</p>
      </div>
      <div className="flex items-center gap-3">
        <Badge variant={isActive ? "default" : "secondary"}>
          {isActive ? "Active" : "Inactive"}
        </Badge>
        {showTestDialog && <TestDialog />}
      </div>
    </div>
  );
}
