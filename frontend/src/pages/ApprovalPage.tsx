import { useState, type FormEvent } from "react";
import { useLocation } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import { evaluateLoanApplication, ApiError } from "../api/client";
import type { LoanDecisionResponse, RuleGroupEvaluationResult } from "../api/types";

function OutcomeBadge({ outcome }: { outcome: string }) {
  return <span className={`outcome-badge outcome-${outcome.toLowerCase()}`}>{outcome}</span>;
}

// A RuleGroup can nest child RuleGroups arbitrarily deep (see RuleEvaluator.cs) -
// this mirrors that recursively rather than assuming a fixed depth.
function RuleGroupResultView({ group }: { group: RuleGroupEvaluationResult }) {
  return (
    <div className="rule-group">
      <div className="rule-group-header">
        <strong>{group.name}</strong>
        <span className="grouping-label">{group.groupingType} of:</span>
        <OutcomeBadge outcome={group.outcome} />
      </div>

      {group.ruleResults.length > 0 && (
        <ul className="rule-list">
          {group.ruleResults.map((rule) => (
            <li key={rule.ruleId}>
              <OutcomeBadge outcome={rule.outcome} />
              <span>{rule.details}</span>
            </li>
          ))}
        </ul>
      )}

      {group.childResults.length > 0 && (
        <div className="child-groups">
          {group.childResults.map((child) => (
            <RuleGroupResultView key={child.ruleGroupId} group={child} />
          ))}
        </div>
      )}
    </div>
  );
}

export function ApprovalPage() {
  const { token } = useAuth();
  const location = useLocation();
  const prefillId = (location.state as { loanApplicationId?: string } | null)?.loanApplicationId;

  const [loanApplicationId, setLoanApplicationId] = useState(prefillId ?? "");
  const [text, setText] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [result, setResult] = useState<LoanDecisionResponse | null>(null);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setIsSubmitting(true);
    setResult(null);
    try {
      const response = await evaluateLoanApplication(token!, loanApplicationId, { text });
      setResult(response);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Unable to reach the API.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="page">
      <h1>Evaluate Application</h1>

      <form className="card" onSubmit={handleSubmit}>
        <label>
          Loan application Id
          <input
            value={loanApplicationId}
            onChange={(e) => setLoanApplicationId(e.target.value)}
            placeholder="00000000-0000-0000-0000-000000000000"
            required
          />
        </label>

        <label>
          Credit profile text
          <textarea
            value={text}
            onChange={(e) => setText(e.target.value)}
            rows={8}
            placeholder="Paste or type a free-text credit profile - e.g. a pay stub summary or credit report narrative. Claude will extract the structured fields from this."
            required
          />
        </label>

        {error && <p className="error">{error}</p>}

        <button type="submit" disabled={isSubmitting}>
          {isSubmitting ? "Evaluating..." : "Evaluate"}
        </button>
      </form>

      {result && (
        <div className="card result-card">
          <h2>
            Decision:{" "}
            <span className={`status-pill status-${result.status.toLowerCase()}`}>
              {result.status}
            </span>
          </h2>
          {result.groupResults.map((group) => (
            <RuleGroupResultView key={group.ruleGroupId} group={group} />
          ))}
        </div>
      )}
    </div>
  );
}
