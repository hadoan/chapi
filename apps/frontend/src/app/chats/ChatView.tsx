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
import ProjectSelectionBar from '@/components/ProjectSelectionBar';
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
import { SidebarProvider } from '@/components/ui/sidebar';
import { toast } from '@/hooks/use-toast';
import { chatApi } from '@/lib/api/chat';
import { EnvironmentDto } from '@/lib/api/environments';
import { llmsApi } from '@/lib/api/llms';
import { ProjectDto } from '@/lib/api/projects';
import type { components } from '@/lib/api/schema';
import { LogOut, MessageSquare, Moon, Settings, Sun, User } from 'lucide-react';
import { useState } from 'react';
import { useConversations } from './hooks/useConversations';

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
  const {
    conversations,
    currentConversationId,
    messages,
    loadingConversations,
    isNewConversation,
    setMessages,
    setCurrentConversationId,
    setIsNewConversation,
    refreshConversations,
    loadConversation,
    handleNewConversation,
    downloadRunPack,
    addNegatives,
  } = useConversations(selectedProject, selectedEnv);
  const [downloadingIndex, setDownloadingIndex] = useState<number>(-1);
  const [fileBrowserOpen, setFileBrowserOpen] = useState(false);
  const [selectedRunId, setSelectedRunId] = useState<string>('');
  const [selectedRunPackId, setSelectedRunPackId] = useState<string>('');
  const [showMobileHistory, setShowMobileHistory] = useState(false);
  // conversation state and actions are provided by useConversations hook

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

        // Detect an effectively-empty card from the backend and show a helpful message
        const isEmptyCard =
          (!card.heading || card.heading.trim() === '') &&
          (!card.plan || card.plan.length === 0) &&
          (!card.files || card.files.length === 0) &&
          (!card.actions || card.actions.length === 0);

        if (isEmptyCard) {
          // Let the user know there were no endpoints or actionable items
          toast({
            title: 'No endpoints found',
            description:
              'No matching endpoints were found for the input. Try importing an OpenAPI spec or broadening your request.',
            variant: 'destructive',
          });

          return {
            role: 'assistant',
            content:
              'No matching endpoints were found for your input. Try importing your OpenAPI specification or broaden the request so we can find relevant endpoints.',
            cards: [],
            buttons: [],
          };
        }

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

        // create conversation via API and refresh; keep the hook responsible for loading
        const newConversation = await chatApi.createConversation({
          title: command.substring(0, 50) + (command.length > 50 ? '...' : ''),
          projectId: selectedProject.id,
          firstUserMessage: command,
          additionalMessages,
        });

        setCurrentConversationId(newConversation.id || null);
        setIsNewConversation(false);
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

  const browseFiles = (runId: string, runPackId?: string) => {
    console.log(
      '📁 browseFiles called with runId:',
      runId,
      'runPackId:',
      runPackId
    );
    setSelectedRunId(runId);
    setSelectedRunPackId(runPackId || runId); // Use runPackId if available, fallback to runId
    setFileBrowserOpen(true);
    console.log('✅ File browser state updated:', {
      selectedRunId: runId,
      selectedRunPackId: runPackId || runId,
      fileBrowserOpen: true,
    });
  };
  // ProjectSelectionBar now handles loading projects and environments

  // Conversation loading/selection is handled inside useConversations hook

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
                  <ProjectSelectionBar
                    initialProjectId={selectedProject?.id}
                    onSelectProject={p =>
                      setSelectedProject(
                        prev =>
                          ({
                            id: p.id ?? prev?.id ?? '',
                            name: p.name ?? prev?.name ?? '',
                          } as ProjectDto)
                      )
                    }
                    onSelectEnv={e => setSelectedEnv(e)}
                    onToggleDarkMode={toggleDarkMode}
                  />
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
                    {messages.map((message, idx) => {
                      console.log(`📨 Rendering message ${idx}:`, {
                        role: message.role,
                        hasButtons: !!message.buttons,
                        buttonCount: message.buttons?.length || 0,
                        buttons: message.buttons?.map(b => b.label),
                        runId: (message as MessageModel).runId,
                        runPackId: (message as MessageModel).runPackId,
                        messageType: typeof message,
                      });

                      return (
                        <div key={idx} className="animate-fade-in">
                          <ChatMessage
                            role={message.role as 'user' | 'assistant'}
                            content={message.content}
                            cards={message.cards as Card[]}
                            buttons={message.buttons as CmdButton[]}
                            runId={
                              message.runId ||
                              (message.runPackId &&
                              message.runPackId !==
                                '00000000-0000-0000-0000-000000000000'
                                ? message.runPackId
                                : null)
                            }
                            runPackId={message.runPackId}
                            onBrowseFiles={browseFiles}
                            onButtonClick={async (label: string) => {
                              console.log(
                                `🔘 Button clicked: "${label}" on message ${idx}`
                              );

                              // MessageModel may include original llmCard under .llmCard
                              const llmCard = (
                                message as unknown as {
                                  llmCard?: import('@/lib/api/llms').ChapiCard;
                                }
                              ).llmCard;
                              if (label === 'Run in Cloud') await runInCloud();
                              if (label === 'Download Run Pack')
                                await downloadRunPack(
                                  message as MessageModel,
                                  selectedProject || undefined
                                );
                              if (label === 'Browse Files') {
                                const messageWithIds = message as unknown as {
                                  runId?: string;
                                  runPackId?: string;
                                };
                                const validRunPackId =
                                  messageWithIds.runPackId &&
                                  messageWithIds.runPackId !==
                                    '00000000-0000-0000-0000-000000000000'
                                    ? messageWithIds.runPackId
                                    : null;
                                const runId =
                                  messageWithIds.runId || validRunPackId;
                                console.log(
                                  '🗂️ Browse Files action - runId:',
                                  runId,
                                  'from runId:',
                                  messageWithIds.runId,
                                  'from runPackId:',
                                  messageWithIds.runPackId,
                                  'validRunPackId:',
                                  validRunPackId
                                );
                                if (runId)
                                  browseFiles(
                                    runId,
                                    validRunPackId || undefined
                                  );
                                else
                                  console.warn(
                                    '❌ No valid runId or runPackId found for Browse Files'
                                  );
                              }
                              if (label === 'Add Negatives')
                                await addNegatives(message as MessageModel);
                            }}
                          />
                        </div>
                      );
                    })}
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

                        const isEmptyCard =
                          (!card.heading || card.heading.trim() === '') &&
                          (!card.plan || card.plan.length === 0) &&
                          (!card.files || card.files.length === 0) &&
                          (!card.actions || card.actions.length === 0);

                        if (isEmptyCard) {
                          toast({
                            title: 'No endpoints found',
                            description:
                              'No matching endpoints were found for the input. Try importing an OpenAPI spec or broadening your request.',
                            variant: 'destructive',
                          });

                          const assistantMessage: MessageModel = {
                            role: 'assistant',
                            content:
                              'No matching endpoints were found for your input. Try importing your OpenAPI specification or broaden the request so we can find relevant endpoints.',
                            cards: [],
                            buttons: [],
                          };

                          // Replace loading message with the helpful assistant message
                          setMessages(prev => {
                            const copy = [...prev];
                            if (
                              loadingIndex >= 0 &&
                              loadingIndex < copy.length
                            ) {
                              copy[loadingIndex] = assistantMessage;
                            } else {
                              copy.push(assistantMessage);
                            }
                            return copy;
                          });

                          // Skip saving and other flows
                          return;
                        }

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

                            // Reset the new conversation flag since we now have an actual conversation
                            setIsNewConversation(false);

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
            runPackId={selectedRunPackId}
            runPackName={`Run Pack ${selectedRunPackId.substring(0, 8)}...`}
          />
        </div>
      </div>
    </SidebarProvider>
  );
}
