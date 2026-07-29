# Third-party art (v1)

PolyPets ships a curated **CC0 / CC-BY** vendor set under `Assets/Art/Vendor/` so the first Unity open has real chrome + icons ready for `UiSpritePack_Default`.

## What's in the repo

| Folder | Source | License | Use |
|--------|--------|---------|-----|
| `Kenney/UIPack/` | [Kenney UI Pack](https://kenney.nl/assets/ui-pack) | **CC0** | Button / panel 9-slices (Grey + Yellow) |
| `Kenney/GameIcons/White2x/` | [Kenney Game Icons](https://kenney.nl/assets/game-icons) (+ expansion coins) | **CC0** | Generic HUD glyphs |
| `Kenney/Animals/RoundOutline/` | [Animal Pack Redux](https://kenney.nl/assets/animal-pack-redux) | **CC0** | Species picker faces (dog, rabbit, future pets — **no cat** in this pack) |
| `GameIconsNet/` | [game-icons.net](https://game-icons.net/) | **CC BY 3.0** | Thematic icons (shop, feed, fishing, dig, carrot, cat…) |

Full attribution text: [`ATTRIBUTION.md`](../Assets/Art/Vendor/ATTRIBUTION.md).

---

## Wire into Unity (after First-Time Setup)

1. Open project → wait for import of `Assets/Art/Vendor/**`.
2. Menu: **`PolyPets → UI → Apply Vendor Sprite Pack (v1)`**  
   Imports sprites as UI sprites, then fills `UiSpritePack_Default` slots.
3. Or manually: open `UiSpritePack_Default` and drag icons from `GameIconsNet/Buttons`.

First-Time Setup also calls this apply step when the vendor folder is present.

---

## Recommended mapping (buttons)

| `UiButtonId` | Primary sprite | Fallback (Kenney) |
|--------------|----------------|-------------------|
| Shop | `GameIconsNet/Buttons/shop.png` | `cart` / basket |
| Feed | `feed.png` | — |
| Play / Minigame | `play.png` / `minigame.png` | `gamepad` |
| Clean | `clean.png` | — |
| Settings | `settings.png` | `gear` |
| Home | `home.png` | `home` |
| Back | `back.png` | `return` / `previous` |
| Close | `close.png` | `cross` / `exit` |
| AcceptWant | `accept.png` | `checkmark` |
| SnoozeWant | `snooze.png` | — |
| Inventory | `inventory.png` | `basket` |
| Renovate | `renovate.png` | — |
| AlwaysOnTop | `pin.png` | — |
| PauseTime | `pause.png` | `pause` |
| Adopt | `adopt.png` | — |
| Prev/Next Room | `prev.png` / `next.png` | `previous` / `next` |
| CollectAll | `coins.png` | `coin` |

Species tutorial icons: `GameIconsNet/Pets/{cat,dog,rabbit}.png`  
Minigame badges: `GameIconsNet/Minigames/{fishing,dig,carrot_farm}.png`  
Fishing play states: `GameIconsNet/Fishing/rod_{idle,waiting,caught}.png` → also `Resources/Minigames/Fishing/`

Chrome backgrounds: Kenney `UIPack/Grey/button_rectangle_depth_gradient.png` (panel) + Yellow depth gradient (buttons).

---

## Other packs worth grabbing later (not vendored)

| Pack | Link | Why |
|------|------|-----|
| Kenney UI Pack — Adventure | https://kenney.nl/assets/ui-pack-adventure | Cozier wood/stone panels |
| Free Casual GUI (Asset Store) | https://assetstore.unity.com/packages/2d/gui/free-casual-gui-332804 | Soft casual buttons + 50 icons |
| Animal World GUI (paid) | https://assetstore.unity.com/packages/2d/gui/animal-world-gui-pack-62974 | Full animal-themed HUD if you want polish |
| Craftpix free GUI | https://craftpix.net/freebies/ | Extra free kits (check each license) |
| itch.io Animal Icons (CC0) | https://ydo4ki.itch.io/animalicons | Tiny 16×16 animal faces |

---

## License reminder

- **Kenney = CC0** — no credit required (still nice to credit kenney.nl).
- **game-icons.net = CC BY 3.0** — keep `ATTRIBUTION.md` in builds / credits screen.
