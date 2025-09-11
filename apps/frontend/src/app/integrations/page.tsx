'use client';

import { Layout } from '@/components/Layout';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { toast } from '@/hooks/use-toast';
import { useState } from 'react';

// Mock data following the spec
const MOCK_INTEGRATIONS = {
  integrations: [
    {
      id: 'github',
      name: 'GitHub',
      description: 'Enable PR checks and comment with test results.',
      icon: '📦', // Using emoji for MVP, can be replaced with proper icons later
      status: 'connected' as const,
      repo: 'openai/chapi',
      actions: ['disconnect'],
    },
    {
      id: 'slack',
      name: 'Slack',
      description: 'Notify your team of test results directly in Slack.',
      icon: '💬',
      status: 'coming-soon' as const,
      actions: [],
    },
  ],
};

type IntegrationStatus = 'connected' | 'not-connected' | 'coming-soon';

interface Integration {
  id: string;
  name: string;
  description: string;
  icon: string;
  status: IntegrationStatus;
  repo?: string;
  actions: string[];
}

function IntegrationCard({
  integration,
  onStatusChange,
}: {
  integration: Integration;
  onStatusChange: (id: string, newStatus: IntegrationStatus) => void;
}) {
  const [showConfirmModal, setShowConfirmModal] = useState(false);

  const getStatusBadge = (status: IntegrationStatus) => {
    switch (status) {
      case 'connected':
        return (
          <Badge className="bg-emerald-100 text-emerald-800 hover:bg-emerald-100 dark:bg-emerald-900/20 dark:text-emerald-400">
            Connected
          </Badge>
        );
      case 'not-connected':
        return (
          <Badge
            variant="secondary"
            className="dark:bg-muted dark:text-muted-foreground"
          >
            Not Connected
          </Badge>
        );
      case 'coming-soon':
        return (
          <Badge className="bg-amber-100 text-amber-800 hover:bg-amber-100 dark:bg-amber-900/20 dark:text-amber-400">
            Coming Soon
          </Badge>
        );
    }
  };

  const handleConnect = () => {
    if (integration.id === 'github') {
      // Mock GitHub connection
      toast({
        title: 'GitHub connected successfully (mock)',
        description: 'Your GitHub integration is now active.',
      });
      onStatusChange(integration.id, 'connected');
    }
  };

  const handleDisconnect = () => {
    onStatusChange(integration.id, 'not-connected');
    setShowConfirmModal(false);
    toast({
      title: 'Integration disconnected',
      description: `${integration.name} has been disconnected.`,
    });
  };

  const getActionButton = () => {
    if (integration.status === 'coming-soon') {
      return (
        <Button disabled variant="outline" size="sm">
          Coming Soon
        </Button>
      );
    }

    if (integration.status === 'connected') {
      return (
        <Button
          variant="outline"
          size="sm"
          onClick={() => setShowConfirmModal(true)}
          className="text-rose-600 border-rose-200 hover:bg-rose-50 hover:text-rose-700 dark:text-rose-400 dark:border-rose-800 dark:hover:bg-rose-900/20 dark:hover:text-rose-300"
        >
          Disconnect
        </Button>
      );
    }

    return (
      <Button
        size="sm"
        onClick={handleConnect}
        className="bg-indigo-600 hover:bg-indigo-700 text-white dark:bg-indigo-500 dark:hover:bg-indigo-600"
      >
        Connect
      </Button>
    );
  };

  return (
    <>
      <Card className="border border-border hover:border-muted-foreground/20 transition-colors bg-card">
        <CardContent className="p-6">
          <div className="flex items-center gap-4">
            {/* Icon */}
            <div className="w-12 h-12 rounded-lg bg-muted flex items-center justify-center text-2xl">
              {integration.icon}
            </div>

            {/* Content */}
            <div className="flex-1 min-w-0">
              <div className="flex items-center gap-3 mb-1">
                <h3 className="text-lg font-semibold text-foreground">
                  {integration.name}
                </h3>
                {getStatusBadge(integration.status)}
              </div>
              <p className="text-sm text-muted-foreground mb-2">
                {integration.description}
              </p>
              {integration.status === 'connected' && integration.repo && (
                <p className="text-xs text-muted-foreground font-mono">
                  Connected to: {integration.repo}
                </p>
              )}
            </div>

            {/* Action */}
            <div className="flex-shrink-0">{getActionButton()}</div>
          </div>
        </CardContent>
      </Card>

      {/* Disconnect Confirmation Modal */}
      {showConfirmModal && (
        <div className="fixed inset-0 bg-black/50 dark:bg-black/70 flex items-center justify-center z-50">
          <Card className="w-96 mx-4 bg-background">
            <CardHeader>
              <CardTitle className="text-foreground">
                Disconnect {integration.name}?
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <p className="text-sm text-muted-foreground">
                This will disable {integration.name} integration. You can
                reconnect it anytime.
              </p>
              {integration.repo && (
                <p className="text-xs text-muted-foreground font-mono bg-muted p-2 rounded">
                  Repository: {integration.repo}
                </p>
              )}
            </CardContent>
            <div className="flex justify-end gap-2 p-6 pt-0">
              <Button
                variant="outline"
                onClick={() => setShowConfirmModal(false)}
              >
                Cancel
              </Button>
              <Button
                onClick={handleDisconnect}
                className="bg-rose-600 hover:bg-rose-700 text-white dark:bg-rose-500 dark:hover:bg-rose-600"
              >
                Disconnect
              </Button>
            </div>
          </Card>
        </div>
      )}
    </>
  );
}

