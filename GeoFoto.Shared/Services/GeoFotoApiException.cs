namespace GeoFoto.Shared.Services;

public class GeoFotoApiException : Exception
{
    public GeoFotoApiException(string message, Exception innerException)
        : base(message, innerException) { }
}
