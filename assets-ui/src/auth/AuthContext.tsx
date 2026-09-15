import React, { createContext, useContext, useMemo, useState, ReactNode } from "react";

export interface AuthState {
  token: string;
  username: string;
  role: "Admin" | "Trader";
}

interface AuthContextValue {
  auth: AuthState | null;
  login: (state: AuthState) => void;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

/**
 * The JWT is kept in React state only - never localStorage/sessionStorage.
 * Trade-off: the user is logged out on a full page refresh. This is a
 * deliberate MVP choice to reduce the token-theft blast radius of an XSS
 * bug (localStorage is readable by any injected script; in-memory state
 * is not, and is cleared automatically on reload). A production version
 * could use a short-lived token plus an httpOnly refresh-token cookie.
 */
export function AuthProvider({ children }: { children: ReactNode }) {
  const [auth, setAuth] = useState<AuthState | null>(null);

  const value = useMemo<AuthContextValue>(
    () => ({
      auth,
      login: (state: AuthState) => setAuth(state),
      logout: () => setAuth(null),
    }),
    [auth]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return ctx;
}
