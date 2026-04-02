# CLOUT -- Phase 3: Advanced Empire Systems

> From vertical slice to living empire. This is where CLOUT becomes dangerous.

---

## Overview

Phase 3 builds the depth layer on top of the ~139 scripts delivered in Phases 1-2. The core loop works: cook, deal, earn, buy property, hire workers, evade police. Phase 3 adds the systems that make the empire feel real -- money laundering, forensic investigation, dynamic markets, rival factions, and advanced law enforcement.

**Steps:** 11 through 15
**Estimated Duration:** 8-12 weeks
**Script Count Target:** ~165-180 total (25-40 new scripts)
**Prerequisite:** Phase 2 fully integrated, TestArenaBuilder functional with all Phase 2 systems

---

## Phase 3 Architecture Principles

1. **Simulation Depth Over Breadth** -- Each system models real-world mechanics at sufficient fidelity to create emergent gameplay.
2. **Data-Driven Configuration** -- All tuning values live in GameBalanceConfig or system-specific ScriptableObjects. Zero magic numbers.
3. **EventBus Integration** -- Every system publishes state changes to EventBus. No direct coupling between Phase 3 systems.
4. **Singleton Management** -- New managers register through the existing GameManager bootstrap. Lazy initialization with null guards.
5. **OnGUI Consistency** -- All Phase 3 UI uses OnGUI with the established style system. UI Toolkit migration deferred to Phase 5.

---

## Step 11: Money Laundering Pipeline

### Design Intent

Transform the placeholder `CashManager.Launder()` into a full-fidelity financial simulation. The player must acquire front businesses, choose laundering methods, manage throughput capacity, and avoid IRS attention. This is the primary cash conversion mechanic and the bridge between illegal revenue and legitimate spending power.

### 11A: LaunderingManager Singleton

Core orchestrator for all laundering operations.

```
Scripts/Economy/Laundering/
    LaunderingManager.cs
```

**Class: LaunderingManager : MonoBehaviour**

| Field | Type | Description |
|-------|------|-------------|
| `_activePipelines` | `List<LaunderingPipeline>` | Currently processing batches |
| `_frontBusinesses` | `List<FrontBusiness>` | Registered front businesses |
| `_dailyCapacityUsed` | `float` | Aggregate laundering volume today |
| `_dailyCapacityMax` | `float` | Sum of all front business capacities |
| `_irsAttention` | `float` | 0.0 - 1.0, cumulative risk meter |
| `_irsDecayRate` | `float` | Daily attention decay (from GameBalanceConfig) |
| `_transactionHistory` | `List<LaunderingTransaction>` | Audit trail for forensics integration |

**Pipeline Stages:**

| Stage | Duration | Description |
|-------|----------|-------------|
| Placement | 1-2 game days | Dirty cash enters the financial system via front business |
| Layering | 2-5 game days | Money moves through multiple transactions to obscure origin |
| Integration | 1-3 game days | Clean money re-enters player economy as legitimate revenue |
| Structuring | Passive | Automatic splitting of large amounts below reporting thresholds |
| Verification | Instant | Final check against IRS detection heuristics before release |

**Key Methods:**

```csharp
public bool StartLaundering(float dirtyAmount, FrontBusiness business, LaunderingMethod method)
public float GetDailyCapacityRemaining()
public float GetIRSAttention()
public void RegisterFrontBusiness(FrontBusiness business)
public void UnregisterFrontBusiness(FrontBusiness business)
private void ProcessPipelines()     // Called each game day
private void DecayIRSAttention()    // Called each game day
private void CheckIRSTriggers()     // Called after each transaction
```

**EventBus Publications:**

- `LaunderingStarted { amount, business, method }`
- `LaunderingCompleted { cleanAmount, fees, business }`
- `LaunderingFailed { reason, seizedAmount }`
- `IRSAttentionChanged { oldValue, newValue }`
- `IRSAuditTriggered { severity, targetBusiness }`

### 11B: FrontBusiness Component

Attached to property GameObjects that serve as laundering fronts.

```
Scripts/Economy/Laundering/
    FrontBusiness.cs
```

**Class: FrontBusiness : MonoBehaviour**

| Field | Type | Description |
|-------|------|-------------|
| `_businessType` | `FrontBusinessType` | Restaurant, AutoShop, Nightclub, Laundromat, CarWash |
| `_legitimateRevenue` | `float` | Simulated daily legitimate income |
| `_legitimateExpenses` | `float` | Simulated daily operating costs |
| `_customerFlow` | `int` | Simulated daily customer count |
| `_launderingCapacity` | `float` | `legitimateRevenue * capacityMultiplier` |
| `_currentLaunderingVolume` | `float` | Today's laundering throughput |
| `_suspicionLevel` | `float` | 0.0 - 1.0, per-business suspicion |
| `_capacityMultiplier` | `float` | From GameBalanceConfig, per business type |

**Suspicion Calculation:**

```
suspicionDelta = (launderingVolume / legitimateRevenue) - safeRatio
if suspicionDelta > 0:
    suspicionLevel += suspicionDelta * suspicionGrowthRate
else:
    suspicionLevel -= suspicionDecayRate
suspicionLevel = clamp(0.0, 1.0)
```

**Business Type Profiles:**

| Type | Base Revenue | Capacity Mult | Suspicion Decay | Notes |
|------|-------------|---------------|-----------------|-------|
| Restaurant | $2,000/day | 1.5x | 0.02/day | High cash volume, moderate capacity |
| AutoShop | $3,500/day | 1.2x | 0.015/day | Plausible large transactions |
| Nightclub | $8,000/day | 2.0x | 0.01/day | Highest capacity, slowest decay |
| Laundromat | $800/day | 3.0x | 0.03/day | Low revenue but excellent ratio |
| CarWash | $1,200/day | 2.5x | 0.025/day | Classic front, good balance |

### 11C: LaunderingMethod ScriptableObject

```
Scripts/Economy/Laundering/
    LaunderingMethod.cs

ScriptableObjects/Laundering/
    SO_Method_Structuring.asset
    SO_Method_Smurfing.asset
    SO_Method_RealEstate.asset
    SO_Method_CashIntensive.asset
```

**Class: LaunderingMethod : ScriptableObject**

| Field | Type | Description |
|-------|------|-------------|
| `methodName` | `string` | Display name |
| `methodType` | `LaunderingMethodType` | Enum: Structuring, Smurfing, RealEstate, CashIntensive |
| `riskProfile` | `float` | 0.0 - 1.0, base IRS attention per transaction |
| `processingSpeed` | `float` | Days to complete full pipeline |
| `dailyCapacity` | `float` | Maximum throughput per day per business |
| `feePercentage` | `float` | Cost as percentage of laundered amount |
| `description` | `string` | Player-facing description |
| `requiredBusinessType` | `FrontBusinessType` | Required front type, or Any |

**Method Profiles:**

