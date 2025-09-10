import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { SidebarTrigger } from '@/components/ui/sidebar';
import { toast } from '@/hooks/use-toast';
import { getOrFetch } from '@/lib/api/cache';
import { environmentsApi } from '@/lib/api/environments';
import { projectsApi } from '@/lib/api/projects';
import { ChevronDown, LogOut, Moon, Settings, Sun, User } from 'lucide-react';
import { useEffect, useRef, useState } from 'react';

type Project = { id?: string; name?: string };
type Env = { id?: string; name?: string };

interface Props {
  onSelectProject?: (p: Project) => void;
  onSelectEnv?: (env: string | null) => void;
  showSidebarTrigger?: boolean;
  showUserMenu?: boolean;
  onToggleDarkMode?: () => void;
  initialProjectId?: string | undefined;
}

export default function ProjectSelectionBar({
  onSelectProject,
  onSelectEnv,
  showSidebarTrigger = true,
  showUserMenu = true,
  onToggleDarkMode,
  initialProjectId,
}: Props) {
  const [projects, setProjects] = useState<Project[]>([]);
  const [selectedProject, setSelectedProject] = useState<Project | null>(null);
  const [envOptions, setEnvOptions] = useState<Env[]>([]);
  const [selectedEnv, setSelectedEnv] = useState<string | null>(null);

  // Simple in-memory caches to prevent repeated API calls across mounts
  const projectsCacheRef = useRef<Project[] | null>(null);
  const envCacheRef = useRef<Map<string, Env[]>>(new Map());

  useEffect(() => {
    let mounted = true;

    // Use cache wrapper which dedupes inflight requests
    getOrFetch('projects', async () => {
      const list = await projectsApi.getAll();
      return list.map(p => ({ id: p.id ?? '', name: p.name ?? '' }));
    })
      .then(mapped => {
        if (!mounted) return;
        projectsCacheRef.current = mapped;
        setProjects(mapped);
        const pick =
          mapped.find(p => p.id === initialProjectId) ?? mapped[0] ?? null;
        setSelectedProject(pick);
        if (pick && typeof onSelectProject === 'function')
          onSelectProject(pick);
      })
      .catch(() => toast({ title: 'Failed to load projects' }));

    return () => {
      mounted = false;
    };
  }, [initialProjectId]);

  useEffect(() => {
    if (!selectedProject?.id) return;
    let mounted = true;

    // Use cached envs for this project if available
    const cached = envCacheRef.current.get(selectedProject.id);
    if (cached) {
      setEnvOptions(cached);
      if (cached.length > 0) {
        setSelectedEnv(cached[0].name ?? null);
        if (typeof onSelectEnv === 'function')
          onSelectEnv(cached[0].name ?? null);
      }
      return () => {
        mounted = false;
      };
    }

    environmentsApi
      .getByProject(selectedProject.id)
      .then(list => {
        if (!mounted) return;
        const mapped = list.map(e => ({ id: e.id ?? '', name: e.name ?? '' }));
        envCacheRef.current.set(selectedProject.id, mapped);
        setEnvOptions(mapped);
        if (mapped.length > 0) {
          setSelectedEnv(mapped[0].name ?? null);
          if (typeof onSelectEnv === 'function')
            onSelectEnv(mapped[0].name ?? null);
        }
      })
      .catch(() => toast({ title: 'Failed to load environments' }));

    return () => {
      mounted = false;
    };
  }, [selectedProject?.id]);

  return (
    <div className="flex items-center gap-2 sm:gap-3 overflow-hidden">
      {showSidebarTrigger && <SidebarTrigger />}

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
              onClick={() => {
                setSelectedProject(project);
                if (onSelectProject) onSelectProject(project);
              }}
            >
              <div className="flex items-center gap-2">
                <div className="w-2 h-2 rounded-full bg-accent"></div>
                {project.name}
              </div>
            </DropdownMenuItem>
          ))}
        </DropdownMenuContent>
      </DropdownMenu>

      {/* Environment Selector - Hidden on small screens, shown as buttons on medium */}
      <div className="hidden sm:flex items-center gap-1 bg-muted rounded-lg p-1">
        {envOptions.map(env => (
          <button
            key={env.id}
            onClick={() => {
              setSelectedEnv(env.name ?? null);
              if (onSelectEnv) onSelectEnv(env.name ?? null);
            }}
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
              <span className="truncate">{selectedEnv || 'Env'}</span>
              <ChevronDown className="w-3 h-3 ml-1" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent className="w-32 bg-popover z-50">
            {envOptions.map(env => (
              <DropdownMenuItem
                key={env.id}
                onClick={() => {
                  setSelectedEnv(env.name ?? null);
                  if (onSelectEnv) onSelectEnv(env.name ?? null);
                }}
              >
                {env.name}
              </DropdownMenuItem>
            ))}
          </DropdownMenuContent>
        </DropdownMenu>
      )}

      {/* User Menu */}
      {showUserMenu && (
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
          <DropdownMenuContent align="end" className="w-48 bg-popover z-50">
            <DropdownMenuItem onClick={onToggleDarkMode}>
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
      )}
    </div>
  );
}
