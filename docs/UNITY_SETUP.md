# PolyPets — Unity Setup

## Target editor (2026)

| Choice | Recommendation |
|--------|----------------|
| Unity | **Unity 6.3 LTS** (`6000.3.6f1`+) — this is the 2026-era editor (Unity no longer uses “2022/2023” naming) |
| Pipeline | **URP 17.3** |
| Platform | Windows Standalone first |
| Resolution | **480×720** windowed (tall companion) |
| Look | **Cel shade** + URP Volume post + **day/night** |

Pinned in-repo:

- `ProjectSettings/ProjectVersion.txt` → `6000.3.6f1`
- `Packages/manifest.json` → `com.unity.render-pipelines.universal`

Open the project with **Unity Hub → Unity 6.3 LTS**. Hub will resolve packages on first open.

---

## First-time setup

1. Install **Unity 6.3 LTS** (6000.3.x) via Hub.
2. Open this repo as a Unity project (or create a URP 3D project and merge `Assets/`, `Packages/`, `ProjectSettings/ProjectVersion.txt`).
3. Wait for package resolve + script compile.
4. Menu: **PolyPets → Ensure URP Pipeline Assets** (also runs automatically during bootstrap).
5. Menu: **PolyPets → Bootstrap Starter House Scene**.
6. Play `Assets/Scenes/House_LivingRoom.unity` at **480×720**.

### Menus

| Menu | What it does |
|------|----------------|
| `PolyPets/Ensure URP Pipeline Assets` | Creates/assigns `PolyPets_URP` + renderer (HDR, extra lights) |
| `PolyPets/Bootstrap Starter House Scene` | Full scene scaffold |
| `PolyPets/Rebuild Volume Profile` | Recreates bloom/vignette/grade overrides |
| `PolyPets/Frame Camera On Active Room` | Re-frame cozy 3/4 camera |
| `PolyPets/Select Starter Scene` | Ping the saved scene |

---

## Visual stack

### Cel shading

- Shader: `PolyPets/CelShade` (`Assets/Shaders/PolyPetsCelShade.shader`)
- Stepped lighting, warm shade tint, rim, inverted-hull outline
- Bootstrap materials under `Assets/Materials/` use this shader

### Post-processing (URP Volume)

Profile: `Assets/Settings/PolyPets_VolumeProfile.asset`

| Override | Intent |
|----------|--------|
| Bloom | Soft lamp glow |
| Vignette | Focus the small window |
| Color Adjustments | Contrast + exposure (driven by day/night) |
| White Balance | Warm day / cool night |
| Tonemapping | Neutral |
| Lift Gamma Gain | Slight punch for cel shapes |

Camera: HDR on, FXAA, `renderPostProcessing = true`.

### Day / night

`DayNightCycle` on `=== SYSTEMS ===`:

- Loops every **480s** (~8 min) by default — glanceable on a desktop companion
- Rotates **Sun_KeyLight**, eases **Fill** + **LampPoint**
- Lerps ambient + camera clear color
- Drives Volume **post exposure** + **white balance temperature**
- Phases: Night → Dawn → Day → Dusk (HUD clock shows time + phase)

Scrub `Time Of Day` on the component in Edit Mode (`editorPreview`) to art-direct lighting.

---

## What the bootstrap builds

```
=== SYSTEMS ===       GameBootstrap, DesktopWindowController, DayNightCycle
=== ENVIRONMENT ===   House + Room_LivingRoom (cel materials)
=== CHARACTERS ===    Pet_Cat_Mochi
=== LIGHTING ===      Sun + Fill + Lamp + GlobalVolume
=== CAMERAS ===       HouseCamera (HDR + post)
=== UI ===            HUD with coins / room / clock
```

---

## Desktop companion settings

`DesktopWindowController` on Play:

- Windowed **480×720**
- Always-on-top request via `DesktopNative` stub

Player Settings:

- Default Screen Width / Height: `480` × `720`
- Fullscreen Mode: **Windowed**
- Run In Background: **On**

---

## Script map

| Script | Role |
|--------|------|
| `DayNightCycle` | Time-of-day lighting + volume grade |
| `PostProcessFactory` | Volume profile defaults / camera PP enable |
| `PolyPets/CelShade` | Toon/cel lit + outline |
| `PolyPetsUrpSetup` | Pipeline asset ensure |
| `PolyPetsSceneBootstrap` | One-click scene |

---

## Performance notes

- One active room
- FXAA instead of MSAA
- HDR kept **on** for bloom (cheap at 480×720)
- Day/night updates lights each frame (fine at this scale); curves are cheap
