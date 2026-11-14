import type {
  CreateServiceRequestInput,
  ServiceRequest,
  ServiceRequestStatus,
} from '../models/serviceRequest';

/** RFC 7807 fields the UI may use. Server `detail` text is never shown for 5xx responses. */
export interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
  errors?: Record<string, string[]>;
}

/** Non-success outcome. `status` 0 means the API could not be reached (network/CORS). */
export class ApiError extends Error {
  readonly status: number;
  readonly problem: ProblemDetails | null;

  constructor(status: number, problem: ProblemDetails | null) {
    super(problem?.title ?? `HTTP ${status}`);
    this.name = 'ApiError';
    this.status = status;
    this.problem = problem;
  }
}

export type CreateOutcome = 'created' | 'replayed';

export interface CreateResult {
  outcome: CreateOutcome;
  request: ServiceRequest;
}

const baseUrl = (import.meta.env.VITE_API_BASE_URL ?? '').trim() || window.location.origin;

function requestsUrl(status?: ServiceRequestStatus): string {
  const url = new URL('/api/requests', baseUrl);
  if (status) {
    url.searchParams.set('status', status);
  }
  return url.toString();
}

async function send(url: string, init: RequestInit): Promise<Response> {
  try {
    return await fetch(url, { ...init, headers: { Accept: 'application/json', ...init.headers } });
  } catch (error) {
    if (error instanceof DOMException && error.name === 'AbortError') {
      throw error;
    }
    throw new ApiError(0, null);
  }
}

async function toApiError(response: Response): Promise<ApiError> {
  let problem: ProblemDetails | null = null;
  if (response.headers.get('content-type')?.includes('json')) {
    try {
      problem = (await response.json()) as ProblemDetails;
    } catch {
      problem = null;
    }
  }
  return new ApiError(response.status, problem);
}

/** POST /api/requests — 201 created, 200 equivalent replay; anything else throws {@link ApiError}. */
export async function createRequest(
  input: CreateServiceRequestInput,
  signal?: AbortSignal,
): Promise<CreateResult> {
  const response = await send(requestsUrl(), {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
    signal,
  });

  if (response.status === 201 || response.status === 200) {
    return {
      outcome: response.status === 201 ? 'created' : 'replayed',
      request: (await response.json()) as ServiceRequest,
    };
  }
  throw await toApiError(response);
}

/** GET /api/requests[?status=] — filtering is done by the API. */
export async function getRequests(
  status: ServiceRequestStatus | undefined,
  signal?: AbortSignal,
): Promise<ServiceRequest[]> {
  const response = await send(requestsUrl(status), { method: 'GET', signal });
  if (!response.ok) {
    throw await toApiError(response);
  }
  return (await response.json()) as ServiceRequest[];
}

export function isAbortError(error: unknown): boolean {
  return error instanceof DOMException && error.name === 'AbortError';
}
