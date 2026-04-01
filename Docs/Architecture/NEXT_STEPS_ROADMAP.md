# CLOUT — Full Development Roadmap
# From Vertical Slice to Criminal Universe Operating System

> Last Updated: March 31, 2026
> Canonical Spec: `Docs/Architecture/BUILD_SPECIFICATION.md` (v2.0 — 70 sections)
> Vision Doc: `Docs/Design/CRIMINAL_ECOSYSTEM_2026.md`
> Gap Analysis: `Docs/Architecture/GAP_ANALYSIS.md`

---

## Current State: 101 Scripts, Phase 2 Step 5 Complete

### What's Playable Right Now

```
Player spawns in procedural city block (160×160m) →
  8 buildings (safehouses, labs, growhouses, shops, warehouse, nightclub, auto shop, restaurant) →
  3 shop NPCs (ingredient supplier, fence, weapon dealer) →
  3 customer NPCs (seeking product) →
  3 enemy NPCs (melee/ranged/hybrid) →
  Full melee + ranged Souls-like combat →
  Buy ingredients → Cook at crafting station → Deal to customers →
  Earn dirty cash → Buy/sell at shops → Purchase properties →
  Manage stash → Upgrade properties → Track finances →
  Heat system (6-tier wanted level) →
  Street grid with roads, sidewalks, lane markings
```

### System Inventory (101 scripts)

| System | Scripts | Status |
|--------|---------|--------|
| Core State Machine | 7 | ✅ Complete |
| Controller Actions | 7 | ✅ Complete |
| Combat System | 9 | ✅ Complete |
| Camera System | 2 | ✅ Complete |
| Animation | 1 | ✅ Complete |
| Player | 2 | ✅ Complete |
| AI System | 7 | ✅ Complete |
| Network (offline stub) | 1 | ✅ Stub |
| Empire — Crafting | 8 | ✅ Complete |
| Empire — Dealing | 10 | ✅ Complete |
| Empire — Economy | 4 | ✅ Complete |
| Empire — Properties | 5 | ✅ Complete |
| Empire — Employees | 1 | 🟡 Template only |
| Empire — Reputation | 1 | ✅ Complete |
| Empire — Territory | 1 | ✅ Complete |
| World — Police | 1 | ✅ Basic |
| World — NPCs | 3 | ✅ Complete |
| World — Procedural | 2 | ✅ Complete |
| Stats | 1 | ✅ Complete |
| Inventory | 2 | ✅ Complete |
| UI / HUD | 6 | ✅ Complete |
| Editor Tools | 9 | ✅ Complete |
| Utils | 4 | ✅ Complete |
| Save System | 1 | ✅ Complete |

---

## Phase 2 Remaining Steps (Current Sprint)

### Step 5.5: Spec v2.0 Catch-Up — Align Existing Systems Before New Features
**Priority: IMMEDIATE (before Step 6)**
**Estimated changes: 4 files modified, 0 new files**

The Build Specification v2.0 (70 sections) revealed 4 systems that need enhancement before the Worker system can be built correctly. See `GAP_ANALYSIS.md` for full breakdown.

#### 5.5A: ReputationManager → 4D Reputation Vector

**Current:** Single CLOUT score (integer rank 0–6)
**Spec v2.0 Section 36:** `[Fear, Respect, Reliability, Ruthlessness]` as 4D float vector

**Why before Step 6:** The betrayal formula (`P(betray) = (Greed + Fear - Loyalty + ExternalOffer) / Compartmentalization`) references the Fear dimension. Worker hiring quality should be gated by Respect, not just CLOUT rank. Guard effectiveness depends on Fear generation.

**Action:** Add 4 float fields to ReputationManager. Keep existing CLOUT rank as a composite derived score. Update EventBus with `ReputationChangedEvent`.

#### 5.5B: EmployeeDefinition → Full NPC Profile

**Current:** skill, loyalty, discretion, ambition, dailyWage, hiringCost, betrayalChance, arrestChance, hasRecord, role
**Spec v2.0 Section 13:** Add greed, courage, intelligence, corruptibility, knownInformation[], directHandler

**Why before Step 6:** The betrayal probability formula requires `Greed` as a field. `Courage` determines if workers flee or fight during raids. `Intelligence` affects CookAI quality bonus. These are the core stats the Worker AI decision-making reads.

**Action:** Add missing fields to EmployeeDefinition.cs SO.

#### 5.5C: EconomyManager → Full Price Formula

**Current:** Basic `GetStreetPrice()` with supply/demand and random swing
**Spec v2.0 Section 23:** `P = P_base × (D/S) × (1 + E_r) × (1 + R_m) × M_s`