| Method | Risk | Speed | Capacity | Fee | Description |
|--------|------|-------|----------|-----|-------------|
| Structuring | 0.15 | 3 days | $8,000/day | 5% | Sub-threshold deposits across multiple accounts |
| Smurfing | 0.25 | 1 day | $15,000/day | 12% | Multiple runners make small deposits simultaneously |
| RealEstate | 0.08 | 14 days | $50,000/batch | 8% | Property purchase/flip cycle, slow but low risk |
| CashIntensive | 0.10 | 2 days | Revenue-based | 3% | Mixed with legitimate high-volume cash business |

### 11D: IRS Investigation System

```
Scripts/Economy/Laundering/
    IRSInvestigation.cs
```

**Class: IRSInvestigation**

**Audit Trigger Conditions:**

| Trigger | Threshold | IRS Attention Increase |
|---------|-----------|----------------------|
| Single deposit > $10,000 | Absolute | +0.15 |
| Velocity anomaly | >3x normal daily volume | +0.10 per occurrence |
| Pattern detection | Same amount repeated 3+ times | +0.08 |
| Round number deposits | Amounts ending in 000 | +0.03 |
| Cross-business correlation | Multiple businesses spike same day | +0.12 |

**Investigation Process:**

```
Stage 1: FLAG (irsAttention > 0.4)
    - Warning notification to player
    - 7-day window to reduce activity

Stage 2: INVESTIGATION (irsAttention > 0.6)
    - IRS agent assigned (invisible timer)
    - Transaction monitoring begins
    - Player warned via attorney contact

Stage 3: AUDIT (irsAttention > 0.8)
    - Formal audit of one or more front businesses
    - Books examined: if laundering volume > legitimate revenue, caught
    - 14-day audit duration

Stage 4: PENALTY / SEIZURE (audit failed)
    - Financial penalty: 50% of detected laundered amount
    - Business seizure: flagged business confiscated
    - Criminal charges: WantedLevel increase
    - Possible cascade: connected businesses investigated
```

**Avoidance Mechanics:**

- Keep laundering volume below business capacity
- Rotate methods across businesses
- Time high-volume operations during high-traffic events
- Upgrade business legitimacy (renovations increase legitimate revenue)
- Hire accountant workers (reduce IRS attention growth rate)

### 11E: Laundering UI

```
Scripts/UI/
    LaunderingUI.cs
```

**OnGUI Layout:**

```
+------------------------------------------+
| MONEY LAUNDERING DASHBOARD               |
+------------------------------------------+
| Dirty Cash:    $XXX,XXX                  |
| Clean Cash:    $XXX,XXX                  |
| Daily Capacity: $XX,XXX / $XX,XXX used   |
| IRS Attention: [========--] 78%          |
+------------------------------------------+
| FRONT BUSINESSES                         |
| [Restaurant]  Rev: $2K  Laund: $1.5K  ! |
| [Nightclub]   Rev: $8K  Laund: $4K      |
| [Car Wash]    Rev: $1.2K Laund: $0      |
+------------------------------------------+
| ACTIVE OPERATIONS                        |
| Structuring via Restaurant: $5K [=====-] |
| CashIntensive via Nightclub: $12k [==---]|
+------------------------------------------+
| [Start New Operation] [Method Config]    |
+------------------------------------------+
```

### 11F: Integration Points

| System | Integration |
|--------|-------------|
| CashManager | Dirty/clean cash separation, `Launder()` delegates to LaunderingManager |
| PropertyManager | FrontBusiness component auto-attached to eligible property types |
| TransactionLedger | All laundering transactions recorded with full metadata |
| GameBalanceConfig | All tuning values: capacity multipliers, risk rates, IRS thresholds |
| EventBus | All state changes published for cross-system reactivity |
| WorkerManager | Accountant worker type reduces IRS attention growth |
| WantedSystem | IRS penalties escalate to criminal wanted level |

### 11G: Files to Create

| File | Type | Lines (est.) |
|------|------|-------------|
| `Scripts/Economy/Laundering/LaunderingManager.cs` | Singleton | 350-400 |
| `Scripts/Economy/Laundering/FrontBusiness.cs` | MonoBehaviour | 200-250 |
| `Scripts/Economy/Laundering/LaunderingMethod.cs` | ScriptableObject | 80-100 |
| `Scripts/Economy/Laundering/IRSInvestigation.cs` | Class | 250-300 |
| `Scripts/UI/LaunderingUI.cs` | OnGUI | 200-250 |
| `TestArenaBuilder.cs` | Update | +50-80 lines |

### 11H: Validation Criteria

- [ ] Player can designate a property as a front business
- [ ] Laundering pipeline processes dirty cash through all 5 stages
- [ ] Each laundering method has distinct risk/speed/capacity tradeoffs
- [ ] IRS attention meter rises with suspicious activity and decays over time
- [ ] Audit triggers fire at correct thresholds
- [ ] Audit process runs through all 4 stages with appropriate timing
- [ ] Business seizure removes property from player ownership
- [ ] UI displays all relevant data: dirty/clean cash, capacity, risk, per-business stats
- [ ] Accountant workers reduce IRS attention growth rate
- [ ] TransactionLedger records all laundering operations
- [ ] EventBus receives all laundering state change events

### 11I: Integration Test Scenarios

1. **Happy Path:** Player buys restaurant, sets CashIntensive method, launders $1,500/day for 10 days. Verify: clean cash increases, IRS attention stays below 0.4, no audit triggered.
2. **Overcapacity:** Player launders 3x restaurant capacity. Verify: suspicion level rises, IRS attention spikes, Stage 1 flag within 3 days.
3. **Full Audit Cascade:** Player deliberately triggers Stage 4. Verify: business seized, cash penalty applied, WantedLevel increases, connected businesses flagged.
4. **Method Rotation:** Player alternates Structuring and CashIntensive across 2 businesses. Verify: IRS attention stays lower than single-method approach.
5. **Accountant Mitigation:** Hire accountant worker, repeat overcapacity test. Verify: IRS attention growth rate reduced by accountant modifier.

---

## Step 12: Signature and Forensics System

### Design Intent

Every batch of product carries a forensic signature linking it to its origin facility. Law enforcement can seize product, analyze signatures, and build an evidence trail back to the player's operations. This creates a compelling reason to invest in scrubbing equipment, manage distribution carefully, and consider the forensic implications of every sale.

### 12A: BatchSignature

```
Scripts/Forensics/
    BatchSignature.cs
```

**Class: BatchSignature**

| Field | Type | Description |
|-------|------|-------------|
| `_signatureVector` | `float[512]` | 512-dimensional fingerprint |
| `_facilitySeed` | `int` | Derived from facility property ID |
| `_recipeHash` | `int` | Hash of recipe ScriptableObject used |
| `_workerSkillHash` | `int` | Hash of worker skill levels at time of craft |
| `_equipmentHash` | `int` | Hash of equipment configuration |
| `_randomVariance` | `float` | Per-batch random noise (0.0 - 0.1) |
| `_timestamp` | `float` | Game time of creation |
| `_scrubLevel` | `int` | Number of scrubbing passes applied |

