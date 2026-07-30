---
name: unity-interact
description: Control a running Unity Editor with minimal tokens — play/stop/pause, refresh assets, read status JSON and Editor.log tails, optional Game-view screenshots. Use whenever interacting with Unity, debugging Play Mode, checking console errors, or reloading assets after code/art changes.
---

# Unity Interact (minimal tokens)

Prefer the CLI + status file over dumping Console, taking videos, or narrating the whole Editor UI.

## Prerequisites

- PolyPets project open in **Unity 6.3** (bridge auto-loads with scripts).
- Bridge: `Assets/Scripts/Editor/PolyPetsAgentBridge.cs`
- CLI: `bash scripts/unity-interact.sh <cmd>`

IPC folder (gitignored): `Temp/PolyPetsAgent/`
- write `cmd.txt` → editor runs command
- read `status.json` → tiny play/compile state

## Token budget (strict)

| Need | Do this | Avoid |
|------|---------|--------|
| Alive / play state | `status` | Full Console dump |
| Compile done | `wait-ready` | Polling screenshots |
| Errors | `errors` (≤60 lines) | Entire `Editor.log` |
| Recent log | `log 40` | `log 2000` |
| Reload assets | `refresh` | Restarting Unity |
| Start/stop game | `play` / `stop` | Clicking via computer-use |
| Visual check | 1 screenshot of Game view only | Screen recordings unless user asks |

Default reply after a command: **one status JSON line + ≤15 error lines** if any. Do not paste full logs into chat.

## Commands

```bash
bash scripts/unity-interact.sh status
bash scripts/unity-interact.sh wait-ready 90
bash scripts/unity-interact.sh play
bash scripts/unity-interact.sh stop
bash scripts/unity-interact.sh pause
bash scripts/unity-interact.sh unpause
bash scripts/unity-interact.sh refresh    # Assets → Refresh
bash scripts/unity-interact.sh ping
bash scripts/unity-interact.sh setup      # FirstTimeSetupBatch
bash scripts/unity-interact.sh log 40
bash scripts/unity-interact.sh errors 60
bash scripts/unity-interact.sh focus-game
```

Aliases: `reload`/`assets` → `refresh`; `resume` → `unpause`.

## Workflows

### After editing scripts/assets
1. `refresh` (if Unity did not auto-import)
2. `wait-ready`
3. `errors` — fix only if real errors
4. `play` → check `status` → `errors` → `stop`

### Debug a Play Mode bug
1. `play`
2. `errors` + `log 40` (not more)
3. If UI/layout needed: one Game-view screenshot (`focus-game` first), else stay on logs
4. `stop`

### Fresh project / missing scene
1. Open project in Hub (Unity 6.3)
2. `wait-ready` then `setup` **or** welcome **Run setup**
3. `status` should show a scene path after setup

## status.json fields

`playing`, `paused`, `compiling`, `updating`, `focused`, `lastCmd`, `lastResult`, `lastError`, `scene`, `t`

If `status` missing: Unity is not open on this project (or scripts not compiled yet).

## Screenshot / computer-use policy

- Use only when logs cannot answer (layout, pink materials, missing UI).
- Prefer Game view; do not capture whole desktop.
- Never start a long screen recording for routine play/stop checks.

## Manual menu fallbacks

`PolyPets → Agent → Play | Stop | Refresh Assets | Write Status Now`
