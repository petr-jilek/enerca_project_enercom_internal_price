using Enerca.EnerkomInternalPrice.Logic.Consts;
using Enerca.EnerkomInternalPrice.Logic.Models;
using Enerca.Logic.Modules.Compute.Db;
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
using Enerca.Logic.Modules.LargeVec.Db;
using Enerca.Logic.Modules.OTValue.Db.DataTypes;
using Enerca.Logic.Modules.Tdd.Db.Extensions;
using Enerca.Logic.Modules.Tdd.Db.Models;
using Enerca.Logic.Vendor.PVGIS.Models;
using Enerca.Logic.Vendor.PVGIS.Predefined;
using Fastdo.Common.Extensions;

namespace Enerca.EnerkomInternalPrice.Logic;

public class EIPPlotServiceHelper(EIPPlotSettings settings)
{
    public static void AddCommunityDynamicModel(ComputeModelDb db)
    {
        var model = new ExternalModelDb
        {
            Info = new ExternalModelInfoDb { CPEntityIds = [.. db.CPEntities.Select(x => x.InfoBasic.Id)] },
            Community = new ExternalModelCommunityDb
            {
                InternalPriceBuy = new OTValueFloatDb { Value = 0 },
                InternalPriceFee = new OTValueFloatDb { Value = 0 },
                Method = ExternalModelCommunitySharingMethodConsts.Dynamic,
            },
        };

        db.ExternalModels.Add(model);
    }

    public static void RemoveBgs(ComputeModelDb db)
    {
        var cpEntitiesToRemove = new List<CPEntityDb>();

        foreach (var cpEntity in db.CPEntities)
            if (cpEntity.InfoBasic.Tags.Contains(EIPPlotConsts.TagBg))
                cpEntitiesToRemove.Add(cpEntity);

        foreach (var cpEntity in cpEntitiesToRemove)
            RemoveCPEntity(db, cpEntity.InfoBasic.Id);
    }

    public static void RemoveCPEntities(ComputeModelDb db, Func<CPEntityDb, bool> predicate)
    {
        var cpEntitiesToRemove = db.CPEntities.Where(predicate).ToList();

        foreach (var cpEntity in cpEntitiesToRemove)
            db.CPEntities.Remove(cpEntity);
    }

    public static void RemoveCPEntity(ComputeModelDb db, string id)
    {
        var cpEntity =
            db.CPEntities.FirstOrDefault(x => x.InfoBasic.Id == id)
            ?? throw new Exception($"CPEntity with id {id} not found");

        db.CPEntities.Remove(cpEntity);

        foreach (var externalModel in db.ExternalModels)
            if (externalModel.Community is not null)
                externalModel.Info.CPEntityIds.Remove(id);
    }

    public void AddConsumption(ComputeModelDb db, float annualConsumption, TddModuleWithItem tdd)
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
                        VariableEnergy = settings.VariableEnergyPrice,
                        VariableNetworkCharges = settings.VariableNetworkChargesPrice,
                        VariableSell = settings.VariableSellPVPrice,
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
                externalModel.Info.CPEntityIds.Add(cpEntity.InfoBasic.Id);
    }

    public async Task AddPvAsync(
        ComputeModelDb db,
        float installedPower,
        float consumptionCoefficient,
        TddModuleWithItem tdd
    )
    {
        var productionValues =
            await EnercaPVGISPredefined.PVGIS.GetPvRelativeProduction(
                request: new EnercaPVGISHourlyRadiationRequest(
                    latitude: settings.Latitude,
                    longitude: settings.Longitude,
                    angle: settings.Angle,
                    aspect: settings.Aspect
                )
            ) ?? throw new Exception("Error getting relative production from PVGIS");

        productionValues = productionValues.ScalarMultiply(scalar: installedPower);

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
                        VariableEnergy = settings.VariableEnergyPrice,
                        VariableNetworkCharges = settings.VariableNetworkChargesPrice,
                        VariableSell = settings.VariableSellPVPrice,
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
                        EnergyVecs =
                        [
                            new EnergyVecDb
                            {
                                AnnualValue = 1000 * installedPower * consumptionCoefficient,
                                Tdd = tdd.ToDb(),
                            },
                        ],
                    },
                    Production = new EnergyVecCompoundedDb
                    {
                        EnergyVecs =
                        [
                            new EnergyVecDb { Values = new LargeVecFloatDbRelation { Values = [.. productionValues] } },
                        ],
                    },
                },
            },
        };

        db.CPEntities.Add(cpEntity);

        foreach (var externalModel in db.ExternalModels)
            if (externalModel.Community is not null)
                externalModel.Info.CPEntityIds.Add(cpEntity.InfoBasic.Id);
    }
}