**Signature Generation:**

```
vector[0..127]   = DeterministicNoise(facilitySeed)
vector[128..255] = DeterministicNoise(recipeHash)
vector[256..383] = DeterministicNoise(workerSkillHash XOR equipmentHash)
vector[384..511] = RandomNoise(variance)

// Normalize to unit vector
vector = Normalize(vector)
```

**Propagation Chain:**

```
CraftingStation creates batch -> BatchSignature attached to InventoryItem
    -> Player sells to dealer -> Signature copied to DealRecord
    -> Dealer sells to customer -> Signature in customer transaction
    -> If seized by police -> Signature enters ForensicLabAI
```

### 12B: SignatureDatabase

```
Scripts/Forensics/
    SignatureDatabase.cs
```

**Class: SignatureDatabase : MonoBehaviour**

| Field | Type | Description |
|-------|------|-------------|
| `_knownSignatures` | `Dictionary<int, List<BatchSignature>>` | Indexed by facility seed cluster |
| `_clusterThreshold` | `float` | Cosine similarity threshold for clustering (0.85 default) |
| `_degradationRate` | `float` | Signature reliability decay per game day |

**Key Methods:**

```csharp
public void RegisterSignature(BatchSignature sig, EvidenceSource source)
public List<SignatureCluster> GetClusters()
public float ComputeSimilarity(BatchSignature a, BatchSignature b)  // Cosine similarity
public List<BatchSignature> FindRelated(BatchSignature query, float threshold)
public void DegradeSignatures()  // Called daily, reduces traceability over time
```

**Cosine Similarity:**

```
similarity = dot(a.vector, b.vector) / (magnitude(a) * magnitude(b))

Thresholds:
    > 0.95 : Same facility, same batch run
    > 0.85 : Same facility, different batch
    > 0.70 : Same recipe, different facility
    < 0.70 : Unrelated
```

**Degradation Model:**

```
effectiveSimilarity = rawSimilarity * (1.0 - degradationRate * daysSinceCreation)
// After ~60 game days, signatures become unreliable
// Scrubbed signatures degrade 3x faster
```

### 12C: ForensicLabAI

```
Scripts/Forensics/
    ForensicLabAI.cs
```

**Class: ForensicLabAI : MonoBehaviour**

| Field | Type | Description |
|-------|------|-------------|
| `_evidenceQueue` | `Queue<EvidenceItem>` | Pending analysis |
| `_processingCapacity` | `int` | Items processed per game day |
| `_activeAnalysis` | `EvidenceItem` | Currently processing |
| `_analysisProgress` | `float` | 0.0 - 1.0 progress |
| `_investigationGraph` | `InvestigationGraph` | Reference to Step 15 graph (null until Step 15) |

**Evidence Sources:**

| Source | Quality | Processing Time |
|--------|---------|-----------------|
| Raid seizure | 0.9 - 1.0 | 1 game day |
| Arrest evidence | 0.7 - 0.9 | 2 game days |
| Street buy (undercover) | 0.5 - 0.7 | 3 game days |
| Informant tip | 0.3 - 0.5 | 5 game days |
| Trash pull | 0.1 - 0.3 | 7 game days |

**Processing Pipeline:**

```
1. Evidence enters queue with quality score
2. ForensicLabAI processes one item per day (upgradeable)
3. Signature extracted and compared against SignatureDatabase
4. If match found: edge added to InvestigationGraph (Step 15)
5. If no match: signature registered as new cluster origin
6. Quality affects confidence: low quality = weaker graph edges
```

**Key Methods:**

```csharp
public void SubmitEvidence(EvidenceItem evidence)
public void ProcessNextEvidence()
public ForensicResult AnalyzeSignature(BatchSignature sig)
public List<FacilityLink> GetFacilityLinks(BatchSignature sig)
```

### 12D: SignatureScrubber

```
Scripts/Forensics/
    SignatureScrubber.cs
```

**Class: SignatureScrubber : MonoBehaviour**

Attached to CraftingStation as an optional equipment upgrade.

| Field | Type | Description |
|-------|------|-------------|
| `_scrubLevel` | `int` | 1-3, each level adds noise to signature |
| `_yieldPenalty` | `float` | Output reduction per scrub level |
| `_noiseInjection` | `float` | Variance added to signature vector per level |
| `_upgradeCost` | `float[]` | Cost per level from GameBalanceConfig |

**Scrubbing Profiles:**

| Level | Yield Penalty | Noise Injection | Effective Similarity Reduction |
|-------|--------------|-----------------|-------------------------------|
| 1 | -5% | 0.15 | Signatures drop from 0.95 to ~0.80 |
| 2 | -12% | 0.30 | Signatures drop to ~0.65 (below cluster threshold) |
| 3 | -20% | 0.50 | Signatures effectively randomized |

**Tradeoff:** Level 3 scrubbing makes product untraceable but reduces output by 20%. Players must weigh forensic safety against profit margins.

### 12E: Integration Points

| System | Integration |
|--------|-------------|
| CraftingStation | Generates BatchSignature on craft completion, applies scrubber if present |
| DealManager | Copies signature to deal records |
| PropertyRaidSystem | Seized product submitted to ForensicLabAI |
| WantedSystem | Forensic links increase wanted level for connected facilities |
| TransactionLedger | Signature hash stored with every transaction |
| InvestigationGraph (Step 15) | ForensicLabAI adds nodes and edges to investigation graph |

### 12F: Files to Create

| File | Type | Lines (est.) |
|------|------|-------------|
| `Scripts/Forensics/BatchSignature.cs` | Data class | 120-150 |
| `Scripts/Forensics/SignatureDatabase.cs` | Singleton | 200-250 |
| `Scripts/Forensics/ForensicLabAI.cs` | MonoBehaviour | 250-300 |
| `Scripts/Forensics/SignatureScrubber.cs` | MonoBehaviour | 100-130 |

### 12G: Validation Criteria

- [ ] Every crafted batch receives a unique 512-bit signature
- [ ] Signatures from the same facility cluster with similarity > 0.85
- [ ] Signatures from different facilities show similarity < 0.70
- [ ] Scrubber level 2 breaks clustering (similarity < 0.70)
- [ ] Scrubber level 3 produces effectively random signatures
- [ ] Yield penalty applied correctly at each scrub level
- [ ] ForensicLabAI processes evidence queue at correct rate
- [ ] Evidence quality affects analysis confidence
- [ ] Signatures degrade over time, becoming unreliable after ~60 game days
- [ ] Seized product creates forensic links to origin facility

### 12H: Integration Test Scenarios

