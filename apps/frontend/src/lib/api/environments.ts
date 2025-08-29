import { AuthService } from './auth-service';
import type { components } from './schema';

export type EnvironmentDto = components['schemas']['Environments.Application.Dtos.EnvironmentDto'];
export type CreateEnvironmentRequest = components['schemas']['Environments.Application.Dtos.CreateEnvironmentRequest'];
export type UpdateEnvironmentRequest = components['schemas']['Environments.Application.Dtos.UpdateEnvironmentRequest'];
export type EnvironmentHeaderDto = components['schemas']['Environments.Application.Dtos.EnvironmentHeaderDto'];
export type SecretRefDto = components['schemas']['Environments.Application.Dtos.SecretRefDto'];

export interface EnvironmentSearchParams {
  search?: string;
  page?: number;
  pageSize?: number;
}

export const environmentsApi = {
  async getAll(params?: EnvironmentSearchParams): Promise<EnvironmentDto[]> {
    const searchParams = new URLSearchParams();
    if (params?.search) searchParams.append('search', params.search);
    if (params?.page) searchParams.append('page', params.page.toString());
    if (params?.pageSize) searchParams.append('pageSize', params.pageSize.toString());

    const queryString = searchParams.toString();
    const url = queryString ? `/api/environments?${queryString}` : '/api/environments';

    return await AuthService.authenticatedFetch<EnvironmentDto[]>(url, { method: 'GET' });
  },

  async getById(id: string): Promise<EnvironmentDto> {
    return await AuthService.authenticatedFetch<EnvironmentDto>(`/api/environments/${id}`, { method: 'GET' });
  },

  async create(environment: CreateEnvironmentRequest): Promise<EnvironmentDto> {
    return await AuthService.authenticatedFetch<EnvironmentDto>('/api/environments', { method: 'POST', data: environment });
  },

  async update(id: string, environment: UpdateEnvironmentRequest): Promise<EnvironmentDto> {
    return await AuthService.authenticatedFetch<EnvironmentDto>(`/api/environments/${id}`, { method: 'PUT', data: environment });
  },

  async delete(id: string): Promise<void> {
    await AuthService.authenticatedFetch<void>(`/api/environments/${id}`, { method: 'DELETE' });
  }

  ,

  // Server-side connection test (performs the outbound request from the server)
  async test(id: string): Promise<{ ok: boolean; status?: number; reason?: string; elapsedMs?: number; error?: string }> {
    return await AuthService.authenticatedFetch<{ ok: boolean; status?: number; reason?: string; elapsedMs?: number; error?: string }>(`/api/environments/${id}/test`, { method: 'POST' });
  }
};
