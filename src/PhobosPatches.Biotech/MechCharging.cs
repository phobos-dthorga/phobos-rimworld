using System.Collections.Generic;
using Phobos.Framework.Defs;
using Phobos.Framework.Rules;
using RimWorld;
using Verse;

namespace Phobos.BiotechPatches;

/// <summary>What a mech on a recharger is taking, worked out once for the tick, the inspect line and the notes.</summary>
public readonly struct ChargeFigures
{
    /// <summary>The share of the full rate the battery takes at its charge level (1 until it eases off).</summary>
    public readonly float Taper;
    /// <summary>Watts the recharger takes from the grid.</summary>
    public readonly float WallWatts;
    /// <summary>Watts of that lost as heat.</summary>
    public readonly float LostWatts;

    public ChargeFigures(float taper, float wallWatts, float lostWatts)
    {
        Taper = taper;
        WallWatts = wallWatts;
        LostWatts = lostWatts;
    }
}

/// <summary>
/// The one place that decides what a mech recharger draws (D'thorga Patches 0.28.0; docs/development/mech-recharging-real-power.md):
/// the sums are D'thorga Framework's BatteryChargeRule, the figures PhobosMechChargingRulesDef's, the size preset the player's. The
/// patch on the recharger calls in here and decides nothing itself.
/// <para><b>How it sits with power-saving mods</b> (owner, 20 Nov 2026): the draw is written through the game's own
/// CompPowerTrader.PowerOutput, as the game's heater, cooler and gene machines write theirs, and compared against the stored figure
/// (powerOutputInt), never the reported one. A mod that changes what a power comp <i>reports</i> (LightsOut scales the getter's
/// result, and holds an unused recharger on standby) then works on top of our figure, and neither undoes the other.</para>
/// </summary>
public static class MechCharging
{
    /// <summary>Game ticks between two pushes of waste heat: once a second, as the game's own heat pushers do.</summary>
    public const int HeatEveryTicks = 60;
    /// <summary>Game ticks between two looks at a recharger with no mech on it, to hold its standby draw.</summary>
    public const int IdleCheckTicks = 250;

    public static PhobosMechChargingRulesDef Rules => PhobosRulesDef.Current<PhobosMechChargingRulesDef>();

    public static bool On => BiotechPatchesMod.Settings.mechsDrawRealPower;

    /// <summary>What this mech takes at its present charge.</summary>
    public static ChargeFigures FiguresFor(Pawn mech, Need_MechEnergy energy) => FiguresAt(mech, energy, Fraction(energy));

    /// <summary>What this mech takes at a charge level (0 to 1).</summary>
    public static ChargeFigures FiguresAt(Pawn mech, Need_MechEnergy energy, float charge) =>
        FiguresAt(mech.BodySize, energy.MaxLevel, charge, BiotechPatchesMod.Settings.batteryScale);

    /// <summary>What a mech of a body size and maximum energy takes at a charge level (0 to 1), at a battery size preset. The one place
    /// the figures are worked out: the recharger, its inspect text, the mech's information card and the settings text all come here.</summary>
    public static ChargeFigures FiguresAt(float bodySize, float maxEnergy, float charge, float batteryScale)
    {
        PhobosMechChargingRulesDef rules = Rules;
        double taper = BatteryChargeRule.Taper(charge, rules.easesFrom, rules.rateWhenFull);
        double stored = BatteryChargeRule.StoredWatts(CapacityWattDays(bodySize, maxEnergy, batteryScale), FractionPerDay(maxEnergy), taper);
        return new ChargeFigures((float)taper, (float)BatteryChargeRule.WallWatts(stored, rules.efficiency),
            (float)BatteryChargeRule.LostWatts(stored, rules.efficiency));
    }

    /// <summary>What a mech's battery holds, in watt-days.</summary>
    public static float CapacityWattDays(float bodySize, float maxEnergy, float batteryScale)
    {
        PhobosMechChargingRulesDef rules = Rules;
        return (float)BatteryChargeRule.CapacityWattDays(bodySize, rules.wattDaysPerBodySize * batteryScale,
            rules.standardMaxEnergy > 0f ? maxEnergy / rules.standardMaxEnergy : 1f);
    }

