import { useAuthProfiles } from '@/hooks/use-auth-profiles';
import { toast } from '@/hooks/use-toast';
import { authProfilesApi } from '@/lib/api/auth-profiles';
import type { components } from '@/lib/api/schema';
import {
  createInitialProfile,
  formatTimestamp,
  getErrorMessage,
  simulateTokenRequest,
  validateProfile,
} from '@/lib/auth-pilot';
import { STORAGE_KEY } from '@/lib/auth-pilot/constants';
import { useProject } from '@/lib/state/projectStore';
import type {
  AuthCandidate,
  AuthProfile,
  Environment,
  LogEntry,
  TokenResult,
} from '@/types/auth-pilot';
import { useCallback, useEffect, useState } from 'react';

export interface UseAuthPilotReturn {
  // State
  environment: Environment;
  profile: AuthProfile;
  tokenResult: TokenResult | undefined;
  logs: LogEntry[];
  isTestingConnection: boolean;
  candidates: AuthCandidate[];
  bestDetection: {
    endpoint: string;
    source: string;
    confidence: number;
  } | null;
  selectedProfile: AuthProfile | null;
  projectId: string | undefined;
  profilesLoading: boolean;
  profiles: AuthProfile[];

  // Computed values
  validation: { isValid: boolean; errors: string[] };
  canTest: boolean;

  // Actions
  setEnvironment: (env: Environment) => void;
  setProfile: (profile: AuthProfile) => void;
  setSelectedProfile: (profile: AuthProfile | null) => void;
  setCandidates: (candidates: AuthCandidate[]) => void;
  setBestDetection: (
    detection: { endpoint: string; source: string; confidence: number } | null
  ) => void;
  handleCandidateSelect: (candidate: AuthCandidate) => void;
  handleUseDetectedEndpoint: (bestEndpoint?: string) => void;
  handleProfileSelection: (selectedAuthProfile: AuthProfile | null) => void;
  handleDeleteProfile: (profile: AuthProfile) => Promise<void>;
  handleDetectCandidates: () => Promise<void>;
  handleSaveProfile: () => Promise<void>;
  handleTestConnection: () => Promise<void>;
  addLog: (entry: Omit<LogEntry, 'timestamp'>) => void;
}

