import React from "react";
import { render, screen, fireEvent, waitFor, act } from "@testing-library/react";
import { MeterDataImport } from "../components/MeterDataImport";
import { AuthProvider, useAuth } from "../auth/AuthContext";
import * as meterDataApi from "../api/meterDataApi";

jest.mock("../api/meterDataApi");

function LoggedInMeterDataImport() {
  const { login } = useAuth();
  React.useEffect(() => {
    login({ token: "t", username: "admin1", role: "Admin" });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);
  return <MeterDataImport />;
}

function renderComponent() {
  return render(
    <AuthProvider>
      <LoggedInMeterDataImport />
    </AuthProvider>
  );
}

describe("MeterDataImport", () => {
  beforeEach(() => {
    jest.useFakeTimers();
  });

  afterEach(() => {
    jest.useRealTimers();
    jest.clearAllMocks();
  });

  it("uploads the file, polls for status, and shows the completed summary", async () => {
    (meterDataApi.uploadMeterDataFile as jest.Mock).mockImplementation(async (_token, _file, onProgress) => {
      onProgress(50);
      onProgress(100);
      return { jobId: "job-1" };
    });
    (meterDataApi.getImportJobStatus as jest.Mock)
      .mockResolvedValueOnce({ jobId: "job-1", fileName: "123.csv", status: "Processing", rowsImported: 0, rowsSkipped: 0, elapsedSeconds: 1, errorMessage: null })
      .mockResolvedValueOnce({ jobId: "job-1", fileName: "123.csv", status: "Completed", rowsImported: 42, rowsSkipped: 3, elapsedSeconds: 2, errorMessage: null });

    renderComponent();

    const file = new File(["Timestamp,Production\n"], "123.csv", { type: "text/csv" });
    const input = screen.getByLabelText(/meter data file/i);
    fireEvent.change(input, { target: { files: [file] } });

    fireEvent.click(screen.getByRole("button", { name: /upload/i }));

    await waitFor(() => expect(meterDataApi.uploadMeterDataFile).toHaveBeenCalled());

    // Advance past two poll intervals to let both mocked responses resolve.
    await act(async () => {
      jest.advanceTimersByTime(1000);
    });
    await act(async () => {
      jest.advanceTimersByTime(1000);
    });

    expect(await screen.findByText(/42 row\(s\) imported, 3 skipped/i)).toBeInTheDocument();
  });

  it("shows an error message if the upload itself fails", async () => {
    const { ApiError } = jest.requireActual("../api/client");
    (meterDataApi.uploadMeterDataFile as jest.Mock).mockRejectedValue(new ApiError(400, "Filename must be in the form <meterPointId>.csv"));

    renderComponent();

    const file = new File(["bad"], "not-valid.csv", { type: "text/csv" });
    fireEvent.change(screen.getByLabelText(/meter data file/i), { target: { files: [file] } });
    fireEvent.click(screen.getByRole("button", { name: /upload/i }));

    expect(await screen.findByRole("alert")).toHaveTextContent(/filename must be in the form/i);
  });
});
