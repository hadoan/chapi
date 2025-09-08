import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import type { TokenResult } from '@/types/auth-pilot';
import { AlertCircle, CheckCircle, Clock, Key } from 'lucide-react';

interface TokenCachePreviewProps {
  tokenResult?: TokenResult;
}

export function TokenCachePreview({ tokenResult }: TokenCachePreviewProps) {
  const isEmpty = !tokenResult;
  const isValid = tokenResult?.status === 'ok';

  const formatExpiresIn = (seconds: number): string => {
    const minutes = Math.floor(seconds / 60);
    const hours = Math.floor(minutes / 60);

    if (hours > 0) {
      return `${hours}h ${minutes % 60}m`;
    }
    return `${minutes}m`;
  };

  const getExpirationStatus = (
    expiresAt?: string
  ): { status: 'valid' | 'expired' | 'expiring'; message: string } => {
    if (!expiresAt) return { status: 'valid', message: 'No expiration info' };

    const now = new Date();
    const expiry = new Date(expiresAt);
    const diffMs = expiry.getTime() - now.getTime();
    const diffMinutes = Math.floor(diffMs / 60000);

    if (diffMinutes < 0) {
      return { status: 'expired', message: 'Expired' };
    } else if (diffMinutes < 5) {
      return { status: 'expiring', message: `Expires in ${diffMinutes}m` };
    } else {
      return {
        status: 'valid',
        message: `Expires in ${formatExpiresIn(Math.floor(diffMs / 1000))}`,
      };
    }
  };

  if (isEmpty) {
    return (
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base flex items-center gap-2">
            <Key className="h-4 w-4" />
            Token Cache
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="text-center py-8 text-muted-foreground">
            <Key className="h-8 w-8 mx-auto mb-3 opacity-50" />
            <p className="text-sm">No token yet — click Test Connection</p>
          </div>
        </CardContent>
      </Card>
    );
  }

  if (!isValid) {
    return (
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base flex items-center gap-2">
            <Key className="h-4 w-4" />
            Token Cache
            <Badge variant="destructive" className="text-xs">
              Failed
            </Badge>
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="text-center py-8 text-destructive">
            <AlertCircle className="h-8 w-8 mx-auto mb-3" />
            <p className="text-sm font-medium">Test Connection Failed</p>
            <p className="text-xs text-muted-foreground mt-1">
              {tokenResult.message}
            </p>
          </div>
        </CardContent>
      </Card>
    );
  }

  const expiration = getExpirationStatus(tokenResult.expires_at);

  return (
    <Card>
      <CardHeader className="pb-3">
        <CardTitle className="text-base flex items-center gap-2">
          <Key className="h-4 w-4" />
          Token Cache
          <Badge
            variant="secondary"
            className="bg-emerald-500 text-white text-xs"
          >
            Valid
          </Badge>
        </CardTitle>
      </CardHeader>
      <CardContent>
        <div className="space-y-4">
          {/* Status */}
          <div className="flex items-center gap-2 p-3 bg-emerald-50 dark:bg-emerald-950/20 rounded-lg border border-emerald-200 dark:border-emerald-800">
            <CheckCircle className="h-4 w-4 text-emerald-600 dark:text-emerald-400" />
            <span className="text-sm font-medium text-emerald-800 dark:text-emerald-200">
              Token acquired successfully
            </span>
            <Badge
              variant="outline"
              className={`text-xs ml-auto ${
                expiration.status === 'expired'
                  ? 'border-red-500 text-red-700'
                  : expiration.status === 'expiring'
                  ? 'border-amber-500 text-amber-700'
                  : 'border-emerald-500 text-emerald-700'
              }`}
            >
              <Clock className="h-3 w-3 mr-1" />
              {expiration.message}
            </Badge>
          </div>

          {/* Token Details */}
          <div className="bg-muted rounded-lg p-4 font-mono text-sm border">
            <div className="space-y-2 text-foreground">
              <div className="flex">
                <span className="text-primary w-24">access_token:</span>
                <span className="break-all">"{tokenResult.access_token}"</span>
              </div>
              <div className="flex">
                <span className="text-primary w-24">token_type:</span>
                <span>"{tokenResult.token_type}"</span>
              </div>
              <div className="flex">
                <span className="text-primary w-24">expires_in:</span>
                <span>{tokenResult.expires_in}</span>
              </div>
              {tokenResult.expires_at && (
                <div className="flex">
                  <span className="text-primary w-24">expires_at:</span>
                  <span>"{tokenResult.expires_at}"</span>
                </div>
              )}
            </div>
          </div>

          {/* Additional Info */}
          <div className="text-xs text-muted-foreground">
            Token retrieved at {new Date().toLocaleTimeString()}
          </div>
        </div>
      </CardContent>
    </Card>
  );
}
