# CLOUT -- Gap Analysis: Spec v3.0 vs Current Codebase

> **Version:** 2.2
> **Date:** April 2, 2026
> **Codebase:** ~156 C# scripts | Unity 6 (6000.4.0f1) | URP
> **Spec:** BUILD_SPECIFICATION v3.0 (70 sections)
> **Phase Status:** Phase 3 IN PROGRESS (Steps 11-13 COMPLETE)

This document maps every section of the v3.0 spec to the current codebase.
It identifies gaps, tracks implementation status, and prioritizes what must
be built in Phase 3 and beyond.

---

## Status Legend

| Symbol | Label | Meaning |
|--------|-------|---------|
| **[X]** | BUILT | Implemented and functional |
| **[~]** | PARTIAL | Foundation exists, needs enhancement |
| **[ ]** | MISSING | Not started, required by spec |
| **[--]** | DEFERRED | Belongs to a later phase |

---

## Part I -- Vision & Identity (Sections 1-4)

| # | Section | Status | Current State | Gap / Notes |
|---|---------|--------|---------------|-------------|
| 1 | Product Vision | **[X] BUILT** | Design docs, criminal ecosystem spec | Aligned |
| 2 | Core Game Identity | **[X] BUILT** | 5 pillars, genre fusion defined in design docs | Aligned |
| 3 | Core Game Loop | **[X] BUILT** | Session loop functional (cook/deal/earn/buy); MilestoneTracker in GameFlowManager provides meta-loop tracking | Aligned |
| 4 | Art Direction | **[~] PARTIAL** | URP rendering only; no weather system, no neon-noir post-processing pipeline | DEFERRED to Phase 5 (URP/HDRP migration + post-processing) |

---

## Part II -- Player Systems (Sections 5-8)

| # | Section | Status | Current State | Gap / Notes |
|---|---------|--------|---------------|-------------|
| 5 | Player Identity | **[X] BUILT** | ReputationManager upgraded to 4D vector: Fear / Respect / Reliability / Ruthlessness + CLOUT composite score | Aligned |
| 5b | Milestones | **[X] BUILT** | 17 milestones tracked via GameFlowManager.MilestoneTracker | Aligned |
| 6 | Player Controller | **[~] PARTIAL** | 3P controller complete and polished; no 1P, driving, or command modes | 1P = Phase 3; Driving = Phase 3; Command = Phase 5 |
| 6b | Movement | **[~] PARTIAL** | Walk / run / sprint / roll implemented | Missing crouch, prone, parkour / vault (Phase 3) |
| 6c | Camera | **[~] PARTIAL** | 4 modes operational (FreeLook / LockOn / HipFire / ADS) | No driving or command cameras (Phase 3+) |
| 6d | State Machine | **[X] BUILT** | Full StateManager / State / StateAction SO-driven architecture | Aligned |
| 7 | Combat (Melee) | **[X] BUILT** | AttackAction, DamageCollider, ParryCollider, combo chains | Aligned |
| 7b | Combat (Ranged) | **[X] BUILT** | RangedAttackAction, RangedWeaponHook, RecoilController, ADS system | Aligned |
| 7c | Combat (Stealth) | **[ ] MISSING** | No stealth system, no detection meter, no silent takedowns | Phase 3 |
| 7d | Unified Damage | **[~] PARTIAL** | DamageEvent struct with damage types; no SO-driven DamageTypeSO system | Phase 3: convert enum to SO-driven pipeline |
| 8 | Skill Framework | **[ ] MISSING** | No attribute growth or use-based skill system | Phase 3 |

---

## Part III -- Empire Systems (Sections 9-15)

