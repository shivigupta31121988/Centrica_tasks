export type ImportJobStatus = "Pending" | "Processing" | "Completed" | "Failed";

export interface ImportJobStatusDto {
  jobId: string;
  fileName: string;
  status: ImportJobStatus;
  rowsImported: number;
  rowsSkipped: number;
  elapsedSeconds: number;
  errorMessage: string | null;
}

export interface ImportAcceptedDto {
  jobId: string;
}
