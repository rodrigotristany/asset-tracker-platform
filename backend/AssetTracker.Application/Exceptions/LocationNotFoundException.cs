namespace AssetTracker.Application.Exceptions;

public class LocationNotFoundException : Exception
{
    public LocationNotFoundException(string deviceId) : base($"No location has been recorded for device '{deviceId}'.") { }
}
