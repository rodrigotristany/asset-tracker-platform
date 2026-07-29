# Frontend Pages

## Routes

| Path | Component | Purpose |
|------|-----------|---------|
| `/` or `/login` | `LoginPage` | Admin JWT login |
| `/devices` | `DevicesPage` | List all devices with latest location + status |
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
- Phase 1: no edit/create/delete flows
- Phase 1: no WebSockets; use polling
- Phase 1: no maps
