import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { ScrollArea } from "@/components/ui/scroll-area";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";
import { Plus, MoreHorizontal, Pin, Trash2, Edit, CheckCircle, XCircle, Filter } from "lucide-react";
import historyData from "@/lib/mock/history.json";

interface HistoryItem {
  id: string;
  title: string;
  updatedAt: string;
  env: string;
  lastStatus: string;
}

const getStatusIcon = (status: string) => {
  return status === 'pass' ? 
    <CheckCircle className="w-3 h-3 text-green-500" /> : 
    <XCircle className="w-3 h-3 text-red-500" />;
};

const formatTime = (dateString: string) => {
  const date = new Date(dateString);
  const now = new Date();
  const diffHours = Math.floor((now.getTime() - date.getTime()) / (1000 * 60 * 60));
  
  if (diffHours < 24) {
    return `${diffHours}h ago`;
  }
  return date.toLocaleDateString();
};

const groupHistory = (items: HistoryItem[]) => {
  const today = new Date().toDateString();
  const yesterday = new Date(Date.now() - 86400000).toDateString();
  
  const groups = {
    today: [] as HistoryItem[],
    recent: [] as HistoryItem[],
    older: [] as HistoryItem[]
  };
  
  items.forEach(item => {
    const itemDate = new Date(item.updatedAt).toDateString();
    if (itemDate === today) {
      groups.today.push(item);
    } else if (itemDate === yesterday) {
      groups.recent.push(item);
    } else {
      groups.older.push(item);
    }
  });
  
  return groups;
};

export const HistoryList = () => {
  const [selectedId, setSelectedId] = useState("chat-1");
  const [filter, setFilter] = useState("all");
  
  const groups = groupHistory(historyData);

  const renderGroup = (title: string, items: HistoryItem[]) => {
    if (items.length === 0) return null;
    
    return (
      <div className="mb-6">
        <h3 className="text-xs font-medium text-muted-foreground mb-2 px-3">{title}</h3>
        <div className="space-y-1">
          {items.map((item) => (
            <div
              key={item.id}
              className={`group relative mx-2 rounded-lg p-3 cursor-pointer transition-colors ${
                selectedId === item.id ? 'bg-accent/10 border border-accent/20' : 'hover:bg-muted/50'
              }`}
              onClick={() => setSelectedId(item.id)}
            >
              <div className="flex items-start justify-between gap-2">
                <div className="flex-1 min-w-0">
                  <div className="font-medium text-sm truncate">{item.title}</div>
                  <div className="flex items-center gap-2 mt-1">
                    <Badge variant="outline" className="text-xs h-5">
                      {item.env}
                    </Badge>
                    <span className="text-xs text-muted-foreground">
                      {formatTime(item.updatedAt)}
                    </span>
                  </div>
                </div>
                
                <div className="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                  {getStatusIcon(item.lastStatus)}
                  <DropdownMenu>
                    <DropdownMenuTrigger asChild>
                      <Button variant="ghost" size="sm" className="h-6 w-6 p-0">
                        <MoreHorizontal className="w-3 h-3" />
                      </Button>
                    </DropdownMenuTrigger>
                    <DropdownMenuContent align="end" className="w-48">
                      <DropdownMenuItem className="text-xs">
                        <Edit className="w-3 h-3 mr-2" />
                        Rename
                      </DropdownMenuItem>
                      <DropdownMenuItem className="text-xs">
                        <Pin className="w-3 h-3 mr-2" />
                        Pin
                      </DropdownMenuItem>
                      <DropdownMenuItem className="text-xs text-red-600">
                        <Trash2 className="w-3 h-3 mr-2" />
                        Delete
                      </DropdownMenuItem>
                    </DropdownMenuContent>
                  </DropdownMenu>
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
    );
  };

  return (
    <div className="h-full flex flex-col">
      {/* Header */}
      <div className="p-4 border-b border-border">
        <Button className="w-full justify-start" size="sm">
          <Plus className="w-4 h-4 mr-2" />
          New Chat
        </Button>
      </div>

      {/* Filter */}
      <div className="p-4 border-b border-border">
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="outline" size="sm" className="w-full justify-start">
              <Filter className="w-4 h-4 mr-2" />
              All Conversations
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent className="w-48">
            <DropdownMenuItem onClick={() => setFilter("all")}>All</DropdownMenuItem>
            <DropdownMenuItem onClick={() => setFilter("runs")}>With Runs</DropdownMenuItem>
            <DropdownMenuItem onClick={() => setFilter("pr")}>With PR</DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>

      {/* History List */}
      <ScrollArea className="flex-1">
        <div className="p-2">
          {renderGroup("Today", groups.today)}
          {renderGroup("Recent", groups.recent)}
          {renderGroup("Older", groups.older)}
        </div>
      </ScrollArea>
    </div>
  );
};