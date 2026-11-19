# Settings pages: one layout for every D'thorga settings window

**Status (20 Nov 2026):** built in D'thorga Framework 0.184.0, D'thorga Medical 0.19.0 and D'thorga Patches 0.27.0; not yet tested in
game (test row 267). The vehicle message level (owner's choice, below) is the next step, not built yet (row 268 held for it).

The owner, 20 Nov 2026: "We absolutely need to straighten/neaten out our settings pages throughout our Rimworld mods. Right now, it's
quite the mess and rather difficult to find anything of what you might be after. Also, do an audit for any new settings that might make
sense while retiring/merging/removing/etc any present ones."

## What it lets the player do, stops, and changes

- **Lets them** find a setting by typing part of its name or tooltip, from every page of a mod at once; see everything they have changed
  from its default, across all pages (**Changed only**, and a dot beside each changed row); put one page back to its defaults; and learn,
  as they close the window, which of their changes wait for a restart (at the main menu, restart there and then).
- **Stops** nothing. Every setting keeps its saved key and value.
- **Changes** where settings are: each mod is one entry in the game's settings list (nine entries became six), with pages down the
  left, as the game's own Options window lays out its categories. The vehicle settings have their own entry.

## The owner's choices (20 Nov 2026)

| Question | Choice |
| --- | --- |
| Layout | **Pages and search**, like the game's Options window: pages down the left, search over every page, Changed only, reset per page, the last page remembered, the search box taking typing at once (keyboard over mouse). Over tabs along the top (cramped past eight) and over tidying the one scroll |
| Where the vehicle settings live | **An entry of their own, D'thorga Vehicle Systems**, over pages inside D'thorga Framework's entry |
| The Harrier disassembly trace | **Moved to Detailed logs**: its own tick (on by default) retired; the trace is the detailed log "D'thorga Patches: Harrier disassembly", off until ticked |
| New settings this pass | **A vehicle message level** and **a restart reminder** |

## Entries before and after

| Before (9 entries) | After (6 entries) |
| --- | --- |
| D'thorga Framework (one scroll, about 20 rows with the log list) | **D'thorga Framework**: General, Log and reports, Detailed logs, Testing |
| D'thorga Framework: vehicle systems (one scroll of about 60 rows; "Engines" held 22 unrelated ones) | **D'thorga Vehicle Systems**: Which vehicles, Systems, Engines and driving, Aircraft and sling loads, Steam and electric, Dangers and raids, Repairs service and fittings, Cabs camps and caravans, Display sound and stories, Removing |
| D'thorga Medical (one scroll, about 45 rows) | **D'thorga Medical**: General, Burns and smoke, Caustic gas, Emergency dosing, Triage, Save bridge, Removing |
| D'thorga Patches (the missile settings only), D'thorga Patches: Combat Extended, D'thorga Patches: Harrier, D'thorga Patches: outposts | **D'thorga Patches**: Combat Extended, Harrier jump jet, IRBM: strikes, IRBM: after-effects, Outposts (each page only while its mod is active) |
| D'thorga Factory Additions | Unchanged: two settings, shown only with Dubs Bad Hygiene; the mod does not depend on D'thorga Framework, so it cannot use the pages without gaining a dependency for two ticks (agent's judgement) |
| D'thorga Radio Framework | Unchanged: on the back burner (owner, 9 Nov 2026) |

## The audit (agent's findings, 20 Nov 2026)

Read from the source and the owner's saved settings (`python scripts/mod-settings.py`).

| Finding | Done |
| --- | --- |
| Show gases at the mouse was drawn outside dev mode, where it does nothing (FrameworkDebugActions checks dev mode) | Shown only in dev mode, under Log and reports' Developer heading |
| Also apply this mod's smoke inhalation with Combat Extended was shown without Combat Extended | Shown only with it |
| Vehicle system sounds on other mods' vehicles lived on the Framework's page, far from the vehicles | Drawn on the vehicles' Display, sound and stories page; still saved in the Framework's file, which closing the vehicle window writes too |
| "(needs a restart)" was written into two labels by hand, in a note over one section, and not at all for Parts for building | Every restart-needing row says so the same way, drawn by the page: Which vehicles (scope, per mod, per kind), Parts for building, The Lightning runs on batteries, Keep Vehicle Framework's own Health tab, Write every Def with all patches |
| The Harrier disassembly trace was a tick of its own, on by default, writing for every player | Retired into Detailed logs (owner's choice) |
| The four Patches entries were named inconsistently, and the missile one only "D'thorga Patches" | One entry |
| Three display modes (vehicles, medical, radio) | **Kept apart**: the owner's own saved settings use Engineer for vehicles and Operator for medical, so merging would take away a real choice |
| Figures only (every system) overlaps the drivetrain's "shown, speeds unchanged" | Kept: different reach (all systems against the drivetrain alone). Both now sit near the top of their pages. Worth a look if the owner finds them confusing |
| Vehicle systems post messages from about 150 places, with no way to quiet them | **The vehicle message level**, next step (below) |
| Nothing said which changes waited for a restart once the window closed | **The restart reminder**, built |

No other setting was found redundant or dead: every saved key is read by the code that it names.

## How it works

- `SettingsBook` (Framework core, `UI/SettingsBook.cs`) is one entry. A `Mod` joins it; the first to join is its host, the only one the
  game's list shows (`SettingsBookMod` returns no category for the others). Closing the window writes every member's file, and any other
  file a page touches (`AlsoWrites`).
- `SettingsPage`s are drawn into a `SettingsList`, whose rows take each setting's default. From that one figure come the changed dot,
  the Changed only list and the reset (applied over three frames, so sub-options shown only under a ticked parent are reset too). A row
  without a default (this colony's raw-materials choice) is never marked or reset.
- A search, or Changed only, draws every page in turn into one list, with each page's name and each section's heading over the rows
  that match; a search matching a page's or section's name shows all of it.
- Rows marked `restart` remember their first value; if any differ when the window closes, a message box lists them. At the main menu it
  offers the game's own restart.

## Next: the vehicle message level (designed, not built)

The owner chose "All / Important only / Letters only". From the code: of the vehicle systems' messages, 73 are refusals, 59 silent
confirmations and 24 cautions, answers to the player's own clicks, which must never be hidden (every refusal says why). The level
applies only to messages nobody asked for. Plan: one Harmony prefix on the game's `Messages.Message(Message, bool)`, which with the
level at All returns at once (no cost); otherwise, for a message posted by a vehicle systems assembly outside the GUI (the stack holds
no `Root.OnGUI` frame, so no click caused it), it hides routine news (neutral, positive, task done) at Important only, and all of it at
Letters only. Refusals, cautions, threats and letters always show. Six other mods in the owner's list already prefix that method
(Block Unwanted Minutiae, Hind and Mongoose among them); ours would skip the original only for our own messages. The prefix needs that
recorded reason, as AGENTS.md asks of a skipping prefix.