**Why before Step 6:** DealerAI auto-pricing must use the real formula. Otherwise dealer earnings won't reflect market conditions, heat risk premiums, or seasonal modifiers.

**Action:** Implement full formula in EconomyManager with elasticity per region, risk modifier from WantedSystem heat, and seasonal multiplier stub.

#### 5.5D: EventBus → Worker Event Types

**Current:** 11 event types defined
**Needed for Step 6:** WorkerFiredEvent, WorkerArrestedEvent, WorkerBetrayedEvent, WorkerShiftEndEvent, WorkerDealCompleteEvent, ReputationChangedEvent

**Action:** Add 6 new event structs to EventBus.cs.

---

### Step 6: Worker Hiring — Autonomous Dealer AI, Recruitment, Wage System
**Priority: NEXT (after 5.5 catch-up)**
**Estimated new scripts: 8–10**

This is the automation layer — the step where the game transforms from "you do everything yourself" to "you run an organization." Workers are the bridge between the dealing/production loop and the empire management vision.

#### 6A: WorkerManager (Singleton)

Central manager for all hired workers. Tracks active employees, processes daily wages, handles firing/death/arrest events.

```
WorkerManager
├── hiredWorkers: List<WorkerInstance>
├── maxWorkers (scales with CLOUT rank)
├── HireWorker(EmployeeDefinition, Property, EmployeeRole)
├── FireWorker(workerId)
├── ProcessDailyWages() → draws from CashManager
├── GetWorkersByRole(EmployeeRole)
├── GetWorkersByProperty(propertyId)
├── OnWorkerArrested / OnWorkerKilled / OnWorkerBetrayed events
└── Subscribes to TransactionLedger.OnDayEnd for wage processing
```

**Key design decisions:**
- Workers are MonoBehaviours with NavMeshAgent, spawned in the world
- Each worker has a `WorkerInstance` runtime data class tracking state
- Max workers gated by CLOUT rank (rank 1 = 2 workers, rank 5 = 15+)
- Workers assigned to specific properties (must own a property to hire)

#### 6B: DealerAI (Autonomous Street Dealing)

The first worker type. Autonomous NPC that follows a route and deals to customers without player intervention.

```
DealerAI : MonoBehaviour
├── States: Idle → Traveling → Seeking → Dealing → Returning → Fleeing
├── assignedProperty: Property (home base, collects product from stash)
├── route: List<Vector3> (patrol waypoints in territory)
├── carriedProduct: ProductStack (loaded from property stash)
├── cashOnHand: float (collected from deals, returns to stash)
│
├── Behavior Loop:
│   1. Load product from assigned property stash
│   2. Walk route waypoints (NavMeshAgent)
│   3. Detect nearby CustomerAI (OverlapSphere)
│   4. Initiate auto-deal (price = street value × worker skill modifier)
│   5. Customer accepts/rejects based on quality + price
│   6. On sale: cash collected, product decremented
│   7. When product empty OR shift over → return to property
│   8. Deposit cash to property stash → CashManager.EarnDirty()
│   9. Take cut (wage + loyalty-based tip)
│
├── Risk Events:
│   - Robbery (rival NPCs or players can rob)
│   - Arrest (police detection based on heat + discretion stat)
│   - Death (combat encounters)
│   - Betrayal (loyalty check: P = (greed + fear - loyalty + externalOffer) / compartmentalization)
│
└── Events: OnDealComplete, OnShiftEnd, OnArrested, OnKilled, OnBetrayed
```

**Betrayal formula** (from Criminal Ecosystem spec):
```
P(betray) = (Greed + Fear - Loyalty + ExternalOfferValue) / CompartmentalizationFactor
```

#### 6C: CookAI (Autonomous Production)

Second worker type. Operates crafting stations without player input.

```
CookAI : MonoBehaviour
├── States: Idle → Loading → Cooking → Storing → Resting
├── assignedStation: CraftingStation
├── assignedRecipe: RecipeDefinition
├── skill: float (0-1, affects output quality)
│
├── Behavior Loop:
│   1. Check station availability
│   2. Pull ingredients from property stash
│   3. Start batch (quality = base × skill modifier)
│   4. Wait for production time
│   5. Store output in property stash
│   6. Rest period (scales with station heat)
│
└── Risk: explosion chance modified by skill, contamination events
```

#### 6D: GuardAI (Property Security)

Third worker type. Defends properties from raids and rival attacks.