1. **Trace to Source:** Craft 5 batches at facility A. Sell to dealers. Police seize product from dealer. ForensicLabAI links to facility A cluster. Verify: correct facility identified.
2. **Scrubber Effectiveness:** Repeat test with Level 2 scrubber. Verify: ForensicLabAI cannot link to facility.
3. **Cross-Facility Confusion:** Craft same recipe at 2 facilities. Verify: signatures cluster separately (facility seed dominates).
4. **Degradation:** Create batch, wait 60 game days, seize. Verify: signature too degraded for reliable match.
5. **Evidence Quality:** Submit low-quality informant tip. Verify: weak edge in investigation graph, insufficient for warrant.

---

## Step 13: Advanced Economy and Market Simulator

### Design Intent

Replace the flat pricing model with a full supply/demand simulation. Prices respond to player actions, rival faction behavior, and random market events. The player can manipulate markets through strategic flooding, artificial scarcity, or cornering supply chains. This transforms the economy from a static backdrop into a dynamic adversary and opportunity.

### 13A: MarketSimulator

```
Scripts/Economy/
    MarketSimulator.cs
```

**Class: MarketSimulator : MonoBehaviour**

Replaces or wraps the existing EconomyManager with full market simulation.

| Field | Type | Description |
|-------|------|-------------|
| `_districtMarkets` | `Dictionary<DistrictId, DistrictMarket>` | Per-district market state |
| `_globalCommodityPrices` | `Dictionary<CommodityType, float>` | Precursor ingredient prices |
| `_activeEvents` | `List<MarketEvent>` | Currently active market events |
| `_priceHistory` | `Dictionary<ProductType, List<PricePoint>>` | Historical price data |
| `_updateInterval` | `float` | Market tick interval (1 game day) |

**DistrictMarket (nested class):**

| Field | Type | Description |
|-------|------|-------------|
| `supply` | `Dictionary<ProductType, float>` | Available supply per product |
| `demand` | `Dictionary<ProductType, float>` | Customer demand per product |
| `basePrice` | `Dictionary<ProductType, float>` | Equilibrium price |
| `currentPrice` | `Dictionary<ProductType, float>` | Actual price after modifiers |
| `elasticity` | `Dictionary<ProductType, float>` | Price sensitivity (0.0 = inelastic, 1.0 = elastic) |
| `competitorPresence` | `float` | Rival dealer density, suppresses prices |

**Price Calculation:**

```
supplyDemandRatio = supply / demand
priceModifier = 1.0 / pow(supplyDemandRatio, elasticity)
competitorModifier = 1.0 - (competitorPresence * competitorPriceImpact)
eventModifier = product of all active event modifiers
currentPrice = basePrice * priceModifier * competitorModifier * eventModifier
currentPrice = clamp(basePrice * 0.2, basePrice * 5.0)  // Floor/ceiling
```

**Market Manipulation Mechanics:**

| Action | Effect | Risk |
|--------|--------|------|
| Flood Market | Dump large volume, crashes price in district | Competitors move in, price war |
| Create Scarcity | Withhold supply, drives price up | Customers leave district, rivals fill gap |
| Corner Market | Control all supply of a product type | Maximum pricing power, highest heat |
| Price War | Undercut rival faction prices | Erodes margins, may trigger faction conflict |
| Quality Dump | Sell low-quality at premium | Short-term profit, reputation damage |

**Key Methods:**

```csharp
public float GetPrice(ProductType product, DistrictId district)
public float GetDemand(ProductType product, DistrictId district)
public void RecordSale(ProductType product, DistrictId district, float quantity)
public void RecordSupply(ProductType product, DistrictId district, float quantity)
public void TriggerMarketEvent(MarketEvent evt)
public MarketForecast GetForecast(ProductType product, DistrictId district, int days)
private void TickMarket()  // Daily update
private void UpdateSupplyDemand()
private void ProcessMarketEvents()
```

### 13B: MarketEvent ScriptableObject

```
Scripts/Economy/
    MarketEvent.cs

ScriptableObjects/Economy/Events/
    SO_Event_Drought.asset
    SO_Event_FestivalDemand.asset
    SO_Event_PortStrike.asset
    SO_Event_PoliceCrackdown.asset
    SO_Event_RivalBust.asset
    SO_Event_MediaExpose.asset
```

**Class: MarketEvent : ScriptableObject**

| Field | Type | Description |
|-------|------|-------------|
| `eventName` | `string` | Display name |
| `eventType` | `MarketEventType` | Enum category |
| `duration` | `int` | Duration in game days |
| `affectedProducts` | `ProductType[]` | Which products are impacted |
| `affectedDistricts` | `DistrictId[]` | Which districts, or All |
| `priceModifier` | `float` | Multiplier on price (1.5 = +50%) |
| `demandModifier` | `float` | Multiplier on demand |
| `supplyModifier` | `float` | Multiplier on supply |
| `probability` | `float` | Daily probability of random trigger |
| `triggerCondition` | `string` | Conditional trigger (e.g., "rival_busted") |
| `description` | `string` | Player-facing event description |

**Event Profiles:**

| Event | Duration | Price Mod | Demand Mod | Supply Mod | Trigger |
|-------|----------|-----------|------------|------------|---------|
| Drought | 14 days | 1.8x | 1.2x | 0.5x | Random (2%/day) |
| Festival Demand | 3 days | 1.5x | 2.0x | 1.0x | Calendar event |
| Port Strike | 21 days | 2.0x | 1.0x | 0.3x | Random (1%/day) |
| Police Crackdown | 7 days | 1.3x | 0.7x | 0.6x | High district heat |
| Rival Bust | 10 days | 1.6x | 1.0x | 0.4x | Rival faction arrested |
| Media Expose | 5 days | 0.7x | 0.8x | 1.0x | High player notoriety |

### 13C: CommodityTracker

```
Scripts/Economy/
    CommodityTracker.cs
```

**Class: CommodityTracker : MonoBehaviour**

Tracks global precursor ingredient prices that affect production costs.

| Field | Type | Description |
|-------|------|-------------|
| `_commodityPrices` | `Dictionary<CommodityType, float>` | Current prices |
| `_basePrices` | `Dictionary<CommodityType, float>` | Equilibrium prices |
| `_volatility` | `Dictionary<CommodityType, float>` | Daily price variance |
| `_priceHistory` | `Dictionary<CommodityType, List<float>>` | 90-day rolling history |

**Price Model (geometric Brownian motion simplified):**

```
dailyReturn = normalRandom(0, volatility)
newPrice = currentPrice * (1.0 + dailyReturn)
newPrice = clamp(basePrice * 0.3, basePrice * 4.0)
// Mean reversion: slight pull toward basePrice each day
meanReversionForce = (basePrice - currentPrice) * 0.02
newPrice += meanReversionForce
```

**Commodity Types:**

| Commodity | Base Price | Volatility | Used In |
|-----------|-----------|------------|---------|
| Pseudoephedrine | $50/unit | 0.05 | Methamphetamine |
| Methylamine | $200/unit | 0.08 | Methamphetamine (premium) |
| Coca Leaf | $30/unit | 0.12 | Cocaine |
| Acetone | $15/unit | 0.03 | Multiple recipes |
| Precursor Chemical A | $80/unit | 0.06 | Synthetic products |
| Cutting Agent | $10/unit | 0.02 | All products (dilution) |

