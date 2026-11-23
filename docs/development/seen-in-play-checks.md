# Seen-in-play checks: figures, order, parts and staleness (18 Nov 2026)

**Status (18 Nov 2026):** A design record, at the owner's request ("write up the design record, please, and then get it built").
Built: N1 and E1 to E8 in phobos-modkit 1.19.0 and 1.19.1, D'thorga Framework 0.138.0 and D'thorga Companion 0.9.0 (two shelves, To
look into and Notes with no row); not yet tested in game or on screen. Where the build differs from the design is under "As built".

The seen-in-play notes (D'thorga Framework 0.118.0; owner, 17 Nov 2026) let the owner play however they like while the game records
what it saw working, and phobos-modkit's test-pass and D'thorga Companion's Tests page turn that into a test list waiting for the
owner's word. The owner likes it because it "plays well with my spontaneity". This record sharpens the evidence without asking
anything more of the owner during play.

How Framework services works now: [systems/framework-services.md](systems/framework-services.md); this record keeps its history.

## Why: two finds the notes held but nothing flagged (18 Nov 2026)

Both were found only by reading the notes by eye:

- **A hollow sighting.** Row 149 ("the grid runs a parked vehicle", about a kilowatt with the turret working) was flagged seen on 25
  notes, every one "0 W": a full battery floating on the grid, about 0.09 W. Nothing compared the figure with what the row expects.
  Framework 0.137.3 raised the note's threshold; the general fault, a note's figure never checked, remains.
- **A fault in the order of events.** The Abrams refused to light its turbine five times with no warning before it, because its only
  warning sat below the turbine's no-light line. That broke the owner's rule of 12 Nov 2026, a warning before a vehicle is stranded.
  Fixed in Framework 0.137.3; nothing would have caught it.

## What the notes cannot do today

| Limit | Effect |
| --- | --- |
| **Once a session per key** | A stage's notes share a key (`vehicles.stage.turbineStart` for both "barely enough to light" and "too weak to light"), so only the first is ever written in a session, and a second vehicle never gets its own |
| **The tool keeps one note per key** | test-pass keeps the count and the last note's detail; every other figure and vehicle is lost |
| **Figures only in words** | "M2A3 Bradley: 0 W" can be read by a person, not checked |
| **Generic keys** | Row 121 (a thrown track) was flagged by an engine stop; row 124 (turret power) by the Systems tab being drawn |
| **No sense of age** | A note from 0.129.0 still flags a row whose behaviour changed in 0.137.3 |

## What each part answers for (owner's rule of 8 Nov 2026)

These are testing tools, not play: nothing here changes a colony. **Lets the owner** see which sightings are hollow, which rows are
covered in part, and when a sequence the design promises went wrong in play. **Stops** a hollow or stale sighting standing as
evidence. **Changes the way they test** only on the Tests page and, if they switch it on, with a message in dev mode when a listed
test has just happened.

## The design

### N1. Notes say what they were seen on, which case, and their figures

A note line gains four optional fields; the format stays 1, as readers ignore fields they do not know.

| Field | Holds | Example |
| --- | --- | --- |
| `on` | A stable name for the thing within its save | `"M1A2Abrams1234"` |
| `kind` | What kind of thing it is | `"M1A2Abrams"` |
| `variant` | Which case of the feature (a stage's label) | `"too weak to light"` |
| `values` | Figures, by name, as numbers | `{"W": 960, "loadsA": 30, "chargingA": 4}` |

**Once a session now means once per key, case and thing**, so the warning and the refusal are both written, for every vehicle. A
quantity's stage notes carry the stage as their case. The grid hook-up's note carries its watts, loads and charging as figures.
*Agent default:* the thing's own ID for `on` and its def for `kind`.

### E1. Expected figures

A row's evidence may say what its figures should be: `"expect": {"W": ">= 300"}` (forms `>= N`, `> N`, `<= N`, `< N`, `= N` and
`A..B`). A part is **seen as expected** when a note meets every expectation, and **seen, figures off** when notes came but none
did; the report shows the figures it saw. A row whose only sightings are off is not marked seen working.

### E2. Order rules

`order` rules say one note must come before another on the same thing: "for this vehicle, the warning before the refusal". For each
`then` note, a `first` note on the same `on` must come before it, after any earlier `then` on that thing. A broken rule is listed
with the thing, the time and the row it belongs to. Notes without `on` (written before N1) cannot be checked and are counted as such.

*Agent's first rules:* the piston warning before the piston refusal, and the turbine warning before the turbine refusal (row 69).

### E3. Rows in parts, with narrower evidence

A row's evidence may be split into named parts: `{"parts": {"wet brakes": [...], "air filter": [...]}}`. The old form, a list of
keys, stays and means one part. The report says "2 of 3 parts seen" and names them; apply's sentence names the parts seen.

Each piece of evidence is a key, or an object narrowing it: `variant` (a case), `detail` (a wildcard on the detail, such as
`"*track*"`), `expect` (E1).

### E4. One generic note, one claim

A key with no narrowing that stands for more than one row must be listed under `shared`, with why. The gate (`tests/test_test_evidence.py`)
refuses any other: a generic note standing for two claims is how rows 121 and 124 were flagged on nothing.

### E5. Stale sightings

A row says which versions it was written for ("(Framework 0.120.0)", "Framework 0.137.3: ..."). A sighting from an older version of
the same mod than the newest the row names is **seen before the row last changed**: listed apart, and not marked seen working.
`versionNames` in the evidence map says how each mod is written in rows (`"D'thorga Framework": "Framework"`). A name directly after
another capitalised word ("Vehicle Framework 1.7.2537") is someone else's and is ignored.

### E6. How widely it was seen

Each row says on how many kinds of thing its notes came (`kind`, or the detail's first words for older notes). For Framework features
that every vehicle shares, "seen on 1 kind" and "on 6 kinds" are different evidence.

### E7. Notes with no row

The evidence map's `unrowed` lists keys that need no row, with why. On the Tests page each note with no row gets two buttons, **Make a
row** and **No row needed**, written to Companion's own folder (`test-key-decisions/<repository>.jsonl`) beside its verdicts. test-pass
reads them: "no row needed" leaves the key out, "make a row" lists it as a request for the next agent session.

### E8. A test-moment message (dev mode, off by default)

With dev mode on and the new setting **Say when a listed test happens** on (D'thorga Framework, under the seen-in-play setting), the
first time a session sees a note for a row still waiting, a message in the corner says so: "Test list row 69: the battery's warnings
(M1A2 Abrams: barely enough to light)". The player can glance at it while it is on screen. It names a test row, so it is dev mode only,
never ordinary play text.

The row map reaches the game through `scripts/deploy.py`, the one install helper: it writes the rows' keys and titles into the
installed D'thorga Framework (`TestRows.json`), never into the repository's mod folder, so nothing ships on the Workshop. Without
the file the message is not shown. *Limit:* the map is as fresh as the last install, so a row passed since still nudges until then.

## The evidence map after this change

```json
{
  "rows": {
    "149": {"parts": {
      "the turret on the grid": [{"key": "vehicles.grid-hook-up", "expect": {"loadsA": ">= 5", "W": ">= 100"}}],
      "charging only": [{"key": "vehicles.grid-hook-up", "expect": {"loadsA": "< 5", "chargingA": "> 0"}}]
    }},
    "121": {"parts": {
      "a tank's thrown track": ["vehicles.track-thrown", {"key": "vehicles.stopped-by-damage", "detail": "*track*"}]
    }}
  },
  "order": [
    {"name": "a turbine warns before it cannot light", "row": "69",
     "first": {"key": "vehicles.stage.turbineStart", "variant": "barely enough to light"},
     "then": {"key": "vehicles.stage.turbineStart", "variant": "too weak to light"}}
  ],
  "shared": {"vehicles.turret-hand-cranked": "rows 124 and 137 both test a gun cranked by hand"},
  "unrowed": {},
  "versionNames": {"D'thorga Framework": "Framework"}
}
```

## What goes where

| Part | phobos-modkit (test-pass, core) | phobos-rimworld | D'thorga Companion |
| --- | --- | --- | --- |
| N1 | Reads the new fields; keeps every note | Framework writes them; once per key, case and thing | Shows them |
| E1 to E3, E5, E6 | Evaluates them; report, JSON and apply | The evidence map | Shows parts, figures, order faults, staleness and breadth |
| E4 | | The gate's test | |
| E7 | Reads `unrowed` and the decisions file | The evidence map | Two buttons; writes its decisions |
| E8 | | Framework's message and setting; deploy writes the map | |

## As built (18 Nov 2026)

- **Stages and radar warnings are narrowed by their detail** ("*: low air", "*: LockedOn"), not by their case, so notes written
  before 0.138.0, which have no case, still count. Notes still carry the case, which keeps a warning and a refusal apart in a session.
- **Row 149 is judged by its figures alone**: its two parts are the turret on the grid (loads 20 A and 500 W or more) and charging
  (1 A or more), agent defaults. Its older notes have no figures, so they show as "figures off", which is the honest reading of 0 W.
- **apply takes back its own "seen working" sentence** when no note read stands for its row any more (modkit 1.19.1), never from a row
  with a verdict and never when no notes were read. Its first run took back rows 69, 121, 124 and 149.
- The test rows reach the game as a tab-separated file (`TestRows.tsv`), which the Framework reads without a JSON library.
- **Agent's first map:** rows 69, 74, 140, 143, 149 and 160 in parts; rows 121 and 124 narrowed; three order rules (row 69's two
  warnings, row 74's low air before the brakes lock); `vehicles.turret-hand-cranked` shared by rows 124 and 137.

## Limits

- **A note still never passes a row.** Only the owner's word does; every check here only says a sighting is weaker than it looked.
- **"Looked right" is not automated**: a light's pool, a sound's level and a message's wording stay the owner's judgement.
- Notes written before N1 have no `on`, `variant` or `values`; they still count as sightings, but cannot meet an expectation or be
  checked for order.
- A case's figures are the first of its session: a later, larger draw on the same vehicle in that session is not written.
