# PolyPets

A low-poly desktop Tamagotchi × idle clicker for PC — by **633 Games**.

Care for box-headed pets, earn currency, buy rooms, and play short minigames in a small always-on-top window.

**Engine:** Unity **6.3 LTS** (`6000.3.x`) · URP · cel shade · day/night · Feel tags · UI prefab kit  
**Studio:** [633 Games](https://github.com/633Games)

## Docs

- **[Getting Started](docs/GETTING_STARTED.md)** — clone, Hub open, First-Time Setup
- **[Unity Interact skill](.cursor/skills/unity-interact/SKILL.md)** — agent play/stop/refresh/logs (minimal tokens)
- **[Startup Sanity](docs/STARTUP_SANITY.md)** — playable checklist after First-Time Setup
- **[Studio Branding](docs/STUDIO_BRANDING.md)** — 633 Games marks & Player Settings
- **[Game Design Document](docs/GAME_DESIGN_DOCUMENT.md)** — vision, loop, pets, rooms, minigames
- **[Unity Setup](docs/UNITY_SETUP.md)** — Unity 6.3 project, cel/post, day-night, bootstrap
- **[Feel + UI Prefabs](docs/FEEL_AND_UI.md)** — `FEEL[Squash]` tags + sprite-pack buttons
- **[Needs + Economy](docs/NEEDS_AND_ECONOMY.md)** — hunger, happiness, food shop, minigame coins
- **[Game Loop](docs/GAME_LOOP.md)** — tutorial, Cat/Dog/Rabbit minigames
- **[Minigame Baselines](docs/MINIGAME_BASELINES.md)** — online reference patterns for each minigame
- **[Sprite Packs](docs/SPRITE_PACKS.md)** — vendored Kenney + game-icons for v1 UI
- **[Color Palette](docs/COLOR_PALETTE.md)** — locked 25 materials + albedo textures
- **[Rooms + Clean](docs/ROOMS_AND_CLEAN.md)** — multi-room décor, dirt scrub, no pet death
- **[Scene Beauty](docs/SCENE_BEAUTY.md)** — what First-Time Setup builds visually
- **[Juice & SFX](docs/JUICE_AND_SFX.md)** — competitive bar + pachislot-style dings
- **[Idle / Feel / Audio / Levels](docs/IDLE_FEEL_AUDIO_LEVELS.md)** — floor coins, Feel, ambient, XP

## Setup on your Unity (do this)

```bash
git clone https://github.com/633Games/PolyPets.git
cd PolyPets
git fetch origin
git checkout cursor/unity-input-setup-aad5
git pull origin cursor/unity-input-setup-aad5
bash scripts/verify-unity-project.sh
```

1. **Unity Hub → Open** → select this repo root (`Assets/` + `Packages/`).
2. Editor: **Unity 6.3 LTS** (`6000.3.6f1` or any 6000.3.x).
3. Wait for package resolve + script compile.
4. Welcome dialog → **Run setup**, or menu **`PolyPets → ★ First-Time Setup (run this)`**.
5. Optional: **`PolyPets → ★ Startup Sanity Check`**
6. Game view **480×720** → **Play**.

Input Handling is set to **Both** automatically on editor load (fixes the Input System prompt).

*(Optional)* Import **Feel** from the Asset Store before First-Time Setup for MMF wiring.

© 633 Games
