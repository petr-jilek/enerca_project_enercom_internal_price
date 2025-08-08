using Fastdo.Common.Extensions;

namespace Enerca.EnerkomInternalPrice.Logic.Models;

public class EIPCPEntityData
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public string Description_ => Description.ToFriendlyUrl();
    public required string Address { get; init; }
    public required string Provider { get; init; }
    public required string Tariff { get; init; }
    public required int PhaseCount { get; init; }
    public required int Current { get; init; }
    public required int Voltage { get; init; }
    public required string? EanC { get; init; }

    public required float AnnualConsumptionVT { get; init; }
    public required float AnnualConsumptionNT { get; init; }
    public required bool Has15MinutesConsumptionData { get; init; } = false;

    public required float VariableEnergyPriceVT { get; init; }
    public required float VariableEnergyPriceNT { get; init; }
    public float VariableEnergy => VariableEnergyPriceVT;
    public required float FixedPrice { get; init; }
    public required float VariableDistributionPriceVT { get; init; }
    public required float VariableDistributionPriceNT { get; init; }
    public required float SystemServicePrice { get; init; }
    public float VariableDistribution =>
        (VariableDistributionPriceNT + VariableDistributionPriceVT) / 2 + SystemServicePrice;
    public required float OzePrice { get; init; }
    public required float InfrastructurePrice { get; init; }
    public required float CircuitBreakerPrice { get; init; }
    public float Fixed => FixedPrice + OzePrice * Current + InfrastructurePrice + CircuitBreakerPrice * Current;

    public required string? ProductionSystemId { get; init; }
    public required string? EanP { get; init; }
    public required string? ProductionType { get; set; }
    public string ProductionType_ => ProductionType?.ToNormalized() ?? string.Empty;
    public required float? ElectricityInstalledPower { get; set; }
    public required float? SellPrice { get; set; }
    public float VariableSell => SellPrice ?? 0;
    public required float? GreenBonus { get; set; }
    public float GreenBonus_ => GreenBonus ?? 0;
    public required int? GreenBonusYearTo { get; set; }
}
