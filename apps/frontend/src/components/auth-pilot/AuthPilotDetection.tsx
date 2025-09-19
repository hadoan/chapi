import { GradientBorder } from '@/components/auth-pilot/AnimationEffects';
import { EnhancedAiDetection } from '@/components/auth-pilot/EnhancedAiDetection';
import {
  EnhancedCandidateList,
  EnhancedDetectionBanner,
} from '@/components/auth-pilot/EnhancedDetectionVisuals';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import type { DetectionResponse } from '@/lib/api/auth-profiles';
import type { AuthCandidate, AuthProfile } from '@/types/auth-pilot';
import { useEffect } from 'react';

interface AuthPilotDetectionProps {
  profile: AuthProfile;
  candidates: AuthCandidate[];
  bestDetection: {
    endpoint: string;
    source: string;
    confidence: number;
  } | null;
  projectId?: string;
  profilesLoading: boolean;
  onCandidateSelect: (candidate: AuthCandidate) => void;
  onUseDetectedEndpoint: (bestEndpoint?: string) => void;
  onDetectCandidates: () => void;
  onAiDetected: (resp: DetectionResponse) => void;
}

export const AuthPilotDetection = ({
  profile,
  candidates,
  bestDetection,
  projectId,
  profilesLoading,
  onCandidateSelect,
  onUseDetectedEndpoint,
  onDetectCandidates,
  onAiDetected,
}: AuthPilotDetectionProps) => {
  // Debug logging for candidates
  useEffect(() => {
    console.log('🎯 AuthPilotDetection - candidates changed:', candidates);
    console.log(
      '🎯 AuthPilotDetection - candidates length:',
      candidates?.length || 0
    );
    if (candidates && candidates.length > 0) {
      console.log('🎯 AuthPilotDetection - first candidate:', candidates[0]);
    }
  }, [candidates]);

  useEffect(() => {
    console.log(
      '🎯 AuthPilotDetection - bestDetection changed:',
      bestDetection
    );
  }, [bestDetection]);

  console.log(
    '🎯 AuthPilotDetection rendering with candidates:',
    candidates?.length || 0
  );

  return (
    <div className="space-y-6">
      {/* Detection Banner */}
      {bestDetection && (
        <GradientBorder hover>
          <EnhancedDetectionBanner
            detection={bestDetection}
            onUseEndpoint={() => onUseDetectedEndpoint(bestDetection.endpoint)}
          />
        </GradientBorder>
      )}

      {/* Enhanced AI Detection Button */}
      <div className="flex gap-2">
        <EnhancedAiDetection
          projectId={projectId}
          serviceId={undefined}
          variant="prominent"
          className="flex-1"
          onDetected={onAiDetected}
        />
      </div>

      {/* Traditional Auth Detection Card */}
      <Card>
        <CardContent className="pt-6">
          <div className="flex items-center justify-between">
            <div>
              <h3 className="text-sm font-medium">Manual Detection</h3>
              <p className="text-xs text-muted-foreground">
                Analyze endpoint for authentication methods
              </p>
            </div>
            <Button
              onClick={onDetectCandidates}
              disabled={!profile.token_url || profilesLoading}
              variant="outline"
              size="sm"
            >
              {profilesLoading ? 'Detecting...' : 'Detect Auth'}
            </Button>
          </div>
        </CardContent>
      </Card>

      {/* Candidate List */}
      <EnhancedCandidateList
        candidates={candidates}
        selectedType={profile.type}
        // Use header_name as the selected token identifier for API key header profiles
        selectedTokenUrl={
          profile.type === 'api_key_header'
            ? profile.header_name
            : profile.token_url
        }
        onSelectCandidate={onCandidateSelect}
      />
    </div>
  );
};
