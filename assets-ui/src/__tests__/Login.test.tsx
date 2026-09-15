import React from "react";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { Login } from "../components/Login";
import { AuthProvider } from "../auth/AuthContext";
import * as assetsApi from "../api/assetsApi";

jest.mock("../api/assetsApi");

describe("Login", () => {
  it("calls the login API and does not show an error on success", async () => {
    (assetsApi.login as jest.Mock).mockResolvedValue({ token: "t", username: "admin1", role: "Admin" });

    render(
      <AuthProvider>
        <Login />
      </AuthProvider>
    );

    fireEvent.change(screen.getByLabelText(/username/i), { target: { value: "admin1" } });
    fireEvent.change(screen.getByLabelText(/password/i), { target: { value: "correct-password" } });
    fireEvent.click(screen.getByRole("button", { name: /sign in/i }));

    await waitFor(() => expect(assetsApi.login).toHaveBeenCalledWith("admin1", "correct-password"));
    expect(screen.queryByRole("alert")).not.toBeInTheDocument();
  });

  it("shows a generic error message on failed login, without revealing why", async () => {
    const { ApiError } = jest.requireActual("../api/client");
    (assetsApi.login as jest.Mock).mockRejectedValue(new ApiError(401, "Invalid username or password."));

    render(
      <AuthProvider>
        <Login />
      </AuthProvider>
    );

    fireEvent.change(screen.getByLabelText(/username/i), { target: { value: "ghost" } });
    fireEvent.change(screen.getByLabelText(/password/i), { target: { value: "whatever" } });
    fireEvent.click(screen.getByRole("button", { name: /sign in/i }));

    expect(await screen.findByRole("alert")).toHaveTextContent(/invalid username or password/i);
  });
});
