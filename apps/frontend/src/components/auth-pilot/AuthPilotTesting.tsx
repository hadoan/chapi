import { GradientBorder } from '@/components/auth-pilot/AnimationEffects';
import { InjectionPreview } from '@/components/auth-pilot/InjectionPreview';
import { ProfileForm } from '@/components/auth-pilot/ProfileForm';
import { TokenCachePreview } from '@/components/auth-pilot/TokenCachePreview';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import type { AuthProfile, TokenResult } from '@/types/auth-pilot';
import { Save, TestTube } from 'lucide-react';

interface AuthPilotTestingProps {
  profile: AuthProfile;
  tokenResult: TokenResult | undefined;
  validation: { isValid: boolean; errors: string[] };
  canTest: boolean;
  isTestingConnection: boolean;
  onProfileChange: (profile: AuthProfile) => void;
  onTestConnection: () => void;
  onSaveProfile: () => void;
}

export const AuthPilotTesting = ({
  profile,
  tokenResult,
  validation,
  canTest,
  isTestingConnection,
  onProfileChange,
  onTestConnection,
  onSaveProfile,
}: AuthPilotTestingProps) => {
  return (
    <div className="space-y-6">
      {/* Profile Form */}
      <ProfileForm
        profile={profile}
        onChange={onProfileChange}
        errors={validation.errors}
      />

      {/* Injection Preview */}
      <InjectionPreview profile={profile} tokenResult={tokenResult} />

      {/* Token Cache Preview */}
      <TokenCachePreview tokenResult={tokenResult} />

      {/* Actions */}
      <Card>
        <CardContent className="pt-6">
          <div className="flex gap-3 flex-wrap">
            <GradientBorder
              gradient="from-indigo-500 via-purple-500 to-pink-500"
              hover
              className="flex-1"
            >
              <Button
                onClick={onTestConnection}
                disabled={!canTest}
                className="w-full bg-gradient-to-r from-indigo-600 to-purple-600 hover:from-indigo-700 hover:to-purple-700 text-white border-0"
                size="lg"
              >
                <TestTube className="h-4 w-4 mr-2" />
                {isTestingConnection ? 'Testing...' : 'Test Connection'}
              </Button>
            </GradientBorder>

            <Button variant="outline" onClick={onSaveProfile}>
              <Save className="h-4 w-4 mr-2" />
              Save Profile
            </Button>
          </div>

          {!validation.isValid && (
            <p className="text-sm text-slate-500 mt-3">
              Complete required fields to enable testing
            </p>
          )}
        </CardContent>
      </Card>
    </div>
  );
};
