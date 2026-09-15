import React, { useEffect, useMemo, useState } from "react";
import { AssetDto } from "../types/Asset";
import { getAssets } from "../api/assetsApi";
import { ApiError } from "../api/client";
import { useAuth } from "../auth/AuthContext";
import { SearchBar } from "./SearchBar";
import { searchAssets } from "../search/fuzzyMatch";

export interface AssetListHandle {
  addAsset: (asset: AssetDto) => void;
}

interface AssetListProps {
  refreshToken: number;
}

const COMMON_FIELDS = ["capacity", "meterPointId"];

export function AssetList({ refreshToken }: AssetListProps) {
  const { auth } = useAuth();
  const [assets, setAssets] = useState<AssetDto[]>([]);
  const [query, setQuery] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!auth) return;
    setLoading(true);
    getAssets(auth.token)
      .then(setAssets)
      .catch((err) => setError(err instanceof ApiError ? err.message : "Could not load assets."))
      .finally(() => setLoading(false));
  }, [auth, refreshToken]);

  const filtered = useMemo(() => searchAssets(assets, query), [assets, query]);

  if (loading) return <div className="card">Loading assets…</div>;

  return (
    <div className="card">
      <h2>Assets</h2>
      {error && <div className="error-banner" role="alert">{error}</div>}
      <SearchBar value={query} onChange={setQuery} />

      {filtered.length === 0 ? (
        <div className="empty-state">No assets match your search yet.</div>
      ) : (
        <table>
          <thead>
            <tr>
              <th>Type</th>
              <th>Meter point ID</th>
              <th>Capacity</th>
              <th>Other details</th>
            </tr>
          </thead>
          <tbody>
            {filtered.map((asset) => (
              <tr key={asset.id}>
                <td><span className="badge">{asset.type}</span></td>
                <td>{asset.fields["meterPointId"]}</td>
                <td>{asset.fields["capacity"]}</td>
                <td>
                  {Object.entries(asset.fields)
                    .filter(([key]) => !COMMON_FIELDS.includes(key))
                    .map(([key, value]) => `${key}: ${value}`)
                    .join(", ")}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
