import { beforeEach, describe, expect, it, vi } from "vitest";
import { useAuthStore } from "./authStore";
import { api } from "../api";

vi.mock("../api", () => ({
  api: { login: vi.fn() },
}));

describe("useAuthStore", () => {
  beforeEach(() => {
    useAuthStore.setState({ isAuthenticated: false, token: undefined });
    vi.mocked(api.login).mockReset();
  });

  it("starts unauthenticated", () => {
    expect(useAuthStore.getState().isAuthenticated).toBe(false);
  });

  it("login updates the store from the API client's result", async () => {
    vi.mocked(api.login).mockResolvedValueOnce({ isAuthenticated: true, token: "jwt-abc" });

    await useAuthStore.getState().login("admin", "secret");

    expect(useAuthStore.getState()).toMatchObject({ isAuthenticated: true, token: "jwt-abc" });
    expect(api.login).toHaveBeenCalledWith("admin", "secret");
  });

  it("logout resets the store to unauthenticated", () => {
    useAuthStore.setState({ isAuthenticated: true, token: "jwt-abc" });

    useAuthStore.getState().logout();

    expect(useAuthStore.getState()).toMatchObject({ isAuthenticated: false, token: undefined });
  });
});
