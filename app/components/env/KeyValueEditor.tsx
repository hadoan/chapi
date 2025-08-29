"use client";
import React from 'react';

type Props = {
  items: Record<string,string>;
  onChange: (v: Record<string,string>)=>void;
  addLabel?: string;
}

export default function KeyValueEditor({items, onChange, addLabel='Add'}: Props){
  const entries = Object.entries(items ?? {});

  function setKey(idx:number, key:string){
    const newObj: Record<string,string> = {};
    entries.forEach(([k,v], i) => {
      if(i===idx) newObj[key] = v;
      else newObj[k] = v;
    });
    onChange(newObj);
  }

  function setVal(idx:number, val:string){
    const newObj: Record<string,string> = {};
    entries.forEach(([k,v], i) => {
      newObj[k] = i===idx ? val : v;
    });
    onChange(newObj);
  }

  function remove(idx:number){
    const newObj: Record<string,string> = {};
    entries.forEach(([k,v], i) => { if(i!==idx) newObj[k]=v });
    onChange(newObj);
  }

  function add(){
    // add empty key with unique placeholder name
    const newKey = `key_${Date.now()}`;
    onChange({...items, [newKey]: ''});
  }

  return (
    <div className="space-y-2">
      {entries.map(([k,v], idx) => (
        <div key={k} className="flex gap-2">
          <input aria-label={`Key ${idx+1}`} value={k} onChange={e=>setKey(idx, e.target.value)} className="flex-1 border rounded px-2 py-1" />
          <input aria-label={`Value ${idx+1}`} value={v} onChange={e=>setVal(idx, e.target.value)} className="flex-1 border rounded px-2 py-1" />
          <button className="text-red-600" onClick={()=>remove(idx)} aria-label={`Delete ${k}`}>
            Delete
          </button>
        </div>
      ))}

      <button className="text-sm text-blue-600" onClick={add} aria-label="Add key">
        {addLabel}
      </button>
    </div>
  );
}
