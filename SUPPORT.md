# Help and troubleshooting

**No mod is published on the Steam Workshop yet.** If you have built and installed one from source, reports and questions are
welcome in [GitHub Issues](https://github.com/phobos-dthorga/phobos-rimworld/issues/new/choose). Search existing issues first.
Support is best-effort, with no promised response time.

## Common problems

| Symptom | What to check |
| --- | --- |
| The ZIP from GitHub has no mod in it | Source archives are not built mods. See [installing](docs/installing-mods.md). |
| A mod is greyed out with a missing dependency | Switch on the mod it names (D'thorga Framework, Harmony, Vanilla Expanded Framework and so on) and load it above. |
| A D'thorga medical mod does nothing | Check the load line in `Player.log`: each mod prints one line saying what it switched on and what it left off, and why. |
| A doctor will not do something you expected | Select the doctor, right-click the patient: the game names the reason. Triage and Emergency Dosing add their own reasons there and in messages. |
| A mod shows "potentially unsupported" in yellow | Your RimWorld version isn't in the mod's `supportedVersions`. The radio framework declares 1.6 only; the others declare 1.6 and 1.7. |
| Errors after removing a mod | Read its README's "Read this first": most need a button pressed or bills deleted before removal. |

## A useful bug report

Include:

- the RimWorld version (bottom-left of the main menu) and the mod's version (its load line in the log shows it);
- what you expected and what happened;
- the steps to reproduce, and whether it began after adding a mod or loading a save;
- the relevant lines from `Player.log`, especially any line starting with `[D'thorga`.

`Player.log` is in `%USERPROFILE%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Logs\` on RimWorld 1.7, and in
the folder above on 1.6. **Each launch overwrites it**: copy it before restarting the game. `Player-prev.log` holds the run before.

Redact personal details such as your user name from paths before posting a log. Never upload credentials, game files or other
authors' mods. Sensitive reports go through [SECURITY](SECURITY.md).
