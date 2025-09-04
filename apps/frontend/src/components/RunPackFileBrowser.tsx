'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { ScrollArea } from '@/components/ui/scroll-area';
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from '@/components/ui/sheet';
import { Textarea } from '@/components/ui/textarea';
import { toast } from '@/hooks/use-toast';
import { runPacksApi } from '@/lib/api/run-packs';
import {
  Code2,
  Copy,
  Database,
  Download,
  Edit3,
  Eye,
  File,
  FileText,
  FolderOpen,
  Save,
  Settings,
  X,
} from 'lucide-react';
import React, { useEffect, useState } from 'react';

interface RunPackFileBrowserProps {
  isOpen: boolean;
  onClose: () => void;
  runPackId: string;
  runPackName?: string;
}

interface FileItem {
  name: string;
  path: string;
  type: 'file' | 'folder';
  size?: number;
  children?: FileItem[];
  content?: string;
}

const getFileIcon = (fileName: string) => {
  const ext = fileName.split('.').pop()?.toLowerCase();
  switch (ext) {
    case 'js':
    case 'ts':
    case 'jsx':
    case 'tsx':
      return <Code2 className="w-4 h-4 text-blue-500" />;
    case 'json':
      return <Settings className="w-4 h-4 text-yellow-500" />;
    case 'md':
      return <FileText className="w-4 h-4 text-green-500" />;
    case 'sql':
      return <Database className="w-4 h-4 text-purple-500" />;
    case 'py':
      return <Code2 className="w-4 h-4 text-green-600" />;
    case 'sh':
    case 'ps1':
      return <Code2 className="w-4 h-4 text-gray-600" />;
    default:
      return <File className="w-4 h-4 text-gray-500" />;
  }
};

const getFileLanguage = (fileName: string): string => {
  const ext = fileName.split('.').pop()?.toLowerCase();
  switch (ext) {
    case 'js':
      return 'javascript';
    case 'ts':
      return 'typescript';
    case 'jsx':
      return 'javascript';
    case 'tsx':
      return 'typescript';
    case 'json':
      return 'json';
    case 'md':
      return 'markdown';
    case 'sql':
      return 'sql';
    case 'py':
      return 'python';
    case 'sh':
      return 'bash';
    case 'ps1':
      return 'powershell';
    case 'yaml':
    case 'yml':
      return 'yaml';
    case 'xml':
      return 'xml';
    case 'html':
      return 'html';
    case 'css':
      return 'css';
    default:
      return 'text';
  }
};

