"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Switch } from "@/components/ui/switch";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Github, CheckCircle } from "lucide-react";
import { toast } from "@/hooks/use-toast";

type Integration = {
  githubConnected: boolean;
  prChecks: boolean;
};

export function GithubIntegrationCard() {
  const [integration, setIntegration] = useState<Integration>({
    githubConnected: false,
    prChecks: false
  });

  const handleConnectGithub = () => {
    // Mock OAuth flow
    setIntegration(prev => ({ ...prev, githubConnected: true }));
    toast({ title: "GitHub connected", description: "Successfully connected to GitHub" });
  };

  const handleDisconnectGithub = () => {
    setIntegration({ githubConnected: false, prChecks: false });
    toast({ title: "GitHub disconnected" });
  };

  const handlePrChecksChange = (enabled: boolean) => {
    setIntegration(prev => ({ ...prev, prChecks: enabled }));
    toast({ title: enabled ? "PR checks enabled" : "PR checks disabled" });
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Github className="w-5 h-5" />
          GitHub Integration
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="flex items-center justify-between">
          <div className="space-y-1">
            <div className="flex items-center gap-2">
              <Label>Connection Status</Label>
              {integration.githubConnected && (
                <CheckCircle className="w-4 h-4 text-green-500" />
              )}
            </div>
            <p className="text-sm text-muted-foreground">
              {integration.githubConnected 
                ? "Connected and ready for PR checks" 
                : "Connect your GitHub account to enable PR checks"}
            </p>
          </div>
          <Badge variant={integration.githubConnected ? 'default' : 'outline'}>
            {integration.githubConnected ? 'Connected' : 'Disconnected'}
          </Badge>
        </div>

        {!integration.githubConnected ? (
          <Button onClick={handleConnectGithub} className="w-full">
            <Github className="w-4 h-4 mr-2" />
            Connect GitHub
          </Button>
        ) : (
          <div className="space-y-4">
            <div className="flex items-center justify-between">
              <div className="space-y-1">
                <Label htmlFor="pr-checks">PR Checks</Label>
                <p className="text-sm text-muted-foreground">
                  Automatically run tests on pull requests
                </p>
              </div>
              <Switch
                id="pr-checks"
                checked={integration.prChecks}
                onCheckedChange={handlePrChecksChange}
              />
            </div>
            
            <Button 
              variant="outline" 
              onClick={handleDisconnectGithub}
              className="w-full"
            >
              Disconnect GitHub
            </Button>
          </div>
        )}
      </CardContent>
    </Card>
  );
}