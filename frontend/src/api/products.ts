import type { ProductDto } from '../types/ProductDto';
import { request } from './client';

export type CreateProductDto = Omit<ProductDto, 'id'>;
export type UpdateProductDto = Omit<ProductDto, 'id'>;

export async function getProducts(signal?: AbortSignal): Promise<ProductDto[]> {
    
    return await request<ProductDto[]>('/Products', { signal });
    
    /*const response = await fetch(`${API_BASE_URL}/Products`, { signal });
    if (!response.ok) {
        throw new Error(`Failed to fetch products: ${response.status} ${response.statusText}`);
    }
    const data = await response.json();
    return data;*/
}

export async function getProductById(id: number, signal?: AbortSignal): Promise<ProductDto> {
    return await request<ProductDto>(`/Products/${id}`, { signal });
}

export async function addProduct(product: CreateProductDto, signal?: AbortSignal): Promise<ProductDto>{
    return await request<ProductDto>('/Products/Add', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify(product),
        signal,
    });
}

export async function updateProduct(id: number, product: UpdateProductDto, signal?: AbortSignal) {
    return await request<void>(`/Products/Update/${id}`, {
        method: 'PUT',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify(product),
        signal,
    });
}

export async function deleteProduct(id: number, signal?: AbortSignal) {
    return await request<void>(`/Products/Delete/${id}`, {
        method: 'DELETE',
        signal,
    });
}

