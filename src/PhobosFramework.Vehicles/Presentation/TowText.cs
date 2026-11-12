using Phobos.Framework;
using Phobos.Framework.Vehicles.Rules;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// A tow in words (hitch slice 3, Framework 0.104.0): the line a towing vehicle shows on its inspect text, Dashboard and Systems tab, and the
/// forecast before a tow ("can this pull that?"). Presentation only: it reads the hitch's cached weight, the drivetrain's last sample and the
/// hitch's forecast, and computes nothing of its own. Plain gives words; Operator adds the share of the speed kept; Engineer adds the train's
/// weight.
/// </summary>
public static class TowText
{
    /// <summary>What a towing vehicle is towing and, while it moves held back, what holds it.</summary>
    public static string Towing(CompPhobosHitch hitch, DisplayMode mode)
    {
        VehiclePawn? towed = hitch.Trailer;
        if (towed == null) return "";
        string line = "PhobosHitch_InspectTowing".Translate(towed.LabelShort.Named("TRAILER"), hitch.TowedKilograms.ToString("N0").Named("WEIGHT")).Resolve();
        if (!hitch.FullModel)
        {
            if (hitch.PlainSpeedCap < 0.995f) line += " " + "PhobosHitch_InspectPlainCap".Translate(hitch.PlainSpeedCap.ToStringPercent().Named("CAP")).Resolve();
            return line;
        }
        Powertrain? train = hitch.Vehicle.GetComp<CompPhobosVehicleSystems>()?.Powertrain;
        if (train == null) return line;
        if (train.Cap < 0.99)
        {
            DriveResult r = train.Result;
            string held = train.Cap <= DrivetrainModel.CrawlFloor + 0.01
                ? (r.Limit == DriveLimit.Traction ? "PhobosHitch_CrawlGrip" : "PhobosHitch_CrawlPower").Translate().Resolve()
                : r.Limit == DriveLimit.Traction ? "PhobosHitch_HeldGrip".Translate().Resolve()
                : r.Gear > 0 ? "PhobosHitch_HeldGear".Translate(r.Gear.Named("GEAR")).Resolve()
                : "PhobosHitch_HeldPower".Translate().Resolve();
            line += " " + held;
            if (mode != DisplayMode.Plain) line += " " + "PhobosHitch_SpeedKept".Translate(((float)train.Cap).ToStringPercent().Named("SHARE")).Resolve();
        }
        if (mode == DisplayMode.Engineer)
        {
            line += " " + "PhobosHitch_TrainWeight".Translate((train.MassKg() + hitch.TowedKilograms).ToString("N0").Named("TRAIN")).Resolve();
        }
        return line;
    }

    /// <summary>The forecast before a tow, in words.</summary>
    public static string Verdict(TowVerdict verdict, VehiclePawn tractor, VehiclePawn towed, DisplayMode mode)
    {
        NamedArgument tractorArg = tractor.LabelShort.Named("TRACTOR"), towedArg = towed.LabelShort.Named("TRAILER");
        NamedArgument train = verdict.TrainKg.ToString("N0").Named("TRAIN");
        string reason = (verdict.Limit == DriveLimit.Traction ? "PhobosHitch_ReasonGrip" : "PhobosHitch_ReasonPower").Translate().Resolve();
        string text = verdict.Outcome switch
        {
            TowOutcome.FullSpeed => "PhobosHitch_OutlookFull".Translate(tractorArg, towedArg, train).Resolve(),
            TowOutcome.Slowed => "PhobosHitch_OutlookSlowed".Translate(tractorArg, towedArg, train, reason.Named("REASON")).Resolve(),
            TowOutcome.Crawls => "PhobosHitch_OutlookCrawls".Translate(tractorArg, towedArg, train, reason.Named("REASON")).Resolve(),
            _ => "PhobosHitch_OutlookTooHeavy".Translate(tractorArg, towedArg, train).Resolve(),
        };
        if (mode != DisplayMode.Plain && verdict.Outcome is TowOutcome.Slowed or TowOutcome.Crawls)
        {
            text += " " + "PhobosHitch_SpeedKept".Translate(((float)verdict.SpeedShare).ToStringPercent().Named("SHARE")).Resolve();
        }
        return text;
    }

    static int hoverHitch = -1, hoverCandidate = -1, hoverTick = -1;
    static string? hoverText;

    /// <summary>
    /// The label beside the mouse while choosing what to tow: the refusal if there is one, otherwise the forecast. Kept until the game moves
    /// on a second or another vehicle is pointed at, since the targeter asks every frame.
    /// </summary>
    public static string? Hover(CompPhobosHitch hitch, VehiclePawn candidate)
    {
        int now = Find.TickManager.TicksGame;
        int hitchId = hitch.Vehicle.thingIDNumber, candidateId = candidate.thingIDNumber;
        if (hitchId == hoverHitch && candidateId == hoverCandidate && hoverTick >= 0 && now - hoverTick < 60) return hoverText;
        hoverHitch = hitchId;
        hoverCandidate = candidateId;
        hoverTick = now;
        hoverText = hitch.RefusalFor(candidate) ?? Verdict(hitch.Outlook(candidate), hitch.Vehicle, candidate, VehicleSystemsMod.Settings.displayMode);
        return hoverText;
    }
}
