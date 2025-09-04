import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import {
  CheckCircle,
  Clock,
  Download,
  FileText,
  FolderOpen,
  GitPullRequest,
  Play,
  XCircle,
} from 'lucide-react';

export interface MessageCard {
  type: 'plan' | 'diff' | 'run' | 'openapi-diff' | 'pr-preview';
  title: string;
  // Optional free-form content used by certain card types (e.g., pr-preview)
  content?: string;
  items?: string[];
  files?: Array<{ path: string; change: 'added' | 'modified'; lines: number }>;
  status?: 'pass' | 'fail' | 'running';
  env?: string;
  duration?: string;
  p95?: number;
  seed?: number;
  passed?: number;
  failed?: number;
}

export interface MessageButton {
  label: string;
  variant: 'primary' | 'secondary';
  loading?: boolean;
}

export interface ChatMessageProps {
  role: 'user' | 'assistant';
  content: string;
  cards?: MessageCard[];
  buttons?: MessageButton[];
  runId?: string;
  onButtonClick?: (label: string) => void;
  onBrowseFiles?: (runId: string) => void;
}

export interface MessageModel {
  role: 'user' | 'assistant';
  content: string;
  cards?: MessageCard[];
  buttons?: MessageButton[];
  runId?: string;
  // Optional original ChapiCard returned from the LLM
  llmCard?: Record<string, unknown>;
}

const getStatusIcon = (status: string) => {
  switch (status) {
    case 'pass':
      return <CheckCircle className="w-4 h-4 text-green-500" />;
    case 'fail':
      return <XCircle className="w-4 h-4 text-red-500" />;
    case 'running':
      return <Clock className="w-4 h-4 text-yellow-500 animate-spin" />;
    default:
      return null;
  }
};

const getChangeIcon = (change: string) => {
  switch (change) {
    case 'added':
      return <span className="text-green-500 font-mono">+</span>;
    case 'modified':
      return <span className="text-yellow-500 font-mono">~</span>;
    default:
      return null;
  }
};

