# Decisions: Medical save bridge

**Status (24 Nov 2026):** living; harvested from the save bridge record's decisions, results and open points, and the code's
comments where the record is silent. One row a decision. How it works now: [the system page](../systems/medical-save-bridge.md).
Dates are 2026. **Who**: owner; agent default or agent choice (the owner may revise either). A later row that overturns an earlier
one says so.

| Date | Decision | Who | Why | Source |
| --- | --- | --- | --- | --- |
| 3 Nov | Hook the game's own conversion points, not the mods | agent choice | One place covers every reference | [sb], Decisions |
| 3 Nov | Never patch Medical Surgery Expanded 2 | agent choice | Upgraded wounds leave it nothing to do | [sb], Decisions |
| 3 Nov | Upgrade old wounds rather than skip its re-install | agent choice | Inert after one save | [sb], Decisions |
| 3 Nov | Write the bleeding state a live wound carries | agent choice | Load defaults stop bleeding | [sb], Decisions |
| 3 Nov | Decide "new" from the save's mod list, for any mod | agent choice | Helps any part-adding mod | [sb], Decisions |
| 3 Nov | Upgrade only classes from a new mod's assembly (overturned 3 Nov) | agent choice | — | [sb], Decisions |
| 3 Nov | Upgrade any subclass with an empty constructor (overturns 3 Nov) | agent choice | Blood loss lost its severity | [sb], Decisions |
| 3 Nov | Hold the wipe by clearing the flag, not skipping the method | agent choice | The author's later changes still apply | [sb], Decisions |
| 3 Nov | Report misplaced added parts, never fix them | owner | The owner's handoff | [sb], Decisions |
| 3 Nov | Its own mod in this repository, deployed like the rest | agent default | Owner to confirm | [sb], Decisions |
| 7 Nov | Ask first: now, not now, or never for this save | owner | At the merge | [sb], For modders (moved from the README, 17 Nov 2026) |
| 7 Nov | Ask only when parts shift or the wipe looms | agent choice | Else every load asks | [sb], For modders (moved from the README, 17 Nov 2026) |
| 7 Nov | Reports to More Injuries and MSE2 drafted, the owner posts | owner | Faults every user meets | [sb], Open points and upstream |

The two 7 Nov rows on asking come from the record's modders' notes and the code (`Services/BridgeChoices.cs`, "owner's call, 7
November 2026" and "agent choice, 7 November 2026").

[sb]: ../archive/medical-save-bridge.md
