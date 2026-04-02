# CLOUT -- Game Design Document

> Version 2.0 | April 2026 | SlicedLabs
> Canonical Spec: Docs/Architecture/BUILD_SPECIFICATION.md (v3.0)

---

## 1. Executive Summary

CLOUT is a criminal ecosystem simulator combining addictive empire-building with deep Souls-like melee combat and tactical shooter gunplay, wrapped in the commercially proven POLYGON low-poly art style. Players rise from street-level nobody to transnational syndicate operator through interconnected systems that prioritize emergence over scripted content.

**Genre:** Crime Simulator / Action RPG / Empire Builder
**Platform:** PC (Steam) / Console (PS5, Xbox Series X)
**Engine:** Unity 6 (URP) with FishNet multiplayer
**Art Style:** Synty POLYGON (low-poly, stylized, neon-noir)
**Multiplayer:** 1-4 Co-op + PvPvE Territory Wars + 10K+ Persistent Shards
**Price:** $24.99 Early Access / $29.99 Full Launch

---

## 2. The Market Gap

| Game | Empire Building | Combat Depth | Multiplayer | Art Consistency |
|------|----------------|-------------|-------------|----------------|
| Schedule 1 | Excellent | Minimal | Co-op only | Custom low-poly |
| GTA Online | Basic (businesses) | Good (shallow melee) | Excellent | Realistic |
| Payday 3 | None | Shooter only | Co-op | Realistic |
| Drug Dealer Sim 2 | Good | Minimal | None | Realistic |
| **CLOUT** | **Excellent** | **Deep (melee+ranged)** | **Co-op + PvPvE** | **POLYGON** |

The gap: no game combines deep empire management, skill-based hybrid combat, and multiplayer territory wars.

---

## 3. Core Design Principles

1. **Simulation > Content** -- Features must emerge from interacting systems, not hand-crafted scripts.
2. **Emergence > Handholding** -- The game feels like discovering criminal physics, not following a quest log.
3. **Consequences > Balance** -- The world is unfair sometimes. Choices carry permanent weight.
4. **Player Agency > Scripted Drama** -- Every major event traces to player or AI decisions.
5. **Scalable Depth** -- Understandable at 10 hours, rewarding at 1,000 hours.

---

## 4. Core Gameplay Loops

### 4.1 Moment-to-Moment (Action)
```
Explore City -> Find Customers -> Deal Product -> Earn Cash -> Avoid Police
         |                                              |
    Fight Rivals <- Defend Territory <- Get Attacked <- Draw Attention
```

### 4.2 Session Loop (Empire)
```
Buy Ingredients -> Cook Product -> Distribute -> Collect Revenue
        |                                          |
  Upgrade Equipment -> Hire Employees -> Automate -> Expand
        |                                          |
  Buy Properties -> Launder Money -> Increase CLOUT -> Unlock Tiers
```

### 4.3 Meta Loop (Progression)
```
Claim Territory -> Build Empire -> Defend from Rivals -> Rank Up
        |                                                |
  Multiplayer Wars -> Win/Lose Territory -> Adapt Strategy -> Grow
```

---

## 5. Systems Design

### 5.1 CLOUT Score

The central progression mechanic. Street reputation that gates access to properties, suppliers, employees, and advanced mechanics.

| Rank | CLOUT | Unlocks |
|------|-------|---------|
| 0 - Nobody | 0 | Street dealing, basic ingredients |
| 1 - Corner Boy | 100 | First property, basic employees |
| 2 - Hustler | 500 | Lab access, better suppliers, weapons |
| 3 - Shot Caller | 2,000 | Multiple properties, territory control |
| 4 - Kingpin | 10,000 | All properties, rival cartels engage |
| 5 - Legend | 50,000 | Everything unlocked, leaderboard eligible |

**Earning:** Deals (+2-10), territory claims (+50), defeating rivals (+25), properties (+30), surviving raids (+20).
**Losing:** Arrests (-50), lost territory (-30), seizures (-10), betrayals (-15).

### 5.2 4D Reputation Vector

Beyond CLOUT, a hidden reputation vector drives NPC behavior:

| Dimension | Effect |
|-----------|--------|
| Fear | NPCs flee, workers less likely to steal, rivals avoid confrontation |
| Respect | Better hiring quality, customer loyalty, favorable pricing |
| Reliability | Worker loyalty bonus, supplier trust, alliance stability |
| Ruthlessness | Intimidation success, rival submission, media attention |

### 5.3 Crafting System

ScriptableObject-driven with combinatorial depth:
```
Base Ingredient + Catalyst + Additives -> Product (Quality varies)
     |               |           |              |
  Determines type  Speeds up   Modifies      Quality = Skill + Equipment + Ingredients
                              Effects/Value
```

Six station types: Mixing, Heating, Chemical, Pressing, Growing, Cutting. Each with risk events (explosions, fume detection, contamination).

