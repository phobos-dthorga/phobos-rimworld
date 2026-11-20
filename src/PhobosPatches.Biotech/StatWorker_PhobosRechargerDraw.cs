using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Phobos.BiotechPatches;

/// <summary>
/// A mech's information card shows what its recharger draws while it charges (D'thorga Patches 0.28.0, the owner's request of 20 Nov
/// 2026), shown only while mech rechargers draw real power. Works for a living mech (its own body size and maximum energy, which other
/// mods may change) and for a mech's kind with no mech to hand (a gestator's recipe, the research tree). Presentation only: every figure
/// comes from MechCharging, as the recharger's own does. The StatDef is PhobosMechRechargerDraw, in Mods/Biotech's Defs.
/// </summary>
public class StatWorker_PhobosRechargerDraw : StatWorker
{
    public override bool ShouldShowFor(StatRequest req) =>
        MechCharging.On && req.Def is ThingDef { race.IsMechanoid: true } && base.ShouldShowFor(req);

    public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
    {
        if (!TryReadMech(req, out float bodySize, out float maxEnergy)) return 0f;
        float watts = MechCharging.FiguresAt(bodySize, maxEnergy, 0f, BiotechPatchesMod.Settings.batteryScale).WallWatts;
        if (req.Thing is Pawn pawn && Seen.Wants("patches.mech-power.card", pawn))
            Seen.Note("patches.mech-power.card", pawn, "information card shows " + watts.ToString("0") + " W at body size " + bodySize.ToString("0.##"),
                new List<KeyValuePair<string, double>> { new("watts", watts), new("bodySize", bodySize) });
        return watts;
    }

    public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
    {
        if (!TryReadMech(req, out float bodySize, out float maxEnergy)) return "";
        PhobosMechChargingRulesDef rules = MechCharging.Rules;
        float scale = BiotechPatchesMod.Settings.batteryScale;
        ChargeFigures full = MechCharging.FiguresAt(bodySize, maxEnergy, 0f, scale);
        ChargeFigures atTop = MechCharging.FiguresAt(bodySize, maxEnergy, 1f, scale);
        double days = MechCharging.DaysEmptyToFull(maxEnergy);
        return "PhobosBiotechPatches_CardExplanation".Translate(
            MechCharging.CapacityWattDays(bodySize, maxEnergy, scale).ToString("F0").Named("CAPACITY"),
            bodySize.ToString("0.##").Named("BODYSIZE"),
            MechCharging.FractionPerDay(maxEnergy).ToStringPercent().Named("SHARE"),
            (full.WallWatts - full.LostWatts).ToString("F0").Named("STORED"),
            full.WallWatts.ToString("F0").Named("WATTS"),
            full.LostWatts.ToString("F0").Named("HEAT"),
            rules.easesFrom.ToStringPercent().Named("EASES"),
            atTop.WallWatts.ToString("F0").Named("ATFULL"),
            (double.IsInfinity(days) ? "-" : days.ToString("0.#")).Named("DAYS"));
    }

    /// <summary>The body size and maximum energy behind a request: a living mech's own, else its kind's.</summary>
    static bool TryReadMech(StatRequest req, out float bodySize, out float maxEnergy)
    {
        if (req.Thing is Pawn pawn && pawn.RaceProps.IsMechanoid)
        {
            bodySize = pawn.BodySize;
            maxEnergy = pawn.needs?.energy?.MaxLevel ?? pawn.RaceProps.maxMechEnergy;
            return true;
        }
        if (req.Def is ThingDef { race: { IsMechanoid: true } race })
        {
            bodySize = race.baseBodySize;
            maxEnergy = race.maxMechEnergy;
            return true;
        }
        bodySize = maxEnergy = 0f;
        return false;
    }
}
