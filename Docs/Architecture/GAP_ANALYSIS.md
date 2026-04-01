# CLOUT — Gap Analysis: Spec v2.0 vs Current Codebase

> **Date:** March 31, 2026
> **Codebase:** 101 C# scripts | Unity 6 (6000.4.0f1) | URP
> **Spec:** BUILD_SPECIFICATION v2.0 (70 sections)

This document maps every section of the v2.0 spec to the current codebase, identifies gaps, and prioritizes what must be built to catch up before advancing to new features.

---

## Status Legend

- ✅ **BUILT** — Implemented and functional
- 🟡 **PARTIAL** — Foundation exists, needs enhancement
- 🔴 **MISSING** — Not started, required by spec
- ⬜ **DEFERRED** — Spec defines it but it's for a later phase

---

## Part I — Vision & Identity (Sections 1–4)

| Section | Spec Requirement | Status | Current State | Gap |
|---------|-----------------|--------|---------------|-----|
| 1. Product Vision | Core fantasy defined | ✅ | Docs/Design/CRIMINAL_ECOSYSTEM_2026.md | Aligned |
| 2. Core Game Identity | 5 pillars, genre fusion | ✅ | README.md + design docs | Aligned |
| 3. Core Game Loop | Meta/session/moment loops | 🟡 | Session loop works (cook→deal→earn→buy), meta loop not tracked | Need: milestone tracking system |
| 4. Art Direction | Neon-noir, HDRP, weather | 🔴 | URP only, no weather, no neon-noir post-processing | DEFERRED to Phase 5 (HDRP migration) |

---

## Part II — Player Systems (Sections 5–8)

| Section | Spec Requirement | Status | Current State | Gap |
|---------|-----------------|--------|---------------|-----|
| 5. Player Identity | Triple-layer (public/criminal/hidden), 4D reputation vector | 🟡 | ReputationManager exists but is basic CLOUT score, no 4D vector | Need: upgrade ReputationManager to 4D vector (Fear/Respect/Reliability/Ruthlessness) |
| 5b. Milestones | 8 milestone tiers | 🔴 | No milestone system | Need: MilestoneTracker component |
| 6. Player Controller | Hybrid 3P+1P, 5 modes | 🟡 | 3P controller complete, no 1P/driving/command/war room modes | PARTIAL — 3P mode solid. 1P = Phase 3, Driving = Phase 3, Command/War Room = Phase 5 |
| 6b. Movement | Walk/run/sprint/crouch/parkour/roll | 🟡 | Walk/run/sprint/roll done. No crouch, prone, parkour | Need: crouch + vault in Phase 3 |
| 6c. Camera | 4 modes + driving + command + war room | 🟡 | 4 modes done (FreeLook/LockOn/HipFire/ADS). No driving/command cameras | DEFERRED to Phase 3+ |
| 6d. State Machine | SO-driven states | ✅ | Full StateManager → State → StateAction architecture | Aligned |
| 7. Combat (Melee) | Souls-like with combos, parry, backstab | ✅ | Full melee pipeline: AttackAction, DamageCollider, ParryCollider, combos | Aligned |
| 7b. Combat (Ranged) | COD-like ADS, recoil, cover | ✅ | RangedAttackAction, RangedWeaponHook, RecoilController, ADS | Aligned |
| 7c. Combat (Stealth) | Silent takedowns, detection meter | 🔴 | No stealth system | DEFERRED to Phase 3 |
| 7d. Unified Damage | DamageTypeSO with effects | 🟡 | DamageEvent struct exists with types, no DamageTypeSO | Need: convert DamageType enum to SO-driven system (Phase 3) |
| 8. Skill Framework | Use-based attributes (12 attributes) | 🔴 | No attribute growth system | DEFERRED to Phase 3 |

### Part II Summary — What Needs Catching Up NOW:
- **ReputationManager → 4D Vector**: Current CLOUT score should evolve to [Fear, Respect, Reliability, Ruthlessness]
- **MilestoneTracker**: Simple milestone check system tied to existing events

---

## Part III — Empire Systems (Sections 9–15)

