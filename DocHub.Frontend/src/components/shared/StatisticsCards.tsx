import React from 'react';
import { FileText, Clock, TrendingUp, Users } from 'lucide-react';
import { Card, CardContent } from '../ui/card';

interface StatisticsCardsProps {
  totalRequests?: number;
  pending?: number;
  approved?: number;
  thisMonth?: number;
  templates?: number;
  signatures?: number;
  customStats?: Array<{
    label: string;
    value: number;
    icon: React.ReactNode;
    color?: string;
  }>;
}

export function StatisticsCards({ 
  totalRequests = 0, 
  pending = 0, 
  approved = 0, 
  thisMonth = 12,
  templates,
  signatures,
  customStats
}: StatisticsCardsProps) {
  const defaultStats = [
    {
      label: 'Total Requests',
      value: totalRequests,
      icon: <FileText className="h-8 w-8 text-neon-blue" />,
    },
    {
      label: 'Pending',
      value: pending,
      icon: <Clock className="h-8 w-8 text-orange-400" />,
    },
    {
      label: 'Approved',
      value: approved,
      icon: <TrendingUp className="h-8 w-8 text-green-400" />,
    },
    {
      label: 'This Month',
      value: thisMonth,
      icon: <Users className="h-8 w-8 text-purple-400" />,
    },
  ];

  // If custom stats are provided, use them instead
  const statsToShow = customStats || defaultStats;

  // Add templates and signatures if provided
  if (templates !== undefined || signatures !== undefined) {
    const additionalStats = [];
    if (templates !== undefined) {
      additionalStats.push({
        label: 'Templates',
        value: templates,
        icon: <FileText className="h-8 w-8 text-green-400" />,
      });
    }
    if (signatures !== undefined) {
      additionalStats.push({
        label: 'Signatures',
        value: signatures,
        icon: <Users className="h-8 w-8 text-purple-400" />,
      });
    }
    statsToShow.splice(-2, 2, ...additionalStats);
  }

  return (
    <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
      {statsToShow.map((stat, index) => (
        <Card key={index} className="glass-panel border-glass-border">
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-muted-foreground">{stat.label}</p>
                <p className="text-2xl font-bold">{stat.value}</p>
              </div>
              {stat.icon}
            </div>
          </CardContent>
        </Card>
      ))}
    </div>
  );
}
