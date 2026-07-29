---
name: unity-bridge
description: >-
  Talk to the live Unity Editor for PolyPets via the file bridge
  (.cursor/unity). Use when inspecting hierarchy, console logs,
  CareHudController refs, running PolyPets menu items, capturing
  Game/Scene screenshots, clicking the Game View, or debugging scenes
  without reading full YAML / Editor.log.
---

# PolyPets Unity Bridge

## When to use

- Live scene hierarchy / GameObject find
- Serialized component field snapshot (null refs)
- Recent console lines (filtered)
- Run allowlisted `PolyPets/...` menu items
- **Game View / Scene View screenshots** (then `Read` the PNG path)
- **Click / tap** UI or world objects in Play Mode
- Focus Game/Scene view; select objects; report Game View size
- Confirm editor is alive and which scene is open

Prefer bridge inspect (`hierarchy`, `get-component`, `log`) for structure; use **screenshots** when you need visual confirmation of layout, lighting, or UI.

## Prerequisites

1. Unity Editor open on this project (Unity 6.3 LTS).
2. Scripts compiled — look for console: `[CursorUnityBridge] v… watching …`
3. `.cursor/unity/status.json` exists and updates while the editor is idle.

## Invoke (PowerShell)

From repo root:

```powershell
powershell -NoProfile -File .cursor/unity/Invoke-Unity.ps1 <cmd> [options]
```

| Command | Purpose |
|---------|---------|
| `ping` | Editor alive, open scene, play/compile flags, Game View size |
| `hierarchy` | Depth-limited named tree + child counts + key comps |
| `find` | By name substring and/or component type |
| `get-component` | Serialized field snapshot for a named object |
| `log` | Last N console lines (ring buffer), optional filter |
| `exec` | `EditorApplication.ExecuteMenuItem` (allowlisted) |
| `menus` | List safe menu paths |
| `screenshot` | Capture Game and/or Scene View → `.cursor/unity/captures/*.png` |
| `click` | Play Mode click at Game View coords (UI then physics) |
| `focus` | Focus Game or Scene View window |
| `select` | Select + ping a GameObject by name |
| `rebuild-hud` | Silent cozy HUD rebuild + save |
| `refresh` | Force AssetDatabase refresh (`-Name force` / `-Name scripts`) |
| `gameview` | Report Game View pixel size |

### Examples

```powershell
powershell -File .cursor/unity/Invoke-Unity.ps1 ping
powershell -File .cursor/unity/Invoke-Unity.ps1 hierarchy -Depth 2 -Limit 50
powershell -File .cursor/unity/Invoke-Unity.ps1 find -Type CareHudController
powershell -File .cursor/unity/Invoke-Unity.ps1 get-component -Name CareHud -Type CareHudController
powershell -File .cursor/unity/Invoke-Unity.ps1 log -Limit 20 -Filter CareHud
powershell -File .cursor/unity/Invoke-Unity.ps1 exec -Menu "PolyPets/UI/Apply Cozy Companion HUD"
powershell -File .cursor/unity/Invoke-Unity.ps1 exec -Menu "Edit/Play"
powershell -File .cursor/unity/Invoke-Unity.ps1 screenshot -View scene -Path house
powershell -File .cursor/unity/Invoke-Unity.ps1 screenshot -View game
powershell -File .cursor/unity/Invoke-Unity.ps1 click -X 240 -Y 650 -Filter topleft
powershell -File .cursor/unity/Invoke-Unity.ps1 click -X 0.5 -Y 0.85 -Normalized -Filter topleft
powershell -File .cursor/unity/Invoke-Unity.ps1 focus -View game
powershell -File .cursor/unity/Invoke-Unity.ps1 select -Name ActionDock
```

Options: `-Name`, `-Type`, `-Filter`, `-Menu`, `-View`, `-Path`, `-X`, `-Y`, `-Normalized`, `-Origin`, `-Depth`, `-Limit`, `-TimeoutSec`, `-Raw`.

### Screenshot notes

- **Scene**: Scene View camera (edit or play).
- **Game in Play** (not paused): true Game View including Screen Space Overlay UI.
- **Game in Edit / paused**: main camera render — Overlay UI may be missing (`cameraFallback: true`).
- PNGs land under `.cursor/unity/captures/` (gitignored). Read the returned `path` with the Read tool to inspect visually.
- `-Limit` = max edge length in px (default 720, max 1280).

### Click notes

- Requires **Play Mode**.
- Default coords: Unity screen space (origin bottom-left). Use `-Filter topleft` or `-Origin topleft` for top-left.
- `-Normalized`: `X`/`Y` in 0–1 relative to Game View.
- `-Type ui|world|auto` (default auto): UI EventSystem first, then physics raycast.

## Protocol (manual)

1. Write `.cursor/unity/cmd.json` (flat JSON for Unity `JsonUtility`):

```json
{"id":"abc123","cmd":"ping","name":"","query":"","type":"","component":"","filter":"","menu":"","view":"","path":"","depth":0,"limit":0,"x":0,"y":0,"normalized":0}
```

2. Unity polls (~0.35s), deletes `cmd.json`, writes `.cursor/unity/out.json`.
3. Match `out.id` to your `id` before trusting the result.
4. Play-mode Game screenshots complete asynchronously (one frame later); `Invoke-Unity.ps1` waits up to 45s for those.

## Code

- Editor: `Assets/Scripts/Editor/CursorUnityBridge.cs`
- Play capture: `Assets/Scripts/Runtime/Debug/CursorUnityCaptureRunner.cs`
- Menus: `PolyPets/Cursor Bridge/Ping`, `Open Bridge Folder`, `Screenshot Scene/Game View`

## Common PolyPets menus (exec)

- `PolyPets/UI/Apply Cozy Companion HUD`
- `PolyPets/Bootstrap Starter House Scene`
- `PolyPets/Select Starter Scene`
- `PolyPets/★ First-Time Setup (run this)`
- `Edit/Play`, `Edit/Pause`, `Edit/Step`, `File/Save`

Use `menus` for the full allowlist.
