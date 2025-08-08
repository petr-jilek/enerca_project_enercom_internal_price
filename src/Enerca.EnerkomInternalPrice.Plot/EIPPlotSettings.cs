using Fastdo.Common.Modules.Files.Models;
using Fastdo.Common.Modules.Formattings;

namespace Enerca.EnerkomInternalPrice.Plot;

public class EIPPlotSettings
{
    public required PathSettings PathSettings { get; set; }
    public required IFormatting Formatting { get; set; }

    public int EvaluationYears { get; set; } = 25;
    public int ComputeYear { get; set; } = 0;

    public PathSettings GetPath(string fileName, string module, string? dir = null)
    {
        var path = PathSettings.WithAddedDirPath(module);

        if (dir is not null)
            path = path.WithAddedDirPath(dir);

        return path.WithFileName(fileName: fileName);
    }
}
