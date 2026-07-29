# PolyPets color & material palette (v1)

**Rule:** put object materials only in `Assets/Materials/`. There are **exactly 25**. New props/pets/food should reuse these — don’t invent one-off mats.

Rebuild anytime: **`PolyPets → Materials → Rebuild Color Palette (25 mats)`**  
(also runs during First-Time Setup / Bootstrap)

Palette asset: `Assets/ScriptableObjects/Rendering/MaterialPalette_V1.asset`

Shader: `PolyPets/CelShade` (`_BaseColor` + `_ShadeColor` + outline)

---

## Swatches

| # | Material | Base | Shade | Use for |
|---|----------|------|-------|---------|
| 1 | `Mat_Floor_WornWood` | `#72523A` | `#472E24` | Floorboards |
| 2 | `Mat_Wall_Peeling` | `#9E9480` | `#665C57` | Room walls |
| 3 | `Mat_Trim_Dark` | `#403833` | `#1F1A1A` | Baseboards, lamp pole, dark details |
| 4 | `Mat_Prop_Dusty` | `#66615C` | `#383333` | Crates, generic furniture |
| 5 | `Mat_Accent_Lamp` | `#F2C773` | `#8C5933` | Lamp shade, warm glow props |
| 6 | `Mat_Rug_Charcoal` | `#2E2A28` | `#161412` | Rug, soft dark mats |
| 7 | `Mat_Shadow_Blob` | `#1A1716` | `#0C0A0A` | Ground blob under pets |
| 8 | `Mat_Bowl_Ceramic` | `#D9D0C4` | `#7A7168` | Food bowl |
| 9 | `Mat_Metal_Dull` | `#8A8E94` | `#3E4248` | Hinges, nails, dull metal |
| 10 | `Mat_Cat_Orange` | `#DB8C47` | `#734029` | Cat body / head |
| 11 | `Mat_Cat_Dark` | `#2E2621` | `#14100F` | Cat ears / eyes / legs |
| 12 | `Mat_Dog_Tan` | `#B88C59` | `#66401F` | Dog body |
| 13 | `Mat_Dog_Brown` | `#40301F` | `#1A140C` | Dog snout / ears / legs |
| 14 | `Mat_Rabbit_Cream` | `#E6D1C7` | `#8C5966` | Rabbit body |
| 15 | `Mat_Rabbit_Rose` | `#8C5966` | `#4D2E38` | Rabbit ears / accents |
| 16 | `Mat_Food_Budget` | `#A68F6A` | `#5C4A33` | Cheap kibble / cans |
| 17 | `Mat_Food_Medium` | `#D98A4A` | `#7A4020` | Mid-tier food |
| 18 | `Mat_Food_Super` | `#E84B5A` | `#7A2030` | Fancy food |
| 19 | `Mat_Coin_Gold` | `#E8C04A` | `#8C6A1A` | Coins, reward pops |
| 20 | `Mat_Plant_Leaf` | `#5A8F4A` | `#2E4D24` | Leaves, sprouts |
| 21 | `Mat_Carrot_Orange` | `#E87A2E` | `#8C3A12` | Carrots / ready harvest |
| 22 | `Mat_Water_Pond` | `#4A7A8C` | `#243E4D` | Fishing water |
| 23 | `Mat_Dirt_Garden` | `#5C4333` | `#2E211A` | Dig garden soil |
| 24 | `Mat_Fish_Silver` | `#C5D0D9` | `#5A6670` | Fish catch prop |
| 25 | `Mat_Sky_Dusk` | `#6B5A7A` | `#2E2438` | Soft dusk / night backdrop accent |

---

## Mood

Warm rundown house + box pets. Accents are **lamp gold**, **coin gold**, and species hues — not purple UI chrome, not flat cream-board layouts.

## Code

```csharp
var palette = /* MaterialPalette_V1 */;
palette.GetPetPair(PetSpecies.Cat, out var primary, out var secondary);
var floor = palette.floorWornWood; // or palette.Get("Mat_Floor_WornWood")
```
