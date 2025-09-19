import { Layout } from '@/components/Layout';
import { AnimatedContainer } from '@/components/auth-pilot/AnimationEffects';
import { AuthPilotDetection } from '@/components/auth-pilot/AuthPilotDetection';
import { AuthPilotHeader } from '@/components/auth-pilot/AuthPilotHeader';
import { AuthPilotLogs } from '@/components/auth-pilot/AuthPilotLogs';
import { AuthPilotProfiles } from '@/components/auth-pilot/AuthPilotProfiles';
import { AuthPilotTesting } from '@/components/auth-pilot/AuthPilotTesting';
import { useAuthPilot } from '@/hooks/use-auth-pilot';
import type { DetectionResponse } from '@/lib/api/auth-profiles';
import type { AuthCandidate } from '@/types/auth-pilot';
import { useCallback, useEffect } from 'react';

// Type for Chapi.AI Detection Response
interface ChapiAiDetectionResponse {
  detect_source?: string | null;
  detect_confidence?: number;
  profile?: {
    type?: string;
    environmentKey?: string;
    parameters?: {
      tokenUrl?: string;
      authorizationUrl?: string | null;
      audience?: string | null;
      scopes?: string;
      clientId?: string;
      clientSecretRef?: string | null;
      usernameRef?: string;
      passwordRef?: string;
      customLoginUrl?: string | null;
      customBodyType?: string;
      customUserKey?: string | null;
      customPassKey?: string | null;
      tokenJsonPath?: string;
    };
    injection?: {
      mode?: string;
      name?: string;
      format?: string;
    };
    secrets?: Array<{
      key: string;
      secretRef: string;
      notes?: string | null;
    }>;
    token_request?: {
      method?: string;
      url?: string;
      headers?: Record<string, string>;
      body?: {
        kind?: string;
        value?: Record<string, string>;
      };
      expect?: {
        status?: number;
        tokenJsonPath?: string;
      };
    };
  };
}

// Helper function to map Chapi.AI auth types to our AuthType
const mapChapiAiTypeToAuthType = (
  chapiType?: string,
  grantType?: string
): AuthCandidate['type'] => {
  if (!chapiType) return 'oauth2_client_credentials';

  // For OAuth2, check the grant type to determine the specific flow
  if (chapiType.toLowerCase() === 'oauth2') {
    if (grantType) {
      switch (grantType.toLowerCase()) {
        case 'password':
          return 'password';
        case 'client_credentials':
          return 'oauth2_client_credentials';
        case 'authorization_code':
          return 'auth_code';
        case 'device_code':
          return 'device_code';
        default:
          return 'oauth2_client_credentials';
      }
    }
    // Default to client_credentials if no grant type specified
    return 'oauth2_client_credentials';
  }

  switch (chapiType.toLowerCase()) {
    case 'oauth2_client_credentials':
      return 'oauth2_client_credentials';
    case 'api_key':
    case 'api_key_header':
      return 'api_key_header';
    case 'bearer':
    case 'bearer_static':
      return 'bearer_static';
    case 'basic':
      return 'basic';
    case 'password':
      return 'password';
    case 'session_cookie':
      return 'session_cookie';
    case 'custom_login':
      return 'custom_login';
    default:
      return 'oauth2_client_credentials'; // Default fallback
  }
};

