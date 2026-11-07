using Phobos.Medical.Rules;
using Phobos.Medical.Services;
using RimWorld;
using Verse;

namespace Phobos.Medical.Comps;

/// <summary>A carbon monoxide and smoke alarm. Other mods can add alarms with this in XML.</summary>
public class CompProperties_PhobosGasAlarm : CompProperties
{
    public CompProperties_PhobosGasAlarm() => compClass = typeof(CompPhobosGasAlarm);
}

public class CompPhobosGasAlarm : ThingComp
{
    /// <summary>What it sounds for. Derived from the air, so never saved: after a load it warns again if the air is still bad.</summary>
    AlarmGases sounding;

    public override void CompTickRare()
    {
        base.CompTickRare();
        sounding = GasAlarmService.Sense(this, sounding);
    }

    public override string CompInspectStringExtra()
    {
        if (!GasAlarmService.Working(this))
        {
            CompPowerTrader? power = parent.TryGetComp<CompPowerTrader>();
            return (!FlickUtility.WantsToBeOn(parent) ? "PhobosMedical_AlarmSwitchedOff" : power != null && !power.PowerOn ? "PhobosMedical_StatusNoPower" : "PhobosMedical_AlarmSwitchedOff").Translate().Resolve();
        }
        return (sounding switch
        {
            AlarmGases.None => "PhobosMedical_AlarmQuiet",
            AlarmGases.CarbonMonoxide => "PhobosMedical_AlarmSoundingCarbonMonoxide",
            AlarmGases.Smoke => "PhobosMedical_AlarmSoundingSmoke",
            _ => "PhobosMedical_AlarmSoundingBoth",
        }).Translate().Resolve();
    }
}
