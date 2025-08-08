using Enerca.Logic.Modules.Compute.Db;
using Enerca.Logic.Modules.Compute.Mappers;
using Enerca.Logic.Modules.Tdd.Db.Models;
using Enerca.Logic.Modules.Tdd.Db.Modules;
using Enerca.Logic.Modules.Tdd.Mappers.Extensions;
using Enerca.Python.Helpers;
using Enerca.Python.Models;
using Fastdo.Common.Extensions;
using Fastdo.Common.Modules.Files.Models;

namespace Enerca.EnerkomInternalPrice.Plot.Services;

public class PlotM6PotentialService(EIPPlotSettings settings, EIPPlotInternalPriceState state)
{
    public async Task PlotAsync(ComputeModelDb db)
    {
        await PlotWithoutBgAsync(db: db.Clone());
        await PlotWithBgAsync(db: db.Clone());
    }

    private PathSettings GetPath(string fileName, string? dir = null) =>
        settings.GetPath(fileName: fileName, module: "M6Potential", dir: dir);

    private async Task PlotWithoutBgAsync(ComputeModelDb db)
    {
        var subModule = "M1WithoutBb";

        var annualConsumptionValues = new List<float>
        {
            0,
            // 100_000,
            500_000,
            1_000_000,
            2_000_000,
        };

        var consumptionCoefficientValues = new List<float>
        {
            0,
            // 0.5f,
            // 1,
            2,
        };

        foreach (var consumptionCoefficient in consumptionCoefficientValues)
            await PlotNewPvForConsumptionCoefficient(
                db: db,
                consumptionCoefficient: consumptionCoefficient,
                annualConsumptionValues: annualConsumptionValues,
                subModule: subModule
            );
    }

    private async Task PlotNewPvForConsumptionCoefficient(
        ComputeModelDb db,
        float consumptionCoefficient,
        List<float> annualConsumptionValues,
        string subModule
    )
    {
        var installedPowers = Enumerable.Range(0, 30).Select(x => MathF.Pow(x, 3) / 20).ToList();
        var energySharedValuesList = new List<List<float>>();
        var npvValuesList = new List<List<float>>();

        foreach (var annualConsumption in annualConsumptionValues)
        {
            var energySharedValues = new List<float>();
            var npvValues = new List<float>();

            var db_ = db.Clone();

            EIPPlotHelper.AddConsumption(
                db: db_,
                tdd: TddModuleElectricityCS2025.Tdd4,
                annualConsumption: annualConsumption
            );

            foreach (var installedPower in installedPowers)
            {
                var db__ = db_.Clone();

                EIPPlotHelper.AddPv(
                    db: db__,
                    installedPower: installedPower,
                    consumptionCoefficient: consumptionCoefficient
                );

                var model = await db__.ToModelAsync();
                var model0 = model.GetForYear(settings.ComputeYear);

                var result = model.Compute(settings.EvaluationYears);
                var result0 = model0.Compute();

                energySharedValues.Add(
                    result0.EnergyStateFinalSystem.Electricity.Consumption.Total
                        - result0.EnergyStateFinal.Electricity.Consumption.Total
                );
                npvValues.Add(result.PresentValue);
            }

            energySharedValuesList.Add(energySharedValues);
            npvValuesList.Add(npvValues);
        }

        var legends = new List<string?>();
        foreach (var annualConsumption in annualConsumptionValues)
            legends.Add($"Přidaná spotřeba: {settings.Formatting.FormatNumber(annualConsumption.ToInt())} kWh/rok");

        var ys = energySharedValuesList;
        var xs = Enumerable.Range(0, energySharedValuesList.Count).Select(x => installedPowers).ToList();

        await PlotHelper.PlotAsync(
            data: new PlotData(
                path: GetPath(
                    "NewPvEnergySharedConsumptionCoefficient_" + consumptionCoefficient.ToString("N2"),
                    dir: subModule
                ),
                ys: ys,
                xs: xs,
                title: "Závislost sdílené energie na přidaném instalovaném výkonu FVE",
                xLabel: "Přidaný instalovaný výkon FVE [kWp]",
                yLabel: "Sdílená energie [kWh]",
                legends: legends,
                scientificNotationAxisY: false,
                yTicksFormatter: x => settings.Formatting.FormatNumber(x.ToInt())
            ),
            plts: [PythonPlotHelper.Plot]
        );

        List<string?>? colors = [.. Enumerable.Repeat<string?>(null, ys.Count)];

        ys.Add([state.MinEnergySharedWithoutBP, state.MinEnergySharedWithoutBP]);
        xs.Add([0, installedPowers.Last()]);
        legends.Add("Minimální potřeba sdílené energie");
        colors.Add("#3C0061");

        await PlotHelper.PlotAsync(
            data: new PlotData(
                path: GetPath(
                    "NewPvEnergySharedConsumptionCoefficient_" + consumptionCoefficient.ToString("N2") + "_Targets",
                    dir: subModule
                ),
                ys: ys,
                xs: xs,
                title: "Závislost sdílené energie na přidaném instalovaném výkonu FVE",
                xLabel: "Přidaný instalovaný výkon FVE [kWp]",
                yLabel: "Sdílená energie [kWh]",
                legends: legends,
                colors: colors,
                scientificNotationAxisY: false,
                yTicksFormatter: x => settings.Formatting.FormatNumber(x.ToInt())
            ),
            plts: [PythonPlotHelper.Plot]
        );

        await PlotHelper.PlotAsync(
            data: new PlotData(
                path: GetPath(
                    "NewPvNPVConsumptionCoefficient_" + consumptionCoefficient.ToString("N2"),
                    dir: subModule
                ),
                ys: npvValuesList,
                xs: [installedPowers],
                title: "Závislost NPV na přidaném instalovaném výkonu FVE",
                xLabel: "Přidaný instalovaný výkon FVE [kWp]",
                yLabel: "NPV [Kč]",
                legends: legends,
                scientificNotationAxisY: false,
                yTicksFormatter: x => settings.Formatting.FormatCurrency(x.ToInt())
            ),
            plts: [PythonPlotHelper.Plot]
        );
    }

