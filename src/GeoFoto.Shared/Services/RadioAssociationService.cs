using GeoFoto.Shared.Models;

namespace GeoFoto.Shared.Services;

public class RadioAssociationService : IRadioAssociationService
{
    private AppConfig _config = new();

    public int RadioMetros => _config.RadioAsociacionMetros;
    public bool AutoAsociacionHabilitada => _config.AutoAsociacionHabilitada;

    public void Configure(AppConfig config) => _config = config;

    public LocalPunto? BuscarMarkerDentroDeRadio(IEnumerable<LocalPunto> markers, double lat, double lng)
    {
        if (!_config.AutoAsociacionHabilitada) return null;

        LocalPunto? closest = null;
        double closestDist = double.MaxValue;

        foreach (var marker in markers)
        {
            double dist = CalcularDistanciaHaversine(lat, lng, (double)marker.Latitud, (double)marker.Longitud);
            if (dist <= _config.RadioAsociacionMetros && dist < closestDist)
            {
                closest = marker;
                closestDist = dist;
            }
        }

        return closest;
    }

    /// <summary>Calcula distancia en metros entre dos coordenadas usando la fórmula de Haversine.</summary>
    public double CalcularDistanciaHaversine(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6_371_000; // radio de la Tierra en metros
        var phi1 = ToRadians(lat1);
        var phi2 = ToRadians(lat2);
        var dPhi = ToRadians(lat2 - lat1);
        var dLambda = ToRadians(lng2 - lng1);

        var a = Math.Sin(dPhi / 2) * Math.Sin(dPhi / 2) +
                Math.Cos(phi1) * Math.Cos(phi2) *
                Math.Sin(dLambda / 2) * Math.Sin(dLambda / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRadians(double deg) => deg * Math.PI / 180.0;
}