```
GuardAI : MonoBehaviour
├── Extends AIStateManager combat behavior
├── Patrols property perimeter
├── Engages hostile NPCs/players entering property zone
├── Security level contribution: +0.1 per guard skill level
├── Raid defense: guards fight police during property raids
└── Alert system: notifies player of threats via EventBus
```

#### 6E: RecruitmentManager

How players find and hire workers.

```
RecruitmentManager
├── availableRecruits: List<EmployeeDefinition> (refreshes daily)
├── recruitmentLocations: bars, street corners, online (phone)
├── CLOUT-gated tiers:
│   Rank 0-1: Street-level (low skill, high betrayal)
│   Rank 2-3: Mid-tier (moderate stats)
│   Rank 4-5: Professional (high skill, expensive)
│   Rank 6+: Elite (rare, custom loyalty contracts)
├── GenerateRecruitPool(cloutRank)
├── HireRecruit(EmployeeDefinition) → cost from CashManager
└── Events: OnRecruitPoolRefreshed
```

#### 6F: HireUI + WorkerManagementUI

```
HireUI (OnGUI)
├── Browse available recruits (name, role, stats, cost)
├── Filter by role type
├── Stat comparison (skill, loyalty, discretion, wage)
├── Hire button → CashManager.Spend(hiringCost)
├── Assignment selector (which property, which role)

WorkerManagementUI (OnGUI)
├── List all hired workers
├── Per-worker: name, role, assigned property, stats, daily earnings
├── Fire worker button
├── Reassign worker to different property
├── Worker loyalty indicator (green/yellow/red)
├── Daily wage total, income vs expense breakdown
```

#### 6G: Wire Into Existing Systems

- `DealManager` → DealerAI uses same deal logic but NPC-to-NPC
- `CraftingStation` → CookAI calls StartBatch() programmatically
- `Property.stash` → Workers load/unload from property stash
- `CashManager` → Daily wages via ProcessDailyWages()
- `TransactionLedger` → All worker transactions categorized as "wages"
- `ReputationManager` → CLOUT rank gates recruitment quality
- `WantedSystem` → Worker arrests contribute to player heat
- `EventBus` → `WorkerHiredEvent` (already defined), new: `WorkerFiredEvent`, `WorkerArrestedEvent`, `WorkerBetrayedEvent`

#### 6H: TestArenaBuilder Integration

Update TestArenaBuilder to spawn:
- RecruitmentManager with 5 test recruits (2 dealers, 2 cooks, 1 guard)
- WorkerManager singleton
- Recruitment interaction point (bar/corner)

---

### Step 7: Police AI Enhancement — Patrol, Investigate, Pursue, Arrest
**Estimated new scripts: 5–7**

#### Current State
- WantedSystem exists with 6-tier heat
- No actual police NPCs in the world
- Heat accumulates from combat/dealing but triggers no response

#### What's Needed

```
PolicePatrolAI : AIStateManager (extends existing AI)
├── States: Patrol → Investigate → Pursue → Arrest → Combat → CallBackup
├── Patrol: follow NavMesh waypoint routes through district
├── Investigate: respond to heat sources (gunfire audio, reported deals, witnesses)
├── Pursue: chase player/worker on foot
├── Arrest: non-lethal takedown at low wanted levels (stun + confiscate)
├── Combat: lethal force at high wanted levels
├── CallBackup: spawns additional officers at high heat

HeatResponseManager (Singleton)
├── Monitors WantedSystem heat levels globally
├── Spawns police based on heat brackets:
│   Heat 0-20: Normal patrols (2 officers)
│   Heat 20-40: Increased patrols (4 officers)
│   Heat 40-60: Active investigation (6 officers + detective)
│   Heat 60-80: Pursuit mode (8 officers + helicopter sound FX)
│   Heat 80-100: SWAT response (10+ officers, property raids trigger)
├── Police spawn at station locations, patrol toward heat source

WitnessSystem
├── Civilian NPCs who see crimes generate heat
├── Evidence degrades over time (30s → 5min based on severity)
├── Player can intimidate witnesses (fear check)
├── Destroying evidence at crime scenes reduces heat gain

PropertyRaidSystem
├── Triggered by PropertyManager when raid check succeeds
├── Spawns police squad at property entrance
├── Guards engage, stash can be confiscated
├── Player can defend or flee
├── Outcome: stash loss, property damage, arrest risk
```

---

### Step 8: District System — From Test Arena to Playable City
**Estimated new scripts: 6–8**

