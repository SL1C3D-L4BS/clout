# CLOUT — Game Design Document

> Version 0.1 | March 2026 | TheArchitect

---

## 1. Executive Summary

**Clout** is a multiplayer crime empire simulator that combines the addictive business-building loop of Schedule 1 with deep Souls-like melee combat and tactical shooter gunplay, all wrapped in the commercially proven POLYGON low-poly art style.

**Genre:** Crime Sim / Action RPG / Multiplayer Empire Builder
**Platform:** PC (Steam) → Console
**Engine:** Unity 6 + FishNet
**Art Style:** Synty POLYGON (low-poly, boxy, stylized)
**Multiplayer:** 1-4 Co-op + PvPvE Territory Wars (up to 16 players)

---

## 2. The Market Gap

| Game | Empire Building | Combat Depth | Multiplayer | Art Consistency |
|------|----------------|-------------|-------------|----------------|
| Schedule 1 | Excellent | Minimal | Co-op only | Custom low-poly |
| GTA Online | Basic (businesses) | Good (shallow melee) | Excellent | Realistic |
| Payday 3 | None | Shooter only | Co-op | Realistic |
| Drug Dealer Sim 2 | Good | Minimal | None | Realistic |
| **Clout** | **Excellent** | **Deep (melee+ranged)** | **Co-op + PvPvE** | **POLYGON** |

The gap: **Nobody combines deep empire management + skill-based combat + multiplayer territory wars.**

---

## 3. Core Gameplay Loops

### 3.1 The Minute-to-Minute Loop (Action)
```
Explore City → Find Customers → Deal Product → Earn Cash → Avoid Police
         ↓                                              ↓
    Fight Rivals ← Defend Territory ← Get Attacked ← Draw Attention
```

### 3.2 The Hour-to-Hour Loop (Empire)
```
Buy Ingredients → Cook Product → Distribute → Collect Revenue
        ↓                                          ↓
  Upgrade Equipment → Hire Employees → Automate → Expand
        ↓                                          ↓
  Buy Properties → Launder Money → Increase CLOUT → Unlock New Tiers
```

### 3.3 The Session-to-Session Loop (Meta)
```
Claim Territory → Build Empire → Defend from Rivals → Rank Up
        ↓                                                ↓
  Multiplayer Wars → Win/Lose Territory → Adapt Strategy → Grow
        ↓                                                ↓
  Unlock Suppliers → Access Better Product → Higher Value → More CLOUT
```

---

## 4. Systems Design

### 4.1 CLOUT Score (The Title Mechanic)

CLOUT is your street reputation — the single most important number in the game. It's not just a score; it's a gating mechanism for progression.

**Ranks:**
| Rank | CLOUT Required | Unlocks |
|------|---------------|---------|
| 0 - Nobody | 0 | Street dealing, basic ingredients |
| 1 - Corner Boy | 100 | First property, basic employees |
| 2 - Hustler | 500 | Lab access, better suppliers, weapons |
| 3 - Shot Caller | 2,000 | Multiple properties, territory control |
| 4 - Kingpin | 10,000 | All properties, rival cartels notice you |
| 5 - Legend | 50,000 | Everything unlocked, leaderboard eligible |

**How you earn CLOUT:**
- Completing deals (+2 to +10 based on size)
- Claiming territory (+50)
- Defeating rivals in combat (+25)
- Buying properties (+30)
- Surviving police raids (+20)
- Multiplayer kills (+8)
- Winning territory wars (+100)

**How you lose CLOUT:**
- Getting arrested (-50)
- Losing territory (-30)
- Product seized (-10)
- Employee betrayal (-15)
- Getting robbed (-20)

### 4.2 Crafting System

Inspired by Schedule 1's cooking but with deeper ScriptableObject-driven modularity.

**Recipe Structure:**
```
Base Ingredient + Catalyst + Additives → Product (Quality varies)
     ↓               ↓           ↓              ↓
  Determines type  Speeds up   Modifies      Quality = Skill + Equipment + Ingredients
                              Effects/Value
```

**Additive Effects (the viral mechanic):**
Each additive creates unique combinations players discover and share. Examples:
- Cough medicine → Adds drowsy effect, increases addiction
- Energy drink → Adds stimulant effect, increases value
- Baking soda → Cuts product (more quantity, less quality)
- Food coloring → Visual only (branding your product)

The combinatorial explosion of additives creates emergent discovery — players share their best recipes on social media like they share Schedule 1 recipes on TikTok.

### 4.3 Property System

Properties are your physical empire infrastructure.

**Types:**
- **Safehouse** — Base of operations. Stash storage, planning.
- **Lab** — Cook product. Equipment affects speed and quality.
- **Growhouse** — Cultivate cannabis. Requires maintenance.
- **Storefront** — Legit business front. Money laundering.
- **Warehouse** — Bulk storage. Enables wholesale deals.
- **Nightclub** — Money laundering + social hub. Events.
- **Auto Shop** — Vehicle modifications + repair.
- **Restaurant** — High-tier money laundering.

**Upgrades:** Each property has an upgrade tree (security cameras, better equipment, hidden rooms, decoy setups). Upgrades improve efficiency and reduce police detection.

### 4.4 Employee System

Employees automate empire operations. Each has stats that affect performance.

