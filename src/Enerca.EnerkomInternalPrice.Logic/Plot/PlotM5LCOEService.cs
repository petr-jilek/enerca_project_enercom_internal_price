using Enerca.EnerkomInternalPrice.Logic.Models;
using Enerca.Logic.Common.Colors;
using Enerca.Logic.Modules.Compute.Abstractions;
using Enerca.Logic.Modules.Compute.Db;
using Enerca.Logic.Modules.Compute.Mappers;
using Enerca.Logic.Modules.Economy.Abstractions;
using Enerca.Logic.Modules.Economy.Mappers.Predefined;
using Enerca.Python.Helpers;
using Enerca.Python.Models;
using Fastdo.Common.Extensions;
using Fastdo.Common.Modules.Files.Models;
using Fastdo.Common.Modules.Maths.Consts;

namespace Enerca.EnerkomInternalPrice.Logic.Plot;

public class PlotM5LCOEService(EIPPlotSettings settings, EIPPlotInternalPriceState state)
{
    public async Task PlotAsync(ComputeModelDb db)
    {
        var db_ = db.Clone();
        var dbWithoutBg_ = db.Clone();

        EIPPlotServiceHelper.RemoveBgs(db: dbWithoutBg_);

        var model = await db_.ToModelAsync();
        var modelWithoutBg = await dbWithoutBg_.ToModelAsync();

        var model0 = model.GetForYear(settings.ComputeYear);
        var modelWithoutBg0 = modelWithoutBg.GetForYear(settings.ComputeYear);

        await PlotLCOEAsync(model: model0, modelWithoutBg: modelWithoutBg0);
        await PlotLCOEConsumerAsync();
    }

    private PathSettings GetPath(string fileName, string? dir = null) =>
        settings.GetPath(fileName: fileName, module: "M5LCOE", dir: dir);

