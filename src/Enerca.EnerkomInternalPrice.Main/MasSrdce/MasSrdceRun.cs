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

        var plotSettings = new EIPPlotSettings
        {
            PathSettings = pathSettings.PathOut,
            Formatting = new FormattingCs(currency: "Kč"),
            FormatFloat = x => x.ToString().Replace(".", ","),
        };

        var plotService = new EIPPlotService(settings: plotSettings);

        await plotService.PlotAsync(db: computeModel);
    }
}
