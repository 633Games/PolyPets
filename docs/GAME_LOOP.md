# PolyPets — Game Loop

## Core loop

1. **Tutorial** — Welcome → name your Poly Pet → choose **Cat / Dog / Rabbit**
2. **Play that pet’s minigame** → earn **coins**
3. **Shop** → buy **food** with coins
4. **Feed** → restore **hunger** + **happiness**
5. Repeat. Expand house / adopt more pets later.

Coins are **only** from minigames (no idle income).

---

## Starter pets & minigames

| Pet | Minigame | How you earn |
|-----|----------|--------------|
| **Cat** | **Fishing** (QTE) | Wait for **BITE**, press Space/click in the window. Several rounds. |
| **Dog** | **Dig + Snap** | Move on the garden grid, **dig**. Wrong holes **fill back in**. Dig the right tile, then **Snap** (Space). |
| **Rabbit** | **Carrot Farm** | Run the field (WASD). Carrots sprout (`c`) then grow (`C`) — step on them to collect before time runs out. |

---

## Tutorial copy (shipped)

1. **Welcome to PolyPets**
2. **Name your Poly Pet**
3. **Pick a pal** — Cat / Dog / Rabbit (with minigame blurbs)

---

## Systems map

```
StarterTutorial → Spawns greybox pet + FEEL[Squash]
MinigameRouter → Fishing | DigSnap | CarrotFarm → EconomyService (+coins)
FoodInventory ← Shop spend coins
PetNeeds ← Feed food / minigame complete
```
