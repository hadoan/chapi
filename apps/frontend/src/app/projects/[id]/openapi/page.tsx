"use client";

import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { Layout } from '@/components/Layout';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { apiSpecsApi } from '@/lib/api/apispecs';
import { toast } from '@/hooks/use-toast';

interface ApiSpecBrief {
  id: string;
  projectId: string;
  sourceUrl?: string | null;
  version?: string | null;
  createdAt?: string | null;
}

export default function ProjectOpenApiPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [specs, setSpecs] = useState<ApiSpecBrief[]>([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!id) return;
    setLoading(true);
    apiSpecsApi.listByProject(id)
      .then(list => setSpecs((list || []).map(s => ({ id: s.id ?? '', projectId: s.projectId ?? '', sourceUrl: s.sourceUrl ?? null, version: s.version ?? null, createdAt: s.createdAt ?? null }))))
      .catch(err => toast({ title: 'Failed', description: err?.message ?? String(err) }))
      .finally(() => setLoading(false));
  }, [id]);

  return (
    <Layout>
      <div className="container mx-auto py-6">
        <div className="flex items-center justify-between mb-4">
          <h1 className="text-2xl font-bold">OpenAPI Specs</h1>
          <div className="flex gap-2">
            <Button onClick={() => navigate(`/app/projects/${id}`)}>Back</Button>
          </div>
        </div>

        <div className="grid gap-4">
          {loading ? (
            <div>Loading...</div>
          ) : specs.length === 0 ? (
            <div>No specs found</div>
          ) : (
            specs.map(s => (
              <Card key={s.id}>
                <CardHeader>
                  <CardTitle className="flex items-center justify-between">
                    <span className="text-sm">{s.version ?? 'version: -'}</span>
                    <span className="text-sm">{s.createdAt ? new Date(s.createdAt).toLocaleString() : 'unknown'}</span>
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  <p className="text-sm text-muted-foreground">{s.sourceUrl ?? 'No source URL'}</p>
                  <div className="pt-2">
                    <Button variant="outline" onClick={() => window.open(s.sourceUrl ?? '#', '_blank')}>Open Source URL</Button>
                    <Button className="ml-2" onClick={() => navigate(`/app/projects/${id}/endpoints`)}>View Endpoints</Button>
                  </div>
                </CardContent>
              </Card>
            ))
          )}
        </div>
      </div>
    </Layout>
  );
}
