# PolyPets — agent notes

## Prefer the Unity bridge

For live editor work, use **`.cursor/unity/`** (skill: `unity-bridge`, rule: `unity-bridge`).

```powershell
powershell -NoProfile -File .cursor/unity/Invoke-Unity.ps1 ping
```

Do **not** dump full scene YAML, screenshots, or entire `Editor.log` for routine inspect/fix loops.

## Project facts

- Unity **6.3 LTS** (`6000.3.x`)
- Starter scene: `Assets/Scenes/House_LivingRoom.unity`
- Editor menus under **PolyPets/** (bootstrap, cozy HUD, URP, Feel tools)
- Runtime UI: `CareHudController`, `HudController`, `FoodShopPanel`

## Verify bridge

1. Open the project in Unity; wait for compile.
2. Console should log `[CursorUnityBridge] …`
3. `PolyPets → Cursor Bridge → Ping` or run `Invoke-Unity.ps1 ping`
4. Expect compact JSON with `scene`, `bridge`, `ok: true`
