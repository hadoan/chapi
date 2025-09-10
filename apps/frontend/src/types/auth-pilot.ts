export type AuthType =
  | 'oauth2_client_credentials'
  | 'api_key_header'
  | 'bearer_static'
  | 'session_cookie'
  | 'password'
  | 'basic'
  | 'custom_login'
  | 'device_code'
  | 'auth_code';

export interface AuthCandidate {
  type: AuthType;
  confidence: number;
  token_url?: string;
  header_name?: string;
  disabled?: boolean;
  disabledReason?: string;
}

export interface AuthProfile {
  type: AuthType;
  token_url: string;
  scopes?: string;
  audience?: string;
  notes?: string;
  // OAuth2 Client Credentials
  client_id?: string;
  client_secret?: string;
  // API Key Header
  header_name?: string;
  api_key?: string;
  // Bearer Static
  bearer_token?: string;
  // Session Cookie
  cookie_value?: string;
  // Username/password secret refs (for password/basic/custom login)
  username_ref?: string;
  password_ref?: string;
  // Custom login options
  login_body_type?: 'json' | 'form';
  login_user_key?: string;
  login_pass_key?: string;
  token_json_path?: string;
}

export interface TokenResult {
  status:
    | 'ok'
    | 'network_error'
    | 'invalid_client'
    | 'invalid_token'
    | 'invalid_key'
    | 'invalid_cookie'
    | 'unsupported_grant_type';
  access_token?: string;
  token_type?: string;
  expires_in?: number;
  expires_at?: string;
  message?: string;
}

export interface Detection {
  endpoint: string;
  source: string;
  confidence: number;
}

export interface LogEntry {
  timestamp: string;
  type: 'test' | 'save' | 'reset' | 'detect';
  message: string;
  status?: 'success' | 'error';
}

export type Environment = 'Dev' | 'Stage' | 'Prod';
