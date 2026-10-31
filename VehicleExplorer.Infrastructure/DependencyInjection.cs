using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VehicleExplorer.Application.Abstractions;
using VehicleExplorer.Infrastructure.Nhtsa;

namespace VehicleExplorer.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNhtsaClient(this IServiceCollection services, IConfiguration configuration)
    {
        var baseUrl = configuration["Nhtsa:BaseUrl"]
            ?? throw new InvalidOperationException("Nhtsa:BaseUrl is required.");

        services.AddHttpClient<INhtsaVehicleClient, NhtsaVehicleClient>(client =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }
}