    /// <summary>Days to charge a mech's battery from empty to full at the full rate, with the taper; endless when it never would.</summary>
    public static double DaysEmptyToFull(float maxEnergy)
    {
        PhobosMechChargingRulesDef rules = Rules;
        return BatteryChargeRule.DaysToReach(0, 1, FractionPerDay(maxEnergy), rules.easesFrom, rules.rateWhenFull);
    }

    /// <summary>What a mech of a body size draws at the full rate, with a standard battery at a size preset: for settings text, where
    /// no mech is to hand.</summary>
    public static float FullRateWatts(float bodySize, float batteryScale) => FiguresAt(bodySize, Rules.standardMaxEnergy, 0f, batteryScale).WallWatts;

    /// <summary>The share of a battery the game's recharger adds in a day at full rate: its own 50 units a day over the mech's maximum.</summary>
    public static float FractionPerDay(float maxEnergy) => maxEnergy > 0f ? Building_MechCharger.ChargePerDay / maxEnergy : 0f;

    /// <summary>What the game's recharger adds to a mech's energy in one tick at the full rate: its 50 units a day over a day's ticks.</summary>
    public static float FullStepPerTick => Building_MechCharger.ChargePerDay / GenDate.TicksPerDay;

    public static float Fraction(Need_MechEnergy energy)
    {
        float max = energy.MaxLevel;
        return max > 0f ? energy.CurLevel / max : 1f;
    }

    /// <summary>Game ticks until this mech reaches the level its control group charges it to, with the taper; -1 when it never would.</summary>
    public static int TicksToTarget(Pawn mech, Need_MechEnergy energy)
    {
        PhobosMechChargingRulesDef rules = Rules;
        float max = energy.MaxLevel;
        if (max <= 0f) return -1;
        float target = UnityEngine.Mathf.Min(JobGiver_GetEnergy.GetMaxRechargeLimit(mech), max) / max;
        double days = BatteryChargeRule.DaysToReach(Fraction(energy), target, FractionPerDay(max), rules.easesFrom, rules.rateWhenFull);
        return double.IsInfinity(days) ? -1 : (int)(days * GenDate.TicksPerDay);
    }

    /// <summary>What a recharger draws with no mech on it: its Def's own idle figure where it has one, else a share of its listed power.</summary>
    public static float IdleWatts(CompPowerTrader power)
    {
        CompProperties_Power props = power.Props;
        if (!UnityEngine.Mathf.Approximately(props.idlePowerDraw, -1f)) return props.idlePowerDraw;
        return props.PowerConsumption * Rules.idleShare;
    }

    /// <summary>Writes a draw in watts as the comp's stored figure, only when it differs.</summary>
    public static void SetDraw(CompPowerTrader power, float watts)
    {
        float output = -watts;
        if (power.powerOutputInt != output) power.PowerOutput = output;
    }

    /// <summary>Warms the room a working recharger stands in with the power it loses, unless the room is already hotter than the limit.</summary>
    public static bool PushWasteHeat(Building_MechCharger charger, float lostWatts)
    {
        PhobosMechChargingRulesDef rules = Rules;
        if (lostWatts <= 0f || charger.AmbientTemperature >= rules.heatUpToCelsius) return false;
        GenTemperature.PushHeat(charger, lostWatts * rules.heatPerSecondPerWatt);
        return true;
    }

    /// <summary>
    /// Puts every recharger on every map back to the figure the game gives it, through the game's own SetUpPowerVars. Called when the
    /// setting changes, so turning it off leaves nothing of ours behind, and turning it on starts from the game's figures.
    /// </summary>
    public static void ResetAllRechargers()
    {
        if (Current.ProgramState != ProgramState.Playing || Find.Maps == null) return;
        foreach (Map map in Find.Maps)
        {
            List<Thing> buildings = map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial);
            for (int i = 0; i < buildings.Count; i++)
                if (buildings[i] is Building_MechCharger charger) charger.Power?.SetUpPowerVars();
        }
    }
}
