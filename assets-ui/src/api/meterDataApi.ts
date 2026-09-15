import { apiRequest, ApiError } from "./client";
import { ImportAcceptedDto, ImportJobStatusDto } from "../types/MeterData";

const API_BASE_URL = process.env.REACT_APP_API_BASE_URL ?? "https://localhost:5001";

/**
 * Uses XMLHttpRequest rather than fetch specifically because only XHR
 * exposes byte-level upload.onprogress events - this is what drives a
 * genuine (not simulated) progress bar during the transfer phase.
 */
export function uploadMeterDataFile(
  token: string,
  file: File,
  onUploadProgress: (percent: number) => void
): Promise<ImportAcceptedDto> {
  return new Promise((resolve, reject) => {
    const xhr = new XMLHttpRequest();
    const formData = new FormData();
    formData.append("file", file);

    xhr.open("POST", `${API_BASE_URL}/api/meter-data/import`);
    xhr.setRequestHeader("Authorization", `Bearer ${token}`);

    xhr.upload.onprogress = (event) => {
      if (event.lengthComputable) {
        onUploadProgress(Math.round((event.loaded / event.total) * 100));
      }
    };

    xhr.onload = () => {
      if (xhr.status >= 200 && xhr.status < 300) {
        try {
          resolve(JSON.parse(xhr.responseText) as ImportAcceptedDto);
        } catch {
          reject(new ApiError(xhr.status, "Unexpected response from server."));
        }
      } else {
        let message = `Upload failed with status ${xhr.status}`;
        try {
          message = JSON.parse(xhr.responseText).message ?? message;
        } catch {
          // Non-JSON error body - keep the generic message above.
        }
        reject(new ApiError(xhr.status, message));
      }
    };

    xhr.onerror = () => reject(new ApiError(0, "Network error during upload."));

    xhr.send(formData);
  });
}

export function getImportJobStatus(token: string, jobId: string): Promise<ImportJobStatusDto> {
  return apiRequest<ImportJobStatusDto>(`/api/meter-data/import/${jobId}`, { token });
}
