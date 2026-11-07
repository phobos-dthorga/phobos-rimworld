using PipeSystem;
using Verse;

namespace Phobos.Medical.Comps;

/// <summary>
/// Something a pipe-network producer needs besides power and its switch, such as water for the electrolyser. A comp that implements this
/// sits on the same building; <see cref="CompPhobosGatedResourceTrader"/> asks every one before producing. The answer is read every
/// pipe-network pass, so it must be a cached flag, worked out on the comp's own tick.
/// </summary>
public interface IPhobosProductionGate
{
    bool AllowsProduction { get; }
}

/// <summary>
/// Vanilla Expanded Framework's resource trader that also waits for its building's gates (the oxygen electrolyser waits for water when
/// Dubs Bad Hygiene is active). With no gate on the building it behaves exactly as the plain trader. While a gate is shut it drops to
/// its low-power idle draw. Both are the trader's own virtual hooks, so no patch is needed.
/// </summary>
public class CompPhobosGatedResourceTrader : CompResourceTrader
{
    bool GatesOpen
    {
        get
        {
            var comps = parent.AllComps;
            for (int i = 0; i < comps.Count; i++)
            {
                if (comps[i] is IPhobosProductionGate gate && !gate.AllowsProduction) return false;
            }
            return true;
        }
    }

    public override bool CanBeOn() => base.CanBeOn() && GatesOpen;

    public override bool ShouldBeLowPowerMode => base.ShouldBeLowPowerMode || !GatesOpen;
}
