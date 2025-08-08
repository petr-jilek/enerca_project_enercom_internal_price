using Enerca.EnerkomInternalPrice.Logic.Helpers;
using Enerca.EnerkomInternalPrice.Logic.Models;
using Enerca.Logic.Modules.Compute.Db;
using Enerca.Logic.Modules.Compute.Mappers;
using Enerca.Logic.Modules.External.Db.Community;
using Enerca.Logic.Modules.OTValue.Db.DataTypes;
using Enerca.Python.Helpers;
using Enerca.Python.Models;
using Fastdo.Common.Extensions;
using Fastdo.Common.Modules.Files.Models;

namespace Enerca.EnerkomInternalPrice.Logic.Plot;

public class PlotM4InternalPriceService(EIPPlotSettings settings, EIPPlotInternalPriceState state)
{
    public async Task PlotAsync(ComputeModelDb db)
    {
        var db_ = db.Clone();
        var dbWithoutBg_ = db.Clone();

        EIPPlotServiceHelper.RemoveBgs(db: dbWithoutBg_);

        await PlotInternalPriceBuy(db: db_);
        await PlotInternalPriceFee(db: db_, dbWithoutBg: dbWithoutBg_);
    }

    private PathSettings GetPath(string fileName, string? dir = null) =>
        settings.GetPath(fileName: fileName, module: "M4InternalPrice", dir: dir);

    private async Task PlotInternalPriceBuy(ComputeModelDb db)
    {
        var priceValues = Enumerable.Range(0, 2).Select(x => (float)x).ToList();
        var npvValues = await GetNpvValuesAsync(
            db: db,
            priceValues: priceValues,
            pricePredicate: x => x.InternalPriceBuy
        );

        await PlotHelper.PlotAsync(
            data: new PlotData(
                path: GetPath("InternalPriceBuy"),
                ys: [npvValues],
                xs: [priceValues],
                title: "Závislost NPV na ceně interního výkupu",
                xLabel: "Interní cena výkupu [Kč/kWh]",
                yLabel: "NPV [Kč]",
                scientificNotationAxisY: false,
                xTicksFormatter: x => x.ToString().Replace(".", ","),
                yTicksFormatter: x => settings.Formatting.FormatCurrency(x.ToInt()),
                yMin: 0,
                yMax: npvValues.Max() * 1.1f
            ),
            plts: [PythonPlotHelper.Plot]
        );
    }

    private async Task PlotInternalPriceFee(ComputeModelDb db, ComputeModelDb dbWithoutBg)
    {
        var priceValues = Enumerable.Range(0, 6).Select(x => (float)x / 2).ToList();

        var npvValues = await GetNpvValuesAsync(
            db: db,
            priceValues: priceValues,
            pricePredicate: x => x.InternalPriceFee
        );

        var npvValuesWithoutBg = await GetNpvValuesAsync(
            db: dbWithoutBg,
            priceValues: priceValues,
            pricePredicate: x => x.InternalPriceFee
        );

        var xFirst = priceValues.First();
        var xLast = priceValues.Last();

        var npvFirst = npvValues.First();
        var npvLast = npvValues.Last();

        var npvFirstWithoutBg = npvValuesWithoutBg.First();
        var npvLastWithoutBg = npvValuesWithoutBg.Last();

        static float getRoot(float x0, float x1, float y0, float y1)
        {
            var slope = (y1 - y0) / (x1 - x0);

            return x0 - y0 / slope;
        }

        state.MinInternalPriceFee = getRoot(x0: xFirst, x1: xLast, y0: npvFirst, y1: npvLast);
        state.MinInternalPriceFeeWithoutBP = getRoot(
            x0: xFirst,
            x1: xLast,
            y0: npvFirstWithoutBg,
            y1: npvLastWithoutBg
        );

        await PlotHelper.PlotAsync(
            data: new PlotData(
                path: GetPath("InternalPriceFee"),
                ys: [npvValues, npvValuesWithoutBg],
                xs: [priceValues],
                title: "Závislost NPV na interním poplatku",
                xLabel: "Interní poplatek [Kč/kWh]",
                yLabel: "NPV [Kč]",
                legends: ["Všichni účastníci", "Bez BP"],
                scientificNotationAxisY: false,
                xTicksFormatter: x => x.ToString().Replace(".", ","),
                yTicksFormatter: x => settings.Formatting.FormatCurrency(x.ToInt()),
                yMin: -5_000_000,
                yMax: npvValues.Max() * 1.1f
            ),
            plts: [PythonPlotHelper.Plot]
        );
    }

    private async Task<List<float>> GetNpvValuesAsync(
        ComputeModelDb db,
        List<float> priceValues,
        Func<ExternalModelCommunityDb, OTValueFloatDb> pricePredicate
    )
    {
        var npvValues = new List<float>();

        foreach (var price in priceValues)
        {
            var db_ = db.Clone();

            foreach (var externalModel in db_.ExternalModels)
                if (externalModel.Community is not null)
                    pricePredicate(externalModel.Community).Value = price;

            var model = await db_.ToModelAsync();
            var result = model.Compute(settings.EvaluationYears);

            npvValues.Add(result.PresentValue);
        }

        return npvValues;
    }
}