    private async Task PlotLCOEAsync(IComputeModel model, IComputeModel modelWithoutBg)
    {
        var subModule = "M1LCOE";

        var result = model.Compute();
        var resultWithoutBg = modelWithoutBg.Compute();

        var consumption = result.EnergyStateInitial.Electricity.Consumption.Total;
        var production = result.EnergyStateInitial.Electricity.Production.Total;
        var withoutBgProduction = resultWithoutBg.EnergyStateInitial.Electricity.Production.Total;

        var energyValues = Enumerable.Range(0, 100).Select(x => 10_000 + 10 * MathF.Pow(x, 3)).ToList();
        var lcoeValues = energyValues
            .Select(energy => (settings.VariableCosts * energy + settings.FixedCosts) / energy)
            .ToList();

        for (var i = 0; i < lcoeValues.Count - 1; i++)
        {
            var energy0 = energyValues[i];
            var lcoe0 = lcoeValues[i];

            var energy1 = energyValues[i + 1];
            var lcoe1 = lcoeValues[i + 1];

            float getEnergy(float lcoe)
            {
                if (lcoe0 == lcoe1)
                    return (energy0 + energy1) / 2;

                return energy0 + (energy1 - energy0) * (lcoe - lcoe0) / (lcoe1 - lcoe0);
            }

            if (state.MinInternalPriceFee < lcoe0 && state.MinInternalPriceFee > lcoe1)
                state.MinEnergyShared = getEnergy(lcoe: state.MinInternalPriceFee);
            if (state.MinInternalPriceFeeWithoutBg < lcoe0 && state.MinInternalPriceFeeWithoutBg > lcoe1)
                state.MinEnergySharedWithoutBg = getEnergy(lcoe: state.MinInternalPriceFeeWithoutBg);
        }

        state.MinAdditionalEnergyShared = state.MinEnergyShared - consumption;
        state.MinAdditionalEnergySharedWithoutBg = state.MinEnergySharedWithoutBg - withoutBgProduction;

        var energyLCOEValues = new EnergyLCOEValues
        {
            EnergyValues = energyValues,
            LCOEValues = lcoeValues,
            LCOEValuesList = [],
            Legends = [],
            WithoutBPProduction = withoutBgProduction,
            Consumption = consumption,
            Production = production,
        };

        var all2LimitMin = withoutBgProduction * 0.8f;
        var energyLowLimitMax = consumption * 1.2f;
        var energyLow2LimitMin = withoutBgProduction * 0.5f;
        var energyHighLimitMin = MathF.Min(MathF.Min(consumption, production), state.MinEnergyShared) * 0.8f;

        await PlotEnergyLCOEValues(
            fileName: "AllUnlabeled",
            subModule: subModule,
            values: energyLCOEValues,
            xLogScale: true,
            label: false
        );
        await PlotEnergyLCOEValues(fileName: "All", subModule: subModule, values: energyLCOEValues, xLogScale: true);
        await PlotEnergyLCOEValues(
            fileName: "All2",
            subModule: subModule,
            values: energyLCOEValues.WithRange(energyMin: all2LimitMin),
            xLogScale: true
        );
        await PlotEnergyLCOEValues(
            fileName: "EnergyLow",
            subModule: subModule,
            values: energyLCOEValues.WithRange(energyMax: energyLowLimitMax),
            xLogScale: true
        );
        await PlotEnergyLCOEValues(
            fileName: "EnergyLow2",
            subModule: subModule,
            values: energyLCOEValues.WithRange(energyMin: energyLow2LimitMin, energyMax: energyLowLimitMax),
            xLogScale: true
        );

        await PlotEnergyLCOEValues(
            fileName: "EnergyHigh",
            subModule: subModule,
            values: energyLCOEValues.WithRange(energyMin: energyHighLimitMin),
            xLogScale: true
        );

        subModule = "M2LCOESensitivity";

        var fixedValues = new List<float> { 300_000f, 500_000f, 700_000f, 900_000f };

        var lcoeValuesList = new List<List<float>>();
        foreach (var fixedValue in fixedValues)
            lcoeValuesList.Add(
                [.. energyValues.Select(energy => (settings.VariableCosts * energy + fixedValue) / energy)]
            );

        energyLCOEValues.LCOEValuesList = lcoeValuesList;
        energyLCOEValues.Legends = [.. fixedValues.Select(x => settings.Formatting.FormatCurrency(x.ToInt()))];

        await PlotEnergyLCOEValues(
            fileName: "AllUnlabeled",
            subModule: subModule,
            values: energyLCOEValues,
            xLogScale: true,
            label: false
        );
        await PlotEnergyLCOEValues(fileName: "All", subModule: subModule, values: energyLCOEValues, xLogScale: true);
        await PlotEnergyLCOEValues(
            fileName: "All2",
            subModule: subModule,
            values: energyLCOEValues.WithRange(energyMin: all2LimitMin),
            xLogScale: true
        );
        await PlotEnergyLCOEValues(
            fileName: "EnergyLow",
            subModule: subModule,
            values: energyLCOEValues.WithRange(energyMax: energyLowLimitMax),
            xLogScale: true
        );
        await PlotEnergyLCOEValues(
            fileName: "EnergyLow2",
            subModule: subModule,
            values: energyLCOEValues.WithRange(energyMin: energyLow2LimitMin, energyMax: energyLowLimitMax),
            xLogScale: true
        );
        await PlotEnergyLCOEValues(
            fileName: "EnergyHigh",
            subModule: subModule,
            values: energyLCOEValues.WithRange(energyMin: energyHighLimitMin),
            xLogScale: true
        );
    }

