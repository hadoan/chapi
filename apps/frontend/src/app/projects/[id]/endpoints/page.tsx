'use client';

import { Layout } from '@/components/Layout';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { toast } from '@/hooks/use-toast';
import { endpointsApi, type EndpointDto } from '@/lib/api/endpoints';
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
  const [selectedEndpoint, setSelectedEndpoint] = useState<EndpointDto | null>(
    null
  );
  const [detailLoading, setDetailLoading] = useState(false);

  const handleEndpointClick = async (endpointId: string) => {
    if (!id) return;

    setDetailLoading(true);
    try {
      const detail = await endpointsApi.get(id, endpointId);
      console.log(detail);
      setSelectedEndpoint(detail);
    } catch (err) {
      toast({
        title: 'Failed to load endpoint details',
        description: err?.message ?? String(err),
      });
    } finally {
      setDetailLoading(false);
    }
  };

  useEffect(() => {
    if (!id) return;
    setLoading(true);
    endpointsApi
      .listByProject(id)
      .then(list =>
        setEndpoints(
          list.map(x => ({
            id: x.id ?? '',
            method: (x.method ?? 'GET').toString(),
            path: x.path ?? '',
            summary: x.summary ?? null,
            tags: x.tags ?? [],
          }))
        )
      )
      .catch(err =>
        toast({ title: 'Failed', description: err?.message ?? String(err) })
      )
      .finally(() => setLoading(false));
  }, [id]);

  // Helper type guards
  const isRequestBody = (
    request: unknown
  ): request is {
    contentType?: string;
    description?: string;
    required?: boolean;
  } => {
    return typeof request === 'object' && request !== null;
  };

  const isResponseMap = (
    responses: unknown
  ): responses is Record<
    string,
    {
      description?: string;
      contentType?: string;
    }
  > => {
    return typeof responses === 'object' && responses !== null;
  };

  const isSecurityArray = (
    security: unknown
  ): security is Array<Record<string, string[]>> => {
    return Array.isArray(security);
  };

  const isParameterArray = (
    parameters: unknown
  ): parameters is Array<{
    name?: string;
    Name?: string; // Handle PascalCase from DB
    in?: string;
    In?: string; // Handle PascalCase from DB
    description?: string;
    Description?: string; // Handle PascalCase from DB
    required?: boolean;
    Required?: boolean; // Handle PascalCase from DB
    type?: string;
    schema?: {
      type?: string;
      format?: string;
    };
    Schema?: {
      // Handle PascalCase from DB
      Type?: string;
      Format?: string;
    };
  }> => {
    return Array.isArray(parameters);
  };

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
              <Select
                onValueChange={v => setMethodFilter(v)}
                defaultValue="ALL"
              >
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
            <Input
              placeholder="Search path, summary or tags"
              value={textFilter}
              onChange={e => setTextFilter(e.target.value)}
            />
          </div>
          {loading ? (
            <div>Loading...</div>
          ) : endpoints.length === 0 ? (
            <div>No endpoints found</div>
          ) : (
            endpoints
              .filter(ep => {
                if (
                  methodFilter &&
                  methodFilter !== 'ALL' &&
                  ep.method.toUpperCase() !== methodFilter
                )
                  return false;
                if (!textFilter) return true;
                const q = textFilter.toLowerCase();
                if (ep.path?.toLowerCase().includes(q)) return true;
                if (ep.summary?.toLowerCase().includes(q)) return true;
                if (ep.tags && ep.tags.join(',').toLowerCase().includes(q))
                  return true;
                return false;
              })
              .map(ep => (
                <Card
                  key={ep.id}
                  className="cursor-pointer hover:shadow-md transition-shadow"
                  onClick={() => handleEndpointClick(ep.id)}
                >
                  <CardHeader>
                    <CardTitle className="flex items-center justify-between">
                      <span className="font-mono text-sm">
                        {ep.method.toUpperCase()}
                      </span>
                      <span className="text-sm">{ep.path}</span>
                    </CardTitle>
                  </CardHeader>
                  <CardContent>
                    <p className="text-sm text-muted-foreground">
                      {ep.summary}
                    </p>
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

        {/* Endpoint Detail Dialog */}
        <Dialog
          open={!!selectedEndpoint}
          onOpenChange={() => setSelectedEndpoint(null)}
        >
          <DialogContent className="max-w-4xl max-h-[80vh] overflow-auto">
            <DialogHeader>
              <DialogTitle className="flex items-center gap-2">
                <span className="font-mono text-sm bg-blue-100 dark:bg-blue-900/50 px-2 py-1 rounded text-blue-800 dark:text-blue-200">
                  {selectedEndpoint?.method?.toUpperCase()}
                </span>
                <span className="font-mono">{selectedEndpoint?.path}</span>
              </DialogTitle>
            </DialogHeader>

            {detailLoading ? (
              <div className="flex items-center justify-center py-8">
                <div>Loading endpoint details...</div>
              </div>
            ) : selectedEndpoint ? (
              <div className="space-y-6">
                {/* Basic Info */}
                <div>
                  <h3 className="font-semibold mb-2">Summary</h3>
                  <p className="text-sm text-muted-foreground">
                    {selectedEndpoint.summary || 'No summary available'}
                  </p>
                </div>

                {selectedEndpoint.description && (
                  <div>
                    <h3 className="font-semibold mb-2">Description</h3>
                    <p className="text-sm text-muted-foreground whitespace-pre-wrap">
                      {selectedEndpoint.description}
                    </p>
                  </div>
                )}

                {selectedEndpoint.operationId && (
                  <div>
                    <h3 className="font-semibold mb-2">Operation ID</h3>
                    <code className="text-sm bg-muted px-2 py-1 rounded border">
                      {selectedEndpoint.operationId}
                    </code>
                  </div>
                )}

                {selectedEndpoint.tags && selectedEndpoint.tags.length > 0 && (
                  <div>
                    <h3 className="font-semibold mb-2">Tags</h3>
                    <div className="flex flex-wrap gap-2">
                      {selectedEndpoint.tags.map((tag, index) => (
                        <span
                          key={index}
                          className="text-xs bg-blue-100 text-blue-800 px-2 py-1 rounded"
                        >
                          {tag}
                        </span>
                      ))}
                    </div>
                  </div>
                )}

                {Array.isArray(selectedEndpoint.servers) &&
                  selectedEndpoint.servers.length > 0 && (
                    <div>
                      <h3 className="font-semibold mb-2">Servers</h3>
                      <div className="space-y-1">
                        {selectedEndpoint.servers.map((server, index) => (
                          <div
                            key={index}
                            className="text-sm font-mono bg-gray-100 px-2 py-1 rounded"
                          >
                            {server}
                          </div>
                        ))}
                      </div>
                    </div>
                  )}

                {isParameterArray(selectedEndpoint.parameters) &&
                  selectedEndpoint.parameters.length > 0 && (
                    <div>
                      <h3 className="font-semibold mb-2">Parameters</h3>
                      <div className="overflow-x-auto">
                        <table className="w-full text-sm border-collapse border border-border">
                          <thead>
                            <tr className="bg-muted/50">
                              <th className="border border-border px-3 py-2 text-left">
                                Name
                              </th>
                              <th className="border border-border px-3 py-2 text-left">
                                In
                              </th>
                              <th className="border border-border px-3 py-2 text-left">
                                Type
                              </th>
                              <th className="border border-border px-3 py-2 text-left">
                                Required
                              </th>
                              <th className="border border-border px-3 py-2 text-left">
                                Description
                              </th>
                            </tr>
                          </thead>
                          <tbody>
                            {isParameterArray(selectedEndpoint.parameters) &&
                              selectedEndpoint.parameters.map(
                                (param, index) => {
                                  // Helper to safely access properties (handling both camelCase and PascalCase)
                                  const p = param as Record<string, unknown>;
                                  return (
                                    <tr
                                      key={index}
                                      className="hover:bg-muted/30"
                                    >
                                      <td className="border border-border px-3 py-2 font-mono">
                                        {(p.name as string) ||
                                          (p.Name as string) ||
                                          'N/A'}
                                      </td>
                                      <td className="border border-border px-3 py-2">
                                        {(p.in as string) ||
                                          (p.In as string) ||
                                          'N/A'}
                                      </td>
                                      <td className="border border-border px-3 py-2">
                                        {((p.schema as Record<string, unknown>)
                                          ?.type as string) ||
                                          ((p.Schema as Record<string, unknown>)
                                            ?.Type as string) ||
                                          (p.type as string) ||
                                          'string'}
                                      </td>
                                      <td className="border border-border px-3 py-2">
                                        {((p.required ?? p.Required) as boolean)
                                          ? 'Yes'
                                          : 'No'}
                                      </td>
                                      <td className="border border-border px-3 py-2">
                                        {(p.description as string) ||
                                          (p.Description as string) ||
                                          '-'}
                                      </td>
                                    </tr>
                                  );
                                }
                              )}
                          </tbody>
                        </table>
                      </div>
                    </div>
                  )}

                {isRequestBody(selectedEndpoint.request) && (
                  <div>
                    <h3 className="font-semibold mb-2">Request Body</h3>
                    <div className="bg-muted/50 p-3 rounded text-sm border border-border">
                      {selectedEndpoint.request.required && (
                        <div className="text-destructive text-xs mb-2">
                          Required
                        </div>
                      )}
                      {selectedEndpoint.request.content &&
                        Object.keys(selectedEndpoint.request.content).length >
                          0 && (
                          <div>
                            <div className="font-medium mb-2">
                              Content Types:
                            </div>
                            <div className="text-muted-foreground">
                              {Object.keys(
                                selectedEndpoint.request.content
                              ).join(', ')}
                            </div>
                          </div>
                        )}
                    </div>
                  </div>
                )}

                {isResponseMap(selectedEndpoint.responses) &&
                  Object.keys(selectedEndpoint.responses).length > 0 && (
                    <div>
                      <h3 className="font-semibold mb-2">Responses</h3>
                      <div className="space-y-3">
                        {Object.entries(selectedEndpoint.responses).map(
                          ([status, response]) => (
                            <div
                              key={status}
                              className="border border-border rounded p-3 bg-card"
                            >
                              <div className="flex items-center gap-2 mb-2">
                                <span
                                  className={`font-mono text-sm px-2 py-1 rounded ${
                                    status.startsWith('2')
                                      ? 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400'
                                      : status.startsWith('4')
                                      ? 'bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-400'
                                      : status.startsWith('5')
                                      ? 'bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-400'
                                      : 'bg-muted text-muted-foreground'
                                  }`}
                                >
                                  {status}
                                </span>
                                <span className="text-sm text-muted-foreground">
                                  {response.description || 'No description'}
                                </span>
                              </div>
                              {response.content &&
                                Object.keys(response.content).length > 0 && (
                                  <div className="text-xs text-muted-foreground">
                                    Content-Types:{' '}
                                    {Object.keys(response.content).join(', ')}
                                  </div>
                                )}
                            </div>
                          )
                        )}
                      </div>
                    </div>
                  )}

                {isSecurityArray(selectedEndpoint.security) &&
                  selectedEndpoint.security.length > 0 && (
                    <div>
                      <h3 className="font-semibold mb-2">Security</h3>
                      <div className="space-y-2">
                        {selectedEndpoint.security.map(
                          (securityRequirement, index) => (
                            <div
                              key={index}
                              className="bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded p-2"
                            >
                              <div className="text-sm">
                                {Object.entries(securityRequirement).map(
                                  ([name, scopes]) => (
                                    <div key={name}>
                                      <span className="font-medium">
                                        {name}
                                      </span>
                                      {Array.isArray(scopes) &&
                                        scopes.length > 0 && (
                                          <span className="text-muted-foreground">
                                            : {scopes.join(', ')}
                                          </span>
                                        )}
                                    </div>
                                  )
                                )}
                              </div>
                            </div>
                          )
                        )}
                      </div>
                    </div>
                  )}
              </div>
            ) : null}
          </DialogContent>
        </Dialog>
      </div>
    </Layout>
  );
}
