# Known issues / session notes

Update this when you fix or discover something durable. Keep it short.

## Open (as of 2026-07-29)

### Unity bridge JSON commas (partially fixed)

- **Symptom:** `Invoke-Unity.ps1` can time out even when Unity writes `out.json` (PowerShell `ConvertFrom-Json` fails).
- **Cause:** Manual JSON builders sometimes omitted commas before nested keys (`data`, `hits`, `tree`, …).
- **Fix:** `CursorUnityBridge` now uses `AppendCommaIfNeeded` before those keys — force a domain reload if replies still look like `"ms":1"data"`.

## Resolved (keep for regression)

| Issue | Fix |
|-------|-----|
| Cozy HUD / Shop missing from scene | `CozyHudBuilder.ApplyCozyHudBatch` applied + saved; `ActionDock` + wired `CareHudController` refs; sprites in `Assets/Art/UI/Cozy/` |
| Batchmode HUD dialog blocks apply | Use `ApplyCozyHudBatch` (no dialogs) instead of `ApplyCozyHudMenu` |
| Camera jumps on Play | `reframeOnStart` default off; `GameBootstrap` checks `ReframeOnStart` |
| Lag from sun | Throttle `DayNightCycle` visual updates |
| Shadows vanish on Play | Keep `LightShadows.Soft`; do not leave shadows None |
| Pet faces away | `RoomRoot` spawn rotation ~180° yaw |
| `Camera` namespace clash | Use `UnityEngine.Camera` in rendering scripts |
| Fishing char literal | Single char in `FishingMinigame` meter draw |
