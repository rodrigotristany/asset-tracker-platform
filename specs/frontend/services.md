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
      headers: this.authHeader(),
    });
    if (!res.ok) throw new Error(await res.text());
    return res.json();
  }

  async getDevicesSummary(): Promise<DeviceSummary[]> {
    // TBD based on available backend endpoint
    // Could be a dedicated summary endpoint or client-side aggregation
    throw new Error("Not implemented: requires backend summary endpoint");
  }

  async login(username: string, password: string): Promise<AuthState> {
    // TBD exact JWT login flow (session cookie vs token response)
    throw new Error("Not implemented: TBD auth flow");
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
