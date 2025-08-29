"use client";
import React, {useState} from 'react';
import { EnvProvider, useEnvStore } from '../lib/state/envStore';
import EnvTable from '../components/env/EnvTable';
import EnvEditDrawer from '../components/env/EnvEditDrawer';

function Inner(){
  const {envs, updateEnv} = useEnvStore();
  const [editing, setEditing] = useState<string | null>(null);

  const env = envs.find(e=> e.name === editing) ?? null;

  return (
    <div className="p-6">
      <header className="mb-4">
        <h1 className="text-2xl font-bold">Environments</h1>
        <p className="text-sm text-slate-600">Configure base URLs, headers & secrets.</p>
      </header>

      <EnvTable envs={envs} onEdit={(n)=> setEditing(n)} />

      <EnvEditDrawer env={env} open={!!editing} onClose={()=> setEditing(null)} onSave={(patch)=>{ if(env) updateEnv(env.name as any, patch); }} />
    </div>
  );
}

export default function EnvironmentsClient(){
  return (
    <EnvProvider>
      <Inner />
    </EnvProvider>
  );
}
