import { Button } from '@/components/ui/button';
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog';
import { Textarea } from '@/components/ui/textarea';
import type { DetectionResponse } from '@/lib/api/auth-profiles';
import { authProfilesApi } from '@/lib/api/auth-profiles';
import React, { useState } from 'react';

export interface AiDetectionModalProps {
  projectId?: string | null;
  serviceId?: string | null;
  onDetected?: (resp: DetectionResponse) => void;
}

export const AiDetectionModal: React.FC<AiDetectionModalProps> = ({
  projectId,
  serviceId,
  onDetected,
}) => {
  const [open, setOpen] = useState(false);
  const [mode, setMode] = useState<'code' | 'prompt'>('code');
  const [code, setCode] = useState('');
  const [prompt, setPrompt] = useState('');
  const [loading, setLoading] = useState(false);

  const submitCode = async () => {
    setLoading(true);
    try {
      const resp = await authProfilesApi.detectByCode({
        code,
        projectId: projectId || undefined,
        serviceId: serviceId || undefined,
      });
      onDetected?.(resp as DetectionResponse);
      setOpen(false);
    } catch (err) {
      console.error('AI detect by code failed', err);
      alert('AI detection failed');
    } finally {
      setLoading(false);
    }
  };

  const submitPrompt = async () => {
    setLoading(true);
    try {
      const resp = await authProfilesApi.detectByPrompt({
        prompt,
        projectId: projectId || undefined,
        serviceId: serviceId || undefined,
      });
      onDetected?.(resp as DetectionResponse);
      setOpen(false);
    } catch (err) {
      console.error('AI detect by prompt failed', err);
      alert('AI detection failed');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button variant="outline">AI Auth Detection</Button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>AI Auth Detection</DialogTitle>
        </DialogHeader>

        <div className="space-y-4">
          <div className="flex gap-2">
            <Button
              variant={mode === 'code' ? 'default' : 'ghost'}
              onClick={() => setMode('code')}
            >
              By Auth Code
            </Button>
            <Button
              variant={mode === 'prompt' ? 'default' : 'ghost'}
              onClick={() => setMode('prompt')}
            >
              By Prompt
            </Button>
          </div>

          {mode === 'code' ? (
            <div>
              <p className="text-sm text-muted-foreground">
                Paste an example auth request or snippet (js, curl, ts, etc.)
              </p>
              <Textarea
                value={code}
                onChange={e => setCode(e.target.value)}
                rows={10}
              />
            </div>
          ) : (
            <div>
              <p className="text-sm text-muted-foreground">
                Provide a prompt to detect auth. Example placeholder: "Detect
                auth for Supabase: provide token endpoint, grant type and
                headers"
              </p>
              <Textarea
                value={prompt}
                onChange={e => setPrompt(e.target.value)}
                rows={6}
                placeholder="Detect auth for Supabase: provide token endpoint, grant type and headers"
              />
            </div>
          )}
        </div>

        <DialogFooter>
          <Button variant="ghost" onClick={() => setOpen(false)}>
            Cancel
          </Button>
          {mode === 'code' ? (
            <Button onClick={submitCode} disabled={loading || !code}>
              {loading ? 'Detecting...' : 'Submit'}
            </Button>
          ) : (
            <Button onClick={submitPrompt} disabled={loading || !prompt}>
              {loading ? 'Detecting...' : 'Submit'}
            </Button>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
};

export default AiDetectionModal;
