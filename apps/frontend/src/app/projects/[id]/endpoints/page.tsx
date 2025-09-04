'use client';

import { Layout } from '@/components/Layout';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Select, SelectTrigger, SelectValue, SelectContent, SelectItem } from '@/components/ui/select';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { toast } from '@/hooks/use-toast';
import { endpointsApi } from '@/lib/api/endpoints';
import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';

interface EndpointBrief {
  id: string;
  method: string;
  path: string;
  summary?: string | null;
  tags?: string[] | null;
}

export default function ProjectEndpointsPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [endpoints, setEndpoints] = useState<EndpointBrief[]>([]);
  const [loading, setLoading] = useState(false);
  const [methodFilter, setMethodFilter] = useState<string>('ALL');
  const [textFilter, setTextFilter] = useState<string>('');

  useEffect(() => {
    if (!id) return;
    setLoading(true);
    endpointsApi.listByProject(id)
      .then(list => setEndpoints(
        list.map(x => ({
          id: x.id ?? '',
          method: (x.method ?? 'GET').toString(),
          path: x.path ?? '',
          summary: x.summary ?? null,
          tags: x.tags ?? []
        }))
      ))
      .catch(err =>
        toast({ title: 'Failed', description: err?.message ?? String(err) })
      )
      .finally(() => setLoading(false));
  }, [id]);

  return (
    <Layout>
      <div className="container mx-auto py-6">
        <div className="flex items-center justify-between mb-4">
          <h1 className="text-2xl font-bold">API Endpoints</h1>
          <div className="flex gap-2">
            <Button onClick={() => navigate(`/app/projects/${id}/openapi`)}>
              View Specs
            </Button>
            <Button onClick={() => navigate(`/app/projects/${id}`)}>
              Back
            </Button>
          </div>
        </div>

        <div className="grid gap-4">
          <div className="flex gap-2 items-center">
            <div style={{ width: 160 }}>
              <Select onValueChange={(v) => setMethodFilter(v)} defaultValue="ALL">
                <SelectTrigger>
                  <SelectValue>{methodFilter}</SelectValue>
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="ALL">ALL</SelectItem>
                  <SelectItem value="GET">GET</SelectItem>
                  <SelectItem value="POST">POST</SelectItem>
                  <SelectItem value="PUT">PUT</SelectItem>
                  <SelectItem value="PATCH">PATCH</SelectItem>
                  <SelectItem value="DELETE">DELETE</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <Input placeholder="Search path, summary or tags" value={textFilter} onChange={(e) => setTextFilter(e.target.value)} />
          </div>
          {loading ? (
            <div>Loading...</div>
          ) : endpoints.length === 0 ? (
            <div>No endpoints found</div>
          ) : (
            endpoints
              .filter(ep => {
                if (methodFilter && methodFilter !== 'ALL' && ep.method.toUpperCase() !== methodFilter) return false;
                if (!textFilter) return true;
                const q = textFilter.toLowerCase();
                if (ep.path?.toLowerCase().includes(q)) return true;
                if (ep.summary?.toLowerCase().includes(q)) return true;
                if (ep.tags && ep.tags.join(',').toLowerCase().includes(q)) return true;
                return false;
              })
              .map(ep => (
              <Card key={ep.id}>
                <CardHeader>
                  <CardTitle className="flex items-center justify-between">
                    <span className="font-mono text-sm">
                      {ep.method.toUpperCase()}
                    </span>
                    <span className="text-sm">{ep.path}</span>
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  <p className="text-sm text-muted-foreground">{ep.summary}</p>
                  {ep.tags && ep.tags.length > 0 && (
                    <div className="mt-2 text-xs text-muted-foreground">
                      Tags: {ep.tags.join(', ')}
                    </div>
                  )}
                </CardContent>
              </Card>
            ))
          )}
        </div>
      </div>
    </Layout>
  );
}
