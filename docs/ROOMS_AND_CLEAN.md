# Rooms, décor & cleanliness

## Pets never die
Neglect only makes them **hungry, unhappy, and dirty**. There is no death / permadeath state.

Meters: **Hunger · Happiness · Cleanliness**

---

## Clean scrub
1. Press **Clean**
2. Click-drag on the pet
3. Cel shader shows a **dirt overlay**; scrubbing paints a mask that wipes it away
4. Cleanliness + happiness recover; Esc or full clean ends the session

Shader props on `PolyPets/CelShade`: `_DirtMap`, `_DirtAmount`, `_ScrubMask`, `_ScrubGlow`

---

## Rooms (demo)
| Room | Id | Notes |
|------|-----|--------|
| Living Room | `living_room` | Sofa, coffee table, floor lamp, plant, window |
| Kitchen | `kitchen` | Counter + sink, shelf jars, fruit bowl |
| Bedroom | `bedroom` | Bed, nightstand lamp, wardrobe |
| Garden | `garden` | Open fence, raised beds, pond, lantern |

Bootstrap dresses each room via `RoomBeautyBuilder` (ceiling, trim, window/sky, multi-part props, warm `RoomLamp`). Re-run **First-Time Setup** to regenerate.

Use **◀ Room / Room ▶** to switch. Pet follows the active room.

Each room has **3 décor slots**.

---

## Décor shop
**Décor** button → buy items with minigame coins → auto-place in the active room.

Placed decorations stack:

| Effect | What it does |
|--------|----------------|
| Happiness passive | Soft cheer per minute |
| Happiness decay multiplier | Slower mood drop |
| Coin earn bonus | Multiplies minigame payouts (house-wide) |

Catalog is created by `DecorationCatalogFactory` under `Assets/ScriptableObjects/Decorations/`.
