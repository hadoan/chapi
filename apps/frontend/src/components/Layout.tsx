import { AppSidebar } from '@/components/AppSidebar';
import ProjectSelectionBar from '@/components/ProjectSelectionBar';
import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { SidebarProvider, SidebarTrigger } from '@/components/ui/sidebar';
import ProjectContext, { ProjectContextType } from '@/lib/state/projectStore';
import { LogOut, Moon, Settings, Sun, User } from 'lucide-react';
import { useState } from 'react';

interface LayoutProps {
  children: React.ReactNode;
  showProjectSelector?: boolean;
}

export function Layout({ children, showProjectSelector = true }: LayoutProps) {
  const [selectedProject, setSelectedProject] =
    useState<ProjectContextType['selectedProject']>(null);
  const [selectedEnv, setSelectedEnv] = useState<string | null>(null);
  const [darkMode, setDarkMode] = useState(true);

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

                  {showProjectSelector && (
                    <ProjectSelectionBar
                      initialProjectId={selectedProject?.id ?? undefined}
                      onSelectProject={p => setSelectedProject(p)}
                      onSelectEnv={e => setSelectedEnv(e)}
                      onToggleDarkMode={toggleDarkMode}
                    />
                  )}
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

            {/* Content Area */}
            <div className="flex-1 overflow-y-auto">
              <ProjectContext.Provider
                value={{
                  selectedProject,
                  setSelectedProject: p => setSelectedProject(p),
                  selectedEnv,
                  setSelectedEnv: e => setSelectedEnv(e),
                }}
              >
                {children}
              </ProjectContext.Provider>
            </div>
          </div>
        </div>
      </div>
    </SidebarProvider>
  );
}
