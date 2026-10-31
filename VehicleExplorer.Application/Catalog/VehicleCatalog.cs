using Microsoft.Extensions.Caching.Memory;
using VehicleExplorer.Application.Abstractions;
using VehicleExplorer.Application.Exceptions;
using VehicleExplorer.Domain;

namespace VehicleExplorer.Application.Catalog;

public sealed class VehicleCatalog(INhtsaVehicleClient client, IMemoryCache cache) : IVehicleCatalog
{
    public const int DefaultPageSize = 40;
    public const int MaxPageSize = 100;

    private static readonly TimeSpan MakesTtl = TimeSpan.FromHours(12);
    private static readonly TimeSpan LookupTtl = TimeSpan.FromHours(1);

    public async Task<PagedResult<Make>> GetMakesAsync(
        string? prefix,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        if (skip < 0)
        {
            throw new InvalidVehicleQueryException("Skip must be zero or greater.");
        }

        if (take < 1 || take > MaxPageSize)
        {
            throw new InvalidVehicleQueryException($"Take must be between 1 and {MaxPageSize}.");
        }

        var makes = await GetOrFetchAsync(
            "makes",
            MakesTtl,
            token => client.GetMakesAsync(token),
            cancellationToken);

        var filter = string.IsNullOrWhiteSpace(prefix) ? null : prefix.Trim();
        var matched = filter is null
            ? makes
            : makes.Where(make => make.Name.StartsWith(filter, StringComparison.OrdinalIgnoreCase)).ToArray();

        return new PagedResult<Make>(matched.Skip(skip).Take(take).ToArray(), matched.Count);
    }

    public Task<IReadOnlyList<VehicleType>> GetVehicleTypesAsync(int makeId, CancellationToken cancellationToken)
    {
        EnsureMakeId(makeId);
        return GetOrFetchAsync(
            $"types:{makeId}",
            LookupTtl,
            token => client.GetVehicleTypesAsync(makeId, token),
            cancellationToken);
    }

    public Task<IReadOnlyList<Model>> GetModelsAsync(
        int makeId,
        int year,
        string? vehicleType,
        CancellationToken cancellationToken)
    {
        EnsureMakeId(makeId);
        EnsureModelYear(year);
        var type = string.IsNullOrWhiteSpace(vehicleType) ? null : vehicleType.Trim();

        return GetOrFetchAsync(
            $"models:{makeId}:{year}:{type}",
            LookupTtl,
            token => client.GetModelsAsync(makeId, year, type, token),
            cancellationToken);
    }

    private async Task<IReadOnlyList<T>> GetOrFetchAsync<T>(
        string key,
        TimeSpan ttl,
        Func<CancellationToken, Task<IReadOnlyList<T>>> fetch,
        CancellationToken cancellationToken)
    {
        var results = await cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = ttl;
            return await fetch(cancellationToken);
        });

        return results ?? [];
    }

    private static void EnsureMakeId(int makeId)
    {
        if (makeId <= 0)
        {
            throw new InvalidVehicleQueryException("Make id must be greater than 0.");
        }
    }

    private static void EnsureModelYear(int year)
    {
        if (year < 1996)
        {
            throw new InvalidVehicleQueryException("Model year must be greater than 1995.");
        }
    }
}