    private async Task PlotEnergyLCOEValues(
        string fileName,
        string subModule,
        EnergyLCOEValues values,
        bool xLogScale = false,
        bool label = true
    )
    {
        var lcoeMax = values.LCOEValues.Max();

        if (values.LCOEValuesList.Count > 0)
        {
            var lcoeMaxFromList = values.LCOEValuesList.Select(x => x.Max()).Max();

            if (lcoeMaxFromList > lcoeMax)
                lcoeMax = lcoeMaxFromList;
        }

        var ys = new List<List<float>> { values.LCOEValues };
        var xs = new List<List<float>> { values.EnergyValues };
        var legends = new List<string?> { "Jednotkové náklady" };
        var colors = new List<string?> { EnercaColorsConsts.ColorPrimary };

        if (values.LCOEValuesList.Count > 0)
        {
            ys = [.. values.LCOEValuesList];
            xs = [.. Enumerable.Repeat(values.EnergyValues, values.LCOEValuesList.Count)];

            legends = [];
            foreach (var legend in values.Legends)
                legends.Add(legend);

            colors = [];
            foreach (var legend in values.Legends)
                colors.Add(null);
        }

        if (label)
        {
            if (values.WithoutBPProduction is not null)
            {
                ys.Add([0, lcoeMax]);
                xs.Add([values.WithoutBPProduction.Value, values.WithoutBPProduction.Value + MathConsts.Epsilon]);
                legends.Add("Výroba bez BP");
                colors.Add("#305CDE");
            }
            if (values.Consumption is not null)
            {
                ys.Add([0, lcoeMax]);
                xs.Add([values.Consumption.Value, values.Consumption.Value + MathConsts.Epsilon]);
                legends.Add("Spotřeba");
                colors.Add("#0BDA51");
            }
            if (values.Production is not null)
            {
                ys.Add([0, lcoeMax]);
                xs.Add([values.Production.Value, values.Production.Value + MathConsts.Epsilon]);
                legends.Add("Výroba");
                colors.Add("#FF2C2C");
            }
        }

        await PlotHelper.PlotAsync(
            data: new PlotData(
                path: GetPath(fileName, subModule),
                ys: ys,
                xs: xs,
                title: "Závislost jednotkových nákladů na sdílenou energii",
                xLabel: "Sdílená energie [kWh]",
                yLabel: "Jednotkové náklady [Kč/kWh]",
                legends: legends,
                colors: colors,
                scientificNotationAxisY: false,
                yTicksFormatter: settings.FormatFloat,
                xLogScale: xLogScale
            ),
            plts: [PythonPlotHelper.Plot]
        );

        var energyValues = values.EnergyValues;

        if (state.MinEnergyShared > energyValues.First() && state.MinEnergyShared < energyValues.Last())
        {
            ys.Add([0, lcoeMax]);
            xs.Add([state.MinEnergyShared, state.MinEnergyShared + MathConsts.Epsilon]);
            legends.Add("Minimální sdílená energie");
            colors.Add("#9D00FF");
        }

        if (
            state.MinEnergySharedWithoutBg > energyValues.First()
            && state.MinEnergySharedWithoutBg < energyValues.Last()
        )
        {
            ys.Add([0, lcoeMax]);
            xs.Add([state.MinEnergySharedWithoutBg, state.MinEnergySharedWithoutBg + MathConsts.Epsilon]);
            legends.Add("Minimální sdílená energie bez BP");
            colors.Add("#3C0061");
        }

        await PlotHelper.PlotAsync(
            data: new PlotData(
                path: GetPath(fileName + "Targets", dir: subModule),
                ys: ys,
                xs: xs,
                title: "Závislost jednotkových nákladů na sdílenou energii",
                xLabel: "Sdílená energie [kWh]",
                yLabel: "Jednotkové náklady [Kč/kWh]",
                legends: legends,
                colors: colors,
                scientificNotationAxisY: false,
                yTicksFormatter: settings.FormatFloat,
                xLogScale: xLogScale
            ),
            plts: [PythonPlotHelper.Plot]
        );
    }

    private class EnergyLCOEValues
    {
        public required List<float> EnergyValues { get; set; }
        public required List<float> LCOEValues { get; set; }

        public required List<List<float>> LCOEValuesList { get; set; }
        public required List<string> Legends { get; set; }

        public required float? WithoutBPProduction { get; set; }
        public required float? Consumption { get; set; }
        public required float? Production { get; set; }

