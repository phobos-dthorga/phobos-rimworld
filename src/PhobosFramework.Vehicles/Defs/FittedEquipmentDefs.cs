using Phobos.Framework.Vehicles.Rules;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// What another author's vehicle carries, marked on its Def by an XML patch (Framework 0.241.0 for suppression; 0.244.0 for the spall liner and
/// the glass, the owner's go of 23 Nov 2026: one extension for all three, as the fire, spall and glass records planned). Our own vehicles say
/// the same through their loadouts and upgrade trees; this is for designs whose trees we leave alone.
/// </summary>
public class PhobosFittedEquipmentExtension : DefModExtension
{
    /// <summary>True or false marks the vehicle armoured or not; unset leaves it to its outer plates' armour. An armoured vehicle takes the
    /// suppression systems as fittings, and its glass is armoured where its loadout says ordinary glass.</summary>
    public bool? armoured;

    /// <summary>The real vehicle carries a spall liner (one source per vehicle, in the patch that marks it): with Combat Extended it has the
    /// liner part, born whole, which wears and is re-lined as ours is.</summary>
    public bool lined;

    /// <summary>What its glass is, where its loadout and its armour would say otherwise.</summary>
    public GlassKind? glass;

    /// <summary>No glass at all: a design whose cab is closed by plate only.</summary>
    public bool noGlass;
}
