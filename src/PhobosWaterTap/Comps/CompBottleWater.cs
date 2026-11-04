using DubsBadHygiene;
using Verse;

namespace Phobos.WaterTap;

/// <summary>
/// Keeps a water bottle's quality when its stack is split or merged, and shows it. Dubs Bad Hygiene's CompContamination
/// holds the quality and is read when someone drinks, but it has no stack rules: a piece split off a stack comes out
/// Treated, and a merged stack keeps only one side's quality. A pawn taking two bottles from a stack of untreated
/// water would carry away clean water. A tap makes such stacks common, so this closes that gap. Saves nothing.
/// </summary>
public class CompBottleWater : ThingComp
{
    /// <summary>Bottles of different water stay in separate stacks: they are separate bottles.</summary>
    public override bool AllowStackWith(Thing other) =>
        base.AllowStackWith(other) && TapService.WaterQualityOf(parent) == TapService.WaterQualityOf(other);

    public override void PostSplitOff(Thing piece)
    {
        base.PostSplitOff(piece);
        TapService.SetWaterQuality(piece, TapService.WaterQualityOf(parent));
    }

    public override string TransformLabel(string label)
    {
        return TapService.WaterQualityOf(parent) switch
        {
            ContaminationLevel.Untreated => "PhobosWaterTap_LabelUntreated".Translate(label.Named("LABEL")),
            ContaminationLevel.Contaminated => "PhobosWaterTap_LabelContaminated".Translate(label.Named("LABEL")),
            _ => label,
        };
    }
}
