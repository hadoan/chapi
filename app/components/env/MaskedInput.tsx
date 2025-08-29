"use client";
import React, {useState} from 'react';

type Props = {
  label: string;
  value: string;
  onChange: (v:string)=>void;
  placeholder?: string;
  disabled?: boolean;
}

export default function MaskedInput({label, value, onChange, placeholder, disabled=false}: Props){
  const [reveal, setReveal] = useState(false);

  return (
    <div>
      <label className="block text-sm font-medium">{label}</label>
      <div className="flex items-center gap-2">
        <input aria-label={label} type={reveal? 'text' : 'password'} value={value} onChange={e=>onChange(e.target.value)} placeholder={placeholder} disabled={disabled}
          className="flex-1 border rounded px-2 py-1" />
        <button type="button" aria-label="Reveal" onClick={()=>setReveal(r=>!r)} className="text-sm text-gray-600">{reveal? 'Hide' : 'Reveal'}</button>
        <button type="button" aria-label="Copy" onClick={()=>{ navigator.clipboard?.writeText(value); alert('Copied'); }} className="text-sm text-gray-600">Copy</button>
      </div>
    </div>
  )
}
