using Phobos.WaterTap.Rules;
using Verse;

namespace Phobos.WaterTap;

/// <summary>
/// The colony limit for one map: its taps stop filling once this many bottles lie on the map. It lives here, not on
/// each tap, because it is one number for the whole map. The game adds this to older saves by itself. See
/// <see cref="TapService"/> for what is counted.
/// </summary>
public class MapComponent_WaterTapStock : MapComponent
{
    /// <summary>Saved key; never rename once saves can hold it.</summary>
    private const string StockLimitSaveKey = "PhobosWaterTap_StockLimit";

    private int stockLimit = TapRule.DefaultStockLimit;

    public MapComponent_WaterTapStock(Map map) : base(map)
    {
    }

    public int StockLimit
    {
        get => stockLimit;
        internal set => stockLimit = value;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref stockLimit, StockLimitSaveKey, TapRule.DefaultStockLimit);
    }
}
