import { toast } from '@/hooks/use-toast';
import { authProfilesApi } from '@/lib/api/auth-profiles';
import type { components } from '@/lib/api/schema';
import { useCallback, useEffect, useState } from 'react';

// Backend types from schema
type CreateAuthProfileRequest =
  components['schemas']['AuthProfiles.Application.Requests.CreateAuthProfileRequest'];
type UpdateAuthProfileRequest =
  components['schemas']['AuthProfiles.Application.Requests.UpdateAuthProfileRequest'];
type AuthProfileDto =
  components['schemas']['AuthProfiles.Application.Dtos.AuthProfileDto'];
type AuthDetectionCandidateDto =
  components['schemas']['AuthProfiles.Application.Dtos.AuthDetectionCandidateDto'];
type DetectRequest =
  components['schemas']['AuthProfiles.Controllers.AuthProfilesController.DetectRequest'];
type AuthType = components['schemas']['AuthProfiles.Domain.AuthType'];
type InjectionMode = components['schemas']['AuthProfiles.Domain.InjectionMode'];

// Convert backend AuthType (numeric enum) to frontend AuthType
const mapBackendAuthType = (
  backendType: AuthType
): import('@/types/auth-pilot').AuthType => {
  // Backend uses numeric enum: 0 | 1 | 2 | 3
  const typeMap: Record<number, import('@/types/auth-pilot').AuthType> = {
    0: 'oauth2_client_credentials',
    1: 'api_key_header',
    2: 'bearer_static',
    3: 'session_cookie',
  };
  return typeMap[backendType as number] || 'oauth2_client_credentials';
};

// Convert frontend AuthType to backend AuthType (numeric enum)
const mapFrontendAuthType = (
  frontendType: import('@/types/auth-pilot').AuthType
): AuthType => {
  const typeMap: Record<import('@/types/auth-pilot').AuthType, AuthType> = {
    oauth2_client_credentials: 0,
    api_key_header: 1,
    bearer_static: 2,
    session_cookie: 3,
    password: 0, // Map unsupported types to oauth2
    device_code: 0,
    auth_code: 0,
  };
  return typeMap[frontendType];
};

// Convert backend profile to frontend profile
const mapToFrontendProfile = (
  backendProfile: AuthProfileDto
): import('@/types/auth-pilot').AuthProfile => {
  return {
    type: mapBackendAuthType(backendProfile.type!),
    token_url: backendProfile.tokenUrl || '',
    scopes: backendProfile.scopesCsv || '',
    audience: backendProfile.audience || '',
    notes: '', // Notes field might be in secretRefs or other location
    client_id: backendProfile.secretRefs?.['client_id'] || '',
    client_secret: backendProfile.secretRefs?.['client_secret'] || '',
    header_name: backendProfile.injectionName || 'X-API-Key',
    api_key: backendProfile.secretRefs?.['api_key'] || '',
    bearer_token: backendProfile.secretRefs?.['bearer_token'] || '',
    cookie_value: backendProfile.secretRefs?.['cookie_value'] || '',
  };
};

// Convert frontend profile to backend create request
const mapToCreateRequest = (
  frontendProfile: import('@/types/auth-pilot').AuthProfile,
  environmentId: string
): CreateAuthProfileRequest => {
  return {
    projectId: undefined, // Will be set by backend or provided separately
    serviceId: undefined, // Will be set by backend or provided separately
    environmentKey: environmentId,
    type: mapFrontendAuthType(frontendProfile.type),
    tokenUrl: frontendProfile.token_url,
    scopesCsv: frontendProfile.scopes,
    audience: frontendProfile.audience,
    injectionMode: 1, // Default to header injection mode
    injectionName: frontendProfile.header_name,
    injectionFormat: undefined,
    secretRefs: {
      client_id: frontendProfile.client_id || '',
      client_secret: frontendProfile.client_secret || '',
      api_key: frontendProfile.api_key || '',
      bearer_token: frontendProfile.bearer_token || '',
      cookie_value: frontendProfile.cookie_value || '',
    },
  };
};

