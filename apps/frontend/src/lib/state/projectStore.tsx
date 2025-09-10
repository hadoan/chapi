'use client';
import { createContext, useContext } from 'react';

export type ProjectModel = { id?: string; name?: string } | null;

export type ProjectContextType = {
  selectedProject: ProjectModel;
  setSelectedProject: (p: ProjectModel) => void;
  selectedEnv: string | null;
  setSelectedEnv: (env: string | null) => void;
};

export const ProjectContext = createContext<ProjectContextType | null>(null);

export function useProject(): ProjectContextType {
  const ctx = useContext(ProjectContext);
  if (!ctx)
    throw new Error('useProject must be used within a ProjectContext provider');
  return ctx;
}

export default ProjectContext;
