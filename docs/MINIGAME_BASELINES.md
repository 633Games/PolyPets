# Minigame baselines

Each PolyPets minigame is modeled on well-known commercial / casual game patterns, then trimmed for a small desktop companion window.

---

## Cat — Fishing

**Baselines**

| Game | What we take |
|------|----------------|
| [Animal Crossing fishing](https://nookipedia.com/wiki/Fishing) | Nibbles vs real bite. Don’t reel on nibbles. Bobber goes under → press immediately. After enough nibbles, bite is guaranteed. |
| [Stardew Valley fishing](https://stardewvalleywiki.com/Fishing) | After hook: vertical tension — hold raises green bar, release drops it. Keep bar on the fish until catch meter fills. |

**Our flow**

1. **Idle** — rod icon with no line; tap **FISH** to cast  
2. **Waiting** — rod icon with line + bobber in the water (early CATCH does nothing)  
3. **Bite / shake** — rod shakes; timing bar appears under the icon with a green good-zone in the middle and a sweeping needle  
4. Tap **CATCH**:
   - Needle in the **green zone** → catch (sure)
   - Needle on either **edge** → ~32% chance to still catch  
5. **Caught** icon (fish on the line) flashes, then next round (3 rounds total)

Icons: `Assets/Resources/Minigames/Fishing/rod_{idle,waiting,caught}.png`  
(Source + notes: `Assets/Art/Vendor/GameIconsNet/Fishing/`)

---

## Dog — Dig + Snap

**Baselines**

| Pattern | What we take |
|---------|----------------|
| Backyard dig-for-treasure / dig-a-hole casual games | Probe garden tiles; many digs are empty. |
| Whack-a-mole / “pesky moles” garden arcade | Target pops from a hole — hit it in a short reaction window before it ducks. |

**Our twist (design)**

- Wrong digs **fill back in** (garden resets empty holes).  
- Correct dig → toy pops (`*`) → **SNAP** (Space) before the timer.  
- Missed snap → ducks under, hole fills, toy re-buries elsewhere.  

---

## Rabbit — Carrot Farm

**Baselines**

| Game | What we take |
|------|----------------|
| [Farm Rush](https://play.google.com/store/apps/details?id=com.miniclip.farmrush) | Keep moving; harvest ripe produce under time pressure. |
| Carrot Farmer / Happy Harvest (itch.io) | Growth stages + interact when ready; don’t take forever. |

**Our flow**

Plots: empty → seed (`,`) → sprout (`c`) → **ready (`C`)** → spoil (`x`) if ignored.  
WASD run, step on ready carrots to harvest. Streak bonus after 3+ in a row. Timed round.

---

## Controls cheat sheet

| Minigame | Move | Action |
|----------|------|--------|
| Fishing | — | F cast · C/Space catch (timing bar) |
| Dig+Snap | WASD | Space dig / snap |
| Carrot Farm | WASD | Auto-harvest on ready tile |
