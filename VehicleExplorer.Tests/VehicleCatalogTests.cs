using Microsoft.Extensions.Caching.Memory;
using VehicleExplorer.Application.Abstractions;
using VehicleExplorer.Application.Catalog;
using VehicleExplorer.Application.Exceptions;
using VehicleExplorer.Domain;

namespace VehicleExplorer.Tests;

public sealed class VehicleCatalogTests
{
    [Fact]
    public async Task GetModels_rejects_year_1995_without_calling_client()
    {
        var client = new FakeNhtsaClient();
        var catalog = CreateCatalog(client);

        var error = await Assert.ThrowsAsync<InvalidVehicleQueryException>(
            () => catalog.GetModelsAsync(1, 1995, null, CancellationToken.None));

        Assert.Equal("Model year must be greater than 1995.", error.Message);
        Assert.Equal(0, client.ModelCalls);
    }

    [Fact]
    public async Task GetModels_accepts_year_1996()
    {
        var client = new FakeNhtsaClient();
        var catalog = CreateCatalog(client);

        var models = await catalog.GetModelsAsync(1, 1996, null, CancellationToken.None);

        Assert.Equal(1, client.ModelCalls);
        Assert.Single(models);
    }

    [Fact]
    public async Task GetModels_rejects_make_id_zero_without_calling_client()
    {
        var client = new FakeNhtsaClient();
        var catalog = CreateCatalog(client);

        var error = await Assert.ThrowsAsync<InvalidVehicleQueryException>(
            () => catalog.GetModelsAsync(0, 2015, null, CancellationToken.None));

        Assert.Equal("Make id must be greater than 0.", error.Message);
        Assert.Equal(0, client.ModelCalls);
    }

    private static VehicleCatalog CreateCatalog(FakeNhtsaClient client)
    {
        return new VehicleCatalog(client, new MemoryCache(new MemoryCacheOptions()));
    }

    private sealed class FakeNhtsaClient : INhtsaVehicleClient
    {
        public int ModelCalls { get; private set; }

        public Task<IReadOnlyList<Make>> GetMakesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<Make>>([]);
        }

        public Task<IReadOnlyList<VehicleType>> GetVehicleTypesAsync(int makeId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<VehicleType>>([]);
        }

        public Task<IReadOnlyList<Model>> GetModelsAsync(
            int makeId,
            int year,
            string? vehicleType,
            CancellationToken cancellationToken)
        {
            ModelCalls++;
            return Task.FromResult<IReadOnlyList<Model>>([new Model(1, "Accord", makeId, "HONDA")]);
        }
    }
}