    private async Task PlotWithBgAsync(ComputeModelDb db)
    {
        var subModule = "M2WithBg";

        var tdds = new List<TddModuleWithItem>
        {
            // TddsElectricityCS2025.Tdd1,
            // TddsElectricityCS2025.Tdd2,
            // TddsElectricityCS2025.Tdd3,
            TddModuleElectricityCS2025.Tdd4,
            // TddsElectricityCS2025.Tdd5Jic,
            // TddsElectricityCS2025.Tdd6,
            // TddsElectricityCS2025.Tdd7,
            TddModuleElectricityCS2025.Tdd8,
        };

        await PlotNewConsumption_ForTDD(db: db, tdds: tdds, subModule: subModule);
    }

    private async Task PlotNewConsumption_ForTDD(ComputeModelDb db, List<TddModuleWithItem> tdds, string subModule)
    {
        var annualConsumptionValues = Enumerable.Range(0, 30).Select(x => 500 * MathF.Pow(x, 3)).ToList();
        var energySharedValuesList = new List<List<float>>();
        var npvValuesList = new List<List<float>>();

        foreach (var tdd in tdds)
        {
            var energySharedValues = new List<float>();
            var npvValues = new List<float>();

            foreach (var annualConsumption in annualConsumptionValues)
            {
                var consumptionValues = await tdd.GetValues(valueAnnual: annualConsumption);

                var db_ = db.Clone();

                EIPPlotHelper.AddConsumption(db: db_, tdd: tdd, annualConsumption: annualConsumption);

                var model = await db_.ToModelAsync();
                var model0 = model.GetForYear(settings.ComputeYear);

                var result = model.Compute(settings.EvaluationYears);
                var result0 = model0.Compute();

                energySharedValues.Add(
                    result0.EnergyStateFinalSystem.Electricity.Consumption.Total
                        - result0.EnergyStateFinal.Electricity.Consumption.Total
                );
                npvValues.Add(result.PresentValue);
            }

            energySharedValuesList.Add(energySharedValues);
            npvValuesList.Add(npvValues);
        }

        var legends = new List<string?>();
        foreach (var tdd in tdds)
            legends.Add(tdd.Item.Id);

        annualConsumptionValues = [.. annualConsumptionValues.Select(x => x / 1_000)];

        var ys = energySharedValuesList;
        var xs = Enumerable.Range(0, energySharedValuesList.Count).Select(x => annualConsumptionValues).ToList();

        var detailTake = 12;
        var ysDetail = energySharedValuesList.Select(x => x.Take(detailTake).ToList()).ToList();
        var xsDetail = Enumerable
            .Range(0, energySharedValuesList.Count)
            .Select(x => annualConsumptionValues.Take(detailTake).ToList())
            .ToList();

        await PlotHelper.PlotAsync(
            data: new PlotData(
                path: GetPath("NewConsumptionEnergyShared", dir: subModule),
                ys: ys,
                xs: xs,
                title: "Závislost sdílené energie na přidané spotřebě",
                xLabel: "Přidaná spotřeba [MWh/rok]",
                yLabel: "Sdílená energie [kWh]",
                legends: legends,
                scientificNotationAxisY: false,
                yTicksFormatter: x => settings.Formatting.FormatNumber(x.ToInt())
            ),
            plts: [PythonPlotHelper.Plot]
        );

        await PlotHelper.PlotAsync(
            data: new PlotData(
                path: GetPath("NewConsumptionEnergySharedDetail", dir: subModule),
                ys: ysDetail,
                xs: xsDetail,
                title: "Závislost sdílené energie na přidané spotřebě",
                xLabel: "Přidaná spotřeba [MWh/rok]",
                yLabel: "Sdílená energie [kWh]",
                legends: legends,
                scientificNotationAxisY: false,
                yTicksFormatter: x => settings.Formatting.FormatNumber(x.ToInt())
            ),
            plts: [PythonPlotHelper.Plot]
        );

        List<string?>? colors = [.. Enumerable.Repeat<string?>(null, ys.Count)];

        ys.Add([state.MinEnergyShared, state.MinEnergyShared]);
        xs.Add([0, annualConsumptionValues.Last()]);
        ysDetail.Add([state.MinEnergyShared, state.MinEnergyShared]);
        xsDetail.Add([0, annualConsumptionValues.Take(detailTake).Last()]);
        legends.Add("Minimální potřeba sdílené energie");
        colors.Add("#9D00FF");

        await PlotHelper.PlotAsync(
            data: new PlotData(
                path: GetPath("NewConsumptionEnergySharedTargets", dir: subModule),
                ys: ys,
                xs: xs,
                title: "Závislost sdílené energie na přidané spotřebě",
                xLabel: "Přidaná spotřeba [MWh/rok]",
                yLabel: "Sdílená energie [kWh]",
                legends: legends,
                colors: colors,
                scientificNotationAxisY: false,
                yTicksFormatter: x => settings.Formatting.FormatNumber(x.ToInt())
            ),
            plts: [PythonPlotHelper.Plot]
        );

        await PlotHelper.PlotAsync(
            data: new PlotData(
                path: GetPath("NewConsumptionEnergySharedDetailTargets", dir: subModule),
                ys: ysDetail,
                xs: xsDetail,
                title: "Závislost sdílené energie na přidané spotřebě",
                xLabel: "Přidaná spotřeba [MWh/rok]",
                yLabel: "Sdílená energie [kWh]",
                legends: legends,
                colors: colors,
                scientificNotationAxisY: false,
                yTicksFormatter: x => settings.Formatting.FormatNumber(x.ToInt())
            ),
            plts: [PythonPlotHelper.Plot]
        );

        await PlotHelper.PlotAsync(
            data: new PlotData(
                path: GetPath("NewConsumptionNPV", dir: subModule),
                ys: npvValuesList,
                xs: [annualConsumptionValues],
                title: "Závislost NPV na přidané spotřebě",
                xLabel: "Přidaná spotřeba [MWh/rok]",
                yLabel: "NPV [Kč]",
                legends: legends,
                scientificNotationAxisY: false,
                yTicksFormatter: x => settings.Formatting.FormatCurrency(x.ToInt())
            ),
            plts: [PythonPlotHelper.Plot]
        );
    }
}
