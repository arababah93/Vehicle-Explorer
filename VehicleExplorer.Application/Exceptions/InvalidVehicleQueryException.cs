namespace VehicleExplorer.Application.Exceptions;

public sealed class InvalidVehicleQueryException : Exception
{
    public InvalidVehicleQueryException(string message)
        : base(message)
    {
    }
}