export const ChatMessage = ({
  role,
  content,
  cards,
  buttons,
  runId,
  onButtonClick,
  onBrowseFiles,
}: ChatMessageProps) => {
  // Debug logging
  console.log('ChatMessage props:', {
    role,
    hasContent: !!content,
    cardsCount: cards?.length || 0,
    buttonsCount: buttons?.length || 0,
    hasRunId: !!runId,
    buttons: buttons?.map(b => b.label),
  });

  if (role === 'user') {
    return (
      <div className="flex justify-end mb-4 sm:mb-6">
        <div className="max-w-[85%] sm:max-w-3xl">
          <div className="bg-primary text-primary-foreground rounded-2xl px-3 sm:px-4 py-2 sm:py-3 text-sm">
            {content}
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="flex justify-start mb-4 sm:mb-6">
      <div className="max-w-full sm:max-w-4xl w-full">
        {/* Assistant Avatar */}
        <div className="flex items-start gap-2 sm:gap-3 mb-2 sm:mb-3">
          <div className="w-6 h-6 sm:w-8 sm:h-8 rounded-full bg-accent/10 flex items-center justify-center shrink-0">
            <span className="text-accent font-medium text-xs sm:text-sm">
              C
            </span>
          </div>
          <div className="font-medium text-xs sm:text-sm text-muted-foreground">
            Chapi
          </div>
        </div>

        {/* Message Content */}
        <div className="ml-8 sm:ml-11">
          <div className="prose prose-sm max-w-none mb-3 sm:mb-4 text-foreground">
            <p className="text-sm leading-relaxed">{content}</p>
          </div>

          {/* Cards */}
          {cards && cards.length > 0 && (
            <div className="space-y-3 sm:space-y-4 mb-3 sm:mb-4">
              {cards.map((card, idx) => (
                <Card key={idx} className="border-border">
                  <CardHeader className="pb-2 sm:pb-3 px-3 sm:px-6 pt-3 sm:pt-6">
                    <CardTitle className="text-xs sm:text-sm flex items-center gap-2 flex-wrap">
                      {card.type === 'plan' && (
                        <FileText className="w-3 h-3 sm:w-4 sm:h-4 flex-shrink-0" />
                      )}
                      {card.type === 'diff' && (
                        <GitPullRequest className="w-3 h-3 sm:w-4 sm:h-4 flex-shrink-0" />
                      )}
                      {card.type === 'run' && getStatusIcon(card.status || '')}
                      <span className="min-w-0 flex-1">{card.title}</span>
                      {card.status && (
                        <Badge
                          variant={
                            card.status === 'pass' ? 'default' : 'destructive'
                          }
                          className="text-xs"
                        >
                          {card.status}
                        </Badge>
                      )}
                    </CardTitle>
                  </CardHeader>
                  <CardContent className="pt-0 px-3 sm:px-6 pb-3 sm:pb-6">
                    {card.type === 'plan' && card.items && (
                      <ul className="space-y-1 text-xs sm:text-sm text-muted-foreground">
                        {card.items.map((item, itemIdx) => (
                          <li key={itemIdx} className="flex items-start gap-2">
                            <div className="w-1.5 h-1.5 rounded-full bg-accent shrink-0 mt-1.5"></div>
                            <span className="leading-relaxed">{item}</span>
                          </li>
                        ))}
                      </ul>
                    )}

                    {card.type === 'diff' && card.files && (
                      <div className="space-y-2">
                        {card.files.map((file, fileIdx) => (
                          <div
                            key={fileIdx}
                            className="flex items-center gap-2 sm:gap-3 text-xs sm:text-sm font-mono overflow-hidden"
                          >
                            <span className="flex-shrink-0">
                              {getChangeIcon(file.change)}
                            </span>
                            <span className="text-foreground truncate flex-1 min-w-0">
                              {file.path}
                            </span>
                            <Badge
                              variant="outline"
                              className="text-xs flex-shrink-0"
                            >
                              {file.change === 'added' ? '+' : '~'}
                              {file.lines}
                            </Badge>
                          </div>
                        ))}
                      </div>
                    )}

                    {card.type === 'run' && (
                      <div className="grid grid-cols-2 lg:grid-cols-4 gap-3 sm:gap-4 text-xs sm:text-sm">
                        <div>
                          <div className="text-muted-foreground mb-1">
                            Environment
                          </div>
                          <Badge variant="outline" className="text-xs">
                            {card.env}
                          </Badge>
                        </div>
                        <div>
                          <div className="text-muted-foreground mb-1">
                            Duration
                          </div>
                          <div className="font-mono">{card.duration}</div>
                        </div>
                        <div>
                          <div className="text-muted-foreground mb-1">P95</div>
                          <div className="font-mono">{card.p95}ms</div>
                        </div>
                        <div>
                          <div className="text-muted-foreground mb-1">
                            Results
                          </div>
                          <div className="font-mono">
                            <span className="text-green-500">
                              {card.passed}
                            </span>
                            {card.failed ? (
                              <>
                                /
                                <span className="text-red-500">
                                  {card.failed}
                                </span>
                              </>
                            ) : null}
                          </div>
                        </div>
                      </div>
                    )}
                  </CardContent>
                </Card>
              ))}
            </div>
          )}

          {/* Action Buttons */}
          {((buttons && buttons.length > 0) || runId) && (
            <div className="flex flex-wrap gap-1.5 sm:gap-2">
              {buttons?.map((button, idx) => (
                <Button
                  key={idx}
                  variant={button.variant === 'primary' ? 'default' : 'outline'}
                  size="sm"
                  className="text-xs h-8"
                  onClick={() => onButtonClick?.(button.label)}
                  disabled={button.loading}
                >
                  {button.loading ? (
                    <Clock className="w-3 h-3 mr-1 animate-spin" />
                  ) : null}
                  {button.label === 'Run in Cloud' && (
                    <Play className="w-3 h-3 mr-1" />
                  )}
                  {button.label === 'Download Run Pack' && !button.loading && (
                    <Download className="w-3 h-3 mr-1" />
                  )}
                  {button.label === 'Create PR' && (
                    <GitPullRequest className="w-3 h-3 mr-1" />
                  )}
                  {button.label}
                </Button>
              ))}

              {/* Browse Files Button - shown when runId exists */}
              {runId && (
                <Button
                  variant="outline"
                  size="sm"
                  className="text-xs"
                  onClick={() => onBrowseFiles?.(runId)}
                >
                  <FolderOpen className="w-3 h-3 mr-1" />
                  Browse Files
                </Button>
              )}
            </div>
          )}
        </div>
      </div>
    </div>
  );
};
