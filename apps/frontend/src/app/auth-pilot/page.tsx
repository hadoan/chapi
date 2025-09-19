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
import type { components } from '@/lib/api/schema';
import { useProject } from '@/lib/state/projectStore';
import { Brain, HelpCircle, Save, Sparkles, TestTube } from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';

// Components
import {
  AnimatedContainer,
  FloatingElements,
  GradientBorder,
} from '@/components/auth-pilot/AnimationEffects';
import { AuthProfileList } from '@/components/auth-pilot/AuthProfileList';
import EnhancedAiDetection from '@/components/auth-pilot/EnhancedAiDetection';
import {
  EnhancedCandidateList,
  EnhancedDetectionBanner,
} from '@/components/auth-pilot/EnhancedDetectionVisuals';
import { InjectionPreview } from '@/components/auth-pilot/InjectionPreview';
import { ProfileForm } from '@/components/auth-pilot/ProfileForm';
import { TokenCachePreview } from '@/components/auth-pilot/TokenCachePreview';

// Types and utilities
import {
  createInitialProfile,
  formatTimestamp,
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

function AuthPilotContent() {
  const [environment, setEnvironment] = useState<Environment>('Dev');
  const [profile, setProfile] = useState<AuthProfile>(createInitialProfile());
  const { selectedProject } = useProject(); // Remove selectedEnv dependency
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

  // New state for selected profile from list
  const [selectedProfile, setSelectedProfile] = useState<AuthProfile | null>(
    null
  );

  // Use the auth profiles hook
  const {
    profiles,
    loading: profilesLoading,
    createProfile,
    detectCandidates,
    loadProfiles,
  } = useAuthProfiles({
    // Remove environment dependency for auth pilot
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

  // Handle profile selection from the list
  const handleProfileSelection = (selectedAuthProfile: AuthProfile | null) => {
    setSelectedProfile(selectedAuthProfile);
    if (selectedAuthProfile) {
      // Update the current profile with selected profile data
      setProfile(selectedAuthProfile);
      setTokenResult(undefined); // Clear previous test results
      addLog({
        type: 'info',
        status: 'success',
        message: `Profile "${
          selectedAuthProfile.notes || 'Untitled'
        }" selected`,
      });
    } else {
      // If null selected, reset to initial profile
      setProfile(createInitialProfile());
      setTokenResult(undefined);
    }
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

  // Project context
  useEffect(() => {
    setProjectId(selectedProject?.id ?? undefined);
  }, [selectedProject]);

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
      <div className="bg-card border-b border-border sticky top-0 z-10 overflow-hidden">
        <FloatingElements />
        <div className="max-w-7xl mx-auto px-6 py-4 relative z-10">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-4">
              <div className="flex items-center gap-3">
                <div className="p-2 bg-gradient-to-br from-purple-100 to-blue-100 rounded-lg">
                  <Brain className="w-6 h-6 text-purple-600" />
                </div>
                <div>
                  <h1 className="text-2xl font-bold text-foreground flex items-center gap-2">
                    AI-Powered Auth Discovery
                    <Sparkles className="w-5 h-5 text-purple-500" />
                  </h1>
                  <p className="text-sm text-muted-foreground">
                    Intelligently detect and configure authentication methods
                  </p>
                </div>
              </div>
            </div>

            <div className="flex items-center gap-3">
              <Tooltip>
                <TooltipTrigger asChild>
                  <Button variant="ghost" size="sm" className="px-2">
                    <HelpCircle className="h-4 w-4" />
                  </Button>
                </TooltipTrigger>
                <TooltipContent>
                  <div className="max-w-sm space-y-2">
                    <p className="font-medium">AI Auth Detection:</p>
                    <p className="text-sm">
                      Use AI to analyze code samples or describe authentication
                      requirements
                    </p>
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
        <div className="grid grid-cols-1 xl:grid-cols-3 lg:grid-cols-2 gap-6">
          {/* Left Column - Detection & Candidates */}
          <div className="space-y-6">
            <AnimatedContainer delay={200}>
              {bestDetection && (
                <GradientBorder hover>
                  <EnhancedDetectionBanner
                    detection={bestDetection}
                    onUseEndpoint={() =>
                      handleUseDetectedEndpoint(bestDetection.endpoint)
                    }
                  />
                </GradientBorder>
              )}
            </AnimatedContainer>

            {/* Enhanced AI Detection Button */}
            <AnimatedContainer delay={400}>
              <div className="flex gap-2">
                <EnhancedAiDetection
                  projectId={selectedProject?.id}
                  serviceId={undefined}
                  variant="prominent"
                  className="flex-1"
                  onDetected={resp => {
                    const mapped = (resp.candidates || []).map(c => {
                      const t =
                        typeof c.type === 'number'
                          ? Number(c.type)
                          : String(c.type || '');
                      return {
                        type: t as unknown as AuthType,
                        confidence: c.confidence || 0,
                        token_url: c.tokenUrl ?? undefined,
                        header_name: c.injection?.name ?? undefined,
                        rawType: String(c.type ?? ''),
                        form: c.form ?? null,
                      } as AuthCandidate;
                    });
                    setCandidates(mapped);
                    setBestDetection(
                      resp.best
                        ? {
                            endpoint: resp.best.endpoint!,
                            source: resp.best.source!,
                            confidence: resp.best.confidence!,
                          }
                        : null
                    );
                  }}
                />
              </div>
            </AnimatedContainer>

            {/* Traditional Auth Detection Card */}
            <AnimatedContainer delay={600}>
              <Card>
                <CardContent className="pt-6">
                  <div className="flex items-center justify-between">
                    <div>
                      <h3 className="text-sm font-medium">Manual Detection</h3>
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
            </AnimatedContainer>

            <AnimatedContainer delay={800}>
              <EnhancedCandidateList
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
            </AnimatedContainer>
          </div>

          {/* Middle Column - Auth Profile List */}
          <div className="space-y-6">
            <AnimatedContainer delay={100}>
              <AuthProfileList
                profiles={profiles}
                selectedProfile={selectedProfile}
                onSelectProfile={handleProfileSelection}
                loading={profilesLoading}
                onCreateNew={() => {
                  // Reset to create new profile
                  setProfile(createInitialProfile());
                  setSelectedProfile(null);
                  setTokenResult(undefined);
                }}
              />
            </AnimatedContainer>
          </div>

          {/* Right Column - Profile Details & Test */}
          <div className="space-y-6">
            <AnimatedContainer delay={300}>
              <ProfileForm
                profile={profile}
                onChange={setProfile}
                errors={validation.errors}
              />
            </AnimatedContainer>

            <AnimatedContainer delay={500}>
              <InjectionPreview profile={profile} tokenResult={tokenResult} />
            </AnimatedContainer>

            <AnimatedContainer delay={700}>
              <TokenCachePreview tokenResult={tokenResult} />
            </AnimatedContainer>

            {/* Actions */}
            <AnimatedContainer delay={900}>
              <Card>
                <CardContent className="pt-6">
                  <div className="flex gap-3 flex-wrap">
                    <GradientBorder
                      gradient="from-indigo-500 via-purple-500 to-pink-500"
                      hover
                      className="flex-1"
                    >
                      <Button
                        onClick={handleTestConnection}
                        disabled={!canTest}
                        className="w-full bg-gradient-to-r from-indigo-600 to-purple-600 hover:from-indigo-700 hover:to-purple-700 text-white border-0"
                        size="lg"
                      >
                        <TestTube className="h-4 w-4 mr-2" />
                        {isTestingConnection ? 'Testing...' : 'Test Connection'}
                      </Button>
                    </GradientBorder>

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
            </AnimatedContainer>

            {/* Status/Log Panel */}
            {logs.length > 0 && (
              <AnimatedContainer delay={1100}>
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
                              log.status === 'success'
                                ? 'default'
                                : 'destructive'
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
              </AnimatedContainer>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

export default function AuthPilotPage() {
  return (
    <Layout
      showEnvironment={false}
      showUserMenu={false}
      showSidebarTrigger={false}
    >
      <AuthPilotContent />
    </Layout>
  );
}