### 13D: Integration Points

| System | Integration |
|--------|-------------|
| EconomyManager | MarketSimulator wraps or replaces EconomyManager price lookups |
| DealManager | Uses MarketSimulator.GetPrice() for deal pricing |
| CraftingStation | Production costs reference CommodityTracker prices |
| FactionAI (Step 14) | Rival factions participate in supply/demand simulation |
| EventBus | Market events published for UI and AI reaction |
| GameBalanceConfig | All elasticity values, event probabilities, price bounds |

### 13E: Files to Create

| File | Type | Lines (est.) |
|------|------|-------------|
| `Scripts/Economy/MarketSimulator.cs` | Singleton | 400-500 |
| `Scripts/Economy/MarketEvent.cs` | ScriptableObject | 60-80 |
| `Scripts/Economy/CommodityTracker.cs` | MonoBehaviour | 200-250 |

### 13F: Validation Criteria

- [ ] Prices respond to supply/demand changes within 1 game day
- [ ] Flooding a district with product reduces price proportional to elasticity
- [ ] Withholding supply increases price up to 5x ceiling
- [ ] Market events trigger at correct probabilities and apply correct modifiers
- [ ] Commodity prices follow Brownian motion with mean reversion
- [ ] Production costs reflect current commodity prices
- [ ] Rival faction sales affect district supply
- [ ] Price history tracked and queryable for forecast UI
- [ ] Competition presence suppresses prices in contested districts

### 13G: Integration Test Scenarios

1. **Supply Shock:** Remove all supply from a district for 5 days. Verify: price rises to ceiling, demand drops as customers leave.
2. **Market Flood:** Sell 10x normal volume in one day. Verify: price crashes, recovers over 3-5 days as demand absorbs.
3. **Port Strike Event:** Trigger port strike. Verify: affected commodity prices double, production costs increase, product prices rise.
4. **Rival Competition:** Spawn rival dealer in district. Verify: competitor presence increases, prices suppressed.
5. **Commodity Volatility:** Run 90-day simulation. Verify: commodity prices stay within bounds, mean-revert toward base.

---

## Step 14: Rival Faction AI

### Design Intent

The player is not operating in a vacuum. Three to five AI-controlled rival organizations compete for territory, customers, and market share. Each faction runs the same simulation loop as the player (production, distribution, territory control, laundering) but driven by AI decision-making with distinct personality profiles. Factions can be threatened, bribed, allied with, or destroyed.

### 14A: FactionManager Singleton

```
Scripts/AI/Factions/
    FactionManager.cs
```

**Class: FactionManager : MonoBehaviour**

| Field | Type | Description |
|-------|------|-------------|
| `_factions` | `List<FactionAI>` | Active rival factions |
| `_playerDisposition` | `Dictionary<FactionId, float>` | -1.0 (hostile) to 1.0 (allied) |
| `_factionRelations` | `Dictionary<(FactionId, FactionId), float>` | Inter-faction relations |
| `_maxFactions` | `int` | 3-5 per game (from GameBalanceConfig) |
| `_factionSpawnCooldown` | `float` | Minimum days between new faction spawns |

**Key Methods:**

```csharp
public void SpawnFaction(FactionProfile profile, DistrictId homeDistrict)
public void DissolveFaction(FactionId id)  // Faction destroyed
public float GetDisposition(FactionId faction)
public void ModifyDisposition(FactionId faction, float delta)
public List<FactionAI> GetFactionsInDistrict(DistrictId district)
public void ProcessFactionTurn()  // Called each game day
private void CheckFactionConflicts()
private void ProcessDiplomacy()
```

**Faction Spawn Profiles:**

| Faction Archetype | Personality | Starting Territory | Strength |
|-------------------|-------------|-------------------|----------|
| Street Gang | Aggressive, territorial | 1 district block | Low |
| Cartel Branch | Cautious, well-funded | 2-3 district blocks | High |
| Syndicate | Diplomatic, networked | Scattered properties | Medium |
| Lone Operator | Expansionist, risk-taking | Mobile, no fixed base | Low-Medium |
| Old Guard | Cautious, entrenched | 1 full district | Very High |

### 14B: FactionAI Component

```
Scripts/AI/Factions/
    FactionAI.cs
```

**Class: FactionAI : MonoBehaviour**

| Field | Type | Description |
|-------|------|-------------|
| `_factionId` | `FactionId` | Unique identifier |
| `_profile` | `FactionProfile` | ScriptableObject personality data |
| `_controlledTerritory` | `List<TerritoryBlock>` | Owned territory |
| `_properties` | `List<PropertyData>` | Owned properties |
| `_workers` | `List<WorkerData>` | Employed NPCs |
| `_cash` | `float` | Liquid funds |
| `_dirtyCash` | `float` | Unlaundered revenue |
| `_productInventory` | `Dictionary<ProductType, float>` | Product stockpile |
| `_militaryStrength` | `float` | Computed from workers + weapons |
| `_monthlyRevenue` | `float` | Rolling 30-day revenue |

**Decision Loop (per game day):**

```
1. ASSESS
   - Calculate military strength
   - Evaluate territory vulnerability
   - Check cash reserves
   - Survey rival positions

2. DECIDE (weighted by personality)
   aggressive_score   = threat_level * aggressiveness_weight
   expansion_score    = opportunity * expansionism_weight
   diplomatic_score   = ally_potential * diplomacy_weight
   defensive_score    = vulnerability * caution_weight

   action = highest_scoring_option

3. EXECUTE
   - EXPAND: Claim unclaimed territory or attack weakest neighbor
   - ATTACK: Send enforcers against rival or player territory
   - DEFEND: Fortify current territory, hire more workers
   - DIPLOMACY: Propose alliance, demand tribute, offer trade
   - PRODUCE: Invest in production capacity
   - LAUNDER: Process dirty cash through front businesses
   - LAY_LOW: Reduce activity if heat is high
```

**Personality Weights (FactionProfile ScriptableObject):**

| Weight | Aggressive | Cautious | Diplomatic | Expansionist |
|--------|-----------|----------|------------|--------------|
| aggressiveness | 0.8 | 0.2 | 0.3 | 0.5 |
| caution | 0.2 | 0.9 | 0.5 | 0.3 |
| diplomacy | 0.1 | 0.4 | 0.9 | 0.2 |
| expansionism | 0.5 | 0.2 | 0.3 | 0.9 |
| greed | 0.6 | 0.7 | 0.4 | 0.5 |

### 14C: FactionDiplomacy

```
Scripts/AI/Factions/
    FactionDiplomacy.cs
```

**Class: FactionDiplomacy**

