const API_BASE_URL = '/api';

// RFC 7807 problem details, as returned by ASP.NET Core. `errors` is present on validation failures.
export interface ProblemDetails {
    type?: string;
    title?: string;
    status?: number;
    detail?: string;
    errors?: Record<string, string[]>;
}

export class ApiError extends Error {
    readonly status: number;
    readonly problem: ProblemDetails | null;

    constructor(status: number, statusText: string, problem: ProblemDetails | null) {
        super(problem?.title ?? `Request failed: ${status} ${statusText}`);
        this.name = 'ApiError';
        this.status = status;
        this.problem = problem;
    }
}

async function readProblem(response: Response): Promise<ProblemDetails | null> {
    const contentType = response.headers.get('Content-Type') ?? '';
    if (!contentType.includes('json')) return null;
    try {
        return await response.json();
    } catch {
        return null;
    }
}

export async function request<T>(path: string, init?: RequestInit): Promise<T>
{
    const response = await fetch(`${API_BASE_URL}${path}`, init);
    if (!response.ok) {
        throw new ApiError(response.status, response.statusText, await readProblem(response));
    }

    if (response.status === 204) {
        return undefined as T;
    }

    return await response.json();
}
