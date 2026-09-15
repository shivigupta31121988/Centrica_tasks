const API_BASE_URL = process.env.REACT_APP_API_BASE_URL ?? "https://localhost:5001";

export class ApiError extends Error {
  constructor(public status: number, message: string) {
    super(message);
  }
}

interface RequestOptions {
  method?: "GET" | "POST";
  body?: unknown;
  token?: string | null;
}

/**
 * Every request goes through this single function, so auth headers and
 * error handling are consistent everywhere - no endpoint can accidentally
 * skip attaching the token or silently swallow a failed response.
 */
export async function apiRequest<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const headers: Record<string, string> = { "Content-Type": "application/json" };
  if (options.token) {
    headers["Authorization"] = `Bearer ${options.token}`;
  }

  const response = await fetch(`${API_BASE_URL}${path}`, {
    method: options.method ?? "GET",
    headers,
    body: options.body ? JSON.stringify(options.body) : undefined,
  });

  if (!response.ok) {
    let message = `Request failed with status ${response.status}`;
    try {
      const problem = await response.json();
      message = problem.message ?? message;
    } catch {
      // Response body wasn't JSON - keep the generic message above.
    }
    throw new ApiError(response.status, message);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}
