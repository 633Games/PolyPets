# Getting started (home / fresh clone)

## What you need

1. **Unity Hub** + **Unity 6.3 LTS** (`6000.3.6f1` or any 6000.3.x)
2. This repo on the setup branch below

## Clone & open

```bash
git clone https://github.com/633Games/PolyPets.git
cd PolyPets
git fetch origin
git checkout cursor/unity-input-setup-aad5
git pull origin cursor/unity-input-setup-aad5
bash scripts/verify-unity-project.sh
```

Open the folder in **Unity Hub → Open →** select the repo root (the folder with `Assets/` + `Packages/`).

Wait for package resolve + script compile (first open can take a few minutes).

On load, PolyPets sets **Active Input Handling → Both** so the Input System package does not block UI.

## Before First-Time Setup (recommended)

1. In Unity Package Manager → **My Assets**, import **More Mountains Feel**  
2. Open a Feel demo once so dependencies resolve  
3. Then run **`PolyPets → ★ First-Time Setup (run this)`** — it auto-upgrades `FEEL[Squash]` tags when Feel is present  

If you skip Feel, built-in idle squash still works; upgrade later via **PolyPets → Feel → Upgrade Tags To MMF Players**.

## One script sets up the environment

After compile finishes, either:

- Click **Run setup** on the welcome dialog, or  
- **Menu → `PolyPets` → `★ First-Time Setup (run this)`**

That single menu:

1. Sets Input Handling to **Both** + 633 Games player branding  
2. Ensures **URP** pipeline assets  
3. Builds the locked **25 materials** in `Assets/Materials/`  
4. Builds UI sprite pack + volume profile + vendor icons  
5. Creates the house scene (4 dressed rooms, shops, clean, idle coins, ambient, levels)  
6. Upgrades Feel tags if the Asset Store pack is imported  
7. Opens the scene  

## Play

1. Game view **480×720** → **Play**
2. Tutorial → name → Cat/Dog/Rabbit  
3. Click spinning floor coins · play minigames · Shop / Décor / Clean  
4. Ambient house loop plays in the background  

More: [`IDLE_FEEL_AUDIO_LEVELS.md`](IDLE_FEEL_AUDIO_LEVELS.md) · [`ROOMS_AND_CLEAN.md`](ROOMS_AND_CLEAN.md)

## If something’s missing

| Symptom | Fix |
|---------|-----|
| No `PolyPets` menu | Wait for compile / check Console for errors |
| Input System / Active Input Handling dialog | Reopen project (auto-sets **Both**) or Edit → Project Settings → Player → **Both** |
| Pink materials | `PolyPets → Ensure URP Pipeline Assets`, then re-run First-Time Setup |
| Empty scene | Run `★ First-Time Setup` again |
| Want the scene file only | `PolyPets → Select Starter Scene` |

Full visual stack notes: [`UNITY_SETUP.md`](UNITY_SETUP.md).
