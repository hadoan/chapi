'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import type { AuthProfile } from '@/types/auth-pilot';
import { Key, Lock, Plus, Settings, Shield, User, Zap } from 'lucide-react';
import { useState } from 'react';

interface AuthProfileListProps {
  profiles: AuthProfile[];
  selectedProfile: AuthProfile | null;
  onSelectProfile: (profile: AuthProfile | null) => void;
  onCreateNew?: () => void;
  loading?: boolean;
}

// Helper function to get auth type display info
const getAuthTypeInfo = (type: AuthProfile['type']) => {
  switch (type) {
    case 'oauth2_client_credentials':
      return {
        icon: Shield,
        label: 'OAuth2 Client',
        color:
          'bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-300',
      };
    case 'password':
      return {
        icon: User,
        label: 'OAuth2 Password',
        color:
          'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-300',
      };
    case 'basic':
      return {
        icon: Lock,
        label: 'Basic Auth',
        color:
          'bg-orange-100 text-orange-800 dark:bg-orange-900/30 dark:text-orange-300',
      };
    case 'api_key_header':
      return {
        icon: Key,
        label: 'API Key',
        color:
          'bg-purple-100 text-purple-800 dark:bg-purple-900/30 dark:text-purple-300',
      };
    case 'bearer_static':
      return {
        icon: Zap,
        label: 'Bearer Token',
        color:
          'bg-teal-100 text-teal-800 dark:bg-teal-900/30 dark:text-teal-300',
      };
    case 'session_cookie':
      return {
        icon: Settings,
        label: 'Session Cookie',
        color: 'bg-yellow-100 text-yellow-800',
      };
    case 'custom_login':
      return {
        icon: Settings,
        label: 'Custom Login',
        color: 'bg-red-100 text-red-800',
      };
    default:
      return {
        icon: Shield,
        label: 'Unknown',
        color: 'bg-gray-100 text-gray-800',
      };
  }
};

export function AuthProfileList({
  profiles,
  selectedProfile,
  onSelectProfile,
  onCreateNew,
  loading = false,
}: AuthProfileListProps) {
  const [hoveredProfile, setHoveredProfile] = useState<string | null>(null);

  return (
    <Card className="h-full">
      <CardHeader className="pb-3">
        <div className="flex items-center justify-between">
          <CardTitle className="text-lg flex items-center gap-2">
            <Shield className="w-5 h-5 text-blue-600 dark:text-blue-400" />
            Auth Profiles
          </CardTitle>
          {onCreateNew && (
            <Button onClick={onCreateNew} size="sm" variant="outline">
              <Plus className="w-4 h-4 mr-1" />
              New
            </Button>
          )}
        </div>
        <p className="text-sm text-muted-foreground">
          {profiles.length} profile{profiles.length !== 1 ? 's' : ''} configured
        </p>
      </CardHeader>
      <CardContent className="p-0">
        {loading ? (
          <div className="p-4 space-y-3">
            {[1, 2, 3].map(i => (
              <div key={i} className="animate-pulse">
                <div className="h-16 bg-muted rounded-lg"></div>
              </div>
            ))}
          </div>
        ) : profiles.length === 0 ? (
          <div className="p-6 text-center space-y-3">
            <div className="w-12 h-12 bg-muted rounded-full flex items-center justify-center mx-auto">
              <Shield className="w-6 h-6 text-muted-foreground" />
            </div>
            <div>
              <p className="font-medium text-muted-foreground">
                No profiles found
              </p>
              <p className="text-sm text-muted-foreground">
                Create your first authentication profile to get started
              </p>
            </div>
            {onCreateNew && (
              <Button onClick={onCreateNew} size="sm" className="mt-3">
                <Plus className="w-4 h-4 mr-1" />
                Create Profile
              </Button>
            )}
          </div>
        ) : (
          <div className="space-y-1 p-2">
            {profiles.map((profile, index) => {
              const typeInfo = getAuthTypeInfo(profile.type);
              const Icon = typeInfo.icon;
              const isSelected = selectedProfile === profile;
              const isHovered = hoveredProfile === profile.token_url;
              const profileName = profile.notes || `${typeInfo.label} Profile`;

              return (
                <div
                  key={`${profile.token_url}-${index}`}
                  className={`p-3 rounded-lg border cursor-pointer transition-all duration-200 ${
                    isSelected
                      ? 'border-blue-500 bg-blue-50 shadow-sm dark:border-blue-400 dark:bg-blue-950/30 dark:shadow-blue-900/20'
                      : isHovered
                      ? 'border-gray-300 bg-gray-50 shadow-sm dark:border-gray-600 dark:bg-gray-800/50 dark:shadow-gray-900/20'
                      : 'border-gray-200 hover:border-gray-300 dark:border-gray-700 dark:hover:border-gray-600 dark:bg-gray-900/20 dark:hover:bg-gray-800/30'
                  }`}
                  onMouseEnter={() => setHoveredProfile(profile.token_url)}
                  onMouseLeave={() => setHoveredProfile(null)}
                  onClick={() => onSelectProfile(profile)}
                >
                  <div className="flex items-start gap-3">
                    <div
                      className={`p-2 rounded-lg ${typeInfo.color} flex-shrink-0`}
                    >
                      <Icon className="w-4 h-4" />
                    </div>
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center justify-between gap-2">
                        <h4 className="font-medium text-sm truncate">
                          {profileName}
                        </h4>
                        {isSelected && (
                          <Badge variant="secondary" className="text-xs">
                            Selected
                          </Badge>
                        )}
                      </div>
                      <p className="text-xs text-muted-foreground truncate mt-1">
                        {profile.token_url || 'No URL configured'}
                      </p>
                      <div className="flex items-center gap-2 mt-2">
                        <Badge variant="outline" className="text-xs">
                          {typeInfo.label}
                        </Badge>
                        {profile.scopes && (
                          <span className="text-xs text-muted-foreground">
                            • {profile.scopes.split(' ').length} scope
                            {profile.scopes.split(' ').length !== 1 ? 's' : ''}
                          </span>
                        )}
                      </div>
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </CardContent>
    </Card>
  );
}
