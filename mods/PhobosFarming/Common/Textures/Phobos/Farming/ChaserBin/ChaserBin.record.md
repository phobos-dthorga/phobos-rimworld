# ChaserBin textures: asset record

**AI-generated, picked by the owner.** Made on 20 Nov 2026 with OpenAI gpt-image-2 through the image API, at medium quality: attempt 3 of
3 (owner's pick, 20 Nov 2026; [record](../../../../../../../assets/phobos-vehicles/requests/chaser-bin-pilot-record.md)). Its only
reference was our own combine body, for style. No game or other author's art was uploaded, and the near-copy check flagged none.

**Body:** `assets/phobos-vehicles/generated/farming-chaser-bin-body-north/farming-chaser-bin-body-north-3.png`, SHA-256 `66e690e26cb40cc12b99d6fec07e72a4d6960acd18311575db92403fa35c6837`,
exported by `assets/phobos-vehicles/export_farming.py` (7 cells long on a 4 by 8 picture), after the background AI looked at it on the
processor. East and south are the north turned, with paint masks (suffix m) and an icon. Checked tinted on dark ground on 20 Nov 2026.

- `ChaserBin_east.png`: SHA-256 `0600a5ad314c92089de257a7a99bfe5a0da2db50d67953de358ad72520768027`
- `ChaserBin_eastm.png`: SHA-256 `52a518bcd3432cd99ca05da0ae18f58e4de544e4ea0fbb61f2baa27aef15d058`
- `ChaserBin_icon.png`: SHA-256 `c2231c381d17bcb5e01385f11702e2ebdd4cf88798401ea4f41b0cc95c0b0680`
- `ChaserBin_north.png`: SHA-256 `4499365725d70ede08a96182b26e47efde2c0872f4f9ba357253b38bf2515e27`
- `ChaserBin_northm.png`: SHA-256 `4375295d3594cd9879c100435f6b5ceefb3984d0de4fbf91e0f51ca129769302`
- `ChaserBin_south.png`: SHA-256 `1e66a5f82434421a0ec6a3da304cc45185f399d5b8ad456a7632b19e42a2ce8c`
- `ChaserBin_southm.png`: SHA-256 `001bca537205cc815228976ff781701400b3802a63f4eed7a3d31348407eea0d`

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
| ChaserBin_north | `5b423a9c30789ba951b97d51e905e13d9eae77c93826431d4dded3ecf0f37685` |
| ChaserBin_east | `5dee6b62c1054b723065f4a650cbc30311b4f0531aa20f1e49af4350eb8ca906` |
| ChaserBin_south | `1f5d2bc66795be00d848bec1e83fd467fd61b334b14d2c7b127d9720d1f33e08` |
