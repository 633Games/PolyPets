# PolyPets — Unity Setup

## Recommended project shape

| Choice | Recommendation |
|--------|----------------|
| Unity | **6000.x** or **2022.3 LTS** |
| Pipeline | **URP** (3D) |
| Platform | Windows Standalone first |
| Resolution | **480×720** windowed (tall companion) |
| Quality | Low/Medium, no HDR, MSAA off for the house cam |

This repo ships the `Assets/` scripts and an **Editor bootstrap**. It does **not** ship a full Unity project (`ProjectSettings/`, Library, URP asset). Create a Unity project, then drop these assets in.

---

## First-time setup

1. Create a new Unity project: **3D (URP)**.
2. Copy this repo’s `Assets/Scripts`, `Assets/Scenes` (after bootstrap), and other `Assets/*` folders into the Unity project’s `Assets/` (or open this repo as the project root after Unity generates `ProjectSettings`).
3. Wait for script compile.
4. Menu: **PolyPets → Bootstrap Starter House Scene**.
5. That creates and saves:
   - `Assets/Scenes/House_LivingRoom.unity`
   - `Assets/Prefabs/Pets/Pet_Cat_Mochi.prefab`
   - `Assets/ScriptableObjects/Pets/PetDefinition_Cat.asset`
   - Greybox materials under `Assets/Materials/`
6. **File → Build Settings** → add `House_LivingRoom`.
7. Game view aspect: fixed **480×720** (or free aspect and resize).
8. Press Play. You should see the rundown room, box-headed cat, and HUD.

### Extra menu actions

| Menu | What it does |
|------|----------------|
| `PolyPets/Bootstrap Starter House Scene` | Full scaffold (safe to re-run on a new empty scene flow) |
| `PolyPets/Select Starter Scene` | Ping the saved scene asset |
| `PolyPets/Frame Camera On Active Room` | Re-apply cozy 3/4 framing |

---

## What the bootstrap builds

```
=== SYSTEMS ===          GameBootstrap, DesktopWindowController
=== ENVIRONMENT ===      House + Room_LivingRoom (floor/walls/props + anchors)
=== CHARACTERS ===       Pet_Cat_Mochi (box head + low-poly body primitives)
=== LIGHTING ===         Key + fill directional, warm lamp point, flat ambient
=== CAMERAS ===          HouseCamera (FOV 32, dark clear color, HouseCameraController)
=== UI ===               EventSystem + HUD_Canvas (480×720 reference)
```

### Room anchors

- `FocusAnchor` — camera look target  
- `PetAnchor` — where the adopted pet is parented  

### Cat greybox

Cube head, ears, eyes, body, four legs, tilted tail. Swap meshes later; keep `PetAgent` on the root.

---

## Desktop companion settings

`DesktopWindowController` applies on Play:

- Windowed mode  
- Resolution **480×720** (serialized, tweakable)  
- Always-on-top **request** via `DesktopNative` (stub until Win32/Cocoa plugin)

### Player Settings (manual for now)

- Default Screen Width / Height: `480` × `720`  
- Fullscreen Mode: **Windowed**  
- Resizable Window: optional  
- Run In Background: **On** (idle earn while unfocused)

Always-on-top needs a tiny native helper (not in this pass). The stub logs the intent so gameplay code can call it early.

---

## Script map

| Script | Role |
|--------|------|
| `GameBootstrap` | Wires house + camera + desktop on Awake |
| `HouseController` | Room list / active room |
| `RoomRoot` | Focus + pet anchors, occupant |
| `PetAgent` / `PetDefinition` | Instance + data |
| `HouseCameraController` | 3/4 framing for tall window |
| `DesktopWindowController` | Window size / always-on-top intent |
| `HudController` | Coins + room label |
| `PolyPetsSceneBootstrap` | Editor one-click setup |

---

## Suggested next editor tools

1. **Bootstrap Kitchen Room** — second room + cow greybox + door nav  
2. **Create Pet Prefab From Selection** — standardize adopted pets  
3. **Apply Desktop Player Settings** — automate width/height/run-in-background  
4. Minigame scene templates (Fishing / Graze / Crossy)  

---

## Performance notes for a corner window

- One room active at a time (`HouseController` disables others)  
- House camera: HDR/MSAA off  
- Prefer simple Lit/Unlit materials; few realtime lights (bootstrap uses 2 directional + 1 point)  
- Idle economy should tick on a timer, not in `Update` math storms  
