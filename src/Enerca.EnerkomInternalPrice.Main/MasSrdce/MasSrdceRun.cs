using Enerca.EnerkomInternalPrice.Logic;
using Enerca.EnerkomInternalPrice.Logic.Helpers;
using Enerca.EnerkomInternalPrice.Logic.Models;
using Enerca.Logic.Common.Logger;
using Enerca.Logic.EanTable;
using Enerca.Logic.Modules.Compute.Db;
using Enerca.Logic.Modules.Compute.Mappers;
using Enerca.Logic.Modules.External.Db;
using Enerca.Logic.Modules.External.Db.Community;
using Enerca.Logic.Modules.External.Implementations.Community.Consts;
using Enerca.Logic.Modules.External.Mappers.Community;
using Enerca.Logic.Modules.OTValue.Db.DataTypes;
using Enerca.Logic.Modules.OTValue.Tensor.Abstractions;
using Fastdo.Common.Modules.Files.Models;
using Fastdo.Common.Modules.Formattings.Implementations;

namespace Enerca.EnerkomInternalPrice.Main.MasSrdce;

public class MasSrdceRun
{
    public static async Task RunAsync()
    {
        Loggers.Logger.Off();

        var pathSettings = new EIPPathSettings { PathProject = new() { DirPath = "MasSrdce" } };

        var computeModel = await EIPDataHelper.GetComputeModelAsync(pathSettings: pathSettings);
        DecreaseProduction(computeModel: computeModel, cpEntityLabel: "21", factor: 0.1f);

        var computeModelWithout18 = computeModel.Clone();
        EIPPlotServiceHelper.RemoveCPEntities(computeModelWithout18, x => x.InfoBasic.Label == "18");

        var pathOut = pathSettings.PathOut.WithAddedDirPath("cp37_production100");

        var plotAll = true;
        var plotSettings = new EIPPlotSettings
        {
            PathSettings = pathOut.WithAddedDirPath("all"),
            Formatting = new FormattingCs(currency: "Kč"),
            FormatFloat = x => x.ToString().Replace(".", ","),
            M1 = plotAll || false,
            M2 = plotAll || false,
            M3 = plotAll || false,
            M4 = plotAll || false,
            M5 = plotAll || false,
            M6 = plotAll || false,
            M7 = plotAll || false,
        };
        await RunComputeModelAsync(db: computeModel, plotSettings: plotSettings);

        plotSettings.PathSettings = pathOut.WithAddedDirPath("without18");
        await RunComputeModelAsync(db: computeModelWithout18, plotSettings: plotSettings);

        DecreaseProduction(computeModel: computeModel, cpEntityLabel: "37", factor: 0.1f);

        computeModelWithout18 = computeModel.Clone();
        EIPPlotServiceHelper.RemoveCPEntities(computeModelWithout18, x => x.InfoBasic.Label == "18");

        pathOut = pathSettings.PathOut.WithAddedDirPath("cp37_production10");

        plotSettings.PathSettings = pathOut.WithAddedDirPath("all");
        await RunComputeModelAsync(db: computeModel, plotSettings: plotSettings);

        plotSettings.PathSettings = pathOut.WithAddedDirPath("without18");
        await RunComputeModelAsync(db: computeModelWithout18, plotSettings: plotSettings);
    }

    private static void DecreaseProduction(ComputeModelDb computeModel, string cpEntityLabel, float factor)
    {
        var cpEntity = computeModel.CPEntities.First(x => x.InfoBasic.Label == cpEntityLabel);

        foreach (var energyVec in cpEntity.EnergyState.Electricity!.Production!.EnergyVecs)
            energyVec.Values!.Values = [.. energyVec.Values.Values!.Select(x => factor * x)];
    }

    private static async Task RunComputeModelAsync(ComputeModelDb db, EIPPlotSettings plotSettings)
    {
        // await plotSettings.PlotService.PlotAsync(db: db);
        await RunOptimizationAsync(db: db, pathOut: plotSettings.PathSettings);
    }

    private static async Task RunOptimizationAsync(ComputeModelDb db, PathSettings pathOut)
    {
        var externalModel = new ExternalModelDb
        {
            Info = new ExternalModelInfoDb { CPEntityIds = [.. db.CPEntities.Select(x => x.InfoBasic.Id)] },
            Community = new ExternalModelCommunityDb
            {
                InternalPriceBuy = new OTValueFloatDb { Value = 0 },
                InternalPriceFee = new OTValueFloatDb { Value = 0 },
                Method = ExternalModelCommunitySharingMethodConsts.Static,
            },
        };

        db.ExternalModels.Add(externalModel);

        var model = await db.ToModelAsync();

        var otValues = model
            .OTValues.OfType<IOTValueTensor>()
            .Where(x =>
                x.Info.ResolvedLabel.Contains(ExternalModelCommunityOTValueConsts.AllocationCoefficients2DTensor)
            )
            .ToList();

        if (otValues.Count != 1)
            throw new Exception("OTValues count is not 1");

        var allocation = otValues.First();
        allocation.Settings.IsOptimized = true;

        Task<float> npv() => Task.FromResult(model.Compute(years: 25).PresentValue);

        await allocation.OptimizeAsAllocationAsync(
            f: async () => -await npv(),
            callback: async (part, epoch) =>
            {
                var npv_ = await npv();

                Loggers.Logger.On();
                Loggers.Logger.Log($"Part: {part}, Epoch: {epoch}, NPV: {npv_}");
                Loggers.Logger.Off();

                await model
                    .GetEanTables(
                        cpEntityIds: externalModel.Info.CPEntityIds,
                        allocationCoefficients2DTensor: allocation.Value
                    )
                    .SaveToCsvAsync(
                        path: pathOut.WithAddedDirPath("EanTables").WithAddedDirPath($"Part{part}_Epoch{epoch}")
                    );
            }
        );
    }
}
