# PloughBody textures: asset record

**Drawn in code, empty on purpose** (20 Nov 2026, D'thorga Farming batch two): a fully transparent 64 by 64 picture, made with Pillow,
for the Sillion plough's body. The plough is reversible, so both its sides are drawn as overlays (`Plough` and `Plough_Turned`, the
owner's picks), one at a time by the side that is down (D'thorga Framework's node looks, the state "turned"); the body under them shows
nothing, or both sides would show at once. Its paint masks (suffix m) are black, painting nothing; the body is patterned only so
the overlays take the plough's colours.

- `PloughBody_east.png`: SHA-256 `ee5f4ae0aae66a3d9ad8166933132bf2fad25da6220d8f02127f69546b5a4795`
- `PloughBody_eastm.png`: SHA-256 `334a66740ff2739817852a78eb03e1cff35da608515457567c739af831d0379d`
- `PloughBody_north.png`: SHA-256 `ee5f4ae0aae66a3d9ad8166933132bf2fad25da6220d8f02127f69546b5a4795`
- `PloughBody_northm.png`: SHA-256 `334a66740ff2739817852a78eb03e1cff35da608515457567c739af831d0379d`
- `PloughBody_south.png`: SHA-256 `ee5f4ae0aae66a3d9ad8166933132bf2fad25da6220d8f02127f69546b5a4795`
- `PloughBody_southm.png`: SHA-256 `334a66740ff2739817852a78eb03e1cff35da608515457567c739af831d0379d`
