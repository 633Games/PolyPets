# PolyPets — Game Design Document

**Working title:** PolyPets  
**Engine:** Unity (PC / Windows desktop)  
**Genre:** Desktop companion · Tamagotchi · Idle clicker · Light minigames  
**Platform:** Small always-on-top PC window  
**Art direction:** Low-poly animals with boxy heads; cozy rundown house that upgrades over time  

---

## 1. Vision

PolyPets is a tiny desktop pet house you keep open while you work or browse. You care for low-poly animals, earn currency from their idle routines and short minigames, then spend that currency to unlock new rooms and adopt more pets.

The fantasy is simple:

> Start with one boxy cat in a rundown house. Earn coins. Buy rooms. Fill the house with weirder pets, each with their own little job and minigame.

It should feel like a Tamagotchi you can glance at, mixed with an idle “idol” clicker loop, and occasional bite-sized arcade moments.

---

## 2. Design Pillars

1. **Glanceable companion** — Readable in a small window; never demands full attention.
2. **One house, growing life** — Progress is visible: more rooms, more pets, less rundown.
3. **Pets with jobs** — Every animal has a personality, preferred activity, and earning loop.
4. **Short play bursts** — Minigames are 30–90 seconds; idle income covers AFK time.
5. **Desktop-native** — Small window, always-on-top, low CPU, easy to tuck in a corner.

---

## 3. Core Loop

```
Care for pets → Pets earn idle currency → Spend on rooms / pets / upgrades
       ↑                                              ↓
   Play minigames ← Pet “wants” / requests ← New room unlocks new pet slot
```

### Session types

| Session | Length | What happens |
|--------|--------|----------------|
| Glance | 5–15 sec | Check mood bubbles, collect idle coins, click a pet |
| Care | 1–3 min | Feed / clean / fulfill a want, start one minigame |
| Progress | 3–8 min | Buy a room, adopt, upgrade furniture, rearrange |

---

## 4. Player Fantasy & Starting State

### Opening beat

- Small desktop window opens on a **rundown house**: peeling paint, sparse furniture, one lit lamp.
- Your first pet: a **box-headed cat** with a simple low-poly body.
- Soft idle animation (blink, tail flick, sit on a crate).
- One tutorial want: *“I want to go fishing…”*

### Long-term fantasy

Turn the house from a dump into a lively multi-room sanctuary packed with odd animals, each doing their thing and raining coins into your wallet.

---

## 5. Presentation & Window UX (PC)

### Window behavior

| Setting | Default | Notes |
|--------|---------|--------|
| Size | ~400×600 or 480×720 | Resizable within a small range |
| Always on top | On (toggleable) | Core feature |
| Border | Thin / custom chrome | Optional borderless + drag bar |
| Opacity | 100% (optional fade) | Later: dim when unfocused |
| Position | Remembers last corner | Snap to screen edges optional |
| Taskbar | Normal tray/taskbar icon | Minimize to tray is a stretch goal |

### Camera / view

- Fixed or lightly orbiting ¾ view of the **current room**.
- Click room tabs / door icons to switch rooms.
- Pets are readable silhouettes; avoid busy VFX that fight desktop readability.

### UI density

Keep chrome minimal:

- Top: currency, room name, always-on-top toggle  
- Bottom: pet tray / adopt / shop  
- Over pets: mood / want speech bubbles only when relevant  

---

## 6. Art & Audio Direction

### Visuals

- **Animals:** Box/cube heads, low-poly bodies, readable colors, **cel-shaded** materials with soft outlines.  
- **House:** Starts worn (cracked walls, junk, dim light). Upgrades replace props and brighten lighting per room.  
- **Lighting:** Realtime day/night loop (sun + warm lamp); nights lean on the lamp and cooler ambient.  
- **Post:** Subtle bloom (lamp), vignette, contrasty grade for toy-like readability in a small window.  
- **Style anchor:** Charming toy-like, not grimdark; rundown = cozy neglect, not horror.  
- **No** busy particle spam; coin pops and soft sparkles only on collect/adopt.  

### Audio

- Soft ambient house bed (creaks, distant traffic, fridge hum).
- Per-pet idle chirps / meows / moos (rare, not spammy).
- Short jingles for earn / unlock / minigame win.
- Mute + volume in settings; default quiet so it works over work/study.

