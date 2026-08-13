---
paths:
  - "**"
---

# General Coding Rules

## Naming Conventions
- **Functions / variables / files (C++, Python where applicable):** `snake_case`
- **Classes / structs / types (all languages):** `PascalCase`
- **Constants / macros:** `UPPER_SNAKE_CASE`
- **JSON / API payloads:** `camelCase` (matches spec schema)
- **Database columns / tables:** `snake_case` (per backend.md)

### C# (backend) — supersedes the blanket rule above
- **Types, public members, methods:** `PascalCase` (e.g., `LocationService`, `GetLatestByDeviceAsync`).
- **Local variables, method parameters:** `camelCase`.
- **Interfaces:** `I` prefix + `PascalCase` (e.g., `ILocationRepository`).
- **Private fields:** `_camelCase` (e.g., `_connectionString`).
- **Async methods:** `Async` suffix (e.g., `RegisterAsync`).
- **File names:** match the type they contain, `PascalCase.cs` (e.g., `LocationRepository.cs`).

## Comments & Documentation
- Document all **public APIs** (headers, public methods, exported functions).
- Use Doxygen style (`/** ... */`) for C++ firmware.
- Use Google-style docstrings for Python backend.
- Use JSDoc for TypeScript dashboard code.
- No requirement to document private helpers unless logic is non-obvious.

## Error Handling
### Firmware (C++)
- Use `std::optional<T>` for absent values instead of sentinel values.
- C++ exceptions are **permitted** for recoverable errors (enabled in ESP-IDF configuration).
- Use `std::expected<T, Error>` (C++23) or `esp_err_t` where exceptions are unsuitable.
- Return `Result<T>` types for operations that can fail with context.
- Always log errors before recovery actions.

### Backend (C#)
- Use `DataAnnotations` validation for all incoming DTOs.
- Raise custom exceptions (`Application.Exceptions`) for business logic errors.
- Return structured JSON error responses (see backend.md).
- Never leak stack traces in production responses.

## Code Organization
- **Circular dependencies: Strictly prohibited.** Use forward declarations and dependency inversion.
- **C++ header/source pairing:** Every `.hpp` must have a corresponding `.cpp` unless template-only header.
- No hard file-length limit; split when a file becomes difficult to navigate.

## Modern C++ Requirements (Firmware)
- **RAII:** Mandatory for all resources (memory, handles, sockets).
- **Smart pointers:** Prefer `std::unique_ptr` for exclusive ownership. `std::shared_ptr` only when shared ownership is required. Avoid raw `new`/`delete`.
- **`std::optional` / `std::variant`:** Use instead of sentinel values, out-parameters, or magic numbers.
- **`constexpr`:** Use for compile-time constants and trivial calculations.
- **Interfaces:** Define abstract base classes (pure virtual) for swappable components (`IGpsReader`, `INetworkManager`).
- **Dependency Injection:** Pass dependencies via constructors. Avoid global singletons for core components.

## Logging & Observability
- All backend log entries must include `request_id` (from middleware).
- All backend log entries involving a device must include `device_id`.
- Log format: JSON structured in production; human-readable in development.
- Firmware logs should include component tag: `[GPS]`, `[WIFI]`, `[API]`, `[STORAGE]`.

