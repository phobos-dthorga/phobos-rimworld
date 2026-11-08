# RimWorld 1.7 readiness of the owner's mod list

24 Oct 2026 · agent record, for the owner's review. Read-only checks; **nothing here is tested in game.**

**The short answer: not ready yet, and nothing forces a move.** RimWorld 1.7 is only on Steam's unstable
beta branch; the main branch stays on 1.6 with no stable date given
([Ludeon, 12 Oct 2026](https://ludeon.com/blog/2026/10/announcing-the-waylith-engine-and-update-1-7/)).
**165 of your 284 switched-on Workshop mods (58%) now declare 1.7.** The 119 that don't include the four
your colony is built on: **Combat Extended, Vehicle Framework, Adaptive Storage Framework and XML
Extensions**. Each has a 1.7 build in progress or promised; none is on Steam yet. Players report that on
1.7, **Graphics Settings+ breaks textures across the game, and The Dead Man's Switch stops new worlds from
generating.** Allow Tool and Adaptive Ideology Storage are also reported broken. On the good side, players
and authors report that at least 15 of the mods that don't declare 1.7 work there anyway. Re-check when the four
frameworks update.

**Re-checked 25 Oct 2026: no change.** The owner wants to move soon because of performance. None of the four
frameworks has a 1.7 build on Steam yet. Steam's last updates: Combat Extended 15 Oct 2026 (1.6 only),
Vehicle Framework 21 Oct 2025, XML Extensions 18 Jun 2026, Adaptive Storage Framework 14 Jun 2025. On GitHub,
CE's `1.7-Test` branch has had no commit since 16 Oct. Vehicle Framework's `develop` branch is active (last
commit 19 Oct) but has no new release. 167 of the 289 switched-on Workshop mods now declare 1.7. The owner's
loading times are measured below, under [Your loading times on 1.6](#your-loading-times-on-16).

## Read this first (gotchas)

- **Keep playing your colony on 1.6** (Steam's default branch). Ludeon says saves load both ways between 1.6
  and 1.7, but a mod that fails to load on 1.7 can't keep its things in a save made there. That last point
  is my inference, not Ludeon's.
- **If you want a look at 1.7 now, test on a copy.** Back up `Saves` and `Config` first (both in
  `%USERPROFILE%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\`), then follow the steps under
  [Trying 1.7 safely](#trying-17-safely).
- **Keep Prepatcher switched off.** It doesn't work on 1.7 yet (community wiki and community list). It is
  already off in your list.
- **Keep Harmony at the top of the load order** (it is: load #1). It ships Harmony 3 for 1.7 in its own `1.7`
  folder.
- **Switch off Graphics Settings+ before you launch 1.7.** Players report that it breaks vanilla and modded
  textures there. Its author says a 1.7 version is nearly done and will need his TeleCore mod (16 Oct).
- **Switch off The Dead Man's Switch before you launch 1.7.** Players report that the world won't generate
  with it on, even when it's the only mod (14 Oct). One says it ruined their save (18 Oct). Its 1.7 edition
  is DMS2, a 1.7-only overhaul with the same package ID. DMS2 keeps almost every DMS name the owner's save
  uses (see [The owner's first try](#the-owners-first-try-26-oct-2026)), so a copy of a 1.6 colony may
  carry over. Its page describes new content throughout, so expect things to behave differently (my
  inference; not yet tested in game).
- **After any trip to 1.7, check The Dead Man's Switch is still its 1.6 version.** Players report that Steam
  serves its old 1.5 files to 1.6 games, especially after switching back from 1.7 (15 to 20 Oct). One fixed
  it by verifying the game files in Steam.
- **On 1.7 the game log moves** to `Logs\Player.log` in the same folder.

## How this was checked

| Check | What it tells you | Source |
| --- | --- | --- |
| Each mod's `About.xml` `supportedVersions` | Whether the author has declared 1.7. Ludeon asks authors to test before they do | Your installed mods, read 24 Oct 2026 |
| Steam's Workshop tags | Catches a mod whose page says 1.7 but whose files don't | Steam's public Web API, no login |
| A byte scan of each mod's files | Whether it carries C# code on 1.7, whether it rewrites game code (Harmony transpilers), uses Prepatcher, or ships asset bundles and shaders. Nothing was run | Your installed mods |
| The latest 50 player comments on each non-1.7 mod's Workshop page | Real reports from players on 1.7 | Steam Workshop, read 24 Oct 2026: 121 of 125 pages. Steam's rate limit kept back Vehicle Framework, Vanilla Hair Retextured, Hemofungus and IRBM Vanilla Retexture; Vehicle Framework's status comes from its GitHub instead |
| The community 1.7 compatibility list | 62 mods tested by players; a mod's absence means nothing | [Google Sheet by Zibrolta16](https://docs.google.com/spreadsheets/d/1uQLgePU_8TmO7pgbeq2QD3x_rEVq2TyJp6Ae2V94qI4/edit) (community source, checked 24 Oct 2026) |
| The frameworks' GitHub repositories | Whether a 1.7 build exists before it reaches Steam | Combat Extended, Vehicle Framework, HugsLib |

The background on what 1.7 changes is in the [RimWorld modding reference](rimworld-modding-reference.md#versions):
the Waylith engine, .NET 10, Harmony 3, and "99% of mods built for 1.6 should work out of the box" (community
wiki, [RimWorld 1.7 Mod Updates](https://rimworldwiki.com/wiki/Modding_Tutorials/RimWorld_1.7_Mod_Updates)).

## The numbers

Your list on 24 Oct 2026: RimWorld 1.6.4871 rev590, 290 switched on (the game, five DLCs and 284 Workshop mods).

| Group | Mods | What it means |
| --- | --- | --- |
| Declare 1.7 | **165** | Their authors say they work. 73 of them ship a separate `1.7` folder, most with code rebuilt for .NET 10. Two of the 165 (Vanilla Vehicles Expanded and its Tier 3) say 1.7 on Steam but not in their files, so 1.7 will still show them in yellow |
| Don't declare 1.7, XML only | 37 | Add things through XML and art alone. Very likely fine, because 1.7 swaps the engine and "looks and plays exactly the same" (Ludeon). Three of them (CE Armors, Armory and Guns) need Combat Extended |
| Don't declare 1.7, C# code, no transpilers | 55 | Most such mods run unchanged (Ludeon, community wiki). The risk is moderate |
| Don't declare 1.7, C# code with transpilers | 27 | **Most fragile.** A transpiler rewrites game code instruction by instruction, so it breaks when that code changes. That reasoning is mine; Harmony 3's stricter type checks add to it (community wiki) |

## The blockers: frameworks other mods stand on

| Framework | Your mods that need it | 1.7 status on 24 Oct 2026 |
| --- | --- | --- |
| **Combat Extended** (and its Armors, Armory and Guns) | Charged Ammo, Munitions Industries for VFE - Factory; CE also carries its own patches for many of your mods | **Not on Steam for 1.7.** The CE team's 1.7 build is on the GitHub branch [`1.7-Test`](https://github.com/CombatExtended-Continued/CombatExtended/tree/1.7-Test) (first commit 12 Oct, last 16 Oct; it declares 1.7 only). Players disagree about running the 1.6 build on 1.7: several say it works, others report a map generation error (15 Oct), broken sounds (13 Oct), and one says CE's 16 Oct update broke it on 1.7. An unofficial port, "Combat Extended 1.7 [Unofficial]" (19 Oct), says it isn't affiliated with the CE team and tells players to switch back once the official build is out. **My advice: wait for the official one** |
| **Vehicle Framework** | Vanilla Vehicles Expanded, its Tier 3, Upgrades and Drop Pod add-ons, Humvee, Trains of the Rim, Frontline Workshop; RimExodus binds to it too | **Not on Steam for 1.7**; the Steam copy dates from 21 Oct 2025. Smash Phil merged a ["Waylith update"](https://github.com/SmashPhil/Vehicle-Framework/pull/376) into the development branch on 12 Oct 2026, and that branch declares 1.7. Its Steam files already include Linux asset bundles, which 1.7 needs for shaders. Vanilla Vehicles Expanded waits on it: Oskar Potocki (22 Oct), "until it is, all I can do is break your saves when I update this mod" |
| **Adaptive Storage Framework** | [sbz] Fridge, Gravship Storage, Neat Storage and Workbench Shelf; Adaptive Ideology Storage; Cargo Holds; Warehouse Storage | **Its author says it already works.** bradson (13 Oct): it "currently already fully works as is"; a clean 1.7 rebuild comes during the beta. One known 1.7 bug: Adaptive Ideology Storage's buildings throw errors, and pawns, animals and doors vanish when zoomed out (player, 21 Oct). The author says it's fixed in the proper 1.7 update |
| **XML Extensions** | All Production Gives XP, TSIND Garage Doors QA | **Update promised.** Imranfish (20 Oct): "Will update to 1.7 later this week". One player says the 1.6 build "causes loading issues on 1.7" (15 Oct) |
| **HugsLib** | Allow Tool | **No update since July 2025.** One player tested it on 1.7 with no problems (13 Oct); the community list marks it partly incompatible because log publishing doesn't work (23 Oct) |

## Reported broken or partly broken on 1.7

From players' Workshop comments and the community list. Each is a report, not a confirmed fault; "one
player" means nobody else has said so yet.

| Mod | Report | My suggestion for 1.7 |
| --- | --- | --- |
| **Graphics Settings+** | Many players: red texture errors and placeholder textures for modded and vanilla things (13 to 24 Oct). One traced it to its texture-loading patch, which makes the game's texture lookups come back empty on 1.7 (17 Oct). The author says a 1.7 version is nearly done and will need his TeleCore mod (16 Oct) | **Switch it off on 1.7** |
| **The Dead Man's Switch** | The world won't generate on 1.7, even with it as the only mod (14 Oct); an error creating a colony (18 Oct); a pawn given a mechlink can't be drafted (14 Oct); "Ruins your save game" (18 Oct). The community list calls it working (14 Oct) but points 1.7 players to [DMS2](https://steamcommunity.com/sharedfiles/filedetails/?id=3819799478), a separate 1.7-only overhaul | **Use DMS2 on 1.7 and the old one on 1.6.** They share a package ID, so only one can be installed at a time |
| **Allow Tool** | "1.7 has broken this mod" (16 Oct); "My gizmos are gone under 1.7" (14 Oct) | Switch it off on 1.7 until it updates |
| **Adaptive Ideology Storage** | Errors whenever one of its storage buildings is on screen (community list, 23 Oct; see Adaptive Storage Framework above) | Switch it off on 1.7, or wait for Adaptive Storage Framework's 1.7 rebuild |
| Improved Map Search | Its grouping and its world map search don't work properly on 1.7 (one player, 18 Oct) | Expect the plain search on 1.7 |
| EdB Prepare Carefully | A pawn's age changes as the mouse moves, without a click (one player, 17 Oct) | Check ages before you start a colony |
| MutatorWorldIcons | Plants and other map-specific resources no longer show on 1.7 (one player, 17 Oct) | Watch for it |
| Warehouse Storage | With it on, right-clicking gave no option to order a pawn to build; removing only this mod fixed it (one player, 24 Oct) | Watch for it; it also needs Adaptive Storage Framework |
| InterRim Ballistic Missile | "dont works in 1.7" (one player, 17 Oct) | Watch for it |
| [kyzy] Bio Ranch | Runs without errors, but the "allow hay" setting won't stay set (one player, 15 Oct) | Minor |
| Death Rattle Continued | "I think the mod doesn't work on 1.7" (one player, 13 Oct). Another player on 1.7 sees enemies die outright where they used to go down, but doesn't know which of their health mods causes it (23 Oct) | Watch how often raiders die rather than go down |
| Better Stacktraces | One player on the 1.7 beta: it throws an error on some stack frames, which stopped another mod's action from finishing (23 Oct) | It loads second in your list and touches every error report. If 1.7 misbehaves, switch it off first (my view) |
| [MUS] Space Base Furniture | "a lot of log error in 1.7" (one player, 17 Oct) | Watch the log |
| Anthrosonae | One player sees invisible lower faces on 1.7 (13 and 14 Oct). The author says others run it fine, so it's likely a clash with another mod (15 Oct) | Watch for it |

## Reported working on 1.7, though not yet declared

Players or the authors themselves say these run on 1.7; most just haven't added the version tag yet.

| Mod | Report |
| --- | --- |
| Adaptive Storage Framework | The author: "already fully works as is" (13 Oct), apart from the Adaptive Ideology Storage bug above |
| Deep And Deeper | The author: "In my tests it worked fine" (17 Oct) |
| RimHUD, Interaction Bubbles | The author, Jaxe: "Good to hear it's still working on 1.7" (20 Oct). Players agree. One player couldn't save RimHUD's settings from its tutorial (22 Oct) |
| Pick Up And Haul | "Mod seems to work just fine still" (22 Oct) |
| HugsLib | Tested on 1.7 with no problems (13 Oct); log publishing doesn't work (community list, 23 Oct) |
| Real Ruins | "works just fine" (18 Oct); one player couldn't download new ruins into the map cache on 1.7 (14 Oct) |
| Tunnelers ARE deep drills, Tilled Soil, Defensive Positions - Forked | Players: works on 1.7 (18, 20 and 22 Oct) |
| Vanilla Nutrient Paste Expanded: Reimagined Progression, and its retexture | Players: works on 1.7 (17 Oct) |
| Cargo Holds | The author: "it is compatible with 1.7 beta", not tagged yet (13 Oct) |
| Mid-saver Saver | A player used it on 1.7 to repair a map broken by removing mods (22 Oct) |
| Munitions Industries for VFE - Factory | "Works in 1.7" (15 Oct). It still needs Combat Extended |

Authors who say a 1.7 update is coming: XML Extensions, Techwear, Vanilla Outposts Expanded: Power Grid,
RimHUD, Interaction Bubbles, Tunnelers ARE deep drills, Deep And Deeper and Graphics Settings+. Colony
Hospital's author is waiting for 1.7 to leave beta.

## Mods to watch: code that rewrites the game's code

These 27 don't declare 1.7 and use Harmony transpilers. A transpiler rewrites game code instruction by
instruction, so it is the kind of patch most likely to break. Some already have reports above. If
something breaks on 1.7 and the log doesn't name the mod, look first at the ones nobody has reported on
yet:

Blueprints Forked, Complementary Odyssey, Float Sub-Menus, Hospitality + Ideology Patch, Launch Sites Expire,
Medical System Expansion 2, Not My Fault, Please use the pews!, Prison Labor, Prisoner Realism, Simple
sidearms, Smart Farming, Spaceship Solar Hull, Spaceship Turret Hull, Tech Level Progression, Ushankas
Glittertech Expansion, VFE Factories: Buffs and tweaks.

With reports above: Adaptive Storage Framework, Allow Tool, Combat Extended, Graphics Settings+, HugsLib,
Improved Map Search, Pick Up And Haul, RimHUD, Vanilla Nutrient Paste Expanded: Reimagined Progression,
Vehicle Framework.

**Two mods with possible texture trouble:** Rails and Roads of the Rim (Continued) and Humvee (Continued)
each ship one asset bundle with no Linux copy. 1.7 renders with Vulkan and loads the Linux versions of
shaders (community wiki), so their custom shaders may not load. Whether those bundles hold shaders the
1.7 engine can use isn't known; watch for pink or missing textures.

## Your own mod: Phobos Radio Framework

**In good shape for 1.7, not yet tested there.** Its `LoadFolders.xml` already sends 1.7 to the 1.6 build,
and its `About.xml` declares only 1.6, which is right until it is tested in game. Its assembly uses no
Harmony, no transpilers and no shaders, and Vanilla Expanded Framework, its one dependency, declares 1.7.
The native .NET 10 build is the step [Building](building.md) describes.

## Trying 1.7 safely

For a look at the speed, not for your colony. Ludeon claims two to four times better performance. One
player with a big CE list reports much faster loading and RAM use down from about 45 GB to about 30 GB
(13 Oct; one player's report).

1. Close RimWorld. **Copy** the `Saves` and `Config` folders somewhere safe, and export your RimSort list.
   One player says 1.7 switched all their mods off at first launch (13 Oct), so the copy of `Config`, which
   holds the mod list, matters.
2. In Steam: right-click RimWorld, Properties, Betas, choose **unstable**. Your Workshop mods stay the same.
3. In the mod list, **switch off Graphics Settings+ and The Dead Man's Switch** first. Graphics Settings+
   removes every DLC texture on 1.7 (seen in the owner's own game, 26 Oct). Allow Tool, Adaptive Ideology
   Storage and Better Stacktraces are worth switching off too.
4. Load a **copy** of a save, play a little, then read `Logs\Player.log`.
5. To go back: Betas, choose **None**. Put the backed-up `Config` folder back if the mod list changed.
6. **Check The Dead Man's Switch is back on its 1.6 version** (players report Steam serving its old files
   after a switch back). If it isn't, verify the game files in Steam (Properties, Installed Files).

## Your loading times on 1.6

Measured 25 Oct 2026 from Loading Progress's own records (`StartupImpactSessions`, 10 launches from 23 to
25 Oct). Read-only; nothing was changed.

**The launches fall into two groups, and the mod list barely matters.**

| Launch | Mods | To the main menu | Engine start, before mods | Texture and graphics stage |
| --- | --- | --- | --- | --- |
| Fast (6 launches) | 284 to 290 | 3.7 to 4.6 min | about 3 s | about 27 s |
| Slow (4 launches) | 289 to 295 | 6.7 to 7.0 min | about 24 s | 168 to 193 s |

- **A slow launch is the first one after a gap.** On 23 Oct a 6.7-minute launch with 291 mods was followed
  11 minutes later by a 4.1-minute one with 290. The newest slow one came 24 minutes after the PC restarted
  (3:08 am, 25 Oct). The extra time is spread across every texture-heavy mod (Vanilla Textures Expanded went
  from 1 s to 15 s), not one bad mod.
- **The mods live on a very fast drive** (a Crucial T700, PCIe 5 NVMe), as about **115,000 files in 8.2 GB**,
  and the PC has 63 GB of memory. Raw drive speed is unlikely to be the cause.
- **My best explanation, not proven:** Windows Defender's real-time scanning checks each file the first time
  it is opened after a restart or a virus-definition update, and 115,000 files add up. Defender is on, and
  its definitions last updated at 8:59 am on 24 Oct; the next launch was a slow one. Its exclusion list can't
  be read without admin rights, so whether the Steam folders are already excluded is unknown.
- **What would settle it:** after a restart, launch, quit, and launch again at once. A big gap between those
  two launches confirms first-read overhead. Loading Progress's own record of each launch is enough; no extra
  tools are needed.
- **Options are the owner's to choose; agents don't change security settings.** A Defender exclusion for the
  RimWorld and Workshop folders would probably remove the gap. **The cost:** Workshop mods include other
  people's code, and an excluded folder is never scanned. Doing nothing costs about three minutes on the first
  launch of the day.
- **Whether 1.7 helps with this is unknown.** Ludeon reports a 1,500-mod pack loading in 6 minutes instead of
  14, and "2-4x" overall ([Ludeon, 12 Oct 2026](https://ludeon.com/blog/2026/10/announcing-the-waylith-engine-and-update-1-7/)),
  but per-file scanning happens outside the game. My inference: 1.7 should shorten the fast launches, and may
  shorten the slow ones less.
- **This is loading time only.** Speed while playing (ticks per second) needs Dubs Performance Analyzer in a
  running game, which the owner has.

## The owner's first try (26 Oct 2026)

The owner launched RimWorld 1.7.4985 rev1269 with the full list and loaded the 1.6 save PG13; **many
graphics were missing in game**. From the game's log (`Logs\Player.log`, read the same morning):

- **Graphics Settings+ caused the missing graphics.** It replaces the game's texture loader and reads each
  texture as a file on disk. On 1.7 the DLCs no longer ship loose `Textures` folders, so all 4,473 of its
  load attempts failed (Royalty 522, Ideology 1,014, Biotech 1,122, Anomaly 992, Odyssey 823). Of the 723
  distinct textures the game then couldn't find, all but two are the DLCs' own; the other two are Vanilla
  Textures Expanded's versions of Ideology's darktorch and burnbong.
- **The owner had swapped The Dead Man's Switch for DMS2** (Workshop 3819799478) for 1.7. DMS2 uses the
  old mod's package ID, `Aoba.DeadManSwitch.Core`, so **the two can't be installed together**: the game
  keeps only the first of two mods with one package ID. DMS2 defines every DMS name PG13 uses (507
  distinct names, read from the save) except two mech-corpse types, `DMS_Mech_Killdozer_b` and
  `DMS_Mech_Geochelone_b`, which appear only in storage and bill filters. Those filter entries are what
  the "Could not load reference" errors were about.
- **The save loaded with broken links**: missing Apex Mechanoids hediffs and a Rimatomics++ item,
  references to individual DMS mechs that weren't in the save (probably stale references from other mods;
  the 1.6 log, which would show whether they're new, had been overwritten), ideoligion and faction
  references that didn't resolve, and a lover with no partner. **Nothing was saved on 1.7**: the newest
  save was still PG13 from 08:22, before the 1.7 runs.

**Second try, the same morning, with Graphics Settings+ off:** the owner reports it "looks fine now,
perhaps with better visual fidelity". The log has **no texture errors**. What it does show:

- **Loading is about four times faster.** Loading Progress timed each launch to the main menu: 263 to 463
  seconds on 1.6 (24 to 26 Oct, 284 to 295 mods); on 1.7, 386 seconds for the first launch, which builds
  the new shader and texture caches, then 87 to 116 seconds.
- **New on 1.7:** Vanilla Expanded Framework's transpiler on the lighting overlay
  (`SectionLayer_LightingOverlay.GenerateLightingOverlay`) can't find its target code, so that patch doesn't
  apply. What it changes in play isn't known. RimExodus switched off six of its Combat Extended hooks
  because CE's methods differ from what it expects. A DMS2 mech had no CE inventory, so CE couldn't give it
  a loadout.
- **Probably not caused by 1.7:** the 238 ideoligion errors are opinions that Enhanced Ideology still keeps
  about ideoligions 85 and 87, which are no longer in the save. Five Apex Mechanoids Maul mechs lost a
  health-based stat effect that the mod's code creates itself (both builds carry the same code), and
  Rimatomics++'s 1.7 build no longer has a placeholder item its 1.6 build had.
- **The colony was saved on 1.7**: PG12 and PG13 (09:12) are now 1.7 saves. **The last 1.6 saves are PG11
  (08:22) and Autosave-19 (08:19).**

**Match the DMS edition to the game version:** DMS2 on 1.7, the old
[The Dead Man's Switch](https://steamcommunity.com/sharedfiles/filedetails/?id=3121742525) on 1.6. DMS2
can't run on 1.6: its `LoadFolders.xml` has only a 1.7 section. Swap them in Steam (subscribe to one,
unsubscribe from the other) each time you change branch.

## When to check again

Re-run this check when any of these reaches Steam: **Combat Extended for 1.7, Vehicle Framework for 1.7,
XML Extensions for 1.7, or Adaptive Storage Framework's rebuild.** Between them they carry about 20 of your
mods, and CE carries the feel of the whole colony.
