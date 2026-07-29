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

1. Cast / wait  
2. `~ nibble ~` events (pressing early spooks the fish)  
3. `!!BITE!!` reaction window  
4. Reel phase with FISH / BAR / CATCH meters  
5. 2 rounds → coins  

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
| Fishing | — | Space/Click (bite + hold to reel) |
| Dig+Snap | WASD | Space dig / snap |
| Carrot Farm | WASD | Auto-harvest on ready tile |