| Section | Spec Requirement | Status | Current State | Gap |
|---------|-----------------|--------|---------------|-----|
| 9. Empire Architecture | Full hierarchy (org, properties, workforce, supply, territory, global) | 🟡 | Properties ✅, Economy ✅, Territory skeleton, Workforce template only | Need: WorkerManager + DealerAI (Step 6) |
| 10. Production | 7-stage lifecycle, mixing, quality tiers, automation | 🟡 | CraftingStation (6 types), recipes, ingredients, quality calc, risk events | Missing: quality decay over time, automation via CookAI, chained stations |
| 11. Signature System | 512-bit batch vectors, cosine similarity, signature scrubbing | 🔴 | Not started | DEFERRED to Phase 3 (critical for investigation AI) |
| 12. Facility System | Upgrade skill trees (efficiency/quality/stealth/security) | 🟡 | Property upgrades exist (PropertyUpgradeType enum) but no skill tree structure | Need: flesh out upgrade paths in Phase 3 |
| 13. Workforce & NPC Graph | Directed weighted graph, betrayal formula, compartmentalization | 🟡 | EmployeeDefinition SO exists with skill/loyalty/discretion/greed stats | Need: WorkerManager, DealerAI, CookAI, GuardAI, betrayal mechanics (Step 6) |
| 14. Supply Chain | 24 regions, 12 transit hubs, War Room visualization | 🔴 | Not started | DEFERRED to Phase 4 |
| 15. Money Laundering | 5-step pipeline, 7 methods, pattern detection | 🟡 | CashManager has basic Launder() method. EconomyManager has LaunderCash() | Need: expand laundering pipeline in Phase 3 |

### Part III Summary — What Needs Catching Up NOW:
- **Step 6 Worker System** is the critical gap — DealerAI, CookAI, GuardAI, WorkerManager, RecruitmentManager
- Production quality decay can be added alongside CookAI automation

---

## Part IV — World Systems (Sections 16–22)

| Section | Spec Requirement | Status | Current State | Gap |
|---------|-----------------|--------|---------------|-----|
| 16. Procedural City Gen | Worley+Perlin noise, Poisson disk, dynamic layers | 🟡 | ProceduralPropertyBuilder + ProceduralCityBlock (test arena 160×160m) | Need: multi-block city gen (Phase 4) |
| 17. Global Regions | 24 sources, 12 hubs, 8 megacities | 🔴 | Not started | DEFERRED to Phase 4 |
| 18. Territory Control | 5 conquest methods, zone grid | 🟡 | TerritoryManager exists (skeleton) | Need: flesh out with conquest mechanics (Phase 3) |
| 19. Open World Districts | Multi-district city | 🟡 | Single procedural city block | Need: DistrictManager + multi-block (Phase 3-4) |
| 20. Vehicle System | Driving, chases, smuggling | 🔴 | VehicleManager placeholder only | DEFERRED to Phase 3 |
| 21. Property System | Buy/own, upgrades, stash, interiors | ✅ | PropertyManager, Property, PropertyDefinition, stash, upgrades, 8 types | Aligned |
| 22. Climate/Weather | Dynamic weather affecting gameplay | 🔴 | Not started | DEFERRED to Phase 5 |

---

## Part V — Economy & Markets (Sections 23–25)

| Section | Spec Requirement | Status | Current State | Gap |
|---------|-----------------|--------|---------------|-----|
| 23. Multi-Layer Economy | Price formula: P = base × (D/S) × elasticity × risk × seasonal | 🟡 | EconomyManager has basic supply/demand. CashManager has dirty/clean. TransactionLedger tracks metrics | Need: implement full price formula (multiply in elasticity, risk modifier, seasonal) |
| 24. Player-Driven Markets | No NPC price floors, player manipulation | 🔴 | NPC-driven prices only | DEFERRED to Phase 4 (multiplayer) |
| 25. Crypto/Dark Web | In-game crypto, dark web marketplace | 🔴 | Not started | DEFERRED to Phase 5 |

### Part V Summary — What Needs Catching Up NOW:
- **EconomyManager price formula** should be enhanced to include the full equation. Currently basic supply/demand only.

---

## Part VI — Law Enforcement (Sections 26–30)

