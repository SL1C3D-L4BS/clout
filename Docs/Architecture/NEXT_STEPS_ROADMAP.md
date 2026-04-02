# CLOUT -- Development Roadmap v3.0
# From Vertical Slice to Criminal Universe Operating System

> Version: 3.2 -- April 2, 2026
> Status: CANONICAL -- This document supersedes all prior roadmap versions.
> Canonical Spec: `Docs/Architecture/BUILD_SPECIFICATION.md` (v2.0 -- 70 sections)
> Vision Doc: `Docs/Design/CRIMINAL_ECOSYSTEM_2026.md`
> Gap Analysis: `Docs/Architecture/GAP_ANALYSIS.md`

---

## Current State Summary

Phase 2 is complete. The game is a playable single-player criminal empire simulator. A player can spawn into a procedurally generated Bay Area district with full road networks and building types, engage in Souls-like melee and ranged combat, purchase ingredients from shop NPCs, cook product at crafting stations, deal to customers with loyalty and addiction modeling, earn dirty cash, buy and upgrade 8+ property types, hire autonomous workers (dealers, cooks, guards, growers) gated by CLOUT rank, manage a growing organization through the phone UI, and contend with a 6-tier wanted system driving dynamic police response including patrols, investigations, pursuits, and property raids. The full game flow includes milestone tracking, difficulty presets, auto-save, and performance monitoring.

### System Inventory (~156 Scripts)

| System                  | Scripts | Status   |
|-------------------------|---------|----------|
| Core State Machine      | 7       | COMPLETE |
| Controller Actions      | 7       | COMPLETE |
| Combat System           | 9       | COMPLETE |
| Camera System           | 2       | COMPLETE |
| Animation               | 1       | COMPLETE |
| Player                  | 2       | COMPLETE |
| AI System               | 7       | COMPLETE |
| Network (offline stub)  | 1       | STUB     |
| Empire -- Crafting      | 8       | COMPLETE |
| Empire -- Dealing       | 10      | COMPLETE |
| Empire -- Economy       | 8       | COMPLETE (Phase 3 Steps 11+13) |
| Empire -- Laundering    | 4       | COMPLETE (Phase 3 Step 11) |
| Empire -- Properties    | 5       | COMPLETE |
| Empire -- Employees     | 10      | COMPLETE |
| Empire -- Reputation    | 1       | COMPLETE |
| Empire -- Territory     | 1       | COMPLETE |
| Forensics               | 5       | COMPLETE (Phase 3 Step 12) |
| World -- Police         | 5       | COMPLETE |
| World -- Districts      | 4       | COMPLETE |
| World -- NPCs           | 3       | COMPLETE |
| World -- Procedural     | 2       | COMPLETE |
| Stats                   | 1       | COMPLETE |
| Inventory               | 2       | COMPLETE |
| UI / HUD                | 6       | COMPLETE |
| UI / Phone              | 6       | COMPLETE |
| UI / Laundering         | 1       | COMPLETE (Phase 3 Step 11) |
| UI / Economy            | 2       | COMPLETE (Phase 3 Step 13) |
| UI / Forensics          | 1       | COMPLETE (Phase 3 Step 12) |
| Core / Game Flow        | 3       | COMPLETE |
| Editor Tools            | 12      | COMPLETE |
| Utils                   | 4       | COMPLETE |
| Save System             | 1       | COMPLETE |
| **Total**               | **~156** |          |

Core / Game Flow includes GameFlowManager, GameBalanceConfig, and PerformanceMonitor.

---

## Completed Phases (Summary)

### Phase 0: Foundation -- COMPLETE

Project scaffolding, Unity project setup, Input System configuration, core architecture decisions (EventBus, ScriptableObject-driven data, singleton managers).

### Phase 1: Core Loop -- COMPLETE (62 scripts)

State machine, third-person controller, Souls-like combat (melee + ranged), camera system, AI framework (patrol/chase/attack/flee), crafting pipeline, dealing system, customer AI, basic economy, inventory, stats, HUD, test arena builder, and all editor tooling.

### Phase 2: Empire Systems -- COMPLETE (10 steps, ~77 new scripts)

- **Step 1-4:** Crafting expansion, dealing mechanics, economy/cash management, property system.
- **Step 5:** Build Specification v2.0 alignment (4D reputation vector, expanded NPC profiles, full price formula, worker event types).
- **Step 6:** Worker system -- autonomous DealerAI, CookAI, GuardAI, GrowerAI, recruitment with CLOUT-gated tiers, wage processing.
- **Step 7:** Police AI -- patrol/investigate/pursue/arrest states, heat response scaling, witness system, property raids.
- **Step 8:** District system -- procedural generation (7-phase pipeline), district management, 13 ambient building types, scene transitions.
- **Step 9:** Phone UI -- 5-tab empire hub (Map, Contacts, Products, Finances, Messages), 15+ notification types.
- **Step 10:** Integration pass -- GameFlowManager (milestones, tutorial, auto-save), GameBalanceConfig (50+ tuning values, 3 difficulty presets), PerformanceMonitor, SaveManager v2.

---

## Phase 3: Advanced Empire Systems (Weeks 7-12)

> Focus: Depth, consequences, forensic investigation layer, rival organizations.
> Estimated new scripts: ~25
> Running total after phase: ~164
> **Progress: Step 11 COMPLETE (5 scripts, 2,564 lines delivered April 2, 2026)**

---

### Step 11: Money Laundering Pipeline

The current `CashManager.Launder()` is a placeholder. This step replaces it with a full 5-step pipeline where dirty cash must flow through front businesses, each with its own simulation, detection risk, and capacity limits.

#### 11A: LaunderingManager

Central orchestrator for all laundering operations.

