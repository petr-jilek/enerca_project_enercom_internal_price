namespace Enerca.EnerkomInternalPrice.Console.Models;

public class EIPCPEntityDataWithValues
{
    public required EIPCPEntityData Data { get; init; }

    public List<float> ConsumptionValues { get; set; } = [];
    public List<float> ProductionValues { get; set; } = [];
}
