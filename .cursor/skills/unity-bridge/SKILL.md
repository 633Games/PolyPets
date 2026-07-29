---
name: unity-bridge
description: >-
  Talk to the live Unity Editor for PolyPets via the file bridge
  (.cursor/unity). Use when inspecting hierarchy, console logs,
  CareHudController refs, running PolyPets menu items, or debugging
  scenes without reading full YAML / Editor.log / screenshots.
---

# PolyPets Unity Bridge

## When to use

- Live scene hierarchy / GameObject find
- Serialized component field snapshot (null refs)
- Recent console lines (filtered)
- Run allowlisted `PolyPets/...` menu items
- Confirm editor is alive and which scene is open

**Do not** read full `.unity` YAML, huge `Editor.log`, or capture screenshots for routine inspection.

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
| `ping` | Editor alive, open scene, play/compile flags |
| `hierarchy` | Depth-limited named tree + child counts + key comps |
| `find` | By name substring and/or component type |
| `get-component` | Serialized field snapshot for a named object |
| `log` | Last N console lines (ring buffer), optional filter |
| `exec` | `EditorApplication.ExecuteMenuItem` (allowlisted) |
| `menus` | List safe menu paths |

### Examples

```powershell
powershell -File .cursor/unity/Invoke-Unity.ps1 ping
powershell -File .cursor/unity/Invoke-Unity.ps1 hierarchy -Depth 2 -Limit 50
powershell -File .cursor/unity/Invoke-Unity.ps1 find -Type CareHudController
powershell -File .cursor/unity/Invoke-Unity.ps1 get-component -Name CareHud -Type CareHudController
powershell -File .cursor/unity/Invoke-Unity.ps1 log -Limit 20 -Filter CareHud
powershell -File .cursor/unity/Invoke-Unity.ps1 exec -Menu "PolyPets/UI/Apply Cozy Companion HUD"
```

Options: `-Name`, `-Type`, `-Filter`, `-Menu`, `-Depth`, `-Limit`, `-TimeoutSec`, `-Raw`.

## Protocol (manual)

1. Write `.cursor/unity/cmd.json` (flat JSON for Unity `JsonUtility`):

```json
{"id":"abc123","cmd":"ping","name":"","query":"","type":"","component":"","filter":"","menu":"","depth":0,"limit":0}
```

2. Unity polls (~0.35s), deletes `cmd.json`, writes `.cursor/unity/out.json`.
3. Match `out.id` to your `id` before trusting the result.

## Code

- Editor: `Assets/Scripts/Editor/CursorUnityBridge.cs`
- Menus: `PolyPets/Cursor Bridge/Ping`, `Open Bridge Folder`

## Common PolyPets menus (exec)

- `PolyPets/UI/Apply Cozy Companion HUD`
- `PolyPets/Bootstrap Starter House Scene`
- `PolyPets/Select Starter Scene`
- `PolyPets/★ First-Time Setup (run this)`

Use `menus` for the full allowlist.
