import { AuthProfileList } from '@/components/auth-pilot/AuthProfileList';
import type { AuthProfile } from '@/types/auth-pilot';

interface AuthPilotProfilesProps {
  profiles: AuthProfile[];
  selectedProfile: AuthProfile | null;
  profilesLoading: boolean;
  onSelectProfile: (profile: AuthProfile | null) => void;
  onCreateNew: () => void;
  onDeleteProfile?: (profile: AuthProfile) => void;
}

export const AuthPilotProfiles = ({
  profiles,
  selectedProfile,
  profilesLoading,
  onSelectProfile,
  onCreateNew,
  onDeleteProfile,
}: AuthPilotProfilesProps) => {
  const handleCreateNew = () => {
    onCreateNew();
  };

  return (
    <AuthProfileList
      profiles={profiles}
      selectedProfile={selectedProfile}
      onSelectProfile={onSelectProfile}
      loading={profilesLoading}
      onCreateNew={handleCreateNew}
      onDeleteProfile={onDeleteProfile}
    />
  );
};
