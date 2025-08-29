import { useState, useEffect } from "react";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Textarea } from "@/components/ui/textarea";
import { Send, Zap, Plus, Play, Download, Menu } from "lucide-react";

const quickChips = [
  { label: "Generate Smoke", icon: Zap },
  { label: "Add 3 Negatives", icon: Plus },
  { label: "Run in Cloud", icon: Play },
  { label: "Download Run Pack", icon: Download },
];

const slashCommands = [
  "/generate",
  "/negatives", 
  "/perf",
  "/import openapi",
  "/curl",
  "/run local",
  "/run cloud",
  "/download run-pack",
  "/explain",
  "/pr preview"
];

interface ChatComposerProps {
  onCommandSelect?: (command: string) => void;
  onMenuOpen?: () => void;
}

export const ChatComposer = ({ onCommandSelect, onMenuOpen }: ChatComposerProps) => {
  const [message, setMessage] = useState("");
  const [showSlashMenu, setShowSlashMenu] = useState(false);

  // Handle Cmd+K to open command palette
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if ((e.metaKey || e.ctrlKey) && e.key === 'k') {
        e.preventDefault();
        onMenuOpen?.();
      }
    };

    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [onMenuOpen]);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (message.trim()) {
      if (message.startsWith('/')) {
        onCommandSelect?.(message);
      }
      // Mock sending message
      console.log("Sending:", message);
      setMessage("");
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      handleSubmit(e);
    }
    
    if (message.startsWith("/")) {
      setShowSlashMenu(true);
    } else {
      setShowSlashMenu(false);
    }
  };

  const handleChipClick = (chipLabel: string) => {
    const command = getCommandFromChip(chipLabel);
    onCommandSelect?.(command);
  };

  const getCommandFromChip = (chipLabel: string) => {
    switch (chipLabel.toLowerCase()) {
      case 'generate smoke': return '/generate smoke';
      case 'add 3 negatives': return '/negatives 3';
      case 'run in cloud': return '/run cloud';
      case 'download run pack': return '/download run-pack';
      default: return chipLabel.toLowerCase();
    }
  };

  return (
    <div className="border-t border-border bg-card p-4">
      {/* Header with Functions Menu */}
      <div className="flex items-center justify-between mb-3">
        <h4 className="text-sm font-medium text-muted-foreground">Quick Actions</h4>
        <Button 
          variant="ghost" 
          size="sm"
          onClick={onMenuOpen}
          className="text-xs"
        >
          <Menu className="w-4 h-4 mr-1" />
          All Functions
        </Button>
      </div>
      {/* Quick Action Chips */}
      <div className="flex flex-wrap gap-2 mb-3">
        {quickChips.map((chip) => (
          <Badge
            key={chip.label}
            variant="outline"
            className="cursor-pointer hover:bg-accent/10 hover:border-accent transition-colors px-3 py-1"
            onClick={() => handleChipClick(chip.label)}
          >
            <chip.icon className="w-3 h-3 mr-1" />
            {chip.label}
          </Badge>
        ))}
      </div>

      {/* Slash Commands Menu */}
      {showSlashMenu && (
        <div className="mb-3 p-3 border border-border rounded-lg bg-muted/50">
          <div className="text-xs text-muted-foreground mb-2">Slash commands</div>
          <div className="flex flex-wrap gap-1">
            {slashCommands
              .filter(cmd => cmd.startsWith(message))
              .map((cmd) => (
                <Badge
                  key={cmd}
                  variant="secondary"
                  className="cursor-pointer hover:bg-accent/10 text-xs"
                  onClick={() => setMessage(cmd + " ")}
                >
                  {cmd}
                </Badge>
              ))}
          </div>
        </div>
      )}

      {/* Message Form */}
      <form onSubmit={handleSubmit} className="space-y-3">
        <div className="relative">
          <Textarea
            value={message}
            onChange={(e) => setMessage(e.target.value)}
            onKeyDown={handleKeyDown}
            placeholder="Describe what you want to test, or use / for commands..."
            className="min-h-[60px] resize-none pr-12 text-sm"
          />
          <Button
            type="submit"
            size="sm"
            className="absolute right-2 bottom-2 h-8 w-8 p-0"
            disabled={!message.trim()}
          >
            <Send className="w-4 h-4" />
          </Button>
        </div>
        
          <div className="flex items-center justify-between text-xs text-muted-foreground">
            <div>
              Press <kbd className="px-1 py-0.5 bg-muted rounded text-xs">Enter</kbd> to send, 
              <kbd className="px-1 py-0.5 bg-muted rounded text-xs ml-1">Shift+Enter</kbd> for new line
            </div>
            <div>
              <kbd className="px-1 py-0.5 bg-muted rounded text-xs">Cmd+K</kbd> for all functions
            </div>
          </div>
      </form>
    </div>
  );
};