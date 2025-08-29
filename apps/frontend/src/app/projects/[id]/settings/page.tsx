"use client";

import { useState } from "react";
import { useParams } from "react-router-dom";
import { Settings, Edit, Github } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Switch } from "@/components/ui/switch";
import { Label } from "@/components/ui/label";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { toast } from "@/hooks/use-toast";
import { Layout } from "@/components/Layout";
import { EnvEditorDrawer } from "@/components/EnvEditorDrawer";
import { GithubIntegrationCard } from "@/components/GithubIntegrationCard";

interface ProjectSettingsPageProps {
  params: { id: string };
}

type Environment = {
  name: 'local' | 'staging' | 'prod';
  baseUrl: string;
  timeoutMs: number;
  followRedirects: boolean;
  headers: Record<string, string>;
  secrets: Record<string, string>;
  updatedAt: string;
};

const mockEnvironments: Environment[] = [
  {
    name: 'local',
    baseUrl: 'http://localhost:3000',
    timeoutMs: 5000,
    followRedirects: true,
    headers: { 'Content-Type': 'application/json' },
    secrets: { API_KEY: 'local-test-key' },
    updatedAt: '2 hours ago'
  },
  {
    name: 'staging',
    baseUrl: 'https://api-staging.example.com',
    timeoutMs: 10000,
    followRedirects: false,
    headers: { 'Content-Type': 'application/json', 'X-Environment': 'staging' },
    secrets: { API_KEY: '***', DATABASE_URL: '***' },
    updatedAt: '1 day ago'
  },
  {
    name: 'prod',
    baseUrl: 'https://api.example.com',
    timeoutMs: 15000,
    followRedirects: false,
    headers: { 'Content-Type': 'application/json', 'X-Environment': 'production' },
    secrets: { API_KEY: '***', DATABASE_URL: '***' },
    updatedAt: '3 days ago'
  }
];

export default function ProjectSettingsPage() {
  const { id } = useParams();
  const [environments, setEnvironments] = useState<Environment[]>(mockEnvironments);
  const [selectedEnv, setSelectedEnv] = useState<Environment | null>(null);
  const [showEnvEditor, setShowEnvEditor] = useState(false);
  const [policies, setPolicies] = useState({
    failOnContractBreak: true,
    artifactRedaction: true
  });

  const handleEditEnvironment = (env: Environment) => {
    setSelectedEnv(env);
    setShowEnvEditor(true);
  };

  const handleSaveEnvironment = (updatedEnv: Environment) => {
    setEnvironments(envs => 
      envs.map(env => env.name === updatedEnv.name ? updatedEnv : env)
    );
    setShowEnvEditor(false);
    setSelectedEnv(null);
    toast({ title: "Environment updated" });
  };

  const handlePolicyChange = (key: keyof typeof policies, value: boolean) => {
    setPolicies(prev => ({ ...prev, [key]: value }));
    toast({ title: "Policy updated" });
  };

  return (
    <Layout showProjectSelector={false}>
      <div className="container mx-auto py-6 space-y-6">
        <div className="flex items-center gap-3">
          <Settings className="w-8 h-8" />
          <div>
            <h1 className="text-3xl font-bold">Project Settings</h1>
            <p className="text-muted-foreground">Configure environments, policies, and integrations</p>
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
                <CardTitle>Environment Configuration</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  {environments.map((env) => (
                    <div key={env.name} className="flex items-center justify-between p-4 border rounded-lg hover-scale">
                      <div className="space-y-1">
                        <div className="flex items-center gap-2">
                          <Badge variant={env.name === 'prod' ? 'destructive' : 'outline'}>
                            {env.name}
                          </Badge>
                          {env.name === 'prod' && (
                            <span className="text-xs text-muted-foreground">Read-only in MVP</span>
                          )}
                        </div>
                        <p className="text-sm font-mono">{env.baseUrl}</p>
                        <p className="text-xs text-muted-foreground">Last edited {env.updatedAt}</p>
                      </div>
                      <Button 
                        variant="outline" 
                        size="sm"
                        onClick={() => handleEditEnvironment(env)}
                        disabled={env.name === 'prod'}
                      >
                        <Edit className="w-4 h-4 mr-2" />
                        Edit
                      </Button>
                    </div>
                  ))}
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
                    <Label htmlFor="fail-contract">Fail on contract break</Label>
                    <p className="text-sm text-muted-foreground">
                      Automatically fail tests when API responses don't match the contract
                    </p>
                  </div>
                  <Switch
                    id="fail-contract"
                    checked={policies.failOnContractBreak}
                    onCheckedChange={(checked) => handlePolicyChange('failOnContractBreak', checked)}
                  />
                </div>
                
                <div className="flex items-center justify-between">
                  <div className="space-y-1">
                    <Label htmlFor="artifact-redaction">Artifact redaction</Label>
                    <p className="text-sm text-muted-foreground">
                      Automatically redact sensitive data in test artifacts
                    </p>
                  </div>
                  <Switch
                    id="artifact-redaction"
                    checked={policies.artifactRedaction}
                    onCheckedChange={(checked) => handlePolicyChange('artifactRedaction', checked)}
                  />
                </div>

                <div className="pt-4 border-t">
                  <p className="text-sm text-muted-foreground">
                    <strong>Advanced policies coming soon:</strong> YAML editor, custom rules, and more granular controls.
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
          environment={selectedEnv}
          open={showEnvEditor}
          onOpenChange={setShowEnvEditor}
          onSave={handleSaveEnvironment}
        />
      </div>
    </Layout>
  );
}