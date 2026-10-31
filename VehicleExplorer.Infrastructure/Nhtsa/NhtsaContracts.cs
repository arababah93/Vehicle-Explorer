using System.Text.Json.Serialization;

namespace VehicleExplorer.Infrastructure.Nhtsa;

internal sealed class NhtsaResponse<T>
{
    public List<T>? Results { get; set; }
}

internal sealed class NhtsaMakeResult
{
    [JsonPropertyName("Make_ID")]
    public int MakeId { get; set; }

    [JsonPropertyName("Make_Name")]
    public string? MakeName { get; set; }
}

internal sealed class NhtsaVehicleTypeResult
{
    [JsonPropertyName("VehicleTypeId")]
    public int VehicleTypeId { get; set; }

    [JsonPropertyName("VehicleTypeName")]
    public string? VehicleTypeName { get; set; }
}

internal sealed class NhtsaModelResult
{
    [JsonPropertyName("Make_ID")]
    public int MakeId { get; set; }

    [JsonPropertyName("Make_Name")]
    public string? MakeName { get; set; }

    [JsonPropertyName("Model_ID")]
    public int ModelId { get; set; }

    [JsonPropertyName("Model_Name")]
    public string? ModelName { get; set; }
}
