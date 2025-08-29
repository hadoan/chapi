import { Toaster } from "@/components/ui/toaster";
import { Toaster as Sonner } from "@/components/ui/sonner";
import { TooltipProvider } from "@/components/ui/tooltip";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { BrowserRouter, Routes, Route } from "react-router-dom";
import Index from "./pages/Index";
import AppPage from "./pages/App";
import ProjectsPage from "./app/projects/page";
import ProjectOverviewPage from "./app/projects/[id]/page";
import ProjectSettingsPage from "./app/projects/[id]/settings/page";
import RunsPage from "./app/runs/page";
import RunDetailPage from "./app/runs/[runId]/page";
import NotFound from "./pages/NotFound";

const queryClient = new QueryClient();

const App = () => (
  <QueryClientProvider client={queryClient}>
    <TooltipProvider>
      <Toaster />
      <Sonner />
      <BrowserRouter>
        <Routes>
          <Route path="/" element={<Index />} />
          <Route path="/app" element={<AppPage />} />
          <Route path="/app/projects" element={<ProjectsPage />} />
          <Route path="/app/projects/:id" element={<ProjectOverviewPage />} />
          <Route path="/app/projects/:id/settings" element={<ProjectSettingsPage />} />
          <Route path="/app/runs" element={<RunsPage />} />
          <Route path="/app/runs/:runId" element={<RunDetailPage />} />
          {/* ADD ALL CUSTOM ROUTES ABOVE THE CATCH-ALL "*" ROUTE */}
          <Route path="*" element={<NotFound />} />
        </Routes>
      </BrowserRouter>
    </TooltipProvider>
  </QueryClientProvider>
);

export default App;
