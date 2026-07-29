# PolyPets — Agent guide

Desktop Tamagotchi × idle clicker. **Unity 6.3 LTS** · URP · 480×720 always-on-top window.

**Canonical repo only:** [633Games/PolyPets](https://github.com/633Games/PolyPets). Never link, clone, or push any other PolyPet repo.

## Read first

| Need | Where |
|------|--------|
| Fast map + menus | [`.cursor/PROJECT_MAP.md`](.cursor/PROJECT_MAP.md) |
| Current WIP / traps | [`.cursor/KNOWN_ISSUES.md`](.cursor/KNOWN_ISSUES.md) |
| Live Unity (ping / hierarchy / logs / menus) | [`.cursor/unity/README.md`](.cursor/unity/README.md) · `Invoke-Unity.ps1` |
| Design / loop | [`docs/GAME_DESIGN_DOCUMENT.md`](docs/GAME_DESIGN_DOCUMENT.md), [`docs/GAME_LOOP.md`](docs/GAME_LOOP.md) |
| Setup | [`docs/GETTING_STARTED.md`](docs/GETTING_STARTED.md) |

## Hard constraints (learned in play)

- **Do not reframe the camera on Play** unless `HouseCameraController.reframeOnStart` is true.
- **Keep soft sun shadows** — disabling them “for perf” removes shadows on Play.
- **Throttle `DayNightCycle`** visual updates; do not run full Apply every frame.
- **Pets spawn facing the camera** (`RoomRoot` uses ~180° yaw).
- **Shop must stay wired** on `CareHudController.shopPanel` + dock `UiChromeButton` refs; rebuild via `PolyPets → UI → Apply Cozy Companion HUD`.
- Prefer editing **our** scripts under `Assets/Scripts/`. Avoid `Assets/Feel/` (vendor) and never commit `Library/`, `Logs/`, `Temp/`.

## Unity menus that matter

1. `PolyPets → ★ First-Time Setup (run this)` — full greybox scene
2. `PolyPets → Bootstrap Starter House Scene` — rebuild scene
3. `PolyPets → UI → Apply Cozy Companion HUD` — HUD/shop only (keeps house/camera)

## Git

Author for this account: **633Games** `<292611213+633Games@users.noreply.github.com>`. Commit only when asked. No Cursor co-author trailers.
