export interface AssetDto {
  id: string;
  type: string;
  fields: Record<string, string | number | null>;
}

export interface AssetFieldMetadata {
  name: string;
  label: string;
  inputType: "number" | "text";
}

export interface AssetTypeMetadata {
  type: string;
  fields: AssetFieldMetadata[];
}

export interface CreateAssetRequest {
  type: string;
  capacity: number;
  meterPointId: string;
  fields: Record<string, string | number>;
}
