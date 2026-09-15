import React, { FormEvent, useEffect, useState } from "react";
import { AssetDto, AssetTypeMetadata } from "../types/Asset";
import { createAsset, getAssetTypes } from "../api/assetsApi";
import { ApiError } from "../api/client";
import { useAuth } from "../auth/AuthContext";

interface CreateAssetFormProps {
  onCreated: (asset: AssetDto) => void;
}

export function CreateAssetForm({ onCreated }: CreateAssetFormProps) {
  const { auth } = useAuth();
  const [assetTypes, setAssetTypes] = useState<AssetTypeMetadata[]>([]);
  const [selectedType, setSelectedType] = useState<string>("");
  const [values, setValues] = useState<Record<string, string>>({});
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [loadingTypes, setLoadingTypes] = useState(true);

  useEffect(() => {
    if (!auth) return;
    getAssetTypes(auth.token)
      .then((types) => {
        setAssetTypes(types);
        if (types.length > 0) setSelectedType(types[0].type);
      })
      .catch(() => setError("Could not load asset type definitions."))
      .finally(() => setLoadingTypes(false));
  }, [auth]);

  const activeType = assetTypes.find((t) => t.type === selectedType);

  function handleTypeChange(type: string) {
    setSelectedType(type);
    setValues({});
  }

  function handleFieldChange(name: string, value: string) {
    setValues((prev) => ({ ...prev, [name]: value }));
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    if (!auth || !activeType) return;

    setError(null);
    setSubmitting(true);
    try {
      const capacity = Number(values["capacity"] ?? 0);
      const meterPointId = values["meterPointId"] ?? "";

      const extraFields: Record<string, string | number> = {};
      for (const field of activeType.fields) {
        if (field.name === "capacity" || field.name === "meterPointId") continue;
        const raw = values[field.name] ?? "";
        extraFields[field.name] = field.inputType === "number" ? Number(raw) : raw;
      }

      const created = await createAsset(auth.token, {
        type: activeType.type,
        capacity,
        meterPointId,
        fields: extraFields,
      });

      onCreated(created);
      setValues({});
    } catch (err) {
      const message = err instanceof ApiError ? err.message : "Could not create the asset. Please try again.";
      setError(message);
    } finally {
      setSubmitting(false);
    }
  }

  if (loadingTypes) return <div className="card">Loading asset types…</div>;

  return (
    <div className="card">
      <h2>Add a new asset</h2>
      {error && <div className="error-banner" role="alert">{error}</div>}
      <form onSubmit={handleSubmit}>
        <div className="field-row">
          <label htmlFor="asset-type">Asset type</label>
          <select id="asset-type" value={selectedType} onChange={(e) => handleTypeChange(e.target.value)}>
            {assetTypes.map((t) => (
              <option key={t.type} value={t.type}>
                {t.type}
              </option>
            ))}
          </select>
        </div>

        {activeType?.fields.map((field) => (
          <div className="field-row" key={field.name}>
            <label htmlFor={field.name}>{field.label}</label>
            <input
              id={field.name}
              type={field.inputType === "number" ? "number" : "text"}
              value={values[field.name] ?? ""}
              onChange={(e) => handleFieldChange(field.name, e.target.value)}
              required
            />
          </div>
        ))}

        <button type="submit" className="btn-primary" disabled={submitting || !activeType}>
          {submitting ? "Adding…" : "Add asset"}
        </button>
      </form>
    </div>
  );
}
