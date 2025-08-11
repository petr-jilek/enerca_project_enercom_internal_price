using Enerca.EnerkomInternalPrice.Logic;
using Enerca.EnerkomInternalPrice.Logic.Helpers;
using Enerca.EnerkomInternalPrice.Logic.Models;
using Enerca.Logic.Modules.Compute.Db;
using Fastdo.Common.Modules.Formattings.Implementations;

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

        var plotSettings = new EIPPlotSettings
        {
            PathSettings = pathOut.WithAddedDirPath("all"),
            Formatting = new FormattingCs(currency: "Kč"),
            FormatFloat = x => x.ToString().Replace(".", ","),
            M1 = false,
            M6 = false,
        };
        await plotSettings.PlotService.PlotAsync(db: computeModel);

        plotSettings.PathSettings = pathOut.WithAddedDirPath("without18");
        await plotSettings.PlotService.PlotAsync(db: computeModelWithout18);

        DecreaseProduction(computeModel: computeModel, cpEntityLabel: "37", factor: 0.1f);

        computeModelWithout18 = computeModel.Clone();
        EIPPlotServiceHelper.RemoveCPEntities(computeModelWithout18, x => x.InfoBasic.Label == "18");

        pathOut = pathSettings.PathOut.WithAddedDirPath("cp37_production10");

        plotSettings.PathSettings = pathOut.WithAddedDirPath("all");
        await plotSettings.PlotService.PlotAsync(db: computeModel);

        plotSettings.PathSettings = pathOut.WithAddedDirPath("without18");
        await plotSettings.PlotService.PlotAsync(db: computeModelWithout18);
    }

    private static void DecreaseProduction(ComputeModelDb computeModel, string cpEntityLabel, float factor)
    {
        var cpEntity = computeModel.CPEntities.First(x => x.InfoBasic.Label == cpEntityLabel);

        foreach (var energyVec in cpEntity.EnergyState.Electricity!.Production!.EnergyVecs)
            energyVec.Values!.Values = [.. energyVec.Values.Values!.Select(x => factor * x)];
    }
}