function EmptyState() {
  return (
    <Card className="border-2 border-dashed border-border bg-card">
      <CardContent className="p-8 text-center">
        <div className="w-16 h-16 rounded-full bg-muted flex items-center justify-center mx-auto mb-4">
          <span className="text-2xl">🔗</span>
        </div>
        <h3 className="text-lg font-semibold text-foreground mb-2">
          No integrations yet
        </h3>
        <p className="text-sm text-muted-foreground mb-4">
          Connect GitHub to enable PR checks and get started with automated
          testing.
        </p>
        <Button className="bg-indigo-600 hover:bg-indigo-700 text-white dark:bg-indigo-500 dark:hover:bg-indigo-600">
          Connect GitHub
        </Button>
      </CardContent>
    </Card>
  );
}

export default function IntegrationsPage() {
  const [integrations, setIntegrations] = useState<Integration[]>(
    MOCK_INTEGRATIONS.integrations
  );

  const handleStatusChange = (id: string, newStatus: IntegrationStatus) => {
    setIntegrations(prev =>
      prev.map(integration => {
        if (integration.id === id) {
          const updated: Integration = { ...integration, status: newStatus };
          if (newStatus === 'not-connected') {
            delete updated.repo;
          }
          return updated;
        }
        return integration;
      })
    );
  };

  const hasConnectedIntegrations = integrations.some(
    int => int.status === 'connected'
  );

  return (
    <Layout showProjectSelector={false}>
      <div className="container mx-auto px-6 py-8 max-w-4xl">
        {/* Header */}
        <div className="mb-8">
          <h1 className="text-3xl font-semibold text-foreground mb-2">
            Integrations
          </h1>
          <p className="text-muted-foreground">
            Connect Chapi with your developer tools.
          </p>
        </div>

        {/* Main Content */}
        <div className="space-y-4">
          {integrations.length === 0 ? (
            <EmptyState />
          ) : (
            integrations.map(integration => (
              <IntegrationCard
                key={integration.id}
                integration={integration}
                onStatusChange={handleStatusChange}
              />
            ))
          )}
        </div>

        {/* Additional Info */}
        {hasConnectedIntegrations && (
          <div className="mt-8 p-4 bg-sky-50 border border-sky-200 rounded-lg dark:bg-sky-900/20 dark:border-sky-800">
            <div className="flex items-start gap-3">
              <div className="w-5 h-5 rounded-full bg-sky-400 dark:bg-sky-500 flex items-center justify-center flex-shrink-0 mt-0.5">
                <span className="text-white text-xs">ℹ</span>
              </div>
              <div>
                <h4 className="font-medium text-foreground mb-1">
                  Integration Active
                </h4>
                <p className="text-sm text-muted-foreground">
                  Your connected integrations will automatically receive
                  notifications when tests complete.
                </p>
              </div>
            </div>
          </div>
        )}
      </div>
    </Layout>
  );
}
