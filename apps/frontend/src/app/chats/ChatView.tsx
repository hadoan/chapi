'use client';

import { AppSidebar } from '@/components/AppSidebar';
import { ChapiLogo } from '@/components/ChapiLogo';
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
import { RunPackFileBrowser } from '@/components/RunPackFileBrowser';
import { Button } from '@/components/ui/button';
import {
  Drawer,
  DrawerContent,
  DrawerHeader,
  DrawerTitle,
} from '@/components/ui/drawer';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { SidebarProvider, SidebarTrigger } from '@/components/ui/sidebar';
import { toast } from '@/hooks/use-toast';
import { chatApi, ConversationDto } from '@/lib/api/chat';
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
import { useCallback, useEffect, useState } from 'react';

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
  const [showCommandPalette, setShowCommandPalette] = useState(false);
  const [messages, setMessages] = useState<MessageModel[]>([]);
  const [conversations, setConversations] = useState<ConversationDto[]>([]);
  const [currentConversationId, setCurrentConversationId] = useState<
    string | null
  >(null);
  const [loadingConversations, setLoadingConversations] = useState(false);
  const [downloadingIndex, setDownloadingIndex] = useState<number>(-1);
  const [fileBrowserOpen, setFileBrowserOpen] = useState(false);
  const [selectedRunId, setSelectedRunId] = useState<string>('');
  const [showMobileHistory, setShowMobileHistory] = useState(false);

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
    if (!selectedProject?.id) {
      toast({ title: 'Please select a project first' });
      return;
    }

    // Simulate command execution
    const assistantResponse = await executeCommand(command);

    const userMessage: MessageModel = { role: 'user', content: command };

    try {
      // If no current conversation, create a new one with both messages atomically
      if (!currentConversationId) {
        const additionalMessages = [];
        if (assistantResponse) {
          additionalMessages.push({
            role: assistantResponse.role,
            content: assistantResponse.content,
            cardType: assistantResponse.cards?.length ? 'generated' : undefined,
            cardPayload: assistantResponse.cards?.length
              ? JSON.stringify(assistantResponse.cards[0])
              : undefined,
          });
        }

        const newConversation = await chatApi.createConversation({
          title: command.substring(0, 50) + (command.length > 50 ? '...' : ''),
          projectId: selectedProject.id,
          firstUserMessage: command,
          additionalMessages,
        });

        setCurrentConversationId(newConversation.id || null);

        // Refresh the conversation list to show the new conversation with updated message count
        await refreshConversations();
      } else {
        // Append to existing conversation - use batch append for both user and assistant messages
        const messagesToAppend = [
          {
            conversationId: currentConversationId,
            role: userMessage.role,
            content: userMessage.content,
            cardType: undefined,
            cardPayload: undefined,
          },
        ];

        if (assistantResponse) {
          messagesToAppend.push({
            conversationId: currentConversationId,
            role: assistantResponse.role,
            content: assistantResponse.content,
            cardType: assistantResponse.cards?.length ? 'generated' : undefined,
            cardPayload: assistantResponse.cards?.length
              ? JSON.stringify(assistantResponse.cards[0])
              : undefined,
          });
        }

        await chatApi.appendMessages({
          conversationId: currentConversationId,
          messages: messagesToAppend,
        });

        // Refresh the conversation list to update the message count and last updated time
        await refreshConversations();
      }

      // Update local state
      setMessages(prev => [...prev, userMessage, assistantResponse]);
    } catch (error) {
      console.error('Failed to save conversation:', error);
      toast({
        title: 'Failed to save conversation. Using local storage only.',
      });

      // Fall back to local state only
      setMessages(prev => [...prev, userMessage, assistantResponse]);
    }

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

  const browseFiles = (runId: string) => {
    setSelectedRunId(runId);
    setFileBrowserOpen(true);
  };

  const handleNewConversation = () => {
    setCurrentConversationId(null);
    setMessages([]);
  };

  // Function to refresh conversation list
  const refreshConversations = async () => {
    if (!selectedProject?.id) return;

    try {
      const conversationList = await chatApi.getConversations(
        selectedProject.id
      );
      setConversations(conversationList);
    } catch (error) {
      console.error('Failed to refresh conversations:', error);
    }
  };

  const loadConversation = useCallback(async (conversationId: string) => {
    try {
      const conversation = await chatApi.getConversation(conversationId);
      setCurrentConversationId(conversationId);

      console.log('Raw conversation data:', conversation);

      // Convert conversation messages to MessageModel format
      const messageModels: MessageModel[] =
        conversation.messages?.map((msg, index) => {
          const hasCardData = msg.cardType && msg.cardPayload;
          let parsedCard;

          if (hasCardData) {
            try {
              parsedCard = JSON.parse(msg.cardPayload);
              console.log(`Message ${index} parsed card:`, parsedCard);
            } catch (e) {
              console.error(
                `Failed to parse card payload for message ${index}:`,
                e
              );
            }
          }

          console.log(`Message ${index}:`, {
            role: msg.role,
            cardType: msg.cardType,
            hasCardPayload: !!msg.cardPayload,
            cardPayload: msg.cardPayload?.substring(0, 100) + '...',
            hasCardData,
            parsedCard: parsedCard ? 'Has parsed card' : 'No parsed card',
          });

          // Generate buttons for assistant messages that have card data
          // Make role comparison case-insensitive
          const isAssistant = msg.role?.toLowerCase() === 'assistant';
          const buttons =
            isAssistant && hasCardData
              ? [
                  { label: 'Run in Cloud', variant: 'primary' as const },
                  { label: 'Download Run Pack', variant: 'secondary' as const },
                  { label: 'Add Negatives', variant: 'secondary' as const },
                ]
              : undefined;

          console.log(`Message ${index} buttons:`, buttons);

          // Extract runId from card data if it exists
          const runId = parsedCard?.runId || parsedCard?.id;

          return {
            role: msg.role as 'user' | 'assistant',
            content: msg.content || '',
            cards: parsedCard ? [parsedCard] : undefined,
            buttons,
            runId, // Add runId to the message model
            llmCard: parsedCard, // Set llmCard for messages with card data
          };
        }) || [];

      console.log('Final messageModels:', messageModels);
      setMessages(messageModels);
    } catch (error) {
      console.error('Failed to load conversation:', error);
      toast({ title: 'Failed to load conversation' });
    }
  }, []);

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
        card: lm.llmCard,
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
          // If llmCard exists, restore the original action buttons plus Browse Files if runId exists
          const hasLl = !!m.llmCard;
          const hasRunId = !!m.runId;
          m.buttons = hasLl
            ? [
                { label: 'Run in Cloud', variant: 'primary' as const },
                { label: 'Download Run Pack', variant: 'secondary' as const },
                ...(hasRunId
                  ? [{ label: 'Browse Files', variant: 'secondary' as const }]
                  : []),
                { label: 'Add Negatives', variant: 'secondary' as const },
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

  // Load conversations when selectedProject changes
  useEffect(() => {
    if (!selectedProject?.id) return;

    let mounted = true;
    setLoadingConversations(true);

    chatApi
      .getConversations(selectedProject.id)
      .then(async conversationList => {
        if (!mounted) return;
        setConversations(conversationList);

        // If there are conversations and no conversation is currently selected, auto-select the latest one
        if (conversationList.length > 0 && !currentConversationId) {
          // Find the most recent conversation by updatedAt or createdAt
          const sortedConversations = [...conversationList].sort((a, b) => {
            const dateA = new Date(a.updatedAt || a.createdAt || '').getTime();
            const dateB = new Date(b.updatedAt || b.createdAt || '').getTime();
            return dateB - dateA; // Most recent first
          });
          
          const latestConversation = sortedConversations[0];
          
          if (latestConversation.id) {
            // Use the existing loadConversation function to properly load messages with all features
            await loadConversation(latestConversation.id);
          }
        } else if (conversationList.length === 0) {
          // No conversations, start with empty messages
          setMessages([]);
          setCurrentConversationId(null);
        }
      })
      .catch(() => {
        if (mounted) {
          toast({ title: 'Failed to load conversations' });
          // Fall back to mock messages if conversation loading fails
          setMessages(mockMessages as MessageModel[]);
        }
      })
      .finally(() => {
        if (mounted) setLoadingConversations(false);
      });

    return () => {
      mounted = false;
    };
  }, [selectedProject, currentConversationId, loadConversation]);

  // Toggle dark mode
  const toggleDarkMode = () => {
    document.documentElement.classList.toggle('dark');
  };

  return (
    <SidebarProvider>
      <div className="h-screen w-full bg-background text-foreground overflow-hidden">
        <div className="flex h-full w-full">
          {/* Sidebar */}
          <AppSidebar />

          {/* Main Content */}
          <div className="flex-1 flex flex-col min-w-0 overflow-hidden">
            {/* Top Bar */}
            <div className="flex-shrink-0 border-b border-border bg-card/50 backdrop-blur-sm p-2 sm:p-4">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2 sm:gap-3 overflow-hidden">
                  <SidebarTrigger />

                  {/* Project Selector */}
                  <DropdownMenu>
                    <DropdownMenuTrigger asChild>
                      <Button
                        variant="outline"
                        className="justify-between min-w-[120px] sm:min-w-[200px] max-w-[200px] truncate"
                        size="sm"
                      >
                        <div className="flex items-center gap-2 overflow-hidden">
                          <div className="w-2 h-2 rounded-full bg-accent flex-shrink-0"></div>
                          <span className="truncate">
                            {selectedProject?.name ?? 'Select project'}
                          </span>
                        </div>
                        <ChevronDown className="w-4 h-4 flex-shrink-0" />
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

                  {/* Environment Selector - Hidden on small screens, shown as dropdown on medium */}
                  <div className="hidden sm:flex items-center gap-1 bg-muted rounded-lg p-1">
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

                  {/* Mobile Environment Selector */}
                  {envOptions.length > 0 && (
                    <DropdownMenu>
                      <DropdownMenuTrigger asChild className="sm:hidden">
                        <Button variant="outline" size="sm" className="px-2">
                          <span className="truncate">
                            {selectedEnv || 'Env'}
                          </span>
                          <ChevronDown className="w-3 h-3 ml-1" />
                        </Button>
                      </DropdownMenuTrigger>
                      <DropdownMenuContent className="w-32 bg-popover z-50">
                        {envOptions.map(env => (
                          <DropdownMenuItem
                            key={env.id}
                            onClick={() => setSelectedEnv(env.name ?? null)}
                          >
                            {env.name}
                          </DropdownMenuItem>
                        ))}
                      </DropdownMenuContent>
                    </DropdownMenu>
                  )}
                </div>

                {/* User Menu */}
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button
                      variant="ghost"
                      size="sm"
                      className="h-8 w-8 rounded-full flex-shrink-0"
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
                      {document.documentElement.classList.contains('dark') ? (
                        <Sun className="w-4 h-4 mr-2" />
                      ) : (
                        <Moon className="w-4 h-4 mr-2" />
                      )}
                      {document.documentElement.classList.contains('dark')
                        ? 'Light mode'
                        : 'Dark mode'}
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
              {/* History Sidebar - Hidden on mobile, collapsible on tablet */}
              <div className="hidden lg:flex w-80 flex-shrink-0 border-r border-border bg-card flex-col overflow-hidden">
                <div className="flex-shrink-0 p-4 border-b border-border">
                  <div className="flex items-center gap-2 mb-4">
                    <ChapiLogo size={32} />
                    <span className="font-semibold text-lg">Chat History</span>
                  </div>
                </div>
                <div className="flex-1 overflow-hidden">
                  <HistoryList
                    conversations={conversations}
                    currentConversationId={currentConversationId}
                    onConversationSelect={loadConversation}
                    onNewConversation={handleNewConversation}
                    loading={loadingConversations}
                  />
                </div>
              </div>

              {/* Chat Area */}
              <div className="flex-1 flex flex-col min-w-0 overflow-hidden relative">
                {/* Mobile Chat History Button */}
                <Button
                  variant="outline"
                  size="sm"
                  className="lg:hidden absolute top-4 left-4 z-10 bg-background/80 backdrop-blur-sm border shadow-md"
                  onClick={() => setShowMobileHistory(true)}
                >
                  <MessageSquare className="w-4 h-4 mr-2" />
                  <span className="hidden sm:inline">History</span>
                </Button>

                {/* Messages */}
                <div className="flex-1 overflow-y-auto">
                  <div className="p-3 sm:p-6 max-w-4xl mx-auto w-full pt-16 lg:pt-6">
                    {messages.map((message, idx) => (
                      <div key={idx} className="animate-fade-in">
                        <ChatMessage
                          role={message.role as 'user' | 'assistant'}
                          content={message.content}
                          cards={message.cards as Card[]}
                          buttons={message.buttons as CmdButton[]}
                          runId={message.runId}
                          onBrowseFiles={browseFiles}
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
                            if (label === 'Browse Files') {
                              const runId = (
                                message as unknown as { runId?: string }
                              ).runId;
                              if (runId) browseFiles(runId);
                            }
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

                        // Save conversation in background
                        try {
                          // If no current conversation, create a new one with both messages atomically
                          if (!currentConversationId) {
                            const newConversation =
                              await chatApi.createConversation({
                                title:
                                  msg.substring(0, 50) +
                                  (msg.length > 50 ? '...' : ''),
                                projectId: selectedProject.id,
                                firstUserMessage: msg,
                                additionalMessages: [
                                  {
                                    role: assistantMessage.role,
                                    content: assistantMessage.content,
                                    cardType: assistantMessage.cards?.length
                                      ? 'generated'
                                      : undefined,
                                    cardPayload: assistantMessage.cards?.length
                                      ? JSON.stringify(
                                          assistantMessage.cards[0]
                                        )
                                      : undefined,
                                  },
                                ],
                              });

                            setCurrentConversationId(
                              newConversation.id || null
                            );

                            // Refresh conversation list
                            await refreshConversations();
                          } else {
                            // Append to existing conversation - use batch append for both user and assistant messages
                            await chatApi.appendMessages({
                              conversationId: currentConversationId,
                              messages: [
                                {
                                  conversationId: currentConversationId,
                                  role: userMessage.role,
                                  content: userMessage.content,
                                  cardType: undefined,
                                  cardPayload: undefined,
                                },
                                {
                                  conversationId: currentConversationId,
                                  role: assistantMessage.role,
                                  content: assistantMessage.content,
                                  cardType: assistantMessage.cards?.length
                                    ? 'generated'
                                    : undefined,
                                  cardPayload: assistantMessage.cards?.length
                                    ? JSON.stringify(assistantMessage.cards[0])
                                    : undefined,
                                },
                              ],
                            });

                            // Refresh conversation list
                            await refreshConversations();
                          }
                        } catch (convError) {
                          console.error(
                            'Failed to save conversation:',
                            convError
                          );
                          // Continue with UI update even if conversation save fails
                        }
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

          {/* Right Drawer - Hidden on mobile */}
          <div className="hidden xl:block">
            <RightDrawer isOpen={rightDrawerOpen} activeTab="diff" />
          </div>

          {/* Mobile History Drawer */}
          <Drawer open={showMobileHistory} onOpenChange={setShowMobileHistory}>
            <DrawerContent className="max-h-[85vh]">
              <DrawerHeader>
                <DrawerTitle className="flex items-center gap-2">
                  <ChapiLogo size={24} />
                  Chat History
                </DrawerTitle>
              </DrawerHeader>
              <div className="flex-1 overflow-hidden px-4 pb-4">
                <HistoryList
                  conversations={conversations}
                  currentConversationId={currentConversationId}
                  onConversationSelect={id => {
                    loadConversation(id);
                    setShowMobileHistory(false);
                  }}
                  onNewConversation={() => {
                    handleNewConversation();
                    setShowMobileHistory(false);
                  }}
                  loading={loadingConversations}
                />
              </div>
            </DrawerContent>
          </Drawer>

          {/* Command Palette */}
          <CommandPalette
            open={showCommandPalette}
            onOpenChange={setShowCommandPalette}
            onCommandSelect={handleCommandSelect}
          />

          {/* Run Pack File Browser */}
          <RunPackFileBrowser
            isOpen={fileBrowserOpen}
            onClose={() => setFileBrowserOpen(false)}
            runId={selectedRunId}
            runPackName={`Run Pack ${selectedRunId.substring(0, 8)}...`}
          />
        </div>
      </div>
    </SidebarProvider>
  );
}
