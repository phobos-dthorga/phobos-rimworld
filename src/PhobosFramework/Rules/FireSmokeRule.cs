namespace Phobos.Framework.Rules;

/// <summary>What a fire adds to the air of its cell each pass. Fires in the open add nothing: the game's own smoke only
/// matters under a roof, and so does ours. Game-free; the amounts per fire size are authored starting values in the rules Def.</summary>
public static class FireSmokeRule
{
    /// <summary>Amount of a gas, as a fraction of a full cell (or room), for one fire of <paramref name="fireSize"/>.</summary>
    public static float Amount(float fireSize, bool roofed, float amountPerSizePerPass, float maxPerPass)
    {
        if (!roofed || fireSize <= 0f || amountPerSizePerPass <= 0f) return 0f;
        float amount = fireSize * amountPerSizePerPass;
        return amount > maxPerPass ? maxPerPass : amount;
    }

    /// <summary>The game's gas grid holds 0 to 255 per cell; this turns a fraction into that unit.</summary>
    public static int ToGridUnits(float fraction)
    {
        if (fraction <= 0f) return 0;
        int units = (int)(fraction * 255f + 0.5f);
        return units > 255 ? 255 : units;
    }
}
