import { useQuery } from "@tanstack/react-query";
import { useParams } from "react-router-dom";
import { api } from "../api";
import type { Location } from "../types";

export function DeviceDetailPage() {
  const { deviceId } = useParams<{ deviceId: string }>();

  const { data, isLoading, isError } = useQuery<Location>({
    queryKey: ["locations", deviceId],
    queryFn: () => api.getLatestLocation(deviceId!),
    refetchInterval: 2000,
    enabled: Boolean(deviceId),
  });

  if (isLoading) return <p>Loading…</p>;
  if (isError) return <p role="alert">Failed to load location for {deviceId}.</p>;
  if (!data) return null;

  return (
    <div>
      <h1>{deviceId}</h1>
      <dl>
        <dt>Timestamp</dt>
        <dd>{data.timestamp}</dd>
        <dt>Latitude</dt>
        <dd>{data.latitude}</dd>
        <dt>Longitude</dt>
        <dd>{data.longitude}</dd>
        {data.altitude !== undefined && (
          <>
            <dt>Altitude</dt>
            <dd>{data.altitude}</dd>
          </>
        )}
        {data.speed !== undefined && (
          <>
            <dt>Speed</dt>
            <dd>{data.speed}</dd>
          </>
        )}
        {data.satellites !== undefined && (
          <>
            <dt>Satellites</dt>
            <dd>{data.satellites}</dd>
          </>
        )}
        {data.hdop !== undefined && (
          <>
            <dt>HDOP</dt>
            <dd>{data.hdop}</dd>
          </>
        )}
      </dl>
      {data.batteryVoltage !== undefined && <p>Battery: {data.batteryVoltage}V</p>}
      {data.isStale && <p role="alert">Stale data warning</p>}
    </div>
  );
}
