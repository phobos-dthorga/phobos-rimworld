# Round three: the Pintle trailer, the tipper's grab, the winch and the long-range tank (12 Nov 2026, evening)

**Owner's direction (12 Nov 2026):** "let's do #1", the Pintle trailer's own picture, which had borrowed the tipper's body since 11 Nov,
and "I agree with your suggestions on #2": the tipper's loading grab (no picture since round two) and the agent's proposal to show the
recovery winch and the auxiliary fuel tank on our own trucks. Everything through the OpenAI image API under
[the paid rules](../../../docs/development/paid-image-generation-rules.md). Shipped in D'thorga Framework 0.105.0 and D'thorga Vehicles
0.49.0. **Not yet tested in game.**

## Read this first

- **Picks (owner, from labelled sheets at in-game size):** trailer body **2**, greedy boards **2**, loading grab **3**, winch **1**,
  auxiliary tank **2**.
- **Spend: US$0.619 for 13 images**, under caps that totalled US$1.45. Nothing refused by moderation; nothing timed out.
- **The near-copy check was wrong about the tank, and was fixed** (below). The owner's words: "There is no tank like it in the original
  game, there is something wrong with that bloody script once more." They were right, as with the cable on 12 Nov.
- **A second flaw is recorded, not fixed:** flat patches score as matching flat patches. It is listed in
  [outstanding work](../../../docs/development/outstanding-work.md) with what was tried.

## Runs

| Run | Request file | Images | Cap | Spent |
| --- | --- | --- | --- | --- |
| Trailer pilot | `trailer-pilot.json` | 4 | US$0.45 | US$0.167 |
| Grab, winch and tank | `fittings-overlays.json` | 7 | US$0.75 | US$0.374 |
| Greedy boards (after the trailer pick, rule 2) | `trailer-overlays.json` | 2 | US$0.25 | US$0.078 |
| **Total** | | **13** | | **US$0.619** |

Each dry run passed before its real run. Sizes came back as asked: 768 x 1536 for the trailer and boards (2 times the trailer's drawn
length of 768 px, above the 1.5 times rule), 1536 x 1024 for the grab and winch, 1024 x 1536 for the tank. Every reference was our own
picked art: the tipper body for the trailer and grab, the bulk tanker body for the winch and tank, the picked trailer for the boards.

**Sizing (rule of 9 Nov):** no trailer is active on the owner's list (RimArrow's 3 x 10 MAZ trailer is downloaded, not active). The
trailer keeps its 2 x 5 footprint (owner, 11 Nov: one medium flatbed), the generator set's width; our trucks that tow it are 2 x 6 to
3 x 10.

## Review at in-game size

