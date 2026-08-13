namespace AssetTracker.Application.Exceptions;

public class DeviceAlreadyExistsException : Exception
{
    public DeviceAlreadyExistsException(string deviceId) : base($"Device '{deviceId}' already exists.") { }
}
