using Enerca.EnerkomInternalPrice.Main.MasRuze;
using Enerca.EnerkomInternalPrice.Main.MasSrdce;
using Enerca.Logic.Common.Colors;
using Enerca.Python;
using Enerca.Python.Helpers;
using Fastdo.Common.Consts;

EnercaPythonSettings.PythonDllPath =
    "/opt/homebrew/opt/python@3.13/Frameworks/Python.framework/Versions/3.13/lib/libpython3.13.dylib";
EnercaPythonSettings.PythonPath = "../../packages/enerca_core_python/env/lib/python3.13/site-packages";
EnercaPythonSettings.GetColors = EnercaColorsHelper.GetColors;
EnercaPythonSettings.DefaultFormats = [FileExtensionConsts.PNG];

PythonHelper.Initialize();

// await MasRuzeRun.RunAsync();
await MasSrdceRun.RunAsync();