```
LaunderingManager (Singleton)
|-- Pipeline Stages:
|   1. PLACEMENT   -- Dirty cash enters front business register
|   2. LAYERING    -- Funds split across methods to obscure origin
|   3. INTEGRATION -- Clean cash deposited to player accounts
|   4. COOLING     -- Mandatory delay before funds are spendable
|   5. EXTRACTION  -- Player withdraws clean cash for use
|
|-- activeFronts: List<FrontBusiness>
|-- dailyLaunderingCap: float (sum of all front capacities)
|-- totalLaundered: float (lifetime, tracked for IRS triggers)
|-- velocityTracker: float[] (rolling 30-day window)
|-- pendingFunds: Queue<LaunderingBatch> (in cooling stage)
|
|-- StartLaundering(amount, method, frontBusiness)
|-- ProcessDailyLaundering() -- called by TransactionLedger.OnDayEnd
|-- GetCleanCashAvailable() -- funds past cooling period
|-- WithdrawCleanCash(amount) -- moves to CashManager clean balance
|-- GetDetectionRisk() -- aggregate risk across all fronts
|
|-- Events: OnLaunderingComplete, OnAuditTriggered, OnFrontBusted
```

Key mechanics:
- Each front business has a maximum daily laundering capacity based on its legitimate revenue (a restaurant doing $2,000/day in real sales can launder ~$3,000 without suspicion).
- Velocity anomaly detection: if laundering volume increases faster than 15% per week, detection risk spikes.
- Cooling period scales with amount (small amounts: 1 day, large amounts: 3-7 days).
- If total laundered exceeds thresholds ($50K, $250K, $1M), IRS scrutiny permanently increases.

#### 11B: FrontBusiness

Each front business is a living simulation that generates legitimate revenue alongside laundered funds.

```
FrontBusiness : MonoBehaviour
|-- businessType: FrontBusinessType (Restaurant, AutoShop, Nightclub)
|-- assignedProperty: Property (must be owned property of matching type)
|-- legitimateRevenue: float (daily, simulated from customer flow)
|-- launderingCapacity: float (function of legitimate revenue)
|-- suspicionLevel: float (0-100, rises with ratio of dirty:clean money)
|-- employees: int (staffing affects revenue and capacity)
|-- operatingCosts: float (rent, wages, supplies)
|-- qualityRating: float (0-5, affects customer flow)
|
|-- Business Simulation:
|   Restaurant -- Customer flow peaks at lunch/dinner, revenue = seats x turnover x avgCheck
|   AutoShop   -- Service appointments, parts markup, cash-heavy transactions ideal for structuring
|   Nightclub  -- Weekend spikes, high cash volume, cover charge + bar revenue
|
|-- LaunderThroughBusiness(amount) -- mix dirty cash into daily revenue
|-- GetDailyReport() -- legitimate vs laundered breakdown
|-- UpgradeBusiness(tier) -- increases capacity and legitimate revenue
|-- SimulateDay() -- generate customers, revenue, expenses
|
|-- Risk Factors:
|   - Revenue-to-expense ratio out of industry norms raises flags
|   - Sudden revenue jumps without corresponding customer increase
|   - Cash deposit patterns (round numbers, just-under-threshold amounts)
```

#### 11C: LaunderingMethod

Different methods carry different risk/reward profiles.

```
LaunderingMethod (ScriptableObject)
|-- methodName: string
|-- riskMultiplier: float
|-- capacityMultiplier: float
|-- coolingPeriodDays: int
|-- requiredCloutRank: int
|
|-- Types:
|   STRUCTURING     -- Split deposits under $10K reporting threshold
|                      Risk: Pattern detection (many sub-threshold deposits)
|                      Capacity: Low, safe for small operations
|                      Unlock: Rank 0
|
|   SMURFING        -- Use multiple workers to make deposits across banks
|                      Risk: Moderate (workers can be traced)
|                      Capacity: Medium, scales with worker count
|                      Unlock: Rank 2
|
|   REAL_ESTATE     -- Buy/sell properties to move large sums
|                      Risk: Low per transaction, high paper trail
|                      Capacity: High per transaction, slow cycle
|                      Unlock: Rank 3
|
|   CASH_INTENSIVE  -- Run through front businesses (default method)
|                      Risk: Depends on business simulation quality
|                      Capacity: Tied to front business revenue
|                      Unlock: Rank 1
|
|   CRYPTO          -- Digital currency mixing (late-game)
|                      Risk: Low if done carefully, blockchain analysis possible
|                      Capacity: Very high, but requires tech investment
|                      Unlock: Rank 5
```

#### 11D: IRSInvestigation

The consequence layer for laundering.

```
IRSInvestigation
|-- auditRisk: float (0-100, persistent)
|-- activeAudit: bool
|-- auditProgress: float (0-1, investigation completion)
|-- frozenAssets: float (seized during audit)
|
|-- Trigger Conditions:
|   - Total laundered exceeds tier thresholds
|   - Velocity anomaly (>15% week-over-week increase)
|   - Structuring pattern detected (multiple sub-threshold deposits)
|   - Front business revenue anomalies flagged
|   - Tips from arrested/betrayed workers
|   - Random audit chance (scales with empire size)
|
|-- Audit Process:
|   1. NOTICE     -- Player receives audit notification (3-day warning)
|   2. REVIEW     -- Assets examined, suspicious transactions flagged
|   3. FREEZE     -- Percentage of clean cash frozen pending investigation
|   4. RESOLUTION -- Fine (10-50% of laundered amount) OR charges
|   5. CHARGES    -- If evidence sufficient, wanted level spike + asset seizure
|
|-- Countermeasures:
|   - Hire accountant (worker type, reduces audit risk)
|   - Diversify methods (no single method > 40% of total)
|   - Maintain realistic front business ratios
|   - Destroy financial records (risky, additional charges if caught)
|
|-- CheckAuditTriggers() -- called daily
|-- ProcessActiveAudit() -- advances audit stages
|-- PayFine(amount) -- resolve audit with financial penalty
|-- ContestAudit() -- hire lawyer, chance to reduce or dismiss
```

#### 11E: LaunderingUI

