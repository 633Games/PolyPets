# PolyPets — Game Loop

## Core loop

1. **Tutorial** — Welcome → name your Poly Pet → choose **Cat / Dog / Rabbit**
2. **Play that pet’s minigame** → earn **coins**
3. **Shop** → buy **species food** (Budget / Medium / Super)
4. **Click their bowl** to feed (or Feed button)
5. If **full**, they refuse food until hunger drops
6. Repeat

Coins are **only** from minigames (boosted by room decorations).

Pets **never die** — they only get hungry, sad, and dirty. Use **Clean** to scrub dirt off their material.

---

## Starter pets & minigames

Baselines and detailed rules: [`MINIGAME_BASELINES.md`](MINIGAME_BASELINES.md).

| Pet | Minigame | How you earn |
|-----|----------|--------------|
| **Cat** | **Fishing** | AC nibbles → **BITE** → Stardew hold/release reel bar |
| **Dog** | **Dig + Snap** | Dig garden; wrong holes fill in; Snap the pop |
| **Rabbit** | **Carrot Farm** | WASD harvest ready carrots before they spoil |

---

## Food shop

Each species has its own food line:

| Tier | Price | Hunger restore (approx) |
|------|-------|-------------------------|
| **Budget** | 6c | +18 |
| **Medium** | 12c | +34 |
| **Super** | 22c | +55 |

- Shop filters to the **active pet’s species** (cat food / dog food / rabbit food)
- Bowl uses the best owned food for that species
- **Full** (hunger ≥ ~92): bowl won’t feed — wait for hunger to drop

---

## Tutorial copy

1. Welcome to PolyPets  
2. Name your Poly Pet  
3. Pick Cat / Dog / Rabbit  
