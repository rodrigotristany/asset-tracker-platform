# Frontend Services — API Client

```typescript
class ApiClient {
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
    return res.json();
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
```

## Notes
- TypeScript types in `types.md` must match backend DTOs exactly.
- Phase 1: no maps, no realtime websocket, polling via TanStack Query is acceptable.
- Phase 2: generate types from backend `/swagger` OpenAPI spec.
