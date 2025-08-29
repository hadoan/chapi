"use client";
import React, {useState} from 'react';
import { EnvProvider, useEnvStore } from '@/lib/state/envStore';
import type { EnvName } from '@/lib/state/types';
import EnvEditDrawer from '@/components/env/EnvEditDrawer';
import { Layout } from '@/components/Layout';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Settings, Lock, Globe, Clock, RefreshCw, Plus } from 'lucide-react';
import { toast } from '@/hooks/use-toast';

function Inner(){
  const {envs, loading, updateEnv, createEnv} = useEnvStore();
  const [editing, setEditing] = useState<string | null>(null);
  const [showCreateDialog, setShowCreateDialog] = useState(false);
  const [newEnvironment, setNewEnvironment] = useState({
    name: '',
    baseUrl: '',
    timeoutMs: 30000,
    followRedirects: true
  });

  const env = envs.find(e=> e.id === editing) ?? null;

  const handleCreateEnvironment = async () => {
    if (!newEnvironment.name.trim() || !newEnvironment.baseUrl.trim()) {
      toast({ title: 'Please fill in all required fields', variant: 'destructive' });
      return;
    }

    try {
      await createEnv({
        name: newEnvironment.name,
        baseUrl: newEnvironment.baseUrl,
        timeoutMs: newEnvironment.timeoutMs,
        followRedirects: newEnvironment.followRedirects,
        headers: {},
        secrets: {},
        locked: false
      });
      
      setNewEnvironment({
        name: '',
        baseUrl: '',
        timeoutMs: 30000,
        followRedirects: true
      });
      setShowCreateDialog(false);
      toast({ title: 'Environment created successfully' });
    } catch (error) {
      console.error('Failed to create environment:', error);
      toast({ title: 'Failed to create environment', variant: 'destructive' });
    }
  };

  const getEnvBadgeVariant = (name: string) => {
    switch(name.toLowerCase()) {
      case 'local': return 'secondary';
      case 'staging': return 'outline';  
      case 'prod':
      case 'production': return 'default';
      default: return 'outline';
    }
  };

  if (loading) {
    return (
      <Layout showProjectSelector={false}>
        <div className="container mx-auto py-6 space-y-6">
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-3xl font-bold">Environments</h1>
              <p className="text-muted-foreground">Configure base URLs, headers & secrets for different environments</p>
            </div>
            <Button disabled>
              <Plus className="w-4 h-4 mr-2" />
              Create Environment
            </Button>
          </div>
          <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
            {[1, 2, 3].map((i) => (
              <Card key={i} className="animate-pulse">
                <CardHeader>
                  <div className="h-4 bg-muted rounded w-3/4"></div>
                  <div className="h-3 bg-muted rounded w-1/2 mt-2"></div>
                </CardHeader>
                <CardContent>
                  <div className="space-y-2">
                    <div className="h-3 bg-muted rounded"></div>
                    <div className="h-3 bg-muted rounded w-2/3"></div>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        </div>
      </Layout>
    );
  }

  return (
    <Layout showProjectSelector={false}>
      <div className="container mx-auto py-6 space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-3xl font-bold">Environments</h1>
            <p className="text-muted-foreground">Configure base URLs, headers & secrets for different environments</p>
          </div>
          
          <Dialog open={showCreateDialog} onOpenChange={setShowCreateDialog}>
            <DialogTrigger asChild>
              <Button>
                <Plus className="w-4 h-4 mr-2" />
                Create Environment
              </Button>
            </DialogTrigger>
            <DialogContent>
              <DialogHeader>
                <DialogTitle>Create New Environment</DialogTitle>
              </DialogHeader>
              <div className="space-y-4">
                <div>
                  <Label htmlFor="env-name">Environment Name</Label>
                  <Input
                    id="env-name"
                    value={newEnvironment.name}
                    onChange={(e) => setNewEnvironment({...newEnvironment, name: e.target.value})}
                    placeholder="e.g., Development, Staging, Production"
                  />
                </div>
                <div>
                  <Label htmlFor="env-baseurl">Base URL</Label>
                  <Input
                    id="env-baseurl"
                    value={newEnvironment.baseUrl}
                    onChange={(e) => setNewEnvironment({...newEnvironment, baseUrl: e.target.value})}
                    placeholder="https://api.example.com"
                  />
                </div>
                <div>
                  <Label htmlFor="env-timeout">Timeout (ms)</Label>
                  <Input
                    id="env-timeout"
                    type="number"
                    value={newEnvironment.timeoutMs}
                    onChange={(e) => setNewEnvironment({...newEnvironment, timeoutMs: parseInt(e.target.value) || 30000})}
                    placeholder="30000"
                  />
                </div>
                <div className="flex items-center space-x-2">
                  <input
                    id="env-redirects"
                    type="checkbox"
                    checked={newEnvironment.followRedirects}
                    onChange={(e) => setNewEnvironment({...newEnvironment, followRedirects: e.target.checked})}
                    className="rounded"
                  />
                  <Label htmlFor="env-redirects">Follow redirects</Label>
                </div>
                <div className="flex gap-2 pt-4">
                  <Button onClick={handleCreateEnvironment} className="flex-1">Create</Button>
                  <Button variant="outline" onClick={() => setShowCreateDialog(false)} className="flex-1">Cancel</Button>
                </div>
              </div>
            </DialogContent>
          </Dialog>
        </div>

        <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
          {envs.map((environment) => (
            <Card key={environment.id} className="animate-fade-in hover-scale">
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
                      onClick={() => setEditing(environment.id)}
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
                  <span>Created</span>
                  <span>{new Date(environment.createdAt).toLocaleDateString()}</span>
                </div>
                
                <div className="flex gap-2">
                  <Button 
                    onClick={() => setEditing(environment.id)}
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

        <EnvEditDrawer 
          env={editing ? envs.find(e => e.id === editing) || null : null} 
          open={!!editing} 
          onClose={() => setEditing(null)} 
          onSave={(patch) => { 
            if (editing) updateEnv(editing, patch); 
          }} 
        />
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
