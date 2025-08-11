using System.Text.Json;
using Enerca.EnerkomInternalPrice.Logic.Consts;
using Enerca.EnerkomInternalPrice.Logic.Models;
using Enerca.Logic.Modules.Compute.Db;
using Enerca.Logic.Modules.Compute.Mappers;
using Enerca.Logic.Modules.Economy.Abstractions;
using Enerca.Logic.Modules.Economy.Mappers.Predefined;
using Enerca.Python.Helpers;
using Enerca.Python.Models;
using Fastdo.Common.Extensions;
using Fastdo.Common.Modules.Files.Models;

namespace Enerca.EnerkomInternalPrice.Logic.Plot;

public class PlotM7CaseService(EIPPlotSettings settings)
{
    public async Task PlotAsync(ComputeModelDb db)
    {
        var db_ = db.Clone();
        var dbWithoutBg_ = db.Clone();

        EIPPlotServiceHelper.RemoveBgs(db: dbWithoutBg_);

        await PlotCurrentAsync(db: db_, dbWithoutBg: dbWithoutBg_);

        await PlotModelAsync(db: db_, dir: "m2_WithBg");
        await PlotModelAsync(db: dbWithoutBg_, dir: "m3_WithoutBg");
    }

    private PathSettings GetPath(string fileName, string? dir = null) =>
        settings.GetPath(fileName: fileName, module: "M7Case", dir: dir);

    private async Task PlotCurrentAsync(ComputeModelDb db, ComputeModelDb dbWithoutBg)
    {
        var dir = "m1_Current";

        var model = await db.ToModelAsync();
        var modelWithoutBg = await dbWithoutBg.ToModelAsync();

        var result = model.Compute(settings.EvaluationYears);
        var resultWithoutBg = modelWithoutBg.Compute(settings.EvaluationYears);

        var annualBenefits = result.CashFlowVec[1];
        var annualBenefitsWithoutBg = resultWithoutBg.CashFlowVec[1];

        var npv = result.PresentValue;
        var npvWithoutBg = resultWithoutBg.PresentValue;

        await PlotHelper.PlotAsync(
            data: new PlotData(
                path: GetPath("AnnualCashFlow", dir: dir),
                ys:
                [
                    [annualBenefits],
                    [annualBenefitsWithoutBg],
                ],
                title: "Roční hotovostní tok",
                yLabel: "Hotovostní tok [Kč]",
                legends: ["Se zapojením BP", "Bez zapojení BP"],
                scientificNotationAxisY: false,
                yTicksFormatter: x => settings.Formatting.FormatCurrency(x.ToInt()),
                xTicksFormatter: _ => string.Empty
            ),
            plts: [PythonPlotHelper.PlotBar]
        );
        await PlotHelper.PlotAsync(
            data: new PlotData(
                path: GetPath("NPV", dir: dir),
                ys:
                [
                    [npv],
                    [npvWithoutBg],
                ],
                title: "NPV",
                yLabel: "NPV [Kč]",
                legends: ["Se zapojením BP", "Bez zapojení BP"],
                scientificNotationAxisY: false,
                yTicksFormatter: x => settings.Formatting.FormatCurrency(x.ToInt()),
                xTicksFormatter: _ => string.Empty
            ),
            plts: [PythonPlotHelper.PlotBar]
        );
    }

