using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Phobos.Framework.Sounds;

/// <summary>
/// The game's song choice when every song it may play now weighs nothing (Framework 0.366.0; the owner's log of 28 Nov 2026: about 4,500
/// errors in six minutes, which filled the log to its cap). In danger the game plays only battle songs; it will not repeat its last seven
/// songs; and once its six battle songs with a weight had all played, the only one left was The Dead Man's Switch - AncientCorps' boss song,
/// which weighs 0 because it is meant to be played on cue. The game forgets its recent songs only when it finds no song at all, not when
/// every one it finds weighs nothing, so it drew nothing and crashed playing it, every frame until the danger passed (MusicManagerPlay
/// ChooseNextSong and AppropriateNow, read from the 1.7 decompile). Here, when every song the game may play weighs nothing, its recent songs
/// are forgotten first, as it does itself when it finds none; and if all still weigh nothing, one of them is played. Any mod's
/// zero-weight song can cause it, so it is guarded for every mod. A prefix that skips the original only in that last case, with a song
/// chosen (recorded reason: the original would return nothing, and the game would crash on it).
/// </summary>
[HarmonyPatch(typeof(MusicManagerPlay), "ChooseNextSong")]
public static class Patch_MusicManagerPlay_ChooseNextSong_NoWeight
{
    static readonly AccessTools.FieldRef<MusicManagerPlay, Queue<SongDef>>? Recent =
        AccessTools.Field(typeof(MusicManagerPlay), "recentSongs") is { } field ? AccessTools.FieldRefAccess<MusicManagerPlay, Queue<SongDef>>(field) : null;
    static readonly Func<MusicManagerPlay, SongDef, bool>? Appropriate =
        AccessTools.Method(typeof(MusicManagerPlay), "AppropriateNow") is { } method ? AccessTools.MethodDelegate<Func<MusicManagerPlay, SongDef, bool>>(method) : null;
    static bool told;

    public static bool Prepare() => Recent != null && Appropriate != null;

    public static bool Prefix(MusicManagerPlay __instance, ref SongDef __result, bool __runOriginal)
    {
        // Another mod already chose (Vanilla Expanded Framework's forced songs): leave it be.
        if (!__runOriginal) return false;
        if (Weighted(__instance, out List<SongDef> unweighted)) return true;
        // No song at all: the game's own path handles that (it forgets its recent songs, then plays any).
        if (unweighted.Count == 0) return true;
        if (Seen.Wants("framework.song-choice")) Seen.Note("framework.song-choice", null, unweighted.Count + " songs the game may play, none weighted");
        Queue<SongDef> recent = Recent!(__instance);
        if (recent.Count > 0)
        {
            recent.Clear();
            if (Weighted(__instance, out unweighted)) return true;
        }
        if (!unweighted.TryRandomElement(out SongDef song)) return true;
        __result = song;
        if (!told)
        {
            told = true;
            FrameworkMod.Logger.Info("Every song the game could play now weighs nothing, even after forgetting the recent ones; " + song.defName
                + " plays rather than nothing (said once).");
        }
        return false;
    }

    /// <summary>Whether any song the game may play now has a weight; the ones it may play that have none, when not.</summary>
    static bool Weighted(MusicManagerPlay manager, out List<SongDef> unweighted)
    {
        unweighted = new List<SongDef>();
        List<SongDef> songs = DefDatabase<SongDef>.AllDefsListForReading;
        for (int i = 0; i < songs.Count; i++)
        {
            if (!Appropriate!(manager, songs[i])) continue;
            if (songs[i].commonality > 0f) return true;
            unweighted.Add(songs[i]);
        }
        return false;
    }
}
