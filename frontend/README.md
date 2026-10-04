# LoanDecisionApi frontend

A small React + TypeScript (Vite) UI over the LoanDecisionApi backend: partner sign-in, a page to submit a new loan application, and an evaluation page that accepts free-text credit-profile data and shows the resulting decision and rule-by-rule breakdown.

## Running

1. Start the backend first (from the repo root): `dotnet run --launch-profile http` — defaults to `http://localhost:5227`.
2. `npm install`
3. `npm run dev` — opens on `http://localhost:5173` by default.

To point at a different backend (e.g. the Azure deployment), copy `.env.example` to `.env.local` and set `VITE_API_BASE_URL`.

## Notes

- Sign in with the dev-seeded partner: `test-partner` / `local-dev-secret-please-change` (only seeded when the backend runs in the `Development` environment).
- The token is kept in `sessionStorage`, not `localStorage` - it disappears when the tab closes, matching the backend's 1-hour JWT lifetime.
- The backend's CORS policy (`Program.cs`) only allows `http://localhost:5173` and `http://localhost:3000` - update it if this runs from a different origin.
- The evaluation page omits `ruleGroupIds` by default, so it evaluates against every rule group that exists in the database - there's no rule-group management UI here.
