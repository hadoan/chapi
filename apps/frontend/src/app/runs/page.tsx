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
import { ExternalLink } from 'lucide-react';
import { useNavigate } from 'react-router-dom';

type Run = {
  id: string;
  projectId: string;
  projectName: string;
  env: 'local' | 'staging' | 'prod';
  status: 'pass' | 'fail' | 'running';
  durationSec: number;
  p95: number;
  startedAt: string;
};

const mockRuns: Run[] = [
  {
    id: 'run-staging-42',
    projectId: 'proj-1',
    projectName: 'Payment API',
    env: 'staging',
    status: 'pass',
    durationSec: 2.4,
    p95: 312,
    startedAt: '2 hours ago',
  },
  {
    id: 'run-local-41',
    projectId: 'proj-2',
    projectName: 'User Service',
    env: 'local',
    status: 'fail',
    durationSec: 1.8,
    p95: 567,
    startedAt: '4 hours ago',
  },
  {
    id: 'run-prod-40',
    projectId: 'proj-1',
    projectName: 'Payment API',
    env: 'prod',
    status: 'pass',
    durationSec: 3.2,
    p95: 189,
    startedAt: '1 day ago',
  },
  {
    id: 'run-staging-39',
    projectId: 'proj-3',
    projectName: 'Analytics Dashboard',
    env: 'staging',
    status: 'running',
    durationSec: 0,
    p95: 0,
    startedAt: '2 days ago',
  },
  {
    id: 'run-local-38',
    projectId: 'proj-2',
    projectName: 'User Service',
    env: 'local',
    status: 'fail',
    durationSec: 4.1,
    p95: 678,
    startedAt: '3 days ago',
  },
];

export default function RunsPage() {
  const navigate = useNavigate();
  return (
    <Layout showProjectSelector={false}>
      <div className="container mx-auto py-6 space-y-6">
        <div className="flex items-center justify-between">
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
            <CardTitle>Recent Runs</CardTitle>
          </CardHeader>
          <CardContent>
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Run ID</TableHead>
                  <TableHead>Project</TableHead>
                  <TableHead>Environment</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Duration</TableHead>
                  <TableHead>P95</TableHead>
                  <TableHead>Started</TableHead>
                  <TableHead></TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {mockRuns.map(run => (
                  <TableRow key={run.id}>
                    <TableCell className="font-mono">{run.id}</TableCell>
                    <TableCell>{run.projectName}</TableCell>
                    <TableCell>
                      <Badge variant="outline">{run.env}</Badge>
                    </TableCell>
                    <TableCell>
                      <Badge
                        variant={
                          run.status === 'pass'
                            ? 'default'
                            : run.status === 'fail'
                            ? 'destructive'
                            : 'secondary'
                        }
                      >
                        {run.status}
                      </Badge>
                    </TableCell>
                    <TableCell className="font-mono">
                      {run.status === 'running' ? '-' : `${run.durationSec}s`}
                    </TableCell>
                    <TableCell className="font-mono">
                      {run.status === 'running' ? '-' : `${run.p95}ms`}
                    </TableCell>
                    <TableCell className="text-muted-foreground">
                      {run.startedAt}
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
          </CardContent>
        </Card>
      </div>
    </Layout>
  );
}
