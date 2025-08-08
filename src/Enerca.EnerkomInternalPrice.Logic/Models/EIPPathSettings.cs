using Fastdo.Common.Consts;
using Fastdo.Common.Modules.Files.Models;

namespace Enerca.EnerkomInternalPrice.Logic.Models;

public class EIPPathSettings
{
    public required PathSettings PathProject { get; init; }

    public PathSettings PathData => PathProject.WithAddedDirPath("Data");

    public PathSettings PathFileData => PathData.WithFileName("Data").WithFileExtension(FileExtensionConsts.CSV);
    public PathSettings PathFileDataProductions =>
        PathData.WithFileName("DataProductions").WithFileExtension(FileExtensionConsts.CSV);
    public PathSettings PathFileDataConsumptions =>
        PathData.WithFileName("DataConsumptions").WithFileExtension(FileExtensionConsts.CSV);

    public PathSettings PathOut => PathProject.WithAddedDirPath("out");
}
