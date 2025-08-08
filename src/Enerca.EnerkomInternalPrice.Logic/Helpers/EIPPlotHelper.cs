using Enerca.EnerkomInternalPrice.Logic.Consts;
using Enerca.Logic.Modules.Compute.Db;
using Enerca.Logic.Modules.CPEntity.Abstractions;
using Enerca.Logic.Modules.CPEntity.Db;
using Enerca.Logic.Modules.CPEntity.Db.Models;
using Enerca.Logic.Modules.Energy.Db;
using Enerca.Logic.Modules.EnergyTariff.Db;
using Enerca.Logic.Modules.EnergyTariff.Db.Electricity;
using Enerca.Logic.Modules.EnergyTariff.Db.Electricity.Implementations;
using Enerca.Logic.Modules.EnergyTariffInfo.Db;
using Enerca.Logic.Modules.EnergyTariffInfo.Implementations.Consts.Electricity;
using Enerca.Logic.Modules.External.Db;
using Enerca.Logic.Modules.External.Db.Community;
using Enerca.Logic.Modules.OTValue.Db.DataTypes;
using Enerca.Logic.Modules.System.Db;
using Enerca.Logic.Modules.System.Db.SystemCP;
using Enerca.Logic.Modules.System.Db.SystemCP.Pv;
using Enerca.Logic.Modules.Tdd.Db.Extensions;
using Enerca.Logic.Modules.Tdd.Db.Models;

namespace Enerca.EnerkomInternalPrice.Logic.Helpers;

public static class EIPPlotHelper
{
    public static void AddCommunityDynamicModel(ComputeModelDb db)
    {
        var model = new ExternalModelDb
        {
            Common = new ExternalModelCommonDb { CPEntityIds = [.. db.CPEntities.Select(x => x.InfoBasic.Id)] },
            Community = new ExternalModelCommunityDb
            {
                InternalPriceBuy = new OTValueFloatDb { Value = 0 },
                InternalPriceFee = new OTValueFloatDb { Value = 0 },
                // TODO: consts
                Method = 3,
            },
        };

        db.ExternalModels.Add(model);
    }

    public static void RemoveBgs(ComputeModelDb db)
    {
        var cpEntitiesToRemove = new List<ICPEntityChangingAnnually>();

        foreach (var cpEntity in db.CPEntities)
            if (cpEntity.InfoBasic.Tags.Contains(EIPPlotConsts.TagBg))
                db.CPEntities.Remove(cpEntity);

        var cpEntitiesToRemoveIds = db.CPEntities.Select(x => x.InfoBasic.Id).ToList();

        foreach (var externalModel in db.ExternalModels)
        {
            externalModel.Common.CPEntityIds =
            [
                .. externalModel.Common.CPEntityIds.Where(id => cpEntitiesToRemoveIds.Contains(id) == false),
            ];
        }
    }

    public static void AddConsumption(ComputeModelDb db, TddModuleWithItem tdd, float annualConsumption)
    {
        var cpEntity = new CPEntityDb
        {
            InfoBasic = new InfoBasicDb { Id = "consumption_virutal", Name = "Consumption Virtual" },
            InfoExternal = new(),
            EnergyTariffs = new EnergyTariffsDb
            {
                Electricity = new EnergyTariffElectricityDb
                {
                    Info = new EnergyTariffElectricityInfoDb
                    {
                        Tariff = EnergyTariffElectricityFixedTariffConsts.D01d,
                        Provider = EnergyTariffElectricityFixedProviderConsts.EGD,
                        PhaseCount = 3,
                        Current = 25,
                        Voltage = 230,
                        EanC = "consumption_virutal",
                        EanP = null,
                    },

                    Fixed = new EnergyTariffElectricityFixedDb
                    {
                        VariableEnergy = 3,
                        VariableNetworkCharges = 3,
                        VariableSell = 0.5f,
                        Fixed = 0,
                    },
                },
            },
            EnergyState = new EnergyStateDb
            {
                Electricity = new EnergyCPDb
                {
                    Consumption = new EnergyVecCompoundedDb
                    {
                        EnergyVecs = [new EnergyVecDb { AnnualValue = annualConsumption, Tdd = tdd.ToDb() }],
                    },
                },
            },
        };

        db.CPEntities.Add(cpEntity);

        foreach (var externalModel in db.ExternalModels)
            if (externalModel.Community is not null)
                externalModel.Common.CPEntityIds.Add(cpEntity.InfoBasic.Id);
    }

    public static void AddPv(ComputeModelDb db, float installedPower, float consumptionCoefficient)
    {
        var cpEntity = new CPEntityDb
        {
            InfoBasic = new InfoBasicDb { Id = "pv_virtual", Name = "PV Virtual" },
            InfoExternal = new(),
            EnergyTariffs = new EnergyTariffsDb
            {
                Electricity = new EnergyTariffElectricityDb
                {
                    Info = new EnergyTariffElectricityInfoDb
                    {
                        Tariff = EnergyTariffElectricityFixedTariffConsts.D01d,
                        Provider = EnergyTariffElectricityFixedProviderConsts.EGD,
                        PhaseCount = 3,
                        Current = 25,
                        Voltage = 230,
                        EanC = "pv_virtual_consumption",
                        EanP = "pv_virtual_production",
                    },

                    Fixed = new EnergyTariffElectricityFixedDb
                    {
                        VariableEnergy = 3,
                        VariableNetworkCharges = 3,
                        VariableSell = 0.5f,
                        Fixed = 0,
                    },
                },
            },
            EnergyState = new EnergyStateDb
            {
                Electricity = new EnergyCPDb
                {
                    Consumption = new EnergyVecCompoundedDb
                    {
                        EnergyVecs = [new EnergyVecDb { AnnualValue = 1000 * installedPower * consumptionCoefficient }],
                    },
                },
            },
            Systems =
            [
                new SystemDb
                {
                    SystemCP = new SystemCPSystemDb
                    {
                        Pv = new PvSystemDb
                        {
                            Entities =
                            [
                                new PvSystemEntityDb
                                {
                                    Angle = 35,
                                    Aspect = 0,
                                    InstalledPower = new OTValueFloatDb { Value = installedPower },
                                },
                            ],
                            Latitude = 14.5595834f,
                            Longitude = 48.9431537f,
                        },
                    },
                },
            ],
        };

        db.CPEntities.Add(cpEntity);

        foreach (var externalModel in db.ExternalModels)
            if (externalModel.Community is not null)
                externalModel.Common.CPEntityIds.Add(cpEntity.InfoBasic.Id);
    }
}
