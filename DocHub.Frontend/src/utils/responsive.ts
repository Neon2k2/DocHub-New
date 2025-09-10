import { useEffect, useState } from 'react';

export const useResponsive = () => {
  const [screenSize, setScreenSize] = useState({
    width: typeof window !== 'undefined' ? window.innerWidth : 1024,
    height: typeof window !== 'undefined' ? window.innerHeight : 768,
  });

  useEffect(() => {
    const handleResize = () => {
      setScreenSize({
        width: window.innerWidth,
        height: window.innerHeight,
      });
    };

    window.addEventListener('resize', handleResize);
    return () => window.removeEventListener('resize', handleResize);
  }, []);

  return {
    ...screenSize,
    isMobile: screenSize.width < 768,
    isTablet: screenSize.width >= 768 && screenSize.width < 1024,
    isDesktop: screenSize.width >= 1024,
    isLargeDesktop: screenSize.width >= 1280,
  };
};

export const getResponsiveClasses = (baseClasses: string, mobileClasses?: string, tabletClasses?: string, desktopClasses?: string) => {
  return `${baseClasses} ${mobileClasses || ''} ${tabletClasses || ''} ${desktopClasses || ''}`;
};

export const getGridCols = (isMobile: boolean, isTablet: boolean) => {
  if (isMobile) return 'grid-cols-1';
  if (isTablet) return 'grid-cols-2';
  return 'grid-cols-3';
};

export const getDialogSize = (isMobile: boolean, isTablet: boolean) => {
  if (isMobile) return 'max-w-[95vw] h-[95vh]';
  if (isTablet) return 'max-w-4xl h-[90vh]';
  return 'max-w-6xl h-[85vh]';
};

export const getCardPadding = (isMobile: boolean) => {
  return isMobile ? 'p-4' : 'p-6';
};

export const getButtonSize = (isMobile: boolean) => {
  return isMobile ? 'sm' : 'default';
};

export const getTextSize = (isMobile: boolean, size: 'sm' | 'md' | 'lg' = 'md') => {
  const sizes = {
    sm: isMobile ? 'text-xs' : 'text-sm',
    md: isMobile ? 'text-sm' : 'text-base',
    lg: isMobile ? 'text-base' : 'text-lg',
  };
  return sizes[size];
};
