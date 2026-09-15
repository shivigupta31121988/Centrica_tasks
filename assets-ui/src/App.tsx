import React, { useState } from "react";
import { useAuth } from "./auth/AuthContext";
import { Login } from "./components/Login";
import { AssetList } from "./components/AssetList";
import { CreateAssetForm } from "./components/CreateAssetForm";
import { MeterDataImport } from "./components/MeterDataImport";
import { SettlementView } from "./components/SettlementView";
import { AssetDto } from "./types/Asset";

export function App() {
  const { auth, logout } = useAuth();
  const [refreshToken, setRefreshToken] = useState(0);

  if (!auth) {
    return <Login />;
  }

  function handleCreated(_asset: AssetDto) {
    // Simplest way to keep the list in sync post-create for an MVP:
    // trigger AssetList's effect to re-fetch. A production version might
    // instead append optimistically or use a shared query cache.
    setRefreshToken((n) => n + 1);
  }

  return (
    <div>
      <header className="app-header">
        <h1>Renewable Asset System</h1>
        <div className="user-info">
          <span>{auth.username} · {auth.role}</span>
          <button onClick={logout}>Sign out</button>
        </div>
      </header>
      <main className="app-main">
        {auth.role === "Admin" && (
          <>
            <CreateAssetForm onCreated={handleCreated} />
            <MeterDataImport />
          </>
        )}
        <AssetList refreshToken={refreshToken} />
        <SettlementView />
      </main>
    </div>
  );
}
