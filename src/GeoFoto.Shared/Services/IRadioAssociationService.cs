using GeoFoto.Shared.Models;

namespace GeoFoto.Shared.Services;

public interface IRadioAssociationService
{
    int RadioMetros { get; }
    bool AutoAsociacionHabilitada { get; }
    void Configure(AppConfig config);
    LocalPunto? BuscarMarkerDentroDeRadio(IEnumerable<LocalPunto> markers, double lat, double lng);
    double CalcularDistanciaHaversine(double lat1, double lng1, double lat2, double lng2);
}
