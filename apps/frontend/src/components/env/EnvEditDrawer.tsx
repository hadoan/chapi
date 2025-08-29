"use client";
import React, {useEffect, useRef, useState} from 'react';
import type {EnvModel} from '@/lib/state/types';
import KeyValueEditor from './KeyValueEditor';
import MaskedInput from './MaskedInput';

type Props = { env: EnvModel | null; open: boolean; onClose:()=>void; onSave: (patch:Partial<EnvModel>)=>void };

export default function EnvEditDrawer({env, open, onClose, onSave}: Props){
  const [form, setForm] = useState<EnvModel | null>(env);
  useEffect(()=> setForm(env ? {...env} : null), [env]);
  useEffect(()=>{ function onKey(e:KeyboardEvent){ if(e.key==='Escape') onClose(); } if(open) window.addEventListener('keydown', onKey); return ()=> window.removeEventListener('keydown', onKey); }, [open, onClose]);
  if(!open || !form) return null;
  const locked = !!form.locked;
  function update(p:Partial<EnvModel>){ setForm(f=> f? {...f, ...p} : f); }
  function save(){ if(!form.baseUrl || !/^https?:\/\//.test(form.baseUrl)) return alert('Enter a valid URL (http/https).'); if(form.timeoutMs<1000||form.timeoutMs>60000) return alert('Timeout must be 1000–60000.'); onSave(form); onClose(); alert('Environment saved.'); }
  async function testConnection(){ await new Promise(r=> setTimeout(r,800)); const ok = Math.random() < 0.7; alert(ok? 'Test success (200 OK).':'Test failed (timeout).'); }

  return (
    <div role="dialog" aria-modal="true" className="fixed inset-0 z-50 flex">
      <div className="flex-1" onClick={onClose} />
      <div className="w-full max-w-md bg-white dark:bg-slate-900 p-4 shadow-lg">
        <div className="flex items-center justify-between"><h2 className="text-lg font-semibold">Edit environment — {form.name}</h2><button aria-label="Close" onClick={onClose}>X</button></div>
        {locked && <div className="bg-yellow-100 text-yellow-900 p-2 rounded my-2">This environment is locked and read-only.</div>}
        <div className="space-y-3 mt-3">
          <label className="block text-sm">Base URL</label>
          <input value={form.baseUrl} disabled={locked} onChange={e=>update({baseUrl: e.target.value})} className="w-full border rounded px-2 py-1" />
          <label className="block text-sm">Timeout (ms)</label>
          <input type="number" value={form.timeoutMs} disabled={locked} onChange={e=>update({timeoutMs: Number(e.target.value)})} className="w-full border rounded px-2 py-1" />
          <label className="flex items-center gap-2"><input type="checkbox" checked={form.followRedirects} disabled={locked} onChange={e=>update({followRedirects: e.target.checked})} /> Follow redirects</label>
          <div><h3 className="font-medium">Headers</h3><KeyValueEditor items={form.headers} onChange={(h)=>update({headers: h})} addLabel="Add header" /></div>
          <div><h3 className="font-medium">Secrets</h3>{Object.entries(form.secrets).map(([k,v])=> (<div key={k} className="mb-2"><label className="block text-sm">{k}</label><MaskedInput label={k} value={v} disabled={locked} onChange={(val)=> update({secrets: {...form.secrets, [k]: val}})} /></div>))}</div>
        </div>
        <div className="flex justify-end gap-2 mt-4"><button onClick={testConnection} className="px-3 py-1 border rounded">Test Connection</button><button onClick={onClose} className="px-3 py-1 border rounded">Cancel</button><button onClick={save} disabled={locked} className="px-3 py-1 bg-blue-600 text-white rounded">Save</button></div>
      </div>
    </div>
  );
}
