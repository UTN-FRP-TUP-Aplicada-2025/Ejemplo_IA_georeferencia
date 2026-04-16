using System.Net.Http.Json;
using GeoFoto.Shared.Models;

namespace GeoFoto.Shared.Services;

public interface IGeoFotoApiClient
{
    Task<List<PuntoDto>> GetPuntosAsync();
    Task<PuntoDetalleDto?> GetPuntoAsync(int id);
    Task<bool> UpdatePuntoAsync(int id, ActualizarPuntoRequest req);
    Task<bool> DeletePuntoAsync(int id);
    Task<UploadResultDto?> UploadFotoAsync(Stream fileStream, string fileName);
    Task<List<FotoDto>> GetFotosByPuntoAsync(int puntoId);
    string GetFotoUrl(int fotoId);
    Task AgregarFotoAPuntoAsync(int puntoId, Stream stream, string nombre, string tipo, CancellationToken ct = default);
    Task<bool> DeleteFotoAsync(int fotoId, CancellationToken ct = default);
    Task<Stream?> DescargarImagenAsync(int fotoId, CancellationToken ct = default);
    Task<SyncDeltaDto> GetSyncDeltaAsync(string? since, CancellationToken ct = default);
    Task<BatchResultDto> SyncBatchAsync(IReadOnlyList<SyncOperationDto> operations, CancellationToken ct = default);
    // GEO-US32: null → punto no existe o sin fotos (204); bytes → ZIP válido
    Task<byte[]?> DescargarFotosZipAsync(int puntoRemoteId, CancellationToken ct = default);
}

public class GeoFotoApiClient : IGeoFotoApiClient
{
    private readonly HttpClient _http;

    public GeoFotoApiClient(HttpClient http) => _http = http;

    public async Task<List<PuntoDto>> GetPuntosAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<PuntoDto>>("api/puntos") ?? [];
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new GeoFotoApiException("Error al obtener puntos", ex);
        }
    }

    public async Task<PuntoDetalleDto?> GetPuntoAsync(int id)
    {
        try
        {
            return await _http.GetFromJsonAsync<PuntoDetalleDto>($"api/puntos/{id}");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new GeoFotoApiException($"Error al obtener punto {id}", ex);
        }
    }

    public async Task<bool> UpdatePuntoAsync(int id, ActualizarPuntoRequest req)
    {
        try
        {
            var resp = await _http.PutAsJsonAsync($"api/puntos/{id}", req);
            return resp.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new GeoFotoApiException($"Error al actualizar punto {id}", ex);
        }
    }

    public async Task<bool> DeletePuntoAsync(int id)
    {
        try
        {
            var resp = await _http.DeleteAsync($"api/puntos/{id}");
            return resp.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new GeoFotoApiException($"Error al eliminar punto {id}", ex);
        }
    }

    public async Task<UploadResultDto?> UploadFotoAsync(Stream fileStream, string fileName)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            using var streamContent = new StreamContent(fileStream);
            content.Add(streamContent, "file", fileName);

            var resp = await _http.PostAsync("api/fotos/upload", content);
            resp.EnsureSuccessStatusCode();

            return await resp.Content.ReadFromJsonAsync<UploadResultDto>();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new GeoFotoApiException($"Error al subir foto {fileName}", ex);
        }
    }

    public async Task<List<FotoDto>> GetFotosByPuntoAsync(int puntoId)
    {
        try
        {
            return await _http.GetFromJsonAsync<List<FotoDto>>($"api/fotos/{puntoId}") ?? [];
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new GeoFotoApiException($"Error al obtener fotos del punto {puntoId}", ex);
        }
    }

    public string GetFotoUrl(int fotoId)
        => $"{_http.BaseAddress}api/fotos/imagen/{fotoId}";

    public async Task AgregarFotoAPuntoAsync(
        int puntoId, Stream stream, string nombre, string tipo,
        CancellationToken ct = default)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            var sc = new StreamContent(stream);
            sc.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(tipo);
            content.Add(sc, "file", nombre);
            var response = await _http.PostAsync(
                $"api/fotos/upload-to-punto/{puntoId}", content, ct);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new GeoFotoApiException($"Error al agregar foto al punto {puntoId}", ex);
        }
    }

    public async Task<bool> DeleteFotoAsync(int fotoId, CancellationToken ct = default)
    {
        try
        {
            var resp = await _http.DeleteAsync($"api/fotos/{fotoId}", ct);
            return resp.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new GeoFotoApiException($"Error al eliminar foto {fotoId}", ex);
        }
    }

    public async Task<Stream?> DescargarImagenAsync(int fotoId, CancellationToken ct = default)
    {
        try
        {
            var resp = await _http.GetAsync($"api/fotos/imagen/{fotoId}", ct);
            if (!resp.IsSuccessStatusCode) return null;
            return await resp.Content.ReadAsStreamAsync(ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new GeoFotoApiException($"Error al descargar imagen {fotoId}", ex);
        }
    }

    public async Task<SyncDeltaDto> GetSyncDeltaAsync(string? since, CancellationToken ct = default)
    {
        try
        {
            var url = string.IsNullOrEmpty(since)
                ? "api/sync/delta"
                : $"api/sync/delta?since={Uri.EscapeDataString(since)}";
            return await _http.GetFromJsonAsync<SyncDeltaDto>(url, ct)
                ?? new SyncDeltaDto([], []);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new GeoFotoApiException("Error al obtener delta de sincronización", ex);
        }
    }

    public async Task<BatchResultDto> SyncBatchAsync(IReadOnlyList<SyncOperationDto> operations, CancellationToken ct = default)
    {
        try
        {
            var resp = await _http.PostAsJsonAsync("api/sync/batch", operations, ct);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<BatchResultDto>(ct)
                ?? new BatchResultDto([]);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new GeoFotoApiException("Error al enviar batch de sincronización", ex);
        }
    }

    public async Task<byte[]?> DescargarFotosZipAsync(int puntoRemoteId, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync($"api/puntos/{puntoRemoteId}/fotos/download", ct);
            if (response.StatusCode == System.Net.HttpStatusCode.NoContent) return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync(ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new GeoFotoApiException($"Error al descargar ZIP del punto {puntoRemoteId}", ex);
        }
    }
}
