import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { toast } from '@/hooks/use-toast';
import { getInjectionPreview } from '@/lib/auth-pilot';
import type { AuthProfile, TokenResult } from '@/types/auth-pilot';
import { Code, Copy } from 'lucide-react';

interface InjectionPreviewProps {
  profile: AuthProfile;
  tokenResult?: TokenResult;
}

export function InjectionPreview({
  profile,
  tokenResult,
}: InjectionPreviewProps) {
  const preview = getInjectionPreview(profile, tokenResult);
  const isValid = tokenResult?.status === 'ok';

  const copyToClipboard = async () => {
    try {
      await navigator.clipboard.writeText(preview);
      toast({
        title: 'Copied to clipboard',
        description: 'Header injection preview copied successfully',
      });
    } catch (err) {
      toast({
        title: 'Failed to copy',
        description: 'Could not copy to clipboard',
        variant: 'destructive',
      });
    }
  };

  return (
    <Card>
      <CardHeader className="pb-3">
        <div className="flex items-center justify-between">
          <CardTitle className="text-base flex items-center gap-2">
            <Code className="h-4 w-4" />
            Injection Preview
          </CardTitle>
          <div className="flex items-center gap-2">
            {isValid && (
              <Badge
                variant="secondary"
                className="bg-emerald-500 text-white text-xs"
              >
                Live Token
              </Badge>
            )}
            <Button
              variant="ghost"
              size="sm"
              onClick={copyToClipboard}
              className="h-7 px-2"
            >
              <Copy className="h-3 w-3" />
            </Button>
          </div>
        </div>
      </CardHeader>
      <CardContent>
        <div className="bg-muted rounded-lg p-4 font-mono text-sm border">
          <div className="text-muted-foreground">
            <span className="text-primary">// Request header:</span>
          </div>
          <div className="text-foreground mt-1 break-all">{preview}</div>
        </div>

        {!isValid && tokenResult && (
          <div className="mt-3 text-xs text-muted-foreground">
            Preview shows placeholder values. Test connection to see live token.
          </div>
        )}

        {isValid && tokenResult && (
          <div className="mt-3 text-xs text-emerald-600 dark:text-emerald-400">
            ✓ Using live token from successful test connection
          </div>
        )}
      </CardContent>
    </Card>
  );
}
