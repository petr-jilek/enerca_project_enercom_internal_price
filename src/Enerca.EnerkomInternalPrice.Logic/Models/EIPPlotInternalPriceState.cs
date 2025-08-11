namespace Enerca.EnerkomInternalPrice.Logic.Models;

public class EIPPlotInternalPriceState
{
    public float MinInternalPriceFee { get; set; }
    public float MinInternalPriceFeeWithoutBg { get; set; }

    public float MinEnergyShared { get; set; }
    public float MinEnergySharedWithoutBg { get; set; }

    public float MinAdditionalEnergyShared { get; set; }
    public float MinAdditionalEnergySharedWithoutBg { get; set; }
}
