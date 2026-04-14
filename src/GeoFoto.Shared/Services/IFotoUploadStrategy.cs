using GeoFoto.Shared.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace GeoFoto.Shared.Services;

public interface IFotoUploadStrategy
{
    Task<UploadResultDto?> SubirAsync(IBrowserFile file, CancellationToken ct = default);
    Task<UploadResultDto?> SubirAsync(Stream stream, string fileName, CancellationToken ct = default);
}

public class ApiUploadStrategy : IFotoUploadStrategy
{
    private readonly IGeoFotoApiClient _api;

    public ApiUploadStrategy(IGeoFotoApiClient api) => _api = api;

    public async Task<UploadResultDto?> SubirAsync(IBrowserFile file, CancellationToken ct = default)
    {
        using var stream = file.OpenReadStream(maxAllowedSize: 50 * 1024 * 1024);
        return await _api.UploadFotoAsync(stream, file.Name);
    }

    public Task<UploadResultDto?> SubirAsync(Stream stream, string fileName, CancellationToken ct = default)
        => _api.UploadFotoAsync(stream, fileName);
}
