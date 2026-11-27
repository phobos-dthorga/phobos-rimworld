using System.Collections.Generic;
using Phobos.Framework.Items;
using RimWorld;
using Verse;

namespace Phobos.Radio.Services;

/// <summary>
/// The hand-helds a person has with them (Radio 0.67.0; the owner's question of 28 Nov 2026: dozens of colonists carried their
/// radios in Combat Extended loadouts, which put kit in the inventory and never wear it). Every hand-held they wear counts, as
/// before. With none worn, the best one in their inventory counts (the most power, then the one held highest, as for a caravan),
/// and works as a worn one does: the owner's choice, "same as worn", so a pouch on a loadout reads as clipped on. A spare in the pack
/// is not a second radio on the air: only the one they would take out to call asks a station for a channel.
/// <para>Cost: one look over a person's worn clothes and inventory, by the net once a recompute (about once a second) for each free
/// colonist, and by a carried radio's button only while its colonist is selected. Allocates nothing.</para>
/// </summary>
public static class PawnHandhelds
{
    static readonly List<CompRadio> found = new();

    /// <summary>Adds the hand-helds this person uses to the list: every worn one; with none worn, the best one carried.</summary>
    public static void InUse(Pawn pawn, List<CompRadio> into)
    {
        CompRadio? carried = Look(pawn, into, out _);
        if (carried != null) into.Add(carried);
    }

    /// <summary>The carried hand-held this person uses, when they wear none: it gets its signal button from <see cref="UI.CarriedRadioButton"/>,
    /// since the game shows buttons only for what is worn.</summary>
    public static CompRadio? Carried(Pawn pawn) => Look(pawn, null, out _);

    /// <summary>The hand-held this person would call with: the first worn, else the best carried; null for none.</summary>
    public static CompRadio? First(Pawn pawn)
    {
        CompRadio? carried = Look(pawn, null, out CompRadio? firstWorn);
        return firstWorn ?? carried;
    }

    /// <summary>
    /// Sorts what the person has: worn hand-helds go to the list (when one is given), the first of them out; the best carried one is
    /// returned only when none is worn.
    /// </summary>
    static CompRadio? Look(Pawn pawn, List<CompRadio>? worn, out CompRadio? firstWorn)
    {
        firstWorn = null;
        CompRadio? bestCarried = null;
        found.Clear();
        PawnGear.CompsOn(pawn, found);
        for (int i = 0; i < found.Count; i++)
        {
            CompRadio radio = found[i];
            if (radio.Props.isStation) continue;
            if (radio.parent is Apparel { Wearer: not null })
            {
                firstWorn ??= radio;
                worn?.Add(radio);
            }
            else
            {
                CaravanRadios.Consider(radio, ref bestCarried);
            }
        }
        found.Clear();
        return firstWorn == null ? bestCarried : null;
    }
}
