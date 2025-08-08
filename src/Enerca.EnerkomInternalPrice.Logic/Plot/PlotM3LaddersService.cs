using System.Text.Json;
using Enerca.EnerkomInternalPrice.Logic.Models;
using Enerca.Logic.Modules.Compute.Abstractions;
using Enerca.Logic.Modules.EnergyTariff.Implementations.Implementations.Electricity.Fixed;
using Enerca.Python.Helpers;
using Enerca.Python.Models;
using Fastdo.Common.Extensions;
using Fastdo.Common.Modules.Files.Models;

namespace Enerca.EnerkomInternalPrice.Logic.Plot;

public class PlotM3LaddersService(EIPPlotSettings settings)
{
    public async Task PlotAsync(IComputeModel model)
    {
        await PlotLadderChartsAsync(model: model);
    }

    private PathSettings GetPath(string fileName, string? dir = null) =>
        settings.GetPath(fileName: fileName, module: "M3Ladders", dir: dir);

    private async Task PlotLadderChartsAsync(IComputeModel model)
    {
        var producers = model.CPEntities.Where(x => x.EnergyTariffs.Electricity?.Info.EanP is not null).ToList();

        var priceValues = Enumerable.Range(0, 300).Select(x => (float)x / 100).ToList();
        var installedPowerValues = new List<float>();
        var annualProductionValues = new List<float>();

        var totalAnnualConsumption = model.CPEntities.Select(x => x.EnergyState.Electricity.Consumption.Total).Sum();

        foreach (var price in priceValues)
        {
            var producersIn = producers
                .Where(x => price > x.EnergyTariffs.Electricity.As<EnergyTariffElectricityFixed>().VariableSell)
                .ToList();

            var installedPower = producersIn
                .Select(x =>
                    JsonSerializer
                        .Deserialize<EIPCPEntityData>(
                            x.InfoBasic.Note
                                ?? throw new Exception($"Note is empty for cpEntity with label: {x.InfoBasic.Label}")
                        )
                        ?.ElectricityInstalledPower ?? 0
                )
                .Sum();

            var annualProduction = producersIn.Select(x => x.EnergyState.Electricity.Production.Total).Sum();

            installedPowerValues.Add(installedPower * 1000); // Convert MW to kW
            annualProductionValues.Add(annualProduction);
        }

        await PlotHelper.PlotAsync(
            data: new PlotData(
                path: GetPath("InstalledPower"),
                ys: [installedPowerValues],
                xs: [priceValues],
                title: "Cenová hladina pro zahrnutí zdroje do sdílení",
                xLabel: "Cena [Kč/kWh]",
                yLabel: "Instalovaný výkon [kW]",
                scientificNotationAxisY: false,
                xTicksFormatter: x => x.ToString().Replace(".", ","),
                yLogScale: true
            ),
            plts: [PythonPlotHelper.Plot]
        );

        await PlotHelper.PlotAsync(
            data: new PlotData(
                path: GetPath("AnnualProduction"),
                ys:
                [
                    annualProductionValues,
                    [.. Enumerable.Repeat(totalAnnualConsumption, annualProductionValues.Count)],
                ],
                xs: [priceValues],
                title: "Cenová hladina pro zahrnutí zdroje do sdílení",
                xLabel: "Cena [Kč/kWh]",
                yLabel: "Roční výroba [kWh]",
                legends: ["Roční výroba", "Roční spotřeba"],
                scientificNotationAxisY: false,
                xTicksFormatter: x => x.ToString().Replace(".", ","),
                yTicksFormatter: x => settings.Formatting.FormatNumber(x.ToInt()),
                yLogScale: true
            ),
            plts: [PythonPlotHelper.Plot]
        );
    }
}
