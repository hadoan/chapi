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
import { apiSpecsApi, type ApiSpecDto } from '@/lib/api/apispecs';
import { authProfilesApi, type AuthProfileDto } from '@/lib/api/auth-profiles';
import { endpointsApi, type EndpointDto } from '@/lib/api/endpoints';
import { testGenApi, type GenerateRequest } from '@/lib/api/llms';
import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';

interface EndpointBrief {
  id: string;
  method: string;
  path: string;
  summary?: string | null;
  tags?: string[] | null;
  specId: string;
}

export default function ProjectEndpointsPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [endpoints, setEndpoints] = useState<EndpointBrief[]>([]);
  const [loading, setLoading] = useState(false);
  const [methodFilter, setMethodFilter] = useState<string>('ALL');
  const [specFilter, setSpecFilter] = useState<string>('ALL');
  const [textFilter, setTextFilter] = useState<string>('');
  const [specs, setSpecs] = useState<ApiSpecDto[]>([]);
  const [selectedEndpoint, setSelectedEndpoint] = useState<EndpointDto | null>(
    null
  );
  const [selectedEndpointId, setSelectedEndpointId] = useState<string | null>(
    null
  );
  const [detailLoading, setDetailLoading] = useState(false);
  const [authProfiles, setAuthProfiles] = useState<AuthProfileDto[]>([]);
  const [selectedAuthProfile, setSelectedAuthProfile] = useState<string>('');
  const [testGenerating, setTestGenerating] = useState(false);

  const handleEndpointClick = async (endpointId: string) => {
    if (!id) return;

    setDetailLoading(true);
    try {
      const detail = await endpointsApi.get(id, endpointId);
      console.log(detail);
      setSelectedEndpoint(detail);
      setSelectedEndpointId(endpointId);
    } catch (err) {
      toast({
        title: 'Failed to load endpoint details',
        description: err?.message ?? String(err),
      });
    } finally {
      setDetailLoading(false);
    }
  };

  const handleGenerateTests = async (mode: 'CARD' | 'FILES' = 'FILES') => {
    if (!selectedEndpoint || !id) return;

    const selectedProfile = authProfiles.find(
      p => p.id === selectedAuthProfile
    );
    if (!selectedProfile) {
      toast({
        title: 'Please select an auth profile',
        description: 'An auth profile is required to generate tests',
      });
      return;
    }

    setTestGenerating(true);
    try {
      // Format auth profile config based on type
      // AuthType enum: 0=NONE, 1=API_KEY, 2=BEARER, 3=OIDC_CLIENT_CREDENTIALS
      // InjectionMode enum: 0=Header, 1=Query, 2=None
      const authConfig: Record<string, string | undefined> = {};
      const authTypeString =
        ['NONE', 'API_KEY', 'BEARER', 'OIDC_CLIENT_CREDENTIALS'][
          selectedProfile.type
        ] || 'NONE';

      if (selectedProfile.type === 1) {
        // API_KEY
        authConfig.headerName =
          selectedProfile.injectionName || 'Authorization';
        authConfig.injectAt =
          selectedProfile.injectionMode === 0 ? 'header' : 'query';
        if (selectedProfile.injectionMode === 1) {
          authConfig.queryName = selectedProfile.injectionName || 'api_key';
        }
      } else if (selectedProfile.type === 2) {
        // BEARER
        authConfig.tokenEnv = 'API_TOKEN';
      } else if (selectedProfile.type === 3) {
        // OIDC_CLIENT_CREDENTIALS
        authConfig.tokenUrl = selectedProfile.tokenUrl;
        authConfig.clientIdEnv = 'CLIENT_ID';
        authConfig.clientSecretEnv = 'CLIENT_SECRET';
        if (selectedProfile.scopesCsv) {
          authConfig.scope = selectedProfile.scopesCsv;
        }
        if (selectedProfile.audience) {
          authConfig.audience = selectedProfile.audience;
        }
      }

      // Create the Chapi-TestGen input format
      const testGenInput = {
        mode,
        project: { id },
        chat: {
          conversation_id: null, // Will create new conversation
          conversation_title: `${selectedEndpoint.method} ${selectedEndpoint.path} — tests`,
        },
        selectedEndpoint: {
          id: selectedEndpointId,
          method: selectedEndpoint.method || 'GET',
          path: selectedEndpoint.path || '',
          summary: selectedEndpoint.summary,
          requiresAuth: !!(
            selectedEndpoint.security && selectedEndpoint.security.length > 0
          ),
          successCode: 200,
          requestSchemaHint: selectedEndpoint.request ? 'json' : 'none',
        },
        authProfile: {
          id: selectedProfile.id,
          name: `${authTypeString} Profile`,
          type: authTypeString,
          config: authConfig,
        },
        options: {
          includeForbidden: true,
          envPlaceholders: ['BASE_URL'],
          fileBaseDir: 'tests/endpoint',
          useJq: true,
          generator_version: 'testgen@2025-09-10',
        },
        user_query: `Generate API tests for ${selectedEndpoint.method} ${selectedEndpoint.path}`,
      };

      const request: GenerateRequest = {
        user_query: `Generate API tests for ${selectedEndpoint.method} ${selectedEndpoint.path}`,
        projectId: id,
        max_files: 10,
        openApiJson: JSON.stringify(testGenInput),
      };

      const response = await testGenApi.generate(request);
      console.log('Generated test response:', response);

      toast({
        title: 'Tests Generated Successfully',
        description: `Generated ${response.card.files?.length || 0} test files`,
      });

      // TODO: Handle the generated card (e.g., navigate to test results page)
    } catch (err) {
      toast({
        title: 'Failed to generate tests',
        description: err?.message ?? String(err),
      });
    } finally {
      setTestGenerating(false);
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
            specId: x.specId ?? '',
          }))
        )
      )
      .catch(err =>
        toast({ title: 'Failed', description: err?.message ?? String(err) })
      )
      .finally(() => setLoading(false));
  }, [id]);

  // Load specs for filtering
  useEffect(() => {
    if (!id) return;
    apiSpecsApi
      .listByProject(id)
      .then(setSpecs)
      .catch(err =>
        toast({
          title: 'Failed to load specs',
          description: err?.message ?? String(err),
        })
      );
  }, [id]);

  // Load auth profiles
  useEffect(() => {
    if (!id) return;
    authProfilesApi
      .getAll({ projectId: id, enabled: true })
      .then(profiles => setAuthProfiles(profiles || []))
      .catch(err =>
        toast({
          title: 'Failed to load auth profiles',
          description: err?.message ?? String(err),
        })
      );
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
            <div style={{ width: 200 }}>
              <Select onValueChange={v => setSpecFilter(v)} defaultValue="ALL">
                <SelectTrigger>
                  <SelectValue placeholder="Filter by spec">
                    {specFilter === 'ALL'
                      ? 'All Specs'
                      : specs.find(s => s.id === specFilter)?.sourceUrl ||
                        'Select Spec'}
                  </SelectValue>
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="ALL">All Specs</SelectItem>
                  {specs.map(spec => (
                    <SelectItem key={spec.id} value={spec.id || ''}>
                      {spec.sourceUrl || `Spec ${spec.id?.slice(0, 8)}`}
                    </SelectItem>
                  ))}
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
                if (
                  specFilter &&
                  specFilter !== 'ALL' &&
                  ep.specId !== specFilter
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
          onOpenChange={() => {
            setSelectedEndpoint(null);
            setSelectedEndpointId(null);
            setSelectedAuthProfile('');
          }}
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

                {/* Test Generation Section */}
                <div className="border-t pt-6">
                  <h3 className="font-semibold mb-4">Generate API Tests</h3>
                  <div className="space-y-4">
                    <div>
                      <label className="text-sm font-medium mb-2 block">
                        Auth Profile
                      </label>
                      <Select
                        value={selectedAuthProfile}
                        onValueChange={setSelectedAuthProfile}
                      >
                        <SelectTrigger>
                          <SelectValue placeholder="Select an auth profile" />
                        </SelectTrigger>
                        <SelectContent>
                          {authProfiles.map(profile => (
                            <SelectItem key={profile.id} value={profile.id}>
                              {profile.type} -{' '}
                              {profile.injectionName || 'Default'}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    </div>

                    <div className="flex gap-2">
                      <Button
                        onClick={() => handleGenerateTests('FILES')}
                        disabled={testGenerating || !selectedAuthProfile}
                        className="flex-1"
                      >
                        {testGenerating ? 'Generating...' : 'Generate Tests'}
                      </Button>
                    </div>

                    {authProfiles.length === 0 && (
                      <p className="text-sm text-muted-foreground">
                        No auth profiles found. Create an auth profile in the
                        project settings to generate tests.
                      </p>
                    )}
                  </div>
                </div>
              </div>
            ) : null}
          </DialogContent>
        </Dialog>
      </div>
    </Layout>
  );
}
