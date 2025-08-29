import { useState } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { ScrollArea } from "@/components/ui/scroll-area";
import { GitPullRequest, FileText, Play, Download, Copy, ChevronDown, ChevronRight } from "lucide-react";
import { useState as useToggle } from "react";

const mockDiff = {
  files: [
    { path: "tests/user-service/smoke.json", change: "added", lines: 45 },
    { path: "tests/user-service/auth.json", change: "added", lines: 23 },
    { path: "tests/user-service/crud.json", change: "added", lines: 67 }
  ]
};

const mockRun = {
  id: "run-staging-42",
  env: "staging",
  status: "pass",
  duration: "2.4s",
  p95: 312,
  passed: 12,
  failed: 0
};

const mockArtifacts = [
  {
    name: "POST /users",
    kind: "req",
    body: JSON.stringify({
      name: "John Doe",
      email: "john@example.com"
    }, null, 2),
    status: 201
  },
  {
    name: "POST /users",
    kind: "res", 
    body: JSON.stringify({
      id: "user_123",
      name: "John Doe",
      email: "john@example.com",
      created_at: "2024-08-25T10:30:00Z"
    }, null, 2),
    status: 201
  }
];

const mockPrPreview = `## API Test Results

✅ **12 tests passed** in staging environment

### Generated Tests
- \`tests/user-service/smoke.json\` (+45 lines)
- \`tests/user-service/auth.json\` (+23 lines)  
- \`tests/user-service/crud.json\` (+67 lines)

### Performance
- **Duration:** 2.4s
- **P95 latency:** 312ms
- **Success rate:** 100%

### Actions
- [ ] Review test coverage
- [ ] Approve for production deployment`;

interface RightDrawerProps {
  isOpen: boolean;
  activeTab?: string;
}

