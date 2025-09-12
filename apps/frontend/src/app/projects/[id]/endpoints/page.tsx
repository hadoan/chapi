'use client';

import EndpointDetail from '@/components/EndpointDetail';
import { Layout } from '@/components/Layout';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
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
  }, [id, selectedAuthProfile]);

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
  }, [id, selectedAuthProfile]);

  // Load auth profiles
  useEffect(() => {
    if (!id) return;
    authProfilesApi
      .getAll({ projectId: id, enabled: true })
      .then(profiles => {
        const list = profiles || [];
        setAuthProfiles(list);
        // default to first profile if available
        if (list.length > 0 && !selectedAuthProfile) {
          setSelectedAuthProfile(list[0].id);
        }
      })
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

        <EndpointDetail
          open={!!selectedEndpoint}
          onClose={() => {
            setSelectedEndpoint(null);
            setSelectedEndpointId(null);
            setSelectedAuthProfile('');
          }}
          selectedEndpoint={selectedEndpoint}
          detailLoading={detailLoading}
          authProfiles={authProfiles}
          selectedAuthProfile={selectedAuthProfile}
          setSelectedAuthProfile={setSelectedAuthProfile}
          testGenerating={testGenerating}
          onGenerateTests={handleGenerateTests}
        />
      </div>
    </Layout>
  );
}
