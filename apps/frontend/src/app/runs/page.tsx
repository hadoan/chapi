'use client';

import { Layout } from '@/components/Layout';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { RunDto, runsApi } from '@/lib/api/runs';
import { ExternalLink, LayoutGrid, Table2 } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';

type Run = {
  id: string;
  projectId?: string | null;
  projectName?: string | null;
  env?: string | null;
  status: string;
  durationSec?: number | null;
  p95?: number | null;
  startedAt?: string | null;
};

function formatLocalDate(iso?: string | null) {
  if (!iso) return '-';
  try {
    const d = new Date(iso);
    return d.toLocaleString();
  } catch {
    return iso;
  }
}

function getStatusBadge(status: string) {
  const key = (status ?? '').toString().toLowerCase();
  switch (key) {
    case 'running':
    case 'in_progress':
    case 'inprogress':
      return (
        <Badge variant="default">
          <svg
            className="w-3 h-3 mr-2 inline-block animate-spin"
            viewBox="0 0 24 24"
            fill="none"
            xmlns="http://www.w3.org/2000/svg"
            aria-hidden
          >
            <circle
              cx="12"
              cy="12"
              r="10"
              stroke="currentColor"
              strokeWidth="4"
              opacity="0.25"
            />
            <path
              d="M22 12a10 10 0 0 1-10 10"
              stroke="currentColor"
              strokeWidth="4"
              strokeLinecap="round"
            />
          </svg>
          Running
        </Badge>
      );
    case 'failed':
    case 'fail':
      return <Badge variant="destructive">Failed</Badge>;
    case 'passed':
    case 'pass':
    case 'success':
      return <Badge variant="default">Passed</Badge>;
    case 'pending':
    case 'queued':
      return <Badge variant="outline">Pending</Badge>;
    default:
      return <Badge variant="secondary">{status}</Badge>;
  }
}

