import { AuthService } from './auth-service';
import type { components } from './schema';

export type RunDto = components['schemas']['Runs.Application.Contracts.RunDto'];
export type CreateRunRequest =
  components['schemas']['Runs.Application.Contracts.CreateRunRequest'];
export type CreateRunResponse =
  components['schemas']['Runs.Application.Contracts.CreateRunResponse'];
export type RunEventDto =
  components['schemas']['Runs.Application.Contracts.RunEventDto'];

export const runsApi = {
  async get(id: string): Promise<RunDto> {
    return await AuthService.authenticatedFetch<RunDto>(`/api/runs/${id}`, {
      method: 'GET',
    });
  },

  async create(request: CreateRunRequest): Promise<CreateRunResponse> {
    return await AuthService.authenticatedFetch<CreateRunResponse>(
      '/api/runs',
      {
        method: 'POST',
        data: request,
      }
    );
  },

  async getTimeline(runId: string): Promise<RunEventDto[]> {
    return await AuthService.authenticatedFetch<RunEventDto[]>(
      `/api/runs/${runId}/timeline`,
      { method: 'GET' }
    );
  },

  async list(
    page = 1,
    pageSize = 20,
    projectId?: string,
    status?: string
  ): Promise<{ Items: RunDto[]; Total: number }> {
    const params = new URLSearchParams();
    params.set('page', String(page));
    params.set('pageSize', String(pageSize));
    if (projectId) params.set('projectId', projectId);
    if (status) params.set('status', status);
    const url = `/api/runs?${params.toString()}`;
    // Backend may return camelCased JSON (items/total) or PascalCased (Items/Total)
    type Resp =
      | { Items?: RunDto[]; Total?: number }
      | { items?: RunDto[]; total?: number };
    const data = await AuthService.authenticatedFetch<Resp>(url, {
      method: 'GET',
    });
    let items: RunDto[] = [];
    let total: number = 0;
    if (data) {
      const d = data as Record<string, unknown>;
      if ('Items' in d && Array.isArray(d['Items']))
        items = d['Items'] as RunDto[];
      else if ('items' in d && Array.isArray(d['items']))
        items = d['items'] as RunDto[];

      if ('Total' in d && typeof d['Total'] === 'number')
        total = d['Total'] as number;
      else if ('total' in d && typeof d['total'] === 'number')
        total = d['total'] as number;
    }
    return { Items: items, Total: total };
  },

  getArtifactUrl(runId: string, stepId: string, name: string): string {
    return `/api/runs/${runId}/artifacts/${stepId}/${name}`;
  },
};
