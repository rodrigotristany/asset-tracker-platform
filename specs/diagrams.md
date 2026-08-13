# System Diagrams

> Rendered on GitHub/GitLab via Mermaid. If you don't see diagrams, view in a Mermaid-compatible markdown viewer.

---

## 1. System Architecture

```mermaid
flowchart LR
    subgraph Hardware [Hardware Layer]
        GPS[GY-GPSV3 GPS Module]
        ESP32[ESP32-S3 + LCD]
        LU[USB Logic Analyzer]
    end

    subgraph Phase1 [Phase 1: WiFi Streaming]
        FW[ESP32 Firmware C++]
        NET[WiFi @ 1Hz]
    end

    subgraph Phase2 [Phase 2: Battery + BLE]
        BLE[BLE Gateway]
        FLASH[Local Flash Storage]
    end

    subgraph Backend [Backend Layer]
        API[ASP.NET Core REST API]
        DB[(SQL Server 30d retention)]
    end

    subgraph Frontend [Frontend Layer]
        DASH[React + TS Dashboard]
    end

    GPS -- UART NMEA --> ESP32
    LU -.-> ESP32
    ESP32 --> FW
    FW --> NET
    NET --> API
    ESP32 -.-> FLASH
    FLASH --> BLE
    BLE --> API
    API --> DB
    DB --> DASH
    DASH -. polls .-> API

    style Hardware fill:#f9f,stroke:#333,stroke-width:2px
    style Phase1 fill:#bbf,stroke:#333,stroke-width:1px
    style Phase2 fill:#bbf,stroke:#333,stroke-width:1px,stroke-dasharray: 5 5
    style Backend fill:#bfb,stroke:#333,stroke-width:2px
    style Frontend fill:#ffb,stroke:#333,stroke-width:2px
```

---

## 2. Data Flow — Phase 1 vs Phase 2

### Phase 1: WiFi Streaming (Active Development)

```mermaid
flowchart TD
    A[GPS acquires fix] --> B{WiFi connected?}
    B -->|Yes| C[POST /api/v1/locations]
    B -->|No| D[Buffer to LocalQueue SPIFFS]
    D --> E{WiFi reconnected?}
    E -->|Yes| C
    E -->|No| D
    C --> F[201 Accepted]
    F --> G[Persist to SQL Server]
    G --> H[Dashboard polls GET /locations/device_id]
    H --> I[Display latest location]

    C -->|3xx retry| C
    C -->|fail after 3 retries| J[Send last known position + is_stale=true]
```

### Phase 2: BLE Gateway (Future)

```mermaid
flowchart TD
    A[GPS records @ high frequency] --> B[Store in local flash]
    B --> C[BLE gateway connects]
    C --> D[Gateway reads batch]
    D --> E[POST /api/v1/locations/batch]
    E --> F[Persist to SQL Server]
    F --> G[Dashboard displays]
```

---

## 3. Firmware Component Diagram

