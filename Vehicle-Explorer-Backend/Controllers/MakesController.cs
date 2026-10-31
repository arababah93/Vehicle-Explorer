using Microsoft.AspNetCore.Mvc;
using VehicleExplorer.Application.Abstractions;
using VehicleExplorer.Domain;

namespace VehicleExplorer.Api.Controllers;

[ApiController]
[Route("api/makes")]
public sealed class MakesController(IVehicleCatalog catalog) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<Make>> GetMakes(CancellationToken cancellationToken)
    {
        return await catalog.GetMakesAsync(cancellationToken);
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
