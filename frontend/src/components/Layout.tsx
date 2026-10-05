import { NavLink, Outlet, Navigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

export function Layout() {
  const { isAuthenticated, partnerName, logout } = useAuth();

  return (
    <div className="app-shell">
      <header className="top-bar">
        <span className="brand">LoanDecisionApi</span>
        {isAuthenticated && (
          <nav className="top-nav">
            <NavLink to="/apply" className={({ isActive }) => (isActive ? "active" : "")}>
              New Application
            </NavLink>
            <NavLink to="/evaluate" className={({ isActive }) => (isActive ? "active" : "")}>
              Evaluate
            </NavLink>
            <NavLink to="/rule-groups" className={({ isActive }) => (isActive ? "active" : "")}>
              Rule Groups
            </NavLink>
          </nav>
        )}
        {isAuthenticated && (
          <div className="session-info">
            <span>{partnerName}</span>
            <button type="button" onClick={logout} className="link-button">
              Log out
            </button>
          </div>
        )}
      </header>
      <main className="page-content">
        <Outlet />
      </main>
    </div>
  );
}

export function RequireAuth({ children }: { children: React.ReactNode }) {
  const { isAuthenticated } = useAuth();
  if (!isAuthenticated) return <Navigate to="/login" replace />;
  return <>{children}</>;
}
