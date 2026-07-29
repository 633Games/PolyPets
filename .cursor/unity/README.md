# Unity file bridge (Cursor ↔ Editor)

Runtime I/O (gitignored): `cmd.json`, `out.json`, `status.json`, `captures/*.png`.

```powershell
# From repo root — Unity must be open on this project
powershell -NoProfile -File .cursor/unity/Invoke-Unity.ps1 ping
powershell -NoProfile -File .cursor/unity/Invoke-Unity.ps1 hierarchy -Depth 2
powershell -NoProfile -File .cursor/unity/Invoke-Unity.ps1 screenshot -View scene
powershell -NoProfile -File .cursor/unity/Invoke-Unity.ps1 screenshot -View game
powershell -NoProfile -File .cursor/unity/Invoke-Unity.ps1 click -X 240 -Y 600 -Filter topleft
powershell -NoProfile -File .cursor/unity/Invoke-Unity.ps1 exec -Menu "Edit/Play"
powershell -NoProfile -File .cursor/unity/Invoke-Unity.ps1 get-component -Name CareHud -Type CareHudController
```

| Command | Notes |
|---------|--------|
| `screenshot -View game\|scene\|both\|window\|camera` | Default **game/scene = Windows desktop blit** of that editor pane (real pixels + Overlay UI). `camera` = Unity render path. |
| `click -X -Y` | Play Mode only. UI EventSystem first, then physics. `-Normalized` for 0–1. `-Filter topleft` for top-left origin. |
| `focus -View game\|scene` | Focus editor window |
| `select -Name …` | Select + ping hierarchy object |
| `gameview` | Report Game View size |
| `rebuild-hud` | Apply cozy HUD silently + save scene (edit mode) |
| `refresh` | `AssetDatabase.Refresh` — `-Name force` or `-Name scripts` for harder reload |
| `exec -Menu …` | Allowlisted menus + any `PolyPets/…` |

Editor script: `Assets/Scripts/Editor/CursorUnityBridge.cs`  
Capture helper: `Assets/Scripts/Runtime/Debug/CursorUnityCaptureRunner.cs`  
Agent skill: `.cursor/skills/unity-bridge/SKILL.md`
