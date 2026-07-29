# PolyPets color & material palette (v1)

**Rule:** put object materials only in `Assets/Materials/`. There are **exactly 25**. New props/pets/food should reuse these — don’t invent one-off mats.

Rebuild anytime: **`PolyPets → Materials → Rebuild Color Palette (25 mats)`**  
(also runs during First-Time Setup / Bootstrap)

Palette asset: `Assets/ScriptableObjects/Rendering/MaterialPalette_V1.asset`

Shader: `PolyPets/CelShade` (`_BaseMap` albedo × `_BaseColor` + `_ShadeColor` + outline)

**Textures:** every mat has an albedo in `Assets/Art/Textures/` (AmbientCG photo or procedural v1). Rebuild wires `_BaseMap`.

---

## Swatches

| # | Material | Base tint | Shade | Albedo texture | Use for |
|---|----------|-----------|-------|----------------|---------|
| 1 | `Mat_Floor_WornWood` | photo×white | `#472E24` | `Photo/Tex_Floor_Wood` | Floorboards |
| 2 | `Mat_Wall_Peeling` | `#E8E0D4` | `#665C57` | `Photo/Tex_Wall_Plaster` | Room walls |
| 3 | `Mat_Trim_Dark` | `#B0A8A0` | `#1F1A1A` | `Photo/Tex_Trim_Stone` | Baseboards, poles |
| 4 | `Mat_Prop_Dusty` | `#E0D8D0` | `#383333` | `Photo/Tex_Prop_Wood` | Crates, furniture |
| 5 | `Mat_Accent_Lamp` | `#F2C773` | `#8C5933` | `Procedural/Tex_Lamp_Glow` | Lamp shade |
| 6 | `Mat_Rug_Charcoal` | photo×tint | `#161412` | `Photo/Tex_Rug_Fabric` | Rug |
| 7 | `Mat_Shadow_Blob` | `#1A1716` | `#0C0A0A` | `Procedural/Tex_Shadow` | Ground blob |
| 8 | `Mat_Bowl_Ceramic` | `#D9D0C4` | `#7A7168` | `Procedural/Tex_Ceramic_Soft` | Food bowl |
| 9 | `Mat_Metal_Dull` | photo×white | `#3E4248` | `Photo/Tex_Metal_Scratched` | Dull metal |
| 10 | `Mat_Cat_Orange` | `#DB8C47` | `#734029` | `Procedural/Tex_Cat_Fur` | Cat body |
| 11 | `Mat_Cat_Dark` | `#2E2621` | `#14100F` | `Procedural/Tex_Cat_DarkFur` | Cat details |
| 12 | `Mat_Dog_Tan` | `#B88C59` | `#66401F` | `Procedural/Tex_Dog_Fur` | Dog body |
| 13 | `Mat_Dog_Brown` | `#40301F` | `#1A140C` | `Procedural/Tex_Dog_DarkFur` | Dog details |
| 14 | `Mat_Rabbit_Cream` | `#E6D1C7` | `#8C5966` | `Procedural/Tex_Rabbit_Fur` | Rabbit body |
| 15 | `Mat_Rabbit_Rose` | `#8C5966` | `#4D2E38` | `Procedural/Tex_Rabbit_Accent` | Rabbit accents |
| 16 | `Mat_Food_Budget` | `#A68F6A` | `#5C4A33` | `Procedural/Tex_Food_Budget` | Cheap food |
| 17 | `Mat_Food_Medium` | `#D98A4A` | `#7A4020` | `Procedural/Tex_Food_Medium` | Mid food |
| 18 | `Mat_Food_Super` | `#E84B5A` | `#7A2030` | `Procedural/Tex_Food_Super` | Fancy food |
| 19 | `Mat_Coin_Gold` | `#E8C04A` | `#8C6A1A` | `Procedural/Tex_Coin_Gold` | Coins |
| 20 | `Mat_Plant_Leaf` | `#5A8F4A` | `#2E4D24` | `Procedural/Tex_Leaf_Soft` | Leaves |
| 21 | `Mat_Carrot_Orange` | `#E87A2E` | `#8C3A12` | `Procedural/Tex_Carrot` | Carrots |
| 22 | `Mat_Water_Pond` | `#4A7A8C` | `#243E4D` | `Procedural/Tex_Water_Pond` | Fishing water |
| 23 | `Mat_Dirt_Garden` | photo×white | `#2E211A` | `Photo/Tex_Dirt_Ground` | Dig soil |
| 24 | `Mat_Fish_Silver` | `#C5D0D9` | `#5A6670` | `Procedural/Tex_Fish_Scales` | Fish prop |
| 25 | `Mat_Sky_Dusk` | photo×white | `#2E2438` | `Procedural/Tex_Sky_Dusk` | Dusk accent |

---

## Mood

Warm rundown house + box pets. Photo maps are **AmbientCG CC0**; pet/food maps are procedural v1.

Swap textures by replacing files under `Assets/Art/Textures/` (same names), then **Rebuild Color Palette**.