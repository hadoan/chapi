'use client';

import { AppSidebar } from '@/components/AppSidebar';
import { ChatComposer } from '@/components/ChatComposer';
import {
  ChatMessage,
  MessageButton,
  MessageCard,
  MessageModel,
} from '@/components/ChatMessage';
import { CommandPalette } from '@/components/CommandPalette';
import { HistoryList } from '@/components/HistoryList';
import { RightDrawer } from '@/components/RightDrawer';
import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { SidebarProvider, SidebarTrigger } from '@/components/ui/sidebar';
import { toast } from '@/hooks/use-toast';
import { EnvironmentDto, environmentsApi } from '@/lib/api/environments';
import { llmsApi } from '@/lib/api/llms';
import { ProjectDto, projectsApi } from '@/lib/api/projects';
import { runPacksApi } from '@/lib/api/run-packs';
import type { components } from '@/lib/api/schema';
import mockMessages from '@/lib/mock/messages/chat-1.json';
import {
  ChevronDown,
  LogOut,
  MessageSquare,
  Moon,
  Settings,
  Sun,
  User,
} from 'lucide-react';
import { useEffect, useState } from 'react';

type Card = MessageCard;
type CmdButton = MessageButton;

// Projects and environments will be fetched from the backend

export default function ChatView() {
  const [projects, setProjects] = useState<ProjectDto[]>([]);
  const [selectedProject, setSelectedProject] = useState<ProjectDto | null>(
    null
  );
  const [envOptions, setEnvOptions] = useState<EnvironmentDto[]>([]);
  const [selectedEnv, setSelectedEnv] = useState<string | null>(null);
  const [rightDrawerOpen, setRightDrawerOpen] = useState(true);
  const [darkMode, setDarkMode] = useState(true);
  const [showCommandPalette, setShowCommandPalette] = useState(false);
  const [messages, setMessages] = useState<MessageModel[]>(
    mockMessages as MessageModel[]
  );
  const [downloadingIndex, setDownloadingIndex] = useState<number>(-1);

  type LlmMessage = MessageModel & {
    llmCard?: components['schemas']['Chapi.AI.Dto.ChapiCard'];
  };

  const executeCommand = async (command: string): Promise<MessageModel> => {
    const baseResponse: {
      role: 'assistant';
      content: string;
      cards: Card[];
      buttons: CmdButton[];
    } = {
      role: 'assistant',
      content: '',
      cards: [] as Card[],
      buttons: [] as CmdButton[],
    };

    if (
      command.includes('/generate smoke') ||
      command.includes('generate smoke')
    ) {
      toast({ title: 'Suite generated (mock)' });
      return {
        ...baseResponse,
        content:
          "I'll generate a comprehensive smoke test suite for your API endpoints.",
        cards: [
          {
            type: 'plan',
            title: 'Test Generation Plan',
            items: [
              'Analyze API structure and endpoints',
              'Create basic smoke tests for each endpoint',
              'Add authentication and error handling tests',
              'Generate test data and assertions',
            ],
          },
          {
            type: 'diff',
            title: 'Generated Tests',
            files: [
              {
                path: 'tests/smoke/auth.test.json',
                change: 'added',
                lines: 24,
              },
              {
                path: 'tests/smoke/users.test.json',
                change: 'added',
                lines: 18,
              },
              {
                path: 'tests/smoke/payments.test.json',
                change: 'added',
                lines: 32,
              },
            ],
          },
        ] as Card[],
        buttons: [
          { label: 'Run in Cloud', variant: 'primary' },
          { label: 'Download Run Pack', variant: 'secondary' },
        ] as CmdButton[],
      };
    }

    // If this is a generate command, call the LLM backend to produce a ChapiCard
    if (command.startsWith('/generate')) {
      // Ensure a project is selected
      if (!selectedProject?.id) {
        toast({ title: 'Select a project first' });
        return {
          role: 'assistant',
          content: 'Please select a project before generating tests.',
          cards: [],
          buttons: [],
        };
      }

      // show a quick toast
      toast({ title: 'Generating test plan...' });

      try {
        const req = {
          user_query: command,
          projectId: selectedProject.id,
          max_files: 3,
          openApiJson: null,
        };

        const card = await llmsApi.generate(req);

        // Build a simple assistant response from the returned ChapiCard
        const assistantContent =
          card.heading ??
          (card.plan ? card.plan.join('\n') : undefined) ??
          JSON.stringify(card);

        const diffCard = card.files
          ? ([
              {
                type: 'diff',
                title: card.heading ?? 'Generated Tests',
                files: card.files.map(f => ({
                  path: f.path ?? '',
                  change: 'added' as const,
                  lines: f.addedLines ?? 0,
                })),
              },
            ] as Card[])
          : ([] as Card[]);

        return {
          role: 'assistant',
          content: assistantContent,
          cards: diffCard,
          buttons: [],
        };
      } catch (err) {
        console.error('LLM generate failed', err);
        toast({ title: 'Failed to generate tests' });
        return {
          role: 'assistant',
          content: 'Failed to generate tests. See logs for details.',
          cards: [],
          buttons: [],
        } as MessageModel;
      }
    }

    if (command.includes('/negatives') || command.includes('add 3 negatives')) {
      toast({ title: 'Negative tests added' });
      return {
        ...baseResponse,
        content: 'Added 3 negative test cases to improve error coverage.',
        cards: [
          {
            type: 'diff',
            title: 'Updated Tests',
            files: [
              {
                path: 'tests/negatives/auth-errors.test.json',
                change: 'added',
                lines: 15,
              },
              {
                path: 'tests/negatives/validation.test.json',
                change: 'modified',
                lines: 8,
              },
              {
                path: 'tests/negatives/rate-limits.test.json',
                change: 'added',
                lines: 12,
              },
            ],
          },
        ] as Card[],
      };
    }

    if (command.includes('/run cloud') || command.includes('run in cloud')) {
      toast({ title: 'Cloud run started (EU)' });
      return {
        ...baseResponse,
        content: 'Started test execution in EU cloud environment.',
        cards: [
          {
            type: 'run',
            title: 'Test Run Results',
            status: 'pass',
            env: 'staging',
            duration: '2.4s',
            p95: 312,
            seed: 42,
            passed: 12,
            failed: 0,
          },
        ] as Card[],
        buttons: [
          { label: 'View Artifacts', variant: 'secondary' },
          { label: 'Create PR', variant: 'primary' },
        ] as CmdButton[],
      };
    }

    if (
      command.includes('/download') ||
      command.includes('download run pack')
    ) {
      toast({ title: 'ZIP downloaded' });
      return {
        ...baseResponse,
        content:
          'Generated run pack with all necessary files for local execution.',
        buttons: [
          { label: 'Open File Explorer', variant: 'secondary' },
        ] as CmdButton[],
      };
    }

    if (command.includes('/pr preview')) {
      return {
        ...baseResponse,
        content: "Here's how the PR comment will look:",
        cards: [
          {
            type: 'pr-preview',
            title: 'PR Comment Preview',
            content: `## API Test Results ✅

**Environment:** staging  
**Duration:** 2.4s  
**P95:** 312ms  

### Test Summary
- ✅ 12 tests passed
- ❌ 0 tests failed

All smoke tests are passing. Ready to merge!`,
          },
        ] as Card[],
      };
    }
    // Default response
    return {
      ...baseResponse,
      content: `Executed command: ${command}. This is a mock response.`,
    };
  };

  const handleCommandSelect = async (command: string) => {
    // Simulate command execution
    const assistantResponse = await executeCommand(command);

    const userMessage: MessageModel = { role: 'user', content: command };

    setMessages(prev => [...prev, userMessage, assistantResponse]);

    // Show drawer for certain commands
    if (
      command.includes('generate') ||
      command.includes('run') ||
      command.includes('download')
    ) {
      setRightDrawerOpen(true);
    }
  };

  // Button actions
  const runInCloud = async () => {
    toast({ title: 'Starting run in cloud...' });
    try {
      // Placeholder: call backend run api if available
      // await llmsApi.runInCloud({ /* params */ });
      toast({ title: 'Cloud run started' });
    } catch (err) {
      console.error('Run in cloud failed', err);
      toast({ title: 'Failed to start cloud run' });
    }
  };

  const downloadRunPack = async (messageModel: MessageModel) => {
    // Find the message index to show loading state
    const idx = messages.indexOf(messageModel as MessageModel);
    try {
      toast({ title: 'Preparing run pack...' });
      if (idx >= 0) {
        setDownloadingIndex(idx);
        // Mark the message's buttons as loading
        setMessages(prev => {
          const copy = [...prev];
          const m = { ...copy[idx] } as LlmMessage & {
            buttons?: Array<{
              label: string;
              variant: string;
              loading?: boolean;
            }>;
          };
          m.buttons = [
            { label: 'Downloading...', variant: 'secondary', loading: true },
          ];
          copy[idx] = m;
          return copy;
        });
      }

      // Call run-packs API which returns a blob and runId
      const lm = messageModel as LlmMessage;
      const result = await runPacksApi.generate({
        projectId: selectedProject?.id ?? '',
        card:
          lm.llmCard ??
          (messageModel as unknown as components['schemas']['Chapi.AI.Controllers.RunPackController.GenerateRequest']),
        userQuery: messageModel.content,
        env: selectedEnv ?? 'local',
      });

      // Store runId in the message for future reference
      if (idx >= 0 && result.runId) {
        setMessages(prev => {
          const copy = [...prev];
          const m = { ...copy[idx] } as LlmMessage & { runId?: string };
          m.runId = result.runId;
          copy[idx] = m;
          return copy;
        });
      }

      const url = URL.createObjectURL(result.blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = 'chapi-run-pack.zip';
      document.body.appendChild(a);
      a.click();
      a.remove();
      URL.revokeObjectURL(url);
      toast({
        title: `Run pack downloaded (ID: ${result.runId.substring(0, 8)}...)`,
      });
    } catch (err) {
      console.error('Download failed', err);
      toast({ title: 'Failed to download run pack' });
    } finally {
      if (idx >= 0) {
        setDownloadingIndex(-1);
        // Restore buttons (remove loading)
        setMessages(prev => {
          const copy = [...prev];
          const m = { ...copy[idx] } as LlmMessage & {
            buttons?: Array<{ label: string; variant: string }>;
          };
          // If llmCard exists, restore the original action buttons
          const hasLl = !!m.llmCard;
          m.buttons = hasLl
            ? [
                { label: 'Run in Cloud', variant: 'primary' },
                { label: 'Download Run Pack', variant: 'secondary' },
                { label: 'Add Negatives', variant: 'secondary' },
              ]
            : [];
          copy[idx] = m;
          return copy;
        });
      }
    }
  };

  const addNegatives = async (messageModel: MessageModel) => {
    try {
      toast({ title: 'Adding negative tests...' });
      // Reuse llms.generate with an augmented prompt to ask for negatives
      const req = {
        user_query: `${
          selectedProject?.name ?? ''
        } Add 3 negative tests for the generated suite`,
        projectId: selectedProject?.id ?? '',
        max_files: 3,
        openApiJson: null,
      };
      const negCard = await llmsApi.generate(req);
      // Append a message with the negatives
      const assistantMessage: MessageModel = {
        role: 'assistant',
        content: negCard.heading ?? 'Added negative tests',
        cards: negCard.files
          ? [
              {
                type: 'diff',
                title: negCard.heading ?? 'Negative Tests',
                files: negCard.files.map(
                  (f: { path?: string; addedLines?: number }) => ({
                    path: f.path ?? '',
                    change: 'added' as const,
                    lines: f.addedLines ?? 0,
                  })
                ),
              },
            ]
          : undefined,
      };

      setMessages(prev => [...prev, assistantMessage]);
      toast({ title: 'Negative tests added' });
    } catch (err) {
      console.error('Add negatives failed', err);
      toast({ title: 'Failed to add negatives' });
    }
  };

  // Load projects on mount
  useEffect(() => {
    let mounted = true;
    projectsApi
      .getAll()
      .then(list => {
        if (!mounted) return;
        setProjects(list);
        if (list.length > 0) {
          // set first project as selected if none yet
          setSelectedProject(prev => prev ?? list[0]);
        }
      })
      .catch(() => toast({ title: 'Failed to load projects' }));

    return () => {
      mounted = false;
    };
  }, []);

  // Load environments when selectedProject changes
  useEffect(() => {
    if (!selectedProject) return;
    let mounted = true;
    environmentsApi
      .getByProject(selectedProject.id ?? '')
      .then(list => {
        if (!mounted) return;
        setEnvOptions(list);
        if (list.length > 0) setSelectedEnv(list[0].name ?? null);
      })
      .catch(() => toast({ title: 'Failed to load environments' }));

    return () => {
      mounted = false;
    };
  }, [selectedProject]);

  // Toggle dark mode
  const toggleDarkMode = () => {
    setDarkMode(!darkMode);
    document.documentElement.classList.toggle('dark');
  };

  return (
    <SidebarProvider>
      <div
        className={`h-screen w-full bg-background text-foreground overflow-hidden ${
          darkMode ? 'dark' : ''
        }`}
      >
        <div className="flex h-full w-full">
          {/* Sidebar */}
          <AppSidebar />

          {/* Main Content */}
          <div className="flex-1 flex flex-col min-w-0 overflow-hidden">
            {/* Top Bar */}
            <div className="flex-shrink-0 border-b border-border bg-card/50 backdrop-blur-sm p-4">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-3">
                  <SidebarTrigger />

                  {/* Project Selector */}
                  <DropdownMenu>
                    <DropdownMenuTrigger asChild>
                      <Button
                        variant="outline"
                        className="justify-between min-w-[200px]"
                      >
                        <div className="flex items-center gap-2">
                          <div className="w-2 h-2 rounded-full bg-accent"></div>
                          {selectedProject?.name ?? 'Select project'}
                        </div>
                        <ChevronDown className="w-4 h-4" />
                      </Button>
                    </DropdownMenuTrigger>
                    <DropdownMenuContent className="w-[200px] bg-popover z-50">
                      {projects.map(project => (
                        <DropdownMenuItem
                          key={project.id}
                          onClick={() => setSelectedProject(project)}
                        >
                          <div className="flex items-center gap-2">
                            <div className="w-2 h-2 rounded-full bg-accent"></div>
                            {project.name}
                          </div>
                        </DropdownMenuItem>
                      ))}
                    </DropdownMenuContent>
                  </DropdownMenu>

                  {/* Environment Selector */}
                  <div className="flex items-center gap-1 bg-muted rounded-lg p-1">
                    {envOptions.map(env => (
                      <button
                        key={env.id}
                        onClick={() => setSelectedEnv(env.name ?? null)}
                        className={`px-3 py-1 rounded text-sm transition-colors ${
                          selectedEnv === (env.name ?? null)
                            ? 'bg-background text-foreground shadow-sm'
                            : 'text-muted-foreground hover:text-foreground'
                        }`}
                      >
                        {env.name}
                      </button>
                    ))}
                  </div>
                </div>

                {/* User Menu */}
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button
                      variant="ghost"
                      size="sm"
                      className="h-8 w-8 rounded-full"
                    >
                      <div className="w-6 h-6 rounded-full bg-accent/20 flex items-center justify-center">
                        <User className="w-4 h-4" />
                      </div>
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent
                    align="end"
                    className="w-48 bg-popover z-50"
                  >
                    <DropdownMenuItem onClick={toggleDarkMode}>
                      {darkMode ? (
                        <Sun className="w-4 h-4 mr-2" />
                      ) : (
                        <Moon className="w-4 h-4 mr-2" />
                      )}
                      {darkMode ? 'Light mode' : 'Dark mode'}
                    </DropdownMenuItem>
                    <DropdownMenuItem>
                      <Settings className="w-4 h-4 mr-2" />
                      Settings
                    </DropdownMenuItem>
                    <DropdownMenuItem>
                      <LogOut className="w-4 h-4 mr-2" />
                      Sign out
                    </DropdownMenuItem>
                  </DropdownMenuContent>
                </DropdownMenu>
              </div>
            </div>

            {/* Chat Layout */}
            <div className="flex-1 flex min-h-0 overflow-hidden">
              {/* History Sidebar */}
              <div className="w-80 flex-shrink-0 border-r border-border bg-card flex flex-col overflow-hidden">
                <div className="flex-shrink-0 p-4 border-b border-border">
                  <div className="flex items-center gap-2 mb-4">
                    <div className="w-8 h-8 rounded-lg bg-primary flex items-center justify-center">
                      <MessageSquare className="w-5 h-5 text-primary-foreground" />
                    </div>
                    <span className="font-semibold text-lg">Chat History</span>
                  </div>
                </div>
                <div className="flex-1 overflow-hidden">
                  <HistoryList />
                </div>
              </div>

              {/* Chat Area */}
              <div className="flex-1 flex flex-col min-w-0 overflow-hidden">
                {/* Messages */}
                <div className="flex-1 overflow-y-auto">
                  <div className="p-6 max-w-4xl mx-auto">
                    {messages.map((message, idx) => (
                      <div key={idx} className="animate-fade-in">
                        <ChatMessage
                          role={message.role as 'user' | 'assistant'}
                          content={message.content}
                          cards={message.cards as Card[]}
                          buttons={message.buttons as CmdButton[]}
                          onButtonClick={async (label: string) => {
                            // MessageModel may include original llmCard under .llmCard
                            const llmCard = (
                              message as unknown as {
                                llmCard?: import('@/lib/api/llms').ChapiCard;
                              }
                            ).llmCard;
                            if (label === 'Run in Cloud') await runInCloud();
                            if (label === 'Download Run Pack')
                              await downloadRunPack(message as MessageModel);
                            if (label === 'Add Negatives')
                              await addNegatives(message as MessageModel);
                          }}
                        />
                      </div>
                    ))}
                  </div>
                </div>

                {/* Composer */}
                <div className="flex-shrink-0 border-t border-border bg-card/50 backdrop-blur-sm">
                  <ChatComposer
                    onCommandSelect={handleCommandSelect}
                    onMenuOpen={() => setShowCommandPalette(true)}
                    onMessageSend={async (msg: string) => {
                      // Build a simple request using selected project
                      if (!selectedProject?.id) {
                        toast({ title: 'Select a project first' });
                        return;
                      }

                      const req = {
                        user_query: msg,
                        projectId: selectedProject.id,
                        max_files: 3,
                        openApiJson: null,
                      };

                      // Create messages: user + loading placeholder
                      const userMessage: MessageModel = {
                        role: 'user',
                        content: msg,
                      };
                      const loadingMessage: MessageModel = {
                        role: 'assistant',
                        content: 'Generating...',
                        cards: [],
                        buttons: [],
                      };

                      // Capture current length and append user + loading so we can replace loading later
                      const currentLength = messages.length;
                      setMessages(prev => [
                        ...prev,
                        userMessage,
                        loadingMessage,
                      ]);

                      const loadingIndex = currentLength + 1; // index where loadingMessage was appended

                      try {
                        const card = await llmsApi.generate(req);
                        console.log('LLM generate response:', card);

                        const assistantMessage: MessageModel = {
                          role: 'assistant',
                          content:
                            card.heading ??
                            (card.plan ? card.plan.join('\n') : ''),
                          cards: card.files
                            ? [
                                {
                                  type: 'diff',
                                  title: card.heading ?? 'Generated Tests',
                                  files: card.files.map(f => ({
                                    path: f.path ?? '',
                                    change: 'added' as const,
                                    lines: f.addedLines ?? 0,
                                  })),
                                },
                              ]
                            : undefined,
                          buttons: [
                            { label: 'Run in Cloud', variant: 'primary' },
                            {
                              label: 'Download Run Pack',
                              variant: 'secondary',
                            },
                            { label: 'Add Negatives', variant: 'secondary' },
                          ],
                          llmCard: card,
                        };

                        // Replace the loading message with the real assistant message
                        setMessages(prev => {
                          const copy = [...prev];
                          // Guard in case messages changed length unexpectedly
                          if (loadingIndex >= 0 && loadingIndex < copy.length) {
                            copy[loadingIndex] = assistantMessage;
                          } else {
                            copy.push(assistantMessage);
                          }
                          return copy;
                        });
                      } catch (err) {
                        console.error('Error calling LLM generate:', err);
                        toast({ title: 'Failed to call LLM' });

                        // Replace loading with an error message
                        setMessages(prev => {
                          const copy = [...prev];
                          if (loadingIndex >= 0 && loadingIndex < copy.length) {
                            copy[loadingIndex] = {
                              role: 'assistant',
                              content: 'Failed to generate response',
                              cards: [],
                              buttons: [],
                            };
                          }
                          return copy;
                        });
                      }
                    }}
                  />
                </div>
              </div>
            </div>
          </div>

          {/* Right Drawer */}
          <RightDrawer isOpen={rightDrawerOpen} activeTab="diff" />

          {/* Command Palette */}
          <CommandPalette
            open={showCommandPalette}
            onOpenChange={setShowCommandPalette}
            onCommandSelect={handleCommandSelect}
          />
        </div>
      </div>
    </SidebarProvider>
  );
}
