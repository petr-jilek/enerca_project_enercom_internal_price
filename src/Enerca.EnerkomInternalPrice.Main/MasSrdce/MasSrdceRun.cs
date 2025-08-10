using Enerca.EnerkomInternalPrice.Logic;
using Enerca.EnerkomInternalPrice.Logic.Helpers;
using Enerca.EnerkomInternalPrice.Logic.Models;
using Fastdo.Common.Modules.Formattings.Implementations;

namespace Enerca.EnerkomInternalPrice.Main.MasSrdce;

public class MasSrdceRun
{
    public static async Task RunAsync()
    {
        var pathSettings = new EIPPathSettings { PathProject = new() { DirPath = "MasSrdce" } };

        var computeModel = await EIPDataHelper.GetComputeModelAsync(pathSettings: pathSettings);
        var computeModelWithout18 = computeModel.Clone();
        EIPPlotServiceHelper.RemoveCPEntities(computeModelWithout18, x => x.InfoBasic.Label == "18");

        var plotSettings = new EIPPlotSettings
        {
            PathSettings = pathSettings.PathOut.WithAddedDirPath("all"),
            Formatting = new FormattingCs(currency: "Kč"),
            FormatFloat = x => x.ToString().Replace(".", ","),
        };
        var plotSettingsWithout18 = new EIPPlotSettings
        {
            PathSettings = pathSettings.PathOut.WithAddedDirPath("without18"),
            Formatting = new FormattingCs(currency: "Kč"),
            FormatFloat = x => x.ToString().Replace(".", ","),
        };

        var plotService = new EIPPlotService(settings: plotSettings);
        var plotServiceWithout18 = new EIPPlotService(settings: plotSettingsWithout18);

        await plotService.PlotAsync(db: computeModel);
        await plotServiceWithout18.PlotAsync(db: computeModelWithout18);
    }
}
