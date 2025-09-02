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
  if (role === 'user') {
    return (
      <div className="flex justify-end mb-6">
        <div className="max-w-3xl">
          <div className="bg-primary text-primary-foreground rounded-2xl px-4 py-3 text-sm">
            {content}
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="flex justify-start mb-6">
      <div className="max-w-4xl w-full">
        {/* Assistant Avatar */}
        <div className="flex items-start gap-3 mb-3">
          <div className="w-8 h-8 rounded-full bg-accent/10 flex items-center justify-center shrink-0">
            <span className="text-accent font-medium text-sm">C</span>
          </div>
          <div className="font-medium text-sm text-muted-foreground">Chapi</div>
        </div>

        {/* Message Content */}
        <div className="ml-11">
          <div className="prose prose-sm max-w-none mb-4 text-foreground">
            <p>{content}</p>
          </div>

          {/* Cards */}
          {cards && cards.length > 0 && (
            <div className="space-y-4 mb-4">
              {cards.map((card, idx) => (
                <Card key={idx} className="border-border">
                  <CardHeader className="pb-3">
                    <CardTitle className="text-sm flex items-center gap-2">
                      {card.type === 'plan' && <FileText className="w-4 h-4" />}
                      {card.type === 'diff' && (
                        <GitPullRequest className="w-4 h-4" />
                      )}
                      {card.type === 'run' && getStatusIcon(card.status || '')}
                      {card.title}
                      {card.status && (
                        <Badge
                          variant={
                            card.status === 'pass' ? 'default' : 'destructive'
                          }
                          className="ml-auto"
                        >
                          {card.status}
                        </Badge>
                      )}
                    </CardTitle>
                  </CardHeader>
                  <CardContent className="pt-0">
                    {card.type === 'plan' && card.items && (
                      <ul className="space-y-1 text-sm text-muted-foreground">
                        {card.items.map((item, itemIdx) => (
                          <li key={itemIdx} className="flex items-center gap-2">
                            <div className="w-1.5 h-1.5 rounded-full bg-accent shrink-0"></div>
                            {item}
                          </li>
                        ))}
                      </ul>
                    )}

                    {card.type === 'diff' && card.files && (
                      <div className="space-y-2">
                        {card.files.map((file, fileIdx) => (
                          <div
                            key={fileIdx}
                            className="flex items-center gap-3 text-sm font-mono"
                          >
                            {getChangeIcon(file.change)}
                            <span className="text-foreground">{file.path}</span>
                            <Badge
                              variant="outline"
                              className="ml-auto text-xs"
                            >
                              {file.change === 'added' ? '+' : '~'}
                              {file.lines}
                            </Badge>
                          </div>
                        ))}
                      </div>
                    )}

                    {card.type === 'run' && (
                      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
                        <div>
                          <div className="text-muted-foreground">
                            Environment
                          </div>
                          <Badge variant="outline">{card.env}</Badge>
                        </div>
                        <div>
                          <div className="text-muted-foreground">Duration</div>
                          <div className="font-mono">{card.duration}</div>
                        </div>
                        <div>
                          <div className="text-muted-foreground">P95</div>
                          <div className="font-mono">{card.p95}ms</div>
                        </div>
                        <div>
                          <div className="text-muted-foreground">Results</div>
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
            <div className="flex flex-wrap gap-2">
              {buttons?.map((button, idx) => (
                <Button
                  key={idx}
                  variant={button.variant === 'primary' ? 'default' : 'outline'}
                  size="sm"
                  className="text-xs"
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