```
LaunderingUI (OnGUI, migrates to UI Toolkit in Phase 5)
|-- Front Business Overview:
|   - List of owned front businesses with daily stats
|   - Revenue breakdown (legitimate vs laundered)
|   - Suspicion meter per business
|   - Upgrade options
|
|-- Laundering Control Panel:
|   - Select method and front business
|   - Amount slider (with risk preview)
|   - Pending funds display (in cooling)
|   - Available clean cash display
|
|-- Risk Dashboard:
|   - Aggregate detection risk gauge
|   - IRS audit probability indicator
|   - Velocity graph (30-day rolling)
|   - Method diversification chart
|
|-- Audit Status (when active):
|   - Current audit stage
|   - Frozen asset amount
|   - Countermeasure options
```

#### Files to Create

```
Assets/Scripts/Empire/Laundering/LaunderingManager.cs
Assets/Scripts/Empire/Laundering/FrontBusiness.cs
Assets/Scripts/Empire/Laundering/LaunderingMethod.cs
Assets/Scripts/Empire/Laundering/IRSInvestigation.cs
Assets/Scripts/UI/Laundering/LaunderingUI.cs
```

#### Integration Points

- `CashManager` -- Replace `Launder()` with `LaunderingManager.StartLaundering()`. Dirty/clean cash balances feed into laundering pipeline.
- `TransactionLedger` -- All laundering transactions logged with method tag. Daily tick triggers `ProcessDailyLaundering()`.
- `PropertyManager` -- Front businesses require owned properties of matching type. Property upgrades affect laundering capacity.
- `WorkerManager` -- Smurfing method requires available workers. Accountant worker type reduces audit risk.
- `ReputationManager` -- CLOUT rank gates method availability. High-profile laundering affects Respect dimension.
- `WantedSystem` -- IRS charges escalate wanted level. Audit events feed into investigation graph (Step 15).
- `EventBus` -- New events: `LaunderingCompleteEvent`, `AuditTriggeredEvent`, `FrontBusinessBustedEvent`, `AssetsFrozenEvent`.
- `SaveManager` -- Serialize active fronts, pending funds, audit state, velocity history.
- `PhoneFinanceTab` -- Add laundering summary section.
- `TestArenaBuilder` -- Wire LaunderingManager, spawn 1 front business for testing.

---

### Step 12: Signature & Forensics System -- COMPLETE

Every production batch generates a unique forensic signature that law enforcement can use to trace product back to its source facility. This creates a persistent cat-and-mouse dynamic where players must actively manage their forensic exposure.

#### 12A: BatchSignature

```
BatchSignature
|-- signatureVector: float[512] -- high-dimensional fingerprint
|-- Generation Inputs:
|   - facilitySeed: int (derived from property ID + equipment hash)
|   - recipeHash: int (recipe definition fingerprint)
|   - workerSkillNoise: float (cook skill introduces variance)
|   - equipmentWear: float (degradation changes signature over time)
|   - ingredientSourceHash: int (supplier batch origin)
|
|-- Properties:
|   - Deterministic: same facility + recipe + conditions = similar signature
|   - Noisy: worker skill and equipment wear add controlled variance
|   - Persistent: signature travels with product through distribution chain
|   - Comparable: cosine similarity between signatures reveals shared origin
|
|-- SignatureDistance(other: BatchSignature) -> float (0-1, 1 = identical)
|-- Scrub(scrubLevel: float) -> BatchSignature (degrades traceability)
|-- Serialize() / Deserialize() for save system
```

#### 12B: ForensicLabAI

```
ForensicLabAI (Singleton)
|-- evidenceDatabase: Dictionary<int, BatchSignature> (seized samples)
|-- signatureClusters: List<SignatureCluster> (grouped by similarity)
|-- clusterThreshold: float (cosine similarity cutoff, default 0.85)
|
|-- ProcessSeizedEvidence(product, location, timestamp)
|   1. Extract BatchSignature from product
|   2. Compare against all known signatures (cosine similarity)
|   3. If similarity > threshold, add to existing cluster
|   4. If new cluster formed, create investigation lead
|
|-- RunClusterAnalysis() -- periodic full re-clustering
|   - Agglomerative clustering on signature vectors
|   - Identify facility origin candidates per cluster
|   - Generate confidence scores per facility match
|
|-- GenerateInvestigationLead(cluster) -> InvestigationLead
|   - Source facility estimate (district, property type)
|   - Confidence level (based on sample count and similarity)
|   - Distribution chain reconstruction
|
|-- GetExposureLevel(facilityId) -> float (0-1, how close LE is to ID)
```

#### 12C: Signature Scrubbing Mechanics

```
SignatureScrubber
|-- Scrubbing Methods:
|   EQUIPMENT_SWAP   -- Replace equipment at facility, resets facility seed
|                       Cost: High (new equipment purchase)
|                       Effectiveness: Full reset of facility component
|
|   CUTTING_AGENT    -- Mix product with inert substance, degrades signature
|                       Cost: Low (reduces quality and street value)
|                       Effectiveness: Partial (reduces similarity by 20-40%)
|
|   REPROCESSING     -- Run product through second facility
|                       Cost: Medium (time + second facility required)
|                       Effectiveness: High (new facility seed overlays original)
|
|   BATCH_MIXING     -- Combine batches from different facilities
|                       Cost: Low (logistics only)
|                       Effectiveness: Moderate (muddies cluster analysis)
|
|-- ApplyScrubbing(batch, method) -> BatchSignature (modified)
|-- GetScrubCost(method) -> float
|-- GetQualityLoss(method) -> float (some methods degrade product)
```

#### Files to Create

```
Assets/Scripts/Empire/Forensics/BatchSignature.cs
Assets/Scripts/Empire/Forensics/ForensicLabAI.cs
Assets/Scripts/Empire/Forensics/SignatureScrubber.cs
Assets/Scripts/Empire/Forensics/SignatureCluster.cs
Assets/Scripts/UI/Forensics/ForensicExposureUI.cs
```