        public EnergyLCOEValues WithRange(float? energyMin = null, float? energyMax = null)
        {
            var newEnergyValues = new List<float>();
            var newLcoeValues = new List<float>();

            var newLcoeValuesList = new List<List<float>>();
            foreach (var _ in LCOEValuesList)
                newLcoeValuesList.Add([]);

            for (var i = 0; i < EnergyValues.Count; i++)
            {
                var energy = EnergyValues[i];
                var lcoe = LCOEValues[i];

                if ((energyMin is null || energy >= energyMin) && (energyMax is null || energy <= energyMax))
                {
                    newEnergyValues.Add(energy);
                    newLcoeValues.Add(lcoe);

                    for (var j = 0; j < LCOEValuesList.Count; j++)
                        newLcoeValuesList[j].Add(LCOEValuesList[j][i]);
                }
            }

            var newWithoutBPProduction = WithoutBPProduction;
            if (newWithoutBPProduction.HasValue)
            {
                if (energyMin is not null && newWithoutBPProduction < energyMin)
                    newWithoutBPProduction = null;
                if (energyMax is not null && newWithoutBPProduction > energyMax)
                    newWithoutBPProduction = null;
            }

            var newConsumption = Consumption;
            if (newConsumption.HasValue)
            {
                if (energyMin is not null && newConsumption < energyMin)
                    newConsumption = null;
                if (energyMax is not null && newConsumption > energyMax)
                    newConsumption = null;
            }

            var newProduction = Production;
            if (newProduction.HasValue)
            {
                if (energyMin is not null && newProduction < energyMin)
                    newProduction = null;
                if (energyMax is not null && newProduction > energyMax)
                    newProduction = null;
            }

            return new EnergyLCOEValues
            {
                EnergyValues = newEnergyValues,
                LCOEValues = newLcoeValues,
                LCOEValuesList = newLcoeValuesList,
                Legends = Legends,
                WithoutBPProduction = newWithoutBPProduction,
                Consumption = newConsumption,
                Production = newProduction,
            };
        }
    }

    private async Task PlotLCOEConsumerAsync()
    {
        var subModule = "M3LCOEConsumer";

        var entranceFee = 4_000;

        var internalPriceBenefitValues = Enumerable.Range(0, 9).Select(x => 0.1f * x).ToList();
        var energySharedValues = Enumerable.Range(0, 9).Select(x => 0 + 500f * x).ToList();

        var ys = new List<List<float>>();

        foreach (var internalPriceBenefit in internalPriceBenefitValues)
        {
            var presentValues = new List<float>();

            foreach (var energyShared in energySharedValues)
            {
                var annualBenefits = internalPriceBenefit * energyShared;
                var cashFlowVec = new CashFlowVec(years: 25);
                cashFlowVec.SetAllYears(value: annualBenefits);
                cashFlowVec.SetZeroYear(value: -entranceFee);

                var presentValue = EconomyEvaluatorPredefined_CS
                    .EconomyEvaluator2024.GetDiscountedCashFlowVec(cashFlowVec: cashFlowVec)
                    .Total;

                presentValues.Add(presentValue);
            }

            ys.Add(presentValues);
        }

        List<string?> legends = [];
        foreach (var internalPriceBenefit in internalPriceBenefitValues)
            legends.Add(internalPriceBenefit.ToString("N2").Replace(".", ",") + " Kč/kWh");

        await PlotHelper.PlotAsync(
            data: new PlotData(
                path: GetPath(fileName: "Consumer", dir: subModule),
                ys: ys,
                xs: [energySharedValues],
                title: "Závislost ekonomické hodnoty pro zákazníka",
                xLabel: "Sdílená energie [kWh]",
                yLabel: "Ekonomická hodnota [Kč]",
                legends: legends,
                scientificNotationAxisY: false,
                yTicksFormatter: x => settings.Formatting.FormatCurrency(x.ToInt())
            ),
            plts: [PythonPlotHelper.Plot]
        );
    }
}
