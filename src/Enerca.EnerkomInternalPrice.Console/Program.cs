using Enerca.EnerkomInternalPrice.Console.Helpers;
using Enerca.EnerkomInternalPrice.Console.Models;
using Fastdo.Common.Extensions;

var masRuzePathSettings = new EIPPathSettings { PathProject = new() { DirPath = "Projects".AddPath("MasRuze") } };

var computeModel = await EIPDataHelper.GetComputeModelAsync(pathSettings: masRuzePathSettings);
