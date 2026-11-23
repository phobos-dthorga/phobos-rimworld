# TractorT130 textures: asset record

**AI-generated, picked by the owner.** The Thane T130 tractor, standard: made on 20 Nov 2026 with OpenAI gpt-image-2 through the image API, at high
quality, attempt 1 of 2 (both attempts kept as two models, owner's picks of 20 Nov 2026; [T130 record](../../../../../../../phobos-vehicles/requests/tractor-pilot-record.md)). Its only reference was our own art (the Thane T130's
standard body, the owner's pick). No game or other author's art was uploaded.

**Master:** `assets/phobos-vehicles/generated/farming-tractor-t130-body-north/farming-tractor-t130-body-north-1.png`, SHA-256 `47247bd6b1dfc01be1de52dd793b7a99c02f51c7a90b12779a7a1463571a7e2b`,
exported by `assets/phobos-vehicles/export_farming.py` (export_vehicle.py's body export: 6 cells long on a
4 by 7 picture, harvest green as the main paint and the yellow as the accent, saturation floor
0.13, painted by regions with flecks repaired by Segment Anything's parts map).

The background AI and Segment Anything looked at every master on the graphics card on 20 Nov 2026 (owner's go); every
fleck left in a paint mask was repainted with the paint that rings it (paint_check.fill_flecks), and each was checked tinted on dark
ground with no flecks left. East and south are the north turned, with paint masks (suffix m).

**Upgrade parts fitted onto it** (`TractorT130_Duals`, `TractorT130_Tank`): AI-generated parts drawn alone and picked by the owner on
20 Nov 2026 ([parts record](../../../../../../../phobos-vehicles/requests/farming-batch2-parts-record.md)), fitted by
`export_farming.py` (`fit_part`) into boxes set by the agent from the pictures, painted by the same rules, and checked tinted on dark
ground. Their export records name each box.

- `TractorT130_Duals_east.png`: SHA-256 `752ed18920421dbb951b531ba0f1b74551101caca2a10a4368d227b52f7cb768`
- `TractorT130_Duals_eastm.png`: SHA-256 `a8b7ea94a61518e8733495ac68ee13f09fe2a7d6e36127aa7e72cec7c06c41c4`
- `TractorT130_Duals_north.png`: SHA-256 `2323fea601f1b5837907aad0f337a89ca2e8deb218a293aa269ea43025778dca`
- `TractorT130_Duals_northm.png`: SHA-256 `61d54554c14ae5b987cc7a9d1d07bae1fbdca7fddb87e5a52ff7480a0a55815d`
- `TractorT130_Duals_south.png`: SHA-256 `e73b7f279267111e3a50cde0a0e48228bb27cee06b978b5a7f73bf50fff9f764`
- `TractorT130_Duals_southm.png`: SHA-256 `61d54554c14ae5b987cc7a9d1d07bae1fbdca7fddb87e5a52ff7480a0a55815d`
- `TractorT130_east.png`: SHA-256 `cb44cab2a64780351860528e62e0c515c48c36b9b4c34c9dc6aa5375ffdbeea8`
- `TractorT130_eastm.png`: SHA-256 `7e18bdaa7140eb58d04a4f71bc121b3e5eb1edab31e213de6ee3aa56c5875978`
- `TractorT130_icon.png`: SHA-256 `ac09dc5b7674509d40262f5aeab541b6d96533b92859eb0ef22c79c98b321a84`
- `TractorT130_north.png`: SHA-256 `06c8f02d646809540ee8e44c2148b1170397972b78ba8f7b94480c353b07d5cd`
- `TractorT130_northm.png`: SHA-256 `479d75183306f13e5b33cb0f3e1afabd2c4e9168383154a78342183c448a4ffa`
- `TractorT130_south.png`: SHA-256 `3b115094f22ac32346f44bd1f400d10da6c05498cad45285e287add92463c27a`
- `TractorT130_southm.png`: SHA-256 `7e582e9fdf03b5883319ebc9a8a2ef5398f52b2a397c407b1d6beebbcce139ae`
- `TractorT130_Tank_east.png`: SHA-256 `86ca0854ab21b5c2692d94006af4a77964b973e010d564931688089b66c78565`
- `TractorT130_Tank_eastm.png`: SHA-256 `404da26a123b69ff8435256bb9994f3fbab1a86590d02d4cec01af770fe6fcd7`
- `TractorT130_Tank_north.png`: SHA-256 `f9cbfa53d3825723cec4a102c500aaca1d11fa66c3d0da86c16f3923a740d3d8`
- `TractorT130_Tank_northm.png`: SHA-256 `bd76a00d10cc7b1f1e85204bac43c36975244023dc6389fba45337f43d2378f3`
- `TractorT130_Tank_south.png`: SHA-256 `432cd049eae037d1908eb42cbb7e99cdf083135016e4fa92f8230ab8fa207789`
- `TractorT130_Tank_southm.png`: SHA-256 `1022fae82b2c3916ed00a8f8781cd73c21b5d4e1866ae2975b4407ebc685944e`

## Own colours, no paint, clean rims (23 Nov 2026)

**Made from the shipped pictures, not from the master**, D'thorga Farming 0.7.7 (the owner's choices of 23 Nov 2026: no paint for
Farming's machines, and the white dithering at the borders fixed). `python scripts/art/bake_paint.py assets/phobos-vehicles/farming-unpainted.json
--apply`: each painted picture became what the game drew in its own colours (the grey texture times this vehicle's Def colours under the
mask, overlays and halves in their vehicle's colours), its mask was removed, and every partly transparent pixel on its outer edge paler
than the pixel inside it took that pixel's colour, keeping its transparency (the pale rim left by exports before the soft-rim fix,
5c6d04e8). Nothing moved: outlines, positions and overlays' fits are as before. East and south are the north turned. No game or other
author's art was used.

| Texture | SHA-256 |
| --- | --- |
| TractorT130_Duals_north | `f0c1ea674b9ff9baafcbb028c0d7e20d0552a9d4d077852c1f26f0d2161bca6d` |
| TractorT130_Duals_east | `d8960918d5a3bdd43d7b19cd1e9647741bce69bc3cdc87de4fb05f36f5d3986e` |
| TractorT130_Duals_south | `d4e90b29b0dd8e7647666c6a18f63d5780d01475051ed3c70de4f62434bfad15` |
| TractorT130_north | `1335b9eeb4616c4643c46141cfc067a9ade7537192716ad32dc027d6f7b4a209` |
| TractorT130_east | `f4e82614aa75598e4c6a4a606d645c7aa510fcaa90b2e0138a6587aa394a88d8` |
| TractorT130_south | `d75258b3240cd255203df1b46eb1430dd920054ed09ac8fb84eb6ef32013c71d` |
| TractorT130_Tank_north | `149d3d70650e1c27ee790d1d7fd6cd5cd4ef2657027fbb300e3f29497a2c09cd` |
| TractorT130_Tank_east | `9dd490724268b0908bbd248c098db6a4f8ab513760cc1991b4174f02edb2f98d` |
| TractorT130_Tank_south | `24716fa6f1750056dde1f4b5f33c108cd2ce792df0f793d119954ed79538a7e3` |
