using GeoFoto.Shared.Services;
using GeoFoto.Web.Components;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();

var apiBaseUrl = builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5000/";
builder.Services.AddHttpClient<IGeoFotoApiClient, GeoFotoApiClient>(c =>
    c.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddScoped<IFotoUploadStrategy, ApiUploadStrategy>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(GeoFoto.Shared._Imports).Assembly);

app.Run();