| Diplomatic Action | Effect | Requirements |
|------------------|--------|--------------|
| Propose Alliance | Shared territory defense, trade routes | Disposition > 0.3 |
| Demand Tribute | Monthly payment for non-aggression | Military strength > target |
| Offer Trade | Exchange product types at negotiated prices | Disposition > 0.0 |
| Declare War | Open hostilities, territory contestable | Disposition < -0.5 |
| Peace Treaty | End hostilities, reset disposition to 0.0 | Both sides agree |
| Betray Alliance | Surprise attack from allied position | Random chance based on greed weight |

**Disposition Modifiers:**

| Event | Disposition Change |
|-------|-------------------|
| Player attacks faction territory | -0.3 |
| Player kills faction worker | -0.2 |
| Player completes trade deal | +0.1 |
| Player defends faction territory | +0.2 |
| Time without conflict | +0.01/day (max 0.3) |
| Tribute paid on time | +0.05 |
| Tribute missed | -0.15 |
| Alliance betrayal | -1.0 (immediate hostile) |

**Betrayal Mechanics:**

```
dailyBetrayalChance = greedWeight * opportunityFactor * (1.0 - disposition)
opportunityFactor = targetWeakness / ownStrength
if random() < dailyBetrayalChance:
    executeSurpriseAttack()
    disposition = -1.0
```

### 14D: DiplomacyUI

```
Scripts/UI/
    DiplomacyUI.cs
```

**OnGUI Layout:**

```
+------------------------------------------+
| FACTION OVERVIEW                         |
+------------------------------------------+
| [Los Diablos]                            |
|   Territory: 4 blocks  Strength: HIGH    |
|   Disposition: HOSTILE (-0.7)            |
|   [Negotiate] [Threaten] [Bribe]         |
|------------------------------------------|
| [White Dragon Triad]                     |
|   Territory: 2 blocks  Strength: MEDIUM  |
|   Disposition: NEUTRAL (0.1)             |
|   [Negotiate] [Propose Alliance] [Trade] |
|------------------------------------------|
| [The Commission]                         |
|   Territory: 6 blocks  Strength: V.HIGH  |
|   Disposition: WARY (-0.2)               |
|   [Negotiate] [Pay Tribute] [Bribe]      |
+------------------------------------------+
| ACTIVE AGREEMENTS                        |
| - Trade deal with White Dragon (14 days) |
| - Tribute to Commission: $5K/month       |
+------------------------------------------+
```

### 14E: Integration Points

| System | Integration |
|--------|-------------|
| TerritoryManager | Factions claim, contest, and lose territory blocks |
| MarketSimulator | Faction sales affect district supply/demand |
| WantedSystem | Faction activity generates heat in districts |
| PropertyManager | Factions own and operate properties |
| WorkerManager | Factions hire from shared NPC pool |
| CombatSystem | Faction enforcers use existing combat AI |
| EventBus | Faction state changes published for all systems |
| LaunderingManager | Factions launder cash through own front businesses |

### 14F: Files to Create

| File | Type | Lines (est.) |
|------|------|-------------|
| `Scripts/AI/Factions/FactionManager.cs` | Singleton | 300-350 |
| `Scripts/AI/Factions/FactionAI.cs` | MonoBehaviour | 400-500 |
| `Scripts/AI/Factions/FactionDiplomacy.cs` | Class | 250-300 |
| `Scripts/UI/DiplomacyUI.cs` | OnGUI | 200-250 |
| `ScriptableObjects/Factions/FactionProfile.cs` | ScriptableObject | 60-80 |

### 14G: Validation Criteria

- [ ] 3-5 factions spawn at game start with distinct personalities
- [ ] Each faction makes daily decisions consistent with personality weights
- [ ] Factions expand territory, produce product, and generate revenue
- [ ] Faction military strength computed correctly from workers and equipment
- [ ] Alliance proposals and trade deals function between player and factions
- [ ] War declaration enables territory contestation
- [ ] Betrayal mechanic triggers based on greed weight and opportunity
- [ ] Faction dissolution occurs when territory and cash reach zero
- [ ] DiplomacyUI displays all faction data and negotiation options
- [ ] Faction activity affects district market prices via MarketSimulator

### 14H: Integration Test Scenarios

1. **Peaceful Coexistence:** Play 30 days without attacking any faction. Verify: dispositions drift toward neutral, trade offers appear.
2. **Territory War:** Attack faction territory. Verify: disposition drops, faction retaliates, enforcers deployed.
3. **Alliance and Betrayal:** Form alliance with high-greed faction, weaken own defenses. Verify: betrayal triggers within probability window.
4. **Economic Competition:** Two factions selling in same district. Verify: prices suppressed, both factions adapt strategy.
5. **Faction Elimination:** Systematically destroy faction territory and workers. Verify: faction dissolves, territory becomes unclaimed.

---

## Step 15: Advanced Police and Investigation System

### Design Intent

Law enforcement operates as an intelligent adversary that builds cases over time. The investigation graph connects people, locations, assets, transactions, and forensic signatures into a prosecutable case. If the graph becomes dense enough, RICO charges threaten the entire empire. This is the ultimate tension system -- the player must manage visibility across all systems to avoid having the investigation graph reach critical mass.

### 15A: InvestigationGraph

```
Scripts/Police/Investigation/
    InvestigationGraph.cs
```

**Class: InvestigationGraph**

| Field | Type | Description |
|-------|------|-------------|
| `_nodes` | `Dictionary<int, GraphNode>` | All known entities |
| `_edges` | `List<GraphEdge>` | All connections |
| `_adjacencyList` | `Dictionary<int, List<int>>` | Fast neighbor lookup |
| `_nodeIdCounter` | `int` | Auto-incrementing ID |

**GraphNode Types:**

| Type | Description | Data Fields |
|------|-------------|-------------|
| Person | Player, worker, dealer, contact | Name, role, last seen location |
| Location | Property, street corner, district | Address, type, owner |
| Asset | Vehicle, equipment, cash account | Type, value, registered owner |
| Transaction | Financial exchange | Amount, date, parties, method |
| Batch | Product batch with signature | Signature hash, origin, quantity |

**GraphEdge Types:**

| Type | Description | Weight Range |
|------|-------------|-------------|
| Temporal | Two nodes active at same time/place | 0.1 - 0.5 |
| Financial | Money flow between nodes | 0.3 - 0.9 |
| Communication | Phone call, meeting, message | 0.2 - 0.6 |
| Physical | Physical proximity or possession | 0.4 - 0.8 |
| Forensic | Signature match linking batch to facility | 0.5 - 1.0 |
| Employment | Worker-employer relationship | 0.6 - 0.9 |
| Ownership | Property or asset ownership | 0.7 - 1.0 |

**Key Methods:**

