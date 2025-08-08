using Enerca.EnerkomInternalPrice.Logic;
using Enerca.EnerkomInternalPrice.Logic.Helpers;
using Enerca.EnerkomInternalPrice.Logic.Models;
using Fastdo.Common.Modules.Formattings.Implementations;

namespace Enerca.EnerkomInternalPrice.Main.MasRuze;

public class MasRuzeRun
{
    public static async Task RunAsync()
    {
        var pathSettings = new EIPPathSettings { PathProject = new() { DirPath = "MasRuze" } };

        var computeModel = await EIPDataHelper.GetComputeModelAsync(pathSettings: pathSettings);

        var plotSettings = new EIPPlotSettings
        {
            PathSettings = pathSettings.PathOut,
            Formatting = new FormattingCs(currency: "Kč"),
        };

        var plotService = new EIPPlotService(settings: plotSettings);

        await plotService.PlotAsync(db: computeModel);
    }
}