```mermaid
classDiagram
    class main {
        +int run()
        -Compose dependencies
        -Orchestrate loop
    }

    class IGpsReader {
        <<interface>>
        +read_location() optional~Location~
        +has_fix() bool
        +start()
        +stop()
    }

    class INetworkManager {
        <<interface>>
        +connect() bool
        +disconnect()
        +is_connected() bool
        +sync_time() optional~string~
    }

    class IApiClient {
        <<interface>>
        +send_location(Location) bool
        +send_batch(vector~Location~) bool
        +is_available() bool
    }

    class IDisplay {
        <<interface>>
        +show_location(Location)
        +show_status(string)
        +clear()
    }

    class IBatteryMonitor {
        <<interface>>
        +read_voltage() optional~double~
        +estimate_percentage() optional~uint8_t~
    }

    class ILocalQueue {
        <<interface>>
        +enqueue(Location) bool
        +drain() vector~Location~
        +size() size_t
        +is_full() bool
    }

    class GpsReader {
        +start()
        +read_location()
        -parse_nmea()
    }

    class WifiManager {
        +connect()
        +sync_time()
        -provision()
    }

    class ApiClient {
        +send_location()
        +send_batch()
        -retry_with_backoff()
    }

    class Display {
        +show_location()
        +show_status()
    }

    class BatteryMonitor {
        +read_voltage()
    }

    class LocalQueue {
        +enqueue()
        +drain()
    }

    class Location {
        +string device_id
        +string timestamp
        +double latitude
        +double longitude
        +double altitude
        +double speed
        +uint8_t satellites
        +double hdop
        +double battery_voltage
        +bool is_stale
    }

    main --> IGpsReader
    main --> INetworkManager
    main --> IApiClient
    main --> IDisplay
    main --> IBatteryMonitor
    main --> ILocalQueue

    IGpsReader <|.. GpsReader
    INetworkManager <|.. WifiManager
    IApiClient <|.. ApiClient
    IDisplay <|.. Display
    IBatteryMonitor <|.. BatteryMonitor
    ILocalQueue <|.. LocalQueue

    ApiClient --> INetworkManager : depends on
    GpsReader --> Location : produces
    ApiClient --> Location : sends

    Location <-- GpsReader
    Location <-- ApiClient
```

---

## 4. Backend Layer Architecture

```mermaid
flowchart TD
    subgraph Api [AssetTracker.Api]
        C1[LocationsController]
        C2[DevicesController]
        C3[AuthController]
    end

    subgraph App [AssetTracker.Application]
        S1[LocationService]
        S2[DeviceService]
        S3[AuthService]
        D1[Dtos]
    end

    subgraph Infra [AssetTracker.Infrastructure]
        REPO1[LocationRepository - Dapper/SPs]
        REPO2[DeviceRepository - Dapper/SPs + EF read]
        REPO3[AdminUserRepository - EF Core]
        CTX[AssetTrackerDbContext]
    end

    subgraph DB [SQL Server]
        T1[(locations)]
        T2[(devices)]
        T3[(admin_users)]
        SP[Stored Procedures]
    end

    C1 --> S1
    C2 --> S2
    C3 --> S3
    S1 --> REPO1
    S2 --> REPO2
    S3 --> REPO3
    REPO1 --> SP
    REPO2 --> SP
    REPO2 --> CTX
    REPO3 --> CTX
    SP --> T1
    SP --> T2
    CTX --> T2
    CTX --> T3
    C1 --> D1
    C2 --> D1
    C3 --> D1

    style Api fill:#bbf,stroke:#333,stroke-width:2px
    style App fill:#bfb,stroke:#333,stroke-width:2px
    style Infra fill:#ffb,stroke:#333,stroke-width:2px
    style DB fill:#f99,stroke:#333,stroke-width:2px
```

---

## 5. Frontend Component Tree

```mermaid
flowchart TD
    APP[App]
    AUTH[AuthProvider]
    QUERY[QueryClientProvider]
    ROUTER[React Router v6]

    LOGIN[LoginPage]
    LAYOUT[DashboardLayout]
    DEVICES[DevicesPage]
    DETAIL[DeviceDetailPage]

    CARD[DeviceCard]
    STATUS[StatusIndicator]
    TABLE[DeviceTable]

    API[ApiClient src/services/]
    TYPES[types src/services/index.ts]

    APP --> AUTH
    APP --> QUERY
    APP --> ROUTER

    ROUTER --> LOGIN
    ROUTER --> LAYOUT

    LAYOUT --> DEVICES
    LAYOUT --> DETAIL

    DEVICES --> TABLE
    TABLE --> CARD
    CARD --> STATUS

    DETAIL --> STATUS

    DEVICES --> API
    DETAIL --> API
    API --> TYPES

    style APP fill:#ffb,stroke:#333,stroke-width:2px
    style LOGIN fill:#bbf,stroke:#333,stroke-width:1px
    style DEVICES fill:#bfb,stroke:#333,stroke-width:1px
    style DETAIL fill:#bfb,stroke:#333,stroke-width:1px
    style API fill:#f9f,stroke:#333,stroke-width:1px,stroke-dasharray: 5 5
```
