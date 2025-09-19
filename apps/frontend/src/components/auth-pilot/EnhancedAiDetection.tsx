import { Button } from '@/components/ui/button';
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog';
import { Progress } from '@/components/ui/progress';
import { Textarea } from '@/components/ui/textarea';
import type { DetectionResponse } from '@/lib/api/auth-profiles';
import { llmApi } from '@/lib/api/llm';
import { cn } from '@/lib/utils';
import {
  AlertTriangle,
  Brain,
  CheckCircle,
  Code,
  Loader2,
  MessageSquare,
  Sparkles,
  Zap,
} from 'lucide-react';
import React, { useState } from 'react';

export interface EnhancedAiDetectionProps {
  projectId?: string | null;
  serviceId?: string | null;
  onDetected?: (resp: DetectionResponse) => void;
  variant?: 'default' | 'prominent' | 'minimal';
  className?: string;
}

export const EnhancedAiDetection: React.FC<EnhancedAiDetectionProps> = ({
  projectId,
  serviceId,
  onDetected,
  variant = 'prominent',
  className,
}) => {
  const [open, setOpen] = useState(false);
  const [mode, setMode] = useState<'code' | 'prompt'>('code');
  const [code, setCode] = useState('');
  const [prompt, setPrompt] = useState('');
  const [loading, setLoading] = useState(false);
  const [progress, setProgress] = useState(0);
  const [lastResult, setLastResult] = useState<{
    status: 'success' | 'error' | null;
    candidates: number;
  }>({ status: null, candidates: 0 });

  const simulateProgress = () => {
    setProgress(0);
    const interval = setInterval(() => {
      setProgress(prev => {
        if (prev >= 90) {
          clearInterval(interval);
          return 90;
        }
        return prev + Math.random() * 20;
      });
    }, 200);
    return interval;
  };

  const submitCode = async () => {
    setLoading(true);
    const progressInterval = simulateProgress();

    try {
      const resp = await llmApi.detectByCode({
        code,
        projectId: projectId || undefined,
        serviceId: serviceId || undefined,
      });

      clearInterval(progressInterval);
      setProgress(100);

      // Brief delay to show completion
      await new Promise(resolve => setTimeout(resolve, 500));

      setLastResult({
        status: 'success',
        candidates: resp.candidates?.length || 0,
      });

      onDetected?.(resp as DetectionResponse);
      setOpen(false);
    } catch (err) {
      clearInterval(progressInterval);
      console.error('AI detect by code failed', err);
      setLastResult({ status: 'error', candidates: 0 });
      // Keep dialog open on error
    } finally {
      setLoading(false);
      setProgress(0);
    }
  };

  const submitPrompt = async () => {
    setLoading(true);
    const progressInterval = simulateProgress();

    try {
      const resp = await llmApi.detectByPrompt({
        prompt,
        projectId: projectId || undefined,
        serviceId: serviceId || undefined,
      });

      clearInterval(progressInterval);
      setProgress(100);

      // Brief delay to show completion
      await new Promise(resolve => setTimeout(resolve, 500));

      setLastResult({
        status: 'success',
        candidates: resp.candidates?.length || 0,
      });

      onDetected?.(resp as DetectionResponse);
      setOpen(false);
    } catch (err) {
      clearInterval(progressInterval);
      console.error('AI detect by prompt failed', err);
      setLastResult({ status: 'error', candidates: 0 });
      // Keep dialog open on error
    } finally {
      setLoading(false);
      setProgress(0);
    }
  };

  const getButtonContent = () => {
    if (loading) {
      return (
        <>
          <Loader2 className="w-4 h-4 animate-spin" />
          Analyzing...
        </>
      );
    }

    if (lastResult.status === 'success' && lastResult.candidates > 0) {
      return (
        <>
          <CheckCircle className="w-4 h-4" />
          Found {lastResult.candidates} method
          {lastResult.candidates !== 1 ? 's' : ''}
        </>
      );
    }

    if (lastResult.status === 'error') {
      return (
        <>
          <AlertTriangle className="w-4 h-4" />
          Try Again
        </>
      );
    }

    return (
      <>
        <Brain className="w-4 h-4" />
        AI Auth Detection
      </>
    );
  };

  const getButtonVariant = () => {
    if (loading) return 'secondary';
    if (lastResult.status === 'success') return 'default';
    if (lastResult.status === 'error') return 'destructive';
    return variant === 'prominent' ? 'default' : 'outline';
  };

  const getButtonClassName = () => {
    const baseClasses = cn(
      'relative overflow-hidden transition-all duration-300',
      className
    );

    if (variant === 'prominent') {
      return cn(
        baseClasses,
        'bg-gradient-to-r from-purple-600 to-blue-600 hover:from-purple-700 hover:to-blue-700',
        'text-white font-semibold shadow-lg hover:shadow-xl',
        'border-0 group',
        lastResult.status === 'success' &&
          'from-green-600 to-emerald-600 hover:from-green-700 hover:to-emerald-700',
        lastResult.status === 'error' &&
          'from-red-600 to-orange-600 hover:from-red-700 hover:to-orange-700'
      );
    }

    if (variant === 'minimal') {
      return cn(
        baseClasses,
        'hover:bg-gradient-to-r hover:from-purple-50 hover:to-blue-50',
        'hover:border-purple-300 group'
      );
    }

    return baseClasses;
  };

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button
          variant={getButtonVariant()}
          size="default"
          className={getButtonClassName()}
          disabled={loading}
        >
          {/* Shimmer effect for prominent variant */}
          {variant === 'prominent' && !loading && (
            <div className="absolute inset-0 bg-gradient-to-r from-transparent via-white/10 to-transparent opacity-0 group-hover:opacity-100 transition-opacity duration-500 transform -skew-x-12 group-hover:animate-pulse" />
          )}

          {getButtonContent()}

          {variant === 'prominent' && !loading && (
            <Sparkles className="w-3 h-3 ml-1 opacity-60 group-hover:opacity-100 transition-opacity" />
          )}
        </Button>
      </DialogTrigger>

      <DialogContent className="sm:max-w-[700px] max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <div className="p-2 bg-gradient-to-br from-purple-100 to-blue-100 dark:from-purple-900/50 dark:to-blue-900/50 rounded-lg">
              <Brain className="w-5 h-5 text-purple-600 dark:text-purple-400" />
            </div>
            AI Authentication Detection
          </DialogTitle>
        </DialogHeader>

        <div className="space-y-6">
          {/* Mode Selection */}
          <div className="flex gap-2 p-1 bg-muted dark:bg-slate-800/50 rounded-lg">
            <Button
              variant={mode === 'code' ? 'default' : 'ghost'}
              onClick={() => setMode('code')}
              className={cn(
                'flex-1 gap-2',
                mode === 'code' && 'bg-white dark:bg-slate-700 shadow-sm'
              )}
              size="sm"
            >
              <Code className="w-4 h-4" />
              Auth Code Analysis
            </Button>
            <Button
              variant={mode === 'prompt' ? 'default' : 'ghost'}
              onClick={() => setMode('prompt')}
              className={cn(
                'flex-1 gap-2',
                mode === 'prompt' && 'bg-white dark:bg-slate-700 shadow-sm'
              )}
              size="sm"
            >
              <MessageSquare className="w-4 h-4" />
              Prompt Detection
            </Button>
          </div>

          {/* Progress Bar */}
          {loading && (
            <div className="space-y-3 p-4 bg-slate-50 dark:bg-slate-800/30 rounded-lg border dark:border-slate-700">
              <div className="flex items-center justify-between text-sm">
                <span className="text-muted-foreground">
                  Analyzing authentication patterns...
                </span>
                <span className="text-sm font-mono text-purple-600 dark:text-purple-400 font-semibold">
                  {Math.round(progress)}%
                </span>
              </div>
              <Progress
                value={progress}
                className="h-3 bg-slate-200 dark:bg-slate-700"
              />
            </div>
          )}

          {/* Content Area */}
          {mode === 'code' ? (
            <div className="space-y-4">
              <div>
                <label className="text-sm font-medium text-foreground">
                  Authentication Code Sample
                </label>
                <p className="text-xs text-muted-foreground mt-1">
                  Paste authentication code in any format (JavaScript, cURL,
                  TypeScript, etc.)
                </p>
              </div>
              <Textarea
                value={code}
                onChange={e => setCode(e.target.value)}
                rows={16}
                className="font-mono text-sm min-h-[400px] resize-none bg-slate-50 dark:bg-slate-900/50 border-slate-300 dark:border-slate-600 focus:border-purple-500 dark:focus:border-purple-400 focus:ring-purple-500/20 dark:focus:ring-purple-400/20"
                placeholder={`// Example: JavaScript fetch with auth
fetch('https://api.example.com/token', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/x-www-form-urlencoded',
  },
  body: new URLSearchParams({
    'grant_type': 'client_credentials',
    'client_id': 'your-client-id',
    'client_secret': 'your-client-secret'
  })
})

// Or cURL example:
curl -X POST https://api.example.com/token \\
  -H "Content-Type: application/x-www-form-urlencoded" \\
  -d "grant_type=client_credentials&client_id=your-id&client_secret=your-secret"

// Or TypeScript/Axios example:
const response = await axios.post('/api/auth/token', {
  grant_type: 'client_credentials',
  client_id: process.env.CLIENT_ID,
  client_secret: process.env.CLIENT_SECRET
});`}
              />
            </div>
          ) : (
            <div className="space-y-4">
              <div>
                <label className="text-sm font-medium text-foreground">
                  Authentication Description
                </label>
                <p className="text-xs text-muted-foreground mt-1">
                  Describe the authentication method or service you want to
                  detect
                </p>
              </div>
              <Textarea
                value={prompt}
                onChange={e => setPrompt(e.target.value)}
                rows={10}
                className="min-h-[250px] resize-none bg-slate-50 dark:bg-slate-900/50 border-slate-300 dark:border-slate-600 focus:border-purple-500 dark:focus:border-purple-400 focus:ring-purple-500/20 dark:focus:ring-purple-400/20"
                placeholder="Describe your authentication requirements in detail:

Examples:
• Detect authentication method for Supabase service. I need the token endpoint, grant type, and required headers for OAuth2 client credentials flow.

• I'm working with a REST API that uses API key authentication. The API key should be passed in a custom header called 'X-API-Key'. Help me configure this.

• My application needs to authenticate with Azure AD using client credentials. I have a client ID and secret, and need to get access tokens for the Microsoft Graph API.

• The service uses bearer token authentication with a static token that's provided by the service administrator.

Be as specific as possible about your use case, service provider, and any requirements you know about."
              />
            </div>
          )}

          {/* Tips */}
          <div className="p-4 bg-gradient-to-r from-blue-50 to-purple-50 dark:from-blue-950/30 dark:to-purple-950/30 rounded-lg border border-blue-200/50 dark:border-blue-800/50">
            <div className="flex gap-3">
              <Zap className="w-5 h-5 text-blue-600 dark:text-blue-400 flex-shrink-0 mt-0.5" />
              <div className="text-sm">
                <p className="font-medium text-blue-900 dark:text-blue-100 mb-1">
                  AI Detection Tips
                </p>
                <p className="text-blue-700 dark:text-blue-300">
                  The AI will analyze your input to identify authentication
                  patterns, extract endpoint URLs, determine grant types, and
                  suggest configuration parameters. The more detailed your
                  input, the better the detection results.
                </p>
              </div>
            </div>
          </div>
        </div>

        <DialogFooter className="gap-2 pt-4 border-t dark:border-slate-700">
          <Button
            variant="ghost"
            onClick={() => setOpen(false)}
            disabled={loading}
            className="hover:bg-slate-100 dark:hover:bg-slate-800"
          >
            Cancel
          </Button>
          {mode === 'code' ? (
            <Button
              onClick={submitCode}
              disabled={loading || !code.trim()}
              className="bg-gradient-to-r from-purple-600 to-blue-600 hover:from-purple-700 hover:to-blue-700 dark:from-purple-500 dark:to-blue-500 dark:hover:from-purple-600 dark:hover:to-blue-600 text-white shadow-lg hover:shadow-xl transition-all duration-200"
              size="default"
            >
              {loading ? (
                <>
                  <Loader2 className="w-4 h-4 animate-spin" />
                  Analyzing Code...
                </>
              ) : (
                <>
                  <Brain className="w-4 h-4" />
                  Analyze Code
                </>
              )}
            </Button>
          ) : (
            <Button
              onClick={submitPrompt}
              disabled={loading || !prompt.trim()}
              className="bg-gradient-to-r from-purple-600 to-blue-600 hover:from-purple-700 hover:to-blue-700 dark:from-purple-500 dark:to-blue-500 dark:hover:from-purple-600 dark:hover:to-blue-600 text-white shadow-lg hover:shadow-xl transition-all duration-200"
              size="default"
            >
              {loading ? (
                <>
                  <Loader2 className="w-4 h-4 animate-spin" />
                  Processing...
                </>
              ) : (
                <>
                  <Brain className="w-4 h-4" />
                  Detect Auth
                </>
              )}
            </Button>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
};

export default EnhancedAiDetection;
