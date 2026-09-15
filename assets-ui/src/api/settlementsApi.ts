import { apiRequest } from "./client";
import { DailySettlementDto, MonthlySettlementDto } from "../types/Settlement";

export function getAssetSettlement(token: string, assetId: string, start: string, end: string): Promise<DailySettlementDto[]> {
  return apiRequest<DailySettlementDto[]>(
    `/api/settlements/assets/${assetId}?start=${start}&end=${end}`,
    { token }
  );
}

export function getTotalSettlement(token: string, start: string, end: string): Promise<MonthlySettlementDto[]> {
  return apiRequest<MonthlySettlementDto[]>(`/api/settlements/total?start=${start}&end=${end}`, { token });
}
