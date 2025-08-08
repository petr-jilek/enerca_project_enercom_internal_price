namespace Enerca.EnerkomInternalPrice.Logic.Models;

public class EIPPlotInternalPriceState
{
    public float MinInternalPriceFee { get; set; }
    public float MinInternalPriceFeeWithoutBP { get; set; }

    public float MinEnergyShared { get; set; }
    public float MinEnergySharedWithoutBP { get; set; }

    public float MinAdditionalEnergyShared { get; set; }
    public float MinAdditionalEnergySharedWithoutBP { get; set; }
}
