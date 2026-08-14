import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ApiClient } from "./api";
import type { Location } from "./types";

describe("ApiClient", () => {
  let client: ApiClient;

  beforeEach(() => {
    client = new ApiClient("http://test.local");
    vi.stubGlobal("fetch", vi.fn());
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("login stores the token and returns an authenticated AuthState", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(
      new Response(JSON.stringify({ token: "jwt-abc" }), { status: 200 }),
    );

    const result = await client.login("admin", "secret");

    expect(result).toEqual({ isAuthenticated: true, token: "jwt-abc" });
    expect(fetch).toHaveBeenCalledWith(
      "http://test.local/api/v1/auth/login",
      expect.objectContaining({
        method: "POST",
        body: JSON.stringify({ username: "admin", password: "secret" }),
      }),
    );
  });

  it("login throws on a non-ok response", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(new Response("bad credentials", { status: 401 }));

    await expect(client.login("admin", "wrong")).rejects.toThrow("bad credentials");
  });

  it("sends the bearer token on authenticated requests after login", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(
      new Response(JSON.stringify({ token: "jwt-abc" }), { status: 200 }),
    );
    await client.login("admin", "secret");

    vi.mocked(fetch).mockResolvedValueOnce(new Response(JSON.stringify([]), { status: 200 }));
    await client.getDevicesSummary();

    expect(fetch).toHaveBeenLastCalledWith(
      "http://test.local/api/v1/devices",
      expect.objectContaining({
        headers: { Authorization: "Bearer jwt-abc" },
      }),
    );
  });

  it("getDevicesSummary marks a device online when its latest location is fresh and not stale", async () => {
    const freshLocation: Location = {
      deviceId: "goat-001",
      timestamp: new Date().toISOString(),
      latitude: 1,
      longitude: 1,
      isStale: false,
    };
    vi.mocked(fetch).mockResolvedValueOnce(new Response(JSON.stringify([freshLocation]), { status: 200 }));

    const result = await client.getDevicesSummary();

    expect(result).toEqual([{ deviceId: "goat-001", latest: freshLocation, status: "online" }]);
  });

  it("getDevicesSummary marks a device offline when its latest location is older than 60 seconds", async () => {
    const staleAgeLocation: Location = {
      deviceId: "goat-002",
      timestamp: new Date(Date.now() - 61_000).toISOString(),
      latitude: 1,
      longitude: 1,
      isStale: false,
    };
    vi.mocked(fetch).mockResolvedValueOnce(new Response(JSON.stringify([staleAgeLocation]), { status: 200 }));

    const result = await client.getDevicesSummary();

    expect(result[0].status).toBe("offline");
  });

  it("getDevicesSummary marks a device stale when isStale is true, regardless of age", async () => {
    const staleFlagLocation: Location = {
      deviceId: "goat-003",
      timestamp: new Date().toISOString(),
      latitude: 1,
      longitude: 1,
      isStale: true,
    };
    vi.mocked(fetch).mockResolvedValueOnce(new Response(JSON.stringify([staleFlagLocation]), { status: 200 }));

    const result = await client.getDevicesSummary();

    expect(result[0].status).toBe("stale");
  });

  it("registerDevice posts the device id and display name", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(
      new Response(JSON.stringify({ deviceId: "goat-004", apiKey: "key-123" }), { status: 201 }),
    );

    const result = await client.registerDevice("goat-004", "Goat 004");

    expect(result).toEqual({ deviceId: "goat-004", apiKey: "key-123" });
    expect(fetch).toHaveBeenCalledWith(
      "http://test.local/api/v1/devices",
      expect.objectContaining({
        method: "POST",
        body: JSON.stringify({ deviceId: "goat-004", displayName: "Goat 004" }),
      }),
    );
  });

  it("getLatestLocation throws on a 404 response", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(new Response("not found", { status: 404 }));

    await expect(client.getLatestLocation("ghost-001")).rejects.toThrow("not found");
  });
});
