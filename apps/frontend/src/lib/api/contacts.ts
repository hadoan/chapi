import axios from 'axios';
import { config } from '../config/app.config';
import { AuthService } from './auth-service';
import type { components } from './schema';

// The OpenAPI schema includes request types for Contacts but the DTO key may be missing
// from the generated schema. Define a minimal ContactDto here that matches the server-side shape.
export type ContactDto = {
  id: string;
  name?: string | null;
  email?: string | null;
  company?: string | null;
  status?: string | null;
  rowVersion?: string | null;
};

export type CreateContactRequest =
  components['schemas']['Contacts.Application.Requests.CreateContactRequest'];
export type UpdateContactRequest =
  components['schemas']['Contacts.Application.Requests.UpdateContactRequest'];

export interface ContactSearchParams {
  page?: number;
  pageSize?: number;
}

export const contactsApi = {
  async getAll(params?: ContactSearchParams): Promise<ContactDto[]> {
    const searchParams = new URLSearchParams();
    if (params?.page) searchParams.append('page', params.page.toString());
    if (params?.pageSize)
      searchParams.append('pageSize', params.pageSize.toString());

    const queryString = searchParams.toString();
    const url = queryString ? `/api/Contact?${queryString}` : '/api/Contact';

    return await AuthService.authenticatedFetch<ContactDto[]>(url, {
      method: 'GET',
    });
  },

  async getById(id: string): Promise<ContactDto> {
    return await AuthService.authenticatedFetch<ContactDto>(
      `/api/Contact/${id}`,
      { method: 'GET' }
    );
  },

  async create(contact: CreateContactRequest): Promise<void> {
    // This endpoint should be anonymous (no auth required). Use anonymous POST to avoid redirect to login.
    await axios.post<void>('/api/Contact', contact, { baseURL: config.apiUrl });
  },

  async update(id: string, contact: UpdateContactRequest): Promise<void> {
    await AuthService.authenticatedFetch<void>(`/api/Contact/${id}`, {
      method: 'PUT',
      data: contact,
    });
  },

  async delete(id: string): Promise<void> {
    await AuthService.authenticatedFetch<void>(`/api/Contact/${id}`, {
      method: 'DELETE',
    });
  },

  async setStatus(id: string, status: string): Promise<void> {
    const url = `/api/Contact/${id}/status`;
    await AuthService.authenticatedFetch<void>(url, {
      method: 'PATCH',
      params: { status },
    });
  },
};