// Convert frontend profile to backend update request
const mapToUpdateRequest = (
  frontendProfile: import('@/types/auth-pilot').AuthProfile
): UpdateAuthProfileRequest => {
  return {
    tokenUrl: frontendProfile.token_url,
    scopesCsv: frontendProfile.scopes,
    audience: frontendProfile.audience,
    injectionMode: 1, // Default to header injection mode
    injectionName: frontendProfile.header_name,
    injectionFormat: undefined,
    secretRefs: {
      client_id: frontendProfile.client_id || '',
      client_secret: frontendProfile.client_secret || '',
      api_key: frontendProfile.api_key || '',
      bearer_token: frontendProfile.bearer_token || '',
      cookie_value: frontendProfile.cookie_value || '',
    },
  };
};

export interface UseAuthProfilesOptions {
  environmentId?: string;
  autoLoad?: boolean;
}

export function useAuthProfiles({
  environmentId = 'default-env',
  autoLoad = true,
}: UseAuthProfilesOptions = {}) {
  const [profiles, setProfiles] = useState<AuthProfileDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Load all profiles
  const loadProfiles = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const result = await authProfilesApi.getAll();
      setProfiles(result || []);
    } catch (err) {
      const errorMessage =
        err instanceof Error ? err.message : 'Failed to load auth profiles';
      setError(errorMessage);
      toast({
        title: 'Error',
        description: errorMessage,
        variant: 'destructive',
      });
    } finally {
      setLoading(false);
    }
  }, []);

  // Create new profile
  const createProfile = useCallback(
    async (frontendProfile: import('@/types/auth-pilot').AuthProfile) => {
      try {
        setLoading(true);
        setError(null);
        const createRequest = mapToCreateRequest(
          frontendProfile,
          environmentId
        );
        const newProfile = await authProfilesApi.create(createRequest);
        if (newProfile) {
          setProfiles(prev => [...prev, newProfile]);
          toast({
            title: 'Success',
            description: 'Auth profile created successfully',
          });
          return newProfile;
        }
      } catch (err) {
        const errorMessage =
          err instanceof Error ? err.message : 'Failed to create auth profile';
        setError(errorMessage);
        toast({
          title: 'Error',
          description: errorMessage,
          variant: 'destructive',
        });
      } finally {
        setLoading(false);
      }
      return null;
    },
    [environmentId]
  );

  // Update existing profile
  const updateProfile = useCallback(
    async (
      profileId: string,
      frontendProfile: import('@/types/auth-pilot').AuthProfile
    ) => {
      try {
        setLoading(true);
        setError(null);
        const updateRequest = mapToUpdateRequest(frontendProfile);
        const updatedProfile = await authProfilesApi.update(
          profileId,
          updateRequest
        );
        if (updatedProfile) {
          setProfiles(prev =>
            prev.map(p => (p.id === profileId ? updatedProfile : p))
          );
          toast({
            title: 'Success',
            description: 'Auth profile updated successfully',
          });
          return updatedProfile;
        }
      } catch (err) {
        const errorMessage =
          err instanceof Error ? err.message : 'Failed to update auth profile';
        setError(errorMessage);
        toast({
          title: 'Error',
          description: errorMessage,
          variant: 'destructive',
        });
      } finally {
        setLoading(false);
      }
      return null;
    },
    []
  );

  // Delete profile
  const deleteProfile = useCallback(async (profileId: string) => {
    try {
      setLoading(true);
      setError(null);
      await authProfilesApi.delete(profileId);
      setProfiles(prev => prev.filter(p => p.id !== profileId));
      toast({
        title: 'Success',
        description: 'Auth profile deleted successfully',
      });
    } catch (err) {
      const errorMessage =
        err instanceof Error ? err.message : 'Failed to delete auth profile';
      setError(errorMessage);
      toast({
        title: 'Error',
        description: errorMessage,
        variant: 'destructive',
      });
    } finally {
      setLoading(false);
    }
  }, []);

  // Enable profile
  const enableProfile = useCallback(
    async (profileId: string) => {
      try {
        setLoading(true);
        setError(null);
        await authProfilesApi.enable(profileId);
        // Reload profiles to get updated state
        await loadProfiles();
        toast({
          title: 'Success',
          description: 'Auth profile enabled successfully',
        });
      } catch (err) {
        const errorMessage =
          err instanceof Error ? err.message : 'Failed to enable auth profile';
        setError(errorMessage);
        toast({
          title: 'Error',
          description: errorMessage,
          variant: 'destructive',
        });
      } finally {
        setLoading(false);
      }
    },
    [loadProfiles]
  );

  // Disable profile
  const disableProfile = useCallback(
    async (profileId: string) => {
      try {
        setLoading(true);
        setError(null);
        await authProfilesApi.disable(profileId);
        // Reload profiles to get updated state
        await loadProfiles();
        toast({
          title: 'Success',
          description: 'Auth profile disabled successfully',
        });
      } catch (err) {
        const errorMessage =
          err instanceof Error ? err.message : 'Failed to disable auth profile';
        setError(errorMessage);
        toast({
          title: 'Error',
          description: errorMessage,
          variant: 'destructive',
        });
      } finally {
        setLoading(false);
      }
    },
    [loadProfiles]
  );

  // Detect auth candidates for a URL
  const detectCandidates = useCallback(
    async (
      url: string,
      projectId?: string
    ): Promise<{
      candidates: import('@/types/auth-pilot').AuthCandidate[];
      best?: { endpoint: string; source: string; confidence: number } | null;
    }> => {
      try {
        setLoading(true);
        setError(null);
        // The backend detect endpoint now fetches specs server-side by projectId
        const detectRequest: DetectRequest = {
          projectId: projectId,
          serviceId: undefined,
          baseUrl: url,
        };

        const res = (await authProfilesApi.detect(
          detectRequest as unknown as DetectRequest
        )) as {
          candidates: AuthDetectionCandidateDto[];
          best?: {
            endpoint: string;
            source: string;
            confidence: number;
          } | null;
        };

        const result = res?.candidates ?? [];
        const best = res?.best ?? null;

        if (result.length === 0) {
          // Show informational alert to user when no candidates found
          toast({
            title: 'No candidates found',
            description:
              'No authentication candidates were detected for this project/service.',
          });
          return { candidates: [], best };
        }

        // Convert backend candidates to frontend format
        const mapped = result.map((candidate: AuthDetectionCandidateDto) => ({
          type: mapBackendAuthType(candidate.type!),
          confidence: candidate.confidence || 0,
          token_url: candidate.tokenUrl,
          header_name: candidate.injectionName,
          disabled: false,
          disabledReason: undefined,
        }));

        return { candidates: mapped, best };
      } catch (err) {
        const errorMessage =
          err instanceof Error
            ? err.message
            : 'Failed to detect auth candidates';
        setError(errorMessage);
        toast({
          title: 'Error',
          description: errorMessage,
          variant: 'destructive',
        });
        return { candidates: [], best: null };
      } finally {
        setLoading(false);
      }
    },
    []
  );

  // Convert profiles to frontend format
  const frontendProfiles = profiles.map(mapToFrontendProfile);

  // Auto-load on mount
  useEffect(() => {
    if (autoLoad) {
      loadProfiles();
    }
  }, [autoLoad, loadProfiles]);

  return {
    profiles: frontendProfiles,
    backendProfiles: profiles,
    loading,
    error,
    loadProfiles,
    createProfile,
    updateProfile,
    deleteProfile,
    enableProfile,
    disableProfile,
    detectCandidates,
  };
}
