# Help and troubleshooting

**No mod is published yet**, so there is nothing to troubleshoot in game today. Questions about the design
and suggestions are welcome in [GitHub Issues](https://github.com/phobos-dthorga/phobos-rimworld/issues/new/choose).
Search existing issues first. Support is best-effort, with no promised response time.

## Common problems

| Symptom | What to check |
| --- | --- |
| The ZIP from GitHub has no mod in it | Source archives are not installable mods, and nothing is published yet. See [installing](docs/installing-mods.md). |
| A mod shows "potentially unsupported" in yellow | Your RimWorld version isn't in the mod's `supportedVersions`. On 1.7 this is usually only a label; the mod's README says what has been tested. |
| A mod is greyed out with a missing dependency | Subscribe to the mod it names (often Harmony), and load it above. |

## A useful bug report (once mods are published)

Include:

- the RimWorld version (bottom-left of the main menu) and the mod's version;
- what you expected and what happened;
- the steps to reproduce, and whether it began after adding a mod or loading a save;
- the relevant lines from `Player.log`.

`Player.log` is in `%USERPROFILE%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\`
(`Logs\Player.log` on RimWorld 1.7). **Each launch overwrites it**: copy it before restarting the game.
`Player-prev.log` holds the run before.

Redact personal details such as your user name from paths before posting a log. Never upload credentials,
game files or other authors' mods. Sensitive reports go through [SECURITY](SECURITY.md).
