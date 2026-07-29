# Scene beauty (bootstrap)

After **`PolyPets → ★ First-Time Setup`**, the house should already feel like half the game — warm cel-shaded rooms, not empty greybox boxes.

## What setup builds
- Closed rooms: floor, walls, **ceiling**, front apron, baseboards, crown trim
- Back **window** with sky-dusk glass + mullions (indoors)
- Garden: dirt floor, fence posts, sky backdrop, pond, raised beds
- Multi-part props: sofa, bed, counters, plants, crates, floor lamps
- Per-room warm **RoomLamp** point lights (driven by day/night)
- Soft shadow blobs under pets & furniture
- Tiled photo albedos (wood / plaster / rug / dirt)
- Stronger bloom / vignette / warm grade on the volume profile

## Rebuild
Re-run First-Time Setup or **Bootstrap Starter House Scene** anytime — rooms are generated in code (`RoomBeautyBuilder`).

## Tuning
| Piece | Where |
|-------|--------|
| Furniture layout | `Assets/Scripts/Editor/RoomBeautyBuilder.cs` |
| Materials / tiling | `MaterialPaletteFactory` + `docs/COLOR_PALETTE.md` |
| Post look | `PostProcessFactory.PopulateProfile` |
| Camera framing | `HouseCameraController` |