#### Integration Points

- `CraftingStation` -- Generate BatchSignature on batch completion using facility + recipe + worker inputs.
- `DealManager` / `DealerAI` -- Propagate BatchSignature through sales. Seized product during arrests carries signature.
- `PropertyRaidSystem` -- Seized product during raids feeds into ForensicLabAI evidence database.
- `WantedSystem` -- Forensic exposure level contributes to heat accumulation.
- `InvestigationGraph` (Step 15) -- Forensic leads create edges between Batch nodes and Location/Person nodes.
- `PhoneProductsTab` -- Add forensic exposure indicator per product line.

---

### Step 13: Advanced Economy / Market Simulator -- COMPLETE

**Delivered April 2, 2026.** Full market simulation wrapping EconomyManager with competition, events, commodities, and manipulation.

#### 13A: MarketSimulator

```
MarketSimulator (Singleton, replaces EconomyManager price logic)
|-- districtMarkets: Dictionary<districtId, DistrictMarket>
|
|-- DistrictMarket:
|   |-- productCurves: Dictionary<ProductType, SupplyDemandCurve>
|   |-- localEvents: List<MarketEvent>
|   |-- competitorPresence: Dictionary<ProductType, float> (rival supply)
|   |-- consumerConfidence: float (0-1, overall spending willingness)
|
|-- SupplyDemandCurve:
|   |-- basePrice: float
|   |-- currentSupply: float (player + rival + import volume)
|   |-- currentDemand: float (consumer population x addiction x preference)
|   |-- elasticity: float (-0.1 to -2.0, how price-sensitive buyers are)
|   |-- qualityPremium: float (higher quality shifts demand curve up)
|   |
|   |-- Price Formula:
|   |   P = P_base x (D/S)^(1/|E|) x (1 + R_heat) x (1 + M_season) x Q_mod
|   |   Where:
|   |     D/S       = demand-to-supply ratio
|   |     E         = price elasticity
|   |     R_heat    = risk premium from district heat level
|   |     M_season  = seasonal modifier (-0.1 to +0.3)
|   |     Q_mod     = quality modifier (0.8 to 1.5)
|
|-- GetPrice(product, district, quality) -> float
|-- SimulateDay() -- advance all district markets
|-- InjectSupply(product, district, amount) -- player/rival adds supply
|-- InjectDemand(district, event) -- events modify demand
```

#### 13B: Market Events

```
MarketEvent (ScriptableObject)
|-- eventName: string
|-- affectedProducts: ProductType[]
|-- demandMultiplier: float
|-- supplyMultiplier: float
|-- duration: int (game days)
|-- probability: float (daily chance of firing)
|
|-- Event Types:
|   DROUGHT          -- Precursor shortage, supply drops 40%, prices spike
|   FESTIVAL         -- Weekend event, demand surges 50% for recreational products
|   BUST             -- Major rival busted, their supply removed, prices spike
|   FLOOD            -- New competitor enters market, supply surges, prices crash
|   CRACKDOWN        -- Police focus on specific product, heat premium doubles
|   MEDIA_PANIC      -- Public awareness campaign, demand drops 20%
|   CELEBRITY_DEATH  -- Specific product demand spikes then crashes
|   SUPPLY_ROUTE_CUT -- Import disruption, precursor prices surge
```

#### 13C: Market Manipulation

```
MarketManipulation
|-- Tactics available to player:
|   DUMP_PRODUCT     -- Flood market to crash rival prices (sacrifice margin)
|   CREATE_SCARCITY  -- Withhold supply to spike prices (risk losing customers)
|   CORNER_MARKET    -- Buy all precursors to starve competitors
|   PRICE_WAR        -- Undercut rivals to drive them out of district
|   QUALITY_FLOOD    -- Saturate with high-quality to shift demand permanently
|
|-- Each tactic has:
|   - Cost (product, cash, or opportunity)
|   - Duration to take effect (1-7 days)
|   - Counter-play available to rival factions
|   - Side effects (heat, reputation changes)
```

#### Files Delivered

```
Assets/Scripts/Empire/Economy/MarketSimulator.cs     -- 550+ lines, core simulation singleton
Assets/Scripts/Empire/Economy/CommodityTracker.cs    -- 300+ lines, O-U Brownian motion precursor prices
Assets/Scripts/Empire/Economy/MarketEvent.cs         -- 165 lines, SO + 8 event types + runtime wrapper
Assets/Scripts/Empire/Economy/MarketManipulation.cs  -- 310+ lines, 5 player tactics
Assets/Scripts/UI/Economy/MarketAnalysisUI.cs        -- 400+ lines, 4-tab OnGUI dashboard (M key)
```

#### Integration Completed

- `EconomyManager.CalculatePrice()` delegates to `MarketSimulator.GetPrice()` when active
- `DealManager.ExecuteDeal()` feeds sales into `MarketSimulator.RecordSale()` with district resolution
- `GameBalanceConfig` +12 market tuning values (supply decay, import recovery, event modifier, commodity mean reversion, manipulation cooldown, competition floor, etc.)
- `EventBus` +5 event structs: MarketPriceChanged, MarketEventTriggered/Ended, CommodityPriceShock, MarketManipulation

---

### Step 14: Rival Faction AI

Populate the world with 3-5 AI-controlled criminal organizations that run the same empire simulation loop as the player: production, distribution, territory control, laundering, and expansion.

#### 14A: FactionManager

```
FactionManager (Singleton)
|-- factions: List<Faction> (3-5 per city)
|-- allianceMatrix: float[,] (faction-to-faction relationship, -1 to +1)
|-- warState: Dictionary<(factionA, factionB), WarState>
|
|-- Faction:
|   |-- factionId: string
|   |-- factionName: string
|   |-- leader: FactionLeader (personality profile)
|   |-- territory: List<districtId> (controlled districts)
|   |-- resources: FactionResources (cash, product, workers, properties)
|   |-- strategy: FactionStrategy (current high-level plan)
|   |-- strength: float (composite military + economic power)
|   |-- heatLevel: float (law enforcement attention)
|
|-- SimulateFactionTick() -- called daily for each faction
|   1. Production phase: generate product based on facilities
|   2. Distribution phase: sell in controlled districts
|   3. Expansion phase: evaluate territory acquisition
|   4. Laundering phase: process dirty cash
|   5. Personnel phase: hire/fire/promote NPCs
|   6. Diplomacy phase: evaluate relationships with other factions and player
|   7. Threat assessment: react to player actions in shared territories
```

