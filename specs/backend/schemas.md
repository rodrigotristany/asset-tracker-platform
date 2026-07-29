# Backend Schemas — Pydantic DTOs

## LocationCreate (Request Body)

```python
class LocationCreate(BaseModel):
    deviceId: str
    timestamp: datetime  # ISO 8601 UTC
    latitude: float
    longitude: float
    altitude: Optional[float] = None
    speed: Optional[float] = None
    satellites: Optional[int] = None
    hdop: Optional[float] = None
    batteryVoltage: Optional[float] = None
    isStale: bool = False
```

## LocationResponse (Response)

```python
class LocationResponse(BaseModel):
    id: int
    status: str  # e.g. "accepted"
```

## LocationRead (DB -> API)

```python
class LocationRead(BaseModel):
    id: int
    deviceId: str
    timestamp: datetime
    latitude: float
    longitude: float
    altitude: Optional[float]
    speed: Optional[float]
    satellites: Optional[int]
    hdop: Optional[float]
    batteryVoltage: Optional[float]
    isStale: bool

    model_config = {"from_attributes": True}
```

## BatchLocationCreate (Batch Upload)

```python
class BatchLocationCreate(BaseModel):
    deviceId: str
    locations: list[LocationCreate]
```

## Standard Error Envelope

```python
class ErrorResponse(BaseModel):
    error: str
    message: str
    details: Optional[dict] = None
```
