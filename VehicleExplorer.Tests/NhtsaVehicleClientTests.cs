using VehicleExplorer.Infrastructure.Nhtsa;

namespace VehicleExplorer.Tests;

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
