import { AuthService } from './auth-service';

// Define interfaces for chat-related DTOs
export interface ConversationDto {
  id: string;
  title: string;
  projectId: string;
  createdAt: string;
  updatedAt: string;
  messages: MessageDto[];
}

export interface MessageDto {
  id: string;
  role: string;
  content: string;
  cardType: string;
  cardPayload?: string;
  createdAt: string;
}

export interface CreateConversationRequest {
  title: string;
  projectId: string;
}

export interface AppendMessageRequest {
  conversationId: string;
  role: string;
  content: string;
  cardType?: string;
  cardPayload?: string;
}

export interface SaveDiffAsSuiteRequest {
  conversationId: string;
  runId?: string;
}

export const chatApi = {
  /**
   * Get list of conversations for a project
   */
  async getConversations(
    projectId: string,
    page = 1,
    pageSize = 50
  ): Promise<ConversationDto[]> {
    const searchParams = new URLSearchParams();
    searchParams.append('projectId', projectId);
    searchParams.append('page', page.toString());
    searchParams.append('pageSize', pageSize.toString());

    const url = `/api/chat?${searchParams.toString()}`;
    return await AuthService.authenticatedFetch<ConversationDto[]>(url, {
      method: 'GET',
    });
  },

  /**
   * Get a specific conversation by ID
   */
  async getConversation(id: string): Promise<ConversationDto> {
    return await AuthService.authenticatedFetch<ConversationDto>(
      `/api/chat/${id}`,
      { method: 'GET' }
    );
  },

  /**
   * Create a new conversation
   */
  async createConversation(
    request: CreateConversationRequest
  ): Promise<ConversationDto> {
    return await AuthService.authenticatedFetch<ConversationDto>('/api/chat', {
      method: 'POST',
      data: request,
    });
  },

  /**
   * Append a message to an existing conversation
   */
  async appendMessage(request: AppendMessageRequest): Promise<MessageDto> {
    return await AuthService.authenticatedFetch<MessageDto>(
      '/api/chat/append',
      { method: 'POST', data: request }
    );
  },

  /**
   * Save diff as test suite
   */
  async saveDiffAsSuite(request: SaveDiffAsSuiteRequest): Promise<void> {
    await AuthService.authenticatedFetch<void>('/api/chat/save-diff-suite', {
      method: 'POST',
      data: request,
    });
  },
};
