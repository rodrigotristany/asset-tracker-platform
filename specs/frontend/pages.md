# Frontend Pages

## Routes

| Path | Component | Purpose |
|------|-----------|---------|
| `/` or `/login` | `LoginPage` | Admin JWT login |
| `/devices` | `DevicesPage` | List all devices with latest location + status |
| `/devices/new` | `AddDevicePage` | Register a new device |
| `/devices/:deviceId` | `DeviceDetailPage` | Detailed GPS data for one device |

## DevicesPage

### Data Shape
```typescript
const { data } = useQuery<DeviceSummary[]>({
  queryKey: ["devices"],
  queryFn: () => api.getDevicesSummary(),
  refetchInterval: 5000, // polling OK
});
```

### UI Requirements
- Table/list of devices
- Columns: Device ID, Last Timestamp, Latitude, Longitude, Status
- Status derived from `is_stale` and timestamp age (e.g., >60s = offline)
- Stale indicator: visual warning when `is_stale === true`
- "+ Add Device" button, links to `/devices/new`

## AddDevicePage

### Data Shape
```typescript
const mutation = useMutation<DeviceRegistrationResult, Error, DeviceRegistrationRequest>({
  mutationFn: (req) => api.registerDevice(req.deviceId, req.displayName),
});
```

### UI Requirements
- Form fields: Device ID (required, text input), Display Name (optional, text input)
- Submit calls `api.registerDevice(deviceId, displayName)`
- On success: display the returned `apiKey` once, with a persistent warning that it will not be shown again (e.g., "Copy this now — you won't be able to see it again")
- After acknowledgment, link back to `/devices`
- On error (e.g., duplicate Device ID): show the error message inline, keep the form filled in

## DeviceDetailPage

### Data Shape
```typescript
const { data } = useQuery<Location>({
  queryKey: ["locations", deviceId],
  queryFn: () => api.getLatestLocation(deviceId),
  refetchInterval: 2000,
});
```

### UI Requirements
- Read-only card showing full GPS payload
- Battery voltage indicator if available
- No historical path or map

## Notes
- Phase 1: device registration only (no edit/delete); locations remain read-only, no create/edit UI for location data
- Phase 1: no WebSockets; use polling
- Phase 1: no maps
