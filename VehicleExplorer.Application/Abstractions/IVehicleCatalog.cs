using VehicleExplorer.Domain;

namespace VehicleExplorer.Application.Abstractions;

public interface IVehicleCatalog
{
    Task<PagedResult<Make>> GetMakesAsync(string? prefix, int skip, int take, CancellationToken cancellationToken);

    Task<IReadOnlyList<VehicleType>> GetVehicleTypesAsync(int makeId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Model>> GetModelsAsync(int makeId, int year, string? vehicleType, CancellationToken cancellationToken);
}