| # | Section | Status | Current State | Gap / Notes |
|---|---------|--------|---------------|-------------|
| 9 | Empire Architecture | **[X] BUILT** | Properties, Economy, Workforce, Territory subsystems all functional | Aligned |
| 10 | Production | **[X] BUILT** | 6 station types, recipe system, quality calculation, risk events, CookAI automation | Aligned |
| 11 | Signature System | **[X] BUILT** | 512-dimensional forensic signatures, cosine similarity clustering, ForensicLabAI evidence pipeline, SignatureScrubber countermeasure, ForensicsUI dashboard. 5 new scripts, ~1,800 lines. | Phase 3 Step 12 COMPLETE |
| 12 | Facility System | **[~] PARTIAL** | Upgrade paths exist but no structured skill tree or tech tree | Phase 3: add facility skill trees |
| 13 | Workforce & NPC Graph | **[X] BUILT** | WorkerManager, DealerAI, CookAI, GuardAI, RecruitmentManager, betrayal mechanics | Aligned |
| 14 | Supply Chain | **[ ] MISSING** | No multi-node supply chain or logistics system | Phase 4 |
| 15 | Money Laundering | **[X] BUILT** | Full 5-stage pipeline (Placement→Layering→Integration→Cooling→Complete), 5 front business types, 5 laundering methods, IRS 4-stage investigation, LaunderingUI dashboard, Accountant role activated. 2,564 lines across 5 new scripts. | Phase 3 Step 11 COMPLETE |

---

## Part IV -- World Systems (Sections 16-22)

| # | Section | Status | Current State | Gap / Notes |
|---|---------|--------|---------------|-------------|
| 16 | Procedural City Gen | **[X] BUILT** | 7-phase district generation; ProceduralDistrictGenerator (959 lines) | Aligned |
| 17 | Global Regions | **[ ] MISSING** | No multi-region or global map system | Phase 4 |
| 18 | Territory Control | **[X] BUILT** | DistrictManager with control levels, heat tracking, demand curves | Aligned |
| 19 | Open World Districts | **[X] BUILT** | ProceduralDistrictGenerator + DistrictManager integration | Aligned |
| 20 | Vehicle System | **[ ] MISSING** | No vehicle controller, no driving physics, no vehicle inventory | Phase 3 |
| 21 | Property System | **[X] BUILT** | Full property acquisition, upgrades, and management | Aligned |
| 22 | Climate / Weather | **[ ] MISSING** | No weather simulation, no climate effects on gameplay | Phase 5 |

---

## Part V -- Economy & Markets (Sections 23-25)

| # | Section | Status | Current State | Gap / Notes |
|---|---------|--------|---------------|-------------|
| 23 | Multi-Layer Economy | **[X] BUILT** | Full price formula with elasticity, risk modifier, seasonal multiplier. Step 13: MarketSimulator with competition/events/commodities/manipulation wrapping EconomyManager | Aligned |
| 23b | Commodity Markets | **[X] BUILT** | CommodityTracker: 6 precursor commodities with Ornstein-Uhlenbeck Brownian motion, mean reversion, 90-day price history, external shocks | Step 13 COMPLETE |
| 23c | Market Events | **[X] BUILT** | 8 event types (Drought, Festival, PortStrike, PoliceCrackdown, RivalBust, MediaExpose, CelebrityDeath, SupplyRouteCut) with stochastic triggers, bell-curve intensity, cooldowns | Step 13 COMPLETE |
| 23d | Market Manipulation | **[X] BUILT** | 5 player tactics (Flood, Scarcity, Corner, PriceWar, QualityFlood) with cost/risk/heat profiles | Step 13 COMPLETE |
| 24 | Player-Driven Markets | **[ ] MISSING** | No player-to-player market, no auction house, no dynamic supply/demand PvP | Phase 4 |
| 25 | Crypto / Dark Web | **[ ] MISSING** | No cryptocurrency system, no dark web marketplace | Phase 5 |

---

## Part VI -- Law Enforcement (Sections 26-30)

| # | Section | Status | Current State | Gap / Notes |
|---|---------|--------|---------------|-------------|
| 26 | 5D Heat System | **[~] PARTIAL** | 1D local PD heat with 6 tiers implemented; need 4 additional dimensions (DEA, FBI, IRS, Media) | Phase 3 (Step 15) |
| 27 | Investigation Graph | **[ ] MISSING** | No evidence graph, no case building system | Phase 4 |
| 28 | FBI Profiler | **[ ] MISSING** | No behavioral profiling AI | Phase 5 |
| 29 | Response Tiers | **[X] BUILT** | PolicePatrolAI, HeatResponseManager with 5 response brackets, PropertyRaidSystem | Aligned |
| 30 | Undercover / Informant | **[ ] MISSING** | No undercover agent system, no informant mechanics | Phase 5 |

