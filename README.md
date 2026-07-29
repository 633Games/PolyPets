# PolyPets

A low-poly desktop Tamagotchi × idle clicker for PC: care for box-headed pets, earn currency, buy rooms, and play short minigames — all in a small always-on-top window.

**Engine:** Unity **6.3 LTS** (`6000.3.x`, 2026) · URP · cel shade · day/night · Feel tags · UI prefab kit

## Docs

- **[Game Design Document](docs/GAME_DESIGN_DOCUMENT.md)** — vision, loop, pets, rooms, minigames
- **[Unity Setup](docs/UNITY_SETUP.md)** — Unity 6.3 project, cel/post, day-night, bootstrap
- **[Feel + UI Prefabs](docs/FEEL_AND_UI.md)** — `FEEL[Squash]` tags + sprite-pack buttons
- **[Needs + Economy](docs/NEEDS_AND_ECONOMY.md)** — hunger, happiness, food shop, minigame coins
- **[Game Loop](docs/GAME_LOOP.md)** — tutorial, Cat/Dog/Rabbit minigames
- **[Minigame Baselines](docs/MINIGAME_BASELINES.md)** — online reference patterns for each minigame
- **[Sprite Packs](docs/SPRITE_PACKS.md)** — vendored Kenney + game-icons for v1 UI
- **[Color Palette](docs/COLOR_PALETTE.md)** — locked 25 materials + albedo textures in `Assets/Materials/` / `Art/Textures/`
- **[Rooms + Clean](docs/ROOMS_AND_CLEAN.md)** — multi-room décor, dirt scrub shader, no pet death
- **[Scene Beauty](docs/SCENE_BEAUTY.md)** — what First-Time Setup builds visually
- **[Idle / Feel / Audio / Levels](docs/IDLE_FEEL_AUDIO_LEVELS.md)** — floor coins, Feel import, ambient loop, XP

## When you get home (fresh clone)

Full walkthrough: **[Getting Started](docs/GETTING_STARTED.md)**.

```bash
git clone https://github.com/633Games/PolyPets.git
cd PolyPets
git checkout cursor/minigame-baselines-da80   # until merged to main
```

1. Open in **Unity Hub** with **Unity 6.3 LTS**.
2. *(Recommended)* Import **Feel** from the Asset Store / Package Manager first.
3. Wait for packages + compile.
4. Menu: **`PolyPets → ★ First-Time Setup (run this)`**
5. Press **Play** at **480×720**.