#### 14B: Faction Decision Engine

```
FactionDecisionEngine
|-- Decision Weights (per faction leader personality):
|   AGGRESSIVE  -- Prioritize territory expansion, initiate wars, high risk tolerance
|   CAUTIOUS    -- Prioritize defense, avoid heat, slow expansion, save resources
|   DIPLOMATIC  -- Prioritize alliances, trade deals, avoid direct conflict
|   OPPORTUNIST -- React to circumstances, exploit weaknesses, flexible strategy
|
|-- Decision Tree (per tick):
|   IF strength > neighbor AND relationship < -0.3 THEN evaluate war
|   IF heat > 60 THEN reduce operations, lay low
|   IF player encroaching on territory THEN respond per personality
|   IF alliance opportunity AND shared enemy THEN propose alliance
|   IF weakened faction nearby THEN evaluate absorption
|
|-- EvaluateWarDeclaration(target) -> bool
|-- EvaluateAllianceProposal(faction) -> bool
|-- EvaluateTerritoryExpansion(district) -> float (priority score)
|-- ReactToPlayerAction(action) -> FactionResponse
```

#### 14C: Diplomacy System

```
DiplomacyManager
|-- Relationship Modifiers:
|   - Territorial overlap: negative
|   - Trade agreements: positive
|   - Attacks on faction members: strongly negative
|   - Shared enemies: positive
|   - Betrayal history: permanent negative
|
|-- Actions:
|   NEGOTIATE    -- Propose terms (territory split, trade deal, ceasefire)
|   THREATEN     -- Intimidation attempt (backed by strength comparison)
|   BRIBE        -- Cash payment to improve relations
|   ALLY         -- Formal alliance (shared defense, trade benefits)
|   DECLARE_WAR  -- Open hostilities (territory raids, worker attacks)
|   ABSORB       -- Take over weakened faction (requires 3x strength)
|   BETRAY       -- Break alliance for tactical advantage (permanent reputation hit)
|
|-- Alliance Benefits:
|   - Shared market access (sell in ally territory at reduced penalty)
|   - Mutual defense (ally workers defend shared borders)
|   - Intelligence sharing (reveal rival faction positions)
|   - Combined operations against common enemies
```

#### 14D: DiplomacyUI

```
DiplomacyUI (OnGUI)
|-- Faction Overview Map:
|   - Color-coded territory control per faction
|   - Faction strength indicators
|   - Relationship status icons
|
|-- Faction Detail Panel:
|   - Leader profile and personality hint
|   - Relationship history
|   - Current agreements
|   - Strength comparison
|
|-- Action Panel:
|   - Available diplomatic actions
|   - Cost/risk preview
|   - Proposal builder (territory, cash, trade terms)
|
|-- War Status (when active):
|   - Territory control changes
|   - Casualty reports
|   - Ceasefire negotiation
```

#### Files to Create

```
Assets/Scripts/Empire/Factions/FactionManager.cs
Assets/Scripts/Empire/Factions/Faction.cs
Assets/Scripts/Empire/Factions/FactionDecisionEngine.cs
Assets/Scripts/Empire/Factions/FactionLeader.cs
Assets/Scripts/Empire/Factions/DiplomacyManager.cs
Assets/Scripts/UI/Factions/DiplomacyUI.cs
```

#### Integration Points

- `DistrictManager` -- Factions compete for district control. Territory changes affect market conditions.
- `MarketSimulator` -- Faction production feeds into district supply curves. Rival pricing creates competition.
- `WorkerManager` -- Faction workers can be poached, attacked, or flipped as informants.
- `WantedSystem` -- Faction actions generate heat in shared districts, affecting player operations.
- `LaunderingManager` -- Factions launder through their own fronts, competing for business types in districts.
- `ReputationManager` -- Diplomatic actions affect Fear, Respect, and Ruthlessness dimensions.
- `PhoneContactsTab` -- Add faction leader contacts with relationship status.
- `PhoneMapTab` -- Territory control overlay shows faction boundaries.

---

### Step 15: Advanced Police & Investigation

Replace the reactive heat-based police system with a persistent investigation graph where law enforcement builds cases over time by connecting people, locations, assets, transactions, and forensic evidence.

#### 15A: InvestigationGraph

```
InvestigationGraph
|-- Node Types:
|   PERSON      -- Player, workers, faction leaders, contacts
|   LOCATION    -- Properties, districts, known meeting points
|   ASSET       -- Vehicles, properties, bank accounts, front businesses
|   TRANSACTION -- Financial records, deals, purchases
|   BATCH       -- Product batches (linked via BatchSignature)
|
|-- Edge Types:
|   TEMPORAL        -- "A was at location B at time T"
|   FINANCIAL       -- "A transferred $X to B"
|   COMMUNICATION   -- "A contacted B" (phone, in-person)
|   PHYSICAL        -- "Evidence A found at location B"
|   EMPLOYMENT      -- "A works for B"
|   OWNERSHIP       -- "A owns asset B"
|   FORENSIC        -- "Batch A matches facility B signature"
|
|-- Edge Properties:
|   - weight: float (evidence strength, 0-1)
|   - timestamp: int (game day recorded)
|   - degradable: bool (some evidence degrades over time)
|   - source: EvidenceSource (witness, surveillance, forensic, financial)
|
|-- AddNode(type, entityId, metadata)
|-- AddEdge(nodeA, nodeB, edgeType, weight)
|-- RunCentralityAnalysis() -- every 30 game-days
|   - PageRank: identify most connected/important nodes
|   - Betweenness Centrality: identify bridge nodes (lieutenants, fronts)
|   - Community Detection: identify organization clusters
|-- GetExposure(entityId) -> float (how central the node is in graph)
|-- DegradeEvidence() -- daily tick, old/weak edges lose weight
```

