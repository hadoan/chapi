import { cn } from '@/lib/utils';
import React from 'react';

interface FloatingElementsProps {
  className?: string;
}

export const FloatingElements: React.FC<FloatingElementsProps> = ({
  className,
}) => {
  return (
    <div
      className={cn(
        'absolute inset-0 overflow-hidden pointer-events-none',
        className
      )}
    >
      {/* Floating AI-themed shapes */}
      <div
        className="absolute top-10 left-10 w-2 h-2 bg-purple-400 dark:bg-purple-500 rounded-full opacity-20 animate-pulse"
        style={{ animationDelay: '0s', animationDuration: '3s' }}
      />
      <div
        className="absolute top-20 right-20 w-1 h-1 bg-blue-400 dark:bg-blue-500 rounded-full opacity-30 animate-pulse"
        style={{ animationDelay: '1s', animationDuration: '4s' }}
      />
      <div
        className="absolute bottom-10 left-20 w-3 h-3 bg-indigo-400 dark:bg-indigo-500 rounded-full opacity-15 animate-pulse"
        style={{ animationDelay: '2s', animationDuration: '5s' }}
      />
      <div
        className="absolute bottom-20 right-10 w-1.5 h-1.5 bg-cyan-400 dark:bg-cyan-500 rounded-full opacity-25 animate-pulse"
        style={{ animationDelay: '0.5s', animationDuration: '3.5s' }}
      />

      {/* Subtle gradient overlay */}
      <div className="absolute inset-0 bg-gradient-to-br from-purple-50/30 via-transparent to-blue-50/30 dark:from-purple-950/20 dark:via-transparent dark:to-blue-950/20 opacity-50" />
    </div>
  );
};

interface AnimatedContainerProps {
  children: React.ReactNode;
  className?: string;
  delay?: number;
}

export const AnimatedContainer: React.FC<AnimatedContainerProps> = ({
  children,
  className,
  delay = 0,
}) => {
  const [isVisible, setIsVisible] = React.useState(false);

  React.useEffect(() => {
    const timer = setTimeout(() => {
      setIsVisible(true);
    }, delay);

    return () => clearTimeout(timer);
  }, [delay]);

  return (
    <div
      className={cn(
        'transition-all duration-700 ease-out',
        isVisible
          ? 'opacity-100 translate-y-0 scale-100'
          : 'opacity-0 translate-y-4 scale-95',
        className
      )}
    >
      {children}
    </div>
  );
};

interface GradientBorderProps {
  children: React.ReactNode;
  className?: string;
  gradient?: string;
  hover?: boolean;
}

export const GradientBorder: React.FC<GradientBorderProps> = ({
  children,
  className,
  gradient = 'from-purple-500 via-blue-500 to-cyan-500',
  hover = false,
}) => {
  return (
    <div className={cn('relative group', className)}>
      <div
        className={cn(
          'absolute -inset-0.5 bg-gradient-to-r rounded-lg blur opacity-75',
          gradient,
          hover &&
            'group-hover:opacity-100 transition duration-1000 group-hover:duration-200'
        )}
      />
      <div className="relative bg-background rounded-lg">{children}</div>
    </div>
  );
};

interface PulsingDotProps {
  className?: string;
  color?: string;
  size?: 'sm' | 'md' | 'lg';
}

export const PulsingDot: React.FC<PulsingDotProps> = ({
  className,
  color = 'bg-purple-500',
  size = 'md',
}) => {
  const sizeClasses = {
    sm: 'w-2 h-2',
    md: 'w-3 h-3',
    lg: 'w-4 h-4',
  };

  return (
    <div className={cn('relative', className)}>
      <div
        className={cn('rounded-full animate-pulse', color, sizeClasses[size])}
      />
      <div
        className={cn(
          'absolute top-0 left-0 rounded-full animate-ping opacity-75',
          color,
          sizeClasses[size]
        )}
      />
    </div>
  );
};
