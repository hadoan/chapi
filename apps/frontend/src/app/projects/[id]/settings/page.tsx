'use client';

import { EnvEditorDrawer } from '@/components/EnvEditorDrawer';
import { GithubIntegrationCard } from '@/components/GithubIntegrationCard';
import { Layout } from '@/components/Layout';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Label } from '@/components/ui/label';
import { Switch } from '@/components/ui/switch';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { toast } from '@/hooks/use-toast';
import { EnvProvider, useEnvStore } from '@/lib/state/envStore';
import type { EnvModel } from '@/lib/state/types';
import { Edit, Plus, Settings } from 'lucide-react';
import { useState } from 'react';
import { useParams } from 'react-router-dom';

interface ProjectSettingsPageProps {
  params: { id: string };
}

// ...existing code...

export function ProjectSettingsPageInner() {
  const { id } = useParams();
  const { envs, loading, updateEnv, createEnv, deleteEnv } = useEnvStore();
  const [selectedEnvId, setSelectedEnvId] = useState<string | null>(null);
  const [showEnvEditor, setShowEnvEditor] = useState(false);
  const [policies, setPolicies] = useState({
    failOnContractBreak: true,
    artifactRedaction: true,
  });

  const handleEditEnvironment = (envId: string) => {
    setSelectedEnvId(envId);
    setShowEnvEditor(true);
  };

  const handleSaveEnvironment = async (
    env: Partial<EnvModel> & { id?: string }
  ) => {
    // env will match EnvModel shape from the drawer
    if (env.id) {
      await updateEnv(env.id, env);
    } else {
      // Build create payload with required fields; drawer should supply name and baseUrl
      const payload = {
        name: env.name as string,
        baseUrl: env.baseUrl as string,
        timeoutMs: env.timeoutMs ?? 30000,
        followRedirects: env.followRedirects ?? true,
        headers: env.headers ?? {},
      } as Omit<EnvModel, 'id' | 'createdAt'>;

      await createEnv(payload, id ?? undefined);
    }
    setShowEnvEditor(false);
    setSelectedEnvId(null);
  };

  const handlePolicyChange = (key: keyof typeof policies, value: boolean) => {
    setPolicies(prev => ({ ...prev, [key]: value }));
    toast({ title: 'Policy updated' });
  };

  return (
    <Layout showProjectSelector={false}>
      <div className="container mx-auto py-6 space-y-6">
        <div className="flex items-center gap-3">
          <Settings className="w-8 h-8" />
          <div>
            <h1 className="text-3xl font-bold">Project Settings</h1>
            <p className="text-muted-foreground">
              Configure environments, policies, and integrations
            </p>
          </div>
        </div>

        <Tabs defaultValue="environments" className="space-y-6">
          <TabsList>
            <TabsTrigger value="environments">Environments</TabsTrigger>
            <TabsTrigger value="policies">Policies</TabsTrigger>
            <TabsTrigger value="integrations">Integrations</TabsTrigger>
          </TabsList>

          <TabsContent value="environments" className="space-y-4">
            <Card>
              <CardHeader>
                <div className="flex items-center justify-between w-full">
                  <CardTitle>Environment Configuration</CardTitle>
                  <Button onClick={() => setShowEnvEditor(true)}>
                    <Plus className="w-4 h-4 mr-2" />
                    Add Environment
                  </Button>
                </div>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  {loading ? (
                    <div>Loading environments...</div>
                  ) : (
                    envs.map(env => (
                      <div
                        key={env.id}
                        className="flex items-center justify-between p-4 border rounded-lg hover-scale"
                      >
                        <div className="space-y-1">
                          <div className="flex items-center gap-2">
                            <Badge
                              variant={env.locked ? 'destructive' : 'outline'}
                            >
                              {env.name}
                            </Badge>
                            {env.locked && (
                              <span className="text-xs text-muted-foreground">
                                Read-only
                              </span>
                            )}
                          </div>
                          <p className="text-sm font-mono">{env.baseUrl}</p>
                          <p className="text-xs text-muted-foreground">
                            Created {env.createdAt}
                          </p>
                        </div>
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => handleEditEnvironment(env.id)}
                          disabled={env.locked}
                        >
                          <Edit className="w-4 h-4 mr-2" />
                          Edit
                        </Button>
                      </div>
                    ))
                  )}
                </div>
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="policies" className="space-y-4">
            <Card>
              <CardHeader>
                <CardTitle>Testing Policies</CardTitle>
              </CardHeader>
              <CardContent className="space-y-6">
                <div className="flex items-center justify-between">
                  <div className="space-y-1">
                    <Label htmlFor="fail-contract">
                      Fail on contract break
                    </Label>
                    <p className="text-sm text-muted-foreground">
                      Automatically fail tests when API responses don't match
                      the contract
                    </p>
                  </div>
                  <Switch
                    id="fail-contract"
                    checked={policies.failOnContractBreak}
                    onCheckedChange={checked =>
                      handlePolicyChange('failOnContractBreak', checked)
                    }
                  />
                </div>

                <div className="flex items-center justify-between">
                  <div className="space-y-1">
                    <Label htmlFor="artifact-redaction">
                      Artifact redaction
                    </Label>
                    <p className="text-sm text-muted-foreground">
                      Automatically redact sensitive data in test artifacts
                    </p>
                  </div>
                  <Switch
                    id="artifact-redaction"
                    checked={policies.artifactRedaction}
                    onCheckedChange={checked =>
                      handlePolicyChange('artifactRedaction', checked)
                    }
                  />
                </div>

                <div className="pt-4 border-t">
                  <p className="text-sm text-muted-foreground">
                    <strong>Advanced policies coming soon:</strong> YAML editor,
                    custom rules, and more granular controls.
                  </p>
                </div>
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="integrations" className="space-y-4">
            <GithubIntegrationCard />
          </TabsContent>
        </Tabs>

        <EnvEditorDrawer
          environment={
            selectedEnvId
              ? envs.find(e => e.id === selectedEnvId) ?? null
              : null
          }
          open={showEnvEditor}
          onOpenChange={setShowEnvEditor}
          onSave={handleSaveEnvironment}
        />
      </div>
    </Layout>
  );
}

export default function ProjectSettingsPage(props?: ProjectSettingsPageProps) {
  const { id } = useParams();
  return (
    <EnvProvider projectId={id}>
      <ProjectSettingsPageInner />
    </EnvProvider>
  );
}
