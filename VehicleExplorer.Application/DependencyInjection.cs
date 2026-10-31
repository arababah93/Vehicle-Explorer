using Microsoft.Extensions.DependencyInjection;
using VehicleExplorer.Application.Abstractions;
using VehicleExplorer.Application.Catalog;

namespace VehicleExplorer.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddVehicleCatalog(this IServiceCollection services)
    {
        services.AddScoped<IVehicleCatalog, VehicleCatalog>();
        return services;
    }
}