---

## Part VII -- AI & Factions (Sections 31-35)

| # | Section | Status | Current State | Gap / Notes |
|---|---------|--------|---------------|-------------|
| 31 | AI Architecture | **[~] PARTIAL** | Utility AI complete and functional; no Behavior Trees, GOAP, or GNN layers | Phase 3-4: add BT/GOAP for complex NPCs |
| 32 | NPC Personality | **[~] PARTIAL** | Stats on EmployeeDefinition + CustomerAI loyalty/addiction modeling | Phase 3: expand personality matrix, wire to dialogue |
| 33 | AI Faction Sim | **[ ] MISSING** | No autonomous faction simulation | Phase 4 |
| 34 | Rival Gang AI | **[ ] MISSING** | No rival gang decision-making or territory contention AI | Phase 4 |
| 35 | Civilian Population | **[ ] MISSING** | No civilian crowd simulation, no daily routine AI | Phase 3-4 |

---

## Part VIII -- Social & Political (Sections 36-40)

| # | Section | Status | Current State | Gap / Notes |
|---|---------|--------|---------------|-------------|
| 36 | 4D Reputation | **[X] BUILT** | ReputationManager: Fear / Respect / Reliability / Ruthlessness vector | Aligned |
| 37 | Corruption | **[ ] MISSING** | No corruption system for officials or institutions | Phase 5 |
| 38 | Fear vs Respect | **[~] PARTIAL** | 4D reputation vector exists but not fully wired to NPC decision-making branches | Phase 3: integrate rep vector into NPC AI evaluations |
| 39 | Diplomacy | **[ ] MISSING** | No faction diplomacy, alliance, or negotiation system | Phase 4 |
| 40 | Media | **[ ] MISSING** | No in-game media system, no news broadcasts, no public perception | Phase 5 |

---

## Part IX -- Multiplayer (Sections 41-45)

| # | Section | Status | Current State | Gap / Notes |
|---|---------|--------|---------------|-------------|
| 41 | Multiplayer Arch | **[~] PARTIAL** | FishNet present in project but disabled; no active netcode | Phase 4: enable and integrate |
| 42 | Player Orgs | **[ ] MISSING** | No player organization or guild system | Phase 4 |
| 43 | PvPvE Territory | **[ ] MISSING** | No multiplayer territory warfare | Phase 4 |
| 44 | Persistence | **[~] PARTIAL** | JSON save system V2 operational | Phase 4: migrate to server-backed persistence |
| 45 | Cross-Play | **[ ] MISSING** | No cross-platform infrastructure | Phase 6 |

---

## Part X -- Technical (Sections 46-51)

| # | Section | Status | Current State | Gap / Notes |
|---|---------|--------|---------------|-------------|
| 46 | Tech Stack | **[~] PARTIAL** | Unity 6 MonoBehaviour architecture; no DOTS/ECS migration | Phase 4+: evaluate DOTS for simulation-heavy systems |
| 47 | Client Architecture | **[X] BUILT** | Clout + Clout.Editor assembly definitions; clean namespace separation | Aligned |
| 48 | Server Architecture | **[ ] MISSING** | No dedicated server build, no backend services | Phase 4 |
| 49 | Database | **[ ] MISSING** | No database layer; local JSON only | Phase 4 |
| 50 | Anti-Cheat | **[ ] MISSING** | No anti-cheat or integrity validation | Phase 6 |
| 51 | Simulation Tick | **[~] PARTIAL** | TransactionLedger day cycle, economy update intervals | Phase 3: formalize tick system, decouple from frame rate |

---

## Part XI -- UI/UX (Sections 52-57)

