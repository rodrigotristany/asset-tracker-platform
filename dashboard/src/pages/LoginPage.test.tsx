import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { MemoryRouter, Routes, Route } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { LoginPage } from "./LoginPage";
import { useAuthStore } from "../store/authStore";
import { api } from "../api";

vi.mock("../api", () => ({
  api: { login: vi.fn() },
}));

function renderLoginPage() {
  const queryClient = new QueryClient();
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={["/login"]}>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/devices" element={<div>Devices Page</div>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe("LoginPage", () => {
  beforeEach(() => {
    useAuthStore.setState({ isAuthenticated: false, token: undefined });
  });

  it("submits the entered credentials and redirects to /devices on success", async () => {
    vi.mocked(api.login).mockResolvedValueOnce({ isAuthenticated: true, token: "jwt-abc" });
    renderLoginPage();

    fireEvent.change(screen.getByLabelText("Username"), { target: { value: "admin" } });
    fireEvent.change(screen.getByLabelText("Password"), { target: { value: "secret" } });
    fireEvent.click(screen.getByRole("button", { name: "Sign in" }));

    await waitFor(() => expect(api.login).toHaveBeenCalledWith("admin", "secret"));
    expect(await screen.findByText("Devices Page")).toBeInTheDocument();
  });

  it("shows an inline error and keeps the username filled in on failed login", async () => {
    vi.mocked(api.login).mockRejectedValueOnce(new Error("Invalid credentials"));
    renderLoginPage();

    fireEvent.change(screen.getByLabelText("Username"), { target: { value: "admin" } });
    fireEvent.change(screen.getByLabelText("Password"), { target: { value: "wrong" } });
    fireEvent.click(screen.getByRole("button", { name: "Sign in" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("Invalid username or password");
    expect(screen.getByLabelText("Username")).toHaveValue("admin");
  });
});