#### 15B: DetectiveAI

```
DetectiveAI
|-- activeCases: List<Case>
|-- maxConcurrentCases: int (scales with police funding / heat)
|
|-- Case:
|   |-- caseId: string
|   |-- target: int (primary suspect node ID)
|   |-- evidenceNodes: List<int> (graph nodes in this case)
|   |-- caseStrength: float (0-1, prosecution viability)
|   |-- stage: CaseStage (OPEN, ACTIVE, WARRANT_PENDING, PROSECUTION)
|   |-- assignedOfficers: int
|
|-- Case Progression:
|   1. OPEN         -- Suspicious activity flagged, case file created
|   2. ACTIVE       -- Detective gathers evidence, adds graph edges
|   3. SURVEILLANCE -- Undercover officers deployed to key locations
|   4. WARRANT      -- Sufficient evidence to request search/arrest warrants
|   5. EXECUTION    -- Raids, arrests, asset seizure
|   6. PROSECUTION  -- Case strength determines outcome severity
|
|-- Evidence Gathering Methods:
|   - Patrol observations (officers witness crimes)
|   - Witness interviews (civilian reports)
|   - Financial audits (IRS referrals from Step 11)
|   - Forensic analysis (lab results from Step 12)
|   - Surveillance (undercover placement)
|   - Informants (arrested workers who flip)
|   - Wiretaps (communication edge discovery)
|
|-- AssignCase(suspectNode) -> Case
|-- GatherEvidence(case) -- daily progression
|-- EvaluateCaseStrength(case) -> float
|-- RequestWarrant(case) -> bool (requires strength > 0.6)
|-- ExecuteWarrant(case) -- triggers raids/arrests
```

#### 15C: Undercover Operations

```
UndercoverSystem
|-- activeOperations: List<UndercoverOp>
|
|-- UndercoverOp:
|   |-- officer: UndercoverOfficer (disguised as civilian/worker)
|   |-- targetFaction: faction or player
|   |-- cover: CoverIdentity (fake name, role, backstory)
|   |-- trustLevel: float (0-1, how much target trusts them)
|   |-- intelligenceGathered: List<GraphEdge> (edges discovered)
|   |-- blownChance: float (daily probability of cover blown)
|
|-- Mechanics:
|   - Officers appear as normal NPCs (customers, potential recruits)
|   - If hired by player, they report back to DetectiveAI
|   - Trust level determines quality of intelligence gathered
|   - Player can detect undercovers via loyalty checks (Compartmentalization stat)
|   - Blown cover: officer flees, case evidence preserved
```

#### 15D: RICO Charges

```
RICOEvaluation
|-- Trigger: InvestigationGraph connects 5+ nodes across categories
|-- Requirements:
|   - Pattern of criminal activity (3+ transaction edges)
|   - Organization structure visible (employment edges)
|   - Financial trail (laundering edges from Step 11)
|   - Multiple participants identified (person nodes)
|
|-- Consequences:
|   - All connected assets subject to forfeiture
|   - All identified workers arrested simultaneously
|   - Properties seized (not just raided)
|   - Player wanted level jumps to maximum
|   - Recovery requires rebuilding from near-zero
|
|-- Countermeasures:
|   - Compartmentalization: limit edges between organization layers
|   - Cutouts: use intermediaries to break direct connections
|   - Signature scrubbing: weaken forensic edges
|   - Evidence destruction: risky, additional charges if caught
|   - Witness intimidation: reduce witness edge weights (Fear dimension)
```

#### Files to Create

```
Assets/Scripts/World/Investigation/InvestigationGraph.cs
Assets/Scripts/World/Investigation/DetectiveAI.cs
Assets/Scripts/World/Investigation/UndercoverSystem.cs
Assets/Scripts/World/Investigation/RICOEvaluation.cs
Assets/Scripts/World/Investigation/CaseFile.cs
Assets/Scripts/UI/Investigation/InvestigationWarningUI.cs
```

#### Integration Points

- `WantedSystem` -- Heat feeds case creation triggers. RICO charges cause maximum heat spike.
- `PolicePatrolAI` -- Patrol observations create temporal edges in graph.
- `WitnessSystem` -- Witness reports create person-location edges.
- `PropertyRaidSystem` -- Warrants issued through DetectiveAI. Raid evidence adds physical edges.
- `ForensicLabAI` (Step 12) -- Forensic results create batch-facility edges.
- `IRSInvestigation` (Step 11) -- Financial audits create transaction edges. IRS referrals open cases.
- `WorkerManager` -- Arrested workers can flip (informant mechanic), revealing employment edges.
- `FactionManager` (Step 14) -- Rival factions also investigated; player can anonymously tip LE about rivals.
- `ReputationManager` -- Compartmentalization (derived from Reliability) reduces graph edge exposure.
- `PhoneMessagesTab` -- Investigation warnings delivered as messages.
- `SaveManager` -- Serialize full investigation graph state.

---

## Phase 4: World Expansion & Multiplayer (Weeks 13-20)

> Focus: Scale the world, add global systems, lay multiplayer foundation.
> Estimated new scripts: ~30
> Running total after phase: ~194

---

### Step 16: Multi-District City

- Expand from 1 to 4+ procedurally generated districts with distinct profiles (wealth, demand, police presence, architecture).
- District-to-district travel with border mechanics (checkpoints at high heat).
- Cross-district property market with location-based pricing.
- District events: block parties (demand spike), construction zones (access restricted), gentrification (wealth shift), gang territory disputes.
- District reputation independent per zone (known in one, anonymous in another).

