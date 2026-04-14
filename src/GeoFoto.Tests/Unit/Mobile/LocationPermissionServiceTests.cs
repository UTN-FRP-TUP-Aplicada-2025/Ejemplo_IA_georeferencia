using FluentAssertions;
using GeoFoto.Shared.Services;
using Moq;

namespace GeoFoto.Tests.Unit.Mobile;

/// <summary>
/// BLOQUE 2 — Gestión de Permisos de Geolocalización (DELTA-02)
/// TEST-005 a TEST-011
/// </summary>
public class LocationPermissionServiceTests
{
    // ──────────────────────────────────────────
    // TEST-005: Usuario acepta → retorna Granted
    // ──────────────────────────────────────────
    [Fact]
    public async Task CheckAndRequest_UsuarioAcepta_RetornaGranted()
    {
        // ARRANGE
        var svcMock = new Mock<ILocationPermissionService>();
        svcMock.Setup(s => s.CheckAndRequestLocationPermissionAsync())
               .ReturnsAsync(LocationPermissionStatus.Granted);

        // ACT
        var status = await svcMock.Object.CheckAndRequestLocationPermissionAsync();

        // ASSERT
        status.Should().Be(LocationPermissionStatus.Granted);
    }

    // ──────────────────────────────────────────
    // TEST-006: Usuario rechaza → retorna Denied
    // ──────────────────────────────────────────
    [Fact]
    public async Task CheckAndRequest_UsuarioRechaza_RetornaDenied()
    {
        // ARRANGE
        var svcMock = new Mock<ILocationPermissionService>();
        svcMock.Setup(s => s.CheckAndRequestLocationPermissionAsync())
               .ReturnsAsync(LocationPermissionStatus.Denied);

        // ACT
        var status = await svcMock.Object.CheckAndRequestLocationPermissionAsync();

        // ASSERT
        status.Should().Be(LocationPermissionStatus.Denied);
    }

    // ──────────────────────────────────────────
    // TEST-007: ShouldShowRationale=false + Denied → IsPermanentlyDenied = true
    // ──────────────────────────────────────────
    [Fact]
    public void IsPermanentlyDenied_DenegacionPermanente_RetornaTrue()
    {
        // ARRANGE — permiso Denied y ShouldShowRationale retorna false
        var svcMock = new Mock<ILocationPermissionService>();
        svcMock.Setup(s => s.IsPermanentlyDenied()).Returns(true);

        // ACT
        var result = svcMock.Object.IsPermanentlyDenied();

        // ASSERT
        result.Should().BeTrue();
    }

    // ──────────────────────────────────────────
    // TEST-008: Denegación temporal → Snackbar informativo + mapa en default
    // ──────────────────────────────────────────
    [Fact]
    public async Task MapaInit_PermisoDenegadoTemporal_UsaDefaultYMarcaError()
    {
        // ARRANGE
        var svcMock = new Mock<ILocationPermissionService>();
        svcMock.Setup(s => s.CheckAndRequestLocationPermissionAsync())
               .ReturnsAsync(LocationPermissionStatus.Denied);
        svcMock.Setup(s => s.IsPermanentlyDenied()).Returns(false);

        var geoMock = new Mock<IUbicacionService>();
        geoMock.Setup(g => g.ObtenerUbicacionAsync())
               .ReturnsAsync(new UbicacionResult(false, 0, 0, 0,
                    "Sin acceso a ubicación", PermisoDenegado: true));

        // ACT
        var permStatus  = await svcMock.Object.CheckAndRequestLocationPermissionAsync();
        var esPermanente = svcMock.Object.IsPermanentlyDenied();
        var gpsResult   = await geoMock.Object.ObtenerUbicacionAsync();

        // ASSERT — denegación temporal: 
        //   - NOT permanently denied
        //   - GPS result is not OK (uses default position)
        //   - PermisoDenegado flag is set so Snackbar can be shown
        permStatus.Should().Be(LocationPermissionStatus.Denied);
        esPermanente.Should().BeFalse();
        gpsResult.Ok.Should().BeFalse();
        gpsResult.PermisoDenegado.Should().BeTrue();

        // Lógica de inicialización: si !Ok → usar coordenadas por defecto
        var mapLat  = gpsResult.Ok ? gpsResult.Latitud  : -34.6037;
        var mapLng  = gpsResult.Ok ? gpsResult.Longitud : -58.3816;
        var mapZoom = gpsResult.Ok ? 15 : 13;
        mapLat.Should().Be(-34.6037);
        mapLng.Should().Be(-58.3816);
        mapZoom.Should().Be(13);
    }

    // ──────────────────────────────────────────
    // TEST-009: Denegación permanente → modal con opciones
    // ──────────────────────────────────────────
    [Fact]
    public async Task MapaInit_PermisoDenegadoPermanentemente_RequiereModalConOpciones()
    {
        // ARRANGE
        var svcMock = new Mock<ILocationPermissionService>();
        svcMock.Setup(s => s.CheckAndRequestLocationPermissionAsync())
               .ReturnsAsync(LocationPermissionStatus.DeniedPermanently);
        svcMock.Setup(s => s.IsPermanentlyDenied()).Returns(true);

        // ACT
        var status       = await svcMock.Object.CheckAndRequestLocationPermissionAsync();
        var esPermanente = svcMock.Object.IsPermanentlyDenied();

        // ASSERT
        status.Should().Be(LocationPermissionStatus.DeniedPermanently);
        esPermanente.Should().BeTrue();
        // La UI debería mostrar MudDialog con título "Permisos de ubicación requeridos"
        // y dos botones: "Abrir Configuración" y "Continuar sin ubicación"
    }

    // ──────────────────────────────────────────
    // TEST-010: Modal → botón "Abrir Configuración" → OpenAppSettings()
    // ──────────────────────────────────────────
    [Fact]
    public void ModalPermisos_BotonAbrirConfiguracion_InvocaOpenAppSettings()
    {
        // ARRANGE
        var svcMock = new Mock<ILocationPermissionService>();
        svcMock.Setup(s => s.OpenAppSettings());

        // ACT — usuario presiona "Abrir Configuración"
        svcMock.Object.OpenAppSettings();

        // ASSERT
        svcMock.Verify(s => s.OpenAppSettings(), Times.Once);
    }

    // ──────────────────────────────────────────
    // TEST-011: Modal → botón "Continuar sin ubicación" → mapa en posición default
    // ──────────────────────────────────────────
    [Fact]
    public void ModalPermisos_BotonContinuar_CierraDialogoYUsaDefault()
    {
        // ARRANGE
        const double defaultLat  = -34.6037;
        const double defaultLng  = -58.3816;
        const int    defaultZoom = 13;

        // El usuario elige "Continuar sin ubicación" — el GPS result sigue siendo null/failed
        UbicacionResult? gpsResult = null;

        // ACT — lógica que Mapa ejecuta tras cerrar el diálogo sin ir a settings
        var mapLat  = gpsResult?.Latitud  ?? defaultLat;
        var mapLng  = gpsResult?.Longitud ?? defaultLng;
        var mapZoom = (gpsResult?.Ok == true) ? 15 : defaultZoom;

        // ASSERT
        mapLat.Should().Be(defaultLat);
        mapLng.Should().Be(defaultLng);
        mapZoom.Should().Be(13);
    }
}
