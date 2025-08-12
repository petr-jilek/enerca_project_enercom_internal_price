using Enerca.EnerkomInternalPrice.Logic.Consts;
using Enerca.EnerkomInternalPrice.Logic.Models;
using Enerca.Logic.Modules.Compute.Abstractions;
using Enerca.Logic.Modules.Energy.Implementations.Implementations.ScalingUp;
using Enerca.Logic.Modules.Tdd.Mappers.Extensions;
using Enerca.Python.Helpers;
using Enerca.Python.Models;
using Fastdo.Common.Extensions;
using Fastdo.Common.Modules.Files.Models;

namespace Enerca.EnerkomInternalPrice.Logic.Plot;

public class PlotM1CurrentStateTechnicalService(EIPPlotSettings settings)
{
    public async Task PlotAsync(IComputeModel model)
    {
        await PlotDiagramsEachAsync(model: model);
        await PlotDiagramsTotalAsync(model: model);
    }

    private PathSettings GetPath(string fileName, string? dir = null) =>
        settings.GetPath(fileName: fileName, module: "M1CurrentStateTechnical", dir: dir);

    private async Task PlotDiagramsEachAsync(IComputeModel model)
    {
        var subModule = "M1DiagramsEach";

        var result = model.Compute();

        var cpEntities = model.CPEntities.ToList();
        var cpResults = result.CPEntityResults.ToList();

        for (var i = 0; i < cpEntities.Count; i++)
        {
            var cpEntity = cpEntities[i];
            var cpResult = cpResults[i];
            var cpResultPre = cpResult.ResultPre;

            var dir = subModule.AddPath(cpEntity.InfoBasic.Label ?? cpEntity.InfoBasic.Name ?? cpEntity.InfoBasic.Id);

            var electricityInitial = cpResultPre.EnergyStateInitial.Electricity;
            var electricityInitialBalanced = cpResultPre.EnergyStateInitialBalanced.Electricity;
            var electricityFinalSystem = cpResultPre.EnergyStateFinalSystem.Electricity;
            var electricityFinal = cpResultPre.EnergyStateFinal.Electricity;

            await PlotCP(
                path: GetPath(fileName: "", dir: dir.AddPath("Initial")),
                cValues: [.. electricityInitial.Consumption.Values],
                pValues: [.. electricityInitial.Production.Values]
            );
            await PlotCP(
                path: GetPath(fileName: "", dir: dir.AddPath("InitialBalanced")),
                cValues: [.. electricityInitialBalanced.Consumption.Values],
                pValues: [.. electricityInitialBalanced.Production.Values]
            );
            await PlotCP(
                path: GetPath(fileName: "", dir: dir.AddPath("FinalSystem")),
                cValues: [.. electricityFinalSystem.Consumption.Values],
                pValues: [.. electricityFinalSystem.Production.Values]
            );
            await PlotCP(
                path: GetPath(fileName: "", dir: dir.AddPath("Final")),
                cValues: [.. electricityFinal.Consumption.Values],
                pValues: [.. electricityFinal.Production.Values]
            );

            var info = cpEntity.EnergyTariffs.Electricity.Info;

            var tdd = info.ToTdd2025();

            if (tdd is not null)
            {
                var tddConsumption = await tdd.GetValues(valueAnnual: electricityInitial.Consumption.Total);

                await PlotHelper.PlotAsync(
                    data: new PlotData(
                        path: GetPath(fileName: "InitialConsumptionTDD", dir: dir),
                        ys:
                        [
                            [.. tddConsumption],
                        ],
                        title: "Spotřeba podle TDD",
                        xLabel: "Čas",
                        yLabel: "Spotřeba [kWh]",
                        scientificNotationAxisY: false
                    ),
                    plts: [PythonPlotHelper.Plot]
                );

                var tddConsumptionSameTimeSteps = new EnergyVecScalingUp(values: [.. tddConsumption]).GetScaled(
                    timeSteps: electricityInitial.Consumption.TimeSteps
                );

                await PlotHelper.PlotAsync(
                    data: new PlotData(
                        path: GetPath(fileName: "InitialConsumptionTDDScaled", dir: dir),
                        ys:
                        [
                            [.. tddConsumptionSameTimeSteps.Values],
                        ],
                        title: "Spotřeba podle TDD",
                        xLabel: "Čas",
                        yLabel: "Spotřeba [kWh]",
                        scientificNotationAxisY: false
                    ),
                    plts: [PythonPlotHelper.Plot]
                );
            }
        }
    }

