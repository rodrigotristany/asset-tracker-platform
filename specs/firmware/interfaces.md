# Firmware Interfaces

## IGpsReader

```cpp
class IGpsReader {
public:
    virtual ~IGpsReader() = default;
    virtual std::optional<domain::Location> read_location() = 0;
    virtual bool has_fix() const = 0;
    virtual void start() = 0;
    virtual void stop() = 0;
};
```

## INetworkManager

```cpp
class INetworkManager {
public:
    virtual ~INetworkManager() = default;
    virtual bool connect() = 0;
    virtual void disconnect() = 0;
    virtual bool is_connected() const = 0;
    virtual std::optional<std::string> sync_time() = 0; // NTP fallback
};
```

## IApiClient

```cpp
class IApiClient {
public:
    virtual ~IApiClient() = default;
    virtual bool send_location(const domain::Location& location) = 0;
    virtual bool send_batch(const std::vector<domain::Location>& locations) = 0;
    virtual bool is_available() const = 0;
};
```

## IDisplay

```cpp
class IDisplay {
public:
    virtual ~IDisplay() = default;
    virtual void show_location(const domain::Location& location) = 0;
    virtual void show_status(const std::string& status) = 0;
    virtual void clear() = 0;
};
```

## IBatteryMonitor

```cpp
class IBatteryMonitor {
public:
    virtual ~IBatteryMonitor() = default;
    virtual std::optional<double> read_voltage() = 0;
    virtual std::optional<uint8_t> estimate_percentage() const = 0;
};
```

## ILocalQueue

```cpp
class ILocalQueue {
public:
    virtual ~ILocalQueue() = default;
    virtual bool enqueue(const domain::Location& location) = 0;
    virtual std::vector<domain::Location> drain() = 0;
    virtual size_t size() const = 0;
    virtual bool is_full() const = 0;
};
```

## Notes
- All interfaces are pure virtual with virtual destructors.
- Concrete implementations live in their respective modules (`gps/`, `network/`, `display/`, `storage/`).
- `main.cpp` wires concrete types to these interfaces via dependency injection.
