# CultivatorLight textures: asset record

**AI-generated, picked by the owner.** The Tillman cultivator, light: made on 20 Nov 2026 with OpenAI gpt-image-2 through the image API, at high
quality, attempt 2 of 2 (both attempts kept as two models, owner's picks of 20 Nov 2026; [batch record](../../../../../../../assets/phobos-vehicles/requests/farming-batch2-pilots-record.md)). Its only reference was our own art (the Thane T130's
standard body, the owner's pick). No game or other author's art was uploaded.

**Master:** `assets/phobos-vehicles/generated/farming-cultivator-body-north/farming-cultivator-body-north-2.png`, SHA-256 `92c19ac663f3621c6126a2d2f15bc6d6662f046bbd344971b57dd56006aa37b1`,
exported by `assets/phobos-vehicles/export_farming.py` (export_vehicle.py's body export: 3 cells long on a
4 by 4 picture, harvest green as the main paint and the yellow as the accent, saturation floor
0.1, painted by regions with flecks repaired by Segment Anything's parts map).

The background AI and Segment Anything looked at every master on the graphics card on 20 Nov 2026 (owner's go); every
fleck left in a paint mask was repainted with the paint that rings it (paint_check.fill_flecks), and each was checked tinted on dark
ground with no flecks left. East and south are the north turned, with paint masks (suffix m).

**Upgrade parts fitted onto it** (`CultivatorLight_Wings`): AI-generated parts drawn alone and picked by the owner on
20 Nov 2026 ([parts record](../../../../../../../assets/phobos-vehicles/requests/farming-batch2-parts-record.md)), fitted by
`export_farming.py` (`fit_part`) into boxes set by the agent from the pictures, painted by the same rules, and checked tinted on dark
ground. Their export records name each box.

- `CultivatorLight_east.png`: SHA-256 `0d241f18112dade055037431aaf9587f104a25eba7a67f40d8666dfadd94f360`
- `CultivatorLight_eastm.png`: SHA-256 `e2889f8c8ca558e5aad07b80f408f49ba6e03abd9358a7f1b7abe321e15c467f`
- `CultivatorLight_icon.png`: SHA-256 `a76e40711b2cf6a936db9ba0a2797b69748f7d826f8e0a963a1664f397669cbb`
- `CultivatorLight_north.png`: SHA-256 `f0f2169d34a48faaf1710a5bf431802071978fa1f54579616e7c6718945b77bd`
- `CultivatorLight_northm.png`: SHA-256 `2b676e63e4b3de53b7636ef5bfeff552a31b7d6c03ed59a3b4ee60eef2c8b157`
- `CultivatorLight_south.png`: SHA-256 `74b37c60a04882ff3178c0289b543db3a61c45c1dddf6f81f330c49d2ce3c6c6`
- `CultivatorLight_southm.png`: SHA-256 `3d346dced8c737e46e2bdf3536c8a9d03a2bc9f4399bb81df9aedfb8a9fc2f26`
- `CultivatorLight_Wings_east.png`: SHA-256 `e9116097373f4a897442f5f8cf2bd1eda1ca31650a6547a9b540e939206cbacf`
- `CultivatorLight_Wings_eastm.png`: SHA-256 `f5a663eb8d89dd4b9569dc0a2961698a62d65583039fed432a48a634628967ef`
- `CultivatorLight_Wings_north.png`: SHA-256 `766f341e493418a7840d362654467d2dae4ed768550ded89eaf321512f108bdc`
- `CultivatorLight_Wings_northm.png`: SHA-256 `86f5a3ea41fb487fde15efd262fa6a2b1213d2b38209fd1a85aa42acecfd2de6`
- `CultivatorLight_Wings_south.png`: SHA-256 `9032f9d8749afb53a2967eb408b4ba582d351a2c75ff93987874938966b2d4e9`
- `CultivatorLight_Wings_southm.png`: SHA-256 `775e8b1258e5aa401cbf2c005e2b2fe360e9bf1d04c41d419f431420da30e48c`

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
| CultivatorLight_north | `6faa43a6bf58f14d57257b500c89801118a35488855b2dc45559f9e554809bdf` |
| CultivatorLight_east | `770777946582e14d57866e5eb904d0e712c1d2a7d2d11f74d83558b4608d0d80` |
| CultivatorLight_south | `b3a90d76073259a47fd17d4b9e3238e52ec682be6f4b629ee859f231d1cce931` |
| CultivatorLight_Wings_north | `67fa8614e71be3ccb12502e2deb3c7c64f78ea02d31051c976b7d63c80f9166c` |
| CultivatorLight_Wings_east | `39ba80af855a71e667d93ee96348d12ef79d15c8dc7a2282612e8a2dae711f7f` |
| CultivatorLight_Wings_south | `003b2338a31d67a05a61b85cc31b5537493aa910eeabcc744ed14d3f61a02fb1` |
