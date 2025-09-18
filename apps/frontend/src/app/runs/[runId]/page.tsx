'use client';

import { Layout } from '@/components/Layout';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { RunDto, runsApi, RunEventDto } from '@/lib/api/runs';
import {
  ArrowLeft,
  Clock,
  AlertCircle,
} from 'lucide-react';
import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';

// Utility functions
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

function formatDuration(start?: string, finish?: string) {
  if (!start) return '-';
  if (!finish) return 'Running...';
  
  try {
    const startTime = new Date(start).getTime();
    const finishTime = new Date(finish).getTime();
    const durationMs = finishTime - startTime;
    const seconds = Math.floor(durationMs / 1000);
    
    if (seconds < 60) {
      return `${seconds}s`;
    } else if (seconds < 3600) {
      const minutes = Math.floor(seconds / 60);
      const remainingSeconds = seconds % 60;
      return `${minutes}m ${remainingSeconds}s`;
    } else {
      const hours = Math.floor(seconds / 3600);
      const minutes = Math.floor((seconds % 3600) / 60);
      return `${hours}h ${minutes}m`;
    }
  } catch {
    return '-';
  }
}

function NotFoundCard({ error }: { error?: string | null }) {
  const navigate = useNavigate();

  return (
    <Layout>
      <div className="container mx-auto py-20">
        <Card className="max-w-md mx-auto text-center">
          <CardHeader>
            <CardTitle className="text-2xl font-semibold flex items-center justify-center gap-2">
              <AlertCircle className="w-6 h-6 text-destructive" />
              {error ? 'Error Loading Run' : 'Run Not Found'}
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <p className="text-slate-600">
              {error || 
                'The requested run could not be found. It may have been deleted or the ID is incorrect.'
              }
            </p>
            <Button onClick={() => navigate('/app/runs')} className="w-full">
              <ArrowLeft className="w-4 h-4 mr-2" />
              Back to Runs
            </Button>
          </CardContent>
        </Card>
      </div>
    </Layout>
  );
}

export default function RunDetailPage() {
  const params = useParams();
  const navigate = useNavigate();
  const runId = params.runId as string;
  
  const [run, setRun] = useState<RunDto | null>(null);
  const [timeline, setTimeline] = useState<RunEventDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    
    async function fetchRunData() {
      if (!runId) return;
      
      setLoading(true);
      setError(null);
      
      try {
        // Fetch run details
        const runData = await runsApi.get(runId);
        if (cancelled) return;
        
        if (!runData) {
          setError('Run not found');
          return;
        }
        
        setRun(runData);
        
        // Fetch timeline
        try {
          const timelineData = await runsApi.getTimeline(runId);
          if (!cancelled) {
            setTimeline(timelineData);
          }
        } catch (timelineErr) {
          // Timeline is optional, don't fail the whole page if it fails
          console.warn('Failed to fetch timeline:', timelineErr);
        }
        
      } catch (err) {
        if (!cancelled) {
          const error = err as Error;
          setError(error.message || 'Failed to fetch run details');
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    }
    
    fetchRunData();
    
    return () => {
      cancelled = true;
    };
  }, [runId]);

  if (loading) {
    return (
      <Layout showProjectSelector={false}>
        <div className="container mx-auto py-6 space-y-6 px-4">
          <div className="flex items-center gap-4">
            <Button
              variant="ghost"
              size="sm"
              onClick={() => navigate('/app/runs')}
            >
              <ArrowLeft className="w-4 h-4 mr-2" />
              Back to Runs
            </Button>
          </div>
          <Card>
            <CardContent className="p-8">
              <div className="flex items-center justify-center">
                <div className="text-muted-foreground">Loading run details...</div>
              </div>
            </CardContent>
          </Card>
        </div>
      </Layout>
    );
  }

  if (error || !run) {
    return <NotFoundCard error={error} />;
  }

  return (
    <Layout showProjectSelector={false}>
      <RunDetailContent run={run} timeline={timeline} />
    </Layout>
  );
}

