'use client';
import { toast } from '@/hooks/use-toast';
import { clearCache, getOrFetch } from '@/lib/api/cache';
import { environmentsApi, type EnvironmentDto } from '@/lib/api/environments';
import React, { createContext, useContext, useEffect, useState } from 'react';
import type { EnvModel } from './types';

type EnvStore = {
  envs: EnvModel[];
  loading: boolean;
  updateEnv: (id: string, patch: Partial<EnvModel>) => Promise<void>;
  createEnv: (
    env: Omit<EnvModel, 'id' | 'createdAt'>,
    projectId?: string
  ) => Promise<void>;
  deleteEnv: (id: string) => Promise<void>;
  refetch: () => Promise<void>;
};

const Context = createContext<EnvStore | null>(null);

// Helper to convert backend DTO to frontend model
function dtoToModel(dto: EnvironmentDto): EnvModel {
  const headers: Record<string, string> = {};
  dto.headers.forEach(h => (headers[h.key] = h.value));

  const secrets: Record<string, string> = {};
  dto.secrets.forEach(s => (secrets[s.keyPath] = s.maskedPreview));

  // Check if environment should be locked (production environments)
  const locked =
    dto.name.toLowerCase() === 'prod' ||
    dto.name.toLowerCase() === 'production';

  return {
    id: dto.id,
    name: dto.name,
    baseUrl: dto.baseUrl,
    timeoutMs: dto.timeoutMs,
    followRedirects: dto.followRedirects,
    headers,
    secrets,
    createdAt: dto.createdAt,
    locked,
  };
}

// Helper to convert frontend model to backend update request
function modelToUpdateRequest(model: EnvModel) {
  const headers = Object.entries(model.headers).map(([key, value]) => ({
    key,
    value,
  }));

  return {
    baseUrl: model.baseUrl,
    timeoutMs: model.timeoutMs,
    followRedirects: model.followRedirects,
    headers,
  };
}

export function EnvProvider({
  children,
  projectId,
}: {
  children: React.ReactNode;
  projectId?: string;
}) {
  const [envs, setEnvs] = useState<EnvModel[]>([]);
  const [loading, setLoading] = useState(true);

  const fetchEnvs = async () => {
    try {
      setLoading(true);
      const dtos = await getOrFetch(`envs-${projectId ?? 'all'}`, async () =>
        projectId
          ? await environmentsApi.getByProject(projectId)
          : await environmentsApi.getAll()
      );
      const models = dtos.map(dtoToModel);
      setEnvs(models);
    } catch (error) {
      console.error('Failed to fetch environments:', error);
      toast({ title: 'Failed to load environments', variant: 'destructive' });
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchEnvs();
  }, [projectId]);

  const updateEnv = async (id: string, patch: Partial<EnvModel>) => {
    try {
      const current = envs.find(e => e.id === id);
      if (!current) return;

      const updated = { ...current, ...patch };
      const updateRequest = modelToUpdateRequest(updated);

      const dto = await environmentsApi.update(id, updateRequest);
      const model = dtoToModel(dto);

      setEnvs(prev => prev.map(e => (e.id === id ? model : e)));
      toast({ title: 'Environment updated successfully' });
      try {
        clearCache(`envs-${projectId ?? 'all'}`);
      } catch (e) {
        /* ignore */
      }
    } catch (error) {
      console.error('Failed to update environment:', error);
      toast({ title: 'Failed to update environment', variant: 'destructive' });
    }
  };

  const createEnv = async (
    env: Omit<EnvModel, 'id' | 'createdAt'>,
    projectId?: string
  ) => {
    try {
      const headers = Object.entries(env.headers).map(([key, value]) => ({
        key,
        value,
      }));
      const createRequest = {
        name: env.name,
        baseUrl: env.baseUrl,
        timeoutMs: env.timeoutMs,
        followRedirects: env.followRedirects,
        headers,
      };
      let dto;
      if (projectId) {
        dto = await environmentsApi.createForProject(projectId, createRequest);
      } else {
        dto = await environmentsApi.create(createRequest);
      }
      const model = dtoToModel(dto);

      setEnvs(prev => [model, ...prev]);
      toast({ title: 'Environment created successfully' });
      try {
        clearCache(`envs-${projectId ?? 'all'}`);
      } catch (e) {
        /* ignore */
      }
    } catch (error) {
      console.error('Failed to create environment:', error);
      toast({ title: 'Failed to create environment', variant: 'destructive' });
    }
  };

  const deleteEnv = async (id: string) => {
    try {
      await environmentsApi.delete(id);
      setEnvs(prev => prev.filter(e => e.id !== id));
      toast({ title: 'Environment deleted successfully' });
      try {
        clearCache(`envs-${projectId ?? 'all'}`);
      } catch (e) {
        /* ignore */
      }
    } catch (error) {
      console.error('Failed to delete environment:', error);
      toast({ title: 'Failed to delete environment', variant: 'destructive' });
    }
  };

  return (
    <Context.Provider
      value={{
        envs,
        loading,
        updateEnv,
        createEnv,
        deleteEnv,
        refetch: fetchEnvs,
      }}
    >
      {children}
    </Context.Provider>
  );
}

export function useEnvStore(): EnvStore {
  const ctx = useContext(Context);
  if (!ctx) throw new Error('useEnvStore must be used within EnvProvider');
  return ctx;
}
