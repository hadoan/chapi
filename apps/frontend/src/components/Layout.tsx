import { useState } from "react";
import { AppSidebar } from "@/components/AppSidebar";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { SidebarProvider, SidebarTrigger } from "@/components/ui/sidebar";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";
import { ChevronDown, Settings, User, LogOut, Sun, Moon } from "lucide-react";

const projects = [
  { id: "proj-1", name: "E-commerce API", env: "staging" },
  { id: "proj-2", name: "User Service", env: "production" },
  { id: "proj-3", name: "Payment Gateway", env: "local" }
];

const environments = ["local", "staging", "production"];

interface LayoutProps {
  children: React.ReactNode;
  showProjectSelector?: boolean;
}

export function Layout({ children, showProjectSelector = true }: LayoutProps) {
  const [selectedProject, setSelectedProject] = useState(projects[1]);
  const [selectedEnv, setSelectedEnv] = useState("staging");
  const [darkMode, setDarkMode] = useState(true);

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
                  
                  {showProjectSelector && (
                    <>
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
                    </>
                  )}
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

            {/* Content Area */}
            <div className="flex-1 overflow-y-auto">
              {children}
            </div>
          </div>
        </div>
      </div>
    </SidebarProvider>
  );
}