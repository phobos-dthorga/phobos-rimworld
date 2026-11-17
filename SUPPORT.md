# Help and troubleshooting

**No mod is published on the Steam Workshop yet.** If you have built and installed one from source, reports and questions are
welcome in [GitHub Issues](https://github.com/phobos-dthorga/phobos-rimworld/issues/new/choose). Search existing issues first.
Support is best-effort, with no promised response time.

## Common problems

| Symptom | What to check |
| --- | --- |
| The ZIP from GitHub has no mod in it | Source archives are not built mods. See [installing](docs/installing-mods.md). |
| A mod is greyed out with a missing dependency | Switch on the mod it names (D'thorga Framework, Harmony, Vanilla Expanded Framework and so on) and load it above. |
| Part of D'thorga Medical does nothing | Check the load lines in `Player.log`: each mod prints one line saying what it switched on and what it left off, and why; D'thorga Medical prints one per part. |
| A doctor will not do something you expected | Select the doctor, right-click the patient: the game names the reason. D'thorga Medical's triage and dosing parts add their own reasons there and in messages. |
| A mod shows "potentially unsupported" in yellow | Your RimWorld version isn't in the mod's `supportedVersions`. Every mod here declares RimWorld 1.7 only (since 8 November 2026). |
| Errors after removing a mod | Read its README's "Read this first": most need a button pressed or bills deleted before removal. |
| Several other mods stopped working at once, with few errors (a missing sidearms button, bills that throw, a blank Bills tab) | See [Harmony 3.0.3.0 and Loading Progress](#harmony-3030-and-loading-progress) below. |

## Harmony 3.0.3.0 and Loading Progress

**Not a fault in our mods, and our mods need no workaround.** Harmony 3.0.3.0 (17 November 2026, RimWorld 1.7) has a bug that its
maintainers are tracking ([pardeike/HarmonyRimWorld issue 52](https://github.com/pardeike/HarmonyRimWorld/issues/52)). With
**Loading Progress** installed and its start-up timing switched on, many mods start with none of their changes to the game applied, and
say nothing. The log's sign is one warning per affected mod: `Mod called PatchAll with a different assembly than itself: <mod> != 0Harmony`.

- **What fixes it, until Harmony ships a fix:** add `-harmony-detours` to RimWorld's launch options (Steam: right-click RimWorld,
  Properties, General, Launch Options) and restart. Or switch off Loading Progress's start-up timing in its settings and restart.
- **Our mods:** every D'thorga mod finds its own changes in a way the bug cannot affect (D'thorga Patches since 0.21.2; every other mod
  already did). You need the launch option only for other authors' mods.
- **Once Harmony is fixed**, remove the launch option. Check the issue above for its state.

## A useful bug report

Include:

- the RimWorld version (bottom-left of the main menu) and the mod's version (its load line in the log shows it);
- what you expected and what happened;
- the steps to reproduce, and whether it began after adding a mod or loading a save;
- the relevant lines from `Player.log`, especially any line starting with `[D'thorga`.

`Player.log` is in `%USERPROFILE%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Logs\` (RimWorld 1.7).
**Each launch overwrites it**: copy it before restarting the game. `Player-prev.log` holds the run before.

Redact personal details such as your user name from paths before posting a log. Never upload credentials, game files or other
authors' mods. Sensitive reports go through [SECURITY](SECURITY.md).
