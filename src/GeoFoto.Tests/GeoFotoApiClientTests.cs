using FluentAssertions;
using GeoFoto.Shared.Services;
using System.Net;

namespace GeoFoto.Tests;

public class GeoFotoApiClientTests
{
    private static GeoFotoApiClient CreateClientWithHandler(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000/") };
        return new GeoFotoApiClient(httpClient);
    }

    [Fact]
    public async Task GetPuntosAsync_CuandoServidorNoDisponible_LanzaGeoFotoApiException()
    {
        var handler = new FakeHandler(_ => throw new HttpRequestException("Connection refused"));
        var client = CreateClientWithHandler(handler);

        var act = () => client.GetPuntosAsync();

        await act.Should().ThrowAsync<GeoFotoApiException>()
            .WithMessage("*obtener puntos*");
    }

    [Fact]
    public async Task GetPuntoAsync_CuandoTimeout_LanzaGeoFotoApiException()
    {
        var handler = new FakeHandler(_ => throw new TaskCanceledException("Timeout"));
        var client = CreateClientWithHandler(handler);

        var act = () => client.GetPuntoAsync(1);

        await act.Should().ThrowAsync<GeoFotoApiException>()
            .WithMessage("*punto 1*");
    }

    [Fact]
    public async Task DeletePuntoAsync_CuandoError_LanzaGeoFotoApiException()
    {
        var handler = new FakeHandler(_ => throw new HttpRequestException("Server error"));
        var client = CreateClientWithHandler(handler);

        var act = () => client.DeletePuntoAsync(5);

        await act.Should().ThrowAsync<GeoFotoApiException>()
            .WithMessage("*eliminar punto 5*");
    }

    [Fact]
    public async Task UploadFotoAsync_CuandoError_LanzaGeoFotoApiException()
    {
        var handler = new FakeHandler(_ => throw new HttpRequestException("Upload failed"));
        var client = CreateClientWithHandler(handler);

        using var stream = new MemoryStream([1, 2, 3]);
        var act = () => client.UploadFotoAsync(stream, "test.jpg");

        await act.Should().ThrowAsync<GeoFotoApiException>()
            .WithMessage("*subir foto*");
    }

    [Fact]
    public async Task SyncBatchAsync_CuandoError_LanzaGeoFotoApiException()
    {
        var handler = new FakeHandler(_ => throw new HttpRequestException("Sync failed"));
        var client = CreateClientWithHandler(handler);

        var act = () => client.SyncBatchAsync([], CancellationToken.None);

        await act.Should().ThrowAsync<GeoFotoApiException>()
            .WithMessage("*batch*");
    }

    private class FakeHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) => _handler = handler;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_handler(request));
    }
}
