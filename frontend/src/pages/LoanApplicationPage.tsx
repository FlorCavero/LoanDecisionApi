import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import { createLoanApplication, ApiError } from "../api/client";
import type { LoanApplicationResponse } from "../api/types";

export function LoanApplicationPage() {
  const { token } = useAuth();
  const navigate = useNavigate();

  const [ssn, setSsn] = useState("");
  const [annualIncome, setAnnualIncome] = useState("");
  const [requestedAmount, setRequestedAmount] = useState("");
  const [dateOfBirth, setDateOfBirth] = useState("");
  const [isKeyedIn, setIsKeyedIn] = useState(false);

  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [created, setCreated] = useState<LoanApplicationResponse | null>(null);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setIsSubmitting(true);
    setCreated(null);
    try {
      const response = await createLoanApplication(token!, {
        ssn,
        annualIncome: Number(annualIncome),
        requestedAmount: Number(requestedAmount),
        dateOfBirth,
        isKeyedIn,
      });
      setCreated(response);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Unable to reach the API.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="page">
      <h1>New Loan Application</h1>

      <div className="two-column">
        <form className="card" onSubmit={handleSubmit}>
          <label>
            SSN (9 digits)
            <input
              value={ssn}
              onChange={(e) => setSsn(e.target.value)}
              placeholder="123456789"
              maxLength={9}
              required
            />
          </label>

          <label>
            Annual income
            <input
              type="number"
              min="0"
              step="0.01"
              value={annualIncome}
              onChange={(e) => setAnnualIncome(e.target.value)}
              required
            />
          </label>

          <label>
            Requested amount
            <input
              type="number"
              min="0.01"
              step="0.01"
              value={requestedAmount}
              onChange={(e) => setRequestedAmount(e.target.value)}
              required
            />
          </label>

          <label>
            Date of birth
            <input
              type="date"
              value={dateOfBirth}
              onChange={(e) => setDateOfBirth(e.target.value)}
              required
            />
          </label>

          <label className="checkbox-row">
            <input
              type="checkbox"
              checked={isKeyedIn}
              onChange={(e) => setIsKeyedIn(e.target.checked)}
            />
            Keyed in by an agent (vs. self-submitted)
          </label>

          {error && <p className="error">{error}</p>}

          <button type="submit" disabled={isSubmitting}>
            {isSubmitting ? "Submitting..." : "Submit application"}
          </button>
        </form>

        {created && (
          <div className="card result-card">
            <h2>Application created</h2>
            <dl>
              <dt>Id</dt>
              <dd><code>{created.id}</code></dd>
              <dt>Status</dt>
              <dd><span className={`status-pill status-${created.status.toLowerCase()}`}>{created.status}</span></dd>
              <dt>SSN</dt>
              <dd>{created.ssn}</dd>
              <dt>Credit score</dt>
              <dd>{created.creditScore === 0 ? "Not yet scored" : created.creditScore}</dd>
            </dl>
            <button
              type="button"
              onClick={() => navigate("/evaluate", { state: { loanApplicationId: created.id } })}
            >
              Continue to evaluation
            </button>
          </div>
        )}
      </div>
    </div>
  );
}
