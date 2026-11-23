# PloughBody textures: asset record

**Drawn in code, never drawn in game** (20 Nov 2026, D'thorga Farming batch two; redrawn 21 Nov 2026). The Sillion plough is
reversible, so both its sides are drawn as overlays (`Plough` and `Plough_Turned`, the owner's picks), one at a time by the side that is
down (D'thorga Framework's node looks, the state "turned"); the body under them must show nothing, or both sides would show at once.

**Why it is not simply empty** (the fault-read of 21 Nov 2026): the first body was a fully transparent 64 by 64 picture, and Combat
Extended, which measures every pawn kind's body picture by its pixels at a quarter alpha or more (`BoundsInjector.ExtractBounds`,
`Def_Extensions.CropVertical`), threw an error at every start-up on a picture with none, and gave the plough no size. The game's cutout
shaders draw only pixels at half alpha or more.

**How it is made**, with Pillow: each of `Plough_north.png`, `Plough_east.png` and `Plough_south.png` (the plough's first side, the
owner's pick) is shrunk to 64 by 64, every pixel above half alpha is set to alpha 77 (three tenths), and the rest to nothing. So the
body is the plough's own shape: Combat Extended can measure it, and the game does not draw it. **To look at in play:** that nothing
faint shows under the plough. Its paint masks (suffix m) are black, painting nothing; the body is patterned only so the overlays take
the plough's colours.

- `PloughBody_east.png`: SHA-256 `8f6c0dd58a3b966b1bdfab843fa7d206c9913c4328896d1370c01c57a7ea68b3`
- `PloughBody_eastm.png`: SHA-256 `334a66740ff2739817852a78eb03e1cff35da608515457567c739af831d0379d`
- `PloughBody_north.png`: SHA-256 `1b94b8db63f63a75cc87f05400478e218734569aaf1375d9a3e624bf02d1adce`
- `PloughBody_northm.png`: SHA-256 `334a66740ff2739817852a78eb03e1cff35da608515457567c739af831d0379d`
- `PloughBody_south.png`: SHA-256 `5639f29d3a7796c9b0029d2a298a6dcd851821768728068de2a37b0d9cb9c80d`
- `PloughBody_southm.png`: SHA-256 `334a66740ff2739817852a78eb03e1cff35da608515457567c739af831d0379d`