Files: `World/City/CityManager.cs`, `World/City/DistrictTransition.cs`, `World/City/CrossDistrictTravel.cs`, `World/City/DistrictEventSystem.cs`.

### Step 17: Global Supply Chain

- Source region selection with risk/cost/quality trade-offs (12+ regions).
- Smuggling pipeline: procurement, transit, customs, delivery.
- Transit route network with interdiction risk per leg.
- Import/export mechanics: bulk precursor purchasing, finished product export to consumption cities.
- International contacts unlocked by CLOUT rank.
- Supply disruption events tied to MarketSimulator (Step 13).

Files: `Empire/Supply/SupplyChainManager.cs`, `Empire/Supply/SourceRegion.cs`, `Empire/Supply/TransitRoute.cs`, `Empire/Supply/SmugglingSim.cs`, `UI/Supply/SupplyChainUI.cs`.

### Step 18: Multiplayer Foundation

- Network architecture decision: FishNet reactivation or Netcode for GameObjects migration.
- Server-authoritative economy (all cash, inventory, and market state validated server-side).
- Player organization system: syndicates with hierarchy, roles, permissions, shared properties.
- Territory wars: real-time PvPvE zone control with capture mechanics.
- Anti-cheat foundation: server-side validation, stat verification, anomaly detection.
- Lobby, matchmaking, and shard management.

Files: `Network/NetworkManager.cs`, `Network/ServerEconomy.cs`, `Network/SyndicateSystem.cs`, `Network/TerritoryWarManager.cs`, `Network/AntiCheat.cs`, `Network/LobbyManager.cs`.

### Step 19: Network Graph System

- Player-to-player trust, debt, and information-flow graph (multiplayer social layer).
- Compartmentalization mechanics: limit what each player in a syndicate knows.
- Informant system: flip enemy organization members for intelligence.
- Communication interception: wiretap mechanic for eavesdropping on rival comms.
- Graph visualization UI: zoomable, filterable relationship map.

Files: `Network/SocialGraph/PlayerNetworkGraph.cs`, `Network/SocialGraph/CompartmentalizationManager.cs`, `Network/SocialGraph/InformantSystem.cs`, `Network/SocialGraph/WiretapSystem.cs`, `UI/Network/NetworkGraphUI.cs`.

---

## Phase 5: Content & Polish (Weeks 21-30)

> Focus: Production quality, content variety, accessibility, UI overhaul.
> Estimated new scripts: ~20
> Running total after phase: ~214

---

### Step 20: Procedural Music System

- Dynamic music layers that intensify with heat level and combat state.
- Per-district ambient soundscapes reflecting wealth, time of day, and activity.
- Combat music with smooth transitions (explore -> tension -> combat -> cooldown).
- Adaptive mixing: empire size and success affect musical tone.
- Procedural beat generation for dealing sequences.

Files: `Audio/ProceduralMusicManager.cs`, `Audio/MusicLayerController.cs`, `Audio/DistrictAmbience.cs`.

### Step 21: Advanced Procedural Generation

- Full San Francisco city template with landmark zones (Chinatown, Financial District, Mission, Tenderloin).
- Interior generation for all 8+ property types with functional layouts.
- Furniture and prop placement algorithms (rule-based + randomized).
- NPC daily schedule simulation (home, work, leisure, sleep) for ambient population.
- Seasonal and time-of-day visual variation.

Files: `World/Procedural/CityTemplateGenerator.cs`, `World/Procedural/InteriorGenerator.cs`, `World/Procedural/NPCScheduleSimulator.cs`, `World/Procedural/SeasonalVariation.cs`.

### Step 22: UI/UX Polish -- OnGUI to UI Toolkit Migration

- Systematic migration of all OnGUI interfaces to Unity UI Toolkit.
- War Room: strategic overview screen with zoomable city map, faction positions, resource flows.
- Network graph viewer: interactive visualization of investigation and social graphs.
- Heat radar: real-time overlay showing police attention zones.
- Legend timeline: scrollable personal history of key events and milestones.
- Consistent visual language, animation, and responsive layout across all screens.

Files: `UI/Toolkit/UIToolkitManager.cs`, `UI/Toolkit/WarRoomUI.cs`, `UI/Toolkit/LegendTimeline.cs`, plus UXML/USS assets.

### Step 23: Accessibility Pass

- Color-blind modes (protanopia, deuteranopia, tritanopia) with full UI recoloring.
- Screen-reader compatible dashboards (ARIA-equivalent labels on all UI elements).
- One-button economy macros (automate repetitive empire management tasks).
- Full control remapping (already using Input System, extend to all new inputs).
- Scalable UI text and HUD elements.
- Subtitle system for all narrative and notification audio.

Files: `Accessibility/AccessibilityManager.cs`, `Accessibility/ColorBlindFilter.cs`, `Accessibility/ScreenReaderBridge.cs`.

### Step 24: Content Pipeline

- 24 source region definitions with unique risk/quality profiles.
- 50+ NPC personality templates (varied appearance, stats, dialogue).
- 20+ property interior layout variants.
- 30+ recipe combinations with distinct quality curves.
- Vehicle system: ownership, visual mods, transport capacity, chase mechanics.
- Weapon variety expansion: 10+ melee, 10+ ranged with distinct movesets.

Files: `Content/ContentDatabase.cs`, `Content/VehicleSystem.cs`, `Content/WeaponDatabase.cs`, plus ScriptableObject assets.

---

## Phase 6: Ship (Weeks 31-40)

> Focus: Scale validation, infrastructure, launch execution.
> Estimated new scripts: ~15
> Running total after phase: ~229

---

### Step 25: Scale Testing

- 10,000 concurrent player stress testing across shards.
- Economy stability validation under adversarial player manipulation.
- Investigation AI performance profiling at scale (graph size limits, query optimization).
- Server cost modeling and optimization (spot instances, auto-scaling policies).
- Load testing for all network-authoritative systems.
- Client performance targets: 60 FPS on target hardware, memory budget enforcement.