export const RightDrawer = ({ isOpen, activeTab = "diff" }: RightDrawerProps) => {
  const [expandedArtifacts, setExpandedArtifacts] = useState<Set<number>>(new Set());

  const toggleArtifact = (index: number) => {
    const newExpanded = new Set(expandedArtifacts);
    if (newExpanded.has(index)) {
      newExpanded.delete(index);
    } else {
      newExpanded.add(index);
    }
    setExpandedArtifacts(newExpanded);
  };

  const copyToClipboard = (text: string) => {
    navigator.clipboard.writeText(text);
  };

  if (!isOpen) return null;

  return (
    <div className="w-80 border-l border-border bg-card h-full">
      <Tabs defaultValue={activeTab} className="h-full flex flex-col">
        <div className="border-b border-border p-4">
          <TabsList className="grid w-full grid-cols-4">
            <TabsTrigger value="diff" className="text-xs">Diff</TabsTrigger>
            <TabsTrigger value="run" className="text-xs">Run</TabsTrigger>
            <TabsTrigger value="artifacts" className="text-xs">Artifacts</TabsTrigger>
            <TabsTrigger value="pr" className="text-xs">PR</TabsTrigger>
          </TabsList>
        </div>

        <div className="flex-1 overflow-hidden">
          <TabsContent value="diff" className="h-full m-0">
            <ScrollArea className="h-full">
              <div className="p-4 space-y-4">
                <div>
                  <h3 className="font-medium mb-3 flex items-center gap-2">
                    <GitPullRequest className="w-4 h-4" />
                    Test Changes
                  </h3>
                  <div className="space-y-2">
                    {mockDiff.files.map((file, idx) => (
                      <div key={idx} className="p-3 border border-border rounded-lg">
                        <div className="flex items-center gap-2 mb-1">
                          <span className="text-green-500 font-mono text-sm">+</span>
                          <span className="font-mono text-sm">{file.path}</span>
                        </div>
                        <Badge variant="outline" className="text-xs">
                          +{file.lines} lines
                        </Badge>
                      </div>
                    ))}
                  </div>
                </div>
              </div>
            </ScrollArea>
          </TabsContent>

          <TabsContent value="run" className="h-full m-0">
            <ScrollArea className="h-full">
              <div className="p-4 space-y-4">
                <div>
                  <h3 className="font-medium mb-3 flex items-center gap-2">
                    <Play className="w-4 h-4" />
                    Last Run Summary
                  </h3>
                  <Card>
                    <CardContent className="p-4">
                      <div className="grid grid-cols-2 gap-4 text-sm">
                        <div>
                          <div className="text-muted-foreground">Status</div>
                          <Badge variant="default">Pass</Badge>
                        </div>
                        <div>
                          <div className="text-muted-foreground">Environment</div>
                          <Badge variant="outline">{mockRun.env}</Badge>
                        </div>
                        <div>
                          <div className="text-muted-foreground">Duration</div>
                          <div className="font-mono">{mockRun.duration}</div>
                        </div>
                        <div>
                          <div className="text-muted-foreground">P95</div>
                          <div className="font-mono">{mockRun.p95}ms</div>
                        </div>
                        <div>
                          <div className="text-muted-foreground">Passed</div>
                          <div className="font-mono text-green-500">{mockRun.passed}</div>
                        </div>
                        <div>
                          <div className="text-muted-foreground">Failed</div>
                          <div className="font-mono text-red-500">{mockRun.failed}</div>
                        </div>
                      </div>
                    </CardContent>
                  </Card>
                </div>
              </div>
            </ScrollArea>
          </TabsContent>

          <TabsContent value="artifacts" className="h-full m-0">
            <ScrollArea className="h-full">
              <div className="p-4 space-y-4">
                <div>
                  <h3 className="font-medium mb-3 flex items-center gap-2">
                    <FileText className="w-4 h-4" />
                    Request/Response
                  </h3>
                  <div className="space-y-3">
                    {mockArtifacts.map((artifact, idx) => (
                      <div key={idx} className="border border-border rounded-lg">
                        <div 
                          className="p-3 cursor-pointer hover:bg-muted/50 transition-colors"
                          onClick={() => toggleArtifact(idx)}
                        >
                          <div className="flex items-center justify-between">
                            <div className="flex items-center gap-2">
                              {expandedArtifacts.has(idx) ? 
                                <ChevronDown className="w-4 h-4" /> : 
                                <ChevronRight className="w-4 h-4" />
                              }
                              <span className="font-mono text-sm">{artifact.name}</span>
                              <Badge variant={artifact.kind === 'req' ? 'outline' : 'secondary'} className="text-xs">
                                {artifact.kind}
                              </Badge>
                            </div>
                            <Button
                              size="sm"
                              variant="ghost"
                              className="h-6 w-6 p-0"
                              onClick={(e) => {
                                e.stopPropagation();
                                copyToClipboard(artifact.body);
                              }}
                            >
                              <Copy className="w-3 h-3" />
                            </Button>
                          </div>
                        </div>
                        {expandedArtifacts.has(idx) && (
                          <div className="px-3 pb-3">
                            <pre className="text-xs font-mono bg-muted p-2 rounded overflow-x-auto">
                              {artifact.body}
                            </pre>
                          </div>
                        )}
                      </div>
                    ))}
                  </div>
                </div>
              </div>
            </ScrollArea>
          </TabsContent>

          <TabsContent value="pr" className="h-full m-0">
            <ScrollArea className="h-full">
              <div className="p-4 space-y-4">
                <div>
                  <h3 className="font-medium mb-3 flex items-center gap-2">
                    <GitPullRequest className="w-4 h-4" />
                    PR Preview
                  </h3>
                  <Card>
                    <CardContent className="p-4">
                      <div className="prose prose-sm max-w-none">
                        <pre className="whitespace-pre-wrap text-xs text-foreground">
                          {mockPrPreview}
                        </pre>
                      </div>
                      <div className="flex gap-2 mt-4">
                        <Button size="sm" className="text-xs">
                          Create PR
                        </Button>
                        <Button size="sm" variant="outline" className="text-xs">
                          <Copy className="w-3 h-3 mr-1" />
                          Copy
                        </Button>
                      </div>
                    </CardContent>
                  </Card>
                </div>
              </div>
            </ScrollArea>
          </TabsContent>
        </div>
      </Tabs>
    </div>
  );
};