---

## 7. Pets System

### Pet identity

Each pet has:

| Field | Purpose |
|------|---------|
| Species | Cat, Cow, Zebra, etc. |
| Name | Player-chosen or default |
| Room | Which room they live in (1 pet per room at MVP) |
| Job / specialty | Idle earn type + linked minigame |
| Needs | Hunger, Cleanliness, Fun (simple meters) |
| Mood | Derived from needs + recent play |
| Level / stars | Raises idle rate after care / minigames |
| Skin / tint | Unlockable cosmetics later |

### Needs (Tamagotchi layer)

Three meters, 0–100:

- **Hunger** — decays slowly; feed with food bought or earned
- **Clean** — decays slower; clean interaction or item
- **Fun** — decays; restored by petting or playing their minigame

**Mood** = weighted average. Low mood → slower idle income + sad bubble. Critical need → urgent want (still soft; no hard permadeath in MVP).

### Idle / companion presence

Pets idle and breathe in the house (Feel squash, day/night). **Coins are earned only from signature minigames**, not from an idle drip.

### Starter roster (tutorial choice)

| Pet | Personality | Signature minigame | Unlock |
|-----|-------------|--------------------|--------|
| Cat | Curious, lazy | Fishing QTE | Starter pick |
| Dog | Loyal, dig-happy | Dig garden → Snap (wrong holes fill in) | Starter pick |
| Rabbit | Energetic, snacky | Carrot farm collect | Starter pick |

More species = more rooms = clearer progress fantasy.

### Adoption rules

- Each **room unlock** grants **+1 pet slot**.
- Adopting costs currency (and later maybe a rare item).
- Player picks from a small available list (2–3 choices) so adoption feels like a decision.
- Cannot adopt the same specialty twice until late game (keeps minigame variety).

---

## 8. House & Rooms

### Metaphor

Currency buys **rooms** (and renovations). Rooms are both:

1. Space for another pet  
2. A themed stage for that pet’s vibe / minigame entry  

### Room progression (MVP → early game)

| # | Room | Theme | Unlocks |
|---|------|-------|---------|
| 0 | Living Room (starter) | Rundown lounge | Cat |
| 1 | Kitchen | Food smells, crumbs | Cow *or* Dog |
| 2 | Bathroom / Pond Nook | Wet tiles → tiny pond | Duck / Frog |
| 3 | Hall / Porch | Door to outside | Dog walk staging |
| 4 | Alley View | Window to street | Zebra crossing |
| 5 | Garden / Yard | Grass patch | Cow graze / Frog |
| 6 | Attic | Dusty collectibles | Fox / rare pet |
| 7+ | Prestige wings | Fancy / weird | Cosmetics, multipliers |

### Renovation tiers (per room)

1. **Rundown** — default  
2. **Fixed** — patched walls, better light (+small idle bonus)  
3. **Cozy** — furniture matching pet (+mood decay slower)  
4. **Showcase** — unique prop set (+minigame reward bump)

Buying a room is the big beat; renovating is the drip upgrade.

### Navigation

- Room strip / door icons along the UI.
- Instant switch; no long load.
- Optional “house overview” dollhouse zoom-out later.

---

## 9. Economy

### Currencies

| Currency | Earn | Spend |
|----------|------|-------|
| **Coins** | **Minigames only** (e.g. Fishing) | Food, rooms, adopts, renovations |
| **Stars** (later) | First clears / dailies | Rare pets, showcase renos |

**No idle coin drip for MVP.** Pets still have idle personality, but cash comes from playing.

### Needs

| Meter | Decay | Restore |
|-------|-------|---------|
| **Hunger** | Over time | Eat **bought** food |
| **Happiness** | Over time (faster when hungry) | Feeding + finishing minigames |

Loop: **Play minigame → earn coins → Shop buys food → Feed pet → meters recover.**

### Sink priorities

1. Food (ongoing)  
2. Next room (main gate)  
3. Adopt fee  
4. Renovation tiers  
5. Cosmetics  

### Pacing targets (design intent, tune in playtests)

