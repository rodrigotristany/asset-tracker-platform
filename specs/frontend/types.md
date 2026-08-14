# Frontend Types — TypeScript

## Location (from backend DTO schema)

```typescript
export interface Location {
  deviceId: string;
  timestamp: string;          // ISO 8601 UTC
  latitude: number;
  longitude: number;
  altitude?: number;
  speed?: number;
  satellites?: number;
  hdop?: number;
  batteryVoltage?: number;
  isStale: boolean;
}

// For DB rows (includes internal id if ever needed)
export interface LocationRow extends Location {
  id: number;
}
```

## Device Summary (dashboard view model)

```typescript
export interface DeviceSummary {
  deviceId: string;
  latest: Location;
  status: "online" | "offline" | "stale";
}
```

## Auth (JWT session)

```typescript
export interface AuthState {
  isAuthenticated: boolean;
  token?: string;
}
```

## Device Registration

```typescript
export interface DeviceRegistrationRequest {
  deviceId: string;
  displayName?: string;
}

export interface DeviceRegistrationResult {
  deviceId: string;
  apiKey: string; // shown once, never retrievable again
}
```
