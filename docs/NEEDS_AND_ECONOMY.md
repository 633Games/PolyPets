# Needs + Economy (quick)

## Loop

1. Tutorial → pick species  
2. Minigame → coins (+ XP)  
3. Idle floor coins drip (max 10; click to collect)  
4. Shop → Budget / Medium / Super **species food**  
5. Click **bowl** to feed  
6. Clean scrub / décor for hygiene & house buffs  
7. Levels raise idle drip + minigame coin %  

## Food tiers

| Tier | Cost | Restore |
|------|------|---------|
| Budget | 6c | low |
| Medium | 12c | mid |
| Super | 22c | high |

Cat / Dog / Rabbit each have their own three tiers.

## Systems

| Piece | Role |
|-------|------|
| `FoodShopPanel` | Species shop UI |
| `PetFoodBowl` | Click to feed |
| `PetNeeds.IsFull` | Blocks feeding when full |
| `FoodInventory` | Buy + stock per item |
| `MinigameRouter` | Coin income + XP |
| `IdleCoinSpawner` | Floor coin drip (cap 10) |
| `PetProgression` | Levels / XP |
| `AmbientAudioPlayer` | Looping house ambience |
