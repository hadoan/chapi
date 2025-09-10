'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { toast } from '@/hooks/use-toast';
import { apiSpecsApi, type ApiSpecDto } from '@/lib/api/apispecs';
import {
  AlertCircle,
  CheckCircle2,
  Download,
  ExternalLink,
  FileText,
  Import,
  Plus,
  RefreshCw,
  Trash2,
} from 'lucide-react';
import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';

interface ApiSpecsManagerProps {
  projectId: string;
  projectName: string;
}

export function ApiSpecsManager({
  projectId,
  projectName,
}: ApiSpecsManagerProps) {
  const [specs, setSpecs] = useState<ApiSpecDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [importDialogOpen, setImportDialogOpen] = useState(false);
  const [importUrl, setImportUrl] = useState('');
  const [editingSpec, setEditingSpec] = useState<ApiSpecDto | null>(null);
  const [deletingSpec, setDeletingSpec] = useState<ApiSpecDto | null>(null);
  const [confirmDeleteOpen, setConfirmDeleteOpen] = useState(false);

  const loadSpecs = async () => {
    setLoading(true);
    try {
      const specsData = await apiSpecsApi.listByProject(projectId);
      setSpecs(Array.isArray(specsData) ? specsData : []);
    } catch (error) {
      toast({
        title: 'Failed to load specs',
        description: error?.message ?? String(error),
        variant: 'destructive',
      });
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadSpecs();
  }, [projectId]); // eslint-disable-line react-hooks/exhaustive-deps

  const navigate = useNavigate();

  const handleImportSpec = () => {
    setEditingSpec(null);
    setImportUrl('');
    setImportDialogOpen(true);
  };

  const handleEditSpec = (spec: ApiSpecDto) => {
    setEditingSpec(spec);
    setImportUrl(spec.sourceUrl || '');
    setImportDialogOpen(true);
  };

  const confirmImport = async () => {
    if (!importUrl.trim()) {
      toast({
        title: 'No URL provided',
        description: 'Please enter an OpenAPI spec URL.',
        variant: 'destructive',
      });
      return;
    }

    try {
      const actionText = editingSpec ? 'Updating' : 'Importing';
      toast({
        title: `${actionText} OpenAPI spec`,
        description: `${actionText} specification...`,
      });

      // Note: The current backend doesn't support updating existing specs,
      // so this will create a new spec even when "editing"
      const created = await apiSpecsApi.importOpenApi(projectId, {
        url: importUrl,
      });

      toast({
        title: `${actionText} successful`,
        description: `OpenAPI spec ${actionText.toLowerCase()} completed successfully.`,
      });

      setImportDialogOpen(false);
      setImportUrl('');
      setEditingSpec(null);
      await loadSpecs();
      // Redirect to endpoints view with the newly imported spec selected
      if (created?.id) {
        navigate(`/app/projects/${projectId}/endpoints?specId=${created.id}`);
      }
    } catch (error) {
      toast({
        title: 'Import failed',
        description: error?.message ?? String(error),
        variant: 'destructive',
      });
    }
  };

  const handleRefreshSpecs = () => {
    loadSpecs();
  };

  const formatDate = (dateString: string | undefined) => {
    if (!dateString) return 'Unknown';
    return new Date(dateString).toLocaleString();
  };

  const getSpecStatus = (spec: ApiSpecDto) => {
    // Since we don't have status info from backend, we'll use creation time
    const now = new Date();
    const created = spec.createdAt ? new Date(spec.createdAt) : now;
    const hoursSinceCreated =
      (now.getTime() - created.getTime()) / (1000 * 60 * 60);

    if (hoursSinceCreated < 1) {
      return { status: 'active', label: 'Active', color: 'default' as const };
    } else if (hoursSinceCreated < 24) {
      return { status: 'recent', label: 'Recent', color: 'secondary' as const };
    } else {
      return { status: 'older', label: 'Older', color: 'outline' as const };
    }
  };

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-4">
        <CardTitle className="text-xl">API Specifications</CardTitle>
        <div className="flex gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={handleRefreshSpecs}
            disabled={loading}
          >
            <RefreshCw
              className={`w-4 h-4 mr-2 ${loading ? 'animate-spin' : ''}`}
            />
            Refresh
          </Button>
          <Button onClick={handleImportSpec}>
            <Plus className="w-4 h-4 mr-2" />
            Import Spec
          </Button>
        </div>
      </CardHeader>

      <CardContent>
        {loading ? (
          <div className="flex items-center justify-center py-8">
            <RefreshCw className="w-6 h-6 animate-spin mr-2" />
            Loading specifications...
          </div>
        ) : specs.length === 0 ? (
          <div className="text-center py-8 space-y-4">
            <div className="flex justify-center">
              <FileText className="w-12 h-12 text-muted-foreground" />
            </div>
            <div className="space-y-2">
              <h3 className="text-lg font-medium">No API specifications</h3>
              <p className="text-muted-foreground">
                Import your first OpenAPI specification to get started.
              </p>
            </div>
            <Button onClick={handleImportSpec}>
              <Import className="w-4 h-4 mr-2" />
              Import OpenAPI Spec
            </Button>
          </div>
        ) : (
          <div className="space-y-4">
            <div className="flex items-center justify-between">
              <p className="text-sm text-muted-foreground">
                {specs.length} specification{specs.length !== 1 ? 's' : ''}{' '}
                found
              </p>
            </div>

            <div className="border rounded-lg">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Source URL</TableHead>
                    <TableHead>ID</TableHead>
                    <TableHead>Version</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead>Imported</TableHead>
                    <TableHead className="text-right">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {specs.map(spec => {
                    const status = getSpecStatus(spec);
                    return (
                      <TableRow key={spec.id}>
                        <TableCell className="font-medium">
                          <div className="flex items-center space-x-2">
                            <FileText className="w-4 h-4 text-muted-foreground" />
                            <div className="max-w-sm">
                              <div className="truncate">
                                <button
                                  className="text-left w-full"
                                  onClick={() =>
                                    navigate(
                                      `/app/projects/${projectId}/endpoints?specId=${spec.id}`
                                    )
                                  }
                                >
                                  {spec.sourceUrl || 'Unknown source'}
                                </button>
                              </div>
                              {/* ID column shown in its own table cell */}
                            </div>
                          </div>
                        </TableCell>
                        <TableCell className="font-mono text-sm">
                          {spec.id?.slice(0, 8)}
                        </TableCell>
                        <TableCell>
                          <code className="text-xs bg-muted px-2 py-1 rounded">
                            {spec.version || 'Unknown'}
                          </code>
                        </TableCell>
                        <TableCell>
                          <Badge variant={status.color}>
                            {status.status === 'active' && (
                              <CheckCircle2 className="w-3 h-3 mr-1" />
                            )}
                            {status.status === 'recent' && (
                              <AlertCircle className="w-3 h-3 mr-1" />
                            )}
                            {status.label}
                          </Badge>
                        </TableCell>
                        <TableCell className="text-sm text-muted-foreground">
                          {formatDate(spec.createdAt)}
                        </TableCell>
                        <TableCell className="text-right">
                          <div className="flex items-center justify-end space-x-2">
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => handleEditSpec(spec)}
                              title="Update spec"
                            >
                              <Import className="w-4 h-4" />
                            </Button>
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => {
                                if (spec.sourceUrl) {
                                  window.open(spec.sourceUrl, '_blank');
                                }
                              }}
                              title="View source"
                              disabled={!spec.sourceUrl}
                            >
                              <ExternalLink className="w-4 h-4" />
                            </Button>
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => {
                                // TODO: Implement spec download when backend supports it
                                toast({
                                  title: 'Download coming soon',
                                  description:
                                    'Spec download functionality will be available soon.',
                                });
                              }}
                              title="Download spec"
                            >
                              <Download className="w-4 h-4" />
                            </Button>
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => {
                                // Open confirmation dialog before deletion
                                setDeletingSpec(spec);
                                setConfirmDeleteOpen(true);
                              }}
                              title="Delete spec"
                              className="text-destructive hover:text-destructive"
                            >
                              <Trash2 className="w-4 h-4" />
                            </Button>
                          </div>
                        </TableCell>
                      </TableRow>
                    );
                  })}
                </TableBody>
              </Table>
            </div>
          </div>
        )}
      </CardContent>

      {/* Import/Edit Dialog */}
      <Dialog open={importDialogOpen} onOpenChange={setImportDialogOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>
              {editingSpec
                ? 'Update API Specification'
                : 'Import API Specification'}
            </DialogTitle>
          </DialogHeader>

          <div className="space-y-4">
            <p className="text-sm text-muted-foreground">
              Provide the URL to an OpenAPI (Swagger) JSON or YAML
              specification.
              {editingSpec &&
                ' This will create a new version of the specification.'}
            </p>

            {editingSpec && (
              <div className="p-3 bg-muted rounded-lg space-y-2">
                <h4 className="text-sm font-medium">Current Specification</h4>
                <div className="text-xs text-muted-foreground space-y-1">
                  <div>Source: {editingSpec.sourceUrl || 'Unknown'}</div>
                  <div>Version: {editingSpec.version || 'Unknown'}</div>
                  <div>Imported: {formatDate(editingSpec.createdAt)}</div>
                </div>
              </div>
            )}

            <div className="space-y-2">
              <label className="text-sm font-medium">Specification URL</label>
              <input
                className="w-full rounded-md border px-3 py-2 bg-background text-sm"
                value={importUrl}
                onChange={e => setImportUrl(e.target.value)}
                placeholder="https://api.example.com/openapi.json"
                autoFocus
              />
            </div>

            <div className="flex gap-2 pt-4 justify-end">
              <Button
                variant="outline"
                onClick={() => {
                  setImportDialogOpen(false);
                  setImportUrl('');
                  setEditingSpec(null);
                }}
              >
                Cancel
              </Button>
              <Button onClick={confirmImport}>
                {editingSpec ? 'Update' : 'Import'}
              </Button>
            </div>
          </div>
        </DialogContent>
      </Dialog>

      {/* Confirm Delete Dialog */}
      <Dialog open={confirmDeleteOpen} onOpenChange={setConfirmDeleteOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Delete API Specification</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <p className="text-sm text-muted-foreground">
              Deleting this specification will permanently remove the spec and
              all API endpoints that were created from it in the endpoint
              catalog. This action cannot be undone.
            </p>
            <div className="p-3 bg-muted rounded-lg">
              <div className="text-sm font-medium">Spec to delete</div>
              <div className="text-xs text-muted-foreground">
                {deletingSpec?.sourceUrl || deletingSpec?.id}
              </div>
            </div>
            <div className="flex gap-2 pt-4 justify-end">
              <Button
                variant="outline"
                onClick={() => {
                  setConfirmDeleteOpen(false);
                  setDeletingSpec(null);
                }}
              >
                Cancel
              </Button>
              <Button
                className="text-destructive"
                onClick={async () => {
                  if (!deletingSpec?.id) return;
                  try {
                    await apiSpecsApi.delete(deletingSpec.id);
                    toast({
                      title: 'Deleted',
                      description:
                        'Specification and related endpoints removed.',
                    });
                    setConfirmDeleteOpen(false);
                    setDeletingSpec(null);
                    await loadSpecs();
                  } catch (err) {
                    toast({
                      title: 'Delete failed',
                      description: err?.message ?? String(err),
                      variant: 'destructive',
                    });
                  }
                }}
              >
                Delete
              </Button>
            </div>
          </div>
        </DialogContent>
      </Dialog>
    </Card>
  );
}
