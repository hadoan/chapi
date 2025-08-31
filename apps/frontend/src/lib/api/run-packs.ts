import { AuthService } from './auth-service';
import type { components } from './schema';

export type GenerateRequest = components['schemas']['Chapi.AI.Controllers.RunPackController.GenerateRequest'];

export const runPacksApi = {
  async generate(body: GenerateRequest): Promise<Blob> {
    // Returns a ZIP blob; use axios responseType via AuthService options
    return await AuthService.authenticatedFetch<Blob>(`/api/run-pack/generate`, { method: 'POST', data: body, responseType: 'blob' });
  }
};
