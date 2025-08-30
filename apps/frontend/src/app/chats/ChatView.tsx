"use client";

import { useState } from "react";
import { HistoryList } from "@/components/HistoryList";
import { ChatMessage, MessageCard, MessageButton, MessageModel } from "@/components/ChatMessage";
import { ChatComposer } from "@/components/ChatComposer";
import { RightDrawer } from "@/components/RightDrawer";
import { CommandPalette } from "@/components/CommandPalette";
import { AppSidebar } from "@/components/AppSidebar";
import { Button } from "@/components/ui/button";
import { toast } from "@/hooks/use-toast";
import { Badge } from "@/components/ui/badge";
import { ScrollArea } from "@/components/ui/scroll-area";
import { SidebarProvider, SidebarTrigger } from "@/components/ui/sidebar";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";
import { MessageSquare, ChevronDown, Settings, User, LogOut, Sun, Moon } from "lucide-react";
import mockMessages from "@/lib/mock/messages/chat-1.json";

type Card = MessageCard;
type CmdButton = MessageButton;

const projects = [
  { id: "proj-1", name: "E-commerce API", env: "staging" },
  { id: "proj-2", name: "User Service", env: "production" },
  { id: "proj-3", name: "Payment Gateway", env: "local" }
];

const environments = ["local", "staging", "production"];

export default function ChatView() {
  const [selectedProject, setSelectedProject] = useState(projects[1]);
  const [selectedEnv, setSelectedEnv] = useState("staging");
  const [rightDrawerOpen, setRightDrawerOpen] = useState(true);
  const [darkMode, setDarkMode] = useState(true);
  const [showCommandPalette, setShowCommandPalette] = useState(false);
  const [messages, setMessages] = useState<MessageModel[]>(mockMessages as MessageModel[]);

  const executeCommand = (command: string): MessageModel => {
    const baseResponse: { role: 'assistant'; content: string; cards: Card[]; buttons: CmdButton[] } = {
      role: 'assistant',
      content: '',
      cards: [] as Card[],
      buttons: [] as CmdButton[]
    };

    if (command.includes('/generate smoke') || command.includes('generate smoke')) {
      toast({ title: "Suite generated (mock)" });
      return {
        ...baseResponse,
        content: "I'll generate a comprehensive smoke test suite for your API endpoints.",
        cards: [
          {
            type: 'plan',
            title: 'Test Generation Plan',
            items: [
              'Analyze API structure and endpoints',
              'Create basic smoke tests for each endpoint',
              'Add authentication and error handling tests',
              'Generate test data and assertions'
            ]
          },
          {
            type: 'diff',
            title: 'Generated Tests',
            files: [
              { path: 'tests/smoke/auth.test.json', change: 'added', lines: 24 },
              { path: 'tests/smoke/users.test.json', change: 'added', lines: 18 },
              { path: 'tests/smoke/payments.test.json', change: 'added', lines: 32 }
            ]
          }
        ] as Card[],
        buttons: [
          { label: 'Run in Cloud', variant: 'primary' },
          { label: 'Download Run Pack', variant: 'secondary' }
        ] as CmdButton[]
      };
    }

    if (command.includes('/negatives') || command.includes('add 3 negatives')) {
      toast({ title: "Negative tests added" });
      return {
        ...baseResponse,
        content: "Added 3 negative test cases to improve error coverage.",
        cards: [
          {
            type: 'diff',
            title: 'Updated Tests',
            files: [
              { path: 'tests/negatives/auth-errors.test.json', change: 'added', lines: 15 },
              { path: 'tests/negatives/validation.test.json', change: 'modified', lines: 8 },
              { path: 'tests/negatives/rate-limits.test.json', change: 'added', lines: 12 }
            ]
          }
        ] as Card[]
      };
    }

    if (command.includes('/run cloud') || command.includes('run in cloud')) {
      toast({ title: "Cloud run started (EU)" });
      return {
        ...baseResponse,
        content: "Started test execution in EU cloud environment.",
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
            failed: 0
          }
        ] as Card[],
        buttons: [
          { label: 'View Artifacts', variant: 'secondary' },
          { label: 'Create PR', variant: 'primary' }
        ] as CmdButton[]
      };
    }

    if (command.includes('/download') || command.includes('download run pack')) {
      toast({ title: "ZIP downloaded" });
      return {
        ...baseResponse,
        content: "Generated run pack with all necessary files for local execution.",
        buttons: [ { label: 'Open File Explorer', variant: 'secondary' } ] as CmdButton[]
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

All smoke tests are passing. Ready to merge!`
          }
        ] as Card[]
      };
    }

    // Default response
    return {
      ...baseResponse,
      content: `Executed command: ${command}. This is a mock response.`
    };
  };

  const handleCommandSelect = (command: string) => {
    // Simulate command execution
    const assistantResponse = executeCommand(command);

    const userMessage: MessageModel = { role: 'user', content: command };

    setMessages(prev => [
      ...prev,
      userMessage,
      assistantResponse as MessageModel
    ]);
    
    // Show drawer for certain commands
    if (command.includes('generate') || command.includes('run') || command.includes('download')) {
      setRightDrawerOpen(true);
    }
  };

  // Toggle dark mode
  const toggleDarkMode = () => {
    setDarkMode(!darkMode);
    document.documentElement.classList.toggle('dark');
  };

  return (
    <SidebarProvider>
      <div className={`h-screen w-full bg-background text-foreground overflow-hidden ${darkMode ? 'dark' : ''}`}>
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
                      <Button variant="outline" className="justify-between min-w-[200px]">
                        <div className="flex items-center gap-2">
                          <div className="w-2 h-2 rounded-full bg-accent"></div>
                          {selectedProject.name}
                        </div>
                        <ChevronDown className="w-4 h-4" />
                      </Button>
                    </DropdownMenuTrigger>
                    <DropdownMenuContent className="w-[200px] bg-popover z-50">
                      {projects.map((project) => (
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
                    {environments.map((env) => (
                      <button
                        key={env}
                        onClick={() => setSelectedEnv(env)}
                        className={`px-3 py-1 rounded text-sm transition-colors ${
                          selectedEnv === env
                            ? "bg-background text-foreground shadow-sm"
                            : "text-muted-foreground hover:text-foreground"
                        }`}
                      >
                        {env}
                      </button>
                    ))}
                  </div>
                </div>

                {/* User Menu */}
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button variant="ghost" size="sm" className="h-8 w-8 rounded-full">
                      <div className="w-6 h-6 rounded-full bg-accent/20 flex items-center justify-center">
                        <User className="w-4 h-4" />
                      </div>
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="end" className="w-48 bg-popover z-50">
                    <DropdownMenuItem onClick={toggleDarkMode}>
                      {darkMode ? <Sun className="w-4 h-4 mr-2" /> : <Moon className="w-4 h-4 mr-2" />}
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
