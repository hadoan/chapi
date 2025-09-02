import axios from 'axios';
import { config } from '../config/app.config';
import { AuthService } from './auth-service';
import type { components } from './schema';

export type GenerateRequest =
  components['schemas']['Chapi.AI.Controllers.RunPackController.GenerateRequest'];

export interface GenerateResponse {
  blob: Blob;
  runId: string;
  storagePath?: string;
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
  async generate(body: GenerateRequest): Promise<GenerateResponse> {
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
    const storagePath =
      response.headers['x-storage-path'] || response.headers['X-Storage-Path'];

    return {
      blob: response.data,
      runId,
      storagePath,
    };
  },

  async downloadRun(runId: string, specificFile?: string): Promise<Blob> {
    const params = specificFile
      ? `?file=${encodeURIComponent(specificFile)}`
      : '';
    return await AuthService.authenticatedFetch<Blob>(
      `/api/run-pack/runs/${runId}${params}`,
      {
        method: 'GET',
        responseType: 'blob',
      }
    );
  },

  async deleteRun(runId: string): Promise<void> {
    await AuthService.authenticatedFetch(`/api/run-pack/runs/${runId}`, {
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
};
