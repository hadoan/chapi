"use client";

import React, {createContext, useContext, useEffect, useState} from 'react';
import type {EnvModel, EnvName} from './types';

import mockData from '../mock/environments.json';

type EnvStore = {
  envs: EnvModel[];
  updateEnv: (name: EnvName, patch: Partial<EnvModel>) => void;
};

const Context = createContext<EnvStore | null>(null);

export function EnvProvider({children}: {children: React.ReactNode}){
  const [envs, setEnvs] = useState<EnvModel[]>([]);

  useEffect(()=>{
    // load fixtures from module import (in-memory mock)
    setEnvs(mockData as EnvModel[]);
  }, []);

  function updateEnv(name: EnvName, patch: Partial<EnvModel>){
    setEnvs(prev => prev.map(e => e.name === name ? {...e, ...patch, updatedAt: new Date().toISOString()} : e));
  }

  return <Context.Provider value={{envs, updateEnv}}>{children}</Context.Provider>;
}

export function useEnvStore(): EnvStore{
  const ctx = useContext(Context);
  if(!ctx) throw new Error('useEnvStore must be used within EnvProvider');
  return ctx;
}
