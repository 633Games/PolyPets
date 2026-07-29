# Getting started (home / fresh clone)

## What you need

1. **Unity Hub** + **Unity 6.3 LTS** (`6000.3.6f1` or any 6000.3.x)
2. This repo (see branch note below)

## Clone & open

```bash
git clone https://github.com/633Games/PolyPets.git
cd PolyPets
git checkout cursor/minigame-baselines-da80   # until PRs land on main
```

Open the folder in **Unity Hub → Open →** select the repo root (the folder with `Assets/` + `Packages/`).

Wait for package resolve + script compile (first open can take a few minutes).

## One script sets up the environment

After compile finishes:

**Menu → `PolyPets` → `★ First-Time Setup (run this)`**

That single menu:

1. Ensures **URP** pipeline assets  
2. Builds the locked **25 materials** in `Assets/Materials/` ([palette](COLOR_PALETTE.md))  
3. Builds UI sprite pack + materials + volume profile  
4. Applies vendored **Kenney / game-icons** art into `UiSpritePack_Default` (see [`SPRITE_PACKS.md`](SPRITE_PACKS.md))  
5. Creates **`Assets/Scenes/House_LivingRoom.unity`** — cel-shaded greybox living room, lights, day/night, HUD, tutorial, minigames, shop  
6. Opens the scene  

On a fresh clone, Unity may also pop a **Welcome to PolyPets** dialog offering the same setup.

## Play

1. Set Game view to **480×720** (or Play — window controller requests that size)
2. Press **Play**
3. Tutorial: welcome → name pet → pick **Cat / Dog / Rabbit**
4. Play minigame → earn coins → Shop → click bowl to feed

## If something’s missing

| Symptom | Fix |
|---------|-----|
| No `PolyPets` menu | Wait for compile / check Console for errors |
| Pink materials | `PolyPets → Ensure URP Pipeline Assets`, then re-run First-Time Setup |
| Empty scene | Run `★ First-Time Setup` again |
| Want the scene file only | `PolyPets → Select Starter Scene` |

Full visual stack notes: [`UNITY_SETUP.md`](UNITY_SETUP.md).