| Section | Spec Requirement | Status | Current State | Gap |
|---------|-----------------|--------|---------------|-----|
| 26. 5D Heat System | [LocalPD, State, Federal, Rival, Media] with separate escalation | 🟡 | WantedSystem has 6-tier heat but single dimension | Need: upgrade to 5D vector (Phase 2 Step 7 or Phase 3) |
| 27. Investigation Graph | Neo4j, PageRank, cosine similarity | 🔴 | Not started | DEFERRED to Phase 4-5 |
| 28. FBI Profiler | Behavioral embedding vectors | 🔴 | Not started | DEFERRED to Phase 5 |
| 29. Response Tiers | Beat cop → detective → OCU → federal | 🔴 | No police NPCs in world | Need: PolicePatrolAI (Phase 2 Step 7) |
| 30. Undercover/Informant | Embedded agents, informant graph | 🔴 | Not started | DEFERRED to Phase 5 |

### Part VI Summary — What Needs Catching Up NOW:
- **PolicePatrolAI** is Step 7 — after Worker system
- **5D heat** can be wired incrementally (start with LocalPD + Rival, add others in Phase 3)

---

## Part VII — AI & Faction Systems (Sections 31–35)

| Section | Spec Requirement | Status | Current State | Gap |
|---------|-----------------|--------|---------------|-----|
| 31. AI Architecture | Utility + BT + GOAP + GNN | 🟡 | Utility AI complete (AIActionScoring). No BT/GOAP/GNN | Need: BT for police (Phase 3), GOAP for factions (Phase 4) |
| 32. NPC Personality | Personality graph, loyalty context | 🟡 | EmployeeDefinition has stats. CustomerAI has addiction/loyalty | Need: expand with personality SO (Phase 3) |
| 33. AI Faction Simulation | Factions run same loop as player | 🔴 | Not started | DEFERRED to Phase 4 |
| 34. Rival Gang AI | Squad tactics, adaptation | 🔴 | AIStateManager handles individual enemies only | DEFERRED to Phase 4 |
| 35. Civilian Population | Schedule-based, DOTS/ECS | 🔴 | No civilian NPCs | DEFERRED to Phase 3-4 |

---

## Part VIII — Social & Political (Sections 36–40)

| Section | Spec Requirement | Status | Current State | Gap |
|---------|-----------------|--------|---------------|-----|
| 36. 4D Reputation | [Fear, Respect, Reliability, Ruthlessness] | 🟡 | ReputationManager is single-axis CLOUT score | Need: upgrade to 4D vector |
| 37. Corruption | Political simulation, blackmail, bribery | 🔴 | Not started | DEFERRED to Phase 5 |
| 38. Fear vs Respect | NPC behavior modified by reputation vector | 🔴 | Not started | Need: wire 4D rep to NPC decisions |
| 39. Diplomacy | Faction alliances, treaties | 🔴 | Not started | DEFERRED to Phase 4 |
| 40. Media | In-game news, social media sim | 🔴 | Not started | DEFERRED to Phase 5 |

---

## Part IX — Multiplayer (Sections 41–45)

| Section | Spec Requirement | Status | Current State | Gap |
|---------|-----------------|--------|---------------|-----|
| 41. Multiplayer Arch | 10K+ shards, microservices | 🟡 | FishNet in project but disabled. NetworkBootstrapper stub | DEFERRED to Phase 4 |
| 42. Player Orgs | Syndicates, shared treasury | 🔴 | Not started | DEFERRED to Phase 4 |
| 43. PvPvE Territory | Real territorial battles | 🔴 | Not started | DEFERRED to Phase 4 |
| 44. Persistence | Event sourcing, PostgreSQL + Neo4j | 🟡 | JSON save system (SaveManager) | Need: event sourcing in Phase 4 |
| 45. Cross-Play | PC + Console | 🔴 | Not started | DEFERRED to Phase 6 |

---

## Part X — Technical Architecture (Sections 46–51)

