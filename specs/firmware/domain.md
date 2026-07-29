# Firmware Domain Model

## Location

```cpp
namespace domain {

struct Location {
    std::string device_id;
    std::string timestamp;        // ISO 8601 UTC
    double latitude;
    double longitude;
    double altitude;
    double speed;
    uint8_t satellites;
    double hdop;
    double battery_voltage;
    bool is_stale;
};

} // namespace domain
```

## Guidelines
- Pure data, zero dependencies on other firmware modules.
- Use `std::string` for variable-length text; prefer fixed-width types for numeric sensor data.
- `timestamp` is ISO 8601 UTC from NMEA or NTP.