    private async Task PlotModelAsync(ComputeModelDb db, string dir)
    {
        var internalPriceBuy = 1.5f;
        var internalPriceFee = 1f;

        var fixedCosts = 700_000;

        foreach (var externalModel in db.ExternalModels)
        {
            if (externalModel.Community is not null)
            {
                externalModel.Community.InternalPriceBuy.Value = internalPriceBuy;
                externalModel.Community.InternalPriceFee.Value = internalPriceFee;
            }
        }

        var model = await db.ToModelAsync();
        var model0 = model.GetForYear(settings.ComputeYear);

        var result = model.Compute(settings.EvaluationYears);
        var result0 = model0.Compute();

        var electricityShared =
            result0.EnergyStateFinalSystem.Electricity.Consumption.Total
            - result0.EnergyStateFinal.Electricity.Consumption.Total;

        var annualBenefits = result.CashFlowVec[1];
        var annualBenefitsEnerkom = electricityShared * (internalPriceFee - 0.2f) - fixedCosts;
        var npv = result.PresentValue;

        var cashFlowVecEnerkom = new CashFlowVec(years: settings.EvaluationYears);
        cashFlowVecEnerkom.SetAllYears(annualBenefitsEnerkom);
        cashFlowVecEnerkom.SetZeroYearToZero();

        var npvEnerkom = EconomyEvaluatorPredefined_CS
            .EconomyEvaluator2024.GetDiscountedCashFlowVec(cashFlowVec: cashFlowVecEnerkom)
            .Total;

        // await PlotHelper.PlotAsync(
        //     data: new PlotData(
        //         path: GetPath("Benefits", dir: dir),
        //         ys:
        //         [
        //             [.. result.BenefitsVec.Values]
        //         ],
        //         title: "Roční úspory",
        //         xLabel: "Rok",
        //         yLabel: "Úspora [Kč]",
        //         scientificNotationAxisY: false,
        //         yTicksFormatter: x => V7MasRuzeConsts.Formatting.FormatCurrency(x.ToInt())
        //     ),
        //     plts: [PythonPlotHelper.PlotBar]
        // );

        await PlotHelper.PlotAsync(
            data: new PlotData(
                path: GetPath("AnnualCashFlow", dir: dir),
                ys:
                [
                    [annualBenefits],
                    [annualBenefitsEnerkom],
                ],
                title: "Roční hotovostní tok",
                yLabel: "Hotovostní tok [Kč]",
                legends: ["Výrobci + spotřebitelé", "Enerkom"],
                scientificNotationAxisY: false,
                yTicksFormatter: x => settings.Formatting.FormatCurrency(x.ToInt()),
                xTicksFormatter: _ => string.Empty
            ),
            plts: [PythonPlotHelper.PlotBar]
        );

        await PlotHelper.PlotAsync(
            data: new PlotData(
                path: GetPath("NPV", dir: dir),
                ys:
                [
                    [npv],
                    [npvEnerkom],
                ],
                title: "NPV",
                yLabel: "NPV [Kč]",
                legends: ["Výrobci + spotřebitelé", "Enerkom"],
                scientificNotationAxisY: false,
                yTicksFormatter: x => settings.Formatting.FormatCurrency(x.ToInt()),
                xTicksFormatter: _ => string.Empty
            ),
            plts: [PythonPlotHelper.PlotBar]
        );

        var chunkSize = 8;
        var resultChunks = result.CPEntityResults.SplitIntoChunks(chunkSize).ToList();
        var cpEntitiesChunks = model.CPEntities.SplitIntoChunks(chunkSize).ToList();

        for (var i = 0; i < resultChunks.Count; i++)
        {
            var cpeEntityResults = resultChunks[i];
            var cpEntities = cpEntitiesChunks[i];

            var labels = new List<string?>();

            foreach (var cpEntity in cpEntities)
            {
                var label = cpEntity.InfoBasic.Label;
                var data =
                    JsonSerializer.Deserialize<EIPCPEntityData>(
                        cpEntity.InfoBasic.Note ?? throw new Exception("No note")
                    ) ?? throw new Exception("No data");

                if (cpEntity.InfoBasic.Tags.Contains(EIPPlotConsts.TagBg))
                    label += $" - BP ({data.ProductionSystemId})";
                else if (cpEntity.InfoBasic.Tags.Contains(EIPPlotConsts.TagPv))
                    label += $" - PV ({data.ProductionSystemId})";
                else
                    label += $" - Spotřeba ({data.Tariff})";

                labels.Add(label);
            }

            List<List<float>> annualCashFlows =
            [
                [.. cpeEntityResults.Select(x => x.CashFlowVec[1])],
            ];

            List<List<float>> presentValues =
            [
                [.. cpeEntityResults.Select(x => x.PresentValue)],
            ];

            await PlotHelper.PlotAsync(
                data: new PlotData(
                    path: GetPath($"AnnualCashFlow_{i + 1}", dir: dir),
                    ys: presentValues.Transpose(),
                    title: "Roční hotovostní tok jednotlivých objektů",
                    yLabel: "Hotovostní tok [Kč]",
                    useEnercaColors: false,
                    legends: labels,
                    scientificNotationAxisY: false,
                    yTicksFormatter: x => settings.Formatting.FormatCurrency(x.ToInt()),
                    xTicksFormatter: _ => string.Empty
                ),
                plts: [PythonPlotHelper.PlotBar]
            );

            await PlotHelper.PlotAsync(
                data: new PlotData(
                    path: GetPath($"PresentValues_{i + 1}", dir: dir),
                    ys: presentValues.Transpose(),
                    title: "Současné hodnoty jednotlivých objektů",
                    yLabel: "NPV [Kč]",
                    useEnercaColors: false,
                    legends: labels,
                    scientificNotationAxisY: false,
                    yTicksFormatter: x => settings.Formatting.FormatCurrency(x.ToInt()),
                    xTicksFormatter: _ => string.Empty
                ),
                plts: [PythonPlotHelper.PlotBar]
            );
        }
    }
}