### Step 26: Live Ops Infrastructure

- Seasonal events engine: time-limited content, global modifiers, leaderboard competitions.
- Modding API: Lua scripting layer for custom game rules, recipes, and events.
- Community tools: faction leaderboards, economy dashboards, player statistics.
- Telemetry and analytics pipeline: player behavior tracking, economy health metrics, engagement funnels.
- Hotfix deployment system: server-side tuning without client patch.
- Content delivery pipeline for post-launch updates.

### Step 27: Early Access Launch -- Target Q3 2027

Scope:
- 1 city, 4 districts with full procedural generation.
- Complete core loop: produce, deal, earn, buy, hire, manage, launder, evade.
- 3 rival AI factions with diplomacy.
- Full investigation system (detective AI, forensics, RICO).
- Cooperative multiplayer (2-4 players per syndicate).
- Money laundering pipeline with IRS audits.
- Market simulation with events.
- Phone UI empire management.

### Step 28: Full Launch -- Target Q4 2027

Scope:
- 4 cities with 8 consumption megacities in global supply chain.
- 10,000+ player shards with server-authoritative economy.
- Full investigation graph with undercover operations.
- War Room strategic UI.
- Modding API and community tools.
- Vehicle system and expanded weapon variety.
- Accessibility features complete.
- Live ops infrastructure operational.

---

## Post-Launch Roadmap

| Timeframe | Focus Area | Content |
|-----------|------------|---------|
| Year 1 Q1 | Regions    | 2 new global source regions, advanced transit route mechanics |
| Year 1 Q2 | Systems    | Advanced laundering methods, cryptocurrency mixing, offshore accounts |
| Year 1 Q3 | Content    | New city (Los Angeles), 2 new districts, 15 new recipes |
| Year 1 Q4 | Social     | Syndicate wars season system, ranked competitive mode |
| Year 2 H1 | Platform   | Mobile companion app for remote facility management and market monitoring |
| Year 2 H2 | Content    | New city (Miami), cartel storyline, boat smuggling routes |
| Year 3     | Innovation | VR War Room mode, cross-platform syndicate management |

---

## Monetization (Fair & Player-First)

- **Cosmetic Only:** Syndicate logos, vehicle skins, safehouse interior themes, character outfits. No gameplay advantage.
- **Expansion Packs:** New global regions, cities, and storylines. Substantial content, fairly priced. No pay-to-win mechanics.
- **Season Pass:** Cosmetic reward tracks tied to seasonal events. Free tier always available.
- **No Loot Boxes.** No randomized monetization. Players see exactly what they are purchasing.
- **No Pay-to-Skip.** Empire progression cannot be accelerated with real money.

---

## Script Count Projections

| Phase | Description | New Scripts | Running Total | Status |
|-------|-------------|-------------|---------------|--------|
| Phase 0 | Foundation | -- | -- | COMPLETE |
| Phase 1 | Core Loop | 62 | 62 | COMPLETE |
| Phase 2 Steps 1-5 | Empire Foundation | 39 | 101 | COMPLETE |
| Phase 2 Steps 6-9 | Empire Expansion | 34 | 135 | COMPLETE |
| Phase 2 Step 10 | Integration & Polish | 4 | 139 | COMPLETE |
| Phase 3 Step 11 | Money Laundering | 5 | 144 | COMPLETE |
| Phase 3 Step 12 | Forensics | 5 | 149 | COMPLETE |
| Phase 3 Step 13 | Market Simulator | 5 | 154 | NEXT |
| Phase 3 Step 14 | Rival Factions | 6 | 160 | Planned |
| Phase 3 Step 15 | Investigation | 6 | 166 | Planned |
| Phase 4 Steps 16-19 | World & Multiplayer | ~28 | ~194 | Planned |
| Phase 5 Steps 20-24 | Content & Polish | ~20 | ~214 | Planned |
| Phase 6 Steps 25-28 | Ship | ~15 | ~229 | Planned |

---

## Immediate Next Action

**Phase 3 Step 11: Money Laundering Pipeline**

Begin implementation. Files to create in order:

```
1. Assets/Scripts/Empire/Laundering/LaunderingMethod.cs
   -- ScriptableObject defining method types (structuring, smurfing, real estate, cash-intensive, crypto)
   -- Risk multipliers, capacity multipliers, cooling periods, CLOUT rank requirements

2. Assets/Scripts/Empire/Laundering/FrontBusiness.cs
   -- MonoBehaviour attached to owned properties (Restaurant, AutoShop, Nightclub)
   -- Business simulation: customer flow, legitimate revenue, operating costs
   -- Laundering capacity derived from legitimate revenue
   -- Suspicion tracking based on dirty-to-clean ratio

3. Assets/Scripts/Empire/Laundering/IRSInvestigation.cs
   -- Audit trigger evaluation (thresholds, velocity, patterns, tips)
   -- 5-stage audit process (notice, review, freeze, resolution, charges)
   -- Countermeasures (accountant worker, diversification, record destruction)

4. Assets/Scripts/Empire/Laundering/LaunderingManager.cs
   -- Singleton orchestrating 5-step pipeline (placement, layering, integration, cooling, extraction)
   -- Velocity tracking (30-day rolling window)
   -- Daily processing tied to TransactionLedger.OnDayEnd
   -- Integration with CashManager, PropertyManager, WorkerManager, WantedSystem, EventBus

5. Assets/Scripts/UI/Laundering/LaunderingUI.cs
   -- OnGUI interface: front business overview, laundering controls, risk dashboard, audit status
   -- Wired to LaunderingManager for real-time data

6. Update: Assets/Scripts/Testing/TestArenaBuilder.cs
   -- Wire LaunderingManager singleton
   -- Spawn one front business for testing
   -- Add test laundering scenario to arena setup
```

Steps 11-13 are complete. Proceed to Step 14: Rival Faction AI.

---

*CLOUT Development Roadmap v3.1 -- SlicedLabs -- April 2, 2026*
