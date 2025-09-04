import { Toaster } from "@/components/ui/toaster";
import { Toaster as Sonner } from "@/components/ui/sonner";
import { TooltipProvider } from "@/components/ui/tooltip";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { BrowserRouter, Routes, Route, useLocation } from "react-router-dom";
import { useEffect } from "react";
import { pageview, isEnabled } from "./lib/ga";
import Index from "./pages/Index";
import AppPage from "./pages/App";
import ProjectsPage from "./app/projects/page";
import ProjectOverviewPage from "./app/projects/[id]/page";
import ProjectSettingsPage from "./app/projects/[id]/settings/page";
import ProjectEndpointsPage from "./app/projects/[id]/endpoints/page";
import LoginPage from "./app/auth/login";
import RunsPage from "./app/runs/page";
import RunDetailPage from "./app/runs/[runId]/page";
import EnvironmentsPage from "./app/environments/page";
import NotFound from "./pages/NotFound";

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
          <Route path="/app/projects/:id/endpoints" element={<ProjectEndpointsPage />} />
          <Route path="/app/projects/:id/settings" element={<ProjectSettingsPage />} />
          <Route path="/app/runs" element={<RunsPage />} />
          <Route path="/app/runs/:runId" element={<RunDetailPage />} />
          <Route path="/app/environments" element={<EnvironmentsPage />} />
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
