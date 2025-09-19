import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Progress } from '@/components/ui/progress';
import { cn } from '@/lib/utils';
import type { AuthCandidate } from '@/types/auth-pilot';
import {
  AlertTriangle,
  CheckCircle,
  Globe,
  Key,
  Lock,
  Shield,
  Sparkles,
  Target,
  TrendingUp,
  Zap,
} from 'lucide-react';
import React from 'react';

interface EnhancedDetectionBannerProps {
  detection: {
    endpoint: string;
    source: string;
    confidence: number;
  };
  onUseEndpoint: () => void;
}

export const EnhancedDetectionBanner: React.FC<
  EnhancedDetectionBannerProps
> = ({ detection, onUseEndpoint }) => {
  const getConfidenceColor = (confidence: number) => {
    if (confidence >= 80) return 'text-green-600 dark:text-green-400';
    if (confidence >= 60) return 'text-yellow-600 dark:text-yellow-400';
    return 'text-orange-600 dark:text-orange-400';
  };

  const getConfidenceBg = (confidence: number) => {
    if (confidence >= 80)
      return 'bg-green-100 border-green-200 dark:bg-green-900/20 dark:border-green-700';
    if (confidence >= 60)
      return 'bg-yellow-100 border-yellow-200 dark:bg-yellow-900/20 dark:border-yellow-700';
    return 'bg-orange-100 border-orange-200 dark:bg-orange-900/20 dark:border-orange-700';
  };

  return (
    <Card
      className={cn(
        'border-2 shadow-lg transition-all duration-300 hover:shadow-xl',
        getConfidenceBg(detection.confidence)
      )}
    >
      <CardHeader className="pb-3">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <div className="p-2 bg-white dark:bg-slate-800 rounded-lg shadow-sm">
              <Target className="w-5 h-5 text-purple-600 dark:text-purple-400" />
            </div>
            <div>
              <CardTitle className="text-lg flex items-center gap-2">
                AI Detection Result
                <Sparkles className="w-4 h-4 text-purple-500 dark:text-purple-400" />
              </CardTitle>
              <p className="text-sm text-muted-foreground">
                Found authentication endpoint with high confidence
              </p>
            </div>
          </div>

          <div className="text-right">
            <div className="flex items-center gap-1 mb-1">
              <TrendingUp
                className={cn(
                  'w-4 h-4',
                  getConfidenceColor(detection.confidence)
                )}
              />
              <span
                className={cn(
                  'font-bold text-lg',
                  getConfidenceColor(detection.confidence)
                )}
              >
                {detection.confidence}%
              </span>
            </div>
            <Progress value={detection.confidence} className="w-20 h-2" />
          </div>
        </div>
      </CardHeader>

      <CardContent className="space-y-4">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div className="space-y-2">
            <label className="text-sm font-medium text-foreground">
              Detected Endpoint
            </label>
            <div className="p-3 bg-white dark:bg-slate-800 rounded-lg border font-mono text-sm break-all">
              {detection.endpoint}
            </div>
          </div>

          <div className="space-y-2">
            <label className="text-sm font-medium text-foreground">
              Detection Source
            </label>
            <div className="flex items-center gap-2">
              <Badge variant="secondary" className="bg-white dark:bg-slate-700">
                <Globe className="w-3 h-3 mr-1" />
                {detection.source}
              </Badge>
            </div>
          </div>
        </div>

        <div className="flex items-center justify-between pt-2">
          <div className="flex items-center gap-2 text-sm text-muted-foreground">
            <CheckCircle className="w-4 h-4 text-green-500" />
            Ready to use detected configuration
          </div>

          <Button
            onClick={onUseEndpoint}
            className="bg-gradient-to-r from-purple-600 to-blue-600 hover:from-purple-700 hover:to-blue-700 dark:from-purple-500 dark:to-blue-500 dark:hover:from-purple-600 dark:hover:to-blue-600 text-white"
          >
            <Zap className="w-4 h-4 mr-2" />
            Use Endpoint
          </Button>
        </div>
      </CardContent>
    </Card>
  );
};

interface EnhancedCandidateCardProps {
  candidate: AuthCandidate;
  isSelected: boolean;
  onSelect: () => void;
  index: number;
}

