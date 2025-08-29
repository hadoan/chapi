export type EnvName = 'local' | 'staging' | 'prod';

export type EnvModel = {
  id: string;
  name: string;
  baseUrl: string;
  timeoutMs: number;
  followRedirects: boolean;
  headers: Record<string, string>;
  secrets: Record<string, string>;
  createdAt: string;
  locked?: boolean;
}