export const useAuthPilot = (): UseAuthPilotReturn => {
  const [environment, setEnvironment] = useState<Environment>('Dev');
  const [profile, setProfile] = useState<AuthProfile>(createInitialProfile());
  const { selectedProject } = useProject();
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
  const [selectedProfile, setSelectedProfileState] =
    useState<AuthProfile | null>(null);

  // Use the auth profiles hook
  const {
    profiles,
    backendProfiles,
    loading: profilesLoading,
    createProfile,
    loadProfiles,
    detectCandidates,
    deleteProfile: deleteProfileApi,
  } = useAuthProfiles({
    projectId: selectedProject?.id,
    serviceId: undefined,
    autoLoad: true,
  });

  // Debug logging for state changes
  useEffect(() => {
    console.log('🏠 Hook - candidates state changed:', candidates);
    console.log('🏠 Hook - candidates length:', candidates?.length || 0);
  }, [candidates]);

  useEffect(() => {
    console.log('🏠 Hook - bestDetection state changed:', bestDetection);
  }, [bestDetection]);

  useEffect(() => {
    console.log('🏠 Hook - logs state changed:', logs);
    console.log('🏠 Hook - logs length:', logs?.length || 0);
  }, [logs]);

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

  const handleCandidateSelect = useCallback(
    (candidate: AuthCandidate) => {
      if (candidate.disabled) return;

      // Start with defaults and then apply form/type hints
      const newProfile: AuthProfile = {
        type: candidate.type,
        token_url: candidate.token_url || profile.token_url,
        // Preserve detected values instead of using current profile values
        scopes: candidate.scopes || profile.scopes,
        audience: candidate.audience || profile.audience,
        notes: profile.notes,
        // OAuth2 Client Credentials - use detected values
        client_id: candidate.client_id || '',
        client_secret: '',
        // API Key Header
        header_name: candidate.header_name || 'X-API-Key',
        api_key: '',
        // Bearer Static
        bearer_token: '',
        // Session Cookie
        cookie_value: '',
        // Map optional prefill secret refs from detection
        username_ref: (candidate.username_ref ?? '') as string,
        password_ref: (candidate.password_ref ?? '') as string,
        // Custom login options - use detected values
        login_body_type: 'form',
        login_user_key: candidate.login_user_key || 'username',
        login_pass_key: candidate.login_pass_key || 'password',
        token_json_path: candidate.token_json_path || '$.access_token',
      };

      // If detection provided a form hint, extract additional field mapping
      if (candidate.form?.grantType) {
        // For password grant, ensure we use the username/password field values from the form if available
        if (candidate.form.grantType.toLowerCase() === 'password') {
          if (candidate.form.fields?.username)
            newProfile.username_ref = candidate.form.fields.username;
          if (candidate.form.fields?.password)
            newProfile.password_ref = candidate.form.fields.password;
        }
      }

      // If candidate indicates api key, ensure header shown
      if (candidate.type === 'api_key_header') {
        newProfile.header_name =
          candidate.header_name || newProfile.header_name;
      }

      setProfile(newProfile);
      setTokenResult(undefined); // Clear previous test results
    },
    [profile]
  );

  const handleUseDetectedEndpoint = useCallback((bestEndpoint?: string) => {
    if (!bestEndpoint) return;

    setProfile(prev => ({
      ...prev,
      token_url: bestEndpoint,
    }));

    toast({
      title: 'Endpoint updated',
      description: `Token URL set to detected endpoint: ${bestEndpoint}`,
    });
  }, []);

  const handleProfileSelection = useCallback(
    (selectedAuthProfile: AuthProfile | null) => {
      setSelectedProfileState(selectedAuthProfile);
      if (selectedAuthProfile) {
        // Update the current profile with selected profile data
        setProfile(selectedAuthProfile);
        setTokenResult(undefined); // Clear previous test results
        addLog({
          type: 'detect',
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
    },
    [addLog]
  );

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

  const handleDeleteProfile = useCallback(
    async (targetProfile: AuthProfile) => {
      try {
        // Find matching backend profile by token_url and type
        const idx = profiles.findIndex(
          p =>
            p.token_url === targetProfile.token_url &&
            p.type === targetProfile.type
        );

        // Fallback to reference equality if not found
        const fallbackIdx = idx === -1 ? profiles.indexOf(targetProfile) : idx;

        const selectedBackend = (backendProfiles || [])[fallbackIdx];
        if (!selectedBackend || !selectedBackend.id) {
          toast({
            title: 'Error',
            description: 'Unable to determine backend profile id for deletion',
            variant: 'destructive',
          });
          return;
        }

        await deleteProfileApi(selectedBackend.id);

        // If the deleted profile was the currently selected one, clear selection
        setSelectedProfileState(prev => {
          if (
            prev &&
            prev.token_url === targetProfile.token_url &&
            prev.type === targetProfile.type
          ) {
            setProfile(createInitialProfile());
            return null;
          }
          return prev;
        });

        // Refresh profiles list from backend so the UI reflects the deletion
        try {
          await loadProfiles();
        } catch (e) {
          // non-fatal
          console.warn('Failed to refresh auth profiles after delete', e);
        }

        addLog({
          type: 'save',
          status: 'success',
          message: `Profile deleted: ${
            targetProfile.notes || targetProfile.token_url
          }`,
        });
      } catch (error) {
        addLog({
          type: 'save',
          status: 'error',
          message: 'Failed to delete profile',
        });
      }
    },
    [profiles, backendProfiles, deleteProfileApi, addLog, loadProfiles]
  );

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

  return {
    // State
    environment,
    profile,
    tokenResult,
    logs,
    isTestingConnection,
    candidates,
    bestDetection,
    selectedProfile,
    projectId,
    profilesLoading,
    profiles,

    // Computed values
    validation,
    canTest,

    // Actions
    setEnvironment,
    setProfile,
    setSelectedProfile: setSelectedProfileState,
    setCandidates,
    setBestDetection,
    handleCandidateSelect,
    handleUseDetectedEndpoint,
    handleProfileSelection,
    handleDetectCandidates,
    handleSaveProfile,
    handleTestConnection,
    handleDeleteProfile,
    addLog,
  };
};
