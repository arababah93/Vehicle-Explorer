namespace VehicleExplorer.Application.Abstractions;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total);
