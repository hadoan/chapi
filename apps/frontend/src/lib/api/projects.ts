import { AuthService } from './auth-service';
import type { components } from './schema';

export type ProjectDto = components['schemas']['Projects.Application.Dtos.ProjectDto'];
export type CreateProjectRequest = components['schemas']['Projects.Application.Dtos.CreateProjectRequest'];
export type UpdateProjectRequest = components['schemas']['Projects.Application.Dtos.UpdateProjectRequest'];

export interface ProjectSearchParams {
  search?: string;
  status?: string;
  page?: number;
  pageSize?: number;
}

export const projectsApi = {
  async getAll(params?: ProjectSearchParams): Promise<ProjectDto[]> {
    const searchParams = new URLSearchParams();
    if (params?.search) searchParams.append('search', params.search);
    if (params?.status) searchParams.append('status', params.status);
    if (params?.page) searchParams.append('page', params.page.toString());
    if (params?.pageSize) searchParams.append('pageSize', params.pageSize.toString());

    const queryString = searchParams.toString();
    const url = queryString ? `/api/projects?${queryString}` : '/api/projects';

    return await AuthService.authenticatedFetch<ProjectDto[]>(url, { method: 'GET' });
  },

  async getById(id: string): Promise<ProjectDto> {
    return await AuthService.authenticatedFetch<ProjectDto>(`/api/projects/${id}`, { method: 'GET' });
  },

  async create(project: CreateProjectRequest): Promise<ProjectDto> {
    return await AuthService.authenticatedFetch<ProjectDto>('/api/projects', { method: 'POST', data: project });
  },

  async update(id: string, project: UpdateProjectRequest): Promise<ProjectDto> {
    return await AuthService.authenticatedFetch<ProjectDto>(`/api/projects/${id}`, { method: 'PUT', data: project });
  },

  async delete(id: string): Promise<void> {
    await AuthService.authenticatedFetch<void>(`/api/projects/${id}`, { method: 'DELETE' });
  }
};
