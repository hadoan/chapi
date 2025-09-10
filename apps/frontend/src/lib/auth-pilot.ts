import type { AuthProfile, TokenResult } from '@/types/auth-pilot';

export const initialProfile: AuthProfile = {
  type: 'oauth2_client_credentials',
  token_url: 'https://api.demo.local/connect/token',
  client_id: '',
  client_secret: '',
  scopes: 'api.read',
  audience: '',
  notes: '',
};

export function simulateTokenRequest(profile: AuthProfile): TokenResult {
  const now = new Date();

  switch (profile.type) {
    case 'oauth2_client_credentials':
      if (profile.token_url?.includes('fail')) {
        return {
          status: 'network_error',
          message: 'Network unreachable',
        };
      }
      if (profile.client_id === 'good' && profile.client_secret === 'good') {
        const expiresIn = 3600;
        return {
          status: 'ok',
          access_token: 'eyJ.mock.token',
          token_type: 'Bearer',
          expires_in: expiresIn,
          expires_at: new Date(now.getTime() + expiresIn * 1000).toISOString(),
        };
      }
      return {
        status: 'invalid_client',
        message: 'invalid_client: bad credentials',
      };

    case 'bearer_static':
      if ((profile.bearer_token?.length || 0) > 10) {
        const expiresIn = 7200;
        return {
          status: 'ok',
          access_token: profile.bearer_token!,
          token_type: 'Bearer',
          expires_in: expiresIn,
          expires_at: new Date(now.getTime() + expiresIn * 1000).toISOString(),
        };
      }
      return {
        status: 'invalid_token',
        message: 'Token too short',
      };

    case 'api_key_header':
      if ((profile.api_key?.length || 0) > 6) {
        const expiresIn = 3600;
        return {
          status: 'ok',
          access_token: 'api-key-used',
          token_type: 'ApiKey',
          expires_in: expiresIn,
          expires_at: new Date(now.getTime() + expiresIn * 1000).toISOString(),
        };
      }
      return {
        status: 'invalid_key',
        message: 'API key too short',
      };

    case 'session_cookie':
      if (profile.cookie_value?.includes('sid=')) {
        const expiresIn = 3600;
        return {
          status: 'ok',
          access_token: 'cookie-session',
          token_type: 'Cookie',
          expires_in: expiresIn,
          expires_at: new Date(now.getTime() + expiresIn * 1000).toISOString(),
        };
      }
      return {
        status: 'invalid_cookie',
        message: 'Missing sid=',
      };

    default:
      return {
        status: 'unsupported_grant_type',
        message: 'Flow not supported in demo',
      };
  }
}

export function getInjectionPreview(
  profile: AuthProfile,
  tokenResult?: TokenResult
): string {
  switch (profile.type) {
    case 'oauth2_client_credentials':
    case 'bearer_static': {
      const token = tokenResult?.access_token || '<access_token>';
      return `Authorization: Bearer ${token}`;
    }

    case 'api_key_header': {
      const headerName = profile.header_name || 'X-API-Key';
      const apiKey =
        tokenResult?.access_token || profile.api_key || '<api_key>';
      return `${headerName}: ${apiKey}`;
    }

    case 'session_cookie': {
      const cookieValue = profile.cookie_value || '<cookie_value>';
      return `Cookie: ${cookieValue}`;
    }

    default:
      return 'Unsupported auth type';
  }
}

export function validateProfile(profile: AuthProfile): {
  isValid: boolean;
  errors: string[];
} {
  const errors: string[] = [];

  if (!profile.token_url?.trim()) {
    errors.push('Token URL is required');
  } else if (!profile.token_url.match(/^https?:\/\//)) {
    errors.push('Token URL must be a valid HTTP(S) URL');
  }

  switch (profile.type) {
    case 'oauth2_client_credentials':
      // In demo UI client credentials are optional — allow empty client_id/client_secret
      // Server/backend may still require them for real token requests.
      break;

    case 'api_key_header':
      if (!profile.header_name?.trim()) errors.push('Header name is required');
      if (!profile.api_key?.trim()) errors.push('API key is required');
      break;

    case 'bearer_static':
      if (!profile.bearer_token?.trim())
        errors.push('Bearer token is required');
      if ((profile.bearer_token?.length || 0) <= 10) {
        errors.push('Bearer token must be longer than 10 characters');
      }
      break;

    case 'session_cookie':
      if (!profile.cookie_value?.trim())
        errors.push('Cookie value is required');
      if (!profile.cookie_value?.includes('sid=')) {
        errors.push('Cookie value must include "sid="');
      }
      break;
  }

  return {
    isValid: errors.length === 0,
    errors,
  };
}

export function getAuthTypeDisplayName(type: string): string {
  const names: Record<string, string> = {
    oauth2_client_credentials: 'OAuth2 Client Credentials',
    api_key_header: 'API Key Header',
    bearer_static: 'Static Bearer Token',
    session_cookie: 'Session Cookie',
    password: 'Resource Owner Password',
    device_code: 'Device Code',
    auth_code: 'Authorization Code',
  };
  return names[type] || type;
}

export function formatTimestamp(): string {
  return new Date().toLocaleTimeString('en-US', {
    hour12: false,
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
  });
}

export function getErrorMessage(status: string): string {
  const messages: Record<string, string> = {
    invalid_client: 'Check CLIENT_ID and CLIENT_SECRET',
    invalid_key: 'API key too short',
    invalid_token: 'Token too short',
    invalid_cookie: 'Missing sid=',
    network_error: 'Network unreachable (simulated)',
  };
  return messages[status] || status;
}
