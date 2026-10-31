using System.Net;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using VehicleExplorer.Application.Abstractions;
using VehicleExplorer.Application.Catalog;
using VehicleExplorer.Application.Exceptions;
using VehicleExplorer.Domain;
using VehicleExplorer.Infrastructure.Nhtsa;

namespace VehicleExplorer.Tests;

public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    public HttpRequestMessage? LastRequest { get; private set; }

    public Func<HttpRequestMessage, HttpResponseMessage> Responder { get; set; } =
        _ => new HttpResponseMessage(HttpStatusCode.OK);

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(Responder(request));
    }

    public static HttpResponseMessage Json(string body)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
    }
}

public sealed class NhtsaVehicleClientTests
{
    [Fact]
    public async Task GetMakes_maps_id_and_name()
    {
        var handler = new StubHttpMessageHandler
        {
            Responder = _ => StubHttpMessageHandler.Json("""
                { "Results": [ { "Make_ID": 474, "Make_Name": "HONDA" } ] }
                """)
        };

        var makes = await CreateClient(handler).GetMakesAsync(CancellationToken.None);

        var make = Assert.Single(makes);
        Assert.Equal(474, make.Id);
        Assert.Equal("HONDA", make.Name);
    }

    [Fact]
    public async Task GetVehicleTypes_trims_trailing_space()
    {
        var handler = new StubHttpMessageHandler
        {
            Responder = _ => StubHttpMessageHandler.Json("""
                { "Results": [ { "VehicleTypeId": 3, "VehicleTypeName": "Truck " } ] }
                """)
        };

        var types = await CreateClient(handler).GetVehicleTypesAsync(474, CancellationToken.None);

        var type = Assert.Single(types);
        Assert.Equal(3, type.Id);
        Assert.Equal("Truck", type.Name);
    }

    [Fact]
    public async Task GetModels_maps_model_and_make()
    {
        var handler = new StubHttpMessageHandler
        {
            Responder = _ => StubHttpMessageHandler.Json("""
                {
                  "Results": [
                    {
                      "Make_ID": 474,
                      "Make_Name": "HONDA",
                      "Model_ID": 1861,
                      "Model_Name": "Accord"
                    }
                  ]
                }
                """)
        };

        var models = await CreateClient(handler).GetModelsAsync(474, 2015, null, CancellationToken.None);

        var model = Assert.Single(models);
        Assert.Equal(1861, model.Id);
        Assert.Equal("Accord", model.Name);
        Assert.Equal(474, model.MakeId);
        Assert.Equal("HONDA", model.MakeName);
    }

    [Fact]
    public async Task GetModels_omits_vehicle_type_when_absent()
    {
        var handler = new StubHttpMessageHandler
        {
            Responder = _ => StubHttpMessageHandler.Json("""{ "Results": [] }""")
        };

        await CreateClient(handler).GetModelsAsync(474, 2015, null, CancellationToken.None);

        var uri = handler.LastRequest!.RequestUri!.AbsoluteUri;
        Assert.Contains("/modelyear/2015", uri);
        Assert.DoesNotContain("vehicletype", uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetModels_includes_vehicle_type_when_requested()
    {
        var handler = new StubHttpMessageHandler
        {
            Responder = _ => StubHttpMessageHandler.Json("""{ "Results": [] }""")
        };

        await CreateClient(handler).GetModelsAsync(474, 2015, "Passenger Car", CancellationToken.None);

        var uri = handler.LastRequest!.RequestUri!.AbsoluteUri;
        Assert.Contains("vehicletype/Passenger%20Car", uri);
    }

    private static NhtsaVehicleClient CreateClient(StubHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://vpic.nhtsa.dot.gov/api/vehicles/")
        };

        return new NhtsaVehicleClient(httpClient);
    }
}

public sealed class VehicleCatalogTests
{
    [Fact]
    public async Task GetMakes_filters_by_prefix_and_returns_one_page()
    {
        var client = new FakeNhtsaClient
        {
            Makes =
            [
                new Make(1, "HONDA"),
                new Make(2, "HONGQI"),
                new Make(3, "TOYOTA")
            ]
        };
        var catalog = CreateCatalog(client);

        var page = await catalog.GetMakesAsync("ho", 0, 1, CancellationToken.None);

        Assert.Equal(2, page.Total);
        var item = Assert.Single(page.Items);
        Assert.Equal("HONDA", item.Name);
    }

    [Fact]
    public async Task GetMakes_without_prefix_skips_to_the_requested_slice()
    {
        var client = new FakeNhtsaClient
        {
            Makes =
            [
                new Make(1, "HONDA"),
                new Make(2, "HONGQI"),
                new Make(3, "TOYOTA")
            ]
        };
        var catalog = CreateCatalog(client);

        var page = await catalog.GetMakesAsync("  ", 1, 1, CancellationToken.None);

        Assert.Equal(3, page.Total);
        Assert.Equal("HONGQI", Assert.Single(page.Items).Name);
    }

    [Fact]
    public async Task GetMakes_rejects_negative_skip()
    {
        var catalog = CreateCatalog(new FakeNhtsaClient());

        await Assert.ThrowsAsync<InvalidVehicleQueryException>(
            () => catalog.GetMakesAsync(null, -1, 10, CancellationToken.None));
    }

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

        public IReadOnlyList<Make> Makes { get; init; } = [];

        public Task<IReadOnlyList<Make>> GetMakesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Makes);
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
