# Steam Workshop pages

**Agent-written drafts, held for the owner's review. No mod is published yet** (owner, 23 November 2026: "Write drafts now").
Each `page.bbcode` is the Workshop description for one mod, in Steam's BBCode, written from the mod's About file, README and
changelog.

| Mod | Page |
| --- | --- |
| D'thorga Factory Additions | [PhobosFactoryAdditions/page.bbcode](PhobosFactoryAdditions/page.bbcode) |
| D'thorga Farming | [PhobosFarming/page.bbcode](PhobosFarming/page.bbcode) |
| D'thorga Framework | [PhobosFramework/page.bbcode](PhobosFramework/page.bbcode) |
| D'thorga Medical | [PhobosMedical/page.bbcode](PhobosMedical/page.bbcode) |
| D'thorga Patches | [PhobosPatches/page.bbcode](PhobosPatches/page.bbcode) |
| D'thorga Radio Framework | [PhobosRadioFramework/page.bbcode](PhobosRadioFramework/page.bbcode) |
| D'thorga Vehicles | [PhobosVehicles/page.bbcode](PhobosVehicles/page.bbcode) |
| D'thorga Water Tap | [PhobosWaterTap/page.bbcode](PhobosWaterTap/page.bbcode) |

- **Page rules** (phobos-modkit's `docs/workshop-publishing.md`): a page opens with `[h1]` and the About file's name, a
  `[b]Version:[/b]` line and a `[b]Publication status:[/b]` line; it holds no ASCII double quotes or backslashes (typographic
  quotes instead) and stays under 7,500 bytes. Each also carries the spelling dthorga and Ludeon's EULA disclaimer word for word.
- **A page's version and status lines are updated with the mod's version.** Keep the gotchas, and the "not yet tested in game"
  wording, in step with the mod's README.
- **`releases/` folders are generated** by the kit's `steam-publish notes --write` from each mod's changelog, and are never
  edited by hand.
