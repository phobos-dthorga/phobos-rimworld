using RimWorld;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>The messages fittings and repairs send, kept quiet while the self-test works on its throwaway vehicle, so its work reaches neither the screen nor the save's history.</summary>
internal static class VehicleNotices
{
    internal static bool Quiet;

    internal static void Send(string text, LookTargets where, MessageTypeDef type)
    {
        if (!Quiet) Messages.Message(text, where, type);
    }

    internal static void Send(string text, MessageTypeDef type, bool historical)
    {
        if (!Quiet) Messages.Message(text, type, historical);
    }
}
