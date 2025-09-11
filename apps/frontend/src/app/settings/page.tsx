'use client';

import { Layout } from '@/components/Layout';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Switch } from '@/components/ui/switch';
import { toast } from '@/hooks/use-toast';
import { 
  User, 
  Key, 
  Settings as SettingsIcon, 
  Trash2, 
  Plus, 
  Eye, 
  EyeOff,
  LogOut,
  Camera,
  Moon,
  Sun
} from 'lucide-react';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';

// Mock data following the spec
const MOCK_SETTINGS_DATA = {
  user: {
    id: 'user-123',
    name: 'Jane Developer',
    email: 'jane@example.com',
    avatarUrl: 'https://i.pravatar.cc/150?img=32'
  },
  apiKeys: [
    { id: 'key-1', name: 'Default Key', value: 'sk-1234abcd5678efgh' },
    { id: 'key-2', name: 'CI/CD Pipeline', value: 'sk-9876zyxw4321lmno' }
  ],
  preferences: {
    theme: 'light' as const,
    notifyOnFailure: true
  }
};

interface ApiKey {
  id: string;
  name: string;
  value: string;
}

interface User {
  id: string;
  name: string;
  email: string;
  avatarUrl: string;
}

interface Preferences {
  theme: 'light' | 'dark';
  notifyOnFailure: boolean;
}

function ProfileSection({ user, onUpdateName }: { user: User; onUpdateName: (name: string) => void }) {
  const navigate = useNavigate();
  const [isEditing, setIsEditing] = useState(false);
  const [editName, setEditName] = useState(user.name);

  const handleSaveName = () => {
    onUpdateName(editName);
    setIsEditing(false);
    toast({
      title: 'Profile updated',
      description: 'Your display name has been saved.'
    });
  };

  const handleLogout = () => {
    toast({
      title: 'Logged out (mock)',
      description: 'You have been logged out successfully.'
    });
    navigate('/auth/login');
  };

  return (
    <Card className="bg-card border-border">
      <CardHeader>
        <CardTitle className="flex items-center gap-2 text-foreground">
          <User className="w-5 h-5" />
          Profile
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="flex items-center gap-4">
          <div className="relative">
            <img 
              src={user.avatarUrl} 
              alt="Profile" 
              className="w-16 h-16 rounded-full"
            />
            <Button 
              size="sm"
              variant="outline" 
              className="absolute -bottom-1 -right-1 h-8 w-8 rounded-full p-0 bg-background"
            >
              <Camera className="w-4 h-4" />
            </Button>
          </div>
          <div className="flex-1">
            <div className="space-y-2">
              <div>
                <Label className="text-sm font-medium text-foreground">Display Name</Label>
                {isEditing ? (
                  <div className="flex gap-2 mt-1">
                    <Input
                      value={editName}
                      onChange={(e) => setEditName(e.target.value)}
                      className="flex-1"
                    />
                    <Button size="sm" onClick={handleSaveName}>Save</Button>
                    <Button size="sm" variant="outline" onClick={() => setIsEditing(false)}>Cancel</Button>
                  </div>
                ) : (
                  <div className="flex items-center gap-2 mt-1">
                    <span className="text-foreground">{user.name}</span>
                    <Button 
                      size="sm" 
                      variant="ghost" 
                      onClick={() => setIsEditing(true)}
                      className="h-6 px-2 text-xs"
                    >
                      Edit
                    </Button>
                  </div>
                )}
              </div>
              <div>
                <Label className="text-sm font-medium text-foreground">Email</Label>
                <div className="text-sm text-muted-foreground mt-1">{user.email}</div>
              </div>
            </div>
          </div>
        </div>
        <div className="pt-4 border-t border-border">
          <Button 
            variant="outline" 
            onClick={handleLogout}
            className="text-muted-foreground hover:text-foreground"
          >
            <LogOut className="w-4 h-4 mr-2" />
            Logout
          </Button>
        </div>
      </CardContent>
    </Card>
  );
}

