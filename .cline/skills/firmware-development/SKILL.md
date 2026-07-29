---
name: firmware-development
description: ESP-IDF C++ firmware with modern C++23, dependency injection, and hardware-agnostic design
---

triggers:
  paths:
    - "firmware/**"
  keywords:
    - "esp32"
    - "gps"
    - "nmea"
    - "uart"
    - "esp-idf"
    - "firmware"
    - "cmake"

tool_restrictions:
  allowed:
    - read_files
    - write_files
    - run_commands
    - search_codebase
  disallowed:
    - fetch_web_content

hardware_assumptions:
  - Hardware-agnostic (use interfaces/fakes for testability)
  - GY-GPSV3 is the reference implementation, but firmware must not hardcode GPS-specific quirks beyond parsing standards

## When to use

Use this skill whenever editing or creating files under `firmware/`, including ESP-IDF components, C++ headers/sources, CMakeLists, NMEA parsing, GPS readers, WiFi managers, display drivers, storage queues, and main orchestration.

## Instructions

1. `main.cpp` is the **composer root**. It instantiates all major classes and wires dependencies. No other file should instantiate `WifiManager`, `GpsReader`, `ApiClient`, etc.
2. Create files following the dependency tree order when introducing new modules:
   - `domain/` first (pure data, zero dependencies)
   - `utils/` second
   - `gps/` next
   - `network/` next
   - `display/` next
   - `storage/` next
   - `config/` alongside domain as needed
3. `GpsReader` must emit events or return data to the orchestrator. It must not call `WifiManager` or `ApiClient` directly.
4. `Display` reads state from the orchestrator (`main.cpp`), not from other modules directly.
5. Define abstract interfaces for swappable components (e.g., `IGpsReader`, `INetworkManager`) and prefer dependency injection via constructors over global singletons.
6. Apply modern C++ principles: RAII, `std::unique_ptr` for exclusive ownership, `std::optional` for absent values, `constexpr` for compile-time constants, and `std::variant`/`std::expected` where exceptions are unsuitable.
7. Use both ESP-IDF native CMake or PlatformIO. Do not force one toolchain unless explicitly asked.
8. Logs must include a component tag: `[GPS]`, `[WIFI]`, `[API]`, `[STORAGE]`.
9. Use `std::optional<T>` and exceptions/recoverable error types. Avoid sentinel values and magic numbers.
10. Every `.hpp` must have a corresponding `.cpp` unless it is template-only.