- First minigame win: ~1–2 minutes in  
- First food purchase: right after first successful catches  
- Second pet / first bought room: several minigame sessions  
- 4–5 pets: several sessions across a day  
- Avoid requiring constant babysitting; decay should be gentle

### Clicker feel

- Big satisfying number pop on collect  
- Optional “collect all” button when multiple pets are generating  
- Soft prestige later: “Move to a Better Block” resets rooms for permanent multiplier (post-MVP)

---

## 10. Minigames

### Design rules

- 30–90 seconds  
- Keyboard + mouse friendly  
- Fail-soft: quitting early still grants tiny coins  
- Clear “Pet Wanted This” entry from speech bubble  
- Each pet maps to **one primary minigame** at MVP  

### Catalog

#### 10.1 Fishing (Cat)

- Side view dock / puddle in the rundown yard.  
- Timing bar or bite indicator; reel with click / space.  
- Catch small fish → coins; rare boot → junk joke; rare golden fish → bonus.  
- Difficulty: bite window shrinks slightly with level.

#### 10.2 Eat Grass (Cow)

- Top-down or side patch of grass tiles.  
- Move cow, eat highlighted tasty tufts before they wilt.  
- Avoid weeds / thorns (lose time or coins).  
- Score = tufts eaten × mood bonus.

#### 10.3 Cross the Road (Zebra) — Crossy Road–lite

- Grid hop forward across lanes.  
- Cars / bikes in patterns; one-hit return to sidewalk (or lose a life).  
- Distance = coins; milestones give Stars.  
- Keep session short (reach goal stripe, not endless by default).

#### 10.4 Walk / Fetch (Dog)

- Side scroller or lane walk.  
- Keep dog in happy zone: press to sniff / avoid trash / collect sticks.  
- Optional fetch: throw ball, time catch.

#### 10.5 Swim / Paddle (Duck)

- Top-down pond.  
- Steer around lily pads, collect breadcrumbs, avoid soap suds / toy boats.  
- Combo collection increases coin multiplier briefly.

#### 10.6 Farm / Bug Snap (Frog or Garden)

- Timing tongue snaps at flies.  
- Or tiny crop water/harvest loop if “farming” is framed as garden care.

### Want system → minigame

Pets periodically raise a **Want**:

- “Fishing time?” / “Street looks crossable…” / “Grass…”  
- Player can Accept → launch minigame, or Snooze (want returns later, small mood dip).  
- Completing the want: Fun up, bonus coins, XP toward pet level.

---

## 11. Meta Progression & Retention

### Short term

- Room unlocks, pets, renovations  
- Pet levels (idle %)  
- Daily light login: “newspaper” with 1–2 easy challenges  

### Medium term

- Cosmetic hats / collars / room posters  
- Minigame high-score ribbons on room walls  
- Collection log: species discovered  

### Long term / post-MVP

- Prestige house move  
- Seasonal events (rainy week → swim bonus)  
- Rare mutant box-head variants  

### Death / neglect policy

**MVP: no permanent death.**  
Critical neglect → pet naps in a “sulk” state with near-zero idle until fed/cleaned. Keeps the Tamagotchi tension without punishing desktop users who step away.

---

## 12. Controls & Accessibility

- Mouse primary; keyboard shortcuts for minigames  
- Colorblind-safe want icons (shape + color)  
- Scalable UI text  
- Full mute, reduce motion toggle (pause non-essential idle anims)  
- Pause idle decay while minigame is open  

---

## 13. Technical Design (Unity)

### Project shape

- **Unity 6.3 LTS** (`6000.3.x`, 2026-era editor) — not 2022  
- **URP 17.3** 3D, low-poly + **cel shade** (`PolyPets/CelShade`)  
- URP Volume post: bloom, vignette, color grade, white balance, neutral tonemap  
- **Day/night cycle** drives sun/fill/lamp, ambient, camera clear, and volume exposure/temp  
- ScriptableObjects for PetDef, RoomDef, MinigameDef, ItemDef  
- Save: JSON local file (coins, pets, rooms, timers)  
- **Editor bootstrap:** `PolyPets → Bootstrap Starter House Scene` (see [`docs/UNITY_SETUP.md`](UNITY_SETUP.md))  

### Desktop features (Windows first)

