import { Toaster as Sonner } from '@/components/ui/sonner';
import { Toaster } from '@/components/ui/toaster';
import { TooltipProvider } from '@/components/ui/tooltip';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { useEffect } from 'react';
import { BrowserRouter, Route, Routes, useLocation } from 'react-router-dom';
import AuthPilotPage from './app/auth-pilot/page';
import LoginPage from './app/auth/login';
import EnvironmentsPage from './app/environments/page';
import IntegrationsPage from './app/integrations/page';
import SettingsPage from './app/settings/page';
import ProjectEndpointsPage from './app/projects/[id]/endpoints/page';
import ProjectOpenApiPage from './app/projects/[id]/openapi/page';
import ProjectOverviewPage from './app/projects/[id]/page';
import ProjectSettingsPage from './app/projects/[id]/settings/page';
import ProjectsPage from './app/projects/page';
import RunDetailPage from './app/runs/[runId]/page';
import RunsPage from './app/runs/page';
import { isEnabled, pageview } from './lib/ga';
import AppPage from './pages/App';
import Index from './pages/Index';
import NotFound from './pages/NotFound';

const queryClient = new QueryClient();

const App = () => (
  <QueryClientProvider client={queryClient}>
    <TooltipProvider>
      <Toaster />
      <Sonner />
      <BrowserRouter>
        <RouteTracker />
        <Routes>
          <Route path="/" element={<Index />} />
          <Route path="/app" element={<AppPage />} />
          <Route path="/app/projects" element={<ProjectsPage />} />
          <Route path="/app/projects/:id" element={<ProjectOverviewPage />} />
          <Route
            path="/app/projects/:id/endpoints"
            element={<ProjectEndpointsPage />}
          />
          <Route
            path="/app/projects/:id/openapi"
            element={<ProjectOpenApiPage />}
          />
          <Route
            path="/app/projects/:id/settings"
            element={<ProjectSettingsPage />}
          />
          <Route path="/app/runs" element={<RunsPage />} />
          <Route path="/app/runs/:runId" element={<RunDetailPage />} />
          <Route path="/app/environments" element={<EnvironmentsPage />} />
          <Route path="/app/integrations" element={<IntegrationsPage />} />
          <Route path="/app/settings" element={<SettingsPage />} />
          <Route path="/app/auth-pilot" element={<AuthPilotPage />} />
          <Route path="/auth/login" element={<LoginPage />} />
          {/* ADD ALL CUSTOM ROUTES ABOVE THE CATCH-ALL "*" ROUTE */}
          <Route path="*" element={<NotFound />} />
        </Routes>
      </BrowserRouter>
    </TooltipProvider>
  </QueryClientProvider>
);

export default App;

function RouteTracker() {
  const location = useLocation();
  useEffect(() => {
    if (!isEnabled()) return;
    pageview(location.pathname + location.search);
  }, [location]);
  return null;
}
