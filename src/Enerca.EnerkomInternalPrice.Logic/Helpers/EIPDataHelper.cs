using Enerca.EnerkomInternalPrice.Logic.Consts;
using Enerca.EnerkomInternalPrice.Logic.Models;
using Enerca.Logic.Modules.Compute.Common;
using Enerca.Logic.Modules.Compute.Db;
using Enerca.Logic.Modules.CPEntity.Db;
using Enerca.Logic.Modules.CPEntity.Db.Models;
using Enerca.Logic.Modules.Energy.Db;
using Enerca.Logic.Modules.EnergyTariff.Db;
using Enerca.Logic.Modules.EnergyTariff.Db.Electricity;
using Enerca.Logic.Modules.EnergyTariff.Db.Electricity.Implementations;
using Enerca.Logic.Modules.EnergyTariffInfo.Db;
using Enerca.Logic.Modules.LargeVec.Db;
using Enerca.Logic.Modules.Tdd.Db.Extensions;
using Enerca.Logic.Modules.Tdd.Db.Models;
using Enerca.Logic.Modules.Tdd.Db.Modules;
using Fastdo.Common.Modules.Files.Helpers;

namespace Enerca.EnerkomInternalPrice.Logic.Helpers;

public static class EIPDataHelper
{
    public static async Task<ComputeModelDb> GetComputeModelAsync(EIPPathSettings pathSettings)
    {
        var cpEntities = await GetCPEntitiesAsync(pathSettings: pathSettings);

        return new ComputeModelDb
        {
            CPEntities = cpEntities,
            InvariancyLevel = (int)ComputeModelInvariancyLevel.Level2Economic,
        };
    }

    public static async Task<List<CPEntityDb>> GetCPEntitiesAsync(EIPPathSettings pathSettings)
    {
        var dataWithValuesList = await EIPCsvHelper.GetDataWithValuesFromCsvAsync(pathSettings: pathSettings);

        return GetCPEntities(dataWithValuesList: dataWithValuesList);
    }

    public static List<CPEntityDb> GetCPEntities(List<EIPCPEntityDataWithValues> dataWithValuesList)
    {
        var cpEntities = new List<CPEntityDb>();

        foreach (var itemWithValues in dataWithValuesList)
        {
            var item = itemWithValues.Data;

            var tags = new List<string>();
            if (item.ProductionType_.Contains(EIPPlotConsts.TagPv) || item.ProductionType_.Contains("fve"))
                tags.Add(EIPPlotConsts.TagPv);
            if (item.ProductionType_.Contains(EIPPlotConsts.TagBg) || item.ProductionType_.Contains("bp"))
                tags.Add(EIPPlotConsts.TagBg);

            var cpEntity = new CPEntityDb
            {
                InfoBasic = new InfoBasicDb
                {
                    Label = item.Id,
                    Name = item.Name,
                    Address = item.Address,
                    Tags = tags,
                    Note = JsonHelper.ToJson(item),
                },
                InfoExternal = new InfoExternalDb { PlaceId = item.Id },
                EnergyTariffs = new EnergyTariffsDb
                {
                    Electricity = new EnergyTariffElectricityDb
                    {
                        Info = new EnergyTariffElectricityInfoDb
                        {
                            Tariff = item.Tariff,
                            Provider = item.Provider,
                            PhaseCount = item.PhaseCount,
                            Current = item.Current,
                            Voltage = item.Voltage,
                            EanC = item.EanC,
                            EanP = item.EanP,
                        },

                        Fixed = new EnergyTariffElectricityFixedDb
                        {
                            VariableEnergy = item.VariableEnergy,
                            VariableNetworkCharges = item.VariableDistribution,
                            VariableSell = item.VariableSell,
                            Fixed = item.Fixed,
                        },
                    },
                },
                EnergyState = new EnergyStateDb
                {
                    Electricity = new EnergyCPDb
                    {
                        Consumption = GetConsumption(itemWithValues),
                        Production = GetProduction(itemWithValues),
                    },
                },
            };

            cpEntities.Add(cpEntity);
        }

        return cpEntities;
    }

    public static EnergyVecCompoundedDb? GetProduction(EIPCPEntityDataWithValues itemWithValues)
    {
        if (string.IsNullOrWhiteSpace(itemWithValues.Data.ProductionSystemId))
            return null;

        return new EnergyVecCompoundedDb
        {
            EnergyVecs =
            [
                new EnergyVecDb { Values = new LargeVecFloatDbRelation { Values = itemWithValues.ProductionValues } },
            ],
        };
    }

    public static EnergyVecCompoundedDb GetConsumption(EIPCPEntityDataWithValues itemWithValues)
    {
        var item = itemWithValues.Data;

        if (item.Has15MinutesConsumptionData)
        {
            if (itemWithValues.ConsumptionValues is not null && itemWithValues.ConsumptionValues.Count == 0)
                throw new Exception($"Consumption values are empty for entity {item.Id}");

            return new EnergyVecCompoundedDb
            {
                EnergyVecs =
                [
                    new EnergyVecDb
                    {
                        Values = new LargeVecFloatDbRelation { Values = itemWithValues.ConsumptionValues },
                    },
                ],
            };
        }

        var energyVec = new EnergyVecDb { AnnualValue = item.AnnualConsumptionVT + item.AnnualConsumptionNT };

        TddModuleWithItem? tdd = null;
        if (item.Description_.Contains("zs-a-ms"))
            tdd = TddModuleElectricityCSEnercoPreview.TddKindergarten;
        if (item.Description_.Contains("zakladni-skola"))
            tdd = TddModuleElectricityCSEnercoPreview.TddSchool1;
        if (item.Description_.Contains("materska-skola"))
            tdd = TddModuleElectricityCSEnercoPreview.TddKindergarten;
        if (item.Description_.Contains("cov"))
            tdd = TddModuleElectricityCSEnerco15.TddCov1;

        energyVec.Tdd = tdd?.ToDb();

        return new EnergyVecCompoundedDb { EnergyVecs = [energyVec] };
    }
}