```csharp
public int AddNode(GraphNodeType type, Dictionary<string, object> data)
public void AddEdge(int nodeA, int nodeB, GraphEdgeType type, float weight)
public void StrengthEdge(int nodeA, int nodeB, float delta)
public List<int> GetNeighbors(int nodeId)
public List<GraphEdge> GetEdges(int nodeId)
public List<int> FindPath(int from, int to)  // BFS shortest path
public float GetConnectionStrength(int nodeA, int nodeB)  // Sum of edge weights
public GraphComponent GetComponent(int nodeId)  // Connected component containing node
public int GetComponentSize(int nodeId)
public void DecayEdges(float rate)  // Called monthly, weakens old connections
```

### 15B: CentralityAnalyzer

```
Scripts/Police/Investigation/
    CentralityAnalyzer.cs
```

**Class: CentralityAnalyzer**

Runs graph analytics every 30 game days to identify investigation priorities.

| Analysis | Algorithm | Purpose |
|----------|-----------|---------|
| PageRank | Iterative link analysis | Identifies most important/connected nodes |
| Betweenness Centrality | Shortest path counting | Finds bridge nodes connecting subgraphs |
| Degree Centrality | Simple neighbor count | Quick importance heuristic |
| Clustering Coefficient | Triangle counting | Identifies tight-knit groups |

**PageRank Implementation:**

```
dampingFactor = 0.85
iterations = 20
initialRank = 1.0 / nodeCount

for each iteration:
    for each node:
        incomingRank = sum(neighbor.rank / neighbor.outDegree) for all incoming neighbors
        node.rank = (1 - dampingFactor) / nodeCount + dampingFactor * incomingRank
```

**Betweenness Centrality:**

```
for each node s:
    BFS from s to all reachable nodes
    count shortest paths through each intermediate node
    betweenness[node] += pathsThroughNode / totalPaths
normalize by (N-1)(N-2)/2
```

**Output: PriorityTarget list**

```
struct PriorityTarget:
    nodeId: int
    pageRank: float
    betweenness: float
    compositeScore: float  // weighted combination
    recommendedAction: enum { Surveil, Investigate, Warrant, Arrest }
```

### 15C: DetectiveAI

```
Scripts/Police/Investigation/
    DetectiveAI.cs
```

**Class: DetectiveAI : MonoBehaviour**

| Field | Type | Description |
|-------|------|-------------|
| `_activeCases` | `List<Case>` | Open investigations |
| `_maxCases` | `int` | Concurrent case capacity (3-5) |
| `_evidenceThresholds` | `EvidenceThresholds` | From GameBalanceConfig |
| `_investigationGraph` | `InvestigationGraph` | Shared graph reference |
| `_centralityAnalyzer` | `CentralityAnalyzer` | Analytics reference |

**Case Lifecycle:**

```
1. OPEN CASE
   Trigger: CentralityAnalyzer identifies high-priority target
   OR: WantedSystem escalation
   OR: ForensicLabAI links evidence to unknown subject

2. INVESTIGATE
   - Assign surveillance to target node
   - Gather evidence: observe transactions, tail suspects, review financial records
   - Each evidence item adds/strengthens graph edges
   - Duration: 7-30 game days depending on target visibility

3. BUILD CASE
   - Evaluate graph connectivity around target
   - Calculate prosecution strength:
     prosecutionStrength = sum(edge.weight) for all edges touching target
   - If prosecutionStrength > warrantThreshold: request warrant

4. WARRANT
   - Authorized raid on target location
   - Property search: discovers inventory, cash, equipment
   - Document seizure: strengthens financial edges
   - Product seizure: enters ForensicLabAI pipeline

5. PROSECUTION
   - Case strength = prosecutionStrength * evidenceQuality
   - Conviction probability = sigmoid(caseStrength - defenseStrength)
   - Defense strength: player can hire lawyer (reduces conviction probability)
   - Outcomes: acquittal, plea deal, conviction (asset forfeiture + jail time)
```

**Key Methods:**

```csharp
public void OpenCase(int targetNodeId, CasePriority priority)
public void AssignSurveillance(int caseId, int targetNodeId)
public void SubmitEvidence(int caseId, EvidenceItem evidence)
public float GetProsecutionStrength(int caseId)
public bool RequestWarrant(int caseId)
public CaseOutcome Prosecute(int caseId)
private void ProcessCases()  // Called each game day
private void ReviewPriorities()  // Called every 30 days with CentralityAnalyzer
```

### 15D: UndercoverAI

```
Scripts/Police/Investigation/
    UndercoverAI.cs
```

**Class: UndercoverAI : MonoBehaviour**

Plainclothes officers that infiltrate the player's organization.

| Field | Type | Description |
|-------|------|-------------|
| `_coverId` | `int` | Appears as normal WorkerData |
| `_trustLevel` | `float` | 0.0 - 1.0, built over time |
| `_informationGathered` | `List<IntelItem>` | Collected intelligence |
| `_exposureRisk` | `float` | Chance of being discovered |
| `_handler` | `DetectiveAI` | Reporting detective |
| `_missionDuration` | `int` | Days undercover |

**Infiltration Process:**

```
1. PLACEMENT
   - Undercover appears in worker hiring pool
   - Slightly above-average stats to encourage hiring
   - No obvious tells (loyalty appears normal)

2. TRUST BUILDING (30-60 game days)
   - Performs assigned work normally
   - Trust increases 0.01-0.02/day
   - At trust > 0.3: gains access to more sensitive operations
   - At trust > 0.6: can observe laundering, production details

3. INFORMATION GATHERING
   - Observes: worker roster, property layout, production volume
   - At higher trust: cash flows, supplier contacts, distribution routes
   - Each observation = potential graph node or edge for InvestigationGraph
   - Reports to handler every 7 game days (invisible to player)

4. EXPOSURE RISK
   exposureRisk += 0.005/day (base)
   exposureRisk += 0.02 per suspicious observation (e.g., counting cash)
   if player has security worker: exposureRisk += 0.01/day
   if random() < exposureRisk: cover blown

5. REVEAL / EXTRACTION
   - If cover blown: arrested or escapes (worker disappears + massive graph update)
   - If case ready: coordinated raid using gathered intelligence
   - If mission too long (>90 days): extracted to avoid exposure
```

**Detection Mechanics (player side):**

- Security worker occasionally flags suspicious behavior
- Player can interrogate workers (risk of false positive)
- Worker loyalty tracking: undercover loyalty never exceeds 0.7
- Paranoia system: checking too often reduces all worker morale

### 15E: RICO Charge System

```
Scripts/Police/Investigation/
    RICOCharge.cs
```

**Class: RICOCharge**

The ultimate law enforcement consequence. Triggered when the investigation graph reaches critical density connecting the player to criminal enterprise.

**RICO Trigger Conditions:**

```
componentSize = investigationGraph.GetComponentSize(playerNodeId)
totalEdgeWeight = sum of all edge weights in player's component
uniqueNodeTypes = count of distinct GraphNodeType in component

ricoScore = componentSize * 0.3 + totalEdgeWeight * 0.5 + uniqueNodeTypes * 0.2

RICO thresholds:
    ricoScore < 5.0  : No RICO risk
    ricoScore 5.0-8.0 : RICO warning (attorney advises)
    ricoScore > 8.0   : RICO charge filed
    ricoScore > 12.0  : RICO conviction near-certain
```

