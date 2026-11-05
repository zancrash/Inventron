import type { ItemDto, PagedResult } from '../types/ItemDto';
import { request } from './client';

export type CreateItemDto = Omit<ItemDto, 'id'>;

export interface ItemQuery {
    search?: string;
    page?: number;
    pageSize?: number;
}

export async function getItems(query: ItemQuery = {}, signal?: AbortSignal): Promise<PagedResult<ItemDto>> {
    const params = new URLSearchParams();
    if (query.search) params.set('search', query.search);
    if (query.page) params.set('page', String(query.page));
    if (query.pageSize) params.set('pageSize', String(query.pageSize));

    const queryString = params.toString();
    return await request<PagedResult<ItemDto>>(`/Items${queryString ? `?${queryString}` : ''}`, { signal });
}

export async function getItemById(id: number, signal?: AbortSignal): Promise<ItemDto> {
    return await request<ItemDto>(`/Items/${id}`, { signal });
}

export async function addItem(item: CreateItemDto, signal?: AbortSignal): Promise<ItemDto> {
    return await request<ItemDto>('/Items/Add', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify(item),
        signal,
    });
}

export async function updateItem(id: number, item: ItemDto, signal?: AbortSignal) {
    return await request<void>(`/Items/${id}`, {
        method: 'PUT',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify(item),
        signal,
    });
}

export async function deleteItem(id: number, signal?: AbortSignal) {
    return await request<void>(`/Items/${id}`, {
        method: 'DELETE',
        signal,
    });
}
