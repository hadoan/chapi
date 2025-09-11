import { Layout } from '@/components/Layout';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from '@/components/ui/tooltip';
import { useAuthProfiles } from '@/hooks/use-auth-profiles';
import { toast } from '@/hooks/use-toast';
import { authProfilesApi } from '@/lib/api/auth-profiles';
import { environmentsApi } from '@/lib/api/environments';
import type { components } from '@/lib/api/schema';
import { useProject } from '@/lib/state/projectStore';
import { HelpCircle, RotateCcw, Save, TestTube } from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';

// Components
import { CandidateList } from '@/components/auth-pilot/CandidateList';
import { DetectionBanner } from '@/components/auth-pilot/DetectionBanner';
import { InjectionPreview } from '@/components/auth-pilot/InjectionPreview';
import { ProfileForm } from '@/components/auth-pilot/ProfileForm';
import { TokenCachePreview } from '@/components/auth-pilot/TokenCachePreview';

// Types and utilities
import {
  createInitialProfile,
  formatTimestamp,
  getAuthTypeDisplayName,
  getErrorMessage,
  simulateTokenRequest,
  validateProfile,
} from '@/lib/auth-pilot';
import type {
  AuthCandidate,
  AuthProfile,
  AuthType,
  Environment,
  LogEntry,
  TokenResult,
} from '@/types/auth-pilot';

const STORAGE_KEY = 'chapi-auth-pilot-demo';

// Local mapping function for backend auth types
const mapBackendAuthType = (backendType: unknown): AuthType => {
  // Backend uses numeric enum: 0 | 1 | 2 | 3 | 4 | 5 | 6
  const typeMap: Record<number, AuthType> = {
    0: 'oauth2_client_credentials',
    1: 'api_key_header',
    2: 'bearer_static',
    3: 'session_cookie',
    4: 'password',
    5: 'basic',
    6: 'custom_login',
  };
  const n =
    typeof backendType === 'number'
      ? backendType
      : parseInt(String(backendType || ''), 10);
  return typeMap[n] || 'oauth2_client_credentials';
};

