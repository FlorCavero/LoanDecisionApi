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

export type RuleGrouping = "And" | "Or";

export type RuleCondition =
  | "Equals"
  | "DoesNotEqual"
  | "GreaterThan"
  | "GreaterOrEqualTo"
  | "LessThan"
  | "LessOrEqualTo"
  | "In"
  | "NotIn";

// Keep in sync with Rule.cs's DataPointTypes dictionary - it's the backend's own
// source of truth for which data points a rule can reference.
export const DATA_POINTS = [
  "CreditScore",
  "AnnualIncome",
  "RequestedAmount",
  "MonthlyDebtPayments",
  "DebtToIncomeRatio",
  "DateOfBirth",
  "DelinquencyStatus",
  "IsIdentityVerified",
  "IsFraudRiskFlagged",
  "IsCreditFreezeFlagged",
] as const;

export interface RuleCreateRequest {
  dataPoint: string;
  condition: RuleCondition;
  value: string;
}

export interface RuleGroupCreateRequest {
  name: string;
  grouping: RuleGrouping;
  rules: RuleCreateRequest[];
  // Nested child groups aren't buildable from this UI yet - always empty on create.
  childGroups: [];
}

export interface RuleResponse {
  id: string;
  dataPoint: string;
  condition: RuleCondition;
  value: string;
}

export interface RuleGroupResponse {
  id: string;
  createdAt: string;
  name: string;
  grouping: RuleGrouping;
  rules: RuleResponse[];
  childGroups: RuleGroupResponse[];
}
