import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { getAuthTypeDisplayName } from '@/lib/auth-pilot';
import type { AuthProfile } from '@/types/auth-pilot';
import { Eye, EyeOff } from 'lucide-react';
import { useState } from 'react';

interface ProfileFormProps {
  profile: AuthProfile;
  onChange: (profile: AuthProfile) => void;
  errors: string[];
}

export function ProfileForm({ profile, onChange, errors }: ProfileFormProps) {
  const [showSecrets, setShowSecrets] = useState<Record<string, boolean>>({});

  const toggleSecret = (field: string) => {
    setShowSecrets(prev => ({ ...prev, [field]: !prev[field] }));
  };

  const updateProfile = (updates: Partial<AuthProfile>) => {
    onChange({ ...profile, ...updates });
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-lg">
          {getAuthTypeDisplayName(profile.type)} Configuration
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-6">
        {/* Shared Fields */}
        <div className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="token_url">Token URL *</Label>
            <Input
              id="token_url"
              value={profile.token_url}
              onChange={e => updateProfile({ token_url: e.target.value })}
              placeholder="https://api.example.com/connect/token"
              className={
                errors.some(e => e.includes('Token URL'))
                  ? 'border-red-500'
                  : ''
              }
            />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="scopes">Scopes</Label>
              <Input
                id="scopes"
                value={profile.scopes || ''}
                onChange={e => updateProfile({ scopes: e.target.value })}
                placeholder="api.read api.write"
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="audience">Audience</Label>
              <Input
                id="audience"
                value={profile.audience || ''}
                onChange={e => updateProfile({ audience: e.target.value })}
                placeholder="https://api.example.com"
              />
            </div>
          </div>
        </div>

        {/* Type-specific Fields */}
        {profile.type === 'oauth2_client_credentials' && (
          <div className="space-y-4 border-t pt-4">
            <h4 className="font-medium text-foreground">
              OAuth2 Client Credentials
            </h4>
            <div className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="client_id">Client ID</Label>
                <Input
                  id="client_id"
                  value={profile.client_id || ''}
                  onChange={e => updateProfile({ client_id: e.target.value })}
                  placeholder="your-client-id"
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="client_secret">Client Secret</Label>
                <div className="relative">
                  <Input
                    id="client_secret"
                    type={showSecrets.client_secret ? 'text' : 'password'}
                    value={profile.client_secret || ''}
                    onChange={e =>
                      updateProfile({ client_secret: e.target.value })
                    }
                    placeholder="your-client-secret"
                    className={`pr-10`}
                  />
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    className="absolute right-0 top-0 h-full px-3"
                    onClick={() => toggleSecret('client_secret')}
                  >
                    {showSecrets.client_secret ? (
                      <EyeOff className="h-4 w-4" />
                    ) : (
                      <Eye className="h-4 w-4" />
                    )}
                  </Button>
                </div>
              </div>
            </div>
          </div>
        )}

        {profile.type === 'api_key_header' && (
          <div className="space-y-4 border-t pt-4">
            <h4 className="font-medium text-foreground">API Key Header</h4>
            <div className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="header_name">Header Name *</Label>
                <Input
                  id="header_name"
                  value={profile.header_name || 'X-API-Key'}
                  onChange={e => updateProfile({ header_name: e.target.value })}
                  placeholder="X-API-Key"
                  className={
                    errors.some(e => e.includes('Header name'))
                      ? 'border-red-500'
                      : ''
                  }
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="api_key">API Key *</Label>
                <div className="relative">
                  <Input
                    id="api_key"
                    type={showSecrets.api_key ? 'text' : 'password'}
                    value={profile.api_key || ''}
                    onChange={e => updateProfile({ api_key: e.target.value })}
                    placeholder="your-api-key"
                    className={`pr-10 ${
                      errors.some(e => e.includes('API key'))
                        ? 'border-red-500'
                        : ''
                    }`}
                  />
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    className="absolute right-0 top-0 h-full px-3"
                    onClick={() => toggleSecret('api_key')}
                  >
                    {showSecrets.api_key ? (
                      <EyeOff className="h-4 w-4" />
                    ) : (
                      <Eye className="h-4 w-4" />
                    )}
                  </Button>
                </div>
              </div>
            </div>
          </div>
        )}

        {profile.type === 'bearer_static' && (
          <div className="space-y-4 border-t pt-4">
            <h4 className="font-medium text-foreground">Static Bearer Token</h4>
            <div className="space-y-2">
              <Label htmlFor="bearer_token">Bearer Token *</Label>
              <div className="relative">
                <Textarea
                  id="bearer_token"
                  value={profile.bearer_token || ''}
                  onChange={e =>
                    updateProfile({ bearer_token: e.target.value })
                  }
                  placeholder="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
                  rows={3}
                  className={`pr-10 ${
                    errors.some(e => e.includes('Bearer token'))
                      ? 'border-red-500'
                      : ''
                  }`}
                />
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  className="absolute right-2 top-2 h-8 px-2"
                  onClick={() => toggleSecret('bearer_token')}
                >
                  {showSecrets.bearer_token ? (
                    <EyeOff className="h-4 w-4" />
                  ) : (
                    <Eye className="h-4 w-4" />
                  )}
                </Button>
              </div>
            </div>
          </div>
        )}

        {profile.type === 'session_cookie' && (
          <div className="space-y-4 border-t pt-4">
            <h4 className="font-medium text-foreground">Session Cookie</h4>
            <div className="space-y-2">
              <Label htmlFor="cookie_value">Cookie Value *</Label>
              <Textarea
                id="cookie_value"
                value={profile.cookie_value || ''}
                onChange={e => updateProfile({ cookie_value: e.target.value })}
                placeholder="sid=abc123; sessionToken=xyz789"
                rows={3}
                className={
                  errors.some(
                    e => e.includes('Cookie value') || e.includes('sid=')
                  )
                    ? 'border-red-500'
                    : ''
                }
              />
              <p className="text-xs text-muted-foreground">
                Include the full cookie string with sid= parameter
              </p>
            </div>
          </div>
        )}

        {/* Notes */}
        <div className="space-y-2 border-t pt-4">
          <Label htmlFor="notes">Notes</Label>
          <Textarea
            id="notes"
            value={profile.notes || ''}
            onChange={e => updateProfile({ notes: e.target.value })}
            placeholder="Additional notes about this auth configuration..."
            rows={2}
          />
        </div>

        {/* Error Display */}
        {errors.length > 0 && (
          <div className="bg-destructive/10 border border-destructive/20 rounded-md p-3">
            <div className="text-sm text-destructive">
              <ul className="list-disc list-inside space-y-1">
                {errors.map((error, index) => (
                  <li key={index}>{error}</li>
                ))}
              </ul>
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
