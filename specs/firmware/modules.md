# Firmware Modules — Public API

## GpsReader

**File:** `gps/gps_reader.hpp` / `gps_reader.cpp`

```cpp
class GpsReader : public IGpsReader {
public:
    struct Config { uint8_t uart_port; int tx_pin; int rx_pin; };  // from device_config.hpp
    explicit GpsReader(const Config& config);
    ~GpsReader() override;

    std::optional<domain::Location> read_location() override;
    bool has_fix() const override;
    void start() override;
    void stop() override;

private:
    void parse_nmea(std::string_view sentence);
    bool extract_gpgga(std::string_view sentence);
    bool extract_gprmc(std::string_view sentence);
};
```

## WifiManager

```cpp
class WifiManager : public INetworkManager {
public:
    struct Config { std::string ssid; std::string password; };
    explicit WifiManager(const Config& config);
    ~WifiManager() override;

    bool connect() override;
    void disconnect() override;
    bool is_connected() const override;
    std::optional<std::string> sync_time() override;  // NTP fallback UTC

private:
    void provision(); // SmartConfig / BLE provisioning
};
```

## ApiClient

```cpp
class ApiClient : public IApiClient {
public:
    struct Config { std::string base_url; std::string api_key; };
    explicit ApiClient(INetworkManager& network, const Config& config);
    ~ApiClient() override;

    bool send_location(const domain::Location& location) override;
    bool send_batch(const std::vector<domain::Location>& locations) override;
    bool is_available() const override;

private:
    INetworkManager& network_;
    HttpClient http_;
};
```

## Display

```cpp
class Display : public IDisplay {
public:
    struct Config { bool enabled; /* lcd pins/bus */ };
    explicit Display(const Config& config);
    ~Display() override;

    void show_location(const domain::Location& location) override;
    void show_status(const std::string& status) override;
    void clear() override;
};
```

## BatteryMonitor

```cpp
class BatteryMonitor : public IBatteryMonitor {
public:
    struct Config { gpio_num_t adc_pin; /* calibration values */ };
    explicit BatteryMonitor(const Config& config);
    ~BatteryMonitor() override;

    std::optional<double> read_voltage() override;
    std::optional<uint8_t> estimate_percentage() const override;

private:
    double adc_to_volts(int raw) const;
};
```

## LocalQueue

```cpp
class LocalQueue : public ILocalQueue {
public:
    struct Config { size_t max_entries; /* SPIFFS/LittleFS path */ };
    explicit LocalQueue(const Config& config);
    ~LocalQueue() override;

    bool enqueue(const domain::Location& location) override;
    std::vector<domain::Location> drain() override;
    size_t size() const override;
    bool is_full() const override;

private:
    std::vector<domain::Location> buffer_;
};
```

## Timer

```cpp
class Timer {
public:
    struct Config { TickType_t period_ticks; };
    explicit Timer(const Config& config);
    ~Timer();
    void start();
    void stop();
    bool has_elapsed() const;
};
```
