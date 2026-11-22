# Windows down icon: two local rounds, then paid (23 Nov 2026)

**Why paid.** The icon is a button, so it went to the local night run (the owner's routing rule of 22 Nov 2026). The owner turned
down both local tries of night 10 (`berths-house.json`, windows-button-b and -c: the smoke looked trapped behind the glass) and asked
for a redraw that keeps the smoke but puts it plainly outside the cab. The local redraw ([windows-redraw.json](windows-redraw.json), 3
tries) missed: the agent gave the model the earlier try as a layout guide and it drew that try again (windows-button-d, seeds 11 and
12), and the other wording drew two grey balls in a window (windows-button-e). The near-copy check against the vanilla and house
datasets flagged none. The owner then chose a paid run for this one icon.

**What was bought.** [windows-paid.json](windows-paid.json): OpenAI image API (gpt-image-2, edits endpoint, medium quality, 1024 x
1024), two tries. Dry run ceiling US$0.30; cap US$0.30; **spent about US$0.11** (US$0.055 each, from the returned token counts).
References, our own only, for style: the Drive through the night and Meal halts icons, each with the role "match its outline weight,
shading, palette and finish only; do not copy its shape". Run report `generated/run-20261123-0204.md`.

**Review (agent).** Both tries exported at 128 px as shipped, shown at three times without smoothing and at actual size on soil and
stone: no white fringe, holes or loose specks. Both draw what was asked: an open cab window with a cloud of smoke pouring out and up
above the door. The agent leaned to try 2, whose darker column of smoke reads most plainly at actual size.

**Decision (owner, 23 Nov 2026, by card): try 2**, shipped as `Windows.png` in D'thorga Framework 0.237.1. Try 1 and the five local
misses (b, c, d-11, d-12, e) are archived on `art-references`.
