"use client";
import React from 'react';
import type {EnvModel} from '@/lib/state/types';

type Props = { envs: EnvModel[]; onEdit: (name:string)=>void };

export default function EnvTable({envs, onEdit}: Props){
  return (
    <table className="min-w-full text-sm">
      <thead>
        <tr>
          <th className="text-left">Environment</th>
          <th className="text-left">Base URL</th>
          <th className="text-left">Timeout (ms)</th>
          <th className="text-left">Follow redirects</th>
          <th className="text-left">Updated</th>
          <th className="text-left">Actions</th>
        </tr>
      </thead>
      <tbody>
        {envs.map(e=> (
          <tr key={e.name} className="border-t">
            <td><span className="px-2 py-1 rounded bg-slate-100 text-slate-900">{e.name}</span></td>
            <td>{e.baseUrl}</td>
            <td>{e.timeoutMs}</td>
            <td>{e.followRedirects ? '✓' : '—'}</td>
            <td>{new Date(e.updatedAt).toLocaleString()}</td>
            <td>
              <button onClick={()=>onEdit(e.name)} disabled={!!e.locked} className="text-blue-600">Edit</button>
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
