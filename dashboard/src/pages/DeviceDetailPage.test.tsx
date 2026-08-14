import { render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { describe, expect, it, vi } from "vitest";
import { DeviceDetailPage } from "./DeviceDetailPage";
import { api } from "../api";
import type { Location } from "../types";

vi.mock("../api", () => ({
  api: { getLatestLocation: vi.fn() },
}));

function renderDeviceDetailPage(deviceId: string) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
    },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/devices/${deviceId}`]}>
        <Routes>
          <Route path="/devices/:deviceId" element={<DeviceDetailPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe("DeviceDetailPage", () => {
  it("renders the full GPS payload for the device", async () => {
    const location: Location = {
      deviceId: "goat-001",
      timestamp: "2026-08-14T12:00:00Z",
      latitude: 1.5,
      longitude: 2.5,
      altitude: 100,
      speed: 5,
      satellites: 9,
      hdop: 0.8,
      batteryVoltage: 3.7,
      isStale: false,
    };
    vi.mocked(api.getLatestLocation).mockResolvedValueOnce(location);

    renderDeviceDetailPage("goat-001");

    expect(await screen.findByText("1.5")).toBeInTheDocument();
    expect(screen.getByText(/3\.7V/)).toBeInTheDocument();
    expect(api.getLatestLocation).toHaveBeenCalledWith("goat-001");
  });

  it("shows a stale data warning when isStale is true", async () => {
    const location: Location = {
      deviceId: "goat-002",
      timestamp: "2026-08-14T12:00:00Z",
      latitude: 1,
      longitude: 2,
      isStale: true,
    };
    vi.mocked(api.getLatestLocation).mockResolvedValueOnce(location);

    renderDeviceDetailPage("goat-002");

    expect(await screen.findByText("Stale data warning")).toBeInTheDocument();
  });

  it("shows an error message when the location fails to load", async () => {
    vi.mocked(api.getLatestLocation).mockRejectedValueOnce(new Error("not found"));

    renderDeviceDetailPage("ghost-001");

    expect(await screen.findByRole("alert")).toHaveTextContent("Failed to load location");
  });
});
