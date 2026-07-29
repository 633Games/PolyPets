# Feel + UI Prefab Kit

## More Mountains Feel

Official docs:

- [Install](https://feel-docs.moremountains.com/how-to-install.html)
- [MMF Player / MMFeedbacks](https://feel-docs.moremountains.com/mmfeedbacks.html)
- [Feedback list (SquashAndStretch, Wiggle, Springs…)](https://feel-docs.moremountains.com/list_mmfeedbacks.html)

Feel is an **Asset Store** package (not in this git repo). Import via Hub/Package Manager → My Assets → **Feel** **before** First-Time Setup when possible, then open a demo once so dependencies install.

`PolyPets → ★ First-Time Setup` detects Feel and auto-runs **Upgrade Tags To MMF Players**. Runtime `FeelBridge` plays any `MMF_Player` safely when Feel is present (no-ops otherwise).

### FEEL[Type] tags (PolyPets)

Name any object (or use **PolyPets → Feel → Wrap Selection With FEEL[Squash]**):

| Name / tag | Behaviour |
|------------|-----------|
| `FEEL[Squash]` | Looping squash & stretch idle (breathing) — **no Animator needed** |
| `FEEL[Wobble]` | Soft position/rotation wobble |
| `FEEL[Bounce]` | Vertical bounce idle |
| `FEEL[Punch]` | Scale punch (buttons use this on click path) |
| `FEEL[Pop]` | Pop-in scale |
| `FEEL[Shake]` | Light shake |

Menus:

| Menu | Purpose |
|------|---------|
| `PolyPets/Feel/Apply Tags In Open Scene` | Scan names → attach behaviours |
| `PolyPets/Feel/Wrap Selection With FEEL[Squash]` | Feel-recommended hierarchy |
| `PolyPets/Feel/Detect More Mountains Feel Package` | Is Feel imported? |
| `PolyPets/Feel/Upgrade Tags To MMF Players` | Swap lite squash for MMF Player when Feel is present |

### Hierarchy (from Feel docs)

Scale feedbacks need a **normalized 1,1,1** container:

```
Pet_Cat_Mochi          (logic / PetAgent, scale 1,1,1)
 └─ Idle_FEEL[Squash]  (FeelIdleSquash or MMF Player target, scale 1,1,1)
     └─ Body / Head / … (any mesh scale)
```

Bootstrap already wraps the starter cat this way.

### Idle squash without an animation clip

Built-in `FeelIdleSquash` sine-breathes on `LateUpdate` (Y→XZ mass conserve, same idea as Feel’s **SquashAndStretch**).

After importing Feel, run **Upgrade Tags To MMF Players**, then on the MMF Player:

1. Add **Transform → SquashAndStretch** (if not auto-added)
2. Target = the `FEEL[Squash]` transform
3. Axis **YtoXZ**, duration ~1.6s, gentle curve
4. Timing → **repeat forever** (or Looper)
5. **Auto Play on Start**

---

## UI sprite pack + prefabs

### Generate

**PolyPets → UI → Build UI Prefab Kit + Sprite Pack**  
(also runs during scene bootstrap)

Creates:

- `Assets/ScriptableObjects/UI/UiSpritePack_Default.asset`
- `Assets/Prefabs/UI/Buttons/Btn_*.prefab` (Shop, Back, Close, Home, Adopt, Feed, Play, Clean, Settings, CollectAll, Prev/Next Room, Accept/Snooze Want, AlwaysOnTop, PauseTime, Inventory, Renovate, Minigame)
- `Assets/Prefabs/UI/Chrome/` — Top bar, Bottom bar, Modal panel, Want prompt

### Use your art

**v1 vendor set (already in repo):** see [`SPRITE_PACKS.md`](SPRITE_PACKS.md).

1. Menu: **`PolyPets → UI → Apply Vendor Sprite Pack (v1)`** (also runs during First-Time Setup)
2. Or open `UiSpritePack_Default` and assign panel / button 9-slices + icons by hand
3. Prefabs refresh from the pack via `UiChromeButton` / `UiHudRoot`

You do **not** rebuild prefabs when swapping art — just the sprite pack.

### Prefab wiring

`UiChromeButton` on each `Btn_*`:

- `buttonId` → which pack entry to read
- `spritePack` → shared pack reference
- Optional Feel punch tag for click juice

Drop prefabs into any canvas; call `SetPack` / parent under `UiHudRoot` to rebind.
