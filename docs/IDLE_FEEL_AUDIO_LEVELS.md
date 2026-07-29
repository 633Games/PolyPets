# Idle coins, Feel, audio & levels

## Idle floor coins
- Coins spawn on the **active room** floor and **spin**
- Click to collect into the wallet
- **Max 10** coins in the room at once — drip pauses until you pick some up
- Drop rate scales with pet **level**, happiness/cleanliness, and décor coin bonus

## Feel (Asset Store)
1. Import **More Mountains Feel** via Package Manager → My Assets **before** First-Time Setup  
2. Run **`PolyPets → ★ First-Time Setup`** — auto-upgrades `FEEL[Squash]` to MMF Players when Feel is detected  
3. Or later: **`PolyPets → Feel → Upgrade Tags To MMF Players`**

Built-in lite squash still works if Feel is missing. Runtime `FeelBridge` safely no-ops without Feel.

## Ambient audio
On Play, `AmbientAudioPlayer` loops:
- `Assets/Audio/Ambient/Amb_CozyHouse_CC0.ogg` (house room tone, CC0)
- `Assets/Audio/Ambient/Amb_SoftPad_Proc.ogg` (soft pad underlay)

Mute via the AudioSource on `=== SYSTEMS ===` / AmbientAudioPlayer.

## Juicy SFX (pachislot bar)
`JuicySfx` plays medal dings, cascades, win stingers, UI ticks on every care/earn action.  
See [`JUICE_AND_SFX.md`](JUICE_AND_SFX.md). Re-run First-Time Setup to wire clips.

## Pet levels
`PetProgression` on each pet:
| Action | XP |
|--------|-----|
| Minigame | ~12–22 |
| Clean session | ~8–16 |
| Feed | 5 |
| Idle coin pickup | 2 |

Levels raise idle drip rate and add a small minigame coin %. HUD shows `Lv X  xp/need`.
