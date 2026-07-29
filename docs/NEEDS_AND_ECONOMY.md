# Needs + Economy (quick)

## Loop

See also [`GAME_LOOP.md`](GAME_LOOP.md).

1. Tutorial: name + pick Cat / Dog / Rabbit  
2. **Play** that pet’s minigame → **coins**  
3. **Shop** → buy food  
4. **Feed** → hunger + happiness up  

Coins are **not** idle income.

## Minigames → money

| Pet | Minigame |
|-----|----------|
| Cat | Fishing QTE |
| Dog | Dig garden → Snap (wrong holes fill in) |
| Rabbit | Carrot farm collect |

## Systems

| Piece | Role |
|-------|------|
| `StarterTutorial` | Welcome / name / species |
| `EconomyService` | Wallet (minigame payouts only) |
| `PetNeeds` | Hunger + happiness |
| `FoodInventory` | Buy + stock + consume |
| `MinigameRouter` | Routes to pet minigame |
| `CareHudController` | Feed / Shop / Play |

## Defaults

- Start with **0 coins**
- Kibble **8c**
- Pity crumbs on failed rounds so the tutorial loop can’t softlock
