using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AxizGeo.Application.Abstractions;
using AxizGeo.Application.Common;
using AxizGeo.Application.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AxizGeo.Infrastructure.Routing;

public sealed class OrsOptions
{
    public const string SectionName = "Ors";

    public string BaseUrl { get; set; } = "https://api.heigit.org/openrouteservice/";
    public string ApiKey { get; set; } = string.Empty;
}

/// <summary>
/// Patrón Adapter: adapta la API externa HeiGIT OpenRouteService (perfil driving-hgv) al puerto IRoutingProvider del dominio.
/// </summary>
public sealed class OpenRouteServiceAdapter(
    HttpClient httpClient,
    IOptions<OrsOptions> options,
    ILogger<OpenRouteServiceAdapter> logger) : IRoutingProvider
{
    private readonly OrsOptions _options = options.Value;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<CalculatedRouteResult> CalculateTruckRouteAsync(
        IReadOnlyList<GeoJsonPointDto> waypoints,
        TruckDimensions? truck = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new BadGatewayException("El servicio de cálculo de rutas ('Ors:ApiKey') no está configurado.");

        object? restrictions = null;
        if (truck is { HeightMeters: not null } or { WeightTons: not null })
        {
            restrictions = new
            {
                height = truck.HeightMeters,
                weight = truck.WeightTons
            };
        }

        var payload = new
        {
            coordinates = waypoints.Select(w => w.Coordinates).ToArray(),
            radiuses = Enumerable.Repeat(1000.0, waypoints.Count).ToArray(),
            options = restrictions is null
                ? null
                : new
                {
                    vehicle_type = "hgv",
                    profile_params = new { restrictions }
                }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "v2/directions/driving-hgv/geojson");
        request.Headers.TryAddWithoutValidation("Authorization", _options.ApiKey);
        request.Content = JsonContent.Create(payload, options: JsonOptions);

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(request, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Error de red al consultar OpenRouteService.");
            throw new BadGatewayException("No fue posible conectar con el proveedor de rutas para tractocamión.");
        }

        var rawBody = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            if (rawBody.Contains("2010", StringComparison.Ordinal))
            {
                throw new ArgumentException("Uno de los puntos seleccionados está demasiado lejos de una vialidad apta para tractocamión.");
            }

            logger.LogWarning("OpenRouteService devolvió HTTP {StatusCode}: {Body}", (int)response.StatusCode, rawBody);
            throw new BadGatewayException("El proveedor externo de rutas rechazó la solicitud de cálculo.");
        }

        using var doc = JsonDocument.Parse(rawBody);
        var features = doc.RootElement.GetProperty("features");
        if (features.GetArrayLength() == 0)
            throw new BadGatewayException("El proveedor de rutas no devolvió ningún trazo disponible.");

        var feature = features[0];
        var coordsElement = feature.GetProperty("geometry").GetProperty("coordinates");
        var coordinates = coordsElement.Deserialize<double[][]>(JsonOptions)
            ?? throw new BadGatewayException("Formato de coordenadas inválido en la respuesta de ruteo.");

        var summary = feature.GetProperty("properties").GetProperty("summary");
        var distanceMeters = summary.TryGetProperty("distance", out var distProp) ? distProp.GetDouble() : 0.0;
        var durationSeconds = summary.TryGetProperty("duration", out var durProp) ? durProp.GetDouble() : 0.0;

        var lineString = GeoJsonMapper.ToLineString(new GeoJsonLineStringDto("LineString", coordinates));
        return new CalculatedRouteResult(lineString, distanceMeters, durationSeconds);
    }
}
