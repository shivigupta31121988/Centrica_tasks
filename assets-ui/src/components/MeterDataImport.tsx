import React, { useEffect, useRef, useState } from "react";
import { uploadMeterDataFile, getImportJobStatus } from "../api/meterDataApi";
import { ApiError } from "../api/client";
import { useAuth } from "../auth/AuthContext";
import { ImportJobStatus } from "../types/MeterData";

type Phase = "idle" | "uploading" | "processing" | "completed" | "failed";

const POLL_INTERVAL_MS = 1000;
const TIMER_TICK_MS = 100;

export function MeterDataImport() {
  const { auth } = useAuth();
  const [file, setFile] = useState<File | null>(null);
  const [phase, setPhase] = useState<Phase>("idle");
  const [uploadPercent, setUploadPercent] = useState(0);
  const [elapsedSeconds, setElapsedSeconds] = useState(0);
  const [rowsImported, setRowsImported] = useState<number | null>(null);
  const [rowsSkipped, setRowsSkipped] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);

  const startTimeRef = useRef<number>(0);
  const timerIntervalRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const pollIntervalRef = useRef<ReturnType<typeof setInterval> | null>(null);

  useEffect(() => {
    return () => {
      // Cleanup on unmount so a stray interval never fires against an
      // unmounted component.
      if (timerIntervalRef.current) clearInterval(timerIntervalRef.current);
      if (pollIntervalRef.current) clearInterval(pollIntervalRef.current);
    };
  }, []);

  function startStopwatch() {
    startTimeRef.current = Date.now();
    timerIntervalRef.current = setInterval(() => {
      setElapsedSeconds((Date.now() - startTimeRef.current) / 1000);
    }, TIMER_TICK_MS);
  }

  function stopStopwatch() {
    if (timerIntervalRef.current) {
      clearInterval(timerIntervalRef.current);
      timerIntervalRef.current = null;
    }
  }

  function stopPolling() {
    if (pollIntervalRef.current) {
      clearInterval(pollIntervalRef.current);
      pollIntervalRef.current = null;
    }
  }

  function pollJobStatus(jobId: string) {
    if (!auth) return;
    pollIntervalRef.current = setInterval(async () => {
      try {
        const status = await getImportJobStatus(auth.token, jobId);
        applyTerminalStatusIfDone(status.status, status.rowsImported, status.rowsSkipped, status.errorMessage);
      } catch {
        // A single missed poll isn't fatal - the interval just tries again
        // on the next tick. If the job truly disappeared, the user can
        // retry the upload.
      }
    }, POLL_INTERVAL_MS);
  }

  function applyTerminalStatusIfDone(
    status: ImportJobStatus,
    imported: number,
    skipped: number,
    errorMessage: string | null
  ) {
    if (status === "Completed") {
      stopPolling();
      stopStopwatch();
      setRowsImported(imported);
      setRowsSkipped(skipped);
      setPhase("completed");
    } else if (status === "Failed") {
      stopPolling();
      stopStopwatch();
      setError(errorMessage ?? "The import failed.");
      setPhase("failed");
    }
    // Pending/Processing: keep polling, keep the stopwatch running.
  }

  async function handleUpload() {
    if (!auth || !file) return;

    setError(null);
    setRowsImported(null);
    setRowsSkipped(null);
    setUploadPercent(0);
    setPhase("uploading");
    startStopwatch();

    try {
      const accepted = await uploadMeterDataFile(auth.token, file, (percent) => {
        setUploadPercent(percent);
      });
      setPhase("processing");
      pollJobStatus(accepted.jobId);
    } catch (err) {
      stopStopwatch();
      const message = err instanceof ApiError ? err.message : "Upload failed. Please try again.";
      setError(message);
      setPhase("failed");
    }
  }

  const isBusy = phase === "uploading" || phase === "processing";

  return (
    <div className="card">
      <h2>Import meter data</h2>
      <p style={{ color: "var(--color-text-muted)", fontSize: "0.9rem", marginTop: 0 }}>
        Upload a CSV or Excel file named after its meter point id (e.g. 570715000000088747.csv).
      </p>

      {error && <div className="error-banner" role="alert">{error}</div>}

      <div className="field-row">
        <input
          type="file"
          accept=".csv,.xlsx"
          aria-label="Meter data file"
          disabled={isBusy}
          onChange={(e) => setFile(e.target.files?.[0] ?? null)}
        />
      </div>

      <button className="btn-primary" onClick={handleUpload} disabled={!file || isBusy}>
        {isBusy ? "Working…" : "Upload"}
      </button>

      {phase === "uploading" && (
        <div style={{ marginTop: "1rem" }}>
          <div aria-label="Upload progress" role="progressbar" aria-valuenow={uploadPercent} aria-valuemin={0} aria-valuemax={100}
               style={{ background: "var(--color-border)", borderRadius: 4, overflow: "hidden", height: 8 }}>
            <div style={{ width: `${uploadPercent}%`, background: "var(--color-accent)", height: "100%", transition: "width 0.1s linear" }} />
          </div>
          <p style={{ fontSize: "0.85rem", color: "var(--color-text-muted)" }}>
            Uploading… {uploadPercent}% · {elapsedSeconds.toFixed(1)}s elapsed
          </p>
        </div>
      )}

      {phase === "processing" && (
        <p style={{ fontSize: "0.85rem", color: "var(--color-text-muted)", marginTop: "1rem" }}>
          Processing on the server… {elapsedSeconds.toFixed(1)}s elapsed
        </p>
      )}

      {phase === "completed" && (
        <p style={{ fontSize: "0.9rem", marginTop: "1rem" }}>
          Imported in {elapsedSeconds.toFixed(1)}s — {rowsImported} row(s) imported, {rowsSkipped} skipped.
        </p>
      )}
    </div>
  );
}
