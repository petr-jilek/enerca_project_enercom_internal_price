using Fastdo.Common.Modules.Files.Models;
using Fastdo.Common.Modules.Formattings;

namespace Enerca.EnerkomInternalPrice.Logic.Models;

public class EIPPlotSettings
{
    public required PathSettings PathSettings { get; set; }
    public required IFormatting Formatting { get; set; }
    public required Func<float, string> FormatFloat { get; set; }

    public bool M1 { get; set; } = true;
    public bool M2 { get; set; } = true;
    public bool M3 { get; set; } = true;
    public bool M4 { get; set; } = true;
    public bool M5 { get; set; } = true;
    public bool M6 { get; set; } = true;
    public bool M7 { get; set; } = true;

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

    public float FixedCosts { get; set; } = 700_000f;
    public float VariableCosts { get; set; } = 0.2f;

    public EIPPlotService PlotService => new(settings: this);

    public PathSettings GetPath(string fileName, string module, string? dir = null)
    {
        var path = PathSettings.WithAddedDirPath(module);

        if (dir is not null)
            path = path.WithAddedDirPath(dir);

        return path.WithFileName(fileName: fileName);
    }
}