function AuthPilotContent() {
  const [environment, setEnvironment] = useState<Environment>('Dev');
  const [profile, setProfile] = useState<AuthProfile>(createInitialProfile());
  const { selectedProject, selectedEnv, setSelectedEnv, setSelectedProject } =
    useProject();
  const [projectId, setProjectId] = useState<string | undefined>(
    selectedProject?.id ?? undefined
  );
  const [tokenResult, setTokenResult] = useState<TokenResult>();
  const [logs, setLogs] = useState<LogEntry[]>([]);
  const [isTestingConnection, setIsTestingConnection] = useState(false);
  const [candidates, setCandidates] = useState<AuthCandidate[]>([]);
  const [bestDetection, setBestDetection] = useState<{
    endpoint: string;
    source: string;
    confidence: number;
  } | null>(null);

  // Use the auth profiles hook
  const {
    profiles,
    backendProfiles,
    loading: profilesLoading,
    error: profilesError,
    createProfile,
    updateProfile,
    deleteProfile,
    detectCandidates,
  } = useAuthProfiles({
    environmentId: environment.toLowerCase(),
    projectId: selectedProject?.id,
    serviceId: undefined, // TODO: Add service selection
    autoLoad: true,
  });

  // Load demo state from localStorage
  useEffect(() => {
    const saved = localStorage.getItem(STORAGE_KEY);
    if (saved) {
      try {
        const data = JSON.parse(saved);
        if (data.profiles?.[environment]) {
          setProfile(data.profiles[environment]);
        }
        if (data.logs) {
          setLogs(data.logs);
        }
      } catch (error) {
        console.error('Failed to load demo state:', error);
      }
    }
  }, [environment]);

  // Save demo state to localStorage
  const saveDemoState = useCallback(
    (updatedProfile?: AuthProfile, updatedLogs?: LogEntry[]) => {
      try {
        const saved = localStorage.getItem(STORAGE_KEY);
        const data = saved ? JSON.parse(saved) : { profiles: {}, logs: [] };

        if (updatedProfile) {
          data.profiles[environment] = updatedProfile;
        }

        if (updatedLogs) {
          data.logs = updatedLogs;
        }

        localStorage.setItem(STORAGE_KEY, JSON.stringify(data));
      } catch (error) {
        console.error('Failed to save demo state:', error);
      }
    },
    [environment]
  );

  const addLog = useCallback(
    (entry: Omit<LogEntry, 'timestamp'>) => {
      const newLog: LogEntry = {
        ...entry,
        timestamp: formatTimestamp(),
      };
      const updatedLogs = [...logs, newLog];
      setLogs(updatedLogs);
      saveDemoState(undefined, updatedLogs);
    },
    [logs, saveDemoState]
  );

  const handleCandidateSelect = (candidate: AuthCandidate) => {
    if (candidate.disabled) return;

    // Start with defaults and then apply form/type hints
    const newProfile: AuthProfile = {
      type: candidate.type,
      token_url: candidate.token_url || profile.token_url,
      scopes: profile.scopes,
      audience: profile.audience,
      notes: profile.notes,
      // Reset type-specific fields
      client_id: '',
      client_secret: '',
      header_name: candidate.header_name || 'X-API-Key',
      api_key: '',
      bearer_token: '',
      cookie_value: '',
      // map optional prefill secret refs from detection
      username_ref: (candidate.username_ref ?? '') as string,
      password_ref: (candidate.password_ref ?? '') as string,
      login_body_type: 'form',
      login_user_key: 'username',
      login_pass_key: 'password',
      token_json_path: '$.access_token',
    };

    // If detection provided a form hint, adapt profile accordingly
    if (candidate.form?.grantType) {
      const grant = candidate.form.grantType.toLowerCase();
      if (grant === 'password') {
        newProfile.type = 'password';
        // keep username/password refs if provided
        if (candidate.form.fields?.username)
          newProfile.username_ref = candidate.form.fields.username;
        if (candidate.form.fields?.password)
          newProfile.password_ref = candidate.form.fields.password;
      } else if (
        grant === 'client_credentials' ||
        grant === 'client_credentials'
      ) {
        newProfile.type = 'oauth2_client_credentials';
      }
    }

    // If candidate indicates api key, ensure header shown
    if (candidate.type === 'api_key_header') {
      newProfile.type = 'api_key_header';
      newProfile.header_name = candidate.header_name || newProfile.header_name;
    }

    setProfile(newProfile);
    setTokenResult(undefined); // Clear previous test results
  };

  const handleUseDetectedEndpoint = (bestEndpoint?: string) => {
    if (!bestEndpoint) return;

    setProfile(prev => ({
      ...prev,
      token_url: bestEndpoint,
    }));

    toast({
      title: 'Endpoint updated',
      description: `Token URL set to detected endpoint: ${bestEndpoint}`,
    });
  };

  // Load candidates from backend
  const handleDetectCandidates = useCallback(async () => {
    if (profile.token_url) {
      try {
        const detected = await detectCandidates(
          profile.token_url,
          selectedProject?.id
        );
        setCandidates(detected.candidates);
        setBestDetection(detected.best ?? null);

        addLog({
          type: 'detect',
          status: 'success',
          message: `Found ${detected.candidates.length} authentication candidates`,
        });
      } catch (error) {
        addLog({
          type: 'detect',
          status: 'error',
          message: 'Failed to detect authentication methods',
        });
      }
    }
  }, [profile.token_url, detectCandidates, addLog, selectedProject]);

  // Project and environment are provided by ProjectContext (top bar)
  useEffect(() => {
    setProjectId(selectedProject?.id ?? undefined);
    if (selectedEnv) setEnvironment(selectedEnv as Environment);
  }, [selectedProject, selectedEnv]);

  // Fetch environment data and update profile when environment changes
  useEffect(() => {
    const fetchEnvironmentData = async () => {
      if (!selectedProject?.id || !selectedEnv) {
        console.log(
          'Skipping environment fetch: missing projectId or selectedEnv'
        );
        return;
      }

      console.log(
        `Fetching environment data for project ${selectedProject.id}, environment ${selectedEnv}`
      );

      try {
        // Use the API endpoint: /api/projects/{projectId}/environments
        console.log(
          `Making API call to: /api/projects/${selectedProject.id}/environments`
        );
        const environments = await environmentsApi.getByProject(
          selectedProject.id
        );
        console.log(
          'API call successful, received environments:',
          environments
        );

        if (!environments || environments.length === 0) {
          console.warn('No environments found for project');
          // Removed addLog to prevent repeated API calls

          // Fallback to default
          setProfile(prev => ({
            ...prev,
            token_url: 'https://api.demo.local/connect/token',
          }));
          return;
        }

        const currentEnv = environments.find(
          env => env.name?.toLowerCase() === selectedEnv.toLowerCase()
        );

        if (currentEnv) {
          console.log('Found matching environment:', currentEnv);

          // Update profile with environment data
          setProfile(prev => {
            const updatedProfile = {
              ...prev,
              token_url: currentEnv.baseUrl || prev.token_url,
            };

            // If environment has headers, we could potentially use them
            // For now, we'll just log them for debugging
            if (currentEnv.headers && currentEnv.headers.length > 0) {
              console.log('Environment headers available:', currentEnv.headers);
              // Removed addLog to prevent repeated API calls
            }

            // If environment has secrets, we could potentially suggest them
            if (currentEnv.secrets && currentEnv.secrets.length > 0) {
              console.log(
                'Environment secrets available:',
                currentEnv.secrets.map(s => s.keyPath)
              );
              // Removed addLog to prevent repeated API calls
            }

            return updatedProfile;
          });

          // Removed addLog to prevent repeated API calls
          console.log(
            `Successfully loaded environment "${currentEnv.name}" with base URL: ${currentEnv.baseUrl}`
          );
        } else {
          console.warn(
            `Environment "${selectedEnv}" not found in project environments`
          );
          // Removed addLog to prevent repeated API calls

          // Fallback to default
          setProfile(prev => ({
            ...prev,
            token_url: 'https://api.demo.local/connect/token',
          }));
        }
      } catch (error) {
        console.error('Failed to fetch environment data:', error);
        // Removed addLog to prevent repeated API calls

        // Fallback to default if environment fetch fails
        setProfile(prev => ({
          ...prev,
          token_url: 'https://api.demo.local/connect/token',
        }));
      }
    };

    fetchEnvironmentData();
  }, [selectedProject?.id, selectedEnv]);

  // Save profile to backend
  const handleSaveProfile = useCallback(async () => {
    try {
      const savedProfile = await createProfile(profile);
      if (savedProfile) {
        addLog({
          type: 'save',
          status: 'success',
          message: 'Profile saved successfully',
        });
      }
    } catch (error) {
      addLog({
        type: 'save',
        status: 'error',
        message: 'Failed to save profile',
      });
    }
  }, [profile, createProfile, addLog]);

  const handleTestConnection = useCallback(async () => {
    const validation = validateProfile(profile);

    if (!validation.isValid) {
      toast({
        title: 'Validation failed',
        description: validation.errors[0],
        variant: 'destructive',
      });
      return;
    }

    setIsTestingConnection(true);

    try {
      // For flows that require server-side secret resolution (OAuth2, Basic, Custom login)
      // call the backend tester which can access secret stores and make outbound requests.
      let result;

      if (
        profile.type === 'oauth2_client_credentials' ||
        profile.type === 'password' ||
        profile.type === 'basic' ||
        profile.type === 'custom_login'
      ) {
        // Map frontend type to backend AuthType enum (using schema types)
        const mapType = (
          t: string
        ): components['schemas']['AuthProfiles.Domain.AuthType'] => {
          switch (t) {
            case 'oauth2_client_credentials':
              return 0; // OAuth2ClientCredentials
            case 'password':
              return 1; // OAuth2Password
            case 'basic':
              return 2; // Basic
            case 'custom_login':
              return 6; // CustomLogin
            default:
              return 0;
          }
        };

        // Helper: if a user-entered value looks like a secret ref (contains ':'), pass it through.
        // Otherwise, place the actual secret into OverrideSecretValues and reference it by an override key.
        const overrides: Record<string, string> = {};
        const makeRef = (val: string | undefined, keyPrefix: string) => {
          if (!val) return null;
          if (val.includes(':')) return val; // assume it's already a secret ref/key
          const overrideKey = `__inline_${keyPrefix}`;
          overrides[overrideKey] = val;
          return overrideKey;
        };

        // Build AuthProfileDto using schema type
        const profileInline: components['schemas']['AuthProfiles.Application.Dtos.AuthProfileDto'] =
          {
            id: null,
            projectId:
              selectedProject?.id || '00000000-0000-0000-0000-000000000000',
            serviceId: null,
            environmentKey: environment.toLowerCase(),
            type: mapType(profile.type),
            tokenUrl: profile.token_url,
            params: {
              ClientId: profile.client_id ?? null,
              // If user typed a client secret, send it via overrides so the server can resolve it
              ClientSecretRef: makeRef(
                profile.client_secret ?? undefined,
                'client_secret'
              ),
              // username_ref/password_ref may be actual values or secret refs in the demo UI
              UsernameRef: makeRef(
                profile.username_ref ?? undefined,
                'username'
              ),
              PasswordRef: makeRef(
                profile.password_ref ?? undefined,
                'password'
              ),
              CustomLoginUrl: profile.token_url ?? null,
              CustomUserKey: profile.login_user_key ?? null,
              CustomPassKey: profile.login_pass_key ?? null,
              CustomBodyType: profile.login_body_type ?? null,
            },
            audience: profile.audience,
            scopesCsv: profile.scopes,
            injectionMode: null,
            injectionName: null,
            injectionFormat: null,
            detectSource: null,
            detectConfidence: null,
            enabled: null,
            createdAt: null,
            updatedAt: null,
            secretRefs: null,
          };

        // Call server-side test using proper schema types
        const resp = await authProfilesApi.test({
          authProfileId: null,
          profileInline: profileInline,
          envId: null,
          overrideSecretValues: Object.keys(overrides).length
            ? overrides
            : null,
        });
        // Normalize to TokenResult-like shape for frontend views
        if (resp.ok) {
          result = {
            status: 'ok',
            access_token:
              resp.accessToken ?? resp.sampleTokenPrefix ?? undefined,
            token_type: resp.tokenType ?? undefined,
            expires_at: resp.expiresAt ?? undefined,
            message: resp.message ?? undefined,
          };
        } else {
          result = {
            status: resp.status || 'error',
            message: resp.message || 'Test failed',
          };
        }
      } else {
        // For simple client-only flows use the local simulator
        // keep simulated delay to show UX feedback
        await new Promise(resolve => setTimeout(resolve, 500));
        result = simulateTokenRequest(profile);
      }

      setTokenResult(result);

      if (result.status === 'ok') {
        const expiresInMinutes = Math.floor((result.expires_in || 0) / 60);
        toast({
          title: 'Token acquired',
          description: `Token expires in ${expiresInMinutes}m`,
        });

        addLog({
          type: 'test',
          message: `Test successful for ${profile.type}`,
          status: 'success',
        });
      } else {
        toast({
          title: 'Test failed',
          description: getErrorMessage(result.status),
          variant: 'destructive',
        });

        addLog({
          type: 'test',
          message: `Test failed: ${result.message}`,
          status: 'error',
        });
      }
    } catch (error) {
      console.error('Test connection error', error);
      toast({
        title: 'Test failed',
        description: 'Unexpected error occurred',
        variant: 'destructive',
      });
    } finally {
      setIsTestingConnection(false);
    }
  }, [profile, addLog, environment, selectedProject?.id]);

  const handleResetDemo = () => {
    localStorage.removeItem(STORAGE_KEY);
    setProfile(createInitialProfile());
    setTokenResult(undefined);
    setLogs([]);

    toast({
      title: 'Demo reset',
      description: 'All demo data has been cleared',
    });
  };

  const validation = validateProfile(profile);
  const canTest = validation.isValid && !isTestingConnection;

  // Keyboard shortcuts
  useEffect(() => {
    const handleKeydown = (e: KeyboardEvent) => {
      if ((e.ctrlKey || e.metaKey) && e.key === 'Enter') {
        e.preventDefault();
        if (canTest) handleTestConnection();
      } else if ((e.ctrlKey || e.metaKey) && e.key === 's') {
        e.preventDefault();
        handleSaveProfile();
      } else if (e.key === 'Escape') {
        // Could handle modal close if needed
      }
    };

    window.addEventListener('keydown', handleKeydown);
    return () => window.removeEventListener('keydown', handleKeydown);
  }, [canTest, handleTestConnection, handleSaveProfile]);

  return (
    <div className="min-h-screen bg-background">
      {/* Header */}
      <div className="bg-card border-b border-border sticky top-0 z-10">
        <div className="max-w-7xl mx-auto px-6 py-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-4">
              <h1 className="text-2xl font-bold text-foreground">
                Token & Auth Profile Wizard
              </h1>
              {/* <Badge
                variant="secondary"
                className="bg-primary/10 text-primary"
              >
                Demo Mode
              </Badge> */}
            </div>

            <div className="flex items-center gap-3">
              {/* Project selection is shown in the global top bar; hide duplicate here */}

              {/* Actions */}

              <Button
                variant="ghost"
                size="sm"
                onClick={handleResetDemo}
                className="text-muted-foreground hover:text-foreground"
              >
                <RotateCcw className="h-4 w-4 mr-2" />
                Detect Auth
              </Button>

              <Button
                variant="ghost"
                size="sm"
                onClick={handleResetDemo}
                className="text-muted-foreground hover:text-foreground"
              >
                <RotateCcw className="h-4 w-4 mr-2" />
                Detect Auth by Prompt
              </Button>

              <Tooltip>
                <TooltipTrigger asChild>
                  <Button variant="ghost" size="sm" className="px-2">
                    <HelpCircle className="h-4 w-4" />
                  </Button>
                </TooltipTrigger>
                <TooltipContent>
                  <div className="max-w-sm space-y-2">
                    <p className="font-medium">Keyboard Shortcuts:</p>
                    <p className="text-sm">Ctrl/Cmd+Enter: Test Connection</p>
                    <p className="text-sm">Ctrl/Cmd+S: Save Profile</p>
                  </div>
                </TooltipContent>
              </Tooltip>
            </div>
          </div>
        </div>
      </div>

      {/* Main Content */}
      <div className="max-w-7xl mx-auto px-6 py-6">
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {/* Left Column - Detection & Candidates */}
          <div className="space-y-6">
            {/* Profile Selector */}
            {backendProfiles.length > 0 && (
              <Card>
                <CardHeader>
                  <CardTitle className="text-lg">Saved Profiles</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="space-y-2">
                    {backendProfiles.map(backendProfile => {
                      const frontendProfile = {
                        type: mapBackendAuthType(backendProfile.type!),
                        token_url: backendProfile.tokenUrl || '',
                        scopes: backendProfile.scopesCsv || '',
                        audience: backendProfile.audience || '',
                        notes: '',
                        client_id:
                          backendProfile.secretRefs?.['client_id'] || '',
                        client_secret:
                          backendProfile.secretRefs?.['client_secret'] || '',
                        header_name:
                          backendProfile.injectionName || 'X-API-Key',
                        api_key: backendProfile.secretRefs?.['api_key'] || '',
                        bearer_token:
                          backendProfile.secretRefs?.['bearer_token'] || '',
                        cookie_value:
                          backendProfile.secretRefs?.['cookie_value'] || '',
                        username_ref:
                          backendProfile.secretRefs?.['username'] || '',
                        password_ref:
                          backendProfile.secretRefs?.['password'] || '',
                        login_body_type: (backendProfile.secretRefs?.[
                          'custom_body_type'
                        ] === 'form'
                          ? 'form'
                          : 'json') as 'json' | 'form',
                        login_user_key:
                          backendProfile.secretRefs?.['custom_user_key'] ||
                          'username',
                        login_pass_key:
                          backendProfile.secretRefs?.['custom_pass_key'] ||
                          'password',
                        token_json_path:
                          backendProfile.secretRefs?.['token_json_path'] ||
                          '$.access_token',
                      };
                      return (
                        <Button
                          key={backendProfile.id}
                          variant="outline"
                          size="sm"
                          className="w-full justify-start text-left"
                          onClick={() => {
                            // Load the selected profile into the form
                            setProfile(frontendProfile);
                            setTokenResult(undefined); // Clear previous test results
                            toast({
                              title: 'Profile loaded',
                              description: `Loaded profile for ${frontendProfile.token_url}`,
                            });
                          }}
                        >
                          <div className="flex flex-col items-start">
                            <span className="font-medium">
                              {getAuthTypeDisplayName(frontendProfile.type)}
                            </span>
                            <span className="text-xs text-muted-foreground truncate max-w-48">
                              {frontendProfile.token_url}
                            </span>
                          </div>
                        </Button>
                      );
                    })}
                  </div>
                </CardContent>
              </Card>
            )}{' '}
            {bestDetection && (
              <DetectionBanner
                detection={bestDetection}
                onUseEndpoint={() =>
                  handleUseDetectedEndpoint(bestDetection.endpoint)
                }
              />
            )}
            {/* Detection Button */}
            <Card>
              <CardContent className="pt-6">
                <div className="flex items-center justify-between">
                  <div>
                    <h3 className="text-sm font-medium">Auth Detection</h3>
                    <p className="text-xs text-muted-foreground">
                      Analyze endpoint for authentication methods
                    </p>
                  </div>
                  <Button
                    onClick={handleDetectCandidates}
                    disabled={!profile.token_url || profilesLoading}
                    variant="outline"
                    size="sm"
                  >
                    {profilesLoading ? 'Detecting...' : 'Detect Auth'}
                  </Button>
                </div>
              </CardContent>
            </Card>
            <CandidateList
              candidates={candidates}
              selectedType={profile.type}
              // Use header_name as the selected token identifier for API key header profiles
              selectedTokenUrl={
                profile.type === 'api_key_header'
                  ? profile.header_name
                  : profile.token_url
              }
              onSelectCandidate={handleCandidateSelect}
            />
          </div>

          {/* Right Column - Profile & Test */}
          <div className="space-y-6">
            <ProfileForm
              profile={profile}
              onChange={setProfile}
              errors={validation.errors}
            />

            <InjectionPreview profile={profile} tokenResult={tokenResult} />

            <TokenCachePreview tokenResult={tokenResult} />

            {/* Actions */}
            <Card>
              <CardContent className="pt-6">
                <div className="flex gap-3 flex-wrap">
                  <Button
                    onClick={handleTestConnection}
                    disabled={!canTest}
                    className="bg-indigo-600 hover:bg-indigo-700 text-white"
                  >
                    <TestTube className="h-4 w-4 mr-2" />
                    {isTestingConnection ? 'Testing...' : 'Test Connection'}
                  </Button>

                  <Button variant="outline" onClick={handleSaveProfile}>
                    <Save className="h-4 w-4 mr-2" />
                    Save Profile
                  </Button>
                </div>

                {!validation.isValid && (
                  <p className="text-sm text-slate-500 mt-3">
                    Complete required fields to enable testing
                  </p>
                )}
              </CardContent>
            </Card>

            {/* Status/Log Panel */}
            {logs.length > 0 && (
              <Card>
                <CardHeader>
                  <CardTitle className="text-base">Activity Log</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="space-y-2 max-h-32 overflow-y-auto">
                    {logs.slice(-5).map((log, index) => (
                      <div
                        key={index}
                        className="flex items-center gap-2 text-sm"
                      >
                        <span className="text-slate-500 font-mono text-xs w-16">
                          {log.timestamp}
                        </span>
                        <Badge
                          variant={
                            log.status === 'success' ? 'default' : 'destructive'
                          }
                          className="text-xs"
                        >
                          {log.type}
                        </Badge>
                        <span className="text-slate-700">{log.message}</span>
                      </div>
                    ))}
                  </div>
                </CardContent>
              </Card>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

export default function AuthPilotPage() {
  return (
    <Layout>
      <AuthPilotContent />
    </Layout>
  );
}
