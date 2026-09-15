import React, { FormEvent, useState } from "react";
import { login as loginApi } from "../api/assetsApi";
import { ApiError } from "../api/client";
import { useAuth } from "../auth/AuthContext";

export function Login() {
  const { login } = useAuth();
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      const response = await loginApi(username, password);
      login(response);
    } catch (err) {
      // Deliberately generic - the API itself never reveals whether the
      // username or the password was wrong, and neither do we.
      const message = err instanceof ApiError ? err.message : "Something went wrong. Please try again.";
      setError(message);
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="app-main" style={{ maxWidth: 360, marginTop: "10vh" }}>
      <div className="card">
        <h2>Sign in</h2>
        {error && <div className="error-banner" role="alert">{error}</div>}
        <form onSubmit={handleSubmit}>
          <div className="field-row">
            <label htmlFor="username">Username</label>
            <input
              id="username"
              type="text"
              autoComplete="username"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              required
            />
          </div>
          <div className="field-row">
            <label htmlFor="password">Password</label>
            <input
              id="password"
              type="password"
              autoComplete="current-password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
            />
          </div>
          <button type="submit" className="btn-primary" disabled={submitting}>
            {submitting ? "Signing in…" : "Sign in"}
          </button>
        </form>
      </div>
    </div>
  );
}
