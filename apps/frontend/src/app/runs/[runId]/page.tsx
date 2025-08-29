"use client";

import { useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Copy, RefreshCw } from "lucide-react";
import { toast } from "@/hooks/use-toast";
import { ArtifactViewer } from "@/components/ArtifactViewer";

interface RunDetailPageProps {
  params: { runId: string };
}

type Failure = {
  testName: string;
  reason: string;
  path?: string;
};

type Artifact = {
  name: string;
  kind: 'req' | 'res';
  body: unknown;
  status?: number;
};

const mockRunDetail = {
  id: 'run-staging-42',
  projectName: 'Payment API',
  env: 'staging' as const,
  status: 'pass' as const,
  durationSec: 2.4,
  p95: 312,
  seed: 42,
  startedAt: '2 hours ago',
  failures: [
    {
      testName: 'POST /payments - Invalid card number',
      reason: 'Expected status 400, got 422',
      path: '/payments'
    },
    {
      testName: 'GET /payments/{id} - Missing payment',
      reason: 'Response missing required field: created_at',
      path: '/payments/123'
    }
  ] as Failure[],
  openApiDiff: {
    missing: ['email', 'phone'],
    extra: ['debug_id', 'internal_ref']
  },
  artifacts: [
    {
      name: 'POST /payments',
      kind: 'req' as const,
      body: {
        card_number: '4111111111111111',
        amount: 2500,
        currency: 'USD'
      },
      status: 422
    },
    {
      name: 'POST /payments',
      kind: 'res' as const,
      body: {
        error: 'Invalid card number format',
        code: 'INVALID_CARD',
        debug_id: 'dbg_123456'
      },
      status: 422
    }
  ] as Artifact[]
};

export default function RunDetailPage() {
  const { runId } = useParams();
  const navigate = useNavigate();
  const [activeTab, setActiveTab] = useState('summary');

  const handleRerun = () => {
    toast({ title: "Re-run started", description: "Running with seed=42" });
  };

  const handleCopyArtifact = (content: string) => {
    navigator.clipboard.writeText(content);
    toast({ title: "Copied to clipboard" });
  };

  return (
    <div className="container mx-auto py-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="space-y-1">
          <div className="flex items-center gap-3">
            <h1 className="text-3xl font-bold">{mockRunDetail.id}</h1>
            <Badge variant={mockRunDetail.status === 'pass' ? 'default' : 'destructive'}>
              {mockRunDetail.status}
            </Badge>
            <Badge variant="outline">{mockRunDetail.env}</Badge>
          </div>
          <p className="text-muted-foreground">{mockRunDetail.projectName}</p>
        </div>
        
        <Button onClick={handleRerun}>
          <RefreshCw className="w-4 h-4 mr-2" />
          Re-run (seed=42)
        </Button>
      </div>

      {/* Summary Stats */}
      <div className="grid gap-4 md:grid-cols-4">
        <Card>
          <CardContent className="pt-4">
            <div className="text-2xl font-bold">{mockRunDetail.durationSec}s</div>
            <p className="text-xs text-muted-foreground">Duration</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-4">
            <div className="text-2xl font-bold">{mockRunDetail.p95}ms</div>
            <p className="text-xs text-muted-foreground">P95 Latency</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-4">
            <div className="text-2xl font-bold">{mockRunDetail.seed}</div>
            <p className="text-xs text-muted-foreground">Seed</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-4">
            <div className="text-2xl font-bold">{mockRunDetail.failures.length}</div>
            <p className="text-xs text-muted-foreground">Failures</p>
          </CardContent>
        </Card>
      </div>

      <Tabs value={activeTab} onValueChange={setActiveTab}>
        <TabsList>
          <TabsTrigger value="summary">Summary</TabsTrigger>
          <TabsTrigger value="artifacts">Artifacts</TabsTrigger>
        </TabsList>

        <TabsContent value="summary" className="space-y-6">
          {/* Failures */}
          {mockRunDetail.failures.length > 0 && (
            <Card>
              <CardHeader>
                <CardTitle>Failures ({mockRunDetail.failures.length})</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  {mockRunDetail.failures.map((failure, idx) => (
                    <div key={idx} className="p-4 border rounded-lg bg-destructive/5">
                      <div className="font-medium text-destructive">{failure.testName}</div>
                      <p className="text-sm text-muted-foreground mt-1">{failure.reason}</p>
                      {failure.path && (
                        <code className="text-xs font-mono mt-2 block">{failure.path}</code>
                      )}
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>
          )}

          {/* OpenAPI Diff */}
          <Card>
            <CardHeader>
              <CardTitle>OpenAPI Contract Diff</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="grid gap-4 md:grid-cols-2">
                <div>
                  <h4 className="font-medium text-destructive mb-2">Missing Fields</h4>
                  <div className="space-y-1">
                    {mockRunDetail.openApiDiff.missing.map((field) => (
                      <code key={field} className="block text-sm font-mono text-destructive">
                        - {field}
                      </code>
                    ))}
                  </div>
                </div>
                <div>
                  <h4 className="font-medium text-warning mb-2">Extra Fields</h4>
                  <div className="space-y-1">
                    {mockRunDetail.openApiDiff.extra.map((field) => (
                      <code key={field} className="block text-sm font-mono text-warning">
                        + {field}
                      </code>
                    ))}
                  </div>
                </div>
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="artifacts">
          <ArtifactViewer artifacts={mockRunDetail.artifacts} />
        </TabsContent>
      </Tabs>
    </div>
  );
}