function RunDetailContent({ run, timeline }: { run: RunDto; timeline: RunEventDto[] }) {
  const navigate = useNavigate();

  return (
    <div className="container mx-auto py-6 space-y-6 px-4">
      {/* Header */}
      <div className="flex items-center gap-4">
        <Button
          variant="ghost"
          size="sm"
          onClick={() => navigate('/app/runs')}
        >
          <ArrowLeft className="w-4 h-4 mr-2" />
          Back to Runs
        </Button>
      </div>

      {/* Run Overview */}
      <Card>
        <CardHeader>
          <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4">
            <div>
              <CardTitle className="text-2xl font-bold">Run Details</CardTitle>
              <p className="text-muted-foreground font-mono text-sm">
                {run.id}
              </p>
            </div>
            <div className="flex items-center gap-2">
              {getStatusBadge(run.status || 'unknown')}
            </div>
          </div>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            <div>
              <h4 className="font-semibold text-sm text-muted-foreground mb-2">Suite Name</h4>
              <p className="font-mono">{run.suiteName || '-'}</p>
            </div>
            <div>
              <h4 className="font-semibold text-sm text-muted-foreground mb-2">Version</h4>
              <p className="font-mono">{run.version || '-'}</p>
            </div>
            <div>
              <h4 className="font-semibold text-sm text-muted-foreground mb-2">Actor</h4>
              <p>{run.actor || '-'}</p>
            </div>
            <div>
              <h4 className="font-semibold text-sm text-muted-foreground mb-2">Trigger</h4>
              <p>{run.trigger || '-'}</p>
            </div>
            <div>
              <h4 className="font-semibold text-sm text-muted-foreground mb-2">Steps Count</h4>
              <p>{run.stepsCount || 0}</p>
            </div>
            <div>
              <h4 className="font-semibold text-sm text-muted-foreground mb-2">Duration</h4>
              <p>{formatDuration(run.startedAt, run.finishedAt)}</p>
            </div>
            <div>
              <h4 className="font-semibold text-sm text-muted-foreground mb-2">Created At</h4>
              <p className="text-sm">{formatLocalDate(run.createdAt)}</p>
            </div>
            <div>
              <h4 className="font-semibold text-sm text-muted-foreground mb-2">Started At</h4>
              <p className="text-sm">{formatLocalDate(run.startedAt)}</p>
            </div>
            <div>
              <h4 className="font-semibold text-sm text-muted-foreground mb-2">Finished At</h4>
              <p className="text-sm">{formatLocalDate(run.finishedAt)}</p>
            </div>
          </div>
          
          {run.error && (
            <div className="mt-6 p-4 bg-destructive/10 border border-destructive/20 rounded-lg">
              <h4 className="font-semibold text-destructive mb-2 flex items-center gap-2">
                <AlertCircle className="w-4 h-4" />
                Error
              </h4>
              <p className="text-sm text-destructive/90">{run.error}</p>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Timeline */}
      {timeline && timeline.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle>Timeline</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {timeline.map((event, index) => (
                <div key={event.id} className="flex items-start gap-4">
                  <div className="flex-shrink-0 w-8 h-8 rounded-full bg-primary/10 flex items-center justify-center">
                    <div className="w-2 h-2 bg-primary rounded-full" />
                  </div>
                  <div className="flex-grow min-w-0">
                    <div className="flex items-center justify-between">
                      <p className="font-medium">{event.kind}</p>
                      <p className="text-sm text-muted-foreground">
                        {formatLocalDate(event.createdAt)}
                      </p>
                    </div>
                    {event.payload && (
                      <p className="text-sm text-muted-foreground mt-1">
                        {event.payload}
                      </p>
                    )}
                    {event.stepId && (
                      <p className="text-xs text-muted-foreground font-mono mt-1">
                        Step: {event.stepId}
                      </p>
                    )}
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}

      {/* Empty state for when there's no additional data */}
      {(!timeline || timeline.length === 0) && (
        <Card>
          <CardContent className="p-8 text-center">
            <Clock className="w-12 h-12 text-muted-foreground mx-auto mb-4" />
            <h3 className="text-lg font-semibold mb-2">No Timeline Data</h3>
            <p className="text-muted-foreground">
              Timeline information is not available for this run.
            </p>
          </CardContent>
        </Card>
      )}
    </div>
  );
}