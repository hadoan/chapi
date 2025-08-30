import { AuthService } from './auth-service';
import type { components } from './schema';

export type ApiSpecDto = components['schemas']['Chapi.ApiSpecs.Application.ApiSpecDto'];
export type ImportOpenApiRequest = components['schemas']['Chapi.ApiSpecs.Application.ImportOpenApiInputDto'];

export const apiSpecsApi = {
  async importOpenApi(projectId: string, body: ImportOpenApiRequest): Promise<ApiSpecDto> {
    return await AuthService.authenticatedFetch<ApiSpecDto>(`/api/projects/${projectId}/openapi/import`, { method: 'POST', data: body });
  },

  async getById(specId: string): Promise<ApiSpecDto> {
    return await AuthService.authenticatedFetch<ApiSpecDto>(`/api/openapi/${specId}`, { method: 'GET' });
  },

  async listByProject(projectId: string): Promise<ApiSpecDto[]> {
    return await AuthService.authenticatedFetch<ApiSpecDto[]>(`/api/projects/${projectId}/openapi`, { method: 'GET' });
  }
};
