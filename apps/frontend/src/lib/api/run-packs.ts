import axios from 'axios';
import { config } from '../config/app.config';
import { AuthService } from './auth-service';
import type { components } from './schema';

export type GenerateRunPackRequest =
  components['schemas']['Chapi.AI.Services.GenerateRunPackRequest'];

export interface GenerateResponse {
  blob: Blob;
  runId: string; // This will now be RunPack ID from X-RunPack-Id header
  runPackId: string; // Explicit RunPack ID field
  storagePath?: string;
}

export interface RunPackDto {
  id: string;
  projectId: string;
  conversationId?: string;
  runId?: string;
  mode: string;
  filesCount: number;
  zipUrl?: string;
  status: string;
  generatorVersion?: string;
  cardHash?: string;
  inputsHash?: string;
  finalizedAt?: string;
  createdAt: string;
  updatedAt: string;
}

export interface RunPackFileListResult {
  files: Array<{
    runId: string;
    projectId: string;
    projectPath: string;
    fileCount: number;
    createdAt: string;
    environment: string;
    generatedFiles: string[];
  }>;
  totalCount: number;
  currentPage: number;
  pageSize: number;
}

export const runPacksApi = {
  async generate(body: GenerateRunPackRequest): Promise<GenerateResponse> {
    // Use axios directly to access headers
    const token = AuthService.getToken();
    if (!token) {
      throw new Error('No authentication token available');
    }

    const response = await axios.post(
      `${config.apiUrl}/api/run-pack/generate`,
      body,
      {
        headers: {
          Authorization: `Bearer ${token}`,
          'Content-Type': 'application/json',
        },
        responseType: 'blob',
      }
    );

    const runId =
      response.headers['x-file-id'] || response.headers['X-File-Id'] || '';
    const runPackId =
      response.headers['x-runpack-id'] ||
      response.headers['X-RunPack-Id'] ||
      runId; // Fallback to runId if no RunPack ID
    const storagePath =
      response.headers['x-storage-path'] || response.headers['X-Storage-Path'];

    return {
      blob: response.data,
      runId: runPackId, // Use RunPack ID as the primary ID for Browse Files
      runPackId,
      storagePath,
    };
  },

  async downloadRun(runPackId: string, specificFile?: string): Promise<Blob> {
    const params = specificFile
      ? `?file=${encodeURIComponent(specificFile)}`
      : '';
    return await AuthService.authenticatedFetch<Blob>(
      `/api/run-pack/runs/${runPackId}${params}`,
      {
        method: 'GET',
        responseType: 'blob',
      }
    );
  },

  async deleteRun(runPackId: string): Promise<void> {
    await AuthService.authenticatedFetch(`/api/run-pack/runs/${runPackId}`, {
      method: 'DELETE',
    });
  },

  async getRunPackFiles(
    projectId?: string,
    page = 1,
    pageSize = 10
  ): Promise<RunPackFileListResult> {
    const params = new URLSearchParams();
    if (projectId) params.append('projectId', projectId);
    params.append('page', page.toString());
    params.append('pageSize', pageSize.toString());

    return await AuthService.authenticatedFetch(
      `/api/run-pack/files?${params}`,
      {
        method: 'GET',
      }
    );
  },

  // RunPack management functions
  async listByConversation(conversationId: string): Promise<RunPackDto[]> {
    return await AuthService.authenticatedFetch<RunPackDto[]>(
      `/api/runpacks/conversation/${conversationId}`,
      {
        method: 'GET',
      }
    );
  },

  async getById(id: string): Promise<RunPackDto> {
    return await AuthService.authenticatedFetch<RunPackDto>(
      `/api/runpacks/${id}`,
      {
        method: 'GET',
      }
    );
  },

  async list(projectId: string): Promise<RunPackDto[]> {
    const params = new URLSearchParams();
    params.append('projectId', projectId);

    return await AuthService.authenticatedFetch<RunPackDto[]>(
      `/api/runpacks?${params}`,
      {
        method: 'GET',
      }
    );
  },
};