| Picture | Agent's review | Decision |
| --- | --- | --- |
| Trailer 1 | Straight overhead; tandem axles just behind the middle, as the follow rule wants (axle 0.2 cells behind the picture's middle) | Not picked |
| Trailer 2 | The same, with cross-members across the deck; 1.54 cells wide | **Picked** |
| Trailer 3 | The plainest and widest, 1.72 cells | Not picked |
| Trailer 4 | Axles spread to front and back, not a tandem pair: it would read oddly swinging on one pivot | Not picked; agent advised against |
| Greedy boards 1, 2 | Clean rectangular frames; from straight above, taller boards show only as their top rails, so a frame is right | **2 picked** |
| Grab 1, 2, 3 | All fit the strip behind the cab; 1 has the clearest jaws, 3 the chunkiest arms | **3 picked** |
| Winch 1, 2 | Both clean; 1 has the bigger hook and reads better at in-game size | **1 picked** |
| Tank 1, 2 | Clean; 2 paler with a clearer filler cap | **2 picked**, after the check was fixed |

## Fitting them (free)

- **The trailer body:** `scripts/art/export_vehicle.py body --cells-long 5 --draw 3 6`. The cut-out cleared only white it could reach from
  the edge, so the white inside the drawbar's triangular frame stayed solid. **New: `--hole X Y`** on `body` and `overlay` names
  background a shape encloses (`white_to_alpha(holes=...)`, with a test); the trailer used four points, the boards one.
- **Everything else:** `assets/phobos-vehicles/export_round_three.py`, reusing batch B's helpers. Boxes are given in in-game pixels and
  turned into each master's pixels. The winch is the pick turned half a turn (hook forward), the boards are the pick stretched to the
  deck's rails, each as a new file. **Agent placements**: the winch overlaps the front edge of the bonnet a little, because each body
  master reaches almost to the top of its canvas; the tank hangs between the wheels on the left, in line with their outer edge.
- **Which trucks:** the tanker, survey truck, drill rig and tipper. The generator set lists its own parts and has no winch mount or tank
  cradle; the trailer has no engine; the Crawler is not drawn with a tank this round.
- **The code:** D'thorga Framework's node looks gained a `fitting` field (Framework 0.105.0); a slot may now be shown by a fitting as well
  as by an upgrade node, checked when the fittings' change count moves (one comparison on the rare tick).

## The near-copy check: what was wrong, and the fix

**What it does:** each picture is cropped to its subject, **stretched to a square**, and compared with every game sprite by a 64-bit
difference hash and by a structural score; either one under its threshold flags it.

**What was wrong:** stretching to a square throws away a subject's proportions. A long thin tank (0.19 wide to tall) and the game's
*wide* 1 x 3 altar (3.1 wide to tall) both became the same square of plain shading, whose eight hash rows repeat one another, so they
matched at 7 bits. The structural score was only 0.34: nothing drawn was alike.

**Tried first, and reverted:** padding each crop to a square instead of stretching it. It made things far worse (155 flags against about
20), because the padding leaves most of the hash blank, and every tall thin subject matched the game's upright books at 0 bits.

**The fix (`scripts/art/sort_rules.py`, `near_copy.py`, with tests):** the hash counts only between subjects of about the same
proportions, within 1.35 times (authored). The structural score is unchanged.

**How it was checked:** 48 deliberate copies of 12 game sprites (exact pastes, 4-degree tilts, heavy blurs, recolours by channel swap).
They stayed within 1.13 times of their source's proportions; every hash-only flag that was false was 1.96 times or more apart. **The
fixed check catches 44 of the 48, exactly as the old one did**, missing the same four tilted copies (the tool's notes already say a
slight tilt can pass, so the owner's eye stays the last check). Against both nights' real pictures (61), tonight's no longer flag. The
reports are in `.local/art/near-copy-recheck/` (`2026-11-12-round-three.json` before, `2026-11-12-proportions.json` after).

**The second flaw, found on the way and not fixed:** this re-check compared against the larger set of 241 game sprites, which last
night's did not, and it flagged three pictures already shipped: the generator set's cable out 1 (structure 0.68 against the game's grey
"select carried pawn" button), and the drill rig's diamond bit 1 and hammer 1 (hash only, against a crater and an icon of the same round
proportions). **Looked at side by side, none is anything like its match.** The cause: the game's flat grey button squares score about
0.94 a patch against blank white, because the structural score treats any two flat patches as alike. Counting only patches with detail
cleared every false flag, but **lost the recoloured copy of the Go-juice sprite**, which is caught only through its flat patches, so it
was not adopted. A fix needs another idea (comparing in colour rather than grey, or judging flat patches by their difference in
brightness), and is listed in outstanding work.

## Archive

- `art-masters` **99784f5**: the five picks with their records, and the derived files the export used (the turned winch, the shaped
  boards, each pick on its body's canvas).
- `art-references` **ab1e4c7**: the eight attempts not picked, with their records. Nothing was truly fugly; nothing was left out.
- Derived previews of the unpicked tank and boards, made only for the review sheet, were deleted: they were resizes, not originals.
