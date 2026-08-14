import { render, screen } from "@testing-library/react";
import { MemoryRouter, Routes, Route } from "react-router-dom";
import { beforeEach, describe, expect, it } from "vitest";
import { ProtectedRoute } from "./ProtectedRoute";
import { useAuthStore } from "../store/authStore";

function renderProtectedRoute() {
  return render(
    <MemoryRouter initialEntries={["/devices"]}>
      <Routes>
        <Route path="/login" element={<div>Login Page</div>} />
        <Route
          path="/devices"
          element={
            <ProtectedRoute>
              <div>Devices Page</div>
            </ProtectedRoute>
          }
        />
      </Routes>
    </MemoryRouter>,
  );
}

describe("ProtectedRoute", () => {
  beforeEach(() => {
    useAuthStore.setState({ isAuthenticated: false, token: undefined });
  });

  it("redirects to /login when not authenticated", () => {
    renderProtectedRoute();
    expect(screen.getByText("Login Page")).toBeInTheDocument();
  });

  it("renders its children when authenticated", () => {
    useAuthStore.setState({ isAuthenticated: true, token: "jwt-abc" });
    renderProtectedRoute();
    expect(screen.getByText("Devices Page")).toBeInTheDocument();
  });
});