export default function RunsPage() {
  const navigate = useNavigate();
  const [runs, setRuns] = useState<Run[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [mobileViewMode, setMobileViewMode] = useState<'cards' | 'table'>(
    'cards'
  );

  useEffect(() => {
    let cancelled = false;
    async function load() {
      setLoading(true);
      setError(null);
      try {
        const res = await runsApi.list(1, 20);
        if (cancelled) return;
        const items = res?.Items ?? [];
        const mapped: Run[] = items.map((r: RunDto) => ({
          id: r.id,
          projectId: r.projectId ?? undefined,
          projectName: undefined, // server doesn't return project name; could be joined later
          env: r.environmentId ? r.environmentId.toString() : undefined,
          status: r.status,
          durationSec: undefined,
          p95: undefined,
          startedAt: r.createdAt
            ? new Date(r.createdAt).toISOString()
            : undefined,
        }));
        setRuns(mapped);
      } catch (err) {
        const e = err as Error | undefined | null;
        setError(e?.message ?? 'Failed to load runs');
      } finally {
        if (!cancelled) setLoading(false);
      }
    }
    load();
    return () => {
      cancelled = true;
    };
  }, []);
  return (
    <Layout showProjectSelector={false}>
      <div className="container mx-auto py-6 space-y-6 px-4">
        <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4 md:gap-0">
          <div>
            <h1 className="text-3xl font-bold">Test Runs</h1>
            <p className="text-muted-foreground">
              Monitor and analyze your API test runs
            </p>
          </div>
          <Button onClick={() => navigate('/app')}>
            <ExternalLink className="w-4 h-4 mr-2" />
            New Run
          </Button>
        </div>

        <Card>
          <CardHeader>
            <div className="flex items-center justify-between">
              <CardTitle>Recent Runs</CardTitle>
              {/* Mobile view toggle */}
              <div className="flex items-center gap-1 sm:hidden">
                <Button
                  variant={mobileViewMode === 'cards' ? 'default' : 'ghost'}
                  size="sm"
                  onClick={() => setMobileViewMode('cards')}
                >
                  <LayoutGrid className="w-4 h-4" />
                </Button>
                <Button
                  variant={mobileViewMode === 'table' ? 'default' : 'ghost'}
                  size="sm"
                  onClick={() => setMobileViewMode('table')}
                >
                  <Table2 className="w-4 h-4" />
                </Button>
              </div>
            </div>
          </CardHeader>
          <CardContent>
            {loading && <div className="p-4">Loading runs...</div>}
            {error && <div className="p-4 text-destructive">{error}</div>}

            {/* Desktop / wide screens: table with horizontal scroll */}
            <div className="hidden sm:block">
              <div className="overflow-x-auto">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead className="min-w-[200px]">Run ID</TableHead>
                      <TableHead className="min-w-[150px]">Project</TableHead>
                      <TableHead className="min-w-[120px]">
                        Environment
                      </TableHead>
                      <TableHead className="min-w-[100px]">Status</TableHead>
                      <TableHead className="min-w-[100px]">Duration</TableHead>
                      <TableHead className="min-w-[80px]">P95</TableHead>
                      <TableHead className="min-w-[180px]">Started</TableHead>
                      <TableHead className="min-w-[60px]"></TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {runs.map(run => (
                      <TableRow key={run.id}>
                        <TableCell className="font-mono truncate max-w-[200px]">
                          {run.id}
                        </TableCell>
                        <TableCell className="truncate max-w-[150px]">
                          {run.projectName ?? run.projectId}
                        </TableCell>
                        <TableCell>
                          <Badge variant="outline">{run.env ?? '-'}</Badge>
                        </TableCell>
                        <TableCell>{getStatusBadge(run.status)}</TableCell>
                        <TableCell className="font-mono">
                          {run.status === 'running'
                            ? '-'
                            : run.durationSec
                            ? `${run.durationSec}s`
                            : '-'}
                        </TableCell>
                        <TableCell className="font-mono">
                          {run.status === 'running'
                            ? '-'
                            : run.p95
                            ? `${run.p95}ms`
                            : '-'}
                        </TableCell>
                        <TableCell className="text-muted-foreground">
                          {formatLocalDate(run.startedAt)}
                        </TableCell>
                        <TableCell>
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => navigate(`/app/runs/${run.id}`)}
                          >
                            <ExternalLink className="w-4 h-4" />
                          </Button>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
            </div>

            {/* Mobile: stacked cards for each run */}
            {mobileViewMode === 'cards' && (
              <div className="flex flex-col gap-4 sm:hidden">
                {runs.map(run => (
                  <Card key={run.id} className="shadow-sm">
                    <CardContent className="p-4">
                      <div className="space-y-3">
                        {/* Header with Run ID and Status */}
                        <div className="flex items-start justify-between">
                          <div className="flex-1 min-w-0">
                            <div className="font-mono text-sm font-medium truncate text-foreground">
                              {run.id}
                            </div>
                            {run.projectName || run.projectId ? (
                              <div className="text-sm text-muted-foreground truncate mt-1">
                                {run.projectName ?? run.projectId}
                              </div>
                            ) : null}
                          </div>
                          <div className="ml-3 flex-shrink-0">
                            {getStatusBadge(run.status)}
                          </div>
                        </div>

                        {/* Details grid */}
                        <div className="grid grid-cols-2 gap-3 text-sm">
                          <div>
                            <div className="text-xs font-medium text-muted-foreground mb-1">
                              Environment
                            </div>
                            <Badge variant="outline" className="h-6">
                              {run.env ?? '-'}
                            </Badge>
                          </div>
                          <div>
                            <div className="text-xs font-medium text-muted-foreground mb-1">
                              Duration
                            </div>
                            <div className="font-mono text-sm">
                              {run.status === 'running'
                                ? '-'
                                : run.durationSec
                                ? `${run.durationSec}s`
                                : '-'}
                            </div>
                          </div>
                          <div>
                            <div className="text-xs font-medium text-muted-foreground mb-1">
                              P95
                            </div>
                            <div className="font-mono text-sm">
                              {run.status === 'running'
                                ? '-'
                                : run.p95
                                ? `${run.p95}ms`
                                : '-'}
                            </div>
                          </div>
                          <div>
                            <div className="text-xs font-medium text-muted-foreground mb-1">
                              Started
                            </div>
                            <div className="text-sm">
                              {formatLocalDate(run.startedAt)}
                            </div>
                          </div>
                        </div>

                        {/* Action button */}
                        <div className="pt-2 border-t">
                          <Button
                            variant="ghost"
                            size="sm"
                            className="w-full justify-start"
                            onClick={() => navigate(`/app/runs/${run.id}`)}
                          >
                            <ExternalLink className="w-4 h-4 mr-2" />
                            View Details
                          </Button>
                        </div>
                      </div>
                    </CardContent>
                  </Card>
                ))}
              </div>
            )}

            {/* Mobile: compact table view */}
            {mobileViewMode === 'table' && (
              <div className="sm:hidden">
                <div className="overflow-x-auto">
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead className="min-w-[180px]">Run ID</TableHead>
                        <TableHead className="min-w-[100px]">Status</TableHead>
                        <TableHead className="min-w-[100px]">
                          Environment
                        </TableHead>
                        <TableHead className="min-w-[160px]">Started</TableHead>
                        <TableHead className="min-w-[60px]"></TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {runs.map(run => (
                        <TableRow key={run.id}>
                          <TableCell className="font-mono truncate max-w-[180px]">
                            <div>{run.id}</div>
                            {run.projectName || run.projectId ? (
                              <div className="text-xs text-muted-foreground truncate">
                                {run.projectName ?? run.projectId}
                              </div>
                            ) : null}
                          </TableCell>
                          <TableCell>{getStatusBadge(run.status)}</TableCell>
                          <TableCell>
                            <Badge variant="outline" className="text-xs">
                              {run.env ?? '-'}
                            </Badge>
                          </TableCell>
                          <TableCell className="text-muted-foreground text-sm">
                            {formatLocalDate(run.startedAt)}
                          </TableCell>
                          <TableCell>
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => navigate(`/app/runs/${run.id}`)}
                            >
                              <ExternalLink className="w-4 h-4" />
                            </Button>
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </div>
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </Layout>
  );
}