| Section | Spec Requirement | Status | Current State | Gap |
|---------|-----------------|--------|---------------|-----|
| 46. Tech Stack | Unity 6.2+ DOTS/ECS | 🟡 | Unity 6 (6000.4.0f1) with MonoBehaviour architecture | DOTS/ECS migration in Phase 4 for NPC scale |
| 47. Client Arch | Assembly definitions | ✅ | Clout + Clout.Editor assemblies | Aligned (may need more asmdef splits later) |
| 48. Server Arch | .NET 9 microservices | 🔴 | No server code | DEFERRED to Phase 4 |
| 49. Database | PostgreSQL + Neo4j + S3 | 🔴 | JSON local save only | DEFERRED to Phase 4 |
| 50. Anti-Cheat | Server-auth + behavioral ML | 🔴 | Not started | DEFERRED to Phase 6 |
| 51. Simulation Tick | 1-4 Hz world simulation | 🟡 | TransactionLedger has day cycle (600s). Economy updates on interval | Need: formalize world tick system (Phase 3) |

---

## Part XI — UI/UX Systems (Sections 52–57)

| Section | Spec Requirement | Status | Current State | Gap |
|---------|-----------------|--------|---------------|-----|
| 52. HUD | 5D heat radar, minimap, cash, weapon | 🟡 | CombatHUD has health, stamina, CLOUT, wanted, ammo, cash | Need: minimap, 5D heat radar (Phase 3) |
| 53. Phone | Contacts, products, map, finances, messages | 🔴 | Not started | Phase 2 Step 9 |
| 54. Command Mode | Top-down tactical | 🔴 | Not started | DEFERRED to Phase 5 |
| 55. War Room | Strategic overlay | 🔴 | Not started | DEFERRED to Phase 5 |
| 56. Investigation Dashboard | Spectator mode | 🔴 | Not started | DEFERRED to Phase 5 |
| 57. Accessibility | Color-blind, screen-reader | 🔴 | Not started | DEFERRED to Phase 6 |

---

## Part XII — Content & Polish (Sections 58–63)

| Section | Spec Requirement | Status | Current State | Gap |
|---------|-----------------|--------|---------------|-----|
| 58. Audio | Procedural music | 🔴 | No audio system | DEFERRED to Phase 5 |
| 59. VFX | Post-processing, particles | 🔴 | Basic URP setup only | DEFERRED to Phase 5 |
| 60. Cinematic Replay | Investigation-powered replays | 🔴 | Not started | DEFERRED to Phase 5 |
| 61. Heist System | 3-phase heists | 🔴 | Not started | DEFERRED to Phase 5 |
| 62. Procedural Events | Emergent crime events | 🔴 | Not started | DEFERRED to Phase 3 |
| 63. Permadeath | Legacy mode | 🔴 | Not started | DEFERRED to Phase 6 |

---

## Part XIII — Production (Sections 64–70)

| Section | Spec Requirement | Status | Current State | Gap |
|---------|-----------------|--------|---------------|-----|
| 64. Dev Phases | Phase 0–7 roadmap | ✅ | NEXT_STEPS_ROADMAP.md | Needs alignment with v2.0 phase numbering |
| 65. Next Steps | Immediate actions | ✅ | NEXT_STEPS_ROADMAP.md | Step 6 = next |
| 66. Asset Pipeline | Synty integration guide | ✅ | SYNTY_ASSET_LIST.md | Aligned |
| 67. Monetization | Fair, cosmetic-only | ✅ | Documented in multiple docs | Aligned |
| 68. Post-Launch | Year 1-3 roadmap | ✅ | Documented | Aligned |
| 69. Performance | Target metrics | ✅ | BUILD_SPECIFICATION.md | Aligned |
| 70. Ethics | Community guardrails | ✅ | Documented | Aligned |

---

## Critical Gaps — Must Address Before Step 6

These are items from the spec that should be enhanced/caught up BEFORE or DURING Step 6 implementation:

### 1. ReputationManager → 4D Vector (Priority: HIGH)

**Current:** Single CLOUT score integer
**Spec requires:** `[Fear, Respect, Reliability, Ruthlessness]` as 4D vector (0.0–1.0 each)
**Why now:** Worker hiring (Step 6) should be gated by reputation vector, not just CLOUT score. Better dealers require high Respect. Guards require high Fear generation. Betrayal formula references these values.
**Action:** Upgrade ReputationManager to track 4D vector + keep CLOUT as composite score.

### 2. EconomyManager Price Formula (Priority: MEDIUM)

