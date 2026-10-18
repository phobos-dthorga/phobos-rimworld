# Writing READMEs and in-game text

How the mods' words should read: READMEs, Workshop pages, settings, and the text players meet in the game.
It follows the Phobos Ostranauts and Phobos Kenshi guides, adapted to RimWorld.

## Who reads it

- **Players**, most of whom know nothing about radio. They want to know what to build, what it costs, and
  why a contact failed.
- **The owner**, who reads fast but finds dense text hard, and learns by re-reading. So the text does the
  reading for them:
  - put the things that bite first;
  - keep the detail, but make it easy to scan;
  - restate a key detail where it matters again instead of saying "see above".
- **Modders** extending the framework, who need exact Def fields, units and examples.

## The README shape

1. **Title and one line**: what the mod does, in the player's words.
2. **Version line**: "Version N, <date>. Not yet tested in game." until the owner says otherwise.
3. **Read this first (gotchas):**
   - requirements (Harmony, DLCs, other mods) and where it goes in the load order;
   - whether it is safe to add to and remove from a save, and what to do first;
   - settings to decide before starting a colony;
   - **whether it is tested in game.**
4. **What it does**, with a table where there are numbers.
5. **Where to find things**: research projects, Architect menu categories, traders, quests.
6. **For modders**: the Defs and fields to extend, with a short XML example.
7. **Checks done**, with dates, saying plainly what was not checked.

## Style

- **Answer first.** Short sentences, plain words.
- **Bold** for must-dos and anything the reader must not miss.
- Tables for comparisons and numbers, with units in the headers.
- Name things as the game names them: research projects, Architect categories, letters, gizmos, the
  Inspect pane.
- Say what was **not** checked as plainly as what was.

## In-game text

- **The game's own register:** plain, matter-of-fact, a little dry, sometimes darkly funny. Letters say
  what happened, what it means and what the player can do, with the letter colour the game would use (red
  for threats, yellow for caution, blue for good news, grey for information).
- **Radio voice:** factions and named contacts may have character, and real operating habits (call signs,
  signal reports, sign-offs) are welcome flavour. Keep jargon out of anything a player must act on, and
  explain a term the first time it appears.
- **Display modes:**
  - **Plain** never shows a decibel: signal bars and a reason in plain English ("the hill between you
    blocks the signal");
  - **Operator** shows SNR and margin, with band names such as "20 m";
  - **Engineer** shows the full link budget, line by line.

  The reason is the same in every mode; only the depth changes.
- **Gizmos and refusals:** every command says what it did; a disabled gizmo says why; a building's inspect
  text names the one thing it waits for (power, an antenna, a clear sky).
- **Descriptions** of radios and antennas give real, accurate specifications in a form a colonist would
  read. A description never hides a hazard or promises something the code doesn't do.
- **Creative text written by Claude** (contact stories, faction voices, letters) is labelled
  agent-written in the changelog, and held for the owner's review.
