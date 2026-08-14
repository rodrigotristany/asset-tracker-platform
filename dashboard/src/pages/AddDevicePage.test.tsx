import { render, screen, fireEvent } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { describe, expect, it, vi } from "vitest";
import { AddDevicePage } from "./AddDevicePage";
import { api } from "../api";

vi.mock("../api", () => ({
  api: { registerDevice: vi.fn() },
}));

function renderAddDevicePage() {
  const queryClient = new QueryClient();
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <AddDevicePage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe("AddDevicePage", () => {
  it("shows the returned API key once with a persistent warning after successful registration", async () => {
    vi.mocked(api.registerDevice).mockResolvedValueOnce({ deviceId: "goat-005", apiKey: "key-xyz" });
    renderAddDevicePage();

    fireEvent.change(screen.getByLabelText("Device ID"), { target: { value: "goat-005" } });
    fireEvent.click(screen.getByRole("button", { name: "Register" }));

    expect(await screen.findByText("key-xyz")).toBeInTheDocument();
    expect(screen.getByText(/won't be able to see it again/)).toBeInTheDocument();
    expect(api.registerDevice).toHaveBeenCalledWith("goat-005", undefined);
  });

  it("sends the display name when provided", async () => {
    vi.mocked(api.registerDevice).mockResolvedValueOnce({ deviceId: "goat-006", apiKey: "key-abc" });
    renderAddDevicePage();

    fireEvent.change(screen.getByLabelText("Device ID"), { target: { value: "goat-006" } });
    fireEvent.change(screen.getByLabelText("Display Name"), { target: { value: "Goat 006" } });
    fireEvent.click(screen.getByRole("button", { name: "Register" }));

    await screen.findByText("key-abc");
    expect(api.registerDevice).toHaveBeenCalledWith("goat-006", "Goat 006");
  });

  it("shows an inline error and keeps the form filled in on failure", async () => {
    vi.mocked(api.registerDevice).mockRejectedValueOnce(new Error("Device ID already registered"));
    renderAddDevicePage();

    fireEvent.change(screen.getByLabelText("Device ID"), { target: { value: "goat-007" } });
    fireEvent.click(screen.getByRole("button", { name: "Register" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("Device ID already registered");
    expect(screen.getByLabelText("Device ID")).toHaveValue("goat-007");
  });
});
