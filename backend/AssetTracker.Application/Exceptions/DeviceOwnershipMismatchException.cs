namespace AssetTracker.Application.Exceptions;

/// <summary>
/// Thrown when an authenticated device's API key is used to act on behalf of a different
/// device (e.g. a location upload whose body claims a deviceId other than the one the
/// caller authenticated as). The caller is authenticated but not authorized for this
/// resource, so this maps to 403 Forbidden rather than 401 Unauthorized.
/// </summary>
public class DeviceOwnershipMismatchException : Exception
{
    public DeviceOwnershipMismatchException(string authenticatedDeviceId, string requestedDeviceId)
        : base($"Authenticated device '{authenticatedDeviceId}' is not authorized to submit data for device '{requestedDeviceId}'.")
    {
    }
}
