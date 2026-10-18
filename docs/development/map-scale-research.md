# Map scale research: what a cell and a world tile measure

19 Oct 2026 · agent record, for the owner's review

**Result:** Ludeon has never stated a scale, and the game's own constants give two incompatible answers:
about **1 m per cell** from how things look, or about **26 m per cell** from how fast pawns walk. For
radio on a single map the choice barely matters, so Phase 1 uses **1 m per cell** as a mod setting. A
world tile is **6 to 9 km across** by the walking and caravan anchors, which agree with each other; that
figure waits for Phase 2.

## The anchors

All game data was read from the RimWorld 1.6.4871 reference assembly and Core Defs on 19 Oct 2026.

| Anchor | Game data | Metres per cell | Metres per world tile |
| --- | --- | --- | --- |
| Visual and furniture | A pawn or a bed is one cell wide; a bed is 1 × 2 cells; a wall is one cell thick | **1** | 340 |
| Walking speed against game time | Human `MoveSpeed` 4.6 cells/s at 60 ticks/s; `GenDate.TicksPerHour` = 2500, so a pawn walks 192 cells per in-game hour; a human walks about 5 km/h | **26** | 8,900 |
| Caravan travel (the fiction) | `CaravanTicksPerMoveUtility.DefaultTicksPerMove` = 3300 ticks = 1.32 in-game hours per tile; at 5 km/h | n/a | **6,600** |
| Movement equivalence | `CaravanTicksPerMoveUtility.CellToTilesConversionRatio` = 340: crossing a tile costs the same as walking 340 cells | links the rows | |

The arithmetic:

- 4.6 cells/s × 60 ticks/s = 276 cells per 60 ticks; 2500 ticks per hour ÷ 60 = 41.7 s per in-game
  hour; 4.6 × 41.7 = 192 cells per in-game hour. At 5 km/h that is 5000 m ÷ 192 = 26 m per cell.
- 3300 ticks ÷ 2500 = 1.32 h per tile; 1.32 × 5 km/h = 6.6 km per tile.
- 340 cells × 1 m = 340 m per tile; 340 × 26 m = 8.9 km per tile.

The walking and caravan anchors are consistent with each other (6 to 9 km per tile). The visual anchor
is consistent with furniture but makes a world tile 340 m across, which cannot be a planet.

## What others concluded

- A Ludeon forum thread from 2017, "Rim-World Scaling Problems: How BIG are they?", argued for about
  20 m per cell from walking speed and tried several other assumptions. Read through search results
  only: the page refused automated reading, so this is second-hand.
- The Regions-and-societies Core-MMF project
  ([issue 67, 18 Sep 2026](https://github.com/Regions-and-societies/Core-MMF/issues/67)) weighed the
  same two anchors, adopted 1 m per cell for furniture and the caravan fiction for the tile, and came
  to about 6 km across and 23.4 km² per tile. Their reasoning is close to ours.

## What it means for radio

Free-space path loss across a map, at 146 MHz, for a 5 W hand-held (37 dBm) with 0 dBi antennas against
an FM voice floor of about −121 dBm (12.5 kHz, 10 dB noise figure, 12 dB required):

| Scale | A 250-cell map across | Path loss | Received | Margin |
| --- | --- | --- | --- | --- |
| 1 m per cell | 250 m | 64 dB | −27 dBm | about 90 dB |
| 26 m per cell | 6.5 km | 92 dB | −55 dBm | about 55 dB |

Either way, distance never decides a VHF contact on one map. Walls, natural rock, overhead mountain and
colony noise do, which is the design's on-map model. So the scale choice only changes what the
Operator and Engineer readouts say about distance.

## Decision for Phase 1

- **`metresPerCell` = 1** by default (agent default, following the visual anchor and the design's "a map
  is only a few hundred cells across"), as a mod setting. Changing it mid-save changes feedline and path
  loss, so the README lists it under settings to decide before starting a colony.
- **The world tile scale is deferred to Phase 2**, with the caravan anchor (about 6.6 km per tile) as its
  starting default and its own setting, because HF and VHF ranges between tiles depend on it.
- Both go in the design's open-questions list as answered when the owner confirms.
