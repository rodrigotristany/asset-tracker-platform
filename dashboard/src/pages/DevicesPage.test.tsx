import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { describe, expect, it, vi } from "vitest";
import { DevicesPage } from "./DevicesPage";
import { api } from "../api";
import type { DeviceSummary } from "../types";

vi.mock("../api", () => ({
  api: { getDevicesSummary: vi.fn() },
}));

function renderDevicesPage() {
  const queryClient = new QueryClient();
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <DevicesPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe("DevicesPage", () => {
  it("renders a row per device with its latest location and status", async () => {
    const devices: DeviceSummary[] = [
      {
        deviceId: "goat-001",
        latest: { deviceId: "goat-001", timestamp: "2026-08-14T12:00:00Z", latitude: 1, longitude: 2, isStale: false },
        status: "online",
      },
    ];
    vi.mocked(api.getDevicesSummary).mockResolvedValueOnce(devices);

    renderDevicesPage();

    expect(await screen.findByText("goat-001")).toBeInTheDocument();
    expect(screen.getByText("online")).toBeInTheDocument();
  });

  it("shows a stale warning indicator when isStale is true", async () => {
    const devices: DeviceSummary[] = [
      {
        deviceId: "goat-002",
        latest: { deviceId: "goat-002", timestamp: "2026-08-14T12:00:00Z", latitude: 1, longitude: 2, isStale: true },
        status: "stale",
      },
    ];
    vi.mocked(api.getDevicesSummary).mockResolvedValueOnce(devices);

    renderDevicesPage();

    expect(await screen.findByRole("img", { name: "stale warning" })).toBeInTheDocument();
  });

  it("has a link to register a new device", async () => {
    vi.mocked(api.getDevicesSummary).mockResolvedValueOnce([]);
    renderDevicesPage();

    expect(await screen.findByRole("link", { name: "+ Add Device" })).toHaveAttribute("href", "/devices/new");
  });

  it("links each device row to its detail page", async () => {
    const devices: DeviceSummary[] = [
      {
        deviceId: "goat-003",
        latest: { deviceId: "goat-003", timestamp: "2026-08-14T12:00:00Z", latitude: 1, longitude: 2, isStale: false },
        status: "online",
      },
    ];
    vi.mocked(api.getDevicesSummary).mockResolvedValueOnce(devices);

    renderDevicesPage();

    expect(await screen.findByRole("link", { name: "goat-003" })).toHaveAttribute("href", "/devices/goat-003");
  });
});