**RICO Consequences:**

| Component | Effect |
|-----------|--------|
| Properties | ALL connected properties seized |
| Cash | ALL cash (dirty and clean) frozen |
| Workers | ALL workers arrested or flee |
| Vehicles | ALL vehicles impounded |
| Reputation | Reset to zero |
| Wanted Level | Maximum |

**RICO Defense:**

- Compartmentalization: keep graph components disconnected (use cutouts, separate operations)
- Graph pruning: eliminate workers who know too much, abandon compromised properties
- Legal defense: hire attorney to challenge evidence quality, suppress weak edges
- Time: graph edges decay, reducing RICO score over time

### 15F: Integration Points

| System | Integration |
|--------|-------------|
| WantedSystem | Wanted level escalation opens cases, investigation graph informs wanted level |
| ForensicLabAI (Step 12) | Lab results add nodes/edges to investigation graph |
| LaunderingManager (Step 11) | Financial transactions create financial edges |
| FactionAI (Step 14) | Faction activities create graph noise (harder to isolate player) |
| PropertyManager | Owned properties are ownership edges in graph |
| WorkerManager | Employed workers are employment edges in graph |
| DealManager | Deals create transaction nodes and temporal edges |
| CombatSystem | Violent incidents create temporal/physical edges |
| TransactionLedger | All transactions feed into investigation graph |
| EventBus | Case updates, RICO warnings, warrant executions published |

### 15G: Files to Create

| File | Type | Lines (est.) |
|------|------|-------------|
| `Scripts/Police/Investigation/InvestigationGraph.cs` | Data structure | 350-400 |
| `Scripts/Police/Investigation/CentralityAnalyzer.cs` | Analytics | 250-300 |
| `Scripts/Police/Investigation/DetectiveAI.cs` | MonoBehaviour | 400-450 |
| `Scripts/Police/Investigation/UndercoverAI.cs` | MonoBehaviour | 300-350 |
| `Scripts/Police/Investigation/RICOCharge.cs` | Class | 200-250 |

### 15H: Validation Criteria

- [ ] InvestigationGraph supports all 5 node types and 7 edge types
- [ ] Graph operations (add, query, path-find) perform within 1ms for graphs up to 500 nodes
- [ ] PageRank converges within 20 iterations for typical game graphs
- [ ] Betweenness centrality correctly identifies bridge nodes
- [ ] DetectiveAI opens cases based on CentralityAnalyzer priority targets
- [ ] Case lifecycle progresses through all 5 stages with correct timing
- [ ] Warrant execution triggers property raid and evidence seizure
- [ ] Prosecution outcome reflects case strength vs defense strength
- [ ] Undercover agents appear as normal workers in hiring pool
- [ ] Undercover trust accumulates over time, unlocking deeper intelligence
- [ ] Exposure risk increases daily, accelerated by security workers
- [ ] RICO score correctly computed from graph component metrics
- [ ] RICO charge seizes all connected assets when threshold exceeded
- [ ] Graph edges decay over time, reducing RICO score naturally
- [ ] Compartmentalization strategy effectively reduces RICO risk

### 15I: Integration Test Scenarios

1. **Investigation Lifecycle:** Player operates openly for 30 days. Verify: DetectiveAI opens case, surveillance assigned, evidence accumulated, warrant requested at threshold.
2. **RICO Buildup:** Player runs large operation with 3 properties, 10 workers, active dealing. Verify: RICO score increases, warning at 5.0, charge at 8.0.
3. **Compartmentalization:** Player uses cutout workers (intermediaries). Verify: graph has disconnected components, RICO score lower than direct operation.
4. **Undercover Infiltration:** Undercover agent hired, operates for 45 days. Verify: intelligence gathered, graph populated, raid uses gathered intel.
5. **RICO Defense:** Player hires attorney, prunes compromised workers, waits for edge decay. Verify: RICO score decreases below threshold.
6. **Graph Performance:** Populate graph with 500 nodes, 2000 edges. Verify: all queries return within 1ms, PageRank completes within 50ms.

---

## Phase 3 Aggregate Metrics

### Script Count

| Step | New Scripts | Updated Scripts |
|------|------------|-----------------|
| Step 11: Money Laundering | 5 | 1 (TestArenaBuilder) |
| Step 12: Forensics | 4 | 1 (CraftingStation) |
| Step 13: Market Simulator | 3 | 1 (EconomyManager) |
| Step 14: Rival Factions | 5 | 0 |
| Step 15: Investigation | 5 | 1 (WantedSystem) |
| **Total** | **22** | **4** |

**Running total: ~161 scripts (139 Phase 1-2 + 22 Phase 3)**

### Dependency Graph

```
Step 11 (Laundering) -----> Step 13 (Market) via production costs
Step 12 (Forensics) ------> Step 15 (Investigation) via evidence pipeline
Step 13 (Market) ----------> Step 14 (Factions) via supply/demand
Step 14 (Factions) --------> Step 15 (Investigation) via graph noise
Step 11 (Laundering) -----> Step 15 (Investigation) via financial edges
```

**Recommended build order:** 11 -> 12 -> 13 -> 14 -> 15

Steps 11 and 12 can be built in parallel as they have no mutual dependencies. Step 13 should follow 11 (laundering costs reference market prices). Step 14 requires 13 (factions participate in market). Step 15 requires 12 and integrates with all prior steps.

### Cross-System Integration Tests

1. **Full Pipeline:** Craft product (signature generated) -> Sell to dealer -> Police seize from dealer -> ForensicLabAI links to facility -> DetectiveAI opens case -> Investigation builds -> Warrant issued -> Raid discovers laundering records -> RICO score increases.

2. **Faction Interference:** Rival faction operates in same district -> Market prices shift -> Player adapts strategy -> Faction war erupts -> Police investigate both -> Investigation graph contains both player and faction nodes -> Faction activity creates noise that delays player case.

3. **Economic Cascade:** Port strike event -> Commodity prices spike -> Production costs increase -> Player raises prices -> Customers go to rival faction -> Player attacks faction -> Violence increases heat -> Police investigation accelerates -> Player must launder emergency funds -> IRS attention spikes -> Simultaneous IRS audit and police warrant.

---

## Phase 3 Exit Criteria

Phase 3 is complete when:

1. All 22 new scripts compile without errors
2. All validation criteria for Steps 11-15 pass
3. All integration test scenarios produce expected results
4. TestArenaBuilder demonstrates all Phase 3 systems in a single session
5. Performance: game maintains 60fps with all Phase 3 systems active
6. No regressions in Phase 1-2 functionality
7. GameBalanceConfig contains all Phase 3 tuning values
8. EventBus handles all Phase 3 event types without dropped messages