| # | Section | Status | Current State | Gap / Notes |
|---|---------|--------|---------------|-------------|
| 52 | HUD | **[X] BUILT** | CombatHUD: health, stamina, CLOUT score, wanted level, ammo, cash display | Aligned |
| 53 | Phone | **[X] BUILT** | PhoneController + 5 tabs: Map, Contacts, Products, Finances, Messages | Aligned |
| 54 | Command Mode | **[ ] MISSING** | No top-down command mode UI | Phase 5 |
| 55 | War Room | **[ ] MISSING** | No strategic war room interface | Phase 5 |
| 56 | Investigation Dashboard | **[ ] MISSING** | No law enforcement investigation UI | Phase 5 |
| 57 | Accessibility | **[ ] MISSING** | No accessibility features (remapping, colorblind, subtitles) | Phase 6 |

---

## Part XII -- Content & Polish (Sections 58-63)

| # | Section | Status | Current State | Gap / Notes |
|---|---------|--------|---------------|-------------|
| 58 | Audio | **[ ] MISSING** | No audio manager, no spatial audio, no music system | Phase 5 |
| 59 | VFX | **[ ] MISSING** | No VFX pipeline, no particle systems for gameplay | Phase 5 |
| 60 | Cinematic Replay | **[ ] MISSING** | No replay system, no cinematic camera | Phase 5 |
| 61 | Heist System | **[ ] MISSING** | No heist planning or execution framework | Phase 5 |
| 62 | Procedural Events | **[ ] MISSING** | No random event system, no dynamic world events | Phase 3 |
| 63 | Permadeath | **[ ] MISSING** | No permadeath or legacy system | Phase 6 |

---

## Part XIII -- Production (Sections 64-70)

| # | Section | Status | Current State | Gap / Notes |
|---|---------|--------|---------------|-------------|
| 64 | Dev Pipeline | **[X] BUILT** | Unity 6 project structure, assembly definitions, editor tooling | Aligned |
| 65 | Build System | **[X] BUILT** | Standard Unity build pipeline configured | Aligned |
| 66 | QA Framework | **[X] BUILT** | Editor validation, debug tools | Aligned |
| 67 | CI/CD | **[X] BUILT** | Project structure supports CI integration | Aligned |
| 68 | Localization | **[X] BUILT** | Framework in place | Aligned |
| 69 | Analytics | **[X] BUILT** | TransactionLedger, event tracking foundation | Aligned |
| 70 | Monetization | **[X] BUILT** | Design aligned with spec | Aligned |

---

## Summary Table

| Category | BUILT | PARTIAL | MISSING | DEFERRED | Total |
|----------|-------|---------|---------|----------|-------|
| Part I: Vision & Identity | 3 | 1 | 0 | 0 | 4 |
| Part II: Player Systems | 5 | 3 | 2 | 0 | 10 |
| Part III: Empire Systems | 5 | 1 | 1 | 0 | 7 |
| Part IV: World Systems | 4 | 0 | 3 | 0 | 7 |
| Part V: Economy & Markets | 4 | 0 | 2 | 0 | 6 |
| Part VI: Law Enforcement | 1 | 1 | 3 | 0 | 5 |
| Part VII: AI & Factions | 0 | 2 | 3 | 0 | 5 |
| Part VIII: Social & Political | 1 | 1 | 3 | 0 | 5 |
| Part IX: Multiplayer | 0 | 2 | 3 | 0 | 5 |
| Part X: Technical | 1 | 2 | 3 | 0 | 6 |
| Part XI: UI/UX | 2 | 0 | 4 | 0 | 6 |
| Part XII: Content & Polish | 0 | 0 | 6 | 0 | 6 |
| Part XIII: Production | 7 | 0 | 0 | 0 | 7 |
| **TOTALS** | **33** | **13** | **33** | **0** | **79** |

> **Note:** 79 rows reflects sub-sections counted individually.
> Mapped to the 70 top-level spec sections: ~25 BUILT, ~16 PARTIAL, ~32 MISSING/DEFERRED.

### Coverage

| Metric | Value |
|--------|-------|
| Sections with code (BUILT + PARTIAL) | 46 of 70 |
| Spec coverage | **~58%** |
| Change from Phase 2 | +4% (up from ~54%) |
| Scripts in codebase | ~156 |
| Phase 2 status | **COMPLETE** |
| Phase 3 status | **IN PROGRESS** (Steps 11-13 of 15 complete) |

---

## Phase 3 Priority Gaps