**Stats:**
- **Skill** — How good they are at their job (cook quality, deal speed)
- **Loyalty** — How likely they stay (vs. steal from you or work for rivals)
- **Discretion** — How likely they talk to police if arrested
- **Ambition** — How likely they attempt to steal product/money

**Events:** Employees generate emergent narrative:
- "Your cook Marcus produced a batch of exceptional quality"
- "Dealer Tyrone got arrested. His discretion will determine if he talks."
- "Guard Jamal is asking for a raise. If denied, loyalty drops."
- "Driver Sam was spotted by a rival crew near their territory."

### 4.5 Wanted System

6-tier heat system with deep police AI.

**Tiers:**
| Level | Heat | Police Response |
|-------|------|----------------|
| 0 - Clean | 0 | None |
| 1 - Suspicious | 50+ | Occasional patrol attention |
| 2 - Wanted | 150+ | Active pursuit, searches |
| 3 - Hunted | 300+ | Aggressive response, roadblocks |
| 4 - Most Wanted | 450+ | SWAT, helicopter, aggressive AI |
| 5 - Kingpin | 500 | Perpetual high alert, special units |

**Heat Sources:** Dealing in public, gunfire, assault, murder, trespassing, speeding, drug possession, lab explosions, neighbor complaints

**Heat Reduction:** Time passing, hiding in properties, bribery, laying low, disguises, leaving the area

### 4.6 Combat System

Two complete combat systems unified under one character controller.

**Melee Combat (Souls-like):**
- Light attack (RB) — fast combo chains
- Heavy attack (RT) — slower, more damage
- Parry (LB) — timing-based counter
- Dodge/Roll (B) — stamina-gated i-frames
- Lock-on (R3) — target enemy for strafe combat
- Backstab — position-based high-damage attack
- Weapons: fists, knife, bat, pipe, machete, katana, sledgehammer

**Ranged Combat (TPS/FPS):**
- Hip fire (RB) — inaccurate but fast
- ADS fire (LT + RT) — accurate, slower movement
- Reload (X) — magazine-based ammo
- Stance toggle (C) — standing, crouching, prone
- Recoil — AnimationCurve-driven, weapon-specific
- Spread — accumulates with continuous fire, recovers
- Weapons: pistol, SMG, assault rifle, shotgun, sniper, RPG

**Context-Sensitive Input:**
- RT fires gun when ranged equipped, heavy attack when melee equipped
- Same controller, different behavior based on weapon type

### 4.7 Territory Wars (Multiplayer)

The competitive endgame layer.

**Zone Control:**
- City divided into zones (corners, blocks, districts)
- Each zone has economic value (customer density, product demand)
- Influence builds through dealing, property ownership, combat

**PvPvE:**
- Player empires compete for the same zones
- AI rival cartels also expand and fight
- Territory can be contested, captured, and lost
- Zone bonuses: +revenue, +customer loyalty, -police attention

---

## 5. Viral Mechanics (2026 TikTok/Twitch Optimization)

### 5.1 Recipe Discovery Sharing
Players discover unique additive combinations → share on social media → others try to replicate. Same mechanic that made Schedule 1 go viral on TikTok.

### 5.2 Empire Tours
Walk through your fully built empire → show off properties, employees, operations → streaming/screenshot content.

### 5.3 Territory War Highlights
PvP firefights over territory → clip-worthy moments with deep combat system.

### 5.4 Rank-Up Moments
Reaching new CLOUT ranks triggers cinematic celebration → shareable moment.

### 5.5 Employee Drama
Emergent narratives (betrayals, arrests, exceptional performance) → stories to tell.

### 5.6 Co-op Cooking Sessions
Friends cooking together, experimenting with recipes, goofing off → Twitch gold.

---

## 6. Technical Architecture

### 6.1 Network Model
- **Server-authoritative** via FishNet
- Economy, territory, wanted level all server-controlled
- Client-side prediction for movement/combat feel
- SyncVar replication for visual state

### 6.2 ScriptableObject Architecture
Everything that's data is a ScriptableObject:
- Weapons, items, ingredients, recipes, products
- Property definitions, upgrade trees
- Employee templates, AI behavior profiles
- NPC customer profiles, police response patterns

### 6.3 State Machine
All characters share `CharacterStateManager`:
- Player: locomotion → combat → interact → vehicle → death
- AI Enemy: patrol → chase → rangedAttack → meleeAttack → flee
- AI Police: patrol → investigate → pursue → arrest → combat
- AI Civilian: routine → flee → report → consume

---

## 7. Monetization Strategy

**Premium ($24.99-$29.99)** — Same price point as Schedule 1
- No microtransactions
- No pay-to-win
- Cosmetic DLC only (character skins, vehicle skins, property decorations)
- Expansion packs for new city districts, product types, story content

---

## 8. Development Timeline

| Phase | Duration | Focus |
|-------|----------|-------|
| Phase 0 | Done | Project setup, architecture |
| Phase 1 | 4 weeks | Character controller + basic combat |
| Phase 2 | 6 weeks | Empire core (crafting, properties, employees) |
| Phase 3 | 4 weeks | World systems (police, reputation, day/night) |
| Phase 4 | 6 weeks | Multiplayer (territory wars, co-op) |
| Phase 5 | 8 weeks | Content, polish, art, audio |
| Alpha | Week 28 | Internal testing |
| Beta | Week 32 | Community testing |
| Launch | Week 40 | Steam Early Access |

---

*This document is a living artifact. Updated as development progresses.*
