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

export interface DeviceSummary {
  deviceId: string;
  latest: Location;
  status: "online" | "offline" | "stale";
}

export interface AuthState {
  isAuthenticated: boolean;
  token?: string;
}

export interface DeviceRegistrationRequest {
  deviceId: string;
  displayName?: string;
}

export interface DeviceRegistrationResult {
  deviceId: string;
  apiKey: string; // shown once, never retrievable again
}