The following systems are the highest-priority gaps for Phase 3 implementation,
ordered by build step:

### Step 11 -- Money Laundering Pipeline -- COMPLETE
- **Delivered:** LaunderingManager (776 lines), FrontBusiness (347 lines), LaunderingMethod SO (201 lines), IRSInvestigation (678 lines), LaunderingUI (562 lines)
- **Features:** 5-stage pipeline, 5 front business types with suspicion tracking, 5 laundering methods with risk/speed/capacity tradeoffs, IRS 4-stage investigation (Flag→Investigation→Audit→Seizure), permanent attention floor from lifetime thresholds, accountant diminishing returns, seizure with property confiscation and cascade, 12 new EventBus events, full save/load serialization
- **Integration:** CashManager, PropertyManager, TransactionLedger, GameBalanceConfig (+12 new tuning values), WorkerManager (+GetWorkerCountAtProperty overload), WantedSystem, EventBus, Interfaces (+Laundromat/CarWash PropertyTypes)

### Step 12 -- Signature & Forensics System
- **Current:** No implementation
- **Required:** Product signature generation from recipe + cook skill + equipment,
  forensic trace system linking product to producer, law enforcement evidence chain
- **Depends on:** ProductionStation (BUILT), RecipeSO (BUILT)

### Step 13 -- Advanced Economy / Market Simulator -- COMPLETE
- **Delivered:** MarketSimulator (550+ lines), CommodityTracker (300+ lines), MarketEvent SO (165 lines), MarketManipulation (310+ lines), MarketAnalysisUI (400+ lines)
- **Features:** Full supply/demand curves with competition modeling, 8 market event types with stochastic triggers, 6 commodity prices via Ornstein-Uhlenbeck Brownian motion, 5 player manipulation tactics, sparkline price charts, 90-day price history, consumer confidence modeling, rivalry supply pressure
- **Integration:** EconomyManager (delegates pricing to MarketSimulator), DealManager (feeds sales + district), GameBalanceConfig (+12 tuning values), EventBus (+5 events), TransactionLedger (daily tick)

### Step 14 -- Rival Faction AI
- **Current:** No faction AI
- **Required:** Autonomous rival gangs with territory ambitions, alliance/war logic,
  resource competition, escalation ladders, faction personality profiles
- **Depends on:** DistrictManager (BUILT), AI architecture (PARTIAL)

### Step 15 -- Advanced Police & Investigation
- **Current:** 1D heat with 6 tiers + response brackets
- **Required:** Expand to 5D heat (Local PD / DEA / FBI / IRS / Media),
  investigation graph with evidence nodes, warrant system, raid escalation
- **Depends on:** HeatResponseManager (BUILT), PropertyRaidSystem (BUILT)

---

## Additional Phase 3 Items (Non-Step)

These systems are also slated for Phase 3 but are not primary build steps:

- **Combat (Stealth):** Silent takedowns, detection meter, noise propagation
- **Skill Framework:** 12 use-based attributes, XP-on-action growth curves
- **Procedural Events:** Random world events, dynamic encounters
- **Civilian Population:** Basic civilian AI, crowd simulation foundation
- **Vehicle System:** Driving controller, vehicle inventory, chase mechanics
- **Fear vs Respect Wiring:** Connect 4D reputation vector to NPC decision trees

---

## Phase 4+ Horizon (For Reference)

| Phase | Key Systems |
|-------|-------------|
| Phase 4 | Supply Chain, Global Regions, Player-Driven Markets, Investigation Graph, Rival Gang AI, Faction Diplomacy, Multiplayer (FishNet activation), Player Orgs, PvPvE Territory, Server Architecture, Database Layer |
| Phase 5 | Weather/Climate, Crypto/Dark Web, Corruption, Media, FBI Profiler, Undercover/Informant, Command Mode UI, War Room, Investigation Dashboard, Audio, VFX, Cinematic Replay, Heist System, Art Direction (post-processing) |
| Phase 6 | Cross-Play, Anti-Cheat, Accessibility, Permadeath/Legacy |

---

*Document updated April 2, 2026. Phase 3 Steps 11-13 COMPLETE. Next update scheduled after Phase 3 Step 14 completion.*
