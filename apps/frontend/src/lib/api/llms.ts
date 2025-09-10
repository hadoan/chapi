import { AuthService } from './auth-service';
import type { components } from './schema';

export type GenerateRequest =
  components['schemas']['Chapi.AI.Dto.ApiTestGenerateRequest'];
export type ChapiCard = components['schemas']['Chapi.AI.Dto.ChapiCard'];

export interface TestGenResponse {
  role: string;
  card: ChapiCard;
  files?: Array<{
    path: string;
    content: string;
  }>;
  db_ops: {
    conversations?: Array<{
      id: string;
      project_id: string;
      title: string;
      created_at: string;
      updated_at: string;
    }>;
    messages: Array<{
      id: string;
      conversation_id: string;
      role: string;
      content: string;
      card_type: string;
      card_payload: unknown;
      created_at: string;
    }>;
    run_packs?: Array<{
      id: string;
      project_id: string;
      conversation_id: string;
      message_id: string;
      mode: string;
      files_count: number;
      status: string;
      generator_version: string;
      card_hash: string;
      inputs_hash: string;
      created_at: string;
    }>;
    run_pack_files?: Array<{
      id: string;
      runpack_id: string;
      path: string;
      content: string;
      size_bytes: number;
      role: string;
      created_at: string;
    }>;
    run_pack_inputs?: Array<{
      id: string;
      runpack_id: string;
      file_roles_json: Record<string, string>;
      role_contexts_json: Record<string, unknown>;
      endpoints_context: string;
      allowed_ops: string;
      env: string;
      selector_output_json: Record<string, unknown>;
      notes: string;
      created_at: string;
    }>;
    run_pack_validations?: Array<{
      id: string;
      runpack_id: string;
      file_path: string;
      rule: string;
      passed: boolean;
      details?: unknown;
      created_at: string;
    }>;
  };
}

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
};
