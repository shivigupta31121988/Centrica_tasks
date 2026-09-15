import { apiRequest } from "./client";
import { AssetDto, AssetTypeMetadata, CreateAssetRequest } from "../types/Asset";

export interface LoginResponse {
  token: string;
  username: string;
  role: "Admin" | "Trader";
}

export function login(username: string, password: string): Promise<LoginResponse> {
  return apiRequest<LoginResponse>("/api/auth/login", {
    method: "POST",
    body: { username, password },
  });
}

export function getAssets(token: string): Promise<AssetDto[]> {
  return apiRequest<AssetDto[]>("/api/assets", { token });
}

export function getAssetTypes(token: string): Promise<AssetTypeMetadata[]> {
  return apiRequest<AssetTypeMetadata[]>("/api/asset-types", { token });
}

export function createAsset(token: string, request: CreateAssetRequest): Promise<AssetDto> {
  return apiRequest<AssetDto>("/api/assets", { method: "POST", body: request, token });
}
