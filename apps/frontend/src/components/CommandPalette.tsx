"use client";

import { useState, useEffect } from "react";
import { Dialog, DialogContent } from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { 
  Zap, 
  Plus, 
  Play, 
  Download, 
  FileUp, 
  Terminal, 
  GitPullRequest, 
  HelpCircle,
  Search
} from "lucide-react";

interface Command {
  id: string;
  label: string;
  description: string;
  icon: React.ComponentType<{ className?: string }>;
  command?: string;
  category: 'generate' | 'run' | 'import' | 'explain' | 'misc';
  shortcut?: string;
}

const commands: Command[] = [
  {
    id: 'generate-smoke',
    label: 'Generate Smoke Tests',
    description: 'Create a basic smoke test suite for your API',
    icon: Zap,
    command: '/generate smoke',
    category: 'generate'
  },
  {
    id: 'add-negatives',
    label: 'Add Negative Tests',
    description: 'Add negative test cases for error scenarios',
    icon: Plus,
    command: '/negatives 3',
    category: 'generate'
  },
  {
    id: 'perf-tests',
    label: 'Performance Tests',
    description: 'Generate performance and load tests',
    icon: Zap,
    command: '/perf',
    category: 'generate'
  },
  {
    id: 'run-local',
    label: 'Run Tests Locally',
    description: 'Execute tests in your local environment',
    icon: Play,
    command: '/run local',
    category: 'run'
  },
  {
    id: 'run-cloud',
    label: 'Run Tests in Cloud',
    description: 'Execute tests in cloud environment',
    icon: Play,
    command: '/run cloud',
    category: 'run'
  },
  {
    id: 'import-openapi',
    label: 'Import OpenAPI Spec',
    description: 'Import tests from OpenAPI/Swagger specification',
    icon: FileUp,
    command: '/import openapi',
    category: 'import'
  },
  {
    id: 'import-curl',
    label: 'Import from cURL',
    description: 'Convert cURL commands to tests',
    icon: Terminal,
    command: '/curl',
    category: 'import'
  },
  {
    id: 'download-pack',
    label: 'Download Run Pack',
    description: 'Download executable test package',
    icon: Download,
    command: '/download run-pack',
    category: 'misc'
  },
  {
    id: 'pr-preview',
    label: 'PR Preview',
    description: 'Preview pull request comments and checks',
    icon: GitPullRequest,
    command: '/pr preview',
    category: 'misc'
  },
  {
    id: 'explain',
    label: 'Explain Results',
    description: 'Get detailed explanation of test results',
    icon: HelpCircle,
    command: '/explain',
    category: 'explain'
  }
];

const categoryColors = {
  generate: 'bg-primary/10 text-primary',
  run: 'bg-accent/10 text-accent',
  import: 'bg-yellow-500/10 text-yellow-600',
  explain: 'bg-purple-500/10 text-purple-600',
  misc: 'bg-muted/50 text-muted-foreground'
};

interface CommandPaletteProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onCommandSelect: (command: string) => void;
}

export function CommandPalette({ open, onOpenChange, onCommandSelect }: CommandPaletteProps) {
  const [search, setSearch] = useState("");
  const [selectedIndex, setSelectedIndex] = useState(0);

  const filteredCommands = commands.filter(cmd =>
    cmd.label.toLowerCase().includes(search.toLowerCase()) ||
    cmd.description.toLowerCase().includes(search.toLowerCase()) ||
    cmd.command?.toLowerCase().includes(search.toLowerCase())
  );

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (!open) return;

      switch (e.key) {
        case 'ArrowDown':
          e.preventDefault();
          setSelectedIndex(prev => (prev + 1) % filteredCommands.length);
          break;
        case 'ArrowUp':
          e.preventDefault();
          setSelectedIndex(prev => prev === 0 ? filteredCommands.length - 1 : prev - 1);
          break;
        case 'Enter':
          e.preventDefault();
          if (filteredCommands[selectedIndex]) {
            handleCommandSelect(filteredCommands[selectedIndex]);
          }
          break;
        case 'Escape':
          e.preventDefault();
          onOpenChange(false);
          break;
      }
    };

    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [open, filteredCommands, selectedIndex, onOpenChange]);

  useEffect(() => {
    setSelectedIndex(0);
  }, [search]);

  const handleCommandSelect = (command: Command) => {
    if (command.command) {
      onCommandSelect(command.command);
    }
    onOpenChange(false);
    setSearch("");
  };

  const groupedCommands = filteredCommands.reduce((acc, cmd) => {
    if (!acc[cmd.category]) {
      acc[cmd.category] = [];
    }
    acc[cmd.category].push(cmd);
    return acc;
  }, {} as Record<string, Command[]>);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-2xl p-0">
        <div className="flex items-center border-b px-3">
          <Search className="mr-2 h-4 w-4 shrink-0 opacity-50" />
          <Input
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search functions and commands..."
            className="border-0 shadow-none focus-visible:ring-0 text-sm"
            autoFocus
          />
          <Badge variant="outline" className="ml-auto text-xs">
            Cmd+K
          </Badge>
        </div>

        <div className="max-h-96 overflow-y-auto p-4">
          {filteredCommands.length === 0 ? (
            <div className="py-6 text-center text-sm text-muted-foreground">
              No commands found.
            </div>
          ) : (
            <div className="space-y-4">
              {Object.entries(groupedCommands).map(([category, cmds]) => (
                <div key={category}>
                  <h4 className="mb-2 text-xs font-medium text-muted-foreground uppercase tracking-wider">
                    {category}
                  </h4>
                  <div className="space-y-1">
                    {cmds.map((cmd, index) => {
                      const globalIndex = filteredCommands.indexOf(cmd);
                      return (
                        <Button
                          key={cmd.id}
                          variant="ghost"
                          className={`w-full justify-start h-auto p-3 ${
                            globalIndex === selectedIndex 
                              ? 'bg-accent/50' 
                              : 'hover:bg-accent/30'
                          }`}
                          onClick={() => handleCommandSelect(cmd)}
                        >
                          <div className="flex items-center gap-3 w-full">
                            <div className={`p-1.5 rounded ${categoryColors[cmd.category]}`}>
                              <cmd.icon className="h-3 w-3" />
                            </div>
                            <div className="flex-1 text-left">
                              <div className="font-medium text-sm">{cmd.label}</div>
                              <div className="text-xs text-muted-foreground">
                                {cmd.description}
                              </div>
                            </div>
                            {cmd.command && (
                              <Badge variant="outline" className="text-xs font-mono">
                                {cmd.command}
                              </Badge>
                            )}
                          </div>
                        </Button>
                      );
                    })}
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        <div className="border-t p-3 text-xs text-muted-foreground">
          <div className="flex items-center justify-between">
            <span>Use ↑↓ to navigate, Enter to select, Esc to close</span>
            <span>Type / to start with slash commands</span>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}