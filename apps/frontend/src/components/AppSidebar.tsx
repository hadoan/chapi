import { ChapiLogo } from '@/components/ChapiLogo';
import {
  Database,
  FolderOpen,
  GitBranch,
  MessageSquare,
  Play,
  Settings,
} from 'lucide-react';
import { NavLink, useLocation } from 'react-router-dom';

import {
  Sidebar,
  SidebarContent,
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  useSidebar,
} from '@/components/ui/sidebar';

const navigationItems = [
  { title: 'Chat', url: '/app', icon: MessageSquare },
  { title: 'Projects', url: '/app/projects', icon: FolderOpen },
  { title: 'Runs', url: '/app/runs', icon: Play },
];

const managementItems = [
  { title: 'Environments', url: '/app/environments', icon: Database },
  { title: 'Integrations', url: '/app/integrations', icon: GitBranch },
  { title: 'Settings', url: '/app/settings', icon: Settings },
];

export function AppSidebar() {
  const { state } = useSidebar();
  const location = useLocation();
  const currentPath = location.pathname;

  const isCollapsed = state === 'collapsed';

  const isActive = (path: string) => {
    if (path === '/app') {
      return currentPath === '/app';
    }
    return currentPath.startsWith(path);
  };

  const getNavClass = (path: string) => {
    return isActive(path)
      ? 'bg-accent text-accent-foreground font-medium'
      : 'hover:bg-accent/50';
  };

  return (
    <Sidebar className={isCollapsed ? 'w-14' : 'w-64'}>
      <SidebarContent>
        {/* Brand */}
        <div className="p-4 border-b border-border">
          <div className="flex items-center gap-2">
            <ChapiLogo size={32} />
            {!isCollapsed && (
              <div>
                <span className="font-semibold text-lg">Chapi</span>
                <div className="text-xs text-muted-foreground">API Testing</div>
              </div>
            )}
          </div>
        </div>

        {/* Navigation */}
        <SidebarGroup>
          <SidebarGroupLabel>Navigation</SidebarGroupLabel>
          <SidebarGroupContent>
            <SidebarMenu>
              {navigationItems.map(item => (
                <SidebarMenuItem key={item.title}>
                  <SidebarMenuButton asChild>
                    <NavLink
                      to={item.url}
                      end={item.url === '/app'}
                      className={getNavClass(item.url)}
                    >
                      <item.icon className="h-4 w-4" />
                      {!isCollapsed && <span>{item.title}</span>}
                    </NavLink>
                  </SidebarMenuButton>
                </SidebarMenuItem>
              ))}
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>

        {/* Management */}
        <SidebarGroup>
          <SidebarGroupLabel>Management</SidebarGroupLabel>
          <SidebarGroupContent>
            <SidebarMenu>
              {managementItems.map(item => (
                <SidebarMenuItem key={item.title}>
                  <SidebarMenuButton asChild>
                    <NavLink to={item.url} className={getNavClass(item.url)}>
                      <item.icon className="h-4 w-4" />
                      {!isCollapsed && <span>{item.title}</span>}
                    </NavLink>
                  </SidebarMenuButton>
                </SidebarMenuItem>
              ))}
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>
      </SidebarContent>
    </Sidebar>
  );
}
