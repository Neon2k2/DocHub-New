import React from 'react';
import { Loader2 } from 'lucide-react';
import { cn } from './utils';

interface LoadingProps {
  size?: 'sm' | 'md' | 'lg';
  text?: string;
  className?: string;
  fullScreen?: boolean;
}

export function Loading({ 
  size = 'md', 
  text, 
  className,
  fullScreen = false 
}: LoadingProps) {
  const sizeClasses = {
    sm: 'h-4 w-4',
    md: 'h-6 w-6',
    lg: 'h-8 w-8'
  };

  const content = (
    <div className={cn('flex items-center justify-center gap-2', className)}>
      <Loader2 className={cn('animate-spin', sizeClasses[size])} />
      {text && <span className="text-sm text-muted-foreground">{text}</span>}
    </div>
  );

  if (fullScreen) {
    return (
      <div className="fixed inset-0 bg-background/80 backdrop-blur-sm flex items-center justify-center z-50">
        <div className="glass-panel border-glass-border rounded-lg p-8">
          {content}
        </div>
      </div>
    );
  }

  return content;
}

export function LoadingSpinner({ size = 'md', className }: { size?: 'sm' | 'md' | 'lg'; className?: string }) {
  return <Loading size={size} className={className} />;
}

export function LoadingOverlay({ text, className }: { text?: string; className?: string }) {
  return (
    <div className={cn('absolute inset-0 bg-background/50 backdrop-blur-sm flex items-center justify-center z-10', className)}>
      <Loading text={text} />
    </div>
  );
}
