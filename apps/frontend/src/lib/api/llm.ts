import { AuthService } from './auth-service';
import type { components } from './schema';

export type DetectionResponse =
  components['schemas']['AuthProfiles.Application.Dtos.DetectionResponse'];

export const llmApi = {
  async detectByCode(request: {
    code: string;
    projectId?: string;
    serviceId?: string;
  }): Promise<DetectionResponse> {
    return await AuthService.authenticatedFetch<DetectionResponse>(
      '/api/llm/detect/code',
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
      '/api/llm/detect/prompt',
      {
        method: 'POST',
        data: request,
      }
    );
  },
};
