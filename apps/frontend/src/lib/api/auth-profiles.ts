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

// For response types, we'll need to extract from the API response content
// Since the backend returns AuthProfileDto but it's not exposed in schema components,
// we'll infer it from the API responses
export interface AuthProfileDto {
  id: string;
  projectId?: string;
  serviceId?: string;
  environmentKey?: string;
  type: AuthType;
  tokenUrl?: string;
  audience?: string;
  scopesCsv?: string;
  injectionMode: InjectionMode;
  injectionName?: string;
  injectionFormat?: string;
  detectSource?: string;
  detectConfidence?: number;
  enabled: boolean;
  createdAt: string;
  updatedAt: string;
  secretRefs: Record<string, string>;
}

export interface GetAuthProfilesQuery {
  page?: number;
  pageSize?: number;
  enabled?: boolean;
  projectId?: string;
  serviceId?: string;
  env?: string;
  search?: string;
}

export interface AuthDetectionResult {
  candidates: AuthDetectionCandidate[];
  source: string;
  confidence: number;
}

export interface AuthDetectionCandidate {
  type: AuthType;
  tokenUrl?: string;
  audience?: string;
  scopesCsv?: string;
  injectionMode: InjectionMode;
  injectionName?: string;
  injectionFormat?: string;
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

  async detect(request: {
    projectId?: string;
    serviceId?: string;
    baseUrl?: string;
    openApiJson?: string;
    postmanJson?: string;
  }): Promise<{
    candidates: AuthDetectionCandidate[];
    best?: { endpoint: string; source: string; confidence: number };
  }> {
    return await AuthService.authenticatedFetch<{
      candidates: AuthDetectionCandidate[];
      best?: { endpoint: string; source: string; confidence: number };
    }>('/api/authprofiles/detect', {
      method: 'POST',
      data: request,
    });
  },
};