export const EnhancedCandidateCard: React.FC<EnhancedCandidateCardProps> = ({
  candidate,
  isSelected,
  onSelect,
  index,
}) => {
  const getAuthIcon = (type: string) => {
    switch (type) {
      case 'oauth2_client_credentials':
        return Shield;
      case 'api_key_header':
        return Key;
      case 'bearer_static':
        return Lock;
      default:
        return Shield;
    }
  };

  const getConfidenceColor = (confidence: number) => {
    if (confidence >= 80)
      return 'bg-green-100 text-green-700 border-green-200 dark:bg-green-900/30 dark:text-green-300 dark:border-green-700';
    if (confidence >= 60)
      return 'bg-yellow-100 text-yellow-700 border-yellow-200 dark:bg-yellow-900/30 dark:text-yellow-300 dark:border-yellow-700';
    return 'bg-orange-100 text-orange-700 border-orange-200 dark:bg-orange-900/30 dark:text-orange-300 dark:border-orange-700';
  };

  const AuthIcon = getAuthIcon(candidate.type);

  return (
    <Card
      className={cn(
        'cursor-pointer transition-all duration-300 hover:shadow-lg border-2 relative',
        isSelected
          ? 'border-purple-300 bg-gradient-to-br from-purple-50 to-blue-50 dark:border-purple-600 dark:from-purple-950/50 dark:to-blue-950/50 shadow-md'
          : 'border-border hover:border-purple-200 dark:hover:border-purple-700',
        candidate.disabled && 'opacity-50 cursor-not-allowed'
      )}
      onClick={candidate.disabled ? undefined : onSelect}
    >
      <CardHeader className="pb-3">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div
              className={cn(
                'p-2 rounded-lg',
                isSelected
                  ? 'bg-purple-100 dark:bg-purple-900/50'
                  : 'bg-muted dark:bg-slate-800'
              )}
            >
              <AuthIcon
                className={cn(
                  'w-5 h-5',
                  isSelected
                    ? 'text-purple-600 dark:text-purple-400'
                    : 'text-muted-foreground'
                )}
              />
            </div>
            <div>
              <CardTitle className="text-base font-semibold">
                {candidate.type.replace(/_/g, ' ').toUpperCase()}
              </CardTitle>
              {candidate.token_url && (
                <p className="text-xs text-muted-foreground font-mono">
                  {candidate.token_url.length > 40
                    ? `${candidate.token_url.substring(0, 40)}...`
                    : candidate.token_url}
                </p>
              )}
            </div>
          </div>

          <div className="flex items-center gap-2">
            {candidate.confidence && (
              <Badge
                variant="outline"
                className={cn(
                  'px-2 py-1',
                  getConfidenceColor(candidate.confidence)
                )}
              >
                {candidate.confidence}%
              </Badge>
            )}
            <div className="text-xs text-muted-foreground font-mono">
              #{index + 1}
            </div>
          </div>
        </div>
      </CardHeader>

      {(candidate.header_name || candidate.form) && (
        <CardContent className="pt-0">
          <div className="space-y-2">
            {candidate.header_name && (
              <div className="flex items-center gap-2 text-sm">
                <span className="text-muted-foreground">Header:</span>
                <code className="px-2 py-1 bg-muted dark:bg-slate-800 rounded text-xs">
                  {candidate.header_name}
                </code>
              </div>
            )}

            {candidate.form?.grantType && (
              <div className="flex items-center gap-2 text-sm">
                <span className="text-muted-foreground">Grant Type:</span>
                <Badge variant="outline" className="text-xs">
                  {candidate.form.grantType}
                </Badge>
              </div>
            )}
          </div>
        </CardContent>
      )}

      {isSelected && (
        <div className="absolute top-2 right-2">
          <CheckCircle className="w-5 h-5 text-purple-600 dark:text-purple-400" />
        </div>
      )}
    </Card>
  );
};

interface EnhancedCandidateListProps {
  candidates: AuthCandidate[];
  selectedType: string;
  selectedTokenUrl?: string;
  onSelectCandidate: (candidate: AuthCandidate) => void;
}

export const EnhancedCandidateList: React.FC<EnhancedCandidateListProps> = ({
  candidates,
  selectedType,
  selectedTokenUrl,
  onSelectCandidate,
}) => {
  if (!candidates.length) {
    return (
      <Card className="border-dashed border-2 dark:border-slate-700">
        <CardContent className="py-8 text-center">
          <div className="flex flex-col items-center gap-3">
            <div className="p-3 bg-muted dark:bg-slate-800 rounded-full">
              <AlertTriangle className="w-6 h-6 text-muted-foreground" />
            </div>
            <div>
              <p className="font-medium text-muted-foreground">
                No candidates found
              </p>
              <p className="text-sm text-muted-foreground/80">
                Try running AI detection on your authentication code or endpoint
              </p>
            </div>
          </div>
        </CardContent>
      </Card>
    );
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h3 className="text-lg font-semibold flex items-center gap-2">
          <Sparkles className="w-5 h-5 text-purple-600 dark:text-purple-400" />
          Authentication Candidates
        </h3>
        <Badge
          variant="secondary"
          className="bg-purple-100 text-purple-700 dark:bg-purple-900/30 dark:text-purple-300"
        >
          {candidates.length} found
        </Badge>
      </div>

      <div className="grid grid-cols-1 gap-4">
        {candidates.map((candidate, index) => {
          const isSelected =
            candidate.type === selectedType &&
            (!candidate.token_url || candidate.token_url === selectedTokenUrl);

          return (
            <EnhancedCandidateCard
              key={index}
              candidate={candidate}
              isSelected={isSelected}
              onSelect={() => onSelectCandidate(candidate)}
              index={index}
            />
          );
        })}
      </div>
    </div>
  );
};

export default EnhancedCandidateList;
