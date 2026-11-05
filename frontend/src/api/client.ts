const API_BASE_URL = '/api';

export async function request<T>(path: string, init?: RequestInit): Promise<T>
{
    const response = await fetch(`${API_BASE_URL}${path}`, init);
    if (!response.ok) {
        throw new Error(`Failed to fetch data: ${response.status} ${response.statusText}`);
    }

    if (response.status === 204) {
        return undefined as T;
    }

    return await response.json();
}