```
DistrictManager
├── Manages one district zone: NPCs, demand, heat, control level
├── Tracks: who controls district, current demand per product, active NPCs
├── Spawns: customers, police, ambient civilians on timer
├── Dynamic demand curves per product type

DistrictDefinition (ScriptableObject)
├── districtName, baseDemand, wealthLevel, policePresence
├── preferredProducts[], maxCustomers, maxPolice
├── propertySlots (available properties for purchase)

ProceduralDistrictGenerator
├── Extends current ProceduralPropertyBuilder to full districts
├── Multi-block generation with road networks
├── Zone types: residential, commercial, industrial, waterfront
├── POI placement: shops, bars, corners, parks, police stations

SceneTransitionManager
├── Additive scene loading for district interiors
├── Property enter/exit flow
├── Loading screens with district flavor text
```

---

### Step 9: Phone UI — Empire Management Device
**Estimated new scripts: 5–7**

```
PhoneController
├── Tab/DPad-Up to open in-game phone
├── Tabs: Map, Contacts, Products, Finances, Messages
│
├── MapTab: district with territory control overlay + heat radar
├── ContactsTab: workers, suppliers, customers (loyalty bars)
├── ProductsTab: inventory summary, pricing, quality breakdown
├── FinanceTab: income/expense charts, TransactionLedger data
├── MessagesTab: worker reports, tips, warnings, deal notifications

Minimap (HUD)
├── Render texture from top-down camera
├── Color-coded blips: player, customers, police, workers, properties
├── Expand to full map on phone
```

---

### Step 10: Integration & Polish Pass
**Estimated new scripts: 3–5**

- Wire all systems end-to-end
- Game flow tutorial prompts
- Balance pass (prices, wages, heat rates)
- Save system enhancement (serialize workers, properties, ledger)
- Performance profiling

---

## Phase 3: Advanced Empire Systems

> **Timeline: Weeks 7–12**
> **Focus: Depth, consequences, and the investigation layer**

### Step 11: Money Laundering Pipeline

Full 5-step laundering system replacing basic `Launder()` method:

```
LaunderingManager
├── Business fronts: Restaurant, AutoShop, Nightclub (from existing PropertyTypes)
├── Daily laundering capacity per business (scales with revenue)
├── Structuring detection: AI flags deposits > threshold
├── Velocity anomaly detection: too-fast laundering raises flags
├── Front business simulation: actual customers, revenue, expenses
├── IRS audit events at high laundering volume
```

### Step 12: Signature & Forensics System

```
BatchSignature (512-bit vector per production batch)
├── Generated from: facility seed + recipe + worker skill + equipment hash
├── Propagated through distribution chain
├── LE cosine similarity clustering traces batches to source
├── Signature scrubbing: equipment upgrade, costs yield
├── Batch recall: if signature compromised, all downstream sales traced

ForensicLabAI
├── Processes seized evidence
├── Links signatures to facilities
├── Builds investigation graph edges
```

### Step 13: Advanced Economy

```
MarketSimulator (replaces basic EconomyManager)
├── Full supply/demand curves per district per product
├── Price elasticity modeling
├── Competition effects (rival dealers suppress your prices)
├── Market events: drought, bust, festival demand spike
├── Global commodity prices for precursors
├── Player can manipulate markets (dump product, create artificial scarcity)
```

### Step 14: Rival Faction AI

```
FactionManager
├── 3-5 AI rival organizations per shard
├── Each runs same simulation loop as player:
│   production → distribution → territory → laundering
├── Faction decision weights: aggressive, cautious, diplomatic
├── Can form alliances, declare war, absorb smaller factions
├── Territorial skirmishes with player empire
├── Diplomacy UI: negotiate, threaten, bribe, ally
```

### Step 15: Advanced Police & Investigation

```
InvestigationGraph
├── Node types: Person, Location, Asset, Transaction, Batch
├── Edge types: temporal, financial, communication, physical
├── Centrality algorithms (PageRank, Betweenness) every 30 game-days
├── Detective AI: assigns cases, builds evidence, requests warrants
├── Undercover operations: plainclothes officers build trust
├── RICO charges: if graph connects enough nodes, empire-wide bust
```

---

## Phase 4: World Expansion & Multiplayer

> **Timeline: Weeks 13–20**
> **Focus: Scale, multiplayer, global reach**

### Step 16: Multi-District City

- 4+ procedural districts with distinct character
- District-to-district travel and territory borders
- Property market across districts (prices vary by location)
- District events (block party = demand spike, construction = access restricted)

### Step 17: Global Supply Chain

- Source region selection (risk vs. cost vs. quality)
- Transit hub routing (smuggling mini-game)
- Import/export mechanics
- International contacts and partnerships

### Step 18: Multiplayer Foundation

