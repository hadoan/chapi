"use client";

import { useState } from "react";
import { Plus, Settings, Trash2, ExternalLink, Play } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { toast } from "@/hooks/use-toast";
import { useNavigate } from "react-router-dom";
import { Layout } from "@/components/Layout";

type Project = {
  id: string;
  name: string;
  region: 'EU' | 'US';
  repo?: string;
  lastRun?: {
    status: 'pass' | 'fail';
    p95: number;
  };
};

const mockProjects: Project[] = [
  {
    id: 'proj-1',
    name: 'Payment API',
    region: 'EU',
    repo: 'company/payment-api',
    lastRun: { status: 'pass', p95: 245 }
  },
  {
    id: 'proj-2',
    name: 'User Service',
    region: 'US',
    lastRun: { status: 'fail', p95: 567 }
  },
  {
    id: 'proj-3',
    name: 'Analytics Dashboard',
    region: 'EU',
    repo: 'company/analytics-api'
  }
];

export default function ProjectsPage() {
  const navigate = useNavigate();
  const [projects, setProjects] = useState<Project[]>(mockProjects);
  const [showCreateDialog, setShowCreateDialog] = useState(false);
  const [newProject, setNewProject] = useState({
    name: '',
    region: 'EU' as 'EU' | 'US'
  });

  const handleCreateProject = () => {
    if (!newProject.name.trim()) return;
    
    const project: Project = {
      id: `proj-${Date.now()}`,
      name: newProject.name,
      region: newProject.region
    };
    
    setProjects([...projects, project]);
    setNewProject({ name: '', region: 'EU' });
    setShowCreateDialog(false);
    toast({ title: "Project created" });
  };

  const handleDeleteProject = (id: string) => {
    setProjects(projects.filter(p => p.id !== id));
    toast({ title: "Project deleted" });
  };

  return (
    <Layout showProjectSelector={false}>
      <div className="container mx-auto py-6 space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-3xl font-bold">Projects</h1>
            <p className="text-muted-foreground">Manage your API testing projects</p>
          </div>
          
          <Dialog open={showCreateDialog} onOpenChange={setShowCreateDialog}>
            <DialogTrigger asChild>
              <Button>
                <Plus className="w-4 h-4 mr-2" />
                Create Project
              </Button>
            </DialogTrigger>
            <DialogContent>
              <DialogHeader>
                <DialogTitle>Create New Project</DialogTitle>
              </DialogHeader>
              <div className="space-y-4">
                <div>
                  <Label htmlFor="name">Project Name</Label>
                  <Input
                    id="name"
                    value={newProject.name}
                    onChange={(e) => setNewProject({...newProject, name: e.target.value})}
                    placeholder="Enter project name"
                  />
                </div>
                <div>
                  <Label>Region</Label>
                  <Select value={newProject.region} onValueChange={(value: 'EU' | 'US') => setNewProject({...newProject, region: value})}>
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="EU">EU (Europe)</SelectItem>
                      <SelectItem value="US">US (United States)</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
                <div className="flex gap-2 pt-4">
                  <Button onClick={handleCreateProject} className="flex-1">Create</Button>
                  <Button variant="outline" onClick={() => setShowCreateDialog(false)} className="flex-1">Cancel</Button>
                </div>
              </div>
            </DialogContent>
          </Dialog>
        </div>

        <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
          {projects.map((project) => (
            <Card key={project.id} className="animate-fade-in hover-scale">
              <CardHeader>
                <div className="flex items-start justify-between">
                  <div className="space-y-1">
                    <CardTitle className="text-lg">{project.name}</CardTitle>
                    <div className="flex items-center gap-2">
                      <Badge variant="outline">{project.region}</Badge>
                      {project.repo && (
                        <Badge variant="secondary" className="text-xs">
                          {project.repo}
                        </Badge>
                      )}
                    </div>
                  </div>
                  <div className="flex gap-1">
                    <Button 
                      variant="ghost" 
                      size="sm"
                      onClick={() => navigate(`/app/projects/${project.id}/settings`)}
                    >
                      <Settings className="w-4 h-4" />
                    </Button>
                    <Button 
                      variant="ghost" 
                      size="sm"
                      onClick={() => handleDeleteProject(project.id)}
                    >
                      <Trash2 className="w-4 h-4" />
                    </Button>
                  </div>
                </div>
              </CardHeader>
              <CardContent className="space-y-4">
                {project.lastRun && (
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-muted-foreground">Last run</span>
                    <div className="flex items-center gap-2">
                      <Badge variant={project.lastRun.status === 'pass' ? 'default' : 'destructive'}>
                        {project.lastRun.status}
                      </Badge>
                      <span className="text-xs text-muted-foreground">
                        {project.lastRun.p95}ms p95
                      </span>
                    </div>
                  </div>
                )}
                
                <div className="flex gap-2">
                  <Button 
                    onClick={() => navigate(`/app/projects/${project.id}`)}
                    className="flex-1"
                  >
                    <ExternalLink className="w-4 h-4 mr-2" />
                    Open
                  </Button>
                  <Button 
                    variant="outline"
                    onClick={() => navigate("/app")}
                  >
                    <Play className="w-4 h-4 mr-2" />
                    Chat
                  </Button>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      </div>
    </Layout>
  );
}