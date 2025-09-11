'use client';

import { Layout } from '@/components/Layout';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
import { Input } from '@/components/ui/input';
import { ScrollArea } from '@/components/ui/scroll-area';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { toast } from '@/hooks/use-toast';
import {
  ArrowLeft,
  CheckCircle,
  Clock,
  Copy,
  Download,
  ExternalLink,
  Eye,
  EyeOff,
  Pause,
  Play,
  Search,
} from 'lucide-react';
import { useEffect, useRef, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';

// Types
interface RunData {
  id: string;
  projectId: string;
  env: {
    id: string;
    name: string;
    baseUrl: string;
    secrets: Record<string, unknown>;
  };
  status: string;
  createdAt: string;
  startedAt: string;
  completedAt: string;
  durationMs: number;
  cloudLogsUrl: string;
  source: {
    conversationId: string;
    messageId: string;
    messagePreview: string;
  };
  card: {
    role: string;
    heading: string;
    plan: string[];
    files: Array<{ path: string; addedLines: number }>;
    actions: string[];
  };
  results: {
    total: number;
    passed: number;
    failed: number;
    byFile: Array<{
      path: string;
      passed: number;
      failed: number;
      durationMs: number;
    }>;
  };
  artifacts: {
    zipUrl: string;
    logsUrl: string;
    resultsUrl: string;
  };
  runPack: {
    id: string;
    mode: string;
    files: Array<{
      path: string;
      role: string;
      content: string;
    }>;
  };
  timeline: Array<{
    ts: string;
    label: string;
  }>;
  logs: {
    stream: string[];
    sseMock: {
      enabled: boolean;
      intervalMs: number;
    };
  };
}

// Mock data
const MOCK_RUNS = {
  'run-staging-42': {
    id: 'run-staging-42',
    projectId: 'proj-email-123',
    env: {
      id: 'env-staging',
      name: 'staging',
      baseUrl: 'https://staging.api.shipmvp.com',
      secrets: {
        TOKEN: '••••••••',
        API_KEY: '••••••••',
      },
    },
    status: 'passed',
    createdAt: '2025-09-10T09:12:15Z',
    startedAt: '2025-09-10T09:12:20Z',
    completedAt: '2025-09-10T09:12:54Z',
    durationMs: 34000,
    cloudLogsUrl: 'https://logs.example.com/run/run-staging-42',
    source: {
      conversationId: 'conv-77',
      messageId: 'msg-891',
      messagePreview: 'Generate smoke tests for our email service API',
    },
    card: {
      role: 'Chapi',
      heading: "I'll create smoke tests for your email service API.",
      plan: [
        'Analyze endpoints to identify key email operations.',
        'Generate CRUD tests focusing on send and status checks.',
        'Verify authentication on protected endpoints.',
        'Include basic edge-case validations.',
      ],
      files: [
        { path: 'tests/email-service/smoke.sh', addedLines: 90 },
        { path: 'tests/email-service/auth.sh', addedLines: 50 },
        { path: 'tests/email-service/crud.sh', addedLines: 70 },
      ],
      actions: ['RUN_CLOUD', 'DOWNLOAD_RUN_PACK', 'ADD_NEGATIVES'],
    },
    results: {
      total: 10,
      passed: 10,
      failed: 0,
      byFile: [
        {
          path: 'tests/email-service/auth.sh',
          passed: 3,
          failed: 0,
          durationMs: 4200,
        },
        {
          path: 'tests/email-service/smoke.sh',
          passed: 5,
          failed: 0,
          durationMs: 6200,
        },
        {
          path: 'tests/email-service/crud.sh',
          passed: 2,
          failed: 0,
          durationMs: 5800,
        },
      ],
    },
    artifacts: {
      zipUrl: 'sandbox:/downloads/run-staging-42-pack.zip',
      logsUrl: 'sandbox:/downloads/run-staging-42-logs.txt',
      resultsUrl: 'sandbox:/downloads/run-staging-42-results.json',
    },
    runPack: {
      id: 'pack-aaa-bbb',
      mode: 'bash-curl',
      files: [
        {
          path: 'tests/email-service/auth.sh',
          role: 'AUTH',
          content: `#!/usr/bin/env bash\nset -euo pipefail\necho "AUTH test script content..."`,
        },
        {
          path: 'tests/email-service/smoke.sh',
          role: 'SMOKE',
          content: `#!/usr/bin/env bash
set -euo pipefail
echo "SMOKE tests stub"`,
        },
        {
          path: 'tests/email-service/crud.sh',
          role: 'CRUD',
          content: `#!/usr/bin/env bash
set -euo pipefail
echo "CRUD tests stub"`,
        },
      ],
    },
    timeline: [
      { ts: '2025-09-10T09:12:15Z', label: 'Queued' },
      { ts: '2025-09-10T09:12:20Z', label: 'Running' },
      { ts: '2025-09-10T09:12:26Z', label: 'Generated RunPack' },
      { ts: '2025-09-10T09:12:39Z', label: 'Executing' },
      { ts: '2025-09-10T09:12:54Z', label: 'Completed' },
    ],
    logs: {
      stream: [
        '09:12:20.100  INFO  run: starting worker',
        '09:12:21.234  INFO  pack: 3 files added',
        '09:12:24.011  INFO  env: BASE_URL=https://staging.api.shipmvp.com',
        '09:12:39.210  INFO  exec: bash tests/email-service/auth.sh',
        '09:12:43.442  PASS  POST /api/Email/welcome without token -> 401/403',
        '09:12:47.108  PASS  POST /api/Email/welcome with invalid token -> 401/403',
        '09:12:52.779  PASS  POST /api/Email/welcome with valid token -> 2xx',
        '09:12:54.000  INFO  summary: passed=10 failed=0 duration=34s',
      ],
      sseMock: {
        enabled: true,
        intervalMs: 1200,
      },
    },
  },
};

function NotFoundCard() {
  const navigate = useNavigate();

  return (
    <Layout>
      <div className="container mx-auto py-20">
        <Card className="max-w-md mx-auto text-center">
          <CardHeader>
            <CardTitle className="text-2xl font-semibold">
              Run Not Found
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <p className="text-slate-600">
              The requested run could not be found. It may have been deleted or
              the ID is incorrect.
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

  const run = MOCK_RUNS[runId as keyof typeof MOCK_RUNS];

  if (!run) {
    return <NotFoundCard />;
  }

  return (
    <Layout showProjectSelector={false}>
      <RunDetailContent run={run} />
    </Layout>
  );
}

function RunDetailContent({ run }: { run: RunData }) {
  const navigate = useNavigate();
  const [searchQuery, setSearchQuery] = useState('');
  const [autoScroll, setAutoScroll] = useState(true);
  const [streamingLogs, setStreamingLogs] = useState([...run.logs.stream]);
  const [isStreaming, setIsStreaming] = useState(true);
  const [selectedFile, setSelectedFile] = useState<string | null>(null);
  const [showSecrets, setShowSecrets] = useState(false);
  const [showRerunModal, setShowRerunModal] = useState(false);
  const logsEndRef = useRef<HTMLDivElement>(null);
  const scrollAreaRef = useRef<HTMLDivElement>(null);
  const [isUserScrolling, setIsUserScrolling] = useState(false);
  const [pauseStreaming, setPauseStreaming] = useState(false);

  // Mock SSE streaming - only when not paused and user is not scrolling
  useEffect(() => {
    if (!isStreaming || pauseStreaming || isUserScrolling) return;

    const interval = setInterval(() => {
      const mockMessages = [
        '09:13:01.123  INFO  stream: mock update',
        '09:13:05.456  DEBUG worker: heartbeat',
        '09:13:10.789  INFO  stream: still running',
      ];

      setStreamingLogs(prev => [
        ...prev,
        mockMessages[Math.floor(Math.random() * mockMessages.length)],
      ]);
    }, run.logs.sseMock.intervalMs);

    return () => clearInterval(interval);
  }, [
    isStreaming,
    run.logs.sseMock.intervalMs,
    pauseStreaming,
    isUserScrolling,
  ]);

  // Only auto-scroll when user is at bottom and not manually scrolling
  useEffect(() => {
    if (
      autoScroll &&
      !isUserScrolling &&
      !pauseStreaming &&
      logsEndRef.current
    ) {
      logsEndRef.current.scrollIntoView({ behavior: 'auto', block: 'end' });
    }
  }, [streamingLogs, autoScroll, isUserScrolling, pauseStreaming]);

  // Handle scroll detection with debouncing
  const handleScroll = (event: React.UIEvent<HTMLDivElement>) => {
    const element = event.currentTarget;
    const isAtBottom =
      Math.abs(
        element.scrollHeight - element.scrollTop - element.clientHeight
      ) <= 10;

    if (!isAtBottom) {
      // User scrolled up - pause everything
      if (!isUserScrolling) {
        setIsUserScrolling(true);
        setPauseStreaming(true);
        setAutoScroll(false);
      }
    } else if (isAtBottom && (isUserScrolling || pauseStreaming)) {
      // User scrolled back to bottom - resume
      setIsUserScrolling(false);
      setPauseStreaming(false);
      setAutoScroll(true);
    }
  };

  const getStatusBadge = (status: string) => {
    const colors = {
      queued: 'text-amber-600 bg-amber-100',
      running: 'text-blue-600 bg-blue-100',
      passed: 'text-emerald-600 bg-emerald-100',
      failed: 'text-rose-600 bg-rose-100',
    };

    return (
      <Badge className={colors[status as keyof typeof colors] || colors.queued}>
        {status}
      </Badge>
    );
  };

  const formatDuration = (ms: number) => {
    const seconds = Math.floor(ms / 1000);
    const minutes = Math.floor(seconds / 60);
    if (minutes > 0) {
      return `${minutes}m ${seconds % 60}s`;
    }
    return `${seconds}s`;
  };

  const formatTimestamp = (iso: string) => {
    return new Date(iso).toLocaleString();
  };

  const copyToClipboard = (text: string) => {
    navigator.clipboard.writeText(text);
    toast({
      title: 'Copied to clipboard',
      description: 'Content has been copied to your clipboard.',
    });
  };

  const handleRerun = () => {
    setShowRerunModal(false);
    toast({
      title: 'Re-run queued (mock)',
      description: `Run ${run.id} has been queued for re-execution.`,
    });
  };

  const handleDownload = () => {
    toast({
      title: 'Download started (mock)',
      description: 'Run pack download has been initiated.',
    });
  };

  const filteredLogs = streamingLogs.filter(log =>
    log.toLowerCase().includes(searchQuery.toLowerCase())
  );

  return (
    <div className="h-screen flex flex-col">
      {/* Header Bar */}
      <div
        data-testid="run-header"
        className="sticky top-0 z-10 bg-background border-b border-border px-6 py-4"
      >
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-4">
            <Button
              variant="ghost"
              size="sm"
              onClick={() => navigate('/app/runs')}
              className="text-muted-foreground hover:text-foreground"
            >
              <ArrowLeft className="w-4 h-4 mr-2" />
              Back to Runs
            </Button>

            <div className="flex items-center gap-3">
              <div className="flex items-center gap-2">
                <span className="font-mono text-sm font-medium text-foreground">
                  {run.id}
                </span>
                <Button
                  variant="ghost"
                  size="sm"
                  className="h-6 w-6 p-0"
                  onClick={() => copyToClipboard(run.id)}
                >
                  <Copy className="w-3 h-3" />
                </Button>
              </div>

              <div data-testid="status-badge">{getStatusBadge(run.status)}</div>

              <span className="text-sm text-muted-foreground">
                {formatTimestamp(run.createdAt)}
              </span>
            </div>
          </div>

          <div className="flex items-center gap-2">
            <Button
              data-testid="action-rerun"
              variant="default"
              size="sm"
              onClick={() => setShowRerunModal(true)}
            >
              <Play className="w-4 h-4 mr-2" />
              Re-run
            </Button>

            <Button
              data-testid="action-download"
              variant="outline"
              size="sm"
              onClick={handleDownload}
            >
              <Download className="w-4 h-4 mr-2" />
              Download Run Pack
            </Button>

            <Button
              data-testid="action-opencloud"
              variant="outline"
              size="sm"
              disabled={!run.cloudLogsUrl}
              onClick={() =>
                run.cloudLogsUrl && window.open(run.cloudLogsUrl, '_blank')
              }
            >
              <ExternalLink className="w-4 h-4 mr-2" />
              Open in Cloud Logs
            </Button>
          </div>
        </div>
      </div>

      {/* Summary Strip */}
      <div className="px-6 py-4 bg-muted/30 border-b border-border">
        <div className="grid grid-cols-4 gap-4">
          <Card className="p-4">
            <div className="flex items-center justify-between">
              <div>
                <div className="text-2xl font-semibold text-foreground">
                  {getStatusBadge(run.status)}
                </div>
                <div className="text-sm text-muted-foreground">
                  Duration: {formatDuration(run.durationMs)}
                </div>
              </div>
              <Clock className="w-6 h-6 text-muted-foreground" />
            </div>
          </Card>

          <Card className="p-4">
            <div className="flex items-center justify-between">
              <div>
                <div className="text-2xl font-semibold text-emerald-600 dark:text-emerald-400">
                  {run.results.passed}
                </div>
                <div className="text-sm text-muted-foreground">
                  {run.results.total} total, {run.results.failed} failed
                </div>
              </div>
              <CheckCircle className="w-6 h-6 text-emerald-500 dark:text-emerald-400" />
            </div>
          </Card>

          <Card className="p-4">
            <div className="flex items-center justify-between">
              <div>
                <div className="text-lg font-semibold text-foreground">
                  {run.env.name}
                </div>
                <div className="text-sm text-muted-foreground truncate">
                  {run.env.baseUrl}
                </div>
              </div>
            </div>
          </Card>

          <Card className="p-4">
            <div className="flex items-center justify-between">
              <div>
                <div className="text-sm font-medium text-foreground">
                  Source
                </div>
                <div className="text-sm text-muted-foreground truncate">
                  {run.source.messagePreview}
                </div>
              </div>
            </div>
          </Card>
        </div>
      </div>

      {/* Main Content */}
      <div className="flex-1 flex min-h-0">
        {/* Left Panel - 2/3 width */}
        <div className="flex-1 flex flex-col border-r border-border">
          <Tabs defaultValue="logs" className="flex-1 flex flex-col">
            <TabsList className="grid grid-cols-3 mx-6 mt-4">
              <TabsTrigger value="logs">Logs</TabsTrigger>
              <TabsTrigger value="results">Results</TabsTrigger>
              <TabsTrigger value="timeline">Timeline</TabsTrigger>
            </TabsList>

            <TabsContent
              value="logs"
              className="flex-1 flex flex-col px-6 pb-6"
            >
              <Card className="flex-1 flex flex-col">
                <CardHeader className="pb-3">
                  <div className="flex items-center justify-between">
                    <CardTitle className="text-lg flex items-center gap-2">
                      Live Logs
                      {pauseStreaming && (
                        <Badge variant="secondary" className="text-xs">
                          Paused - Scroll to bottom to resume
                        </Badge>
                      )}
                    </CardTitle>
                    <div className="flex items-center gap-2">
                      <div className="flex items-center gap-2">
                        <Checkbox
                          id="auto-scroll"
                          checked={autoScroll}
                          onCheckedChange={checked =>
                            setAutoScroll(checked === true)
                          }
                        />
                        <label htmlFor="auto-scroll" className="text-sm">
                          Auto-scroll
                        </label>
                      </div>

                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => setIsStreaming(!isStreaming)}
                        className={pauseStreaming ? 'opacity-50' : ''}
                      >
                        {isStreaming && !pauseStreaming ? (
                          <Pause className="w-4 h-4" />
                        ) : (
                          <Play className="w-4 h-4" />
                        )}
                        {isStreaming && !pauseStreaming ? 'Pause' : 'Resume'}
                      </Button>

                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() =>
                          copyToClipboard(streamingLogs.join('\n'))
                        }
                      >
                        <Copy className="w-4 h-4 mr-2" />
                        Copy all
                      </Button>

                      {(!autoScroll || pauseStreaming) && (
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => {
                            setAutoScroll(true);
                            setIsUserScrolling(false);
                            setPauseStreaming(false);
                            logsEndRef.current?.scrollIntoView({
                              behavior: 'smooth',
                              block: 'end',
                            });
                          }}
                        >
                          Jump to bottom
                        </Button>
                      )}
                    </div>
                  </div>

                  <div className="relative">
                    <Search className="absolute left-3 top-3 w-4 h-4 text-muted-foreground" />
                    <Input
                      placeholder="Search logs..."
                      value={searchQuery}
                      onChange={e => setSearchQuery(e.target.value)}
                      className="pl-10"
                    />
                  </div>
                </CardHeader>

                <CardContent className="flex-1 p-0 min-h-0">
                  <div
                    ref={scrollAreaRef}
                    onScroll={handleScroll}
                    className="h-full overflow-y-auto font-mono text-sm"
                    data-testid="logs-viewer"
                  >
                    <div className="p-4 space-y-1">
                      {filteredLogs.map((log, index) => (
                        <div
                          key={index}
                          className="text-foreground whitespace-pre-wrap"
                          dangerouslySetInnerHTML={{
                            __html: searchQuery
                              ? log.replace(
                                  new RegExp(searchQuery, 'gi'),
                                  match =>
                                    `<mark class="bg-yellow-200 dark:bg-yellow-900/40">${match}</mark>`
                                )
                              : log,
                          }}
                        />
                      ))}
                      <div ref={logsEndRef} />
                    </div>
                  </div>
                </CardContent>
              </Card>
            </TabsContent>

            <TabsContent
              value="results"
              className="flex-1 flex flex-col px-6 pb-6"
            >
              <Card className="flex-1">
                <CardHeader className="">
                  <CardTitle>Test Results</CardTitle>
                </CardHeader>
                <CardContent className="flex-1">
                  <Table data-testid="results-table">
                    <TableHeader>
                      <TableRow>
                        <TableHead>File</TableHead>
                        <TableHead>Passed</TableHead>
                        <TableHead>Failed</TableHead>
                        <TableHead>Duration</TableHead>
                        <TableHead></TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {run.results.byFile.map(file => (
                        <TableRow key={file.path}>
                          <TableCell className="font-mono text-sm">
                            {file.path}
                          </TableCell>
                          <TableCell>
                            <span className="text-emerald-600 font-medium">
                              {file.passed}
                            </span>
                          </TableCell>
                          <TableCell>
                            <span className="text-rose-600 font-medium">
                              {file.failed}
                            </span>
                          </TableCell>
                          <TableCell>
                            {formatDuration(file.durationMs)}
                          </TableCell>
                          <TableCell>
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => setSelectedFile(file.path)}
                            >
                              View
                            </Button>
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </CardContent>
              </Card>
            </TabsContent>

            <TabsContent
              value="timeline"
              className="flex-1 flex flex-col px-6 pb-6"
            >
              <Card className="flex-1 flex flex-col">
                <CardHeader className="mt-0">
                  <CardTitle>Execution Timeline</CardTitle>
                </CardHeader>
                <CardContent className="flex-1" data-testid="timeline-panel">
                  <div className="space-y-4">
                    {run.timeline.map((step, index: number) => (
                      <div key={index} className="flex items-center gap-4">
                        <div
                          className={`w-3 h-3 rounded-full ${
                            index === run.timeline.length - 1
                              ? run.status === 'passed'
                                ? 'bg-emerald-500 dark:bg-emerald-400'
                                : 'bg-rose-500 dark:bg-rose-400'
                              : 'bg-muted-foreground'
                          }`}
                        />
                        <div className="flex-1">
                          <div className="font-medium text-foreground">
                            {step.label}
                          </div>
                          <div className="text-sm text-muted-foreground">
                            {formatTimestamp(step.ts)}
                          </div>
                        </div>
                      </div>
                    ))}
                  </div>
                </CardContent>
              </Card>
            </TabsContent>
          </Tabs>
        </div>

        {/* Right Sidebar - 1/3 width */}
        <div className="w-1/3 p-6 space-y-6 overflow-y-auto">
          {/* Files Panel */}
          <Card data-testid="files-panel">
            <CardHeader>
              <CardTitle>Files</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-2">
                {run.runPack.files.map(file => (
                  <div
                    key={file.path}
                    className="flex items-center justify-between p-2 rounded bg-muted/50"
                  >
                    <span className="font-mono text-sm truncate text-foreground">
                      {file.path}
                    </span>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => setSelectedFile(file.path)}
                    >
                      <Eye className="w-4 h-4" />
                    </Button>
                  </div>
                ))}
              </div>

              {selectedFile && (
                <div className="mt-4 border-t border-border pt-4">
                  <div className="flex items-center justify-between mb-2">
                    <span className="font-mono text-sm font-medium text-foreground">
                      {selectedFile}
                    </span>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => {
                        const file = run.runPack.files.find(
                          f => f.path === selectedFile
                        );
                        if (file) copyToClipboard(file.content);
                      }}
                    >
                      <Copy className="w-4 h-4" />
                    </Button>
                  </div>
                  <ScrollArea className="h-40 bg-slate-900 dark:bg-slate-950 text-green-400 dark:text-green-300 p-3 rounded font-mono text-xs">
                    <pre className="whitespace-pre-wrap">
                      {run.runPack.files.find(f => f.path === selectedFile)
                        ?.content || ''}
                    </pre>
                  </ScrollArea>
                </div>
              )}
            </CardContent>
          </Card>

          {/* Environment Panel */}
          <Card data-testid="env-panel">
            <CardHeader>
              <div className="flex items-center justify-between">
                <CardTitle>Environment</CardTitle>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => setShowSecrets(!showSecrets)}
                >
                  {showSecrets ? (
                    <EyeOff className="w-4 h-4" />
                  ) : (
                    <Eye className="w-4 h-4" />
                  )}
                </Button>
              </div>
            </CardHeader>
            <CardContent>
              <div className="space-y-3">
                <div>
                  <div className="text-sm font-medium text-foreground">
                    BASE_URL
                  </div>
                  <div className="text-sm text-muted-foreground font-mono">
                    {run.env.baseUrl}
                  </div>
                </div>
                {Object.entries(run.env.secrets).map(([key, value]) => (
                  <div key={key}>
                    <div className="text-sm font-medium text-foreground">
                      {key}
                    </div>
                    <div className="text-sm text-muted-foreground font-mono">
                      {showSecrets ? 'sk-actual-secret-value' : String(value)}
                    </div>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>

          {/* Card Panel */}
          <Card data-testid="card-panel">
            <CardHeader>
              <CardTitle>{run.card.role}</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                <p className="text-sm text-foreground">{run.card.heading}</p>

                <div>
                  <div className="text-sm font-medium mb-2 text-foreground">
                    Plan:
                  </div>
                  <ul className="space-y-1">
                    {run.card.plan.map((item: string, index: number) => (
                      <li key={index} className="text-sm text-muted-foreground">
                        • {item}
                      </li>
                    ))}
                  </ul>
                </div>

                <div>
                  <div className="text-sm font-medium mb-2 text-foreground">
                    Files generated:
                  </div>
                  <div className="space-y-1">
                    {run.card.files.map((file, index: number) => (
                      <div
                        key={index}
                        className="text-sm text-muted-foreground"
                      >
                        <span className="font-mono">{file.path}</span>
                        <span className="text-emerald-600 dark:text-emerald-400 ml-2">
                          +{file.addedLines}
                        </span>
                      </div>
                    ))}
                  </div>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>

      {/* Re-run Modal */}
      {showRerunModal && (
        <div className="fixed inset-0 bg-black/50 dark:bg-black/70 flex items-center justify-center z-50">
          <Card className="w-96">
            <CardHeader>
              <CardTitle>Re-run Test</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <p className="text-sm text-muted-foreground">
                This will re-execute the run with the same configuration.
              </p>
              <div>
                <label className="text-sm font-medium text-foreground">
                  Environment:
                </label>
                <div className="text-sm text-muted-foreground mt-1">
                  {run.env.name}
                </div>
              </div>
            </CardContent>
            <div className="flex justify-end gap-2 p-6 pt-0">
              <Button
                variant="outline"
                onClick={() => setShowRerunModal(false)}
              >
                Cancel
              </Button>
              <Button onClick={handleRerun}>Confirm Re-run</Button>
            </div>
          </Card>
        </div>
      )}
    </div>
  );
}
