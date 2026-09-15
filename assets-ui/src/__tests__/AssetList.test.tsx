import React from "react";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { AssetList } from "../components/AssetList";
import { AuthProvider, useAuth } from "../auth/AuthContext";
import * as assetsApi from "../api/assetsApi";

jest.mock("../api/assetsApi");

const MOCK_ASSETS = [
  { id: "1", type: "SolarPanel", fields: { capacity: 20, meterPointId: "570715000000088747", compassOrientation: "South" } },
  { id: "2", type: "WindTurbine", fields: { capacity: 150, meterPointId: "570715000000099999", hubHeight: 90, rotorDiameter: 120 } },
];

function LoggedInAssetList() {
  const { login } = useAuth();
  React.useEffect(() => {
    login({ token: "t", username: "admin1", role: "Admin" });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);
  return <AssetList refreshToken={0} />;
}

describe("AssetList", () => {
  beforeEach(() => {
    (assetsApi.getAssets as jest.Mock).mockResolvedValue(MOCK_ASSETS);
  });

  it("renders both assets once loaded", async () => {
    render(
      <AuthProvider>
        <LoggedInAssetList />
      </AuthProvider>
    );

    expect(await screen.findByText("SolarPanel")).toBeInTheDocument();
    expect(screen.getByText("WindTurbine")).toBeInTheDocument();
  });

  it("filters to the solar panel when searching a typo'd query", async () => {
    render(
      <AuthProvider>
        <LoggedInAssetList />
      </AuthProvider>
    );

    await screen.findByText("SolarPanel");

    fireEvent.change(screen.getByLabelText(/search assets/i), { target: { value: "sloar" } });

    await waitFor(() => {
      expect(screen.getByText("SolarPanel")).toBeInTheDocument();
      expect(screen.queryByText("WindTurbine")).not.toBeInTheDocument();
    });
  });
});
