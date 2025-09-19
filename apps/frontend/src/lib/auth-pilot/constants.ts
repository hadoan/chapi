import type { AuthType, Environment } from '@/types/auth-pilot';

// Constants for the Auth Pilot feature
export const STORAGE_KEY = 'chapi-auth-pilot-demo';

// Default values
export const DEFAULT_ENVIRONMENT: Environment = 'Dev';
export const DEFAULT_LOGIN_BODY_TYPE = 'form';
export const DEFAULT_LOGIN_USER_KEY = 'username';
export const DEFAULT_LOGIN_PASS_KEY = 'password';
export const DEFAULT_HEADER_NAME = 'X-API-Key';
export const DEFAULT_TOKEN_JSON_PATH = '$.access_token';
export const DEFAULT_TOKEN_TYPE = 'Bearer';
export const DEFAULT_EXPIRES_IN = 3600;

// Environment options
export const ENVIRONMENT_OPTIONS: Environment[] = ['Dev', 'Stage', 'Prod'];

// Auth type labels for display
export const AUTH_TYPE_LABELS: Record<AuthType, string> = {
  oauth2_client_credentials: 'OAuth2 Client Credentials',
  password: 'Password Flow',
  api_key_header: 'API Key Header',
  bearer_static: 'Bearer Token',
  session_cookie: 'Session Cookie',
  basic: 'Basic Auth',
  custom_login: 'Custom Login',
  device_code: 'Device Code',
  auth_code: 'Authorization Code',
};

// Form configuration
export const FORM_CONFIG = {
  maxLogs: 100,
  debounceDelay: 300,
  animationDuration: 200,
} as const;

// Error messages
export const ERROR_MESSAGES = {
  INVALID_REQUEST: 'Invalid request parameters',
  INVALID_CLIENT: 'Invalid client credentials',
  INVALID_GRANT: 'Invalid grant type or credentials',
  UNAUTHORIZED_CLIENT: 'Client not authorized for this grant',
  UNSUPPORTED_GRANT_TYPE: 'Unsupported grant type',
  INVALID_SCOPE: 'Invalid scope requested',
  SIMULATION_FAILED: 'Simulation failed - check profile configuration',
} as const;
