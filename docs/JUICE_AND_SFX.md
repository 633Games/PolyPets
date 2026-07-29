# Competitive juice: graphics & sound

## Benchmark (similar games)

| Reference | Strength | PolyPets target |
|-----------|----------|-----------------|
| **VPet / desktop pets** | Cute presence, soft care SFX | Match presence; **beat** on payout juice |
| **Neko Atsume / AC-lite** | Soft ambient, gentle UI ticks | Ambient done; add denser reward audio |
| **Cookie Clicker / idle** | Number go up + click tick | Coin ladder + HUD punch |
| **JP pachislot / medal games** | Cascading dings, rising pitch, win stingers | **Design north star for earn moments** |
| **Tamagotchi / Digimon** | Short chirps on every care action | Feed / clean / room whoosh |

**Bar to clear:** after First-Time Setup, the house looks cozy (cel + dressed rooms) and **every earn/care action sings**.

## Juicy SFX bank (`Assets/Audio/Sfx/`)

Procedural project-owned clips (slot-style):

| Clip | When |
|------|------|
| `Sfx_UiClick` | Chrome buttons, tutorial next |
| `Sfx_CoinSpawn` | Idle coin appears |
| `Sfx_CoinDing` + ladder `_0…_5` | Collect / hits — rising pitch combo |
| `Sfx_CoinCascade` | Big payouts (8+) |
| `Sfx_Purchase` / `Sfx_Deny` | Shop spend / can't afford |
| `Sfx_Feed` | Bowl feed |
| `Sfx_ScrubTick` / `Sfx_CleanSparkle` | Scrub + finish |
| `Sfx_LevelUp` | Pet level |
| `Sfx_MinigameWin` | Minigame complete |
| `Sfx_Hit` / `Sfx_Miss` | Mid-minigame success/fail |
| `Sfx_ReelTick` | Nibbles / streak spice |
| `Sfx_RoomWhoosh` | Room change |
| `Sfx_Celebrate` | Tutorial pet chosen |
| `Sfx_Pop` | Extra pop on feed/collect |

Runtime: `JuicySfx` on `=== SYSTEMS ===` (12-voice pool, pitch variance, cascade coroutine).

## Feel pairing
Visual punch/pop tags still fire; SFX layers on top. Import **Feel** for MMF squash on pets.

## Mute
`JuicySfx.SetMuted(true)` / master volume on the component. Ambient stays on `AmbientAudioPlayer`.
