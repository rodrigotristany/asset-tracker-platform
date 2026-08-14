import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import { api } from "../api";
import type { DeviceSummary } from "../types";

export function DevicesPage() {
  const { data, isLoading, isError } = useQuery<DeviceSummary[]>({
    queryKey: ["devices"],
    queryFn: () => api.getDevicesSummary(),
    refetchInterval: 5000,
  });

  return (
    <div>
      <h1>Devices</h1>
      <Link to="/devices/new">+ Add Device</Link>
      {isLoading && <p>Loading…</p>}
      {isError && <p role="alert">Failed to load devices.</p>}
      {data && (
        <table>
          <thead>
            <tr>
              <th>Device ID</th>
              <th>Last Timestamp</th>
              <th>Latitude</th>
              <th>Longitude</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            {data.map((device) => (
              <tr key={device.deviceId}>
                <td>
                  <Link to={`/devices/${device.deviceId}`}>{device.deviceId}</Link>
                </td>
                <td>{device.latest.timestamp}</td>
                <td>{device.latest.latitude}</td>
                <td>{device.latest.longitude}</td>
                <td>
                  {device.status}
                  {device.latest.isStale && (
                    <span role="img" aria-label="stale warning">
                      {" "}
                      ⚠️
                    </span>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
