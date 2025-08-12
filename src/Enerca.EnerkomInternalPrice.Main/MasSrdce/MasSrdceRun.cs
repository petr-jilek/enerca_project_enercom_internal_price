using Enerca.EnerkomInternalPrice.Logic;
using Enerca.EnerkomInternalPrice.Logic.Helpers;
using Enerca.EnerkomInternalPrice.Logic.Models;
using Enerca.Logic.Common.Tensor.Db;
using Enerca.Logic.Common.Tensor.Extensions;
using Enerca.Logic.Modules.Compute.Db;
using Enerca.Logic.Modules.Compute.Mappers;
using Enerca.Logic.Modules.External.Db;
using Enerca.Logic.Modules.External.Db.Community;
using Enerca.Logic.Modules.OTValue.Abstractions.Extensions;
using Enerca.Logic.Modules.OTValue.Db;
using Enerca.Logic.Modules.OTValue.Db.DataTypes;
using Enerca.Logic.Modules.OTValue.Tensor.Abstractions;
using Enerca.Logic.Modules.OTValue.Tensor.Db;
using Enerca.Logic.Modules.OTValue.Tensor.Implementations.Optimize;
using Fastdo.Common.Modules.Files.Models;
using Fastdo.Common.Modules.Formattings.Implementations;
using TorchSharp;

namespace Enerca.EnerkomInternalPrice.Main.MasSrdce;

public class MasSrdceRun
{
    public static async Task RunAsync()
    {
        var pathSettings = new EIPPathSettings { PathProject = new() { DirPath = "MasSrdce" } };

        var computeModel = await EIPDataHelper.GetComputeModelAsync(pathSettings: pathSettings);
        DecreaseProduction(computeModel: computeModel, cpEntityLabel: "21", factor: 0.1f);

        var computeModelWithout18 = computeModel.Clone();
        EIPPlotServiceHelper.RemoveCPEntities(computeModelWithout18, x => x.InfoBasic.Label == "18");

        var pathOut = pathSettings.PathOut.WithAddedDirPath("cp37_production100");

        var plotAll = false;
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
        return;

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
        await plotSettings.PlotService.PlotAsync(db: db);
        await RunOptimizationAsync(db: db, pathOut: plotSettings.PathSettings);
    }

    private static async Task RunOptimizationAsync(ComputeModelDb db, PathSettings pathOut)
    {
        var allocation = torch.ones(db.CPEntities.Count, db.CPEntities.Count);
        var mask = torch.ones(db.CPEntities.Count, db.CPEntities.Count);

        for (var i = 0; i < db.CPEntities.Count; i++)
        {
            for (var j = 0; j < db.CPEntities.Count; j++)
            {
                if (i == j)
                {
                    mask[i, j] = 0;
                    allocation[i, j] = 0;

                    continue;
                }

                // TODO: Remove
                if (i > 4)
                {
                    mask[i, j] = 0;
                    allocation[i, j] = 0;
                }

                var eanP = db.CPEntities[i].EnergyTariffs.Electricity?.Info.EanP;
                var eanC = db.CPEntities[j].EnergyTariffs.Electricity?.Info.EanC;

                if (eanP == null || eanC == null)
                {
                    mask[i, j] = 0;
                    allocation[i, j] = 0;

                    continue;
                }
            }
        }

        var externalModel = new ExternalModelDb
        {
            Info = new ExternalModelInfoDb { CPEntityIds = [.. db.CPEntities.Select(x => x.InfoBasic.Id)] },
            Community = new ExternalModelCommunityDb
            {
                InternalPriceBuy = new OTValueFloatDb { Value = 0 },
                InternalPriceFee = new OTValueFloatDb { Value = 0 },
                Method = ExternalModelCommunitySharingMethodConsts.Static,
                AllocationCoefficients2Dor3DTensor = new OTValueTensorDb
                {
                    Settings = new OTValueSettingsDb { IsOptimized = true },
                    Tensor = new TensorDb
                    {
                        Values = [.. allocation.flatten().ToFloatArray1D()],
                        Shape = [.. allocation.shape],
                    },
                    Mask = new TensorDb { Values = [.. mask.flatten().ToFloatArray1D()], Shape = [.. mask.shape] },
                },
            },
        };

        db.ExternalModels.Add(externalModel);

        var model = await db.ToModelAsync();

        var otValues = model.OTValues.IsOptimized().OfType<IOTValueTensor>().ToList();

        Console.WriteLine(otValues.Count);

        Console.WriteLine(model.Compute(years: 25).PresentValue);

        Console.WriteLine("Initial");
        foreach (var otValue in otValues)
            otValue.Value[4..5].PrintTensorAsJulia();

        for (var i = 0; i < 10; i++)
        {
            Console.WriteLine($"Epoch: {i}");

            foreach (var otValue in otValues)
            {
                await otValue.OptimizerBase.ComputeAsync(f: () =>
                    Task.FromResult(-model.Compute(years: 25).PresentValue)
                );
                otValue.OptimizerBase.Commit();

                otValue.Value[4..5].PrintTensorAsJulia();

                Console.WriteLine("NPV:");
                Console.WriteLine(model.Compute(years: 25).PresentValue);
            }
        }
    }
}
