import { AuthService } from './auth-service';
import type { components } from './schema';

export type GenerateRequest =
  components['schemas']['Chapi.AI.Dto.ApiTestGenerateRequest'];
export type ChapiCard = components['schemas']['Chapi.AI.Dto.ChapiCard'];

// DTO for backend GenerateEndpointRequest (C# -> JSON camelCase)
export interface GenerateEndpointRequest {
  authProfileId: string; // GUID
  endpointId: string; // GUID
}

// Use generated schema type for TestGenResponse
export type TestGenResponse =
  components['schemas']['Chapi.AI.Dto.TestGenResponse'];

export const llmsApi = {
  async generate(body: GenerateRequest): Promise<ChapiCard> {
    return await AuthService.authenticatedFetch<ChapiCard>(
      `/api/llm/generate`,
      { method: 'POST', data: body }
    );
  },
};

export const testGenApi = {
  async generate(body: GenerateRequest): Promise<TestGenResponse> {
    return await AuthService.authenticatedFetch<TestGenResponse>(
      `/api/testgen/generate`,
      { method: 'POST', data: body }
    );
  },
  async generateEndpoint(
    body: GenerateEndpointRequest | GenerateRequest
  ): Promise<TestGenResponse> {
    return await AuthService.authenticatedFetch<TestGenResponse>(
      `/api/testgen/generate/endpoint`,
      { method: 'POST', data: body }
    );
  },
};
