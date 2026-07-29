# Unity file bridge (Cursor ↔ Editor)

Runtime I/O (gitignored): `cmd.json`, `out.json`, `status.json`.

```powershell
# From repo root — Unity must be open on this project
powershell -NoProfile -File .cursor/unity/Invoke-Unity.ps1 ping
powershell -NoProfile -File .cursor/unity/Invoke-Unity.ps1 hierarchy -Depth 2
powershell -NoProfile -File .cursor/unity/Invoke-Unity.ps1 get-component -Name CareHud -Type CareHudController
```

Editor script: `Assets/Scripts/Editor/CursorUnityBridge.cs`  
Agent skill: `.cursor/skills/unity-bridge/SKILL.md`
