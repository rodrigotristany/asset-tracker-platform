import type {
  AuthState,
  DeviceRegistrationResult,
  DeviceSummary,
  Location,
} from "./types";

export function deriveDeviceStatus(location: Location): "online" | "offline" | "stale" {
  if (location.isStale) return "stale";
  const ageMs = Date.now() - new Date(location.timestamp).getTime();
  return ageMs > 60_000 ? "offline" : "online";
}

export class ApiClient {
  private baseUrl: string;
  private token?: string;

  constructor(baseUrl: string) {
    this.baseUrl = baseUrl;
  }

  setToken(token: string) {
    this.token = token;
  }

  async getLatestLocation(deviceId: string): Promise<Location> {
    const res = await fetch(`${this.baseUrl}/api/v1/locations/${deviceId}`, {
      headers: this.authHeaders(),
    });
    if (!res.ok) throw new Error(await res.text());
    return res.json();
  }

  async getDevicesSummary(): Promise<DeviceSummary[]> {
    const res = await fetch(`${this.baseUrl}/api/v1/devices`, {
      headers: this.authHeaders(),
    });
    if (!res.ok) throw new Error(await res.text());
    const locations: Location[] = await res.json();
    return locations.map((latest) => ({
      deviceId: latest.deviceId,
      latest,
      status: deriveDeviceStatus(latest),
    }));
  }

  async login(username: string, password: string): Promise<AuthState> {
    const res = await fetch(`${this.baseUrl}/api/v1/auth/login`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ username, password }),
    });
    if (!res.ok) throw new Error(await res.text());
    const { token }: { token: string } = await res.json();
    this.setToken(token);
    return { isAuthenticated: true, token };
  }

  async registerDevice(deviceId: string, displayName?: string): Promise<DeviceRegistrationResult> {
    const res = await fetch(`${this.baseUrl}/api/v1/devices`, {
      method: "POST",
      headers: { "Content-Type": "application/json", ...this.authHeaders() },
      body: JSON.stringify({ deviceId, displayName }),
    });
    if (!res.ok) throw new Error(await res.text());
    return res.json();
  }

  private authHeaders(): HeadersInit {
    if (!this.token) return {};
    return { Authorization: `Bearer ${this.token}` };
  }
}

export const api = new ApiClient(import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5125");
