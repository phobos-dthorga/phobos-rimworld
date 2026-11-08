# In-game test pass (9 Nov 2026)

A short list of the checks that matter most among what is built and not yet tested in game, for the owner to run in their colony
(owner's request of 9 November 2026: "a short in-game test pass list"). Three checks first, then four that are worth a look only
if play brings them up. Nothing here counts as tested until the owner says so; each result goes into that mod's changelog.

## Read this first

- **Install first, with RimWorld closed:** `python scripts/deploy.py --build --apply --all`, or say "go" and an agent runs it.
  The game holds D'thorga Framework 0.27.0 and D'thorga Patches 0.14.0; checks 1 to 3 need **Framework 0.29.0 and Patches
  0.14.1**. The other five mods already match the repository (read from the installed About files, 9 Nov 2026). **Close any Explorer
  window open in a mod folder**, or the install fails.
- **Play on a copy of the live save if you prefer**, but the Visit Settlements check needs the colony where the storm happened
  (a friendly settlement there holds a pawn that does not eat).
- **After the session, just say "done"**: the agent reads Player.log with `python scripts/triage-log.py` and the save with
  `python scripts/query-save.py`, so nothing needs copying out by hand. Say anything that looked wrong, even if it seems small.
- Dev mode is needed for checks 2 and 3 (Options, then Development mode).

## The three that matter

| # | What it proves | What to do | What passes |
| --- | --- | --- | --- |
| 1 | **Visit Settlements' food drops no longer throw every tick** (Patches 0.14.1). This storm filled the log twice on 8 November | Load the colony and play **one in-game day** at any speed | Player.log holds no repeating error from Visit Settlements' settlement events, and at most one warning a day from our guard. The agent checks it |
| 2 | **The game's log comes back after its 10,000-message limit** (Framework 0.28.0, setting on by default), and two open questions in the logging design | Dev mode on. In the debug actions menu, D'thorga group: **"Framework: log probe T2, one line through our category"**, then **"Framework: log probe T1/T4, flood past the game's limit"**; wait a few seconds, then **"Framework: game log status"** | The game pauses for a moment while the flood is written. Afterwards the in-game log window keeps showing new messages, and the status line says it revived once. **Tell the agent whether the T2 line showed in the in-game log window**: the logging design waits on that answer |
| 3 | **The vehicle Health tab** (Framework 0.26.0) and **part conditions** (0.29.0): parts grouped as a pawn's are, the stages and refusals under them, the crew row, a condition under its part | Select one vehicle on the colony map and open **Health**; hover a few parts. Then dev mode, D'thorga group: **"Vehicle systems: hit a part"**, choose **Radiator**, and look under the radiator. Repair the vehicle. Last, form a short caravan with it and open the tab on the world map | The tab draws with no red error, hovering a part highlights it, the hit radiator shows **"Holed"** with a stage and the coolant falls on the Dashboard, the repair clears it, and the same tab opens on the world map |

## If play brings them up

| # | What | When it comes up | What passes |
| --- | --- | --- | --- |
| 4 | The Harrier over a RimPacts battle (Patches 0.11.0) | A RimPacts client state is under attack | Press **Join** first; the jet then flies sorties. Without joining, the jet's button says why it cannot go |
| 5 | Caustic gas (Medical 0.8.0: slow-acting, eye scarring, wilting plants, rinsing) | A tox gas event, or a tox weapon in a fight | Exposed pawns worsen slowly rather than at once; a pawn who rinses at a shower or eyewash improves. Rinsing at Dubs Bad Hygiene's showers is the open question |
| 6 | Machining bays reading every stack in a deep factory hopper (Factory Additions 0.11.4) | A bay waits for an ingredient that a deep input hopper holds | The bay takes it from any stack in the hopper, not only the first |
| 7 | Vehicle systems on the world map: climate control by era and bleed air (Framework 0.22.0 and 0.24.0, Vehicles 0.10.0) | A caravan travels with a vehicle in heat or cold | The cab's temperature stays comfortable, and the Systems tab shows what is running |

## Not on this list, on purpose

- The IRBM strike model (Patches 0.8.0 and 0.9.0) needs a strategic warhead to land: run it when you choose to; its bug report
  waits for that test ([outstanding work](outstanding-work.md), item J).
- The probes T3 and T5 (stream counts, timing) are for measuring later, not for this pass.
