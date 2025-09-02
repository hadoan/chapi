import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { ScrollArea } from '@/components/ui/scroll-area';
import type { ConversationDto } from '@/lib/api/chat';
import {
  CheckCircle,
  Edit,
  MoreHorizontal,
  Pin,
  Plus,
  Trash2,
  XCircle,
} from 'lucide-react';
import { useState } from 'react';

interface HistoryListProps {
  conversations: ConversationDto[];
  currentConversationId: string | null;
  onConversationSelect: (conversationId: string) => void;
  onNewConversation: () => void;
  loading?: boolean;
}

const getStatusIcon = (status: string) => {
  return status === 'pass' ? (
    <CheckCircle className="w-3 h-3 text-green-500" />
  ) : (
    <XCircle className="w-3 h-3 text-red-500" />
  );
};

const formatTime = (dateString: string) => {
  const date = new Date(dateString);
  const now = new Date();
  const diffHours = Math.floor(
    (now.getTime() - date.getTime()) / (1000 * 60 * 60)
  );

  if (diffHours < 24) {
    return `${diffHours}h ago`;
  }
  return date.toLocaleDateString();
};

const groupConversations = (conversations: ConversationDto[]) => {
  const today = new Date().toDateString();
  const yesterday = new Date(Date.now() - 86400000).toDateString();

  const groups = {
    today: [] as ConversationDto[],
    recent: [] as ConversationDto[],
    older: [] as ConversationDto[],
  };

  conversations.forEach(conversation => {
    const itemDate = new Date(
      conversation.updatedAt || conversation.createdAt || ''
    ).toDateString();
    if (itemDate === today) {
      groups.today.push(conversation);
    } else if (itemDate === yesterday) {
      groups.recent.push(conversation);
    } else {
      groups.older.push(conversation);
    }
  });

  return groups;
};

export function HistoryList({
  conversations,
  currentConversationId,
  onConversationSelect,
  onNewConversation,
  loading = false,
}: HistoryListProps) {
  const [selectedItems, setSelectedItems] = useState<Set<string>>(new Set());

  const groups = groupConversations(conversations);

  const renderGroup = (title: string, items: ConversationDto[]) => {
    if (items.length === 0) return null;

    return (
      <div className="mb-6">
        <h3 className="text-xs font-medium text-muted-foreground mb-2 px-3">
          {title}
        </h3>
        <div className="space-y-1">
          {items.map(conversation => (
            <div
              key={conversation.id}
              className={`group relative mx-2 rounded-lg p-3 cursor-pointer transition-colors hover:bg-accent ${
                currentConversationId === conversation.id ? 'bg-accent' : ''
              }`}
              onClick={() => onConversationSelect(conversation.id || '')}
            >
              <div className="flex items-start justify-between">
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 mb-1">
                    <h4 className="text-sm font-medium truncate">
                      {conversation.title || 'Untitled Conversation'}
                    </h4>
                  </div>
                  <div className="flex items-center gap-2 text-xs text-muted-foreground">
                    <span>
                      {formatTime(
                        conversation.updatedAt || conversation.createdAt || ''
                      )}
                    </span>
                    <span>•</span>
                    <span>{conversation.messages?.length || 0} messages</span>
                  </div>
                </div>
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button
                      variant="ghost"
                      size="sm"
                      className="h-6 w-6 p-0 opacity-0 group-hover:opacity-100"
                    >
                      <MoreHorizontal className="w-3 h-3" />
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="end" className="w-40">
                    <DropdownMenuItem>
                      <Pin className="w-3 h-3 mr-2" />
                      Pin
                    </DropdownMenuItem>
                    <DropdownMenuItem>
                      <Edit className="w-3 h-3 mr-2" />
                      Rename
                    </DropdownMenuItem>
                    <DropdownMenuItem className="text-destructive">
                      <Trash2 className="w-3 h-3 mr-2" />
                      Delete
                    </DropdownMenuItem>
                  </DropdownMenuContent>
                </DropdownMenu>
              </div>
            </div>
          ))}
        </div>
      </div>
    );
  };

  return (
    <div className="w-full h-full flex flex-col">
      {/* Header */}
      <div className="flex items-center justify-between p-4 border-b">
        <div className="flex items-center gap-2">
          <h2 className="text-lg font-semibold">Chat History</h2>
        </div>
        <Button size="sm" onClick={onNewConversation} disabled={loading}>
          <Plus className="w-4 h-4" />
        </Button>
      </div>

      {/* Content */}
      <ScrollArea className="flex-1">
        <div className="p-2">
          {loading ? (
            <div className="flex items-center justify-center p-8">
              <div className="text-sm text-muted-foreground">
                Loading conversations...
              </div>
            </div>
          ) : conversations.length === 0 ? (
            <div className="flex flex-col items-center justify-center p-8 text-center">
              <div className="text-sm text-muted-foreground mb-2">
                No conversations yet
              </div>
              <div className="text-xs text-muted-foreground">
                Start a new conversation to see it here
              </div>
            </div>
          ) : (
            <>
              {renderGroup('Today', groups.today)}
              {renderGroup('Recent', groups.recent)}
              {renderGroup('Older', groups.older)}
            </>
          )}
        </div>
      </ScrollArea>

      {/* Footer */}
      <div className="p-4 border-t">
        <div className="text-xs text-muted-foreground text-center">
          All Conversations
        </div>
      </div>
    </div>
  );
}