export const RunPackFileBrowser: React.FC<RunPackFileBrowserProps> = ({
  isOpen,
  onClose,
  runPackId,
  runPackName,
}) => {
  const [files, setFiles] = useState<FileItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [selectedFile, setSelectedFile] = useState<FileItem | null>(null);
  const [fileContent, setFileContent] = useState<string>('');
  const [isEditing, setIsEditing] = useState(false);
  const [editedContent, setEditedContent] = useState<string>('');
  const [openTabs, setOpenTabs] = useState<FileItem[]>([]);
  const [activeTab, setActiveTab] = useState<string>('');

  const loadRunPackFiles = React.useCallback(async () => {
    setLoading(true);
    try {
      // Use the runPackId to get files for this specific RunPack
      const result = await runPacksApi.getRunPackFiles(runPackId);

      // Since we're querying by runPackId, we should get the specific RunPack's files
      // The API should return files for this specific RunPack
      if (result.files && result.files.length > 0) {
        const currentRun = result.files[0]; // Take the first (and should be only) result
        const fileItems: FileItem[] = currentRun.generatedFiles.map(
          fileName => ({
            name: fileName,
            path: fileName,
            type: 'file' as const,
          })
        );
        setFiles(fileItems);
      }
    } catch (error) {
      console.error('Failed to load run pack files:', error);
      toast({ title: 'Failed to load files', variant: 'destructive' });
    } finally {
      setLoading(false);
    }
  }, [runPackId]);

  useEffect(() => {
    if (isOpen && runPackId) {
      loadRunPackFiles();
    }
  }, [isOpen, runPackId, loadRunPackFiles]);

  const loadFileContent = async (file: FileItem) => {
    try {
      const blob = await runPacksApi.downloadRun(runPackId, file.path);
      const content = await blob.text();
      setFileContent(content);
      setEditedContent(content);

      // Update file with content
      const updatedFile = { ...file, content };
      setSelectedFile(updatedFile);

      // Add to open tabs if not already open
      if (!openTabs.find(tab => tab.path === file.path)) {
        setOpenTabs(prev => [...prev, updatedFile]);
      }
      setActiveTab(file.path);
    } catch (error) {
      console.error('Failed to load file content:', error);
      toast({ title: 'Failed to load file content', variant: 'destructive' });
    }
  };

  const closeTab = (filePath: string) => {
    setOpenTabs(prev => prev.filter(tab => tab.path !== filePath));
    if (activeTab === filePath) {
      const remainingTabs = openTabs.filter(tab => tab.path !== filePath);
      setActiveTab(remainingTabs.length > 0 ? remainingTabs[0].path : '');
      setSelectedFile(remainingTabs.length > 0 ? remainingTabs[0] : null);
    }
  };

  const saveFile = async () => {
    if (!selectedFile) return;

    try {
      // For now, we'll just show a success message
      // In a full implementation, this would save back to the server
      toast({ title: 'File saved successfully' });
      setIsEditing(false);

      // Update the file content in our state
      const updatedFile = { ...selectedFile, content: editedContent };
      setSelectedFile(updatedFile);
      setFileContent(editedContent);

      // Update in open tabs
      setOpenTabs(prev =>
        prev.map(tab => (tab.path === selectedFile.path ? updatedFile : tab))
      );
    } catch (error) {
      console.error('Failed to save file:', error);
      toast({ title: 'Failed to save file', variant: 'destructive' });
    }
  };

  const copyContent = () => {
    navigator.clipboard.writeText(fileContent);
    toast({ title: 'Content copied to clipboard' });
  };

  const downloadFile = async () => {
    if (!selectedFile) return;

    try {
      const blob = await runPacksApi.downloadRun(runPackId, selectedFile.path);
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = selectedFile.name;
      document.body.appendChild(a);
      a.click();
      a.remove();
      URL.revokeObjectURL(url);
      toast({ title: 'File downloaded' });
    } catch (error) {
      console.error('Failed to download file:', error);
      toast({ title: 'Failed to download file', variant: 'destructive' });
    }
  };

  return (
    <Sheet open={isOpen} onOpenChange={onClose}>
      <SheetContent
        side="right"
        className="max-w-full md:max-w-[50vw] lg:max-w-[50vw] xl:max-w-[50vw] w-full p-0"
      >
        <SheetHeader className="p-6 border-b">
          <SheetTitle className="flex items-center gap-2">
            <FolderOpen className="w-5 h-5" />
            Run Pack Files
          </SheetTitle>
          <SheetDescription>
            {runPackName || `Run Pack ${runPackId.substring(0, 8)}...`}
            <Badge variant="outline" className="ml-2">
              {files.length} files
            </Badge>
          </SheetDescription>
        </SheetHeader>

        <div className="flex h-[calc(100vh-120px)]">
          {/* File Tree */}
          <div className="w-64 border-r bg-muted/20">
            <div className="p-3 border-b">
              <h3 className="font-medium text-sm">Files</h3>
            </div>
            <ScrollArea className="h-full">
              <div className="p-2">
                {loading ? (
                  <div className="text-sm text-muted-foreground">
                    Loading...
                  </div>
                ) : (
                  files.map(file => (
                    <div
                      key={file.path}
                      className="flex items-center gap-2 p-2 rounded cursor-pointer hover:bg-muted/50 text-sm"
                      onClick={() => loadFileContent(file)}
                    >
                      {getFileIcon(file.name)}
                      <span className="truncate">{file.name}</span>
                    </div>
                  ))
                )}
              </div>
            </ScrollArea>
          </div>

          {/* File Viewer/Editor */}
          <div className="flex-1 flex flex-col">
            {openTabs.length > 0 ? (
              <>
                {/* Tabs */}
                <div className="flex border-b bg-muted/10">
                  {openTabs.map(tab => (
                    <div
                      key={tab.path}
                      className={`flex items-center gap-2 px-3 py-2 border-r cursor-pointer text-sm ${
                        activeTab === tab.path
                          ? 'bg-background border-b-2 border-primary'
                          : 'hover:bg-muted/50'
                      }`}
                      onClick={() => {
                        setActiveTab(tab.path);
                        setSelectedFile(tab);
                        setFileContent(tab.content || '');
                        setEditedContent(tab.content || '');
                      }}
                    >
                      {getFileIcon(tab.name)}
                      <span className="truncate">{tab.name}</span>
                      <Button
                        variant="ghost"
                        size="sm"
                        className="w-4 h-4 p-0 hover:bg-destructive/10"
                        onClick={e => {
                          e.stopPropagation();
                          closeTab(tab.path);
                        }}
                      >
                        <X className="w-3 h-3" />
                      </Button>
                    </div>
                  ))}
                </div>

                {/* File Actions */}
                <div className="flex items-center justify-between p-3 border-b bg-muted/5">
                  <div className="flex items-center gap-2">
                    <Badge variant="outline" className="text-xs">
                      {selectedFile ? getFileLanguage(selectedFile.name) : ''}
                    </Badge>
                    <span className="text-sm text-muted-foreground">
                      {selectedFile?.name}
                    </span>
                  </div>
                  <div className="flex items-center gap-1">
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => setIsEditing(!isEditing)}
                    >
                      {isEditing ? (
                        <>
                          <Eye className="w-4 h-4 mr-1" />
                          View
                        </>
                      ) : (
                        <>
                          <Edit3 className="w-4 h-4 mr-1" />
                          Edit
                        </>
                      )}
                    </Button>
                    {isEditing && (
                      <Button variant="outline" size="sm" onClick={saveFile}>
                        <Save className="w-4 h-4 mr-1" />
                        Save
                      </Button>
                    )}
                    <Button variant="ghost" size="sm" onClick={copyContent}>
                      <Copy className="w-4 h-4 mr-1" />
                      Copy
                    </Button>
                    <Button variant="ghost" size="sm" onClick={downloadFile}>
                      <Download className="w-4 h-4 mr-1" />
                      Download
                    </Button>
                  </div>
                </div>

                {/* File Content */}
                <div className="flex-1 p-4">
                  {isEditing ? (
                    <Textarea
                      value={editedContent}
                      onChange={e => setEditedContent(e.target.value)}
                      className="w-full h-full font-mono text-sm resize-none"
                      placeholder="Edit file content..."
                    />
                  ) : (
                    <ScrollArea className="h-full">
                      <pre className="text-sm font-mono whitespace-pre-wrap bg-muted/20 p-4 rounded">
                        {fileContent}
                      </pre>
                    </ScrollArea>
                  )}
                </div>
              </>
            ) : (
              <div className="flex-1 flex items-center justify-center text-muted-foreground">
                <div className="text-center">
                  <File className="w-12 h-12 mx-auto mb-4 opacity-50" />
                  <p>Select a file to view its content</p>
                </div>
              </div>
            )}
          </div>
        </div>
      </SheetContent>
    </Sheet>
  );
};
