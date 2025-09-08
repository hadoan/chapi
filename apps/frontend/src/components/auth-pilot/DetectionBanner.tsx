import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import type { Detection } from '@/types/auth-pilot';
import { CheckCircle, Target } from 'lucide-react';

interface DetectionBannerProps {
  detection: Detection;
  onUseEndpoint: () => void;
}

export function DetectionBanner({
  detection,
  onUseEndpoint,
}: DetectionBannerProps) {
  return (
    <Card className="border-l-4 border-l-emerald-500">
      <CardContent className="p-4">
        <div className="flex items-start justify-between gap-4">
          <div className="flex items-start gap-3">
            <div className="mt-0.5">
              <Target className="h-5 w-5 text-emerald-600" />
            </div>
            <div className="space-y-2">
              <div className="flex items-center gap-2 flex-wrap">
                <span className="font-medium text-foreground">
                  Token endpoint found:
                </span>
                <code className="bg-muted px-2 py-1 rounded text-sm font-mono">
                  {detection.endpoint}
                </code>
                <Badge variant="secondary" className="text-xs">
                  Confidence {(detection.confidence * 100).toFixed(0)}%
                </Badge>
                <Badge variant="outline" className="text-xs">
                  Source: {detection.source}
                </Badge>
              </div>
              <p className="text-sm text-muted-foreground">
                Auto-detected OAuth2 token endpoint with heuristic analysis
              </p>
            </div>
          </div>
          <Button
            onClick={onUseEndpoint}
            size="sm"
            className="bg-primary hover:bg-primary/90 text-primary-foreground shrink-0"
          >
            <CheckCircle className="h-4 w-4 mr-2" />
            Use this endpoint
          </Button>
        </div>
      </CardContent>
    </Card>
  );
}
