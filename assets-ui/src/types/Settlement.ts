export interface DailySettlementDto {
  date: string; // yyyy-MM-dd
  amount: number;
  currency: string;
  incompleteHours: number;
}

export interface MonthlySettlementDto {
  month: string; // yyyy-MM
  amount: number;
  currency: string;
  incompleteHours: number;
}
