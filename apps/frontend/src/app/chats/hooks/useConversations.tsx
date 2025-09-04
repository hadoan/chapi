import type { MessageModel } from '@/components/ChatMessage';
import { toast } from '@/hooks/use-toast';
import { chatApi, ConversationDto } from '@/lib/api/chat';
import { llmsApi } from '@/lib/api/llms';
import { runPacksApi } from '@/lib/api/run-packs';
import type { components } from '@/lib/api/schema';
import mockMessages from '@/lib/mock/messages/chat-1.json';
import { useCallback, useEffect, useState } from 'react';

type LlmMessage = MessageModel & {
  llmCard?: components['schemas']['Chapi.AI.Dto.ChapiCard'];
};

export function useConversations(
  selectedProject?: { id?: string },
  selectedEnv?: string
) {
  const [conversations, setConversations] = useState<ConversationDto[]>([]);
  const [currentConversationId, setCurrentConversationId] = useState<
    string | null
  >(null);
  const [messages, setMessages] = useState<MessageModel[]>([]);
  const [loadingConversations, setLoadingConversations] = useState(false);
  const [isNewConversation, setIsNewConversation] = useState(false);

  const refreshConversations = useCallback(async () => {
    if (!selectedProject?.id) return;
    try {
      const conversationList = await chatApi.getConversations(
        selectedProject.id
      );
      setConversations(conversationList);
    } catch (error) {
      console.error('Failed to refresh conversations:', error);
    }
  }, [selectedProject]);

  const loadConversation = useCallback(async (conversationId: string) => {
    try {
      const conversation = await chatApi.getConversation(conversationId);
      setCurrentConversationId(conversationId);
      setIsNewConversation(false);

      const messageModels: MessageModel[] =
        conversation.messages?.map(msg => {
          const hasCardData = msg.cardType && msg.cardPayload;
          let parsedCard: Record<string, unknown> | null = null;
          if (hasCardData) {
            try {
              parsedCard = JSON.parse(msg.cardPayload);
            } catch (e) {
              console.error('Failed to parse card payload', e);
            }
          }

          const isAssistant = msg.role?.toLowerCase() === 'assistant';
          const hasValidRunPackId =
            !!msg.runPackId &&
            msg.runPackId !== '00000000-0000-0000-0000-000000000000';

          const buttons =
            isAssistant && hasCardData
              ? [
                  { label: 'Run in Cloud', variant: 'primary' as const },
                  { label: 'Download Run Pack', variant: 'secondary' as const },
                  ...(hasValidRunPackId
                    ? [{ label: 'Browse Files', variant: 'secondary' as const }]
                    : []),
                  { label: 'Add Negatives', variant: 'secondary' as const },
                ]
              : undefined;

          const validRunPackId =
            msg.runPackId &&
            msg.runPackId !== '00000000-0000-0000-0000-000000000000'
              ? msg.runPackId
              : null;
          const runId = parsedCard?.runId || parsedCard?.id || validRunPackId;

          return {
            id: msg.id,
            role: msg.role as 'user' | 'assistant',
            content: msg.content || '',
            cards: parsedCard ? [parsedCard] : undefined,
            buttons,
            runId,
            runPackId: msg.runPackId,
            llmCard: parsedCard,
          } as unknown as MessageModel;
        }) || [];

      setMessages(messageModels);
    } catch (error) {
      console.error('Failed to load conversation:', error);
      toast({ title: 'Failed to load conversation' });
    }
  }, []);

  const handleNewConversation = useCallback(() => {
    setCurrentConversationId(null);
    setMessages([]);
    setIsNewConversation(true);
    toast({
      title: 'New conversation started',
      description: 'Ready for new conversation.',
    });
  }, []);

  const downloadRunPack = useCallback(
    async (messageModel: MessageModel, selectedProject?: { id?: string }) => {
      // copied minimal logic from ChatView; updates messages locally
      const idx = messages.indexOf(messageModel as MessageModel);
      try {
        toast({ title: 'Preparing run pack...' });
        if (idx >= 0) {
          // set a temporary loading badge on the message
          setMessages(prev => {
            const copy = [...prev];
            type BtnLoading = {
              label: string;
              variant: string;
              loading?: boolean;
            };
            const m = { ...copy[idx] } as LlmMessage & {
              buttons?: Array<BtnLoading>;
            };
            m.buttons = [
              { label: 'Downloading...', variant: 'secondary', loading: true },
            ];
            copy[idx] = m;
            return copy;
          });
        }

        const lm = messageModel as LlmMessage;
        const generateRequest = {
          projectId: selectedProject?.id ?? '',
          card: lm.llmCard,
          userQuery: messageModel.content,
          environment: selectedEnv ?? 'local',
          conversationId: currentConversationId || undefined,
          messageId: messageModel.id,
        };

        const result = await runPacksApi.generate(generateRequest);

        if (idx >= 0 && result.runId) {
          setMessages(prev => {
            const copy = [...prev];
            const m = { ...copy[idx] } as LlmMessage & {
              runId?: string;
              runPackId?: string;
            };
            m.runId = result.runId;
            m.runPackId = result.runPackId;
            copy[idx] = m;
            return copy;
          });
        }

        const url = URL.createObjectURL(result.blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = 'chapi-run-pack.zip';
        document.body.appendChild(a);
        a.click();
        a.remove();
        URL.revokeObjectURL(url);
        toast({
          title: `Run pack downloaded (ID: ${result.runId.substring(0, 8)}...)`,
        });
      } catch (err) {
        console.error('Download failed', err);
        toast({ title: 'Failed to download run pack' });
      } finally {
        if (idx >= 0) {
          setMessages(prev => {
            const copy = [...prev];
            type Btn = { label: string; variant: string };
            const m = { ...copy[idx] } as LlmMessage & { buttons?: Array<Btn> };
            const hasLl = !!m.llmCard;
            m.buttons = hasLl
              ? [
                  { label: 'Run in Cloud', variant: 'primary' },
                  { label: 'Download Run Pack', variant: 'secondary' },
                  { label: 'Browse Files', variant: 'secondary' },
                  { label: 'Add Negatives', variant: 'secondary' },
                ]
              : [];
            copy[idx] = m;
            return copy;
          });
        }
      }
    },
    [messages, currentConversationId, selectedEnv]
  );

  const addNegatives = useCallback(
    async (
      messageModel: MessageModel,
      selectedProject?: { name?: string; id?: string }
    ) => {
      try {
        toast({ title: 'Adding negative tests...' });
        const req = {
          user_query: `${
            selectedProject?.name ?? ''
          } Add 3 negative tests for the generated suite`,
          projectId: selectedProject?.id ?? '',
          max_files: 3,
          openApiJson: null,
        };
        const negCard = await llmsApi.generate(req);
        const assistantMessage: MessageModel = {
          role: 'assistant',
          content: negCard.heading ?? 'Added negative tests',
          cards: negCard.files
            ? [
                {
                  type: 'diff',
                  title: negCard.heading ?? 'Negative Tests',
                  files: negCard.files.map(
                    (f: { path?: string; addedLines?: number }) => ({
                      path: f.path ?? '',
                      change: 'added' as const,
                      lines: f.addedLines ?? 0,
                    })
                  ),
                },
              ]
            : undefined,
        } as MessageModel;

        setMessages(prev => [...prev, assistantMessage]);
        toast({ title: 'Negative tests added' });
      } catch (err) {
        console.error('Add negatives failed', err);
        toast({ title: 'Failed to add negatives' });
      }
    },
    []
  );

  useEffect(() => {
    if (!selectedProject?.id) return;

    let mounted = true;
    setLoadingConversations(true);

    chatApi
      .getConversations(selectedProject.id)
      .then(async conversationList => {
        if (!mounted) return;
        setConversations(conversationList);

        if (
          conversationList.length > 0 &&
          !currentConversationId &&
          !isNewConversation
        ) {
          const sortedConversations = [...conversationList].sort((a, b) => {
            const dateA = new Date(a.updatedAt || a.createdAt || '').getTime();
            const dateB = new Date(b.updatedAt || b.createdAt || '').getTime();
            return dateB - dateA;
          });
          const latestConversation = sortedConversations[0];
          if (latestConversation.id) {
            await loadConversation(latestConversation.id);
          }
        } else if (conversationList.length === 0 || isNewConversation) {
          setMessages([]);
          setCurrentConversationId(null);
        }
      })
      .catch(() => {
        if (mounted) {
          toast({ title: 'Failed to load conversations' });
          setMessages(mockMessages as MessageModel[]);
        }
      })
      .finally(() => {
        if (mounted) setLoadingConversations(false);
      });

    return () => {
      mounted = false;
    };
  }, [
    selectedProject,
    currentConversationId,
    loadConversation,
    isNewConversation,
  ]);

  return {
    conversations,
    currentConversationId,
    messages,
    loadingConversations,
    isNewConversation,
    setMessages,
    setCurrentConversationId,
    setIsNewConversation,
    refreshConversations,
    loadConversation,
    handleNewConversation,
    downloadRunPack,
    addNegatives,
  };
}
