import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import { ApiError } from "../api/client";

export function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [name, setName] = useState("");
  const [secret, setSecret] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setIsSubmitting(true);
    try {
      await login(name, secret);
      navigate("/apply");
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Unable to reach the API.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="centered-card">
      <form className="card" onSubmit={handleSubmit}>
        <h1>Partner Sign In</h1>
        <p className="muted">Authenticate as an API partner to continue.</p>

        <label>
          Partner name
          <input value={name} onChange={(e) => setName(e.target.value)} required autoFocus />
        </label>

        <label>
          Secret
          <input
            type="password"
            value={secret}
            onChange={(e) => setSecret(e.target.value)}
            required
          />
        </label>

        {error && <p className="error">{error}</p>}

        <button type="submit" disabled={isSubmitting}>
          {isSubmitting ? "Signing in..." : "Sign in"}
        </button>

        <p className="muted small">
          Local dev default seed: <code>test-partner</code> /{" "}
          <code>local-dev-secret-please-change</code>
        </p>
      </form>
    </div>
  );
}
