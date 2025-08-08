using Enerca.EnerkomInternalPrice.Logic.Helpers;
using Enerca.EnerkomInternalPrice.Logic.Models;
using Enerca.EnerkomInternalPrice.Logic.Plot;
using Enerca.Logic.Common.Colors;
using Enerca.Python;
using Enerca.Python.Helpers;
using Fastdo.Common.Modules.Formattings.Implementations;

EnercaPythonSettings.PythonDllPath =
    "/opt/homebrew/opt/python@3.13/Frameworks/Python.framework/Versions/3.13/lib/libpython3.13.dylib";
EnercaPythonSettings.PythonPath = "../../packages/enerca_core_python/env/lib/python3.13/site-packages";
EnercaPythonSettings.GetColors = EnercaColorsHelper.GetColors;

PythonHelper.Initialize();

var masRuzePathSettings = new EIPPathSettings { PathProject = new() { DirPath = "MasRuze" } };

var computeModel = await EIPDataHelper.GetComputeModelAsync(pathSettings: masRuzePathSettings);

var plotSettings = new EIPPlotSettings
{
    PathSettings = masRuzePathSettings.PathOut,
    Formatting = new FormattingCs(currency: "Kč"),
};

var plotService = new EIPPlotService(settings: plotSettings);

await plotService.PlotAsync(db: computeModel);
