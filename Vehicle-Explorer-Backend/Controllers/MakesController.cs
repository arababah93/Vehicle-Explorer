using Microsoft.AspNetCore.Mvc;
using VehicleExplorer.Application.Abstractions;
using VehicleExplorer.Application.Catalog;
using VehicleExplorer.Domain;

namespace VehicleExplorer.Api.Controllers;

[ApiController]
[Route("api/makes")]
public sealed class MakesController(IVehicleCatalog catalog) : ControllerBase
{
    [HttpGet]
    public async Task<PagedResult<Make>> GetMakes(
        [FromQuery] string? prefix,
        [FromQuery] int skip = 0,
        [FromQuery] int take = VehicleCatalog.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        return await catalog.GetMakesAsync(prefix, skip, take, cancellationToken);
    }

    [HttpGet("{makeId:int}/vehicle-types")]
    public async Task<IReadOnlyList<VehicleType>> GetVehicleTypes(int makeId, CancellationToken cancellationToken)
    {
        return await catalog.GetVehicleTypesAsync(makeId, cancellationToken);
    }

    [HttpGet("{makeId:int}/models")]
    public async Task<IReadOnlyList<Model>> GetModels(
        int makeId,
        [FromQuery] int year,
        [FromQuery] string? vehicleType,
        CancellationToken cancellationToken)
    {
        return await catalog.GetModelsAsync(makeId, year, vehicleType, cancellationToken);
    }
}
