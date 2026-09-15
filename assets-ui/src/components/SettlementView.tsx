import React, { useEffect, useState } from "react";
import { useAuth } from "../auth/AuthContext";
import { getAssets } from "../api/assetsApi";
import { getAssetSettlement, getTotalSettlement } from "../api/settlementsApi";
import { ApiError } from "../api/client";
import { AssetDto } from "../types/Asset";
import { DailySettlementDto, MonthlySettlementDto } from "../types/Settlement";

function todayIso(): string {
  return new Date().toISOString().slice(0, 10);
}

function firstOfMonthIso(): string {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-01`;
}

export function SettlementView() {
  const { auth } = useAuth();
  const [assets, setAssets] = useState<AssetDto[]>([]);
  const [assetId, setAssetId] = useState("");
  const [start, setStart] = useState(firstOfMonthIso());
  const [end, setEnd] = useState(todayIso());
  const [dailyResults, setDailyResults] = useState<DailySettlementDto[] | null>(null);
  const [monthlyResults, setMonthlyResults] = useState<MonthlySettlementDto[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!auth) return;
    getAssets(auth.token).then((list) => {
      setAssets(list);
      if (list.length > 0) setAssetId(list[0].id);
    });
  }, [auth]);

  async function handleCalculate() {
    if (!auth) return;
    setError(null);
    setLoading(true);
    setDailyResults(null);
    setMonthlyResults(null);
    try {
      const [daily, monthly] = await Promise.all([
        assetId ? getAssetSettlement(auth.token, assetId, start, end) : Promise.resolve([]),
        getTotalSettlement(auth.token, start, end),
      ]);
      setDailyResults(daily);
      setMonthlyResults(monthly);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Could not calculate settlement.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="card">
      <h2>Settlement</h2>
      {error && <div className="error-banner" role="alert">{error}</div>}

      <div className="field-row">
        <label htmlFor="settlement-asset">Asset (daily breakdown)</label>
        <select id="settlement-asset" value={assetId} onChange={(e) => setAssetId(e.target.value)}>
          {assets.map((a) => (
            <option key={a.id} value={a.id}>
              {a.type} · {a.fields["meterPointId"]}
            </option>
          ))}
        </select>
      </div>

      <div className="field-row">
        <label htmlFor="settlement-start">Start date</label>
        <input id="settlement-start" type="date" value={start} onChange={(e) => setStart(e.target.value)} />
      </div>
      <div className="field-row">
        <label htmlFor="settlement-end">End date</label>
        <input id="settlement-end" type="date" value={end} onChange={(e) => setEnd(e.target.value)} />
      </div>

      <button className="btn-primary" onClick={handleCalculate} disabled={loading}>
        {loading ? "Calculating…" : "Calculate"}
      </button>

      {dailyResults && (
        <div style={{ marginTop: "1.5rem" }}>
          <h3 style={{ fontSize: "0.9rem" }}>Daily (selected asset)</h3>
          {dailyResults.length === 0 ? (
            <div className="empty-state">No data for this period.</div>
          ) : (
            <table>
              <thead>
                <tr><th>Date</th><th>Amount</th><th>Incomplete hours</th></tr>
              </thead>
              <tbody>
                {dailyResults.map((d) => (
                  <tr key={d.date}>
                    <td>{d.date}</td>
                    <td>{d.amount.toFixed(2)} {d.currency}</td>
                    <td>{d.incompleteHours > 0 ? d.incompleteHours : "—"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      )}

      {monthlyResults && (
        <div style={{ marginTop: "1.5rem" }}>
          <h3 style={{ fontSize: "0.9rem" }}>Monthly (all assets)</h3>
          {monthlyResults.length === 0 ? (
            <div className="empty-state">No data for this period.</div>
          ) : (
            <table>
              <thead>
                <tr><th>Month</th><th>Amount</th><th>Incomplete hours</th></tr>
              </thead>
              <tbody>
                {monthlyResults.map((m) => (
                  <tr key={m.month}>
                    <td>{m.month}</td>
                    <td>{m.amount.toFixed(2)} {m.currency}</td>
                    <td>{m.incompleteHours > 0 ? m.incompleteHours : "—"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      )}
    </div>
  );
}
