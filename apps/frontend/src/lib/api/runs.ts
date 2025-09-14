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

  getArtifactUrl(runId: string, stepId: string, name: string): string {
    return `/api/runs/${runId}/artifacts/${stepId}/${name}`;
  },
};
