using System.Text.Json;
using Enerca.EnerkomInternalPrice.Logic.Models;
using Enerca.Logic.Modules.Compute.Abstractions;
using Enerca.Logic.Modules.CPEntity.Abstractions;
using Enerca.Logic.Modules.EnergyTariff.Implementations.Implementations.Electricity.Fixed;
using Enerca.Python.Helpers;
using Enerca.Python.Models;
using Fastdo.Common.Extensions;
using Fastdo.Common.Modules.Files.Models;

namespace Enerca.EnerkomInternalPrice.Logic.Plot;

public class PlotM2CurrentStateEconomyService(EIPPlotSettings settings)
{
    public async Task PlotAsync(IComputeModel model)
    {
        await PlotByPropertyPredicatesAsync(model: model);
    }

    private PathSettings GetPath(string fileName, string? dir = null) =>
        settings.GetPath(fileName: fileName, module: "M2CurrentStateEconomy", dir: dir);

    private async Task PlotByPropertyPredicatesAsync(IComputeModel model)
    {
        var result = model.Compute();

        var cpEntities = model.CPEntities.ToList();
        var cpResults = result.CPEntityResults.ToList();

        var cpEntityResultList = new List<(ICPEntity, ICPEntityResult)>();

        for (var i = 0; i < cpEntities.Count; i++)
            cpEntityResultList.Add((cpEntities[i], cpResults[i]));

        async Task Plot(
            string name,
            string title,
            string yLabel,
            Func<(ICPEntity, ICPEntityResult), float> propertyPredicate,
            Func<float, string>? yTicksFormatter = null,
            bool yLogScale = true,
            float? yMin = null,
            float? yMax = null
        )
        {
            var cpEntityResultListFiltered = cpEntityResultList.Where(x => propertyPredicate(x) > 0).ToList();
            var cpEntityResultListFilteredSorted = cpEntityResultListFiltered
                .OrderByDescending(x => propertyPredicate(x))
                .ToList();

            await PlotHelper.PlotAsync(
                data: new PlotData(
                    path: GetPath(fileName: name),
                    ys:
                    [
                        [.. cpEntityResultListFiltered.Select(propertyPredicate)],
                    ],
                    title: title,
                    yLabel: yLabel,
                    scientificNotationAxisY: false,
                    yTicksFormatter: yTicksFormatter,
                    xTicksLabels: [.. cpEntityResultListFiltered.Select(x => x.Item1.InfoBasic.Label)],
                    xTicksRotation: 0,
                    yLogScale: yLogScale,
                    yMin: yMin,
                    yMax: yMax
                ),
                plts: [PythonPlotHelper.PlotBar]
            );

            await PlotHelper.PlotAsync(
                data: new PlotData(
                    path: GetPath(fileName: $"{name}Sorted"),
                    ys:
                    [
                        [.. cpEntityResultListFilteredSorted.Select(propertyPredicate)],
                    ],
                    title: title,
                    yLabel: yLabel,
                    scientificNotationAxisY: false,
                    yTicksFormatter: yTicksFormatter,
                    xTicksLabels: [.. cpEntityResultListFilteredSorted.Select(x => x.Item1.InfoBasic.Label)],
                    xTicksRotation: 0,
                    yLogScale: yLogScale,
                    yMin: yMin,
                    yMax: yMax
                ),
                plts: [PythonPlotHelper.PlotBar]
            );
        }

        static float sellPropertyPredicate((ICPEntity, ICPEntityResult) x)
        {
            var production = x.Item2.ResultPre.EnergyStateInitial.Electricity.Production.Total;
            var sellPrice = x.Item1.EnergyTariffs.Electricity.As<EnergyTariffElectricityFixed>().VariableSell;

            return production * sellPrice;
        }

        static float sellWithGreenBonusPropertyPredicate((ICPEntity, ICPEntityResult) x)
        {
            var production = x.Item2.ResultPre.EnergyStateInitial.Electricity.Production.Total;
            var sellPrice = x.Item1.EnergyTariffs.Electricity.As<EnergyTariffElectricityFixed>().VariableSell;

            var greenBonus = greenBonusPropertyPredicate(x);

            return production * (sellPrice + greenBonus);
        }

        static float sellPricePropertyPredicate((ICPEntity, ICPEntityResult) x) =>
            x.Item1.EnergyTariffs.Electricity.As<EnergyTariffElectricityFixed>().VariableSell;

        static float greenBonusPropertyPredicate((ICPEntity, ICPEntityResult) x) =>
            JsonSerializer
                .Deserialize<EIPCPEntityData>(
                    x.Item1.InfoBasic.Note
                        ?? throw new Exception($"Note is empty for cpEntity with label: {x.Item1.InfoBasic.Label}")
                )
                ?.GreenBonus_
            ?? throw new Exception($"Note is not deserializable for cpEntity with label: {x.Item1.InfoBasic.Label}");

        static float sellPriceWithGreenBonusPropertyPredicate((ICPEntity, ICPEntityResult) x)
        {
            var sellPrice = x.Item1.EnergyTariffs.Electricity.As<EnergyTariffElectricityFixed>().VariableSell;
            var greenBonus = greenBonusPropertyPredicate(x);

            return sellPrice + greenBonus;
        }

        static float greenBonusYearToPropertyPredicate((ICPEntity, ICPEntityResult) x) =>
            JsonSerializer
                .Deserialize<EIPCPEntityData>(
                    x.Item1.InfoBasic.Note
                        ?? throw new Exception($"Note is empty for cpEntity with label: {x.Item1.InfoBasic.Label}")
                )
                ?.GreenBonusYearTo ?? -1;

        await Plot(
            name: "CashFlowSell",
            title: "Hotovostní tok z prodeje",
            yLabel: "Hotovostní tok [Kč]",
            propertyPredicate: sellPropertyPredicate,
            yTicksFormatter: (x) => settings.Formatting.FormatCurrency(x.ToInt())
        );

        await Plot(
            name: "CashFlowSellWithGreenBonus",
            title: "Hotovostní tok z prodeje se zeleným bonusem",
            yLabel: "Hotovostní tok [Kč]",
            propertyPredicate: sellWithGreenBonusPropertyPredicate,
            yTicksFormatter: (x) => settings.Formatting.FormatCurrency(x.ToInt())
        );

        await Plot(
            name: "SellPrice",
            title: "Aktuální jednotkové ocenění elektrické energie",
            yLabel: "Cena [Kč/kWh]",
            propertyPredicate: sellPricePropertyPredicate,
            yLogScale: false
        );

        await Plot(
            name: "GreenBonus",
            title: "Aktuální výše zeleného bonusu",
            yLabel: "Zelený bonus [Kč/kWh]",
            propertyPredicate: greenBonusPropertyPredicate,
            yLogScale: false
        );

        await Plot(
            name: "SellPriceWithGreenBonus",
            title: "Aktuální jednotkové ocenění elektrické energie se ZB",
            yLabel: "Cena [Kč/kWh]",
            propertyPredicate: sellPriceWithGreenBonusPropertyPredicate,
            yLogScale: false
        );

        var greenBonusYearToList = cpEntityResultList
            .Select(greenBonusYearToPropertyPredicate)
            .Where(x => x > 0)
            .ToList();

        await Plot(
            name: "GreenBonusYearTo",
            title: "Rok ukončení zeleného bonusu",
            yLabel: "Rok",
            propertyPredicate: greenBonusYearToPropertyPredicate,
            yLogScale: false,
            yMin: greenBonusYearToList.Min() - 1,
            yMax: greenBonusYearToList.Max() + 1,
            yTicksFormatter: (x) => x.ToInt().ToString()
        );
    }
}