function AuthPilotContent() {
  const {
    // State
    profile,
    tokenResult,
    logs,
    isTestingConnection,
    candidates,
    bestDetection,
    selectedProfile,
    profilesLoading,
    projectId,
    profiles,

    // Computed values
    validation,
    canTest,

    // Actions
    setProfile,
    handleCandidateSelect,
    handleUseDetectedEndpoint,
    handleProfileSelection,
    handleDetectCandidates,
    handleSaveProfile,
    handleTestConnection,
    setCandidates,
    setBestDetection,
    addLog,
  } = useAuthPilot();
  const { handleDeleteProfile } = useAuthPilot();

  // Handle AI detection response
  const handleAiDetected = useCallback(
    (resp: DetectionResponse) => {
      console.log('🎯 handleAiDetected TRIGGERED! Response received:', resp);

      let mapped: AuthCandidate[] = [];

      // Check if response has candidates array (AuthProfiles format)
      if (resp.candidates && Array.isArray(resp.candidates)) {
        console.log('📋 Processing AuthProfiles format with candidates array');
        mapped = (resp.candidates || []).map(c => {
          console.log('🔄 Mapping candidate:', c);
          const t =
            typeof c.type === 'number' ? Number(c.type) : String(c.type || '');
          const mappedCandidate = {
            type: t as unknown as AuthCandidate['type'],
            confidence: c.confidence || 0,
            token_url: c.tokenUrl ?? undefined,
            header_name: c.injection?.name ?? undefined,
            rawType: String(c.type ?? ''),
            form: c.form ?? null,
          } as AuthCandidate;
          console.log('✅ Mapped candidate:', mappedCandidate);
          return mappedCandidate;
        });
      }
      // Check if response has profile object (Chapi.AI format)
      else if ((resp as ChapiAiDetectionResponse).profile) {
        console.log('📋 Processing Chapi.AI format with profile object');
        const chapiResp = resp as ChapiAiDetectionResponse;
        const profile = chapiResp.profile!;
        const confidence = chapiResp.detect_confidence || 0.5;

        // Map Chapi.AI profile to AuthCandidate
        const grantType =
          chapiResp.profile?.token_request?.body?.value?.grant_type;
        const candidate: AuthCandidate = {
          type: mapChapiAiTypeToAuthType(profile.type, grantType),
          confidence: confidence,
          token_url: profile.parameters?.tokenUrl || undefined,
          header_name: profile.injection?.name || undefined,
          rawType: profile.type || '',
          username_ref: profile.parameters?.usernameRef || undefined,
          password_ref: profile.parameters?.passwordRef || undefined,
          // Map additional OAuth2 fields
          client_id: profile.parameters?.clientId || undefined,
          scopes: profile.parameters?.scopes || undefined,
          audience: profile.parameters?.audience || undefined,
          // Extract user/pass keys from token_request or use parameters
          login_user_key:
            profile.parameters?.customUserKey ||
            (chapiResp.profile?.token_request?.body?.value
              ? Object.keys(chapiResp.profile.token_request.body.value).find(
                  k =>
                    k.toLowerCase().includes('user') ||
                    k.toLowerCase() === 'username'
                )
              : undefined) ||
            'username',
          login_pass_key:
            profile.parameters?.customPassKey ||
            (chapiResp.profile?.token_request?.body?.value
              ? Object.keys(chapiResp.profile.token_request.body.value).find(
                  k =>
                    k.toLowerCase().includes('pass') ||
                    k.toLowerCase() === 'password'
                )
              : undefined) ||
            'password',
          token_json_path:
            profile.parameters?.tokenJsonPath ||
            chapiResp.profile?.token_request?.expect?.tokenJsonPath ||
            undefined,
          form:
            profile.parameters?.customBodyType === 'form'
              ? {
                  grantType:
                    chapiResp.profile?.token_request?.body?.value?.grant_type ||
                    'password',
                  fields: chapiResp.profile?.token_request?.body?.value || {},
                }
              : null,
        };

        console.log('✅ Mapped Chapi.AI profile to candidate:', candidate);
        mapped = [candidate];
      } else {
        console.log(
          '⚠️ Unknown response format, no candidates or profile found'
        );
        mapped = [];
      }

      console.log('📋 Final mapped candidates array:', mapped);
      console.log('📊 Number of candidates:', mapped.length);

      // Update candidates and best detection through the hook's state
      console.log('🔄 Calling setCandidates with:', mapped);
      setCandidates(mapped);

      const bestDetectionData = resp.best
        ? {
            endpoint: resp.best.endpoint!,
            source: resp.best.source!,
            confidence: resp.best.confidence!,
          }
        : (resp as ChapiAiDetectionResponse).detect_source
        ? {
            endpoint:
              (resp as ChapiAiDetectionResponse).profile?.parameters
                ?.tokenUrl || '',
            source: (resp as ChapiAiDetectionResponse).detect_source,
            confidence:
              (resp as ChapiAiDetectionResponse).detect_confidence || 0,
          }
        : null;
      console.log('🔄 Calling setBestDetection with:', bestDetectionData);
      setBestDetection(bestDetectionData);

      // Add log entry for the detection
      if (mapped.length > 0) {
        console.log('📝 Adding log entry for detection');
        addLog({
          type: 'detect',
          status: 'success',
          message: `AI detected ${mapped.length} authentication candidate(s)`,
        });
      } else {
        console.log('⚠️ No candidates detected, adding warning log');
        addLog({
          type: 'detect',
          status: 'error',
          message: 'AI detection completed but no candidates found',
        });
      }
    },
    [setCandidates, setBestDetection, addLog]
  );

  // Handle profile creation
  const handleCreateNew = useCallback(() => {
    setProfile({
      type: 'oauth2_client_credentials',
      token_url: '',
      scopes: '',
      audience: '',
      notes: '',
      client_id: '',
      client_secret: '',
      header_name: 'X-API-Key',
      api_key: '',
      bearer_token: '',
      cookie_value: '',
      username_ref: '',
      password_ref: '',
      login_body_type: 'form',
      login_user_key: 'username',
      login_pass_key: 'password',
      token_json_path: '$.access_token',
    });
  }, [setProfile]);

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

  // Debug logging for candidates
  useEffect(() => {
    console.log('🚀 Page - candidates changed:', candidates);
    console.log('🚀 Page - candidates length:', candidates?.length || 0);
    if (candidates && candidates.length > 0) {
      console.log('🚀 Page - first candidate:', candidates[0]);
    }
  }, [candidates]);

  useEffect(() => {
    console.log('🚀 Page - bestDetection changed:', bestDetection);
  }, [bestDetection]);

  return (
    <div className="min-h-screen bg-background">
      {/* Header */}
      <AuthPilotHeader />

      {/* Main Content */}
      <div className="max-w-7xl mx-auto px-6 py-6">
        <div className="grid grid-cols-1 xl:grid-cols-3 lg:grid-cols-2 gap-6">
          {/* Left Column - Detection & Candidates */}
          <div className="space-y-6">
            <AnimatedContainer delay={200}>
              <AuthPilotDetection
                profile={profile}
                candidates={candidates}
                bestDetection={bestDetection}
                projectId={projectId}
                profilesLoading={profilesLoading}
                onCandidateSelect={handleCandidateSelect}
                onUseDetectedEndpoint={handleUseDetectedEndpoint}
                onDetectCandidates={handleDetectCandidates}
                onAiDetected={handleAiDetected}
              />
            </AnimatedContainer>
          </div>

          {/* Middle Column - Auth Profile List */}
          <div className="space-y-6">
            <AnimatedContainer delay={100}>
              <AuthPilotProfiles
                profiles={profiles}
                selectedProfile={selectedProfile}
                profilesLoading={profilesLoading}
                onSelectProfile={handleProfileSelection}
                onDeleteProfile={async p => {
                  // ensure selected in UI and then call delete handler from hook
                  handleProfileSelection(p);
                  try {
                    await handleDeleteProfile(p);
                  } catch (e) {
                    console.error('Delete failed', e);
                  }
                }}
                onCreateNew={handleCreateNew}
              />
            </AnimatedContainer>
          </div>

          {/* Right Column - Profile Details & Test */}
          <div className="space-y-6">
            <AnimatedContainer delay={300}>
              <AuthPilotTesting
                profile={profile}
                tokenResult={tokenResult}
                validation={validation}
                canTest={canTest}
                isTestingConnection={isTestingConnection}
                onProfileChange={setProfile}
                onTestConnection={handleTestConnection}
                onSaveProfile={handleSaveProfile}
              />
            </AnimatedContainer>

            {/* Status/Log Panel */}
            <AuthPilotLogs logs={logs} />
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