    private async Task PlotDiagramsTotalAsync(IComputeModel model)
    {
        var dir = "M2DiagramsTotal";

        var result = model.Compute();

        var pvConsumption = new EnergyVecScalingUp();
        var pvProduction = new EnergyVecScalingUp();
        var pvConsumptionBalanced = new EnergyVecScalingUp();
        var pvProductionBalanced = new EnergyVecScalingUp();

        var bpConsumption = new EnergyVecScalingUp();
        var bpProduction = new EnergyVecScalingUp();
        var bpConsumptionBalanced = new EnergyVecScalingUp();
        var bpProductionBalanced = new EnergyVecScalingUp();

        foreach (var cpEntity in model.CPEntities)
        {
            var consumption = cpEntity.EnergyState.Electricity.Consumption;
            var production = cpEntity.EnergyState.Electricity.Production;

            if (cpEntity.InfoBasic.Tags.Contains(EIPPlotConsts.TagPv))
            {
                pvConsumption.Mutable_Increase(consumption);
                pvProduction.Mutable_Increase(production);

                var energyCP = new EnergyCPScalingUp(consumption: consumption, production: production);
                energyCP.Mutable_Balance();

                pvConsumptionBalanced.Mutable_Increase(energyCP.Consumption);
                pvProductionBalanced.Mutable_Increase(energyCP.Production);
            }
            else if (cpEntity.InfoBasic.Tags.Contains(EIPPlotConsts.TagBg))
            {
                bpConsumption.Mutable_Increase(consumption);
                bpProduction.Mutable_Increase(production);

                var energyCP = new EnergyCPScalingUp(consumption: consumption, production: production);
                energyCP.Mutable_Balance();

                bpConsumptionBalanced.Mutable_Increase(energyCP.Consumption);
                bpProductionBalanced.Mutable_Increase(energyCP.Production);
            }
        }

        await PlotCP(
            path: GetPath(fileName: "", dir: dir.AddPath("Pv")),
            cValues: [.. pvConsumption.Values],
            pValues: [.. pvProduction.Values]
        );
        await PlotCP(
            path: GetPath(fileName: "", dir: dir.AddPath("PvBalancedSeparately")),
            cValues: [.. pvConsumptionBalanced.Values],
            pValues: [.. pvProductionBalanced.Values]
        );
        await PlotCP(
            path: GetPath(fileName: "", dir: dir.AddPath("Bg")),
            cValues: [.. bpConsumption.Values],
            pValues: [.. bpProduction.Values]
        );
        await PlotCP(
            path: GetPath(fileName: "", dir: dir.AddPath("BgBalancedSeparately")),
            cValues: [.. bpConsumptionBalanced.Values],
            pValues: [.. bpProductionBalanced.Values]
        );

        var pvCP = new EnergyCPScalingUp(consumption: pvConsumption, production: pvProduction);
        pvCP.Mutable_Balance();
        var bpCP = new EnergyCPScalingUp(consumption: bpConsumption, production: bpProduction);
        bpCP.Mutable_Balance();

        await PlotCP(
            path: GetPath(fileName: "", dir: dir.AddPath("PvBalanced")),
            cValues: [.. pvCP.Consumption.Values],
            pValues: [.. pvCP.Production.Values]
        );
        await PlotCP(
            path: GetPath(fileName: "", dir: dir.AddPath("BgBalanced")),
            cValues: [.. bpCP.Consumption.Values],
            pValues: [.. bpCP.Production.Values]
        );

        var electricityTotal = result.EnergyStateInitial.Clone().Electricity;

        await PlotCP(
            path: GetPath(fileName: "", dir: dir.AddPath("Total")),
            cValues: [.. electricityTotal.Consumption.Values],
            pValues: [.. electricityTotal.Production.Values]
        );

        var electricityTotalClone = electricityTotal.Clone();
        electricityTotalClone.Mutable_Balance();

        await PlotCP(
            path: GetPath(fileName: "", dir: dir.AddPath("TotalBalanced")),
            cValues: [.. electricityTotalClone.Consumption.Values],
            pValues: [.. electricityTotalClone.Production.Values]
        );

        var consumptionWithoutBg = new EnergyVecScalingUp();
        consumptionWithoutBg.Mutable_Increase(electricityTotal.Consumption);
        consumptionWithoutBg.Mutable_Decrease(bpConsumption);

        await PlotHelper.PlotAsync(
            data: new PlotData(
                path: GetPath(fileName: "ConsumptionWithoutBg", dir: dir),
                ys:
                [
                    [.. consumptionWithoutBg.Values],
                ],
                title: "Spotřeba bez spotřeby bioplynových stanic",
                xLabel: "Čas",
                yLabel: "Spotřeba [kWh]",
                scientificNotationAxisY: false
            ),
            plts: [PythonPlotHelper.Plot]
        );

        await PlotHelper.PlotAsync(
            data: new PlotData(
                path: GetPath(fileName: "ProductionCombined", dir: dir),
                ys:
                [
                    [.. electricityTotal.Production.Values],
                    [.. bpProduction.Values],
                ],
                title: "Výroba",
                xLabel: "Čas",
                yLabel: "Výroba [kWh]",
                legends: ["Příspěvek FVE", "Výroba bioplynových stanic"],
                scientificNotationAxisY: false
            ),
            plts: [PythonPlotHelper.Plot]
        );

        await PlotHelper.PlotAsync(
            data: new PlotData(
                path: GetPath(fileName: "Comparison", dir: dir),
                ys:
                [
                    [pvProduction.Total, bpProduction.Total, electricityTotal.Consumption.Total],
                ],
                title: "Srovnání výroby a spotřeby",
                yLabel: "Energie [kWh]",
                scientificNotationAxisY: false,
                xTicksLabels: ["FVE", "BP", "Spotřeba"],
                yLogScale: true
            ),
            plts: [PythonPlotHelper.PlotBar]
        );
    }

    private static async Task PlotCP(PathSettings path, List<float> cValues, List<float> pValues)
    {
        await PlotHelper.PlotAsync(
            data: new PlotData(
                path: path.WithFileName(fileName: path.FileName + "Consumption"),
                ys: [cValues],
                title: "Spotřeba",
                xLabel: "Čas",
                yLabel: "Spotřeba [kWh]",
                scientificNotationAxisY: false
            ),
            plts: [PythonPlotHelper.Plot]
        );

        await PlotHelper.PlotAsync(
            data: new PlotData(
                path: path.WithFileName(fileName: path.FileName + "Production"),
                ys: [pValues],
                title: "Výroba",
                xLabel: "Čas",
                yLabel: "Výroba [kWh]",
                scientificNotationAxisY: false
            ),
            plts: [PythonPlotHelper.Plot]
        );
    }
}