- Re-enable FishNet / migrate to Netcode for GameObjects
- Server-authoritative economy
- Player organization system (syndicates, hierarchies, permissions)
- Territory wars (real-time PvPvE zone control)
- Anti-cheat foundation

### Step 19: Network Graph System

- Player-to-player trust/debt/info-flow graph
- Compartmentalization mechanics
- Informant system (flip enemy workers for intel)
- Communication interception (wiretaps)

---

## Phase 5: Content & Polish

> **Timeline: Weeks 21–30**
> **Focus: Production quality, content variety, accessibility**

### Step 20: Procedural Music System

- Dynamic music layers that intensify with heat
- Ambient district-specific soundscapes
- Combat music transitions

### Step 21: Advanced Procedural Generation

- Full San Francisco template with landmark zones
- Interior generation for all property types
- Furniture and prop placement algorithms
- NPC home/work/leisure schedule simulation

### Step 22: UI/UX Polish

- Migrate OnGUI → UI Toolkit
- War Room strategic overview
- Network graph viewer (zoomable, filterable)
- Heat radar map
- Legend timeline (personal history)

### Step 23: Accessibility Pass

- Color-blind modes
- Screen-reader compatible dashboards
- One-button economy macros
- Remappable controls (already using Input System)

### Step 24: Content Pipeline

- 24 source region definitions
- 50+ NPC personality templates
- 20+ property interior variants
- 30+ recipe combinations
- Vehicle system (ownership, mods, transport)

---

## Phase 6: Ship

> **Timeline: Weeks 31–40**
> **Focus: Scale testing, live ops, launch**

### Step 25: Scale Testing

- 10k concurrent player stress test
- Economy stability under player manipulation
- Investigation AI performance at scale
- Server cost optimization (Spot instances, auto-scaling)

### Step 26: Live Ops Infrastructure

- Seasonal events engine
- Modding API (Lua scripting for custom game rules)
- Community tools (faction leaderboards, economy dashboards)
- Telemetry and analytics pipeline

### Step 27: Early Access Launch (Q3 2026 Target)

- 1 city (4 districts)
- Core loop: produce → deal → earn → buy → hire → manage → evade
- 3 rival AI factions
- Basic multiplayer (co-op empire, 2-4 players)
- Investigation system (detective AI, evidence, warrants)

### Step 28: Full Launch (Q4 2027 Target)

- 4 cities with 8 consumption megacities
- Global supply chain
- 10k+ player shards
- Full investigation graph
- War Room UI
- Modding API

---

## Post-Launch Roadmap

| Timeframe | Content |
|-----------|---------|
| Year 1 | New global regions, advanced laundering mechanics (free update) |
| Year 2 | Mobile companion app for managing remote facilities |
| Year 3 | VR War Room mode |

---

## Monetization Plan (Fair & Player-First)

- **Cosmetic only**: syndicate logos, vehicle skins, safehouse themes
- **Expansion Packs**: new global regions (not pay-to-win)
- **No loot boxes** — ever

---

## Script Count Projections

| Phase | New Scripts | Running Total |
|-------|-------------|---------------|
| Phase 1 (complete) | 62 | 62 |
| Phase 2 Steps 1-5 (complete) | 39 | 101 |
| Phase 2 Steps 6-10 (next) | ~35 | ~136 |
| Phase 3 (advanced empire) | ~25 | ~161 |
| Phase 4 (world + multiplayer) | ~30 | ~191 |
| Phase 5 (content + polish) | ~20 | ~211 |
| Phase 6 (ship) | ~15 | ~226 |

---

## Immediate Next Action

**Build Phase 2 Step 6: Worker Hiring System**

Files to create:
1. `Empire/Employees/WorkerManager.cs` — singleton workforce orchestrator
2. `Empire/Employees/WorkerInstance.cs` — runtime worker data
3. `Empire/Employees/DealerAI.cs` — autonomous street dealing behavior
4. `Empire/Employees/CookAI.cs` — autonomous production behavior
5. `Empire/Employees/GuardAI.cs` — property security behavior
6. `Empire/Employees/RecruitmentManager.cs` — hire pool generation + CLOUT gating
7. `UI/Employees/HireUI.cs` — recruitment interface
8. `UI/Employees/WorkerManagementUI.cs` — worker overview + assignment
9. `Editor/WorkerSystemFactory.cs` — editor tool for test scene integration
10. Update `TestArenaBuilder.cs` — spawn WorkerManager + recruitment point

---

*CLOUT Development Roadmap v2.0 — SlicedLabs — March 31, 2026*
