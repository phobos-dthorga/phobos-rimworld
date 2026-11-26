using System;

namespace Phobos.Framework.Rules;

/// <summary>
/// Water treated by reverse osmosis in a vehicle's tank, game-free (docs/development/vehicle-road-work.md, the reverse-osmosis kit). A
/// compartment's quality is one number, so the treatment counts the litres that have passed the membrane; once all of what is left has,
/// the compartment is clean. The reject share and the membrane's life are the agent's figures (no source read gives them); the rate
/// comes from the kit's stat, the US Army's Lightweight Water Purifier's 125 gallons an hour.
/// </summary>
public static class TreatmentRule
{
    /// <summary>Of what passes the membrane, the share run off as reject to carry the salts and dirt away (owner's card: a quarter).</summary>
    public const float RejectShare = 0.25f;
    /// <summary>Litres one membrane treats before it is spent (agent's figure: about twenty bowser loads).</summary>
    public const float MembraneLitres = 50000f;
    /// <summary>The membrane wear at which it is spent and the kit stops.</summary>
    public const float SpentAt = 0.95f;

    /// <summary>One step's outcome for a compartment.</summary>
    public readonly struct Pass
    {
        public readonly float processed;
        public readonly float rejected;
        public readonly float amount;
        public readonly float treated;
        public readonly bool clean;

        public Pass(float processed, float rejected, float amount, float treated, bool clean)
        {
            this.processed = processed;
            this.rejected = rejected;
            this.amount = amount;
            this.treated = treated;
            this.clean = clean;
        }
    }

    /// <summary>
    /// Passes up to <paramref name="litres"/> of the compartment's untreated water through the membrane: a quarter of it runs off, the
    /// rest joins the treated. Clean once nothing untreated is left.
    /// </summary>
    public static Pass Step(float amount, float treated, float litres)
    {
        if (amount <= 0f) return new Pass(0f, 0f, 0f, 0f, false);
        treated = Math.Clamp(treated, 0f, amount);
        float untreated = amount - treated;
        float processed = Math.Min(Math.Max(0f, litres), untreated);
        float rejected = processed * RejectShare;
        float newAmount = amount - rejected;
        float newTreated = Math.Min(newAmount, treated + processed - rejected);
        return new Pass(processed, rejected, newAmount, newTreated, newAmount - newTreated <= 0.01f);
    }

    /// <summary>The membrane wear a number of litres treated adds, as a share of its life.</summary>
    public static float Wear(float processedLitres) => processedLitres <= 0f ? 0f : processedLitres / MembraneLitres;

    /// <summary>Whether the membrane is spent.</summary>
    public static bool Spent(float wear) => wear >= SpentAt;
}