### 5.4 Property System

Eight property types form the physical empire infrastructure:

| Type | Function | Laundering |
|------|----------|-----------|
| Safehouse | Base of operations, stash storage | No |
| Lab | Product manufacturing | No |
| Growhouse | Cultivation | No |
| Storefront | Legal front business | Yes |
| Warehouse | Bulk storage, wholesale | No |
| Nightclub | Social hub, events | Yes |
| Auto Shop | Vehicle modifications | Yes |
| Restaurant | High-tier food front | Yes |

Each property supports upgrades (security, equipment, hidden rooms, decoys), employee slots, and stash storage.

### 5.5 Employee System

Autonomous workers operate empire infrastructure:

| Role | Function | Risk |
|------|----------|------|
| Dealer | Autonomous street sales | Arrest, robbery, betrayal |
| Cook | Lab production | Explosion, contamination |
| Guard | Property security | Combat death, bribery |
| Grower | Cultivation management | Detection, seizure |
| Driver | Transport/delivery | Traffic stops (Phase 3) |
| Accountant | Money laundering | Audit trail (Phase 3) |
| Lookout | Early warning | Negligence (Phase 3) |
| Enforcer | Territory enforcement | Murder charges (Phase 3) |

**Betrayal Formula:** `P(betray) = (Greed + Fear - Loyalty + ExternalOffer) / Compartmentalization`

### 5.6 Wanted System

Six-tier heat system with escalating police response:

| Level | Heat | Response |
|-------|------|----------|
| 0 - Clean | 0 | None |
| 1 - Suspicious | 50+ | Patrol attention |
| 2 - Wanted | 150+ | Active pursuit, searches |
| 3 - Hunted | 300+ | Aggressive response |
| 4 - Most Wanted | 450+ | SWAT, raids |
| 5 - Kingpin | 500 | Perpetual high alert |

Heat sources: dealing in public, gunfire, assault, murder, lab explosions. Reduction: time, hiding, bribery, leaving area.

### 5.7 Combat System

Two complete combat systems under one character controller:

**Melee (Souls-like):** Light/heavy attacks, combo chains, parry, dodge/roll with i-frames, lock-on, backstab. Stamina-gated, animation-driven.

**Ranged (TPS/FPS):** Hip fire, ADS, recoil curves, spread accumulation, magazine reloading, stance modifiers. Context-sensitive: same buttons, different behavior based on equipped weapon.

**Four camera modes:** FreeLook, HipFire, ADS, LockOn via Cinemachine priority switching.

### 5.8 Economy

Multi-layer market formula:
```
P(t) = P_base x (D(t)/S(t)) x (1 + E_r) x (1 + R_m) x M_s
```

- D/S: Dynamic demand/supply ratio per district per product
- E_r: Price elasticity per region
- R_m: Risk modifier from heat/investigation
- M_s: Seasonal/geopolitical multiplier

Dirty/clean cash separation. All transactions logged to TransactionLedger with daily/weekly metrics.

---

## 6. Territory Wars (Multiplayer -- Phase 4)

- City divided into zones with economic value
- Influence builds through dealing, property ownership, combat
- PvPvE: player empires compete alongside AI cartels
- Zone bonuses: revenue, customer loyalty, reduced police attention
- Alliance and war mechanics between player organizations

---

## 7. Technical Architecture

### 7.1 Core Patterns
- **State Machine:** All characters share CharacterStateManager base
- **Strategy Pattern:** Pluggable StateAction classes composed into states
- **ScriptableObject Architecture:** All game data is SO-driven
- **Event Bus:** Type-safe pub/sub with 20+ event types
- **Singleton Managers:** Central access for single-player systems
- **Utility Theory AI:** Weighted scoring for AI decisions

### 7.2 Network Model (Phase 4+)
- Server-authoritative via FishNet
- Client-side prediction for movement/combat
- SyncVar replication for visual state
- Economy and territory server-controlled

---

## 8. Monetization

**Premium ($24.99-$29.99)**
- No microtransactions
- No pay-to-win
- Cosmetic DLC only (character skins, vehicle skins, property decorations)
- Expansion packs for new regions and content

---

## 9. Development Timeline

| Phase | Focus | Status |
|-------|-------|--------|
| Phase 0 | Foundation (architecture, URP, input) | COMPLETE |
| Phase 1 | Core Loop (combat, AI, camera, tools) | COMPLETE |
| Phase 2 | Empire Systems (10 steps) | COMPLETE |
| Phase 3 | Advanced Empire (laundering, forensics, factions) | Next |
| Phase 4 | World & Multiplayer | Planned |
| Phase 5 | Content & Polish | Planned |
| Phase 6 | Ship | Planned |

---

*CLOUT Game Design Document v2.0 -- SlicedLabs -- April 2026*
