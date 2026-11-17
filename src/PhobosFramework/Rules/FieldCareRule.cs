namespace Phobos.Framework.Rules;

/// <summary>Whether a piece of care is worth giving to a patient where they lie, or waits until they are in a bed.</summary>
public enum CareUrgency
{
    /// <summary>Care that keeps someone alive or out of shock now (pain relief, breathing, rinsing acid off): given where they lie.</summary>
    CannotWait,
    /// <summary>Care that works over days (a dose against a disease): given in a bed, so no doctor walks out to someone the carrier is about to lift.</summary>
    WaitsForBed,
}

/// <summary>
/// When field care is given, and when it gives way to the carry to a bed (Framework 0.126.0; owner's report of 17 Nov 2026, downed
/// prisoners left on the ground while doctors and carriers took turns at them). Game-free.
/// <para>
/// The answer to "when does field care stop": it never holds a patient on the ground against anyone who would carry them. Care given
/// on the ground fills the time until a carrier arrives and ends the moment the patient is lifted; it goes on at the bed. Agent
/// default, for the owner to revise: the carry wins even over a hand pump, because only a bed (with oxygen) fixes what a pump holds off.
/// </para>
/// </summary>
public static class FieldCareRule
{
    /// <summary>Whether care of this urgency may be given now, to a patient in a bed or not.</summary>
    public static bool MayActNow(CareUrgency urgency, bool inBed) => urgency == CareUrgency.CannotWait || inBed;

    /// <summary>
    /// Whether a doctor's hold on the patient keeps carriers away: only in a bed. On the ground the hold keeps other doctors of ours
    /// from doubling up, and nobody else.
    /// </summary>
    public static bool HoldKeepsCarriersAway(bool onTheGround) => !onTheGround;
}

/// <summary>
/// Where a carrier takes a downed patient when a vehicle with litters is taking casualties aboard (Framework 0.126.0): to the litter
/// when it is nearer than the patient's bed, or when they have no bed. Distances are straight lines, squared, as the game's own nearest
/// searches compare them. No radius and no authored figure. Game-free.
/// </summary>
public static class LitterRule
{
    /// <summary>Stands for "no bed for this patient" where a squared distance is expected.</summary>
    public const int NoBed = -1;

    /// <param name="litterDistanceSquared">From the patient to the vehicle.</param>
    /// <param name="bedDistanceSquared">From the patient to their bed, or <see cref="NoBed"/>.</param>
    public static bool PrefersLitter(int litterDistanceSquared, int bedDistanceSquared) =>
        bedDistanceSquared == NoBed || litterDistanceSquared < bedDistanceSquared;

    /// <summary>Litters a vehicle still has for patients not yet on their way to it.</summary>
    public static int FreeForNewPatients(int freeLitters, int carriersOnTheWay) => System.Math.Max(0, freeLitters - carriersOnTheWay);
}
