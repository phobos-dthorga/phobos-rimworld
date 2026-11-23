# SeedDrill textures: asset record

**AI-generated, picked by the owner.** The Coulter seed drill: made on 20 Nov 2026 with OpenAI gpt-image-2 through the image API, at high
quality, attempt 2 of 2 (the owner's pick, owner's picks of 20 Nov 2026; [batch record](../../../../../../../phobos-vehicles/requests/farming-batch2-pilots-record.md)). Its only reference was our own art (the Thane T130's
standard body, the owner's pick). No game or other author's art was uploaded.

**Master:** `assets/phobos-vehicles/generated/farming-seed-drill-body-north/farming-seed-drill-body-north-2.png`, SHA-256 `08258ab26386d5c69f087372f1cc2b4aeb4c894d55b0d625233430a38fa12589`,
exported by `assets/phobos-vehicles/export_farming.py` (export_vehicle.py's body export: 4 cells long on a
4 by 5 picture, harvest green as the main paint and the yellow as the accent, saturation floor
0.1, painted by regions with flecks repaired by Segment Anything's parts map).

The background AI and Segment Anything looked at every master on the graphics card on 20 Nov 2026 (owner's go); every
fleck left in a paint mask was repainted with the paint that rings it (paint_check.fill_flecks), and each was checked tinted on dark
ground with no flecks left. East and south are the north turned, with paint masks (suffix m).

**Upgrade parts fitted onto it** (`SeedDrill_Hopper`, `SeedDrill_Wings`): AI-generated parts drawn alone and picked by the owner on
20 Nov 2026 ([parts record](../../../../../../../phobos-vehicles/requests/farming-batch2-parts-record.md)), fitted by
`export_farming.py` (`fit_part`) into boxes set by the agent from the pictures, painted by the same rules, and checked tinted on dark
ground. Their export records name each box.

- `SeedDrill_east.png`: SHA-256 `ce2f6cc9a1382893300e671ff4a6ff9f1e0f3cd7a0498b2072bea6d4fa4c644d`
- `SeedDrill_eastm.png`: SHA-256 `2c9d5a90b4f5b9e2e9dc62f16d54ff3a27c966f6e4b51d072a91c8a06a69d18f`
- `SeedDrill_Hopper_east.png`: SHA-256 `5cc9abc203b4f04bddbfd8be0ca039ee803a8c448bd490857413e2eb8c1b9faf`
- `SeedDrill_Hopper_eastm.png`: SHA-256 `bece47647508313aec34601382444bb8e51faca72bd52f4a0e631556f566eb16`
- `SeedDrill_Hopper_north.png`: SHA-256 `3fa614d1987e5ff17e0f48764c3c7983473a3dd69cda3a4d5aca0decee712940`
- `SeedDrill_Hopper_northm.png`: SHA-256 `3b8083472cae3bc295658b8512d83ca9b86854e5a91fef953e3697c01dfe7252`
- `SeedDrill_Hopper_south.png`: SHA-256 `60a6888aa81b70bcee2f2b7c8dc96a6d4cc9b6c8cd389f76d4583d49278c3a84`
- `SeedDrill_Hopper_southm.png`: SHA-256 `c2fbe41f2e20dca18875433b6b8dcf75eedb94357f62c7ff4563617fa772fc66`
- `SeedDrill_icon.png`: SHA-256 `ffd6fe754ebbee382ce80e555ba6d0861afd478c79c843c8d4b882750e3c6d8b`
- `SeedDrill_north.png`: SHA-256 `9a8533f811ddd917f5488da25e8e143729043b51d049ecd264326b544abd33c0`
- `SeedDrill_northm.png`: SHA-256 `a6fb2b3add2d5986255c70aeb4f4d640e62cf0eb8301bd2831c3b0c20f7665b1`
- `SeedDrill_south.png`: SHA-256 `6e528335bea338a1d6b80ea59cbf0d721ed37083ba51e580e715e8e8a2cc78db`
- `SeedDrill_southm.png`: SHA-256 `436d96b457e1252e8d378ede1efe3447463e3590448c7da53f1e336ea707b8c9`
- `SeedDrill_Wings_east.png`: SHA-256 `e880ccffcb3aef557aa937fdd9523e3a63d718616e563f1338c48f308ea224ac`
- `SeedDrill_Wings_eastm.png`: SHA-256 `42a5200c21840d5cbfaacbcd45c23c0ee228c5289c2a246657fc9c1933c9054f`
- `SeedDrill_Wings_north.png`: SHA-256 `fb0633c07ed69eaeaf6371b6b16062ce04da287ef2654b994182f0299f953687`
- `SeedDrill_Wings_northm.png`: SHA-256 `f09b8b623f77dda47317a5daa475ba92f3186203b24550730a97d6500faed775`
- `SeedDrill_Wings_south.png`: SHA-256 `3165a3ce27107648de6f1da7745987888a9d1754156a613975a32edb26884717`
- `SeedDrill_Wings_southm.png`: SHA-256 `1a1bf26a3d3cc609d58971fc183db62ddf57f3a68de75d0c26b62072bcd8d0ab`

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
| SeedDrill_Hopper_north | `d3cb3c72bd2af2d14cf588adb0badf042473b020af76550ae19720ec1d7a6fde` |
| SeedDrill_Hopper_east | `acec9e362d68cb7c3aafb1f5338dacd2a78577e90ef593efc1fa4a826ae3b381` |
| SeedDrill_Hopper_south | `9626943ba1f16553129da51c2d2fb8ee5cf72d8735e0ab2a4f12b2dacb171bd1` |
| SeedDrill_north | `e655eb7ca955d4d5831fe05543b7efdf2d9f7e9e4dc87bddab829b2fc060d1fe` |
| SeedDrill_east | `7f7837af2ca980ae4539f3e9b4c691590cbf0515bc7a8b57bf75f0a4164f39cb` |
| SeedDrill_south | `2028e81aca00d95927473ef058d367acc7aeadd073edf7725574b0c9b39ece6d` |
| SeedDrill_Wings_north | `74a6e9bea13863cf3acc8b6fa1cfa6f6931eaf9142dc619c5a7126d0680816fa` |
| SeedDrill_Wings_east | `951ffcb0da42a1cacfb2d61f51cacb2421b0d5d548c4999e7f1328c35d48511c` |
| SeedDrill_Wings_south | `33e0f13919b58dbba13c72527b4182c7cb8c6ad8fd17b631d6870c812018c7e9` |
