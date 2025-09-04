'use client';

import { Layout } from '@/components/Layout';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { toast } from '@/hooks/use-toast';
import { AuthService } from '@/lib/api/auth-service';
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

  useEffect(() => {
    if (!id) return;
    setLoading(true);
    AuthService.authenticatedFetch<EndpointBrief[]>(
      `/api/projects/${id}/endpoints`,
      { method: 'GET' }
    )
      .then(list => setEndpoints(list))
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
          {loading ? (
            <div>Loading...</div>
          ) : endpoints.length === 0 ? (
            <div>No endpoints found</div>
          ) : (
            endpoints.map(ep => (
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
