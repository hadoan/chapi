import { Badge } from '@/components/ui/badge';
import { Card, CardContent } from '@/components/ui/card';
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from '@/components/ui/tooltip';
import { getAuthTypeDisplayName } from '@/lib/auth-pilot';
import type { AuthCandidate, AuthType } from '@/types/auth-pilot';
import { Check, Lock } from 'lucide-react';
import React from 'react';

interface CandidateListProps {
  candidates: AuthCandidate[];
  selectedType: AuthType;
  selectedTokenUrl?: string;
  onSelectCandidate: (candidate: AuthCandidate) => void;
}

export function CandidateList({
  candidates,
  selectedType,
  selectedTokenUrl,
  onSelectCandidate,
}: CandidateListProps) {
  return (
    <div className="space-y-3">
      <h3 className="text-lg font-semibold text-foreground">
        Authentication Candidates
      </h3>
      <div className="space-y-2">
        {candidates.map(candidate => {
          const key = `${candidate.type}|${
            candidate.token_url ?? candidate.header_name ?? ''
          }`;
          const selectedKey = `${selectedType}|${selectedTokenUrl ?? ''}`;
          return (
            <CandidateCard
              key={key}
              candidate={candidate}
              isSelected={key === selectedKey}
              onSelect={() => onSelectCandidate(candidate)}
            />
          );
        })}
      </div>
    </div>
  );
}

interface CandidateCardProps {
  candidate: AuthCandidate;
  isSelected: boolean;
  onSelect: () => void;
}

function CandidateCard({
  candidate,
  isSelected,
  onSelect,
}: CandidateCardProps) {
  const getConfidenceColor = (confidence: number): string => {
    if (confidence >= 0.6) return 'bg-emerald-500';
    if (confidence >= 0.4) return 'bg-amber-500';
    return 'bg-slate-400';
  };

  const CardWrapper = ({ children }: { children: React.ReactNode }) => {
    if (candidate.disabled) {
      return (
        <Tooltip>
          <TooltipTrigger asChild>
            <div className="cursor-not-allowed">{children}</div>
          </TooltipTrigger>
          <TooltipContent>
            <p>{candidate.disabledReason}</p>
          </TooltipContent>
        </Tooltip>
      );
    }

    return (
      <div className="cursor-pointer" onClick={onSelect}>
        {children}
      </div>
    );
  };

  return (
    <CardWrapper>
      <Card
        className={`transition-all ${
          candidate.disabled
            ? 'opacity-50 cursor-not-allowed border-border'
            : isSelected
            ? 'border-primary bg-primary/5 shadow-md'
            : 'hover:border-border hover:shadow-sm border-border'
        }`}
      >
        <CardContent className="p-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              {candidate.disabled ? (
                <Lock className="h-4 w-4 text-muted-foreground" />
              ) : isSelected ? (
                <Check className="h-4 w-4 text-primary" />
              ) : (
                <div className="h-4 w-4" />
              )}
              <div>
                <div className="font-medium text-foreground">
                  {getAuthTypeDisplayName(candidate.type)}
                  {candidate.rawType && (
                    <span className="ml-2 text-xs text-muted-foreground font-mono">
                      {candidate.rawType}
                    </span>
                  )}
                </div>
                {candidate.form?.grantType && (
                  <div className="text-xs text-muted-foreground mt-1">
                    Grant: {candidate.form.grantType}
                  </div>
                )}
                {candidate.token_url && (
                  <div className="text-xs text-muted-foreground font-mono mt-1">
                    {candidate.token_url}
                  </div>
                )}
                {candidate.header_name && (
                  <div className="text-xs text-muted-foreground mt-1">
                    Header: {candidate.header_name}
                  </div>
                )}
              </div>
            </div>
            <Badge
              variant="secondary"
              className={`text-white text-xs ${getConfidenceColor(
                candidate.confidence
              )}`}
            >
              {(candidate.confidence * 100).toFixed(0)}%
            </Badge>
          </div>
        </CardContent>
      </Card>
    </CardWrapper>
  );
}
