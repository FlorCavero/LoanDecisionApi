import { createContext, useContext, useState, type ReactNode } from "react";
import { login as apiLogin } from "../api/client";

interface AuthState {
  token: string | null;
  partnerName: string | null;
  isAuthenticated: boolean;
  login: (name: string, secret: string) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthState | undefined>(undefined);

// sessionStorage (not localStorage) deliberately - the token disappears when the
// tab closes rather than lingering indefinitely, which fits a 1-hour JWT anyway.
const STORAGE_KEY = "loanDecisionApi.token";
const STORAGE_PARTNER_KEY = "loanDecisionApi.partnerName";

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(() => sessionStorage.getItem(STORAGE_KEY));
  const [partnerName, setPartnerName] = useState<string | null>(() =>
    sessionStorage.getItem(STORAGE_PARTNER_KEY),
  );

  async function login(name: string, secret: string) {
    const response = await apiLogin({ name, secret });
    sessionStorage.setItem(STORAGE_KEY, response.accessToken);
    sessionStorage.setItem(STORAGE_PARTNER_KEY, name);
    setToken(response.accessToken);
    setPartnerName(name);
  }

  function logout() {
    sessionStorage.removeItem(STORAGE_KEY);
    sessionStorage.removeItem(STORAGE_PARTNER_KEY);
    setToken(null);
    setPartnerName(null);
  }

  return (
    <AuthContext.Provider value={{ token, partnerName, isAuthenticated: token !== null, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthState {
  const context = useContext(AuthContext);
  if (!context) throw new Error("useAuth must be used within an AuthProvider.");
  return context;
}
