import { useState, type FormEvent } from "react";
import { useMutation } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import { api } from "../api";
import type { DeviceRegistrationResult } from "../types";

export function AddDevicePage() {
  const [deviceId, setDeviceId] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [result, setResult] = useState<DeviceRegistrationResult | null>(null);

  const mutation = useMutation<DeviceRegistrationResult, Error, { deviceId: string; displayName: string }>({
    mutationFn: (req) => api.registerDevice(req.deviceId, req.displayName || undefined),
    onSuccess: (data) => setResult(data),
  });

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault();
    mutation.mutate({ deviceId, displayName });
  };

  if (result) {
    return (
      <div>
        <h1>Device registered</h1>
        <p role="alert">Copy this now — you won't be able to see it again</p>
        <code>{result.apiKey}</code>
        <p>
          <Link to="/devices">Back to devices</Link>
        </p>
      </div>
    );
  }

  return (
    <form onSubmit={handleSubmit}>
      <h1>Add Device</h1>
      <label htmlFor="deviceId">Device ID</label>
      <input id="deviceId" value={deviceId} onChange={(event) => setDeviceId(event.target.value)} required />
      <label htmlFor="displayName">Display Name</label>
      <input id="displayName" value={displayName} onChange={(event) => setDisplayName(event.target.value)} />
      <button type="submit" disabled={mutation.isPending}>
        Register
      </button>
      {mutation.isError && <p role="alert">{mutation.error.message}</p>}
    </form>
  );
}
