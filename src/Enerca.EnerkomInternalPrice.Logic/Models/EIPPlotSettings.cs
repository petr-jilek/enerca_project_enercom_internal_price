using Fastdo.Common.Modules.Files.Models;
using Fastdo.Common.Modules.Formattings;

namespace Enerca.EnerkomInternalPrice.Logic.Models;

public class EIPPlotSettings
{
    public required PathSettings PathSettings { get; set; }
    public required IFormatting Formatting { get; set; }

    public int EvaluationYears { get; set; } = 25;
    public int ComputeYear { get; set; } = 0;

    public float VariableEnergyPrice { get; set; } = 3;
    public float VariableNetworkChargesPrice { get; set; } = 3;
    public float VariableSellPVPrice { get; set; } = 1;
    public float VariableSellBPPrice { get; set; } = 1;

    public float Latitude { get; set; } = 48.8248888f;
    public float Longitude { get; set; } = 14.5085012f;
    public float Angle { get; set; } = 35f;
    public float Aspect { get; set; } = 0f;

    public PathSettings GetPath(string fileName, string module, string? dir = null)
    {
        var path = PathSettings.WithAddedDirPath(module);

        if (dir is not null)
            path = path.WithAddedDirPath(dir);

        return path.WithFileName(fileName: fileName);
    }
}
