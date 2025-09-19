import type { AuthProfile, TokenResult } from '@/types/auth-pilot';

export const createInitialProfile = (): AuthProfile => ({
  type: 'oauth2_client_credentials',
  token_url: '',
  scopes: '',
  audience: '',
  notes: '',
  client_id: '',
  client_secret: '',
  header_name: 'X-API-Key',
  api_key: '',
  bearer_token: '',
  cookie_value: '',
  username_ref: '',
  password_ref: '',
  login_body_type: 'form',
  login_user_key: 'username',
  login_pass_key: 'password',
  token_json_path: '$.access_token',
});

export const formatTimestamp = (): string => {
  const now = new Date();
  return now.toLocaleTimeString('en-US', {
    hour12: false,
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
  });
};

export const getErrorMessage = (status: string): string => {
  switch (status) {
    case 'invalid_request':
      return 'Invalid request parameters';
    case 'invalid_client':
      return 'Invalid client credentials';
    case 'invalid_grant':
      return 'Invalid grant type or credentials';
    case 'unauthorized_client':
      return 'Client not authorized for this grant';
    case 'unsupported_grant_type':
      return 'Unsupported grant type';
    case 'invalid_scope':
      return 'Invalid scope requested';
    default:
      return 'Authentication failed';
  }
};

export const simulateTokenRequest = (profile: AuthProfile): TokenResult => {
  if (profile.type === 'api_key_header' && profile.api_key) {
    return {
      status: 'ok',
      access_token: `simulated_${profile.api_key}`,
      token_type: 'Bearer',
      expires_in: 3600,
    };
  }
  if (profile.type === 'bearer_static' && profile.bearer_token) {
    return {
      status: 'ok',
      access_token: profile.bearer_token,
      token_type: 'Bearer',
      expires_in: 3600,
    };
  }
  return {
    status: 'invalid_client',
    message: 'Simulation failed - check profile configuration',
  };
};

export const validateProfile = (
  profile: AuthProfile
): { isValid: boolean; errors: string[] } => {
  const errors: string[] = [];
  if (
    !profile.token_url &&
    profile.type !== 'api_key_header' &&
    profile.type !== 'bearer_static'
  ) {
    errors.push('Token URL is required');
  }
  if (
    profile.type === 'oauth2_client_credentials' &&
    (!profile.client_id || !profile.client_secret)
  ) {
    errors.push(
      'Client ID and Secret are required for OAuth2 Client Credentials'
    );
  }
  if (
    profile.type === 'password' &&
    (!profile.username_ref || !profile.password_ref)
  ) {
    errors.push(
      'Username and Password references are required for Password flow'
    );
  }
  if (profile.type === 'api_key_header' && !profile.api_key) {
    errors.push('API Key is required');
  }
  if (profile.type === 'bearer_static' && !profile.bearer_token) {
    errors.push('Bearer Token is required');
  }
  return { isValid: errors.length === 0, errors };
};
