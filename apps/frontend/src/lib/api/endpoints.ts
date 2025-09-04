import { AuthService } from './auth-service';
import type { components } from './schema';

export type EndpointBriefDto = components['schemas']['Chapi.EndpointCatalog.Application.EndpointBriefDto'];
export type EndpointDto = components['schemas']['Chapi.EndpointCatalog.Application.EndpointDto'];

export const endpointsApi = {
  async listByProject(projectId: string): Promise<EndpointBriefDto[]> {
    return await AuthService.authenticatedFetch<EndpointBriefDto[]>(`/api/projects/${projectId}/endpoints`, { method: 'GET' });
  },

  async get(projectId: string, endpointId: string): Promise<EndpointDto> {
    return await AuthService.authenticatedFetch<EndpointDto>(`/api/projects/${projectId}/endpoints/${endpointId}`, { method: 'GET' });
  }
};
