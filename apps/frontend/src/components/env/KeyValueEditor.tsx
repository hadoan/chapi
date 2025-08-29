"use client";
import React from 'react';
import type {EnvModel} from '@/lib/state/types';

type Props = { items: Record<string,string>; onChange: (v:Record<string,string>)=>void; addLabel?:string };

export default function KeyValueEditor({items, onChange, addLabel='Add header'}: Props){
  const entries = Object.entries(items ?? {});
  function setKey(i:number, key:string){ const obj:Record<string,string>={}; entries.forEach(([k,v], idx)=> obj[idx===i? key:k]=v); onChange(obj); }
  function setVal(i:number, val:string){ const obj:Record<string,string>={}; entries.forEach(([k,v], idx)=> obj[k]= idx===i? val:v); onChange(obj); }
  function remove(i:number){ const obj:Record<string,string>={}; entries.forEach(([k,v], idx)=> { if(idx!==i) obj[k]=v }); onChange(obj); }
  function add(){ onChange({...items, ['key_'+Date.now()]: ''}); }
  return (
    <div className="space-y-2">
      {entries.map(([k,v], i)=> (
        <div key={k} className="flex gap-2">
          <input aria-label={`Key ${i+1}`} value={k} onChange={e=>setKey(i, e.target.value)} className="flex-1 border rounded px-2 py-1" />
          <input aria-label={`Value ${i+1}`} value={v} onChange={e=>setVal(i, e.target.value)} className="flex-1 border rounded px-2 py-1" />
          <button onClick={()=>remove(i)} className="text-red-600">Delete</button>
        </div>
      ))}
      <button onClick={add} className="text-sm text-blue-600">{addLabel}</button>
    </div>
  );
}
