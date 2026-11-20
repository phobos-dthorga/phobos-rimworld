using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Phobos.BiotechPatches;

/// <summary>
/// Mech rechargers draw power by the mech on them (D'thorga Patches 0.28.0, off by default; the owner's request of 20 Nov 2026,
/// docs/development/mech-recharging-real-power.md). The game's recharger draws its listed power whoever stands on it, and fills every
/// mech at one speed. These two patches on the recharger's own tick change three things while the setting is on, and nothing while it
/// is off (one boolean read):
/// <list type="bullet">
/// <item>the recharger's draw follows the mech's battery (MechCharging.FiguresAt), written the way the game's heater writes its own;</item>
/// <item>the charge the game added this tick, and the waste it made, are scaled by the taper, so the last of a charge comes slowly;</item>
/// <item>the power the recharger loses warms its room once a second.</item>
/// </list>
/// A prefix and a postfix, because the game adds the charge inside its tick with a fixed figure: the prefix notes the level before,
/// the postfix scales what was added. Neither skips the original. Decisions live in MechCharging; this class only applies them.
/// </summary>
[HarmonyPatch(typeof(Building_MechCharger), "Tick")]
public static class RechargerRealPowerPatch
{
    /// <summary>What the prefix saw: the mech on the recharger, its energy and the recharger's waste, before the game's tick.</summary>
    public struct Before
    {
        public Pawn? Mech;
        public float Level;
        public float Waste;
    }

    public static void Prefix(Building_MechCharger __instance, float ___wasteProduced, out Before __state)
    {
        __state = default;
        if (!MechCharging.On) return;
        Pawn? mech = __instance.CurrentlyChargingMech;
        Need_MechEnergy? energy = mech?.needs?.energy;
        if (energy == null) return;
        __state = new Before { Mech = mech, Level = energy.CurLevel, Waste = ___wasteProduced };
    }

    public static void Postfix(Building_MechCharger __instance, ref float ___wasteProduced, Before __state)
    {
        if (!MechCharging.On) return;
        Pawn? mech = __instance.CurrentlyChargingMech;
        if (mech == null)
        {
            // No mech: hold the standby draw, at once when one has just left and at a slow look otherwise (the game puts its listed
            // figure back when the recharger is spawned, rewired or a research project finishes).
            if (__state.Mech != null || __instance.IsHashIntervalTick(MechCharging.IdleCheckTicks)) HoldStandby(__instance);
            return;
        }
        Need_MechEnergy? energy = mech.needs?.energy;
        CompPowerTrader? power = __instance.Power;
        if (energy == null || power == null) return;

        bool sameMech = __state.Mech == mech;
        float max = energy.MaxLevel;
        // The taper goes by the level before this tick's charge, so the scaling below and the draw agree.
        float charge = sameMech && max > 0f ? __state.Level / max : MechCharging.Fraction(energy);
        ChargeFigures figures = MechCharging.FiguresAt(mech, energy, charge);
        if (sameMech && figures.Taper < 1f)
        {
            float gained = energy.CurLevel - __state.Level;
            if (gained > 0f) energy.CurLevel = __state.Level + gained * figures.Taper;
            // Waste follows the energy put in. A fall means a wastepack was just made and the count restarted: leave it.
            float waste = ___wasteProduced - __state.Waste;
            if (waste > 0f) ___wasteProduced = __state.Waste + waste * figures.Taper;
        }
        MechCharging.SetDraw(power, figures.WallWatts);
        if (!power.PowerOn) return;

        bool warmed = __instance.IsHashIntervalTick(MechCharging.HeatEveryTicks) && MechCharging.PushWasteHeat(__instance, figures.LostWatts);
        if (Seen.Wants("patches.mech-power.draw", __instance))
            Seen.Note("patches.mech-power.draw", __instance, mech.LabelShort + " (body size " + mech.BodySize.ToString("0.##") + ") takes "
                + figures.WallWatts.ToString("0") + " W where the recharger lists " + power.Props.PowerConsumption.ToString("0") + " W",
                new List<KeyValuePair<string, double>> { new("watts", figures.WallWatts), new("listed", power.Props.PowerConsumption), new("bodySize", mech.BodySize) });
        if (figures.Taper < 1f && Seen.Wants("patches.mech-power.taper", __instance))
            Seen.Note("patches.mech-power.taper", __instance, mech.LabelShort + " at " + charge.ToStringPercent() + " charges at "
                + figures.Taper.ToStringPercent() + " of the full rate, taking " + figures.WallWatts.ToString("0") + " W",
                new List<KeyValuePair<string, double>> { new("rate", figures.Taper), new("charge", charge), new("watts", figures.WallWatts) });
        if (warmed && Seen.Wants("patches.mech-power.heat", __instance))
            Seen.Note("patches.mech-power.heat", __instance, figures.LostWatts.ToString("0") + " W lost as heat into a room at "
                + __instance.AmbientTemperature.ToString("0.#") + " C",
                new List<KeyValuePair<string, double>> { new("heat", figures.LostWatts), new("room", __instance.AmbientTemperature) });
    }

    static void HoldStandby(Building_MechCharger charger)
    {
        CompPowerTrader? power = charger.Power;
        if (power == null) return;
        float watts = MechCharging.IdleWatts(power);
        MechCharging.SetDraw(power, watts);
        if (Seen.Wants("patches.mech-power.standby", charger))
            Seen.Note("patches.mech-power.standby", charger, "with no mech on it, set to " + watts.ToString("0.#") + " W where it lists "
                + power.Props.PowerConsumption.ToString("0") + " W",
                new List<KeyValuePair<string, double>> { new("watts", watts), new("listed", power.Props.PowerConsumption) });
    }
}

/// <summary>
/// The recharger's inspect text says what the mech on it is taking and how long it has to go, or what it waits for (D'thorga Patches
/// 0.28.0), while mech rechargers draw real power. Presentation only: it reads MechCharging's figures and changes nothing.
/// </summary>
[HarmonyPatch(typeof(Building_MechCharger), nameof(Building_MechCharger.GetInspectString))]
public static class RechargerInspectPatch
{
    public static void Postfix(Building_MechCharger __instance, ref string __result)
    {
        if (!MechCharging.On) return;
        Pawn? mech = __instance.CurrentlyChargingMech;
        Need_MechEnergy? energy = mech?.needs?.energy;
        if (mech == null || energy == null) return;
        ChargeFigures figures = MechCharging.FiguresFor(mech, energy);
        NamedArgument who = mech.LabelShortCap.Named("MECH"), watts = figures.WallWatts.ToString("F0").Named("WATTS");
        string line;
        if (!__instance.IsPowered)
        {
            line = "PhobosBiotechPatches_WaitingForPower".Translate(who, watts);
        }
        else
        {
            int ticks = MechCharging.TicksToTarget(mech, energy);
            if (ticks <= 0) line = "PhobosBiotechPatches_ChargingLineNoTime".Translate(who, watts);
            else if (figures.Taper < 1f) line = "PhobosBiotechPatches_ChargingEasingLine".Translate(who, watts, ticks.ToStringTicksToPeriod().Named("TIME"));
            else line = "PhobosBiotechPatches_ChargingLine".Translate(who, watts, ticks.ToStringTicksToPeriod().Named("TIME"));
        }
        __result = string.IsNullOrEmpty(__result) ? line : __result + "\n" + line;
    }
}
