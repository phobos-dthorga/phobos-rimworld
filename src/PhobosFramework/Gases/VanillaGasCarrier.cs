using Phobos.Framework.Rules;
using Verse;

namespace Phobos.Framework.Gases;

/// <summary>
/// The game's own gas grid: four gases, 0 to 255 per cell, spreading through open doors, blocked by walls, fading slower
/// under a roof (Verse.GasGrid, read 4 Nov 2026). Tox gas needs Biotech and deadlife dust Anomaly; the game logs an error
/// when asked for them without the expansion, so the carrier refuses first.
/// </summary>
public sealed class VanillaGasCarrier : IGasCarrier
{
    public string Name => "the game's gas grid";

    public float Density(IntVec3 cell, Map map, GasKind kind)
    {
        if (!TryGasType(kind, out GasType type)) return 0f;
        return cell.GasDensity(map, type) / 255f;
    }

    public bool CanWrite(GasKind kind)
    {
        switch (kind)
        {
            case GasKind.Smoke: return true;
            case GasKind.Toxic: return ModsConfig.BiotechActive;
            default: return false;
        }
    }

    public void Add(IntVec3 cell, Map map, GasKind kind, float fraction)
    {
        if (!CanWrite(kind) || !TryGasType(kind, out GasType type)) return;
        int units = FireSmokeRule.ToGridUnits(fraction);
        if (units > 0) GasUtility.AddGas(cell, map, type, units);
    }

    static bool TryGasType(GasKind kind, out GasType type)
    {
        switch (kind)
        {
            case GasKind.Smoke: type = GasType.BlindSmoke; return true;
            case GasKind.Toxic: type = GasType.ToxGas; return ModsConfig.BiotechActive;
            case GasKind.RotStink: type = GasType.RotStink; return true;
            case GasKind.DeadlifeDust: type = GasType.DeadlifeDust; return ModsConfig.AnomalyActive;
            default: type = GasType.BlindSmoke; return false;
        }
    }
}
