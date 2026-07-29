# Needs + Economy (quick)

## Loop

1. **Play** (Fishing — Space/click on bite) → **coins**
2. **Shop** → buy food with coins
3. **Feed** → hunger up, happiness up
4. Skip meals → hunger drops → happiness drops faster

Coins are **not** idle income.

## Systems

| Piece | Role |
|-------|------|
| `EconomyService` | Wallet (minigame payouts only) |
| `PetNeeds` | Hunger + happiness decay/restore |
| `FoodItemDefinition` | Kibble / Fish Bowl / Catnip Cookie |
| `FoodInventory` | Buy + stock + consume |
| `MinigameRouter` + `FishingMinigame` | Earn coins |
| `CareHudController` | Feed / Shop / Play wiring + meter labels |

## Defaults

- Start with **0 coins** (earn first)
- Kibble **8c**, restores hunger
- Fishing catch **12c** (perfect **+8**); miss still gives **2c** pity so the loop can't softlock
