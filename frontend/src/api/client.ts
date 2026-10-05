import type {
  TokenRequest,
  TokenResponse,
  LoanApplicationCreateRequest,
  LoanApplicationResponse,
  LoanApplicationEvaluationTextRequest,
  LoanDecisionResponse,
  RuleGroupCreateRequest,
  RuleGroupResponse,
} from "./types";

const BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5227";

export class ApiError extends Error {
  status: number;

  constructor(status: number, message: string) {
    super(message);
    this.status = status;
  }
}

// The API returns different shapes for different failure types (a plain 401 with
// no body, a 404 with no body, a 422 with a JSON string[] of validation errors) -
// this normalizes all of them into one readable message.
async function extractErrorMessage(response: Response): Promise<string> {
  if (response.status === 401) return "Invalid partner name or secret.";
  if (response.status === 404) return "Not found.";

  try {
    const body = await response.json();
    if (Array.isArray(body)) return body.join(" ");
    if (Array.isArray(body?.errors)) return body.errors.join(" ");
    if (typeof body?.title === "string") return body.title;
  } catch {
    // Response body wasn't JSON (or was empty) - fall through to the generic message.
  }

  return `Request failed (${response.status}).`;
}

async function request<TResponse>(
  path: string,
  options: { method: string; token?: string; body?: unknown },
): Promise<TResponse> {
  const headers: Record<string, string> = { "Content-Type": "application/json" };
  if (options.token) headers.Authorization = `Bearer ${options.token}`;

  const response = await fetch(`${BASE_URL}${path}`, {
    method: options.method,
    headers,
    body: options.body !== undefined ? JSON.stringify(options.body) : undefined,
  });

  if (!response.ok) {
    throw new ApiError(response.status, await extractErrorMessage(response));
  }

  if (response.status === 204) return undefined as TResponse;
  return (await response.json()) as TResponse;
}

export function login(body: TokenRequest): Promise<TokenResponse> {
  return request<TokenResponse>("/api/auth/token", { method: "POST", body });
}

export function createLoanApplication(
  token: string,
  body: LoanApplicationCreateRequest,
): Promise<LoanApplicationResponse> {
  return request<LoanApplicationResponse>("/api/LoanApplication", { method: "POST", token, body });
}

export function evaluateLoanApplication(
  token: string,
  loanApplicationId: string,
  body: LoanApplicationEvaluationTextRequest,
): Promise<LoanDecisionResponse> {
  return request<LoanDecisionResponse>(`/api/evaluate/${loanApplicationId}`, {
    method: "POST",
    token,
    body,
  });
}

export function listRuleGroups(token: string): Promise<RuleGroupResponse[]> {
  return request<RuleGroupResponse[]>("/api/RuleGroup", { method: "GET", token });
}

export function createRuleGroup(
  token: string,
  body: RuleGroupCreateRequest,
): Promise<RuleGroupResponse> {
  return request<RuleGroupResponse>("/api/RuleGroup", { method: "POST", token, body });
}
