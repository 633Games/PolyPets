# PolyPets project map

Use this instead of scanning the whole repo.

## Layout

```
Assets/Scripts/Runtime/   # gameplay (UI, pets, house, minigames, economy…)
Assets/Scripts/Editor/    # bootstrap, cozy HUD, factories, URP setup
Assets/Scenes/            # House_LivingRoom.unity (main)
Assets/ScriptableObjects/ # pets, foods, UI sprite pack
Assets/Materials/         # locked 25-color palette
Assets/Art/UI/            # including Cozy/ rounded sprites
docs/                     # design + setup (source of truth for features)
```

## Scene roots (`House_LivingRoom`)

| Root | Role |
|------|------|
| `=== SYSTEMS ===` | `GameBootstrap`, economy, inventory, minigames, day/night, tutorial |
| `=== ENVIRONMENT ===` | House + rooms |
| `=== CHARACTERS ===` | Spawned pets |
| `=== LIGHTING ===` | Key light + volume |
| `=== CAMERAS ===` | Main camera + `HouseCameraController` |
| `=== UI ===` | `HUD_Canvas`, care HUD, shop, tutorial |

## Key scripts (by feature)

| Feature | Scripts |
|---------|---------|
| Boot | `Core/GameBootstrap.cs`, `Editor/PolyPetsSceneBootstrap.cs`, `Editor/PolyPetsFirstRun.cs` |
| Camera | `Camera/HouseCameraController.cs` — respect `reframeOnStart` / `ReframeOnStart` |
| Day/night | `Rendering/DayNightCycle.cs` — throttled Apply, soft shadows |
| Care HUD | `UI/CareHudController.cs`, `UI/HudController.cs`, `UI/NeedMeterView.cs` |
| Cozy look | `UI/CozyUiTheme.cs`, `Editor/CozyHudBuilder.cs` |
| Shop | `UI/FoodShopPanel.cs`, `Shop/FoodInventory.cs`, `Shop/FoodItemDefinition.cs` |
| Pets | `Pets/PetAgent.cs`, `Pets/StarterPetFactory.cs`, `Pets/PetMouseLook.cs`, `Pets/PetFoodBowl.cs` |
| Needs | `Needs/PetNeeds.cs` |
| Minigames | `Minigames/*` + `MinigameRouter` / `MinigameHud` |
| Tutorial | `Tutorial/StarterTutorial.cs` |

## Efficient change order

1. Confirm feature lives in Runtime vs Editor.
2. **Prefer the Unity bridge** (`.cursor/unity/Invoke-Unity.ps1`) for live hierarchy / component refs / console / PolyPets menus — not full scene YAML or Editor.log.
3. If UI looks stale or Shop missing → apply cozy HUD in **live Editor** (batchmode dialogs fail):  
   `Invoke-Unity.ps1 exec -Menu "PolyPets/UI/Apply Cozy Companion HUD"`
4. Verify bindings: `Invoke-Unity.ps1 get-component -Name CareHud -Type CareHudController`
5. Persist scene in Editor; play at **480×720**; verify shadows + camera framing unchanged.

## Unity bridge

| Path | Role |
|------|------|
| `Assets/Scripts/Editor/CursorUnityBridge.cs` | Editor poller |
| `.cursor/unity/Invoke-Unity.ps1` | Cursor-facing CLI |
| `.cursor/skills/unity-bridge/SKILL.md` | Agent instructions |
| `.cursor/unity/status.json` | Heartbeat (gitignored) |
