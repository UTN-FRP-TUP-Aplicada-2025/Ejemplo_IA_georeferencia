using GeoFoto.Api.Data;
using GeoFoto.Api.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<GeoFotoDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("GeoFoto")));

builder.Services.AddScoped<IExifService, ExifService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IPuntosService, PuntosService>();
builder.Services.AddScoped<IFotosService, FotosService>();
builder.Services.AddScoped<ISyncApiService, SyncApiService>();

builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = 52_428_800);

builder.Services.AddCors(o => o.AddPolicy("GeoFoto", p =>
    p.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? [])
     .AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("GeoFoto");
app.UseStaticFiles();
app.MapControllers();

app.Run();

// Exponer Program para WebApplicationFactory en GeoFoto.Tests
public partial class Program { }
