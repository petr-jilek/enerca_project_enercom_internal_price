using Enerca.EnerkomInternalPrice.Logic.Models;
using Enerca.EnerkomInternalPrice.Logic.Plot;
using Enerca.Logic.Modules.Compute.Db;
using Enerca.Logic.Modules.Compute.Mappers;

namespace Enerca.EnerkomInternalPrice.Logic;

public class EIPPlotService(EIPPlotSettings settings)
{
    public async Task PlotAsync(ComputeModelDb db)
    {
        var db_ = db.Clone();

        EIPPlotServiceHelper.AddCommunityDynamicModel(db: db_);

        var state = new EIPPlotInternalPriceState();

        var m1 = new PlotM1CurrentStateTechnicalService(settings: settings);
        var m2 = new PlotM2CurrentStateEconomyService(settings: settings);
        var m3 = new PlotM3LaddersService(settings: settings);
        var m4 = new PlotM4InternalPriceService(settings: settings, state: state);
        var m5 = new PlotM5LCOEService(settings: settings, state: state);
        var m6 = new PlotM6PotentialService(settings: settings, state: state);
        var m7 = new PlotM7CaseService(settings: settings);

        var model = await db_.ToModelAsync();
        var model0 = model.GetForYear(settings.ComputeYear);

        // await m1.PlotAsync(model: model0);
        // await m2.PlotAsync(model: model0);
        // await m3.PlotAsync(model: model0);
        await m4.PlotAsync(db: db_);
        // await m5.PlotAsync(db: db_);
        // await m6.PlotAsync(db: db_);
        // await m7.PlotAsync(db: db_);
    }
}