function ApiKeysSection({ apiKeys, onGenerateKey, onRevokeKey }: { 
  apiKeys: ApiKey[]; 
  onGenerateKey: () => void;
  onRevokeKey: (keyId: string) => void;
}) {
  const [showNewKeyModal, setShowNewKeyModal] = useState(false);
  const [revokeKeyId, setRevokeKeyId] = useState<string | null>(null);
  const [visibleKeys, setVisibleKeys] = useState<Set<string>>(new Set());
  const [newGeneratedKey] = useState('sk-newkey1234567890abcdef');

  const maskKey = (key: string) => {
    return `${key.slice(0, 3)}${'•'.repeat(12)}${key.slice(-4)}`;
  };

  const toggleKeyVisibility = (keyId: string) => {
    const newVisible = new Set(visibleKeys);
    if (newVisible.has(keyId)) {
      newVisible.delete(keyId);
    } else {
      newVisible.add(keyId);
    }
    setVisibleKeys(newVisible);
  };

  const handleGenerateKey = () => {
    onGenerateKey();
    setShowNewKeyModal(true);
  };

  const handleRevokeKey = (keyId: string) => {
    onRevokeKey(keyId);
    setRevokeKeyId(null);
    toast({
      title: 'API key revoked',
      description: 'The API key has been permanently revoked.'
    });
  };

  return (
    <>
      <Card className="bg-card border-border">
        <CardHeader>
          <div className="flex items-center justify-between">
            <CardTitle className="flex items-center gap-2 text-foreground">
              <Key className="w-5 h-5" />
              API Keys
            </CardTitle>
            <Button onClick={handleGenerateKey} size="sm" className="bg-indigo-600 hover:bg-indigo-700 text-white">
              <Plus className="w-4 h-4 mr-2" />
              Generate New Key
            </Button>
          </div>
        </CardHeader>
        <CardContent className="space-y-4">
          <p className="text-sm text-muted-foreground">
            API keys allow CLI and automation to access Chapi.
          </p>
          
          <div className="space-y-3">
            {apiKeys.map((key) => (
              <div key={key.id} className="flex items-center gap-3 p-3 border border-border rounded-lg bg-muted/20">
                <div className="flex-1">
                  <div className="font-medium text-foreground text-sm">{key.name}</div>
                  <div className="font-mono text-xs text-muted-foreground">
                    {visibleKeys.has(key.id) ? key.value : maskKey(key.value)}
                  </div>
                </div>
                <Button
                  size="sm"
                  variant="ghost"
                  onClick={() => toggleKeyVisibility(key.id)}
                  className="h-8 w-8 p-0"
                >
                  {visibleKeys.has(key.id) ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                </Button>
                <Button
                  size="sm"
                  variant="outline"
                  onClick={() => setRevokeKeyId(key.id)}
                  className="text-rose-600 border-rose-200 hover:bg-rose-50 hover:text-rose-700 dark:text-rose-400 dark:border-rose-800 dark:hover:bg-rose-900/20"
                >
                  Revoke
                </Button>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>

      {/* New Key Modal */}
      {showNewKeyModal && (
        <div className="fixed inset-0 bg-black/50 dark:bg-black/70 flex items-center justify-center z-50">
          <Card className="w-96 mx-4 bg-background">
            <CardHeader>
              <CardTitle className="text-foreground">New API Key Generated</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <p className="text-sm text-muted-foreground">
                Copy this key now. You won't be able to see it again.
              </p>
              <div className="p-3 bg-muted rounded font-mono text-sm break-all">
                {newGeneratedKey}
              </div>
              <div className="flex justify-end gap-2">
                <Button
                  onClick={() => {
                    navigator.clipboard.writeText(newGeneratedKey);
                    toast({ title: 'Copied to clipboard' });
                  }}
                >
                  Copy Key
                </Button>
                <Button variant="outline" onClick={() => setShowNewKeyModal(false)}>
                  Done
                </Button>
              </div>
            </CardContent>
          </Card>
        </div>
      )}

      {/* Revoke Confirmation Modal */}
      {revokeKeyId && (
        <div className="fixed inset-0 bg-black/50 dark:bg-black/70 flex items-center justify-center z-50">
          <Card className="w-96 mx-4 bg-background">
            <CardHeader>
              <CardTitle className="text-foreground">Revoke API Key?</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <p className="text-sm text-muted-foreground">
                This will permanently revoke the API key. Any applications using this key will stop working.
              </p>
              <div className="flex justify-end gap-2">
                <Button variant="outline" onClick={() => setRevokeKeyId(null)}>
                  Cancel
                </Button>
                <Button 
                  onClick={() => handleRevokeKey(revokeKeyId)}
                  className="bg-rose-600 hover:bg-rose-700 text-white"
                >
                  Revoke Key
                </Button>
              </div>
            </CardContent>
          </Card>
        </div>
      )}
    </>
  );
}

function PreferencesSection({ preferences, onUpdatePreferences }: { 
  preferences: Preferences; 
  onUpdatePreferences: (prefs: Partial<Preferences>) => void;
}) {
  const handleThemeToggle = (checked: boolean) => {
    const newTheme = checked ? 'dark' : 'light';
    onUpdatePreferences({ theme: newTheme });
    toast({
      title: 'Theme updated (mock)',
      description: `Switched to ${newTheme} theme.`
    });
  };

  const handleNotificationToggle = (checked: boolean) => {
    onUpdatePreferences({ notifyOnFailure: checked });
    toast({
      title: 'Preferences updated',
      description: `Email notifications ${checked ? 'enabled' : 'disabled'}.`
    });
  };

  return (
    <Card className="bg-card border-border">
      <CardHeader>
        <CardTitle className="flex items-center gap-2 text-foreground">
          <SettingsIcon className="w-5 h-5" />
          Preferences
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-6">
        <div className="flex items-center justify-between">
          <div className="space-y-1">
            <div className="flex items-center gap-2">
              {preferences.theme === 'light' ? <Sun className="w-4 h-4" /> : <Moon className="w-4 h-4" />}
              <Label className="text-sm font-medium text-foreground">Theme</Label>
            </div>
            <p className="text-xs text-muted-foreground">
              Choose your preferred theme
            </p>
          </div>
          <div className="flex items-center gap-2">
            <span className="text-sm text-muted-foreground">Light</span>
            <Switch
              checked={preferences.theme === 'dark'}
              onCheckedChange={handleThemeToggle}
            />
            <span className="text-sm text-muted-foreground">Dark</span>
          </div>
        </div>

        <div className="flex items-center justify-between">
          <div className="space-y-1">
            <Label className="text-sm font-medium text-foreground">Email Notifications</Label>
            <p className="text-xs text-muted-foreground">
              Get notified when runs fail
            </p>
          </div>
          <Switch
            checked={preferences.notifyOnFailure}
            onCheckedChange={handleNotificationToggle}
          />
        </div>
      </CardContent>
    </Card>
  );
}

function DangerZoneSection() {
  const [showDeleteModal, setShowDeleteModal] = useState(false);

  const handleDeleteAccount = () => {
    setShowDeleteModal(false);
    toast({
      title: 'Account deleted (mock)',
      description: 'Your account would be permanently deleted.',
      variant: 'destructive'
    });
  };

  return (
    <>
      <Card className="bg-card border-rose-200 dark:border-rose-800">
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-rose-600 dark:text-rose-400">
            <Trash2 className="w-5 h-5" />
            Danger Zone
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-center justify-between">
            <div>
              <div className="text-sm font-medium text-foreground">Delete Account</div>
              <div className="text-xs text-muted-foreground">
                Permanently remove your account and all data
              </div>
            </div>
            <Button 
              onClick={() => setShowDeleteModal(true)}
              className="bg-rose-600 hover:bg-rose-700 text-white"
            >
              Delete Account
            </Button>
          </div>
        </CardContent>
      </Card>

      {/* Delete Confirmation Modal */}
      {showDeleteModal && (
        <div className="fixed inset-0 bg-black/50 dark:bg-black/70 flex items-center justify-center z-50">
          <Card className="w-96 mx-4 bg-background">
            <CardHeader>
              <CardTitle className="text-rose-600 dark:text-rose-400">Delete Account?</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <p className="text-sm text-muted-foreground">
                This action cannot be undone. This will permanently delete your account and remove all data.
              </p>
              <div className="flex justify-end gap-2">
                <Button variant="outline" onClick={() => setShowDeleteModal(false)}>
                  Cancel
                </Button>
                <Button 
                  onClick={handleDeleteAccount}
                  className="bg-rose-600 hover:bg-rose-700 text-white"
                >
                  Delete Account
                </Button>
              </div>
            </CardContent>
          </Card>
        </div>
      )}
    </>
  );
}

export default function SettingsPage() {
  const [user, setUser] = useState(MOCK_SETTINGS_DATA.user);
  const [apiKeys, setApiKeys] = useState<ApiKey[]>(MOCK_SETTINGS_DATA.apiKeys);
  const [preferences, setPreferences] = useState<Preferences>(MOCK_SETTINGS_DATA.preferences);

  const handleUpdateName = (name: string) => {
    setUser(prev => ({ ...prev, name }));
  };

  const handleGenerateKey = () => {
    const newKey: ApiKey = {
      id: `key-${Date.now()}`,
      name: 'Generated Key',
      value: 'sk-newkey1234567890abcdef'
    };
    setApiKeys(prev => [...prev, newKey]);
  };

  const handleRevokeKey = (keyId: string) => {
    setApiKeys(prev => prev.filter(key => key.id !== keyId));
  };

  const handleUpdatePreferences = (newPrefs: Partial<Preferences>) => {
    setPreferences(prev => ({ ...prev, ...newPrefs }));
  };

  return (
    <Layout showProjectSelector={false}>
      <div className="container mx-auto px-6 py-8 max-w-4xl">
        {/* Header */}
        <div className="mb-8">
          <h1 className="text-3xl font-semibold text-foreground mb-2">
            Settings
          </h1>
          <p className="text-muted-foreground">
            Manage your Chapi account and preferences.
          </p>
        </div>

        {/* Main Content */}
        <div className="space-y-8">
          <ProfileSection user={user} onUpdateName={handleUpdateName} />
          
          <ApiKeysSection 
            apiKeys={apiKeys} 
            onGenerateKey={handleGenerateKey}
            onRevokeKey={handleRevokeKey}
          />
          
          <PreferencesSection 
            preferences={preferences}
            onUpdatePreferences={handleUpdatePreferences}
          />
          
          <DangerZoneSection />
        </div>
      </div>
    </Layout>
  );
}