- Borderless / small windowed mode (**480×720** default)  
- `Always on top` via Win32 hook or Unity player setting + plugin (`DesktopNative` stub in place)  
- Remember window position/size  

### Systems map

```
GameBootstrap
 ├─ DayNightCycle (sun / lamp / volume grade)
 ├─ SaveSystem
 ├─ EconomyService (coins, spend, earn ticks)
 ├─ HouseController (rooms, renovations)
 ├─ PetManager (instances, needs decay, wants)
 ├─ IdleTicker
 ├─ UI (HUD, shop, room nav)
 └─ MinigameRouter → Fishing / Graze / Crossy / …
```

Bootstrap already places: `GameBootstrap`, `HouseController`, `RoomRoot`, `PetAgent`, `HouseCameraController`, `DesktopWindowController`, `DayNightCycle`, `Volume`, `HudController`.

### Scene strategy

- `Boot` → `House` (additive room content) → `Minigame_*` loaded additive or single active swap  
- Keep house scene alive under minigames if possible for instant return  
- Starter scene path: `Assets/Scenes/House_LivingRoom.unity`  

### Performance budget

- Few dynamic lights; lamp is the hero additional light at night  
- Cap particle counts  
- Idle tick on a 0.5–1.0s interval, not per-frame economy math  
- House camera: HDR **on** for bloom, FXAA, no MSAA — cheap at 480×720  

---

## 14. Content Scope by Phase

### Phase 0 — Vertical slice (prove the fantasy)

- Always-on-top small window  
- 1 rundown living room  
- 1 cat (needs + idle coins + tap)  
- Fishing minigame  
- Buy Kitchen room + adopt Cow  
- Save/load  

### Phase 1 — Core loop complete

- 4–5 rooms  
- 4 pets (Cat, Cow, Zebra, Dog)  
- 4 minigames  
- Renovation tier 1–2  
- Wants system  
- Soft offline earnings  

### Phase 2 — Juice & retention

- More pets/rooms  
- Cosmetics  
- Daily challenges  
- House overview  
- Audio pass, polish, balancing  

### Phase 3 — Stretch

- Prestige  
- Tray mode / opacity  
- Mac support  
- Workshop-style pet mods (unlikely early)  

---

## 15. Success Metrics (for playtests)

- Does the player leave the window open while doing something else?  
- Time to first smile (pet animation + first coin pop)  
- Time to first room purchase  
- Minigame completion rate vs abandon rate  
- “One more want” pull without guilt/spam  

---

## 16. Out of Scope (for now)

- Multiplayer / visiting friends’ houses  
- Real-money shop (decide much later)  
- Full open-world walking sim  
- Hardcore permadeath  
- Mobile (desktop-first)  

---

## 17. Open Decisions

| Topic | Options | Recommendation |
|------|---------|----------------|
| Pet per room | Strict 1 vs multi | **1 per room** for MVP clarity |
| Offline earn | None / capped / full | **Capped** (2–4h) |
| Second currency | Coins only vs Coins+Stars | **Coins only** until Phase 1 end |
| Minigame fail | Retry free vs fee | **Free retry**, lower rewards if spam |
| Art production | In-house greybox → polish | Greybox animals ASAP for feel |

---

## 18. One-Page Pitch

**PolyPets** is a low-poly desktop Tamagotchi-idle hybrid. A box-headed cat lives in a rundown house that floats above your other windows. Pets earn coins like tiny idols; you spend those coins on new rooms and new animals—cows that graze, zebras that cross roads, ducks that swim—each with a short signature minigame. Care is light, progress is visible in the house itself, and the whole game is built to live in the corner of your PC.

---

## Appendix A — Example Want Copy

- Cat: “Pond’s calling. Fishing?”  
- Cow: “That grass looks illegal. In a good way.”  
- Zebra: “I could make it across. Probably.”  
- Dog: “Walk? Walk. Walk??”  
- Duck: “Splash budget: unlimited.”  

## Appendix B — Suggested File / Doc Follow-ups

- `docs/ECONOMY_TUNING.md` — curves for room costs & idle rates  
- `docs/PET_BIBLE.md` — per-species sheets  
- `docs/MINIGAMES_SPEC.md` — controls, scoring, wireframes  
- `docs/TECH_WINDOWS.md` — always-on-top implementation notes  