**Current:** Basic supply/demand with random swing (±30%)
**Spec requires:** `P = P_base × (D/S) × (1 + E_r) × (1 + R_m) × M_s`
**Why now:** Worker dealer earnings depend on accurate pricing. Shop prices should reflect market conditions.
**Action:** Implement full price formula in EconomyManager.GetStreetPrice().

### 3. EmployeeDefinition Enhancement (Priority: HIGH)

**Current:** Basic stats: skill, loyalty, discretion, ambition, dailyWage, hiringCost, betrayalChance
**Spec requires:** Additional: greed, courage, intelligence, corruptibility, knownInformation, directHandler
**Why now:** These feed directly into the betrayal formula and compartmentalization mechanics needed for Step 6.
**Action:** Expand EmployeeDefinition SO with full NPC profile stats.

### 4. EventBus — Missing Event Types (Priority: MEDIUM)

**Current events:** DealCompleted, ProductCooked, PropertyPurchased/Raided/Upgraded, WorkerHired, MoneyChanged, HeatChanged, WantedLevelChanged, DistrictEntered
**Spec needs:** WorkerFired, WorkerArrested, WorkerBetrayed, WorkerShiftEnd, WorkerDealComplete, ReputationChanged
**Action:** Add new event structs to EventBus.cs.

---

## Items That Do NOT Need Catching Up (Correctly Deferred)

These are spec features that belong in later phases and should NOT be built now:

- Signature/forensics system (Phase 3)
- Stealth combat (Phase 3)
- Vehicle system (Phase 3)
- 5D heat system (Phase 3 — can upgrade incrementally)
- Skill/attribute framework (Phase 3)
- Civilian population (Phase 3-4)
- AI faction simulation (Phase 4)
- Investigation graph (Phase 4-5)
- Multiplayer infrastructure (Phase 4)
- DOTS/ECS migration (Phase 4)
- Global supply chain (Phase 4)
- War Room / Command Mode (Phase 5)
- HDRP migration (Phase 5)
- Procedural music (Phase 5)
- All Phase 6 features

---

## Recommended Execution Order for Current Sprint

```
1. Catch-up tasks (enhance existing systems to align with v2.0):
   a. ReputationManager → 4D vector [Fear, Respect, Reliability, Ruthlessness]
   b. EmployeeDefinition → full NPC profile (add greed, courage, intelligence, etc.)
   c. EconomyManager → full price formula
   d. EventBus → add worker event types

2. Step 6: Worker Hiring System (main deliverable)
   a. WorkerManager (singleton)
   b. WorkerInstance (runtime data)
   c. DealerAI (autonomous street dealing)
   d. CookAI (autonomous production)
   e. GuardAI (property security)
   f. RecruitmentManager (hire pool + CLOUT gating)
   g. HireUI + WorkerManagementUI
   h. TestArenaBuilder integration

3. Step 7: Police AI Enhancement
   a. PolicePatrolAI
   b. HeatResponseManager
   c. WitnessSystem
```

---

## Spec v2.0 Coverage Summary

| Category | Sections | Built | Partial | Missing | Deferred |
|----------|----------|-------|---------|---------|----------|
| Vision & Identity | 4 | 2 | 1 | 1 | 0 |
| Player Systems | 4 (+subs) | 3 | 4 | 3 | 0 |
| Empire Systems | 7 | 1 | 4 | 2 | 0 |
| World Systems | 7 | 1 | 2 | 4 | 0 |
| Economy & Markets | 3 | 0 | 1 | 2 | 0 |
| Law Enforcement | 5 | 0 | 1 | 4 | 0 |
| AI & Factions | 5 | 0 | 2 | 3 | 0 |
| Social & Political | 5 | 0 | 1 | 4 | 0 |
| Multiplayer | 5 | 0 | 2 | 3 | 0 |
| Technical | 6 | 1 | 2 | 3 | 0 |
| UI/UX | 6 | 0 | 1 | 5 | 0 |
| Content & Polish | 6 | 0 | 0 | 6 | 0 |
| Production | 7 | 5 | 1 | 1 | 0 |
| **TOTAL** | **70** | **13** | **22** | **41** | **—** |

**Overall coverage: 13 built + 22 partial = 50% of spec has code touching it. Remaining 41 sections are Phase 3+ features.**

---

*CLOUT Gap Analysis v1.0 — SlicedLabs — March 31, 2026*
