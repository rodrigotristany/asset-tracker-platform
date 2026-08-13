namespace AssetTracker.Application.Exceptions;

public class DeviceNotFoundException : Exception
{
    public DeviceNotFoundException(string deviceId) : base($"Device '{deviceId}' was not found.") { }
}
