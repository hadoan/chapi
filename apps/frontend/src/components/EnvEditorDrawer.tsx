"use client";

import { useState, useEffect } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Switch } from "@/components/ui/switch";
import { Sheet, SheetContent, SheetHeader, SheetTitle } from "@/components/ui/sheet";
import { Badge } from "@/components/ui/badge";
import { Trash2, Plus } from "lucide-react";

import type { EnvModel } from '@/lib/state/types';

interface EnvEditorDrawerProps {
  environment: EnvModel | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onSave: (env: Partial<EnvModel> & { id?: string }) => void;
}

export function EnvEditorDrawer({ environment, open, onOpenChange, onSave }: EnvEditorDrawerProps) {
  const [formData, setFormData] = useState<Partial<EnvModel> | null>(null);

  useEffect(() => {
    if (environment) {
      setFormData({ ...environment });
    }
  }, [environment]);

  if (!formData) return null;

  const isProduction = (formData.name ?? '').toString().toLowerCase() === 'prod';

  const handleSave = () => {
    onSave({
      ...formData,
      // Ensure createdAt exists for new items; createdAt will be ignored on create if backend sets it
      createdAt: (formData.createdAt ?? new Date().toISOString()) as string
    });
  };

  const handleHeaderChange = (key: string, value: string, oldKey?: string) => {
    const newHeaders = { ...formData.headers };
    if (oldKey && oldKey !== key) {
      delete newHeaders[oldKey];
    }
    if (key && value) {
      newHeaders[key] = value;
    } else if (key === '' && oldKey) {
      delete newHeaders[oldKey];
    }
    setFormData({ ...formData, headers: newHeaders });
  };

  const handleSecretChange = (key: string, value: string, oldKey?: string) => {
    const newSecrets = { ...formData.secrets };
    if (oldKey && oldKey !== key) {
      delete newSecrets[oldKey];
    }
    if (key && value) {
      newSecrets[key] = value;
    } else if (key === '' && oldKey) {
      delete newSecrets[oldKey];
    }
    setFormData({ ...formData, secrets: newSecrets });
  };

  const addHeader = () => {
    setFormData({
      ...formData,
      headers: { ...formData.headers, '': '' }
    });
  };

  const addSecret = () => {
    setFormData({
      ...formData,
      secrets: { ...formData.secrets, '': '' }
    });
  };

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent className="sm:max-w-lg overflow-y-auto">
        <SheetHeader>
          <SheetTitle className="flex items-center gap-2">
            Edit Environment
            <Badge variant={isProduction ? 'destructive' : 'outline'}>
              {formData.name}
            </Badge>
          </SheetTitle>
        </SheetHeader>

        <div className="space-y-6 py-6">
          {isProduction && (
            <div className="p-3 bg-destructive/10 border border-destructive/20 rounded-lg">
              <p className="text-sm text-destructive">
                <strong>Prod guard:</strong> Production environment is read-only in MVP
              </p>
            </div>
          )}

          {/* Basic Settings */}
          <div className="space-y-4">
            <div>
              <Label htmlFor="baseUrl">Base URL</Label>
              <Input
                id="baseUrl"
                value={formData.baseUrl}
                onChange={(e) => setFormData({ ...formData, baseUrl: e.target.value })}
                disabled={isProduction}
              />
            </div>

            <div>
              <Label htmlFor="timeout">Timeout (ms)</Label>
              <Input
                id="timeout"
                type="number"
                value={formData.timeoutMs}
                onChange={(e) => setFormData({ ...formData, timeoutMs: parseInt(e.target.value) || 0 })}
                disabled={isProduction}
              />
            </div>

            <div className="flex items-center justify-between">
              <Label htmlFor="followRedirects">Follow redirects</Label>
              <Switch
                id="followRedirects"
                checked={formData.followRedirects}
                onCheckedChange={(checked) => setFormData({ ...formData, followRedirects: checked })}
                disabled={isProduction}
              />
            </div>
          </div>

          {/* Headers */}
          <div className="space-y-3">
            <div className="flex items-center justify-between">
              <Label>Headers</Label>
              <Button
                variant="outline"
                size="sm"
                onClick={addHeader}
                disabled={isProduction}
              >
                <Plus className="w-4 h-4" />
              </Button>
            </div>
            <div className="space-y-2">
              {Object.entries(formData.headers).map(([key, value], idx) => (
                <div key={idx} className="flex gap-2">
                  <Input
                    placeholder="Header name"
                    value={key}
                    onChange={(e) => handleHeaderChange(e.target.value, value, key)}
                    disabled={isProduction}
                    className="flex-1"
                  />
                  <Input
                    placeholder="Header value"
                    value={value}
                    onChange={(e) => handleHeaderChange(key, e.target.value)}
                    disabled={isProduction}
                    className="flex-1"
                  />
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => handleHeaderChange('', '', key)}
                    disabled={isProduction}
                  >
                    <Trash2 className="w-4 h-4" />
                  </Button>
                </div>
              ))}
            </div>
          </div>

          {/* Secrets */}
          <div className="space-y-3">
            <div className="flex items-center justify-between">
              <Label>Secrets</Label>
              <Button
                variant="outline"
                size="sm"
                onClick={addSecret}
                disabled={isProduction}
              >
                <Plus className="w-4 h-4" />
              </Button>
            </div>
            <div className="space-y-2">
              {Object.entries(formData.secrets).map(([key, value], idx) => (
                <div key={idx} className="flex gap-2">
                  <Input
                    placeholder="Secret name"
                    value={key}
                    onChange={(e) => handleSecretChange(e.target.value, value, key)}
                    disabled={isProduction}
                    className="flex-1"
                  />
                  <Input
                    placeholder="Secret value"
                    type="password"
                    value={value}
                    onChange={(e) => handleSecretChange(key, e.target.value)}
                    disabled={isProduction}
                    className="flex-1"
                  />
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => handleSecretChange('', '', key)}
                    disabled={isProduction}
                  >
                    <Trash2 className="w-4 h-4" />
                  </Button>
                </div>
              ))}
            </div>
          </div>

          <div className="flex gap-2 pt-4">
            <Button onClick={handleSave} disabled={isProduction} className="flex-1">
              Save Changes
            </Button>
            <Button variant="outline" onClick={() => onOpenChange(false)} className="flex-1">
              Cancel
            </Button>
          </div>
        </div>
      </SheetContent>
    </Sheet>
  );
}