import { AuthService } from './auth-service';
import type { components } from './schema';

export type GenerateRequest = components['schemas']['Chapi.AI.Dto.ApiTestGenerateRequest'];
export type ChapiCard = components['schemas']['Chapi.AI.Dto.ChapiCard'];

export const llmsApi = {
  async generate(body: GenerateRequest): Promise<ChapiCard> {
    return await AuthService.authenticatedFetch<ChapiCard>(`/api/llm/generate`, { method: 'POST', data: body });
  }
};
