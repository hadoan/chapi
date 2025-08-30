"use client";

import { useState, useEffect } from "react";
import { Settings, FileDown, Import, ExternalLink, Play } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { toast } from "@/hooks/use-toast";
import { AuthService } from "@/lib/api/auth-service";
import { apiSpecsApi } from "@/lib/api/apispecs";
import { useNavigate, useParams } from "react-router-dom";
import { Layout } from "@/components/Layout";

interface ProjectOverviewPageProps {
  params: { id: string };
}

interface Project {
  id: string;
  name: string;
  region: string;
  repo?: string | null;
}

const mockProject = {
  id: 'proj-1',
  name: 'Payment API',
  region: 'EU' as const,
  repo: 'company/payment-api'
};

const mockRecentRuns = [
  { id: 'run-1', status: 'pass' as const, p95: 245, timestamp: '2 hours ago' },
  { id: 'run-2', status: 'fail' as const, p95: 567, timestamp: '1 day ago' },
  { id: 'run-3', status: 'pass' as const, p95: 189, timestamp: '2 days ago' },
  { id: 'run-4', status: 'pass' as const, p95: 234, timestamp: '3 days ago' },
  { id: 'run-5', status: 'fail' as const, p95: 678, timestamp: '5 days ago' }
];

export default function ProjectOverviewPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [showRunPackModal, setShowRunPackModal] = useState(false);

  const [project, setProject] = useState<Project | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!id) return;
    setLoading(true);
    AuthService.authenticatedFetch<Project>(`/api/projects/${id}`, { method: 'GET' })
      .then((p) => setProject(p))
      .catch((err) => toast({ title: 'Failed to load project', description: err?.message ?? String(err) }))
      .finally(() => setLoading(false));
  }, [id]);

  const handleImportOpenAPI = () => {
    if (!id) {
      toast({ title: 'Project id missing', description: 'Cannot import without project id' });
      return;
    }

    const url = window.prompt('Enter OpenAPI spec URL (http(s)://...)');
    if (!url) return;

    toast({ title: 'OpenAPI import started', description: 'Importing specification...' });

    apiSpecsApi.importOpenApi(id, { url })
      .then(() => {
        toast({ title: 'Import queued', description: 'OpenAPI spec import started successfully.' });
      })
      .catch((err) => {
        toast({ title: 'Import failed', description: err?.message ?? String(err) });
      });
  };

  const handleDownloadRunPack = () => {
    setShowRunPackModal(true);
  };

  const handleDownloadZip = () => {
    toast({ title: "ZIP downloaded", description: "Run pack downloaded successfully." });
    setShowRunPackModal(false);
  };

  const files = ['tests.json', 'run.sh', 'run.ps1', '.env.example'];

  const displayedProject = project ?? mockProject;

  return (
    <Layout>
      <div className="container mx-auto py-6 space-y-6">
        {/* Header */}
        <div className="flex items-center justify-between">
          <div className="space-y-1">
            <div className="flex items-center gap-3">
              <h1 className="text-3xl font-bold">{loading ? 'Loading...' : displayedProject.name}</h1>
              <Badge variant="outline">{displayedProject.region}</Badge>
            </div>
            {displayedProject?.repo && (
              <p className="text-muted-foreground">{displayedProject.repo}</p>
            )}
          </div>
          
          <div className="flex gap-2">
            <Button onClick={() => navigate("/app")}>
              <Play className="w-4 h-4 mr-2" />
              Open Chat
            </Button>
            <Button 
              variant="outline" 
              onClick={() => navigate(`/app/projects/${id}/settings`)}
            >
              <Settings className="w-4 h-4 mr-2" />
              Settings
            </Button>
          </div>
        </div>

        <div className="grid gap-6 lg:grid-cols-2">
          {/* Quick Links */}
          <Card>
            <CardHeader>
              <CardTitle>Quick Links</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <Button 
                variant="outline" 
                onClick={handleImportOpenAPI}
                className="w-full justify-start"
              >
                <Import className="w-4 h-4 mr-2" />
                Import OpenAPI Spec
              </Button>
              <Button 
                variant="outline" 
                onClick={handleDownloadRunPack}
                className="w-full justify-start"
              >
                <FileDown className="w-4 h-4 mr-2" />
                Download Run Pack
              </Button>
              <Button 
                variant="outline" 
                onClick={() => navigate(`/app/projects/${id}/settings`)}
                className="w-full justify-start"
              >
                <Settings className="w-4 h-4 mr-2" />
                Project Settings
              </Button>
            </CardContent>
          </Card>

          {/* Recent Runs */}
          <Card>
            <CardHeader className="flex flex-row items-center justify-between">
              <CardTitle>Recent Runs</CardTitle>
              <Button 
                variant="outline" 
                size="sm"
                onClick={() => navigate("/app/runs")}
              >
                <ExternalLink className="w-4 h-4 mr-2" />
                View All
              </Button>
            </CardHeader>
            <CardContent>
              <div className="space-y-3">
                {mockRecentRuns.map((run) => (
                  <div key={run.id} className="flex items-center justify-between p-2 rounded border hover-scale">
                    <div className="flex items-center gap-3">
                      <Badge variant={run.status === 'pass' ? 'default' : 'destructive'}>
                        {run.status}
                      </Badge>
                      <span className="text-sm font-mono">{run.p95}ms p95</span>
                    </div>
                    <span className="text-xs text-muted-foreground">{run.timestamp}</span>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Run Pack Modal */}
        <Dialog open={showRunPackModal} onOpenChange={setShowRunPackModal}>
          <DialogContent>
            <DialogHeader>
              <DialogTitle>Download Run Pack</DialogTitle>
            </DialogHeader>
            <div className="space-y-4">
              <p className="text-sm text-muted-foreground">
                This run pack contains everything you need to run tests locally.
              </p>
              <div className="space-y-2">
                <h4 className="text-sm font-medium">Files included:</h4>
                <div className="space-y-1">
                  {files.map((file) => (
                    <div key={file} className="flex items-center gap-2 text-sm font-mono">
                      <div className="w-2 h-2 rounded-full bg-accent"></div>
                      {file}
                    </div>
                  ))}
                </div>
              </div>
              <div className="flex gap-2 pt-4">
                <Button onClick={handleDownloadZip} className="flex-1">
                  <FileDown className="w-4 h-4 mr-2" />
                  Download ZIP
                </Button>
                <Button 
                  variant="outline" 
                  onClick={() => {
                    navigator.clipboard.writeText('NODE_ENV=local\nAPI_URL=http://localhost:3000');
                    toast({ title: ".env example copied" });
                  }}
                  className="flex-1"
                >
                  Copy .env example
                </Button>
              </div>
            </div>
          </DialogContent>
        </Dialog>
      </div>
    </Layout>
  );
}