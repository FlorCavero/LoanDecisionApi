// Mirrors the backend DTOs in Models/DTO exactly - keep these in sync by hand,
// there's no shared schema generation between the two projects.

export interface TokenRequest {
  name: string;
  secret: string;
}

export interface TokenResponse {
  accessToken: string;
  expiresAt: string;
}

export interface LoanApplicationCreateRequest {
  ssn: string;
  annualIncome: number;
  requestedAmount: number;
  dateOfBirth: string; // yyyy-MM-dd
  isKeyedIn?: boolean;
}

export type LoanStatus =
  | "Entered"
  | "PendingReview"
  | "PreApproved"
  | "Approved"
  | "Denied";

export interface LoanApplicationResponse {
  id: string;
  createdAt: string;
  ssn: string; // masked by the backend - only the last 4 digits are ever visible
  annualIncome: number;
  requestedAmount: number;
  creditScore: number;
  status: LoanStatus;
}

export interface LoanApplicationEvaluationTextRequest {
  text: string;
  ruleGroupIds?: string[];
}

export type EvaluationOutcome = "Passed" | "Failed" | "Skipped";

export interface RuleEvaluationResult {
  ruleId: string;
  dataPoint: string;
  outcome: EvaluationOutcome;
  details: string;
}

export interface RuleGroupEvaluationResult {
  ruleGroupId: string;
  name: string;
  groupingType: string;
  outcome: EvaluationOutcome;
  ruleResults: RuleEvaluationResult[];
  childResults: RuleGroupEvaluationResult[];
}

export interface LoanDecisionResponse {
  loanApplicationId: string;
  status: LoanStatus;
  groupResults: RuleGroupEvaluationResult[];
}

export interface ApiErrorBody {
  errors?: string[];
  title?: string;
}
