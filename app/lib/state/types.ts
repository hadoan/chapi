export type EnvName = 'local' | 'staging' | 'prod';

export type EnvModel = {
  name: EnvName;
  baseUrl: string;
  timeoutMs: number;
  followRedirects: boolean;
  headers: Record<string, string>;
  secrets: Record<string, string>;
  updatedAt: string;
  locked?: boolean;
}
