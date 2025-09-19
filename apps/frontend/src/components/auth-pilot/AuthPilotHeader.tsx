import { FloatingElements } from '@/components/auth-pilot/AnimationEffects';
import { Button } from '@/components/ui/button';
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from '@/components/ui/tooltip';
import { Brain, HelpCircle, Sparkles } from 'lucide-react';

export const AuthPilotHeader = () => {
  return (
    <div className="bg-card border-b border-border sticky top-0 z-10 overflow-hidden">
      <FloatingElements />
      <div className="max-w-7xl mx-auto px-6 py-4 relative z-10">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-4">
            <div className="flex items-center gap-3">
              <div className="p-2 bg-gradient-to-br from-purple-100 to-blue-100 rounded-lg">
                <Brain className="w-6 h-6 text-purple-600" />
              </div>
              <div>
                <h1 className="text-2xl font-bold text-foreground flex items-center gap-2">
                  AI-Powered Auth Discovery
                  <Sparkles className="w-5 h-5 text-purple-500" />
                </h1>
                <p className="text-sm text-muted-foreground">
                  Intelligently detect and configure authentication methods
                </p>
              </div>
            </div>
          </div>

          <div className="flex items-center gap-3">
            <Tooltip>
              <TooltipTrigger asChild>
                <Button variant="ghost" size="sm" className="px-2">
                  <HelpCircle className="h-4 w-4" />
                </Button>
              </TooltipTrigger>
              <TooltipContent>
                <div className="max-w-sm space-y-2">
                  <p className="font-medium">AI Auth Detection:</p>
                  <p className="text-sm">
                    Use AI to analyze code samples or describe authentication
                    requirements
                  </p>
                  <p className="font-medium">Keyboard Shortcuts:</p>
                  <p className="text-sm">Ctrl/Cmd+Enter: Test Connection</p>
                  <p className="text-sm">Ctrl/Cmd+S: Save Profile</p>
                </div>
              </TooltipContent>
            </Tooltip>
          </div>
        </div>
      </div>
    </div>
  );
};
