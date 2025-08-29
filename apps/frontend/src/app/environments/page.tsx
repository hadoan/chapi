"use client";
import React, {useState} from 'react';
import { EnvProvider, useEnvStore } from '@/lib/state/envStore';
import type { EnvName } from '@/lib/state/types';
import EnvEditDrawer from '@/components/env/EnvEditDrawer';
import { Layout } from '@/components/Layout';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Settings, Lock, Globe, Clock, RefreshCw } from 'lucide-react';

function Inner(){
  const {envs, updateEnv} = useEnvStore();
  const [editing, setEditing] = useState<string | null>(null);

  const env = envs.find(e=> e.name === editing) ?? null;

  const getEnvBadgeVariant = (name: string) => {
    switch(name) {
      case 'local': return 'secondary';
      case 'staging': return 'outline';  
      case 'prod': return 'default';
      default: return 'outline';
    }
  };

  return (
    <Layout showProjectSelector={false}>
      <div className="container mx-auto py-6 space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-3xl font-bold">Environments</h1>
            <p className="text-muted-foreground">Configure base URLs, headers & secrets for different environments</p>
          </div>
        </div>

        <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
          {envs.map((environment) => (
            <Card key={environment.name} className="animate-fade-in hover-scale">
              <CardHeader>
                <div className="flex items-start justify-between">
                  <div className="space-y-1">
                    <CardTitle className="text-lg flex items-center gap-2">
                      {environment.name}
                      {environment.locked && <Lock className="w-4 h-4 text-muted-foreground" />}
                    </CardTitle>
                    <div className="flex items-center gap-2">
                      <Badge variant={getEnvBadgeVariant(environment.name)}>
                        {environment.name}
                      </Badge>
                      <Badge variant="outline" className="text-xs">
                        <Globe className="w-3 h-3 mr-1" />
                        {environment.followRedirects ? 'Redirects' : 'No redirects'}
                      </Badge>
                    </div>
                  </div>
                  <div className="flex gap-1">
                    <Button 
                      variant="ghost" 
                      size="sm"
                      onClick={() => setEditing(environment.name)}
                      disabled={environment.locked}
                    >
                      <Settings className="w-4 h-4" />
                    </Button>
                  </div>
                </div>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="space-y-2">
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-muted-foreground">Base URL</span>
                    <span className="text-xs font-mono">{environment.baseUrl}</span>
                  </div>
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-muted-foreground">Timeout</span>
                    <div className="flex items-center gap-1">
                      <Clock className="w-3 h-3 text-muted-foreground" />
                      <span className="text-xs">{environment.timeoutMs}ms</span>
                    </div>
                  </div>
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-muted-foreground">Headers</span>
                    <Badge variant="outline" className="text-xs">
                      {Object.keys(environment.headers).length} configured
                    </Badge>
                  </div>
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-muted-foreground">Secrets</span>
                    <Badge variant="outline" className="text-xs">
                      {Object.keys(environment.secrets).length} configured
                    </Badge>
                  </div>
                </div>
                
                <div className="flex items-center justify-between text-xs text-muted-foreground">
                  <span>Updated</span>
                  <span>{new Date(environment.updatedAt).toLocaleDateString()}</span>
                </div>
                
                <div className="flex gap-2">
                  <Button 
                    onClick={() => setEditing(environment.name)}
                    disabled={environment.locked}
                    className="flex-1"
                    variant={environment.locked ? "outline" : "default"}
                  >
                    <Settings className="w-4 h-4 mr-2" />
                    {environment.locked ? 'Locked' : 'Configure'}
                  </Button>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>

        <EnvEditDrawer env={env} open={!!editing} onClose={()=> setEditing(null)} onSave={(patch)=>{ if(env) updateEnv(env.name as EnvName, patch); }} />
      </div>
    </Layout>
  );
}

export default function Page(){
  return (
    <EnvProvider>
      <Inner />
    </EnvProvider>
  );
}
