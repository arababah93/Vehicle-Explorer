using System.Net.Http.Json;
using System.Text.Json;
using VehicleExplorer.Application.Abstractions;
using VehicleExplorer.Domain;

namespace VehicleExplorer.Infrastructure.Nhtsa;

public sealed class NhtsaVehicleClient(HttpClient httpClient) : INhtsaVehicleClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<IReadOnlyList<Make>> GetMakesAsync(CancellationToken cancellationToken)
    {
        var results = await GetAsync<NhtsaMakeResult>("getallmakes?format=json", cancellationToken);
        return results
            .Select(item => new Make(item.MakeId, item.MakeName?.Trim() ?? string.Empty))
            .Where(item => item.Name.Length > 0)
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<IReadOnlyList<VehicleType>> GetVehicleTypesAsync(int makeId, CancellationToken cancellationToken)
    {
        var results = await GetAsync<NhtsaVehicleTypeResult>(
            $"GetVehicleTypesForMakeId/{makeId}?format=json",
            cancellationToken);

        return results
            .Select(item => new VehicleType(item.VehicleTypeId, item.VehicleTypeName?.Trim() ?? string.Empty))
            .Where(item => item.Name.Length > 0)
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<IReadOnlyList<Model>> GetModelsAsync(
        int makeId,
        int year,
        string? vehicleType,
        CancellationToken cancellationToken)
    {
        var results = await GetAsync<NhtsaModelResult>(ModelsPath(makeId, year, vehicleType), cancellationToken);
        return results
            .Select(item => new Model(
                item.ModelId,
                item.ModelName?.Trim() ?? string.Empty,
                item.MakeId,
                item.MakeName?.Trim() ?? string.Empty))
            .Where(item => item.Name.Length > 0)
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    internal static string ModelsPath(int makeId, int year, string? vehicleType)
    {
        var path = $"GetModelsForMakeIdYear/makeId/{makeId}/modelyear/{year}";
        if (!string.IsNullOrWhiteSpace(vehicleType))
        {
            path += $"/vehicletype/{Uri.EscapeDataString(vehicleType.Trim())}";
        }

        return path + "?format=json";
    }

    private async Task<IReadOnlyList<T>> GetAsync<T>(string relativeUrl, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(relativeUrl, cancellationToken);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<NhtsaResponse<T>>(JsonOptions, cancellationToken);
        return payload?.Results ?? [];
    }
}
