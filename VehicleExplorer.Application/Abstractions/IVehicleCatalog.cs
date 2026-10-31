using VehicleExplorer.Domain;

namespace VehicleExplorer.Application.Abstractions;

public interface IVehicleCatalog
{
    Task<IReadOnlyList<Make>> GetMakesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<VehicleType>> GetVehicleTypesAsync(int makeId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Model>> GetModelsAsync(int makeId, int year, string? vehicleType, CancellationToken cancellationToken);
}
