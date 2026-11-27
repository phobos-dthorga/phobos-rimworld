using System.Collections.Generic;
using Phobos.Framework.Interface;
using Phobos.Radio.Services;
using Verse;

namespace Phobos.Radio.UI;

/// <summary>
/// The signal button for a hand-held carried in a colonist's inventory rather than worn (Radio 0.67.0). The game asks worn apparel
/// for its buttons but never what is in an inventory, so this one comes through D'thorga Framework's <see cref="PawnCommands"/>,
/// drawn by the comp it already puts on every humanlike race: no comp of ours on every person, no Harmony patch. The same button
/// and the same test as a worn radio's (<see cref="CompRadio.CompGetWornGizmosExtra"/>); asked only while the colonist is selected.
/// </summary>
[StaticConstructorOnStartup]
public static class CarriedRadioButton
{
    static CarriedRadioButton() => PawnCommands.Register(For);

    static IEnumerable<Gizmo> For(Pawn pawn)
    {
        if (!RadioNet.Counts(pawn) || PawnHandhelds.Carried(pawn) is not { } radio) yield break;
        RadioNet.For(pawn.Map).TryGetBest(pawn, out var link);
        yield return SignalGizmo.ForHandheld(radio, link);
    }
}
