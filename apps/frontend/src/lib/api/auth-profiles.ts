import { AuthService } from './auth-service';
import type { components } from './schema';

// Extract types from the generated schema
export type CreateAuthProfileRequest =
  components['schemas']['AuthProfiles.Application.Requests.CreateAuthProfileRequest'];
export type UpdateAuthProfileRequest =
  components['schemas']['AuthProfiles.Application.Requests.UpdateAuthProfileRequest'];
export type DetectRequest =
  components['schemas']['AuthProfiles.Controllers.AuthProfilesController.DetectRequest'];
export type AuthType = components['schemas']['AuthProfiles.Domain.AuthType'];
export type InjectionMode =
  components['schemas']['AuthProfiles.Domain.InjectionMode'];

// For response types, we'll use the generated schema types
export type AuthProfileDto =
  components['schemas']['AuthProfiles.Application.Dtos.AuthProfileDto'];
export type AuthDetectionCandidate =
  components['schemas']['AuthProfiles.Application.Dtos.AuthDetectionCandidateDto'];
export type TestAuthResponse =
  components['schemas']['AuthProfiles.Application.Dtos.TestAuthResult'];
export type DetectTokenRequest =
  components['schemas']['AuthProfiles.Application.Dtos.DetectTokenRequest'];
export type DetectionResponse =
  components['schemas']['AuthProfiles.Application.Dtos.DetectionResponse'];
export type SimpleDetection =
  components['schemas']['AuthProfiles.Application.Dtos.SimpleDetection'];

// Query parameter interface (not in schema)
export interface GetAuthProfilesQuery {
  page?: number;
  pageSize?: number;
  enabled?: boolean;
  projectId?: string;
  serviceId?: string;
  env?: string;
  search?: string;
}

// Detection result interface (not in schema)
export interface AuthDetectionResult {
  candidates: AuthDetectionCandidate[];
  source: string;
  confidence: number;
}

export const authProfilesApi = {
  async getAll(params?: GetAuthProfilesQuery): Promise<AuthProfileDto[]> {
    const searchParams = new URLSearchParams();
    if (params?.page) searchParams.append('Page', params.page.toString());
    if (params?.pageSize)
      searchParams.append('PageSize', params.pageSize.toString());
    if (params?.enabled !== undefined)
      searchParams.append('Enabled', params.enabled.toString());
    if (params?.projectId) searchParams.append('ProjectId', params.projectId);
    if (params?.serviceId) searchParams.append('ServiceId', params.serviceId);
    if (params?.env) searchParams.append('Env', params.env);
    if (params?.search) searchParams.append('Search', params.search);

    const queryString = searchParams.toString();
    const url = queryString
      ? `/api/authprofiles?${queryString}`
      : '/api/authprofiles';

    return await AuthService.authenticatedFetch<AuthProfileDto[]>(url, {
      method: 'GET',
    });
  },

  async getById(id: string): Promise<AuthProfileDto> {
    return await AuthService.authenticatedFetch<AuthProfileDto>(
      `/api/authprofiles/${id}`,
      { method: 'GET' }
    );
  },

  async getFirst(
    projectId: string,
    environmentKey: string
  ): Promise<AuthProfileDto | null> {
    const url = `/api/authprofiles/first?projectId=${encodeURIComponent(
      projectId
    )}&environmentKey=${encodeURIComponent(environmentKey)}`;
    try {
      const res = await AuthService.authenticatedFetch<AuthProfileDto>(url, {
        method: 'GET',
      });
      return res || null;
    } catch (err) {
      // If 404, return null; otherwise rethrow
      // AuthService throws for non-2xx; try to detect 404 by message
      if (err instanceof Error && err.message && err.message.includes('404'))
        return null;
      throw err;
    }
  },

  async create(profile: CreateAuthProfileRequest): Promise<AuthProfileDto> {
    return await AuthService.authenticatedFetch<AuthProfileDto>(
      '/api/authprofiles',
      {
        method: 'POST',
        data: profile,
      }
    );
  },

  async update(
    id: string,
    profile: UpdateAuthProfileRequest
  ): Promise<AuthProfileDto> {
    return await AuthService.authenticatedFetch<AuthProfileDto>(
      `/api/authprofiles/${id}`,
      {
        method: 'PUT',
        data: profile,
      }
    );
  },

  async delete(id: string): Promise<void> {
    await AuthService.authenticatedFetch<void>(`/api/authprofiles/${id}`, {
      method: 'DELETE',
    });
  },

  async enable(id: string): Promise<void> {
    await AuthService.authenticatedFetch<void>(
      `/api/authprofiles/${id}/enable`,
      { method: 'POST' }
    );
  },

  async disable(id: string): Promise<void> {
    await AuthService.authenticatedFetch<void>(
      `/api/authprofiles/${id}/disable`,
      { method: 'POST' }
    );
  },

  async detect(request: DetectTokenRequest): Promise<DetectionResponse> {
    return await AuthService.authenticatedFetch<DetectionResponse>(
      '/api/authprofiles/detect',
      {
        method: 'POST',
        data: request,
      }
    );
  },

  async test(
    request: components['schemas']['AuthProfiles.Controllers.AuthProfilesController.TestRequest']
  ): Promise<TestAuthResponse> {
    return await AuthService.authenticatedFetch<TestAuthResponse>(
      '/api/authprofiles/test',
      {
        method: 'POST',
        data: request,
      }
    );
  },

  // AI-based detection endpoints
  async detectByCode(request: {
    code: string;
    projectId?: string;
    serviceId?: string;
  }): Promise<DetectionResponse> {
    return await AuthService.authenticatedFetch<DetectionResponse>(
      '/api/authprofiles/detect/ai/code',
      {
        method: 'POST',
        data: request,
      }
    );
  },

  async detectByPrompt(request: {
    prompt: string;
    projectId?: string;
    serviceId?: string;
  }): Promise<DetectionResponse> {
    return await AuthService.authenticatedFetch<DetectionResponse>(
      '/api/authprofiles/detect/ai/prompt',
      {
        method: 'POST',
        data: request,
      }
    );
  },
};
