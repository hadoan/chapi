"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Badge } from "@/components/ui/badge";
import { Copy } from "lucide-react";
import { toast } from "@/hooks/use-toast";

type Artifact = {
  name: string;
  kind: 'req' | 'res';
  body: unknown;
  status?: number;
};

interface ArtifactViewerProps {
  artifacts: Artifact[];
}

export function ArtifactViewer({ artifacts }: ArtifactViewerProps) {
  const [selectedArtifact, setSelectedArtifact] = useState<Artifact | null>(
    artifacts.length > 0 ? artifacts[0] : null
  );

  const handleCopy = (content: string) => {
    navigator.clipboard.writeText(content);
    toast({ title: "Copied to clipboard" });
  };

  const formatJson = (obj: unknown) => {
    return JSON.stringify(obj, null, 2);
  };

  const getStatusColor = (status?: number) => {
    if (!status) return 'outline';
    if (status >= 200 && status < 300) return 'default';
    if (status >= 400) return 'destructive';
    return 'secondary';
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>Test Artifacts</CardTitle>
      </CardHeader>
      <CardContent>
        <div className="grid gap-4 lg:grid-cols-3">
          {/* Artifact List */}
          <div className="space-y-2">
            <h4 className="text-sm font-medium">Requests & Responses</h4>
            <div className="space-y-1">
              {artifacts.map((artifact, idx) => (
                <button
                  key={idx}
                  onClick={() => setSelectedArtifact(artifact)}
                  className={`w-full text-left p-3 rounded border transition-colors ${
                    selectedArtifact === artifact 
                      ? 'border-primary bg-primary/5' 
                      : 'border-border hover:bg-muted/50'
                  }`}
                >
                  <div className="space-y-1">
                    <div className="flex items-center gap-2">
                      <Badge variant="outline" className="text-xs">
                        {artifact.kind.toUpperCase()}
                      </Badge>
                      {artifact.status && (
                        <Badge variant={getStatusColor(artifact.status)} className="text-xs">
                          {artifact.status}
                        </Badge>
                      )}
                    </div>
                    <div className="text-sm font-mono truncate">{artifact.name}</div>
                  </div>
                </button>
              ))}
            </div>
          </div>

          {/* Artifact Content */}
          <div className="lg:col-span-2">
            {selectedArtifact ? (
              <div className="space-y-3">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <h4 className="font-medium">{selectedArtifact.name}</h4>
                    <Badge variant="outline">
                      {selectedArtifact.kind.toUpperCase()}
                    </Badge>
                    {selectedArtifact.status && (
                      <Badge variant={getStatusColor(selectedArtifact.status)}>
                        {selectedArtifact.status}
                      </Badge>
                    )}
                  </div>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => handleCopy(formatJson(selectedArtifact.body))}
                  >
                    <Copy className="w-4 h-4 mr-2" />
                    Copy
                  </Button>
                </div>

                <div className="relative">
                  <pre className="bg-muted/50 p-4 rounded-lg overflow-auto text-sm font-mono max-h-96">
                    {formatJson(selectedArtifact.body)}
                  </pre>
                </div>
              </div>
            ) : (
              <div className="flex items-center justify-center h-64 text-muted-foreground">
                Select an artifact to view its content
              </div>
            )}
          </div>
        </div>
      </CardContent>
    </Card>
  );
}