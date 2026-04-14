using FluentAssertions;
using GeoFoto.Api.Services;

namespace GeoFoto.Tests;

public class ExifServiceTests
{
    private readonly ExifService _sut = new();

    [Fact]
    public void ExtractGeoData_ConArchivoSinExif_RetornaNulls()
    {
        // Create a minimal valid JPEG without EXIF data
        var jpegBytes = CreateMinimalJpeg();
        using var stream = new MemoryStream(jpegBytes);

        var result = _sut.ExtractGeoData(stream);

        result.Latitud.Should().BeNull();
        result.Longitud.Should().BeNull();
        result.FechaTomada.Should().BeNull();
    }

    [Fact]
    public void ExtractGeoData_ConStreamVacio_RetornaNulls()
    {
        using var stream = new MemoryStream([]);

        var result = _sut.ExtractGeoData(stream);

        result.Latitud.Should().BeNull();
        result.Longitud.Should().BeNull();
        result.FechaTomada.Should().BeNull();
    }

    private static byte[] CreateMinimalJpeg()
    {
        // Minimal JPEG: SOI + minimal JFIF APP0 + minimal frame + EOI
        return
        [
            0xFF, 0xD8,             // SOI (Start of Image)
            0xFF, 0xE0,             // APP0 marker
            0x00, 0x10,             // Length = 16
            0x4A, 0x46, 0x49, 0x46, 0x00,  // "JFIF\0"
            0x01, 0x01,             // Version 1.1
            0x00,                   // Aspect ratio units (0 = no units)
            0x00, 0x01,             // X density
            0x00, 0x01,             // Y density
            0x00, 0x00,             // Thumbnail dimensions 0x0
            0xFF, 0xDB,             // DQT marker
            0x00, 0x43,             // Length = 67
            0x00,                   // Table 0, 8-bit precision
            // 64 bytes quantization table (all 1s for minimal valid)
            0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
            0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
            0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
            0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
            0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
            0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
            0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
            0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
            0xFF, 0xC0,             // SOF0 marker (Start of Frame)
            0x00, 0x0B,             // Length = 11
            0x08,                   // Precision 8 bits
            0x00, 0x01,             // Height = 1
            0x00, 0x01,             // Width = 1
            0x01,                   // Number of components = 1
            0x01, 0x11, 0x00,       // Component 1: ID=1, sampling=1x1, quant table 0
            0xFF, 0xC4,             // DHT marker
            0x00, 0x1F,             // Length = 31
            0x00,                   // DC table 0
            0x00, 0x01, 0x05, 0x01, 0x01, 0x01, 0x01, 0x01,
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
            0x08, 0x09, 0x0A, 0x0B,
            0xFF, 0xDA,             // SOS marker (Start of Scan)
            0x00, 0x08,             // Length = 8
            0x01,                   // Number of components = 1
            0x01, 0x00,             // Component 1, DC/AC table 0/0
            0x00, 0x3F, 0x00,       // Spectral selection
            0x7B, 0x40,             // Scan data (minimal)
            0xFF, 0xD9              // EOI (End of Image)
        ];
    }
}
