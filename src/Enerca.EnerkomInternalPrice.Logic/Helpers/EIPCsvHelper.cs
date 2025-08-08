using Enerca.EnerkomInternalPrice.Logic.Models;
using Fastdo.Common.Extensions;
using Fastdo.Common.Modules.Files.Helpers;
using Fastdo.Common.Modules.Files.Models;

namespace Enerca.EnerkomInternalPrice.Logic.Helpers;

public static class EIPCsvHelper
{
    public static async Task<List<EIPCPEntityDataWithValues>> GetDataWithValuesFromCsvAsync(
        EIPPathSettings pathSettings
    )
    {
        var data = await GetDataFromCsvAsync(path: pathSettings.PathFileData);

        var dataWithValuesList = new List<EIPCPEntityDataWithValues>();

        foreach (var item in data)
        {
            var dataWithValues = new EIPCPEntityDataWithValues { Data = item };

            if (item.Has15MinutesConsumptionData)
                dataWithValues.ConsumptionValues = await GetConsumptionValuesFromCsvAsync(
                    path: pathSettings.PathFileDataConsumptions,
                    id: item.Id
                );
            if (item.ProductionSystemId is not null)
                dataWithValues.ProductionValues = await GetProductionValuesFromCsvAsync(
                    path: pathSettings.PathFileDataProductions,
                    id: item.ProductionSystemId
                );

            dataWithValuesList.Add(dataWithValues);
        }

        return dataWithValuesList;
    }

    public static async Task<List<float>> GetProductionValuesFromCsvAsync(PathSettings path, string id)
    {
        using var reader = new StreamReader(path.Path);

        var keyIndexDictionary = CsvHelper.GetKeyIndexDictionary(
            await reader.ReadLineAsync() ?? throw new InvalidOperationException("Cannot read header line.")
        );

        if (keyIndexDictionary.TryGetValue(id, out var index) == false)
            throw new InvalidOperationException($"Id {id} not found in {path.Path}");

        await reader.ReadLineAsync();
        await reader.ReadLineAsync();

        var values = new List<float>();

        while (await reader.ReadLineAsync() is { } line)
            values.Add(float.Parse(CsvHelper.ParseCsvLine(line)[index]));

        return values;
    }

    public static async Task<List<float>> GetConsumptionValuesFromCsvAsync(PathSettings path, string id)
    {
        using var reader = new StreamReader(path.Path);

        var keyIndexDictionary = CsvHelper.GetKeyIndexDictionary(
            await reader.ReadLineAsync() ?? throw new InvalidOperationException("Cannot read header line.")
        );

        if (keyIndexDictionary.TryGetValue(id, out var index) == false)
            throw new InvalidOperationException($"Id {id} not found in {path.Path}");

        await reader.ReadLineAsync();
        await reader.ReadLineAsync();

        var values = new List<float>();

        while (await reader.ReadLineAsync() is { } line)
            values.Add(float.Parse(CsvHelper.ParseCsvLine(line)[index]));

        return values;
    }

    public static async Task<List<EIPCPEntityData>> GetDataFromCsvAsync(PathSettings path)
    {
        using var reader = new StreamReader(path.Path);

        await reader.ReadLineAsync();
        await reader.ReadLineAsync();

        var data = new List<EIPCPEntityData>();

        while (await reader.ReadLineAsync() is { } line)
            data.Add(GetDataFromRow([.. CsvHelper.ParseCsvLine(line)]));

        return data;
    }

    private static EIPCPEntityData GetDataFromRow(List<string> row)
    {
        var id = row[0];
        var name = row[1];
        var description = row[2];
        var address = row[3];
        var provider = "EGD";
        var tariff = row[6];
        var phaseCount = int.Parse(row[7]);
        var current = int.Parse(row[8]);
        var voltage = 230;
        var eanC = row[9];

        var annualConsumptionVT = float.Parse(row[14]) * 1000;
        var annualConsumptionNT = float.Parse(row[15]) * 1000;
        var has15MinutesConsumptionData = row[16].ToNormalized() == "1";

        var variableEnergyPriceVT = float.Parse(row[18]) / 1000;
        var variableEnergyPriceNT = float.Parse(row[19]) / 1000;
        var fixedPrice = float.Parse(row[20]);
        var variableDistributionPriceVT = float.Parse(row[21]) / 1000;
        var variableDistributionPriceNT = float.Parse(row[22]) / 1000;
        var systemServicePrice = float.Parse(row[23]) / 1000;
        var ozePrice = float.Parse(row[24]);
        var infrastructurePrice = float.Parse(row[25]);
        var circuitBreakerPrice = float.Parse(row[26]);

        var productionSystemId = row[28];
        var productionType = row[29];
        var eanP = row[30];
        var electricityInstalledPowerString = row[32];
        var hasElectricityInstalledPower = float.TryParse(
            electricityInstalledPowerString,
            out var electricityInstalledPower
        );

        var variableSellPriceString = row[35];
        var greenBonusString = row[36];
        var greenBonusYearToString = row[37];

        var hasVariableSellPrice = float.TryParse(variableSellPriceString, out var variableSellPrice);
        var hasGreenBonus = float.TryParse(greenBonusString, out var greenBonus);
        var hasGreenBonusYearTo = int.TryParse(greenBonusYearToString, out var greenBonusYearTo);

        variableSellPrice /= 1000;
        greenBonus /= 1000;

        return new EIPCPEntityData
        {
            Id = id,
            Name = name,
            Description = description,
            Address = address,
            Provider = provider,
            Tariff = tariff,
            PhaseCount = phaseCount,
            Current = current,
            Voltage = voltage,
            EanC = eanC,

            AnnualConsumptionVT = annualConsumptionVT,
            AnnualConsumptionNT = annualConsumptionNT,
            Has15MinutesConsumptionData = has15MinutesConsumptionData,

            VariableEnergyPriceVT = variableEnergyPriceVT,
            VariableEnergyPriceNT = variableEnergyPriceNT,
            FixedPrice = fixedPrice,
            VariableDistributionPriceVT = variableDistributionPriceVT,
            VariableDistributionPriceNT = variableDistributionPriceNT,
            SystemServicePrice = systemServicePrice,
            OzePrice = ozePrice,
            InfrastructurePrice = infrastructurePrice,
            CircuitBreakerPrice = circuitBreakerPrice,

            ProductionSystemId = string.IsNullOrWhiteSpace(productionSystemId) ? null : productionSystemId,
            EanP = string.IsNullOrWhiteSpace(eanP) ? null : eanP,
            ProductionType = string.IsNullOrWhiteSpace(productionType) ? null : productionType,
            ElectricityInstalledPower = hasElectricityInstalledPower ? electricityInstalledPower : null,
            SellPrice = hasVariableSellPrice ? variableSellPrice : null,
            GreenBonus = hasGreenBonus ? greenBonus : null,
            GreenBonusYearTo = hasGreenBonusYearTo ? greenBonusYearTo : null,
        };
    }
}
