import { AuthService } from './auth-service';
import type { components } from './schema';

// Use types from the generated schema
export type ConversationDto =
  components['schemas']['Chat.Application.Dtos.ConversationDto'];
export type MessageDto =
  components['schemas']['Chat.Application.Dtos.MessageDto'];
export type CreateConversationRequest =
  components['schemas']['Chat.Application.Requests.CreateConversationRequest'];
export type AppendMessageRequest =
  components['schemas']['Chat.Application.Requests.AppendMessageRequest'];

export type AppendMessagesRequest =
  components['schemas']['Chat.Application.Requests.AppendMessagesRequest'];
export type SaveDiffAsSuiteRequest =
  components['schemas']['Chat.Application.Requests.SaveDiffAsSuiteRequest'];

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
   * Append multiple messages to an existing conversation in a single operation
   */
  async appendMessages(request: AppendMessagesRequest): Promise<MessageDto[]> {
    return await AuthService.authenticatedFetch<MessageDto[]>(
      '/api/chat/append-messages',